using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.Skaaring;

namespace TestBase.IntegrationTests;

/// <summary>
/// Ren enhetstest (ingen DB/HTTP) av den kombinerte "tilbakemelding over tid"-grafens geometri —
/// se KorFitGrafBeregner. Speiler mønsteret fra UtviklingsGrafBeregnerTests.cs. Oppdatert
/// 2026-10-22 for SWLS/SAI (prosentbasert Y-akse) i stedet for ORS/SRS (delt råskår-akse), se
/// docs/beslutningslogg.md "Fjerning av diagnostiske tester / MDR-opprydding".
/// </summary>
public sealed class KorFitGrafBeregnerTests
{
    private static SkaaringHistorikkPunkt Punkt(DateTimeOffset dato, int raaSkaar, int raaSkaarMaks) =>
        new(new TestTildeling { FullfortUtc = dato }, new TestSkaaring(raaSkaar, raaSkaarMaks, raaSkaar * 100 / raaSkaarMaks, "Fortolkning"));

    [Fact]
    public void SwlsOgSaiPunkter_SlaasSammen_OrdinaltOverDenSamledeTidslinjen()
    {
        var swls = new[] { Punkt(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero), 25, 35) };
        var sai = new[] { Punkt(new DateTimeOffset(2026, 1, 1, 10, 15, 0, TimeSpan.Zero), 20, 30) };

        var data = KorFitGrafBeregner.Beregn(swls, sai);

        Assert.Single(data.SwlsPunkter);
        Assert.Single(data.SaiPunkter);
        // SWLS besvares tidligere samme dag — skal ligge til VENSTRE for SAI-punktet.
        Assert.True(data.SwlsPunkter[0].X < data.SaiPunkter[0].X);
    }

    [Fact]
    public void Punkter_PosisjoneresEtterProsentAvRespektiveMaksSkaar_IkkeDeltRaaskaarAkse()
    {
        // SWLS 35/35 (100 %) og SAI 15/30 (50 %) — ulik maks-skala, må begge regnes om til prosent
        // for å havne riktig på en DELT Y-akse (se KorFitGrafBeregner sin klassekommentar).
        var swls = new[] { Punkt(DateTimeOffset.UtcNow, 35, 35) };
        var sai = new[] { Punkt(DateTimeOffset.UtcNow, 15, 30) };

        var data = KorFitGrafBeregner.Beregn(swls, sai);

        // SWLS (100 %) skal ligge HØYERE i grafen (lavere Y-pikselverdi, SVG-koordinater vokser nedover) enn SAI (50 %).
        Assert.True(data.SwlsPunkter[0].Y < data.SaiPunkter[0].Y);
        Assert.Equal(100, data.SwlsPunkter[0].ProsentSkaar);
        Assert.Equal(50, data.SaiPunkter[0].ProsentSkaar);
    }

    [Fact]
    public void TomHistorikkForEnSerie_GirIngenPunkterForDenSerien()
    {
        var swls = new[] { Punkt(DateTimeOffset.UtcNow, 20, 35) };

        var data = KorFitGrafBeregner.Beregn(swls, Array.Empty<SkaaringHistorikkPunkt>());

        Assert.Single(data.SwlsPunkter);
        Assert.Empty(data.SaiPunkter);
    }
}
