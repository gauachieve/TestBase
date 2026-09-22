using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Grupper;

/// <summary>Ett punkt i spredningsplottet, allerede omregnet til SVG-koordinater — se AggregertModel.BeregnPlott.</summary>
public sealed record ScatterPunkt(double Cx, double Cy, string Tittel);

/// <summary>
/// Fase 4: aggregert rapport på tvers av gruppens pasienter, per tilordnet
/// test — til konferanse-/presentasjonsbruk (prøvedata) eller oppfølging av
/// ekte pasienter over en tidsperiode. Beregnes på nytt for hver visning
/// (ingen lagret rapport), se GruppeService.HentAggregatAsync.
/// Utvidet med et "enkelttest"-visningsmodus (<paramref name="testId"/> satt) —
/// navngitt rapport for ÉN valgt test + datointervall, med standardavvik og et
/// spredningsplott, trigget fra "Generer Temp/Ekte Gruppe Rapport"-popupen på
/// Grupper/Rediger, se GruppeService.HentEnkelttestAsync og
/// docs/beslutningslogg.md "Gruppe-rapportgenerator".
/// </summary>
public sealed class AggregertModel : PageModel
{
    private readonly GruppeService _grupper;
    private readonly ICurrentUserContext _currentUser;

    public AggregertModel(GruppeService grupper, ICurrentUserContext currentUser)
    {
        _grupper = grupper;
        _currentUser = currentUser;
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
    public IReadOnlyList<ScatterPunkt> Plott { get; private set; } = Array.Empty<ScatterPunkt>();

    private long HentBehandlerId() => long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

    public async Task<IActionResult> OnGetAsync(
        long id, bool provedata = true, DateOnly? fra = null, DateOnly? til = null, long? testId = null, CancellationToken cancellationToken = default)
    {
        var innhold = await _grupper.HentMedTesterAsync(id, cancellationToken);
        if (innhold is null || innhold.Gruppe.BehandlerId != HentBehandlerId())
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
            Plott = BeregnPlott(Enkelttest?.Detaljer.Datapunkter ?? Array.Empty<ProsentDatapunkt>());
            return Page();
        }

        Fra = fra ?? Til.AddDays(-30);
        var fraUtc = provedata ? null : (DateTimeOffset?)new DateTimeOffset(Fra.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var tilUtc = provedata ? null : (DateTimeOffset?)new DateTimeOffset(Til.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        Rader = await _grupper.HentAggregatAsync(id, provedata, fraUtc, tilUtc, cancellationToken);
        return Page();
    }

    /// <summary>
    /// Regner om rå (dato, prosentskår)-datapunkter til SVG-koordinater for et
    /// 560×220-plott (marg 40px venstre for y-akse-tekst, 16px ellers) — X er
    /// kronologisk rekkefølge (jevnt fordelt, ingen egen tidsakse-mening utover
    /// spredning), Y er prosentskår 0–100 (invertert, SVG-y vokser nedover).
    /// </summary>
    private static IReadOnlyList<ScatterPunkt> BeregnPlott(IReadOnlyList<ProsentDatapunkt> datapunkter)
    {
        if (datapunkter.Count == 0)
        {
            return Array.Empty<ScatterPunkt>();
        }

        const double venstreMarg = 40, hoyreMarg = 16, toppMarg = 12, bunnMarg = 12, bredde = 560, hoyde = 220;
        var plottBredde = bredde - venstreMarg - hoyreMarg;
        var plottHoyde = hoyde - toppMarg - bunnMarg;

        var punkter = new List<ScatterPunkt>();
        for (var i = 0; i < datapunkter.Count; i++)
        {
            var dp = datapunkter[i];
            var x = datapunkter.Count == 1
                ? venstreMarg + plottBredde / 2
                : venstreMarg + plottBredde * i / (datapunkter.Count - 1);
            var y = toppMarg + plottHoyde * (1 - dp.ProsentSkaar / 100.0);
            var tittel = $"{dp.FullfortUtc.ToLocalTime():dd.MM.yyyy}: {dp.ProsentSkaar}%";
            punkter.Add(new ScatterPunkt(x, y, tittel));
        }

        return punkter;
    }
}
