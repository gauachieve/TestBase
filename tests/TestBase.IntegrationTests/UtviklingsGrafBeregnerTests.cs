using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.Skaaring;

namespace TestBase.IntegrationTests;

/// <summary>
/// Ren enhetstest (ingen DB/HTTP) av geometriberegningen bak "utvikling over
/// tid"-grafen — se UtviklingsGrafBeregner. Introdusert sammen med WHO-5 VAS
/// (2026-09-14), men beregneren selv er generisk og skal fungere for enhver
/// test med registrert skåring, jf. ønsket om gjenbruk.
/// </summary>
public sealed class UtviklingsGrafBeregnerTests
{
    private static SkaaringHistorikkPunkt Punkt(DateTimeOffset dato, int prosentSkaar) =>
        new(new TestTildeling { FullfortUtc = dato }, new TestSkaaring(prosentSkaar, 100, prosentSkaar, "Fortolkning"));

    [Fact]
    public void ForstePunkt_LiggerHeltTilVenstre_SistePunkt_LiggerHeltTilHoyre()
    {
        var historikk = new[]
        {
            Punkt(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), 40),
            Punkt(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), 60),
            Punkt(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), 80)
        };

        var data = UtviklingsGrafBeregner.Beregn(historikk, Array.Empty<TestSkaaringReferanselinje>());

        Assert.Equal(3, data.Punkter.Count);
        Assert.Equal(data.PlotVenstre, data.Punkter[0].X, precision: 3);
        Assert.Equal(data.PlotHoyre, data.Punkter[^1].X, precision: 3);
    }

    [Fact]
    public void HoyereProsentskaar_GirLavereYKoordinat_SidenSvgYVokserNedover()
    {
        var historikk = new[]
        {
            Punkt(DateTimeOffset.UtcNow.AddDays(-1), 20),
            Punkt(DateTimeOffset.UtcNow, 90)
        };

        var data = UtviklingsGrafBeregner.Beregn(historikk, Array.Empty<TestSkaaringReferanselinje>());

        Assert.True(data.Punkter[1].Y < data.Punkter[0].Y, "En høyere prosentskår skal tegnes HØYERE OPPE (lavere Y) i SVG-koordinater.");
    }

    [Fact]
    public void ReferanselinjerFaarSammeYSkalaSomDatapunktene()
    {
        var historikk = new[] { Punkt(DateTimeOffset.UtcNow.AddDays(-1), 50), Punkt(DateTimeOffset.UtcNow, 50) };
        var referanselinjer = new[] { new TestSkaaringReferanselinje("Velvære", 50) };

        var data = UtviklingsGrafBeregner.Beregn(historikk, referanselinjer);

        // Et datapunkt PÅ 50 og en referanselinje PÅ 50 skal havne på nøyaktig samme Y.
        Assert.Equal(data.Punkter[0].Y, data.Referanselinjer[0].Y, precision: 3);
    }

    [Fact]
    public void ManyMaalepunkter_TynnerUtDatoEtikettene_MenBeholderAlleDataPunkter()
    {
        var historikk = Enumerable.Range(0, 20)
            .Select(i => Punkt(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i), 50))
            .ToArray();

        var data = UtviklingsGrafBeregner.Beregn(historikk, Array.Empty<TestSkaaringReferanselinje>());

        Assert.Equal(20, data.Punkter.Count); // alle 20 punkter tegnes fortsatt
        var antallLabels = data.Punkter.Count(p => p.VisDatoLabel);
        Assert.True(antallLabels < 20, "Med 20 målepunkter skal ikke alle datoene vises som etikett.");
        Assert.True(data.Punkter[0].VisDatoLabel, "Første dato skal alltid vises.");
        Assert.True(data.Punkter[^1].VisDatoLabel, "Siste dato skal alltid vises.");
    }

    [Fact]
    public void FaaMaalepunkter_ViserAlleDatoEtikettene()
    {
        var historikk = new[]
        {
            Punkt(DateTimeOffset.UtcNow.AddDays(-2), 50),
            Punkt(DateTimeOffset.UtcNow.AddDays(-1), 60),
            Punkt(DateTimeOffset.UtcNow, 70)
        };

        var data = UtviklingsGrafBeregner.Beregn(historikk, Array.Empty<TestSkaaringReferanselinje>());

        Assert.All(data.Punkter, p => Assert.True(p.VisDatoLabel));
    }
}
