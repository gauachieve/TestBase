namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Partner-administratorens EGEN andel (i kr) av pasientens betaling for en
/// gitt test — selvbetjent, satt av partnerens egen partner-admin, men
/// alltid klemt til minst <see cref="Test.MinstePartnerAndelKr"/> på
/// skrivetidspunktet (se TestPrisberegner). Mangler det en rad for et gitt
/// partner+test-par, er effektiv andel <see cref="Test.MinstePartnerAndelKr"/>.
/// </summary>
public sealed class PartnerTestAndel
{
    public long Id { get; set; }
    public long PartnerId { get; set; }
    public long TestId { get; set; }
    public decimal AndelKr { get; set; }
    public long SistEndretAvBehandlerId { get; set; }
    public DateTimeOffset SistEndretUtc { get; set; }
}
