using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester.Skaaring;

namespace TestBase.Web.Areas.Admin.Pages.Grupper;

/// <summary>
/// Én søyle i spredningshistogrammet, allerede omregnet til SVG-koordinater
/// — se AggregertModel.BeregnHistogram. Byttet fra et spredningsplott OVER TID
/// (2026-09-23, brukerfeedback: "spredning" skal vise fordelingen AV VERDIER,
/// ikke prosentskår kronologisk) til et histogram over 10 (eller færre, se
/// BeregnHistogram) intervaller — i RÅSKÅR for de fleste tester, prosent kun
/// for de få som selv rapporterer slik, se TestAggregatDetaljer.VisSomProsent.
/// </summary>
public sealed record HistogramSoyle(double X, double Y, double Bredde, double Hoyde, string Etikett, int Antall);

/// <summary>Én cutoff-linje i histogrammet, med X/Y1/Y2 allerede omregnet til SVG-koordinater — se AggregertModel.BeregnHistogram.</summary>
public sealed record HistogramGrenselinje(string Navn, int Verdi, double X, double Y1, double Y2);

/// <summary>
/// Admin sin variant av Behandlerportal/Grupper/Aggregert — samme
/// funksjonalitet, uten eierskapssjekk, se GruppeService.HentAggregatAsync.
/// Utvidet med "enkelttest"-visningsmodus, se Behandlerportal-variantens
/// klassekommentar for full begrunnelse (identisk her).
/// </summary>
public sealed class AggregertModel : PageModel
{
    private readonly GruppeService _grupper;

    public AggregertModel(GruppeService grupper)
    {
        _grupper = grupper;
    }

    public long Id { get; private set; }
    public string? GruppeNavn { get; private set; }
    public bool Provedata { get; private set; }
    public DateOnly Fra { get; private set; }
    public DateOnly Til { get; private set; }
    public IReadOnlyList<TestAggregatRad> Rader { get; private set; } = Array.Empty<TestAggregatRad>();
    public string? Feilmelding { get; private set; }

    public long? TestId { get; private set; }
    public EnkelttestAggregat? Enkelttest { get; private set; }
    public IReadOnlyList<HistogramSoyle> Plott { get; private set; } = Array.Empty<HistogramSoyle>();
    public IReadOnlyList<HistogramGrenselinje> Grenselinjer { get; private set; } = Array.Empty<HistogramGrenselinje>();

