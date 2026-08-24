using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Tester;

public sealed class SiderModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public SiderModel(AppDbContext db, TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public Test? Test { get; private set; }
    public List<TestSide> Sider { get; private set; } = new();

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public string? Instruksjon { get; set; }

    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        Test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (Test is null)
        {
            return NotFound();
        }

        Sider = await _db.TestSider.Where(s => s.TestId == id).OrderBy(s => s.Rekkefolge).ToListAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Navn er obligatorisk.";
            Test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            Sider = await _db.TestSider.Where(s => s.TestId == id).OrderBy(s => s.Rekkefolge).ToListAsync(cancellationToken);
            return Page();
        }

        var side = await _testService.LeggTilSideAsync(id, Navn, Instruksjon, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "LeggTilTestSide",
            nameof(TestSide), side.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage(new { id });
    }
}
