using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// Én utsendt pasient-invitasjon. Token lagres allerede nå slik at Del 4 kan
/// bygge en fullføringsside direkte på denne, men i DENNE slicen sendes det
/// bevisst ingen lenke i mock-meldingen (se PasientInvitasjonService) siden
/// landingssiden ikke finnes ennå.
/// </summary>
public sealed class PasientInvitasjon
{
    public long Id { get; set; }
    public long PasientId { get; set; }
    public required string Token { get; set; }
    public KontaktMetode KontaktMetode { get; set; }
    public required string KontaktVerdi { get; set; }
    public DateTimeOffset UtlopUtc { get; set; }
    public DateTimeOffset? BruktUtc { get; set; }
    public long OpprettetAvBehandlerId { get; set; }
}
