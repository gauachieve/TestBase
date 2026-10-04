namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// SDQ-20-skåring: ren listesum (20-100), trygt uten ledd-informasjon (ingen
/// delskalaer). Cutoff ≥30 er Nijenhuis' egen, mye siterte grenseverdi for
/// sannsynlig dissosiativ lidelse (Nijenhuis et al. 1996/1998).
/// </summary>
public sealed class Sdq20Skaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 100;
    private const int Cutoff = 30;

    public string TestKode => "sdq20";

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = new[]
    {
        new TestSkaaringGrenseverdi("Grenseverdi", Cutoff)
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = svar.Sum(s => int.Parse(s.SvarVerdi));
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);
        var overGrense = raaSkaar >= Cutoff;

        var fortolkning =
            $"Sumskår {raaSkaar}/{Maks} — " +
            (overGrense
                ? $"OVER grenseverdien ({Cutoff}), som indikerer sannsynlig somatoform dissosiasjon og bør følges opp videre."
                : $"under grenseverdien ({Cutoff}).");

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Somatoform dissosiasjon", overGrense ? "Over grenseverdi" : "Under grenseverdi", !overGrense)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
