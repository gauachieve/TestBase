using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Administratorer;

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

    public List<Administrator> Administratorer { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Administratorer = await _db.Administratorer.OrderBy(a => a.AdminId).ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostArkiverAsync(long id, CancellationToken cancellationToken)
    {
        var administrator = await _db.Administratorer.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (administrator is not null)
        {
            administrator.ErArkivert = !administrator.ErArkivert;
            administrator.ArkivertUtc = administrator.ErArkivert ? DateTimeOffset.UtcNow : null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId,
                _currentUser.Role.ToString(),
                administrator.ErArkivert ? "ArkiverAdministrator" : "GjenopprettAdministrator",
                nameof(Administrator),
                administrator.Id.ToString(),
                cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }
}
