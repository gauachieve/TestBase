namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// PAQ-11R-skåring: fem domenesummer med foreløpige grenseverdier, sitert
/// ordrett fra kildedokumentet (se Paq11RTestSeeder). Ledd 1, 2, 8 og 9
/// reverseres (4 - rå verdi) før de inngår i domenesummene. "Borderline
/// feature"-composite (ledd 2,3,7,9,11,12,16,17, rå verdier, ingen cutoff
/// oppgitt av kilden) tas med som ekstra informativ indikator.
/// </summary>
public sealed class Paq11RSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 68; // 17 ledd × 4 poeng

    public string TestKode => "paq11r";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        int Raa(int leddnummer) => int.Parse(svar[leddnummer - 1].SvarVerdi);
        int Rev(int leddnummer) => 4 - Raa(leddnummer);

        var anankastia = Raa(4) + Raa(6) + Raa(7);
        var tilbaketrekning = Rev(1) + Rev(2) + Rev(8) + Raa(17);
        var disinhibition = Raa(5) + Raa(11);
        var dyssosialitet = Rev(9) + Raa(13) + Raa(14);
        var negativAffektivitet = Raa(3) + Raa(10) + Raa(12) + Raa(15) + Raa(16);
        var borderlineTrekk = Raa(2) + Raa(3) + Raa(7) + Raa(9) + Raa(11) + Raa(12) + Raa(16) + Raa(17);

        var raaSkaar = Enumerable.Range(1, 17).Sum(Raa);
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Anankasme (terskel 7)", $"{anankastia}", anankastia < 7),
            new("Tilbaketrekning (terskel 7)", $"{tilbaketrekning}", tilbaketrekning < 7),
            new("Disinhibition (terskel 4)", $"{disinhibition}", disinhibition < 4),
            new("Dyssosialitet (terskel 6)", $"{dyssosialitet}", dyssosialitet < 6),
            new("Negativ affektivitet (terskel 10)", $"{negativAffektivitet}", negativAffektivitet < 10),
            new("Borderline-trekk (informativ, ingen fastsatt cutoff)", $"{borderlineTrekk}", true)
        };

        var fortolkning = $"Totalskår {raaSkaar}/{Maks}. Domenesummer — Anankasme: {anankastia} (terskel 7), " +
                           $"Tilbaketrekning: {tilbaketrekning} (terskel 7), Disinhibition: {disinhibition} " +
                           $"(terskel 4), Dyssosialitet: {dyssosialitet} (terskel 6), Negativ affektivitet: " +
                           $"{negativAffektivitet} (terskel 10). Terskelverdiene er foreløpige — kildeforfatterne " +
                           "har ikke fastsatt dem endelig. Klinisk vurdering skal alltid ha forrang.";

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
