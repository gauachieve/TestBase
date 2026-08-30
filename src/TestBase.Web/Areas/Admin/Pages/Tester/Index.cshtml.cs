using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.InnebygdeTester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Tester;

public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly IEnumerable<IInnebygdTestSeeder> _seedere;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public IndexModel(
        AppDbContext db, TestService testService, IEnumerable<IInnebygdTestSeeder> seedere,
        IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _seedere = seedere;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public List<Test> Tester { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Tester = await _db.Tester.OrderBy(t => t.Navn).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Regenererer innebygde, kode-definerte tester (WHO-5 m.fl.) idempotent —
    /// jf. kravdokumentet "husk alltid å lage regenerering av tester". Samme
    /// mekanisme kjøres også i dev-seed, se Program.cs.
    /// </summary>
    public async Task<IActionResult> OnPostRegenererAsync(CancellationToken cancellationToken)
    {
        foreach (var seeder in _seedere)
        {
            await seeder.SeedAsync(_testService, cancellationToken);
        }

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "RegenererInnebygdeTester",
            nameof(Test), "n/a", cancellationToken: cancellationToken);

        return RedirectToPage();
    }
}
