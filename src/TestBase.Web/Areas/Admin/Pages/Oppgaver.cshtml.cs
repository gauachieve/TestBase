using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages;

/// <summary>
/// Admins oppgaveliste — bugliste 2026-09-13 punkt 11 fylte den med den
/// første ekte oppgavetypen: behandlere med utløpt, ugodkjent HPR-frist.
/// Delt datakilde med alle andre admin-kontoer (og partner-admin sin
/// filtrerte visning på Behandlerportal/Oppgaver) — når én godkjenner,
/// forsvinner den fra alles liste automatisk siden det er DB-tilstand, ikke
/// noe lokalt per bruker.
/// </summary>
[Authorize(Policy = "AdminOmrade")]
public sealed class OppgaverModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public OppgaverModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public List<Behandler> UtlopteHprFrister { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LastOppgaverAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostGodkjennHprAsync(long id, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.HprGodkjent = true;
            var administratorId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var aid) ? aid : (long?)null;
            behandler.HprGodkjentAvAdministratorId = administratorId;
            behandler.HprGodkjentUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GodkjennHpr",
                nameof(Behandler), behandler.Id.ToString(), $"HPR-nr {behandler.HprNr}", cancellationToken);
        }

        return RedirectToPage();
    }

    private async Task LastOppgaverAsync(CancellationToken cancellationToken)
    {
        var behandlere = await _db.Behandlere
            .Where(b => !b.HprGodkjent && b.Status != BehandlerStatus.Arkivert && !b.ErSlettet && b.RegistrertUtc != null)
            .ToListAsync(cancellationToken);

        UtlopteHprFrister = behandlere
            .Where(b => TestBase.Shared.Domain.Administrasjon.HprPolicy.ErUtlopt(b, DateTimeOffset.UtcNow))
            .OrderBy(b => TestBase.Shared.Domain.Administrasjon.HprPolicy.BeregnFrist(b))
            .ToList();
    }
}
