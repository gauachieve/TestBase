using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

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

    public List<Pasient> Pasienter { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        Pasienter = await _db.Pasienter
            .Where(p => p.BehandlerId == behandlerId)
            .OrderByDescending(p => p.OpprettetUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostArkiverAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id && p.BehandlerId == behandlerId, cancellationToken);
        if (pasient is not null)
        {
            var arkiveres = pasient.Status != PasientStatus.Arkivert;
            // Gjenopprettes til Invitert, ikke Aktiv — ingen pasient kan bli Aktiv før
            // Del 4 bygger pasientens egen fullføringsside (se PasientStatus).
            pasient.Status = arkiveres ? PasientStatus.Arkivert : PasientStatus.Invitert;
            pasient.ArkivertUtc = arkiveres ? DateTimeOffset.UtcNow : null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(),
                arkiveres ? "ArkiverPasient" : "GjenopprettPasient",
                nameof(Pasient), pasient.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
