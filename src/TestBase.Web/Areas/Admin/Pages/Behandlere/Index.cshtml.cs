using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Behandlere;

public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public IndexModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public List<Behandler> Behandlere { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Behandlere = await _db.Behandlere.OrderByDescending(b => b.OpprettetUtc).ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostFrysAsync(long id, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null && behandler.Status != BehandlerStatus.Arkivert)
        {
            behandler.Status = behandler.Status == BehandlerStatus.Fryst ? BehandlerStatus.Aktiv : BehandlerStatus.Fryst;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "EndreBehandlerStatus",
                nameof(Behandler), behandler.Id.ToString(), behandler.Status.ToString(), cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostArkiverAsync(long id, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.Status = BehandlerStatus.Arkivert;
            behandler.ArkivertUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "ArkiverBehandler",
                nameof(Behandler), behandler.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGodkjennHprAsync(long id, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.HprGodkjent = !behandler.HprGodkjent;
            var administratorId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var aid) ? aid : (long?)null;
            behandler.HprGodkjentAvAdministratorId = behandler.HprGodkjent ? administratorId : null;
            behandler.HprGodkjentUtc = behandler.HprGodkjent ? DateTimeOffset.UtcNow : null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(),
                behandler.HprGodkjent ? "GodkjennHpr" : "TilbakekallHprGodkjenning",
                nameof(Behandler), behandler.Id.ToString(), $"HPR-nr {behandler.HprNr}", cancellationToken);
        }

        return RedirectToPage();
    }
}
