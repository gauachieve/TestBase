namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// CORE-OM-skåring: 4 domener (Velvære 4 ledd, Problemer/symptomer 12 ledd,
/// Livsfunksjon 12 ledd, Risiko 6 ledd), 10 positivt formulerte ledd
/// REVERSE-SKÅRES (4 − rå verdi) før summering — offisiell CORE-konvensjon
/// (se Core10Skaaringsberegner for samme prinsipp i kortversjonen). Krever
/// ekte TestLeddId-oppslag (ITestSkaaringsberegnerMedLedd) for domene-
/// gruppering OG reverse-skåring, uavhengig av hoppet-over ledd — se
/// docs/beslutningslogg.md "Reell 500-feil i GADIT-skåring". Total
/// gjennomsnittsskår ≥1,0 er en forenklet, mye brukt tilnærming til den
/// kliniske grensen (offisielle CORE-OM-normer skiller noe mellom kjønn —
/// IKKE modellert her, se seeder-kommentar). Risikoledd (29-34) flagges
/// ALLTID separat.
/// </summary>
public sealed class CoreOmSkaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int AntallVelvare = 4;
    private const int AntallProblemer = 12;
    private const int AntallFunksjon = 12;
    private const int AntallRisiko = 6;
    private const int AntallTotalt = AntallVelvare + AntallProblemer + AntallFunksjon + AntallRisiko;
    private const int Maks = AntallTotalt * 4;
    private const decimal KlinuskSnittCutoff = 1.0m;

    private static readonly HashSet<int> ReverserteVelvarePosisjoner = new() { 0, 1, 3 };
    private static readonly HashSet<int> ReverserteFunksjonPosisjoner = new() { 0, 1, 3, 6, 8, 9, 11 };

    public string TestKode => "core_om";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("CORE-OM krever ledd-informasjon for domenegruppering/reverse-skåring — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);
        int RaVerdi(int indeks) =>
            indeks < alleLedd.Count && svarPerLeddId.TryGetValue(alleLedd[indeks].Id, out var v) && int.TryParse(v, out var tall) ? tall : 0;

        int SkaartVerdi(int indeks, bool reversert) => reversert ? 4 - RaVerdi(indeks) : RaVerdi(indeks);

        var velvareSum = 0;
        for (var i = 0; i < AntallVelvare; i++)
        {
            velvareSum += SkaartVerdi(i, ReverserteVelvarePosisjoner.Contains(i));
        }

        var problemerSum = 0;
        for (var i = AntallVelvare; i < AntallVelvare + AntallProblemer; i++)
        {
            problemerSum += SkaartVerdi(i, false);
        }

        var funksjonSum = 0;
        for (var i = 0; i < AntallFunksjon; i++)
        {
            var globalIndeks = AntallVelvare + AntallProblemer + i;
            funksjonSum += SkaartVerdi(globalIndeks, ReverserteFunksjonPosisjoner.Contains(i));
        }

        var risikoStart = AntallVelvare + AntallProblemer + AntallFunksjon;
        var risikoVerdier = new int[AntallRisiko];
        for (var i = 0; i < AntallRisiko; i++)
        {
            risikoVerdier[i] = RaVerdi(risikoStart + i);
        }
        var risikoSum = risikoVerdier.Sum();

        var raaSkaar = velvareSum + problemerSum + funksjonSum + risikoSum;
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);
        var snitt = raaSkaar / (decimal)AntallTotalt;
        var overGrense = snitt >= KlinuskSnittCutoff;

        var fortolkning =
            $"Gjennomsnittsskår {snitt:0.00} (av skala 0-4) — " +
            (overGrense ? "OVER den forenklede kliniske grensen 1,0, som indikerer klinisk signifikant distress." : "under grensen 1,0.") +
            $" Domener: Velvære {velvareSum}/{AntallVelvare * 4}, Problemer/symptomer {problemerSum}/{AntallProblemer * 4}, " +
            $"Livsfunksjon {funksjonSum}/{AntallFunksjon * 4}, Risiko {risikoSum}/{AntallRisiko * 4}. " +
            "CORE-OM er et bredt distressmål, ikke tilstrekkelig alene for å stille diagnose.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Klinisk signifikant distress", overGrense ? "Over grense" : "Under grense", !overGrense)
        };

        // Risiko for seg selv: ledd 29-32 (posisjon 0-3 i risikoblokken). Risiko for andre: ledd 33-34 (posisjon 4-5).
        var risikoForSegSelv = risikoVerdier.Take(4).Sum();
        var risikoForAndre = risikoVerdier.Skip(4).Sum();
        if (risikoForSegSelv > 0)
        {
            indikatorer.Insert(0, new TestSkaaringIndikator("OBS: Risiko for seg selv (ledd 29-32)", $"Sum {risikoForSegSelv}/16 — minst ett ledd besvart over «Ikke i det hele tatt»", false));
        }
        if (risikoForAndre > 0)
        {
            indikatorer.Insert(0, new TestSkaaringIndikator("OBS: Risiko for andre (ledd 33-34)", $"Sum {risikoForAndre}/8 — minst ett ledd besvart over «Ikke i det hele tatt»", false));
        }

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
