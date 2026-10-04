namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// BSQ-14-skåring: ren listesum (14-84), trygt uten ledd-informasjon siden
/// ALLE ledd inngår i samme, udelte skår (ingen delskalaer/ekskluderte ledd).
/// Cutoffs er en proporsjonal skalering av de mye siterte BSQ-34-grensene
/// (Cooper et al. 1987) ned til 14 ledd — IKKE hentet fra en offisiell
/// BSQ-14-spesifikk kilde ennå, bør kvalitetssikres. Se docs/beslutningslogg.md.
/// </summary>
public sealed class Bsq14Skaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 84;

    public string TestKode => "bsq14";

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = new[]
    {
        new TestSkaaringGrenseverdi("Mild bekymring", 41),
        new TestSkaaringGrenseverdi("Moderat bekymring", 53),
        new TestSkaaringGrenseverdi("Markert bekymring", 68)
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = svar.Sum(s => int.Parse(s.SvarVerdi));
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var (kategori, positiv) = raaSkaar switch
        {
            < 41 => ("ingen bekymring for kroppsform", true),
            < 53 => ("mild bekymring for kroppsform", false),
            < 68 => ("moderat bekymring for kroppsform", false),
            _ => ("markert bekymring for kroppsform", false)
        };

        var fortolkning =
            $"Sumskår {raaSkaar}/{Maks} — {kategori} (grenser: <41 ingen, 41-52 mild, 53-67 moderat, ≥68 markert — " +
            "proporsjonalt skalert fra de mye siterte BSQ-34-cutoffene).";

        var indikatorer = new List<TestSkaaringIndikator> { new("Bekymringsnivå", kategori, positiv) };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