    public async Task<IActionResult> OnGetAsync(
        long id, bool provedata = true, DateOnly? fra = null, DateOnly? til = null, long? testId = null, CancellationToken cancellationToken = default)
    {
        var innhold = await _grupper.HentMedTesterAsync(id, cancellationToken);
        if (innhold is null)
        {
            return RedirectToPage("Index");
        }

        Id = id;
        GruppeNavn = innhold.Gruppe.Navn;
        Provedata = provedata;
        Til = til ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (innhold.Tester.Count == 0)
        {
            Feilmelding = "Ingen tester er tilordnet denne gruppen ennå.";
            return Page();
        }

        if (testId is not null && innhold.Tester.Any(t => t.Id == testId))
        {
            TestId = testId;
            if (fra is null)
            {
                var tidligste = await _grupper.HentTidligsteTildeltDatoPerTestAsync(id, cancellationToken);
                var funnet = tidligste.GetValueOrDefault(testId.Value);
                fra = (provedata ? funnet.Provedata : funnet.Ekte) ?? Til.AddDays(-30);
            }
            Fra = fra.Value;

            Enkelttest = await _grupper.HentEnkelttestAsync(id, testId.Value, provedata, Fra, Til, cancellationToken);
            if (Enkelttest is not null)
            {
                var (soyler, grenselinjer) = BeregnHistogram(
                    Enkelttest.Detaljer.Datapunkter, Enkelttest.Detaljer.VisSomProsent,
                    Enkelttest.Detaljer.SkalaMaks, Enkelttest.Detaljer.Grenseverdier);
                Plott = soyler;
                Grenselinjer = grenselinjer;
            }
            return Page();
        }

        Fra = fra ?? Til.AddDays(-30);
        var fraUtc = provedata ? null : (DateTimeOffset?)new DateTimeOffset(Fra.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var tilUtc = provedata ? null : (DateTimeOffset?)new DateTimeOffset(Til.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        Rader = await _grupper.HentAggregatAsync(id, provedata, fraUtc, tilUtc, cancellationToken);
        return Page();
    }

    /// <summary>
    /// Bøtter alle skår (råskår for de fleste tester, prosent kun for de få som
    /// selv rapporterer slik — se <paramref name="visSomProsent"/>) i inntil 10
    /// like brede intervaller over [0, <paramref name="skalaMaks"/>]
    /// (bøttebredde = ⌈maks/10⌉, så en liten skala som GADIT sin 0-8 IKKE tvinges
    /// til 10 kunstig smale bøtter — se docs/beslutningslogg.md), og regner
    /// søylehøydene om til SVG-koordinater for et 560×220-plott, skalert mot den
    /// STØRSTE bøtta (ikke mot et fast tall), siden antallet deltakere varierer
    /// helt fritt fra gruppe til gruppe. Tegner i tillegg en vertikal, presist
    /// plassert linje for hver kjent cutoff (<paramref name="grenseverdier"/>) —
    /// IKKE bøtte-indeksbasert, slik at en cutoff midt i en bøtte vises der den
    /// faktisk er, ikke hoppet til nærmeste bøttekant.
    /// </summary>
    private static (IReadOnlyList<HistogramSoyle> Soyler, IReadOnlyList<HistogramGrenselinje> Grenselinjer) BeregnHistogram(
        IReadOnlyList<ProsentDatapunkt> datapunkter, bool visSomProsent, int skalaMaks,
        IReadOnlyList<TestSkaaringGrenseverdi> grenseverdier)
    {
        if (skalaMaks <= 0)
        {
            return (Array.Empty<HistogramSoyle>(), Array.Empty<HistogramGrenselinje>());
        }

        var bucketBredde = Math.Max(1, (int)Math.Ceiling(skalaMaks / 10.0));
        var antallBoetter = (int)Math.Ceiling(skalaMaks / (double)bucketBredde);

        var boetter = new int[antallBoetter];
        foreach (var dp in datapunkter)
        {
            var verdi = visSomProsent ? dp.ProsentSkaar : dp.RaaSkaar;
            boetter[Math.Clamp(verdi / bucketBredde, 0, antallBoetter - 1)]++;
        }

        var maksAntall = boetter.Max();
        if (maksAntall == 0)
        {
            return (Array.Empty<HistogramSoyle>(), Array.Empty<HistogramGrenselinje>());
        }

        const double venstreMarg = 10, hoyreMarg = 10, toppMarg = 20, bunnMarg = 24, bredde = 560, hoyde = 220, mellomrom = 4;
        var plottBredde = bredde - venstreMarg - hoyreMarg;
        var plottHoyde = hoyde - toppMarg - bunnMarg;
        var soyleBredde = (plottBredde - mellomrom * (antallBoetter - 1)) / antallBoetter;

        var soyler = new List<HistogramSoyle>();
        for (var i = 0; i < antallBoetter; i++)
        {
            var antall = boetter[i];
            var soyleHoyde = plottHoyde * antall / (double)maksAntall;
            var x = venstreMarg + i * (soyleBredde + mellomrom);
            var y = toppMarg + (plottHoyde - soyleHoyde);
            var nedreGrense = i * bucketBredde;
            var ovreGrense = i == antallBoetter - 1 ? skalaMaks : nedreGrense + bucketBredde - 1;
            var etikett = nedreGrense == ovreGrense ? $"{nedreGrense}" : $"{nedreGrense}-{ovreGrense}";
            soyler.Add(new HistogramSoyle(x, y, soyleBredde, soyleHoyde, etikett, antall));
        }

        var grenselinjer = grenseverdier
            .Where(g => g.Verdi >= 0 && g.Verdi <= skalaMaks)
            .Select(g => new HistogramGrenselinje(g.Navn, g.Verdi, venstreMarg + g.Verdi / (double)skalaMaks * plottBredde, toppMarg, toppMarg + plottHoyde))
            .ToList();

        return (soyler, grenselinjer);
    }
}
