using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages;

/// <summary>
/// Behandlers personlige side: uleste meldinger (se BehandlerMelding — én per
/// pasient som nettopp har fullført en test), fullførte tester som venter på
/// godkjenning, tester tildelt egne pasienter som ennå ikke er besvart, og
/// (for partner-admin) kollegers utløpte HPR-frister — alt sammen tidligere
/// spredt over en egen "Oppgaver"-side, slått sammen hit (bugliste 2026-09-13
/// punkt 22) siden de i praksis alltid ble sjekket sammen.
/// </summary>
[Authorize(Policy = "BehandlerOmrade")]
public sealed class MinSideModel : PageModel
{
    private readonly BehandlerMeldingService _meldingService;
    private readonly TestService _testService;
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public MinSideModel(BehandlerMeldingService meldingService, TestService testService, AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _meldingService = meldingService;
        _testService = testService;
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public IReadOnlyList<MeldingMedDetaljer> UlesteMeldinger { get; private set; } = Array.Empty<MeldingMedDetaljer>();
    public IReadOnlyList<TestService.TildelingMedTestOgPasient> VenterPaaGodkjenning { get; private set; } = Array.Empty<TestService.TildelingMedTestOgPasient>();
    public IReadOnlyList<TestService.TildelingMedTestOgPasient> IkkeFullfort { get; private set; } = Array.Empty<TestService.TildelingMedTestOgPasient>();
    public List<Behandler> UtlopteHprFrister { get; private set; } = new();

    public int AntallOppgaver => UlesteMeldinger.Count + VenterPaaGodkjenning.Count + UtlopteHprFrister.Count;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        UlesteMeldinger = await _meldingService.HentUlesteAsync(behandlerId, cancellationToken);
        VenterPaaGodkjenning = await _testService.HentUgodkjenteFullforteForBehandlerAsync(behandlerId, cancellationToken);
        IkkeFullfort = await _testService.HentIkkeFullforteForBehandlerAsync(behandlerId, cancellationToken);

        if (_currentUser.ErPartnerAdministrator && _currentUser.PartnerId is not null)
        {
            var kolleger = await _db.Behandlere
                .Where(b => b.PartnerId == _currentUser.PartnerId && !b.HprGodkjent && b.Status != BehandlerStatus.Arkivert && !b.ErSlettet && b.RegistrertUtc != null)
                .ToListAsync(cancellationToken);

            UtlopteHprFrister = kolleger
                .Where(b => HprPolicy.ErUtlopt(b, DateTimeOffset.UtcNow))
                .OrderBy(b => HprPolicy.BeregnFrist(b))
                .ToList();
        }
    }

    public async Task<IActionResult> OnPostGodkjennHprAsync(long id, CancellationToken cancellationToken)
    {
        if (!_currentUser.ErPartnerAdministrator || _currentUser.PartnerId is null)
        {
            return Forbid();
        }

        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id && b.PartnerId == _currentUser.PartnerId, cancellationToken);
        if (behandler is not null)
        {
            // HprGodkjentAvAdministratorId er reservert for en faktisk Administrator-Id —
            // en partner-admin ER en Behandler, ikke en Administrator, så feltet står
            // bevisst urørt her. Hvem som godkjente står uansett i audit-loggen under.
            behandler.HprGodkjent = true;
            behandler.HprGodkjentUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GodkjennHpr",
                nameof(Behandler), behandler.Id.ToString(), $"HPR-nr {behandler.HprNr}", cancellationToken);
        }

        return RedirectToPage();
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
