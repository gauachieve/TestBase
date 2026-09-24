namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Skåring for den forenklede SIPP-118-inspirerte testen (se Sipp118TestSeeder
/// for VIKTIG forbehold om at dette IKKE er det validerte 118-ledds/16-fasetts
/// instrumentet). 5 domener à 6 ledd, skala 1-4. Negativt formulerte
/// ("maladaptive") ledd REVERSE-SKÅRES (5 − rå verdi) slik at HØYERE skår
/// alltid betyr BEDRE personlighetsfunksjon i alle domener, konsistent med
/// SIPP-118s egen tolkningsretning. Krever ekte TestLeddId-oppslag
/// (ITestSkaaringsberegnerMedLedd) for domenegruppering/reverse-skåring
/// uavhengig av hoppet-over ledd, se docs/beslutningslogg.md "Reell 500-feil
/// i GADIT-skåring". Cutoff (snitt ≤2,5, under skalamidtpunktet) er en EGEN,
/// ikke-validert tommelfingerregel for denne forenklede testen — IKKE en
/// offisiell SIPP-118-norm.
/// </summary>
public sealed class Sipp118Skaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int AntallPerDomene = 6;
    private const int AntallDomener = 5;
    private const int AntallTotalt = AntallPerDomene * AntallDomener;
    private const int Maks = AntallTotalt * 4;
    private const decimal LavFunksjonCutoff = 2.5m;

    private static readonly string[] DomeneNavn =
    {
        "Selvkontroll", "Identitetsintegrasjon", "Relasjonell kapasitet", "Ansvarlighet", "Sosial harmoni"
    };

    private static readonly HashSet<int>[] ReverserteposisjonerPerDomene =
    {
        new() { 0, 2, 3, 4 }, // Selvkontroll
        new() { 1 },          // Identitetsintegrasjon
        new() { 1 },          // Relasjonell kapasitet
        new() { 3 },          // Ansvarlighet
        new() { 3, 5 }        // Sosial harmoni
    };

    public string TestKode => "sipp118";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("Denne testen krever ledd-informasjon for domenegruppering/reverse-skåring — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);
        int RaVerdi(int indeks) =>
            indeks < alleLedd.Count && svarPerLeddId.TryGetValue(alleLedd[indeks].Id, out var v) && int.TryParse(v, out var tall) ? tall : 0;

        var domeneSummer = new int[AntallDomener];
        for (var d = 0; d < AntallDomener; d++)
        {
            var reverserte = ReverserteposisjonerPerDomene[d];
            var sum = 0;
            for (var i = 0; i < AntallPerDomene; i++)
            {
                var globalIndeks = d * AntallPerDomene + i;
                var raVerdi = RaVerdi(globalIndeks);
                sum += reverserte.Contains(i) && raVerdi > 0 ? 5 - raVerdi : raVerdi;
            }
            domeneSummer[d] = sum;
        }

        var raaSkaar = domeneSummer.Sum();
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);
        var snitt = raaSkaar / (decimal)AntallTotalt;
        var lavFunksjon = snitt <= LavFunksjonCutoff;

        var domeneTekst = string.Join(", ", DomeneNavn.Select((navn, i) => $"{navn} {domeneSummer[i]}/{AntallPerDomene * 4}"));

        var fortolkning =
            $"Gjennomsnittsskår {snitt:0.00} (av skala 1-4, høyere = bedre personlighetsfunksjon) — " +
            (lavFunksjon ? "under den forenklede grensen 2,5, som kan indikere personlighetsproblematikk." : "over grensen 2,5.") +
            $" Domener: {domeneTekst}. Dette er en KRAFTIG FORENKLET, ikke-validert tilpasning inspirert av " +
            "SIPP-118 sine 5 overordnede domener — IKKE det validerte 118-ledds instrumentet, og ikke " +
            "tilstrekkelig alene for å stille diagnose.";

        var indikatorer = DomeneNavn.Select((navn, i) => new TestSkaaringIndikator(
            navn, $"{domeneSummer[i]}/{AntallPerDomene * 4}", domeneSummer[i] > AntallPerDomene * 4 / 2)).ToList();
        indikatorer.Insert(0, new TestSkaaringIndikator("Samlet personlighetsfunksjon", lavFunksjon ? "Mulig lav funksjon" : "Innenfor forventet område", !lavFunksjon));

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
