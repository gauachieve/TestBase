using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Tester.Prising;

/// <summary>
/// Superadmin-only (se SuperadminOmrade i Program.cs, egen undermappe av
/// /Tester nettopp for å kunne strengere-gate KUN denne siden) — definerer
/// Min/Maks-pris, typisk behandler-honorar og minste partnerandel per test.
/// Se docs/beslutningslogg.md "Partner System + Test Monetization".
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

    public List<Test> Tester { get; private set; } = new();
    public string? Feilmelding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Tester = await _db.Tester.OrderBy(t => t.Navn).ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(
        long testId, decimal minstePrisKr, decimal storstePrisKr, decimal typiskBehandlerHonorarKr, decimal minstePartnerAndelKr,
        CancellationToken cancellationToken)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (test is null)
        {
            return NotFound();
        }

        // Sikrer at prisformelen i TestPrisberegner aldri havner i en umulig
        // tilstand — plattform- og partnerandelen er garanterte gulv, de må
        // til sammen få plass under maks-grensen.
        if (minstePrisKr + minstePartnerAndelKr > storstePrisKr)
        {
            Feilmelding = $"For «{test.Navn}»: minste pris ({minstePrisKr:0.00}) pluss minste partnerandel ({minstePartnerAndelKr:0.00}) kan ikke overstige største pris ({storstePrisKr:0.00}).";
            Tester = await _db.Tester.OrderBy(t => t.Navn).ToListAsync(cancellationToken);
            return Page();
        }

        test.MinstePrisKr = minstePrisKr;
        test.StorstePrisKr = storstePrisKr;
        test.TypiskBehandlerHonorarKr = typiskBehandlerHonorarKr;
        test.MinstePartnerAndelKr = minstePartnerAndelKr;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterTestPrising",
            nameof(Test), test.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage();
    }
}
