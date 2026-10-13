using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages;

/// <summary>
/// Driftsstatus-dashboard (brukerbeslutning 2026-10-13) — en rask oversikt over om de
/// eksterne leverandørene og databasen faktisk fungerer, for admin/superadmin/utvikler.
/// Se DriftsstatusService for hva som faktisk sjekkes og hva som BEVISST ikke er dekket ennå
/// (Azure-ressurshelse, "er siden nede"-overvåking).
/// </summary>
[Authorize(Policy = "AdminOmrade")]
public sealed class DriftsstatusModel : PageModel
{
    private readonly DriftsstatusService _driftsstatus;
    private readonly DriftsbryterInnstillingService _driftsbrytere;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _auditLogger;

    public DriftsstatusModel(
        DriftsstatusService driftsstatus, DriftsbryterInnstillingService driftsbrytere,
        ICurrentUserContext currentUser, IAuditLogger auditLogger)
    {
        _driftsstatus = driftsstatus;
        _driftsbrytere = driftsbrytere;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public IReadOnlyList<DriftsstatusSjekk> Sjekker { get; private set; } = Array.Empty<DriftsstatusSjekk>();
    public DateTimeOffset SistOppdatertUtc { get; private set; }

    /// <summary>Push-varsel-oppsettet (Pushover) er vist til Superadmin/Utvikler, samme gruppe som de andre driftsbryterne — se DriftsstatusVarslingBakgrunnstjeneste (TestBase.Web) for selve utsendingsjobben.</summary>
    public bool VisKritiskVarslingOppsett => _currentUser.Role is UserRole.Superadmin or UserRole.Utvikler;
    public bool KritiskVarslingErAktivNaa { get; private set; }
    public string? KritiskVarslingPushoverBrukerKeyNaa { get; private set; }
    public bool Lagret { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Sjekker = await _driftsstatus.KjorAlleSjekkerAsync(cancellationToken);
        SistOppdatertUtc = DateTimeOffset.UtcNow;

        if (VisKritiskVarslingOppsett)
        {
            var innstilling = await _driftsbrytere.HentAsync(cancellationToken);
            KritiskVarslingErAktivNaa = innstilling.KritiskVarslingErAktiv;
            KritiskVarslingPushoverBrukerKeyNaa = innstilling.KritiskVarslingPushoverBrukerKey;
        }

        Lagret = TempData["KritiskVarslingLagret"] as bool? ?? false;
    }

    public async Task<IActionResult> OnPostSettKritiskVarslingAsync(bool erAktiv, string? pushoverBrukerKey, CancellationToken cancellationToken)
    {
        if (_currentUser.Role is not (UserRole.Superadmin or UserRole.Utvikler))
        {
            return Forbid();
        }

        var userId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var uid) ? uid : 0;
        await _driftsbrytere.SettKritiskVarslingAsync(erAktiv, pushoverBrukerKey, userId, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "EndreKritiskVarslingOppsett",
            nameof(Shared.Domain.Administrasjon.DriftsbryterInnstilling), "1", $"ErAktiv={erAktiv}", cancellationToken);

        TempData["KritiskVarslingLagret"] = true;
        return RedirectToPage();
    }
}
