namespace TestBase.Shared.Domain.Tester;

/// <summary>Om forespørselen ber om å legge til, eller fjerne, en test fra partnerens PartnerTestTilgang-allow-list.</summary>
public enum TestTilgangHandling
{
    LeggTil,
    Fjern
}

public enum TestTilgangForespoerselStatus
{
    Venter,
    Godkjent,
    Avvist
}

/// <summary>
/// Partner-admins selvbetjente forespørsel om å legge til/fjerne en test fra
/// partnerens PartnerTestTilgang-allow-list — trer IKKE i kraft før en
/// administrator/superadmin har godkjent den (se
/// TestService.BehandleTestTilgangForesporslerAsync), i motsetning til
/// Admin/Partnere/Tester sin direkte OnPostToggleAsync. Se
/// docs/beslutningslogg.md "Test-tilgangsforespørsler". Bevisst partner-scoped
/// kun — unaffiliert behandler har allerede ubegrenset testtilgang, se samme
/// seksjon for begrunnelse.
/// </summary>
public sealed class TestTilgangForespoersel
{
    public long Id { get; set; }
    public long PartnerId { get; set; }
    public long TestId { get; set; }
    public TestTilgangHandling Handling { get; set; }
    public TestTilgangForespoerselStatus Status { get; set; } = TestTilgangForespoerselStatus.Venter;
    public long ForespurtAvBehandlerId { get; set; }
    public DateTimeOffset ForespurtUtc { get; set; }
    public long? BehandletAvAdministratorId { get; set; }
    public DateTimeOffset? BehandletUtc { get; set; }
}
