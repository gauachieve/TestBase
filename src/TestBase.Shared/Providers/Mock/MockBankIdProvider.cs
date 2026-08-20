using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers.Mock;

/// <summary>
/// Later som en BankID-innlogging lykkes, uten å kalle noen ekstern
/// tjeneste. Returnerer alltid en fast, tydelig fiktiv testperson —
/// KUN til bruk i lokalt utviklingsmiljø.
/// </summary>
public sealed class MockBankIdProvider : IBankIdProvider
{
    private readonly ILogger<MockBankIdProvider> _logger;

    public MockBankIdProvider(ILogger<MockBankIdProvider> logger)
    {
        _logger = logger;
    }

    public Task<BankIdResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK BankID] Simulerer vellykket innlogging for fiktiv testperson.");

        var result = new BankIdResult(
            Success: true,
            PersonNummer: "01019012345",
            FullName: "Fiktiv Testperson",
            ErrorMessage: null);

        return Task.FromResult(result);
    }
}
