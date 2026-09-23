using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Pasienter;

namespace TestBase.Web.Areas.Admin.Pages.Grupper;

/// <summary>
/// Én søyle i spredningshistogrammet, allerede omregnet til SVG-koordinater
/// — se AggregertModel.BeregnHistogram. Byttet fra et spredningsplott OVER TID
/// (2026-09-23, brukerfeedback: "spredning" skal vise fordelingen AV VERDIER,
/// ikke prosentskår kronologisk) til et histogram over 10 %-intervaller.
/// </summary>
public sealed record HistogramSoyle(double X, double Y, double Bredde, double Hoyde, string Etikett, int Antall);

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
            Plott = BeregnHistogram(Enkelttest?.Detaljer.Datapunkter ?? Array.Empty<ProsentDatapunkt>());
            return Page();
        }

        Fra = fra ?? Til.AddDays(-30);
        var fraUtc = provedata ? null : (DateTimeOffset?)new DateTimeOffset(Fra.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var tilUtc = provedata ? null : (DateTimeOffset?)new DateTimeOffset(Til.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        Rader = await _grupper.HentAggregatAsync(id, provedata, fraUtc, tilUtc, cancellationToken);
        return Page();
    }

    /// <summary>
    /// Bøtter alle prosentskår i 10 %-brede intervaller (0-9, 10-19, …, 90-100 —
    /// siste bøtte er 11 bred slik at en skår på nøyaktig 100 har et hjem) og
    /// regner søylehøydene om til SVG-koordinater for et 560×220-plott, skalert
    /// mot den STØRSTE bøtta (ikke mot et fast tall), siden selve antallet
    /// deltakere varierer helt fritt fra gruppe til gruppe.
    /// </summary>
    private static IReadOnlyList<HistogramSoyle> BeregnHistogram(IReadOnlyList<ProsentDatapunkt> datapunkter)
    {
        const int antallBoetter = 10;
        var boetter = new int[antallBoetter];
        foreach (var dp in datapunkter)
        {
            boetter[Math.Clamp(dp.ProsentSkaar / 10, 0, antallBoetter - 1)]++;
        }

        var maksAntall = boetter.Max();
        if (maksAntall == 0)
        {
            return Array.Empty<HistogramSoyle>();
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
            var nedreGrense = i * 10;
            var etikett = i == antallBoetter - 1 ? $"{nedreGrense}-100" : $"{nedreGrense}-{nedreGrense + 9}";
            soyler.Add(new HistogramSoyle(x, y, soyleBredde, soyleHoyde, etikett, antall));
        }

        return soyler;
    }
}
