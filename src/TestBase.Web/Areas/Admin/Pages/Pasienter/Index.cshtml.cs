using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Pasienter;

/// <summary>
/// Admins oversikt over ALLE pasienter på tvers av behandlere — reint
/// lesetilgang (patient-CRUD hører til behandler, se Behandlerportal/Pasienter),
/// samme grunnleggende idé som Behandlerportal/Pasienter/Index men med en
/// Behandler-kolonne siden admin ser flere behandleres pasienter samtidig
/// (samme mønster som Admin/Tildel/Pasienter). Superadmin kan i tillegg se
/// SLETTEDE pasienter her og gjenopprette dem — se
/// docs/beslutningslogg.md "Bugliste 2026-09-13".
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _auditLogger;

    public IndexModel(AppDbContext db, TestService testService, ICurrentUserContext currentUser, IAuditLogger auditLogger)
    {
        _db = db;
        _testService = testService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public sealed record PasientRad(Pasient Pasient, string? BehandlerNavn, string? PartnerNavn, int Tildelt, int Besvart);

    public List<PasientRad> Rader { get; private set; } = new();
    public bool ErSuperadmin => _currentUser.Role == UserRole.Superadmin;
    public bool VisSlettede { get; private set; }

    public async Task OnGetAsync(bool visSlettede, CancellationToken cancellationToken)
    {
        VisSlettede = visSlettede && ErSuperadmin;

        var sporring = _db.Pasienter.AsQueryable();
        if (!VisSlettede)
        {
            sporring = sporring.Where(p => !p.ErSlettet);
        }

        var pasienter = await sporring.OrderByDescending(p => p.OpprettetUtc).ToListAsync(cancellationToken);

        var behandlere = await _db.Behandlere.ToListAsync(cancellationToken);
        var behandlerNavnById = behandlere.ToDictionary(b => b.Id, b => b.Visningsnavn);
        var partnerIdPerBehandlerId = behandlere.ToDictionary(b => b.Id, b => b.PartnerId);
        var partnerNavnById = (await _db.Partnere.ToListAsync(cancellationToken)).ToDictionary(p => p.Id, p => p.Navn);

        var tellinger = await _testService.HentTildelingTellingerAsync(pasienter.Select(p => p.Id).ToList(), cancellationToken);

        Rader = pasienter.Select(p =>
        {
            var telling = tellinger.GetValueOrDefault(p.Id, new TestService.TildelingTelling(0, 0));
            var partnerId = partnerIdPerBehandlerId.GetValueOrDefault(p.BehandlerId);
            var partnerNavn = partnerId is null ? null : partnerNavnById.GetValueOrDefault(partnerId.Value);
            return new PasientRad(p, behandlerNavnById.GetValueOrDefault(p.BehandlerId), partnerNavn, telling.Tildelt, telling.Besvart);
        }).ToList();
    }

    public async Task<IActionResult> OnPostGjenopprettFraSlettetAsync(long id, CancellationToken cancellationToken)
    {
        if (!ErSuperadmin)
        {
            return Forbid();
        }

        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pasient is not null)
        {
            pasient.ErSlettet = false;
            pasient.SlettetUtc = null;
            // Gjenopprettes til Invitert, ikke Aktiv — samme regel som Behandlerportal/
            // Pasienter/Index sin Arkiver-handler (ingen pasient kan bli Aktiv før
            // egen fullført registrering).
            pasient.Status = PasientStatus.Invitert;
            pasient.ArkivertUtc = null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GjenopprettSlettetPasient",
                nameof(Pasient), pasient.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage(new { visSlettede = true });
    }
}
