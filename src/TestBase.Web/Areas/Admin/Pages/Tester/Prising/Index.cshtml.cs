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
///
/// Ett samlet skjema/én Lagre-knapp for ALLE rader (bugliste 2026-09-13 punkt
/// 12) — tidligere var hver rad sitt eget skjema, som gjorde at et
/// "Lagre"-klikk på én rad lastet siden på nytt og forkastet utfylte, men
/// ulagrede tall i alle andre rader. Feltene leses manuelt fra Request.Form
/// (samme "HonorarKr[...]"-mønster som Tildel/Tester.cshtml.cs bruker, se
/// CLAUDE.md sin fallgruve om [BindProperty] på et Dictionary som kan komme
/// tomt) i stedet for én binding-parameter per felt.
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

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Tester = await _db.Tester.OrderBy(t => t.Navn).ToListAsync(cancellationToken);
        var feilmeldinger = new List<string>();

        foreach (var test in Tester)
        {
            var minstePrisKr = LesDecimal($"MinstePrisKr[{test.Id}]", test.MinstePrisKr);
            var storstePrisKr = LesDecimal($"StorstePrisKr[{test.Id}]", test.StorstePrisKr);
            var typiskBehandlerHonorarKr = LesDecimal($"TypiskBehandlerHonorarKr[{test.Id}]", test.TypiskBehandlerHonorarKr);
            var minstePartnerAndelKr = LesDecimal($"MinstePartnerAndelKr[{test.Id}]", test.MinstePartnerAndelKr);

            // Sikrer at prisformelen i TestPrisberegner aldri havner i en umulig
            // tilstand — plattform- og partnerandelen er garanterte gulv, de må
            // til sammen få plass under maks-grensen.
            if (minstePrisKr + minstePartnerAndelKr > storstePrisKr)
            {
                feilmeldinger.Add($"«{test.Navn}»: minste pris ({minstePrisKr:0.00}) pluss minste partnerandel ({minstePartnerAndelKr:0.00}) kan ikke overstige største pris ({storstePrisKr:0.00}).");
                continue;
            }

            test.MinstePrisKr = minstePrisKr;
            test.StorstePrisKr = storstePrisKr;
            test.TypiskBehandlerHonorarKr = typiskBehandlerHonorarKr;
            test.MinstePartnerAndelKr = minstePartnerAndelKr;
        }

        if (feilmeldinger.Count > 0)
        {
            Feilmelding = string.Join(" ", feilmeldinger) + " Ingen endringer ble lagret.";
            return Page();
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterTestPrising",
            nameof(Test), string.Join(",", Tester.Select(t => t.Id)), cancellationToken: cancellationToken);

        return RedirectToPage();
    }

    private decimal LesDecimal(string feltnavn, decimal fallback)
    {
        var raw = Request.Form[feltnavn].ToString();
        return decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var verdi)
            ? verdi
            : fallback;
    }
}
