namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// En behandler, jf. Del 2 i kravdokumentet: "en bruker hvor følgende data må
/// være lagt til av administrator, eller selv-innlagt av tester selv". Opprettes
/// av administrator med kun kontaktinfo (status <see cref="BehandlerStatus.Invitert"/>);
/// <see cref="FulltNavn"/> og <see cref="HprNr"/> fylles inn av behandleren selv via
/// invitasjonslenken (se BehandlerInvitasjonService). Selve behandler-innlogging/-portal
/// er Del 3 og finnes ikke ennå — denne entiteten er kun stamdata + status.
/// </summary>
public sealed class Behandler
{
    public long Id { get; set; }
    public required string MobilNr { get; set; }
    public required string Email { get; set; }
    public string? FulltNavn { get; set; }
    public string? HprNr { get; set; }
    public BehandlerStatus Status { get; set; } = BehandlerStatus.Invitert;
    public long InvitertAvAdministratorId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
    public DateTimeOffset? ArkivertUtc { get; set; }
}
