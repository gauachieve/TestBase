using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers.Mock;

/// <summary>
/// Later som en Stripe-betaling opprettes og umiddelbart lykkes, uten å kalle
/// Stripe og uten at penger noensinne flyttes. KUN til bruk i lokalt
/// utviklingsmiljø/test uten ekte Stripe-konto.
/// </summary>
public sealed class MockStripeClient : IStripeClient
{
    private readonly ILogger<MockStripeClient> _logger;

    public MockStripeClient(ILogger<MockStripeClient> logger)
    {
        _logger = logger;
    }

    public Task<StripeOpprettetBetaling> OpprettBetalingsintensjonAsync(
        decimal belopNok, string beskrivelse, string referanse, CancellationToken cancellationToken = default)
    {
        var fiktivId = $"MOCK-{Guid.NewGuid():N}";
        _logger.LogInformation(
            "[MOCK Stripe] Simulerer opprettelse av betalingsintensjon {Id} på {Belop} kr for '{Beskrivelse}' (referanse {Referanse}).",
            fiktivId, belopNok, beskrivelse, referanse);

        return Task.FromResult(new StripeOpprettetBetaling(true, fiktivId, ClientSecret: null, ErrorMessage: null));
    }

    public Task<StripeStatusResultat> HentStatusAsync(string betalingsId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Stripe] Simulerer statusoppslag for {Id}: betalt.", betalingsId);
        return Task.FromResult(new StripeStatusResultat(true, ErBetalt: true, ErrorMessage: null));
    }
}
