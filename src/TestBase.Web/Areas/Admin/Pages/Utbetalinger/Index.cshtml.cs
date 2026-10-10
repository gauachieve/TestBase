using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Utbetaling;

namespace TestBase.Web.Areas.Admin.Pages.Utbetalinger;

/// <summary>
/// Liste over alle månedlige utbetalingsbatcher — SuperadminOmrade (se
/// Program.cs), samme trygghetsnivå som /Partnere og /Tester/Prising, men
/// strengere begrunnet: POST-handlerne på Detaljer-siden flytter ekte penger.
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<UtbetalingsBatch> Batcher { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Batcher = await _db.UtbetalingsBatcher
            .OrderByDescending(b => b.Aar).ThenByDescending(b => b.Maned)
            .ToListAsync(cancellationToken);
    }
}
