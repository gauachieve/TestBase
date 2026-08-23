namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Én utsendt invitasjonslenke til en behandler under opprettelse. Token er
/// engangsbruk og tidsbegrenset — se BehandlerInvitasjonService.
/// </summary>
public sealed class BehandlerInvitasjon
{
    public long Id { get; set; }
    public long BehandlerId { get; set; }
    public required string Token { get; set; }
    public KontaktMetode KontaktMetode { get; set; }
    public required string KontaktVerdi { get; set; }
    public DateTimeOffset UtlopUtc { get; set; }
    public DateTimeOffset? BruktUtc { get; set; }
    public long OpprettetAvAdministratorId { get; set; }
}
