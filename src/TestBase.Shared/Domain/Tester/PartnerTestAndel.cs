namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Partner-administratorens EGEN andel (prosent, 0-100) av pasientens
/// totalpris for en gitt test — selvbetjent, satt av partnerens egen
/// partner-admin, men alltid klemt til minst
/// <see cref="Test.MinstePartnerAndelProsent"/> på skrivetidspunktet (se
/// TestPrisberegner). Mangler det en rad for et gitt partner+test-par, er
/// effektiv andel <see cref="Test.MinstePartnerAndelProsent"/>.
/// </summary>
public sealed class PartnerTestAndel
{
    public long Id { get; set; }
    public long PartnerId { get; set; }
    public long TestId { get; set; }

    public decimal AndelProsent { get; set; }

    public long SistEndretAvBehandlerId { get; set; }
    public DateTimeOffset SistEndretUtc { get; set; }
}
