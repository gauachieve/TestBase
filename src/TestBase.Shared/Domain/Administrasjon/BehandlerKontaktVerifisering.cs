namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Én engangskode for å bekrefte at en behandlers e-post eller mobilnummer er
/// reelle (jf. kravdokumentets Del 3), sendt via mock e-post/SMS. To rader
/// opprettes ved fullføring av egenregistrering (én per kanal) — begge må
/// bekreftes før <see cref="Behandler.Status"/> blir Aktiv, se
/// BehandlerInvitasjonService.
/// </summary>
public sealed class BehandlerKontaktVerifisering
{
    public long Id { get; set; }
    public long BehandlerId { get; set; }
    public KontaktMetode Kanal { get; set; }
    public required string KodeHash { get; set; }
    public DateTimeOffset UtlopUtc { get; set; }
    public DateTimeOffset? BruktUtc { get; set; }
    public int Forsok { get; set; }
}
