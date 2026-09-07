using Microsoft.Extensions.Logging;
using Stripe;

namespace TestBase.Shared.Providers;

/// <summary>
/// Ekte implementasjon mot Stripe (https://stripe.com), via den offisielle
/// Stripe.net-SDK-en. Registreres kun når "Stripe:SecretKey" er satt (se
/// Program.cs) — ellers brukes MockStripeClient.
///
/// Bruker "automatic_payment_methods" fremfor å liste opp betalingsmetoder
/// eksplisitt — dette er det Stripe selv anbefaler, og er det som gjør at
/// Apple Pay/Google Pay dukker opp automatisk i Payment Element på
/// klientsiden når enheten støtter det, uten egen kode her.
/// </summary>
public sealed class StripePaymentClient : IStripeClient
{
    private readonly string _secretKey;
    private readonly ILogger<StripePaymentClient> _logger;

    public StripePaymentClient(string secretKey, ILogger<StripePaymentClient> logger)
    {
        _secretKey = secretKey;
        _logger = logger;
    }

    public async Task<StripeOpprettetBetaling> OpprettBetalingsintensjonAsync(
        decimal belopNok, string beskrivelse, string referanse, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)Math.Round(belopNok * 100m, MidpointRounding.AwayFromZero),
                Currency = "nok",
                Description = beskrivelse,
                Metadata = new Dictionary<string, string> { ["referanse"] = referanse },
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
            };
            var requestOptions = new RequestOptions { ApiKey = _secretKey };
            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options, requestOptions, cancellationToken);

            return new StripeOpprettetBetaling(true, intent.Id, intent.ClientSecret, null);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe OpprettBetalingsintensjon feilet: {Melding}", ex.StripeError?.Message);
            return new StripeOpprettetBetaling(false, string.Empty, null, ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task<StripeStatusResultat> HentStatusAsync(string betalingsId, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestOptions = new RequestOptions { ApiKey = _secretKey };
            var service = new PaymentIntentService();
            var intent = await service.GetAsync(betalingsId, requestOptions: requestOptions, cancellationToken: cancellationToken);
            return new StripeStatusResultat(true, intent.Status == "succeeded", null);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe HentStatus feilet for {Id}: {Melding}", betalingsId, ex.StripeError?.Message);
            return new StripeStatusResultat(false, false, ex.StripeError?.Message ?? ex.Message);
        }
    }
}
