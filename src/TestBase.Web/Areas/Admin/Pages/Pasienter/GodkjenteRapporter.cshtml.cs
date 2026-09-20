using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;

namespace TestBase.Web.Areas.Admin.Pages.Pasienter;

/// <summary>
/// Admins (og superadmin/dev, se AdminOmrade-policyen som allerede dekker begge)
/// liste over ALLE godkjente rapporter for én pasient, på tvers av behandlere —
/// samme funksjonalitet som Behandlerportal-motstykket, men uten eierskapssjekk
/// siden admin ser alle pasienter (jf. Admin/Pasienter/Index). Lenket fra en
/// egen knapp per rad der. Rapportvisningen har sin egen, admin-lokale
/// Rapport.cshtml siden Behandlerportal/Pasienter/Rapport krever at innlogget
/// bruker HAR en BehandlerId som eier pasienten.
/// </summary>
public sealed class GodkjenteRapporterModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;

    public GodkjenteRapporterModel(AppDbContext db, TestService testService)
    {
        _db = db;
        _testService = testService;
    }

    public sealed record GodkjentRapportRad(TestTildeling Tildeling, string TestNavn);

    public Pasient? Pasient { get; private set; }
    public List<GodkjentRapportRad> Rapporter { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        Pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (Pasient is null)
        {
            return NotFound();
        }

        var tildelinger = await _testService.HentGodkjenteForPasientAsync(id, kunSynligForPasient: false, cancellationToken);
        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var testNavn = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);

        Rapporter = tildelinger.Select(t => new GodkjentRapportRad(t, testNavn.GetValueOrDefault(t.TestId, "(ukjent test)"))).ToList();
        return Page();
    }
}
