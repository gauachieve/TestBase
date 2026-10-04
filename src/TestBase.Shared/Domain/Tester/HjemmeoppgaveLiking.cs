namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Hjemmeoppgaver (2026-10-04): en behandler "likte" en annen behandlers delte hjemmeoppgave —
/// REN REFERANSE til originalen (ikke en kopi), som gjør at den dukker opp i behandlerens egen
/// "Personlig"-liste uten å duplisere noe data. En kopi ("Kopi av X") opprettes først i det
/// øyeblikket behandleren faktisk trykker "Rediger" på en likt, ikke-eid rad — se
/// TestService.KopierHjemmeoppgaveAsync. Fjerning av likingen fjerner KUN referansen, aldri
/// originalen eller en eventuell allerede opprettet kopi.
/// </summary>
public sealed class HjemmeoppgaveLiking
{
    public long Id { get; set; }
    public long BehandlerId { get; set; }
    public long TestId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
}
