namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Skåring for SWLS (jf. SwlsTestSeeder): råskår = sum av de fem 1–7-leddene (5–35), tolket med
/// Dieners egne publiserte, BESKRIVENDE bånd (labs.psychology.illinois.edu/~ediener/Documents/
/// Understanding%20SWLS%20Scores.pdf) — IKKE en klinisk grenseverdi/cutoff for noen tilstand,
/// bevisst formulert uten diagnostisk språk (se docs/beslutningslogg.md "Fjerning av
/// diagnostiske tester / MDR-opprydding" for hvorfor dette skillet er avgjørende).
/// </summary>
public sealed class SwlsSkaaringsberegner : ITestSkaaringsberegner
{
    private const int RaaSkaarMin = 5;
    private const int RaaSkaarMaks = 35;
    private const int NoytraltPunkt = 20;

    public string TestKode => "swls";

    public bool VisSomProsentIHistogram => true;

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = new[]
    {
        new TestSkaaringGrenseverdi("Nøytralt punkt (gjennomsnittlig)", NoytraltPunkt)
    };

    public IReadOnlyList<TestSkaaringReferanselinje> Referanselinjer { get; } = new[]
    {
        new TestSkaaringReferanselinje("Nøytralt punkt", (int)Math.Round(NoytraltPunkt / (double)RaaSkaarMaks * 100))
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = svar.Select(s => int.Parse(s.SvarVerdi)).Sum();
        var prosentSkaar = (int)Math.Round(raaSkaar / (double)RaaSkaarMaks * 100, MidpointRounding.AwayFromZero);

        var baand = raaSkaar switch
        {
            >= 31 => "svært høy livstilfredshet",
            >= 26 => "høy livstilfredshet",
            >= 21 => "litt over gjennomsnittlig livstilfredshet",
            20 => "omtrent gjennomsnittlig livstilfredshet",
            >= 15 => "litt under gjennomsnittlig livstilfredshet",
            >= 10 => "lav livstilfredshet",
            _ => "svært lav livstilfredshet"
        };

        var fortolkning = $"Råskår {raaSkaar} av {RaaSkaarMaks} ({prosentSkaar} %) — beskrives i " +
            $"Dieners publiserte, beskrivende bånd som \"{baand}\". Dette er IKKE en klinisk " +
            "grenseverdi og sier ikke noe om noen diagnose — SWLS måler generell livstilfredshet, ikke symptomer.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Livstilfredshet", baand, raaSkaar >= NoytraltPunkt)
        };

        return new TestSkaaring(raaSkaar, RaaSkaarMaks, prosentSkaar, fortolkning, indikatorer);
    }
}
