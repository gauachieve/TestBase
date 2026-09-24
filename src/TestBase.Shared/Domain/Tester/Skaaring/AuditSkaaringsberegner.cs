namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// AUDIT-skåring: enkel sum over alle 10 ledd (0-40) — TRYGT som en ren
/// listesum (ikke posisjonsbasert utvelgelse/eksludering av bestemte ledd,
/// se docs/beslutningslogg.md "Reell 500-feil i GADIT-skåring"), siden et
/// hoppet-over ledd bare uteblir fra summen, akkurat som forventet.
/// Cutoffs (WHO, Babor et al. 2001): 0-7 lav risiko, 8-15 risikofylt bruk,
/// 16-19 skadelig bruk, 20-40 sannsynlig avhengighet.
/// </summary>
public sealed class AuditSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 40;

    public string TestKode => "audit";

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = new[]
    {
        new TestSkaaringGrenseverdi("Risikofylt", 8),
        new TestSkaaringGrenseverdi("Skadelig", 16),
        new TestSkaaringGrenseverdi("Sannsynlig avhengighet", 20)
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = svar.Sum(s => int.Parse(s.SvarVerdi));
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var (kategori, positiv) = raaSkaar switch
        {
            <= 7 => ("lav risiko", true),
            <= 15 => ("risikofylt bruk", false),
            <= 19 => ("skadelig bruk", false),
            _ => ("sannsynlig avhengighet", false)
        };

        var fortolkning =
            $"Sumskår {raaSkaar}/{Maks} — {kategori} (WHO-cutoffs: 0-7 lav risiko, 8-15 risikofylt, 16-19 " +
            "skadelig, 20-40 sannsynlig avhengighet). AUDIT er et screeningverktøy, ikke tilstrekkelig alene " +
            "for å stille diagnose.";

        var indikatorer = new List<TestSkaaringIndikator> { new("Risikonivå", kategori, positiv) };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
