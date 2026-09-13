using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages;

/// <summary>
/// Admins "Min side" — tidligere en egen "Oppgaver"-side, slått sammen hit
/// (bugliste 2026-09-13 punkt 22, samme prinsipp som Behandlerportal/MinSide)
/// siden Admin ikke har noen annen personlig side (meldingsinnboks finnes
/// kun for Behandler). Eneste oppgavetype foreløpig: behandlere med utløpt,
/// ugodkjent HPR-frist (punkt 11) — delt datakilde med partner-admin sin
/// filtrerte visning på Behandlerportal/MinSide.
/// </summary>
[Authorize(Policy = "AdminOmrade")]
public sealed class MinSideModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public MinSideModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser)
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
            .Where(b => HprPolicy.ErUtlopt(b, DateTimeOffset.UtcNow))
            .OrderBy(b => HprPolicy.BeregnFrist(b))
            .ToList();
    }
}
