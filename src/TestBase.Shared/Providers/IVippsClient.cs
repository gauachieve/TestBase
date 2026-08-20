namespace TestBase.Shared.Providers;

public sealed record VippsPaymentResult(bool Success, string TransactionReference, string? ErrorMessage);

/// <summary>
/// Grensesnitt mot Vipps-betaling. Selve korttall/betalingsdetaljer skal
/// ALDRI lagres i egen database — kun transaksjonsreferansen Vipps gir
/// tilbake (se compliance-risikovurdering-utkast.md). Ekte implementasjon
/// kommer i fase 6. I dev/test brukes MockVippsClient.
/// </summary>
public interface IVippsClient
{
    Task<VippsPaymentResult> ChargeAsync(decimal amountNok, string description, CancellationToken cancellationToken = default);
}
