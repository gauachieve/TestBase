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

    /// <summary>Én cutoff-linje til å tegne over resultat-fremdriftsbaren — Posisjon er allerede
    /// omregnet til 0-100% av baren (råskår-cutoffs skaleres mot RaaSkaarMaks, se OnGetAsync).</summary>
    public sealed record CutoffMarkering(string Navn, decimal PosisjonProsent);

    public Pasient? Pasient { get; private set; }
    public Test? Test { get; private set; }
    public TestTildeling? Tildeling { get; private set; }
    public TestSkaaring? Skaaring { get; private set; }
    public IReadOnlyList<CutoffMarkering> Cutoffs { get; private set; } = Array.Empty<CutoffMarkering>();

    /// <summary>Radar-graf av de 5 SIPP-118-inspirerte domenene — kun satt for Test.Kode == "sipp118" (se Sipp118RadarBeregner).</summary>
    public Sipp118RadarData? Sipp118Radar { get; private set; }

    /// <summary>Stolpediagram per personlighetsforstyrrelse — kun satt for Test.Kode == "scid5_pf" (se Scid5PfBarBeregner).</summary>
    public Scid5PfBarData? Scid5PfBar { get; private set; }

    /// <summary>To hexagon-radarer (fleksibilitet/rigiditet) — kun satt for Test.Kode == "mpfi_24" (se MpfiRadarBeregner).</summary>
    public MpfiRadarData? MpfiFleksibilitetRadar { get; private set; }
    public MpfiRadarData? MpfiRigiditetRadar { get; private set; }
    public List<SideMedSvar> Sider { get; private set; } = new();
    public bool IkkeGodkjentEnna { get; private set; }

    /// <summary>Se Behandlerportal-motstykket sin XML-doc — samme "-"-sentinel-telling.</summary>
    public int AntallLedd { get; private set; }
    public int AntallUbesvart { get; private set; }

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

        if (Skaaring is not null && !Skaaring.SkjulProsent && Skaaring.RaaSkaarMaks > 0)
        {
            var visSomProsent = _testService.VisSomProsentIHistogram(Test.Kode);
            Cutoffs = _testService.HentHistogramgrenser(Test.Kode)
                .Select(g => new CutoffMarkering(g.Navn, visSomProsent ? g.Verdi : g.Verdi * 100m / Skaaring.RaaSkaarMaks))
                .ToList();
        }

        if (Test.Kode == "sipp118" && Skaaring is not null)
        {
            Sipp118Radar = Sipp118RadarBeregner.Beregn(Skaaring.Indikatorer);
        }
        else if (Test.Kode == "scid5_pf")
        {
            Scid5PfBar = Scid5PfBarBeregner.Beregn(innhold.AlleLedd, innhold.EksisterendeSvar);
        }
        else if (Test.Kode == "mpfi_24" && Skaaring is not null)
        {
            MpfiFleksibilitetRadar = MpfiRadarBeregner.Beregn(Skaaring.Indikatorer, "Fleksibilitet — ");
            MpfiRigiditetRadar = MpfiRadarBeregner.Beregn(Skaaring.Indikatorer, "Rigiditet — ");
        }

        var kommentarPerLeddId = await _db.TestSvar
            .Where(s => s.TestTildelingId == id && s.BehandlerKommentar != null)
            .ToDictionaryAsync(s => s.TestLeddId, s => s.BehandlerKommentar!, cancellationToken);

        Sider = innhold.Sider.Select(side =>
        {
            // Bilde-ledd (hjemmeoppgaver) er rent visningsinnhold, ikke et besvart spørsmål — se
            // Behandlerportal-motstykket for samme filter.
            var svar = innhold.AlleLedd.Where(l => l.TestSideId == side.Id && l.Svartype != TestSvartype.Bilde).Select(ledd =>
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

        AntallLedd = innhold.AlleLedd.Count(l => l.Svartype != TestSvartype.Bilde);
        AntallUbesvart = innhold.AlleLedd.Count(l => l.Svartype != TestSvartype.Bilde && !innhold.EksisterendeSvar.ContainsKey(l.Id));

        return Page();
    }
}
