using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Tester;

public sealed class LeddModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public LeddModel(AppDbContext db, TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public TestSide? Side { get; private set; }
    public List<TestLedd> LeddListe { get; private set; } = new();

    [BindProperty]
    public string Sporsmalstekst { get; set; } = string.Empty;

    [BindProperty]
    public string? Instruksjon { get; set; }

    [BindProperty]
    public TestSvartype Svartype { get; set; }

    [BindProperty]
    public string? Svaralternativer { get; set; }

    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        Side = await _db.TestSider.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (Side is null)
        {
            return NotFound();
        }

        LeddListe = await _db.TestLedd.Where(l => l.TestSideId == id).OrderBy(l => l.Rekkefolge).ToListAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Sporsmalstekst))
        {
            Feilmelding = "Spørsmålstekst er obligatorisk.";
            Side = await _db.TestSider.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            LeddListe = await _db.TestLedd.Where(l => l.TestSideId == id).OrderBy(l => l.Rekkefolge).ToListAsync(cancellationToken);
            return Page();
        }

        var ledd = await _testService.LeggTilLeddAsync(id, Sporsmalstekst, Instruksjon, Svartype, Svaralternativer, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "LeggTilTestLedd",
            nameof(TestLedd), ledd.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage(new { id });
    }
}
