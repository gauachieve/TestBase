using TestBase.Shared.Domain.Pasienter;

namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Valgfri utvidelse for tester med KJØNNSSPESIFIKK normering (per nå kun
/// MPFI-24 — se Mpfi24Skaaringsberegner og Test.KreverBiologiskKjonn). Det
/// NORMALE innslagspunktet er Test.KreverBiologiskKjonn, håndhevet i
/// TestTildelingsService.TildelOgVarsleAsync — en pasient uten registrert
/// kjønn skal ALDRI få en slik test tildelt i utgangspunktet. Dette
/// grensesnittet er likevel et andre forsvarslag (f.eks. en tildeling som
/// fantes FØR kjønnet ble nullstilt/fjernet igjen): <paramref name="biologiskKjonn"/>
/// kan være null her, og implementasjonen MÅ degradere nådig (en tydelig
/// GyldighetsAdvarsel i returnert TestSkaaring) — ALDRI kaste en exception,
/// som ville gitt en rå 500-side (samme prinsipp som EDE-Q/GADIT-fiksene,
/// se docs/beslutningslogg.md).
///
/// TestService.BeregnSkaaringAsync/HentSkaaringHistorikkAsync sjekker dette
/// grensesnittet FØR ITestSkaaringsberegnerMedLedd (som dette arver fra) —
/// se der for hvordan Pasient slås opp.
/// </summary>
public interface ITestSkaaringsberegnerMedBiologiskKjonn : ITestSkaaringsberegnerMedLedd
{
    TestSkaaring BeregnSkaaringMedBiologiskKjonn(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd, BiologiskKjonn? biologiskKjonn);
}
