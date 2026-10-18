namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Skåring for SRS (jf. SrsTestSeeder) — samme 0–100-glidebryter-/10-mekanisme som
/// OrsSkaaringsberegner (se dens klassekommentar for full begrunnelse), men med cutoff 36 og
/// "lavt enkeltledd" &lt; 9 av 10 — de offentlig siterte terskelverdiene for SRS.
/// </summary>
public sealed class SrsSkaaringsberegner : ITestSkaaringsberegner
{
    private const int CutoffRaaskaar = 36;
    private const double LavtEnkeltleddRaaskaar = 9.0;
    private const int RaaSkaarMaks = 40;

    public string TestKode => "srs";

    public bool VisSomProsentIHistogram => false;

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = new[]
    {
        new TestSkaaringGrenseverdi("SRS-cutoff", CutoffRaaskaar)
    };

    /// <summary>Se OrsSkaaringsberegner sin tilsvarende kommentar — kun for enkelttest-grafen, ikke KorFitGrafBeregner.</summary>
    public IReadOnlyList<TestSkaaringReferanselinje> Referanselinjer { get; } = new[]
    {
        new TestSkaaringReferanselinje("SRS-cutoff", (int)Math.Round(CutoffRaaskaar / (double)RaaSkaarMaks * 100))
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var verdierPerLedd = svar.Select(s => int.Parse(s.SvarVerdi) / 10.0).ToList();
        var raaSkaarDesimal = verdierPerLedd.Sum();
        var raaSkaar = (int)Math.Round(raaSkaarDesimal, MidpointRounding.AwayFromZero);
        var prosentSkaar = (int)Math.Round(raaSkaarDesimal / RaaSkaarMaks * 100, MidpointRounding.AwayFromZero);
        var lavtEnkeltledd = verdierPerLedd.Any(v => v < LavtEnkeltleddRaaskaar);
        var underCutoff = raaSkaarDesimal < CutoffRaaskaar;

        var fortolkning = underCutoff
            ? $"Råskår {raaSkaar} av {RaaSkaarMaks}. Under SRS-cutoffen ({CutoffRaaskaar}) — kan indikere en " +
              "svekket allianse i denne timen, og bør vurderes sammen med tilstandsmålet (ORS)."
            : $"Råskår {raaSkaar} av {RaaSkaarMaks}. På eller over SRS-cutoffen ({CutoffRaaskaar}) — indikerer " +
              "en god opplevd allianse i denne timen.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("SRS-cutoff", underCutoff ? "Under cutoff" : "Over cutoff", !underCutoff)
        };
        if (lavtEnkeltledd)
        {
            indikatorer.Add(new TestSkaaringIndikator("Enkeltledd", "Ett eller flere ledd lavt (< 9 av 10)", false));
        }

        return new TestSkaaring(raaSkaar, RaaSkaarMaks, prosentSkaar, fortolkning, indikatorer);
    }
}
