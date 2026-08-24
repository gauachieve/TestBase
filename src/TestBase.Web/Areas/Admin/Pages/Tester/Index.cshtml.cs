using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;

namespace TestBase.Web.Areas.Admin.Pages.Tester;

public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Test> Tester { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Tester = await _db.Tester.OrderBy(t => t.Navn).ToListAsync(cancellationToken);
    }
}
