namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Superadmin-kuratert allow-list: hvilke tester en partners behandlere får
/// lov til å bruke. Partneren velger IKKE dette selv (se
/// docs/beslutningslogg.md "Partner System + Test Monetization") — alle
/// tester er felles/delt, men tilgjengeligheten per partner styres av oss.
/// </summary>
public sealed class PartnerTestTilgang
{
    public long Id { get; set; }
    public long PartnerId { get; set; }
    public long TestId { get; set; }
    public long GittAvAdministratorId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
}
