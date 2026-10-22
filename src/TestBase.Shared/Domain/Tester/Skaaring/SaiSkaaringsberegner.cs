namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Skåring for SAI (jf. SaiTestSeeder): råskår = sum av de seks 0–5-leddene (0–30). Falkenström
/// m.fl. (2015) publiserer INGEN kliniske grenseverdier/normer for individuell tolkning av denne
/// skalaen (den ble utviklet og validert som et forskningsinstrument for gruppenivå-analyser,
/// ikke for individuell cutoff-basert screening) — derfor rapporteres KUN råskår/prosentskår uten
/// noen oppdiktet grenseverdi, i motsetning til SWLS som faktisk har publiserte beskrivende bånd.
/// Se docs/beslutningslogg.md "Fjerning av diagnostiske tester / MDR-opprydding".
/// </summary>
public sealed class SaiSkaaringsberegner : ITestSkaaringsberegner
{
    private const int RaaSkaarMaks = 30;

    public string TestKode => "sai";

    public bool VisSomProsentIHistogram => true;

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = Array.Empty<TestSkaaringGrenseverdi>();

    public IReadOnlyList<TestSkaaringReferanselinje> Referanselinjer { get; } = Array.Empty<TestSkaaringReferanselinje>();

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = svar.Select(s => int.Parse(s.SvarVerdi)).Sum();
        var prosentSkaar = (int)Math.Round(raaSkaar / (double)RaaSkaarMaks * 100, MidpointRounding.AwayFromZero);

        var fortolkning = $"Opplevd allianse i denne timen: råskår {raaSkaar} av {RaaSkaarMaks} ({prosentSkaar} %). " +
            "Instrumentet har ingen publiserte kliniske grenseverdier — skåren er ment å følges over tid " +
            "(sammen med SWLS), ikke tolkes alene mot en fast terskel.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Allianse denne timen", $"{prosentSkaar} %", true)
        };

        return new TestSkaaring(raaSkaar, RaaSkaarMaks, prosentSkaar, fortolkning, indikatorer);
    }
}
