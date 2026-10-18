using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.Skaaring;

namespace TestBase.IntegrationTests;

/// <summary>
/// Ren enhetstest (ingen DB/HTTP) av den kombinerte KOR/FIT-grafens geometri/klassifisering —
/// se KorFitGrafBeregner. Speiler mønsteret fra UtviklingsGrafBeregnerTests.cs.
/// </summary>
public sealed class KorFitGrafBeregnerTests
{
    private static SkaaringHistorikkPunkt Punkt(DateTimeOffset dato, int raaSkaar) =>
        new(new TestTildeling { FullfortUtc = dato }, new TestSkaaring(raaSkaar, 40, raaSkaar * 100 / 40, "Fortolkning"));

    [Fact]
    public void OrsOgSrsPunkter_SlaasSammen_OrdinaltOverDenSamledeTidslinjen()
    {
        var ors = new[] { Punkt(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero), 30) };
        var srs = new[] { Punkt(new DateTimeOffset(2026, 1, 1, 10, 15, 0, TimeSpan.Zero), 38) };

        var data = KorFitGrafBeregner.Beregn(ors, srs);

        Assert.Single(data.OrsPunkter);
        Assert.Single(data.SrsPunkter);
        // ORS besvares tidligere samme dag — skal ligge til VENSTRE for SRS-punktet.
        Assert.True(data.OrsPunkter[0].X < data.SrsPunkter[0].X);
    }

    [Fact]
    public void PunktMellomCutoffene_ErSamtaleTid()
    {
        // Mellom ORS-cutoff (25) og SRS-cutoff (36), strengt.
        var ors = new[] { Punkt(DateTimeOffset.UtcNow, 30) };
        var srs = Array.Empty<SkaaringHistorikkPunkt>();

        var data = KorFitGrafBeregner.Beregn(ors, srs);

        Assert.True(data.OrsPunkter[0].ErSamtaleTid);
    }

    [Fact]
    public void PunktNoyaktigPaaCutoff_ErIkkeSamtaleTid()
    {
        // Strengt større/mindre-sammenligning — et punkt NØYAKTIG på en av cutoffene teller ikke.
        var paaOrsCutoff = new[] { Punkt(DateTimeOffset.UtcNow, KorFitGrafBeregner.OrsCutoffRaaskaar) };
        var paaSrsCutoff = new[] { Punkt(DateTimeOffset.UtcNow, KorFitGrafBeregner.SrsCutoffRaaskaar) };

        Assert.False(KorFitGrafBeregner.Beregn(paaOrsCutoff, Array.Empty<SkaaringHistorikkPunkt>()).OrsPunkter[0].ErSamtaleTid);
        Assert.False(KorFitGrafBeregner.Beregn(paaSrsCutoff, Array.Empty<SkaaringHistorikkPunkt>()).OrsPunkter[0].ErSamtaleTid);
    }

    [Fact]
    public void PunktUnderOrsCutoff_ErIkkeSamtaleTid()
    {
        var ors = new[] { Punkt(DateTimeOffset.UtcNow, 10) };
        var data = KorFitGrafBeregner.Beregn(ors, Array.Empty<SkaaringHistorikkPunkt>());

        Assert.False(data.OrsPunkter[0].ErSamtaleTid);
    }

    [Fact]
    public void PunktOverSrsCutoff_ErIkkeSamtaleTid()
    {
        var srs = new[] { Punkt(DateTimeOffset.UtcNow, 40) };
        var data = KorFitGrafBeregner.Beregn(Array.Empty<SkaaringHistorikkPunkt>(), srs);

        Assert.False(data.SrsPunkter[0].ErSamtaleTid);
    }

    [Fact]
    public void CutoffYKoordinater_FolgerSammeSkalaSomPunktene()
    {
        var ors = new[] { Punkt(DateTimeOffset.UtcNow, KorFitGrafBeregner.OrsCutoffRaaskaar) };
        var data = KorFitGrafBeregner.Beregn(ors, Array.Empty<SkaaringHistorikkPunkt>());

        Assert.Equal(data.OrsCutoffY, data.OrsPunkter[0].Y, precision: 3);
    }
}
