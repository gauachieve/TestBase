namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Skåring for ORS (jf. OrsTestSeeder): hvert av de fire leddene besvares med en glidebryter
/// 0–100 (samme generiske VAS-mekanisme som WHO-5 VAS), tolket som 0–10 per ledd (/10, kontinuerlig
/// — IKKE avrundet per ledd, kun den endelige summen avrundes) for å matche den kjente,
/// publiserte 4×10 cm-skalaen (total 0–40) ORS/SRS-litteraturen siterer cutoffs på. Cutoff 25 og
/// "lavt enkeltledd" &lt; 9 er de offentlig siterte terskelverdiene for ORS (se
/// docs/beslutningslogg.md "MDR..."-uavhengig KOR/FIT-innføring, 2026-10-18, for kildene brukt ved
/// research). Råskår rapporteres i STEDET for prosent (VisSomProsentIHistogram = false) — klinikere
/// kjenner cutoffen som "25 av 40", ikke en prosentandel, samme prinsipp som
/// ITestSkaaringsberegner sin klassekommentar beskriver for GADIT/PHQ-9 m.fl.
/// </summary>
public sealed class OrsSkaaringsberegner : ITestSkaaringsberegner
{
    private const int CutoffRaaskaar = 25;
    private const double LavtEnkeltleddRaaskaar = 9.0;
    private const int RaaSkaarMaks = 40;

    public string TestKode => "ors";

    public bool VisSomProsentIHistogram => false;

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = new[]
    {
        new TestSkaaringGrenseverdi("ORS-cutoff", CutoffRaaskaar)
    };

    /// <summary>Prosent-ekvivalenter av råskår-cutoffen (25/40 og "lavt enkeltledd" 9/10), KUN brukt når ORS vises ALENE i den generiske, prosent-baserte "utvikling over tid"-grafen — se UtviklingsGrafBeregner. Den kombinerte KOR/FIT-grafen (KorFitGrafBeregner) bruker råskår direkte, ikke disse.</summary>
    public IReadOnlyList<TestSkaaringReferanselinje> Referanselinjer { get; } = new[]
    {
        new TestSkaaringReferanselinje("ORS-cutoff", (int)Math.Round(CutoffRaaskaar / (double)RaaSkaarMaks * 100))
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
            ? $"Råskår {raaSkaar} av {RaaSkaarMaks}. Under ORS-cutoffen ({CutoffRaaskaar}) — indikerer lavere " +
              "opplevd tilstand/funksjon enn normert referansegruppe, og bør følges opp sammen med alliansemålet (SRS)."
            : $"Råskår {raaSkaar} av {RaaSkaarMaks}. På eller over ORS-cutoffen ({CutoffRaaskaar}) — indikerer " +
              "opplevd tilstand/funksjon innenfor normert referanseområde.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("ORS-cutoff", underCutoff ? "Under cutoff" : "Over cutoff", !underCutoff)
        };
        if (lavtEnkeltledd)
        {
            indikatorer.Add(new TestSkaaringIndikator("Enkeltledd", "Ett eller flere ledd lavt (< 9 av 10)", false));
        }

        return new TestSkaaring(raaSkaar, RaaSkaarMaks, prosentSkaar, fortolkning, indikatorer);
    }
}
