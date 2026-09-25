using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.Skaaring;

namespace TestBase.Web.Areas.Admin.Pages.Pasienter;

/// <summary>
/// Admins (og superadmin/dev) reine lesetilgang til én godkjent rapport, lenket fra
/// GodkjenteRapporter.cshtml. Bevisst uten godkjenn/forkast/send-handlinger — det er
/// behandlers ansvar (se Behandlerportal/Pasienter/Rapport.cshtml.cs), admin skal bare
/// kunne SE den. Viser rapporten uavhengig av RapportSynligForPasient (i motsetning til
/// pasientens egen visning), siden admin ikke er begrenset av delingsvalget.
/// </summary>
public sealed class RapportModel : PageModel
{
    private readonly TestService _testService;
    private readonly AppDbContext _db;

    public RapportModel(TestService testService, AppDbContext db)
    {
        _testService = testService;
        _db = db;
    }

    public sealed record SvarRad(string Sporsmal, string SvarLabel, string? BehandlerKommentar = null);
    public sealed record SideMedSvar(TestSide Side, IReadOnlyList<SvarRad> Svar);

    public Pasient? Pasient { get; private set; }
    public Test? Test { get; private set; }
    public TestTildeling? Tildeling { get; private set; }
    public TestSkaaring? Skaaring { get; private set; }
    public List<SideMedSvar> Sider { get; private set; } = new();
    public bool IkkeGodkjentEnna { get; private set; }

    public int TotalAntallArk => 1 + Sider.Count;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null)
        {
            return NotFound();
        }

        Tildeling = innhold.Tildeling;
        Test = innhold.Test;
        Pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == Tildeling.PasientId, cancellationToken);

        if (Tildeling.RapportGodkjentUtc is null)
        {
            IkkeGodkjentEnna = true;
            return Page();
        }

        Skaaring = await _testService.BeregnSkaaringAsync(id, cancellationToken);

        var kommentarPerLeddId = await _db.TestSvar
            .Where(s => s.TestTildelingId == id && s.BehandlerKommentar != null)
            .ToDictionaryAsync(s => s.TestLeddId, s => s.BehandlerKommentar!, cancellationToken);

        Sider = innhold.Sider.Select(side =>
        {
            var svar = innhold.AlleLedd.Where(l => l.TestSideId == side.Id).Select(ledd =>
            {
                var raaVerdi = innhold.EksisterendeSvar.GetValueOrDefault(ledd.Id, "-");
                var label = ledd.Svartype switch
                {
                    TestSvartype.LikertSkala => TestLeddSvaralternativer.Parse(ledd.Svaralternativer)
                        .FirstOrDefault(p => p.Verdi.ToString() == raaVerdi)?.Tekst ?? raaVerdi,
                    TestSvartype.VisuellAnalogSkala => $"{raaVerdi}/100",
                    _ => raaVerdi
                };
                return new SvarRad(ledd.Sporsmalstekst, label, kommentarPerLeddId.GetValueOrDefault(ledd.Id));
            }).ToList();
            return new SideMedSvar(side, svar);
        }).ToList();

        return Page();
    }
}
