using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers.Mock;

/// <summary>
/// Later som en Vipps-betaling lykkes, uten å kalle noen ekstern tjeneste
/// og uten at penger noensinne flyttes. KUN til bruk i lokalt utviklingsmiljø.
/// </summary>
public sealed class MockVippsClient : IVippsClient
{
    private readonly ILogger<MockVippsClient> _logger;

    public MockVippsClient(ILogger<MockVippsClient> logger)
    {
        _logger = logger;
    }

    public Task<VippsPaymentResult> ChargeAsync(decimal amountNok, string description, CancellationToken cancellationToken = default)
    {
        var fakeReference = $"MOCK-{Guid.NewGuid():N}";
        _logger.LogInformation(
            "[MOCK Vipps] Simulerer belastning av {Amount} kr for '{Description}'. Referanse: {Reference}",
            amountNok, description, fakeReference);

        var result = new VippsPaymentResult(Success: true, TransactionReference: fakeReference, ErrorMessage: null);
        return Task.FromResult(result);
    }
}
