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

    /// <summary>
    /// UTFASES (se docs/beslutningslogg.md "Abonnement/subscription-system") —
    /// erstattes av <see cref="AndelProsent"/>. Beholdes til alle kallsteder
    /// er flyttet over.
    /// </summary>
    public decimal AndelKr { get; set; }

    /// <summary>Partner-adminens egen andel (0-100) av pasientens totalpris, klemt til minst Test.MinstePartnerAndelProsent.</summary>
    public decimal AndelProsent { get; set; }

    public long SistEndretAvBehandlerId { get; set; }
    public DateTimeOffset SistEndretUtc { get; set; }
}
