namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// En behandlers "like" (tommel opp) på en ANNEN behandlers delte hjemmeoppgave
/// (Test.ErDeltMedAlle/ErDeltMedPartner) — se docs/beslutningslogg.md
/// "Hjemmeoppgaver og programmer". En liking er bevisst IKKE det samme som en
/// kopi: den likte testen dukker opp i behandlerens "Personlig"-fane som en
/// REFERANSE til originalen (fortsatt eid/redigerbar KUN av opprinnelig
/// forfatter) — først når behandleren trykker "Rediger" på den likte, ikke-eide
/// testen opprettes en ekte, selvstendig kopi (Test.KopiertFraTestId satt,
/// navngitt "Kopi av {originalnavn}"), se TestService sin kopieringsmetode.
/// Fjernes IKKE automatisk om originalen senere avpubliseres/slettes — samme
/// "historisk faktum består" prinsipp som ellers i prosjektet; visningslaget
/// filtrerer bort likede rader der TestId ikke lenger finnes/er aktiv.
/// </summary>
public sealed class HjemmeoppgaveLiking
{
    public long Id { get; set; }
    public long BehandlerId { get; set; }
    public long TestId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
}
