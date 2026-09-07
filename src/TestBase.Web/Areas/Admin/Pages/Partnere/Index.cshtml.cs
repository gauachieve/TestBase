using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Partnere;

/// <summary>
/// Superadmin-only (se SuperadminOmrade i Program.cs) — se
/// docs/beslutningslogg.md "Partner System + Test Monetization".
/// </summary>
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

    public List<Partner> Partnere { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Partnere = await _db.Partnere.OrderByDescending(p => p.OpprettetUtc).ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostArkiverAsync(long id, CancellationToken cancellationToken)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is not null)
        {
            partner.ErArkivert = !partner.ErArkivert;
            partner.ArkivertUtc = partner.ErArkivert ? DateTimeOffset.UtcNow : null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "EndreArkivertPartner",
                nameof(Partner), partner.Id.ToString(), partner.ErArkivert.ToString(), cancellationToken);
        }

        return RedirectToPage();
    }
}
