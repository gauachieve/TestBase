namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// CORE-A-skåring: BEVISST IKKE et sumskår-verktøy — risikoscreening skal
/// aldri "middelverdi-vurderes bort". Ledd 1-5 (risiko for seg selv) og ledd
/// 6-8 (risiko for andre) rapporteres som EGNE indikatorer, og ETHVERT ledd
/// over 0 flagges individuelt i fortolkningsteksten. RaaSkaar/RaaSkaarMaks
/// finnes kun for konsistens med resten av domenemodellen (histogram/
/// utvikling over tid) — den kliniske konklusjonen skal ALDRI leses av
/// totalskåren alene. Krever ekte TestLeddId-oppslag
/// (ITestSkaaringsberegnerMedLedd) for å skille risiko-for-seg-selv fra
/// risiko-for-andre uavhengig av hoppet-over ledd.
/// </summary>
public sealed class CoreARisikoSkaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int AntallSegSelv = 5;
    private const int AntallAndre = 3;
    private const int AntallTotalt = AntallSegSelv + AntallAndre;
    private const int Maks = AntallTotalt * 4;

    public string TestKode => "core_a";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("CORE-A krever ledd-informasjon for risikokategorisering — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);
        int RaVerdi(int indeks) =>
            indeks < alleLedd.Count && svarPerLeddId.TryGetValue(alleLedd[indeks].Id, out var v) && int.TryParse(v, out var tall) ? tall : 0;

        var segSelvVerdier = Enumerable.Range(0, AntallSegSelv).Select(RaVerdi).ToList();
        var andreVerdier = Enumerable.Range(AntallSegSelv, AntallAndre).Select(RaVerdi).ToList();

        var segSelvSum = segSelvVerdier.Sum();
        var andreSum = andreVerdier.Sum();
        var raaSkaar = segSelvSum + andreSum;
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var noenSegSelv = segSelvVerdier.Any(v => v > 0);
        var noenAndre = andreVerdier.Any(v => v > 0);

        var fortolkning =
            (noenSegSelv || noenAndre
                ? "MINST ETT RISIKOLEDD ER BESVART OVER «IKKE I DET HELE TATT» — krever klinisk oppfølging " +
                  "UAVHENGIG av totalskåren under."
                : "Ingen risikoledd besvart over «Ikke i det hele tatt» i denne besvarelsen.") +
            $" Risiko for seg selv: sum {segSelvSum}/{AntallSegSelv * 4}. Risiko for andre: sum {andreSum}/{AntallAndre * 4}. " +
            "CORE-A er en screening, ikke en fullstendig risikovurdering — klinisk skjønn har alltid forrang.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Risiko for seg selv", noenSegSelv ? "Flagget" : "Ikke flagget", !noenSegSelv),
            new("Risiko for andre", noenAndre ? "Flagget" : "Ikke flagget", !noenAndre)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
