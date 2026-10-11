namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Bugliste 2026-10-11 punkt 4 — en klinikk-admin kurerer en liste tester/hjemmeoppgaver som
/// automatisk tildeles og varsles enhver pasient som enten (a) nettopp ble lagt til av en
/// behandler i klinikken (eller klinikk-admin selv), eller (b) nettopp ble flyttet til "Avsluttet"
/// — se <see cref="PartnerAutomatiskUtsendelseUtloser"/>. Analogt til PartnerTestTilgang
/// (klinikken velger ikke HVILKE tester som finnes, men HER velger den selv hvilke av de allerede
/// tilgjengelige testene som skal sendes automatisk), men styrt av klinikken selv, ikke
/// Superadmin.
/// </summary>
public sealed class PartnerAutomatiskUtsendelse
{
    public long Id { get; set; }
    public long PartnerId { get; set; }
    public long TestId { get; set; }
    public PartnerAutomatiskUtsendelseUtloser Utloser { get; set; }
    public long OpprettetAvBehandlerId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
}

public enum PartnerAutomatiskUtsendelseUtloser
{
    VedNyPasient,
    VedAvsluttet
}
