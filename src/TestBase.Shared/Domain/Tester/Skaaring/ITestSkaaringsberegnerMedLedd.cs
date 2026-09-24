namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Valgfri utvidelse av <see cref="ITestSkaaringsberegner"/> for tester med
/// FLERE delskalaer der ledd må grupperes til riktig delskala UAVHENGIG av om
/// et tidligere ledd ble hoppet over. Samme rotårsak-klasse som knakk GADIT
/// (se docs/beslutningslogg.md "Reell 500-feil i GADIT-skåring") — der løsningen
/// var å klassifisere svar etter EGEN VERDI, noe som bare virker når ulike
/// delskalaer faktisk har ulikt format (tall vs. Ja/Nei). For tester der ALLE
/// ledd deler samme Likert-skala på tvers av delskalaer (typisk for
/// spørreskjemaer med flere subskalaer, f.eks. EDE-Q/SIPP-118/CORE-OM) er
/// value-basert klassifisering umulig — løsningen her er ekte TestLeddId-
/// oppslag mot <see cref="TestLedd.Rekkefolge"/>/TestSideId i stedet for
/// listeposisjon, som ALDRI forskyves av et hoppet-over spørsmål.
///
/// TestService.BeregnSkaaringAsync bruker denne i stedet for
/// <see cref="ITestSkaaringsberegner.BeregnSkaaring"/> når skåringsberegneren
/// implementerer dette grensesnittet — ingen av de eksisterende ~20
/// skåringsberegnerne trenger endring for å fortsette å virke som før.
/// </summary>
public interface ITestSkaaringsberegnerMedLedd : ITestSkaaringsberegner
{
    /// <param name="svar">Samme "fullstendige" svarliste som BeregnSkaaring ville fått (besvarte + normert-imputerte ledd, IKKE nødvendigvis alle).</param>
    /// <param name="alleLedd">ALLE testens ledd (også ubesvarte, uansett imputering), sortert (side.Rekkefolge, ledd.Rekkefolge) — bruk TestLedd.Id til å slå opp riktig delskala for hvert svar i <paramref name="svar"/>.</param>
    TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd);
}
