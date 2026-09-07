namespace TestBase.Shared.Providers;

public sealed record StripeOpprettetBetaling(bool Success, string BetalingsId, string? ClientSecret, string? ErrorMessage);

public sealed record StripeStatusResultat(bool Success, bool ErBetalt, string? ErrorMessage);

/// <summary>
/// Grensesnitt mot Stripe (kort + Apple Pay + Google Pay — Apple/Google Pay er
/// bevisst IKKE egne betalingsmetoder å velge mellom, de dukker automatisk opp
/// som knapper i Stripes "Payment Element" på klientsiden når enheten/nettleseren
/// støtter dem og et betalingsmiddel av type "card" er aktivert, se
/// docs/beslutningslogg.md "Vipps + Stripe (Apple Pay/Google Pay)").
///
/// Som Vipps er dette asynkront: OpprettBetalingsintensjonAsync gir en
/// ClientSecret som brukes av Stripe.js på klientsiden til å vise betalings-
/// skjemaet, og selve utfallet bekreftes via webhook (se Security/PaymentWebhooks.cs)
/// eller HentStatusAsync — ALDRI klientsiden alene.
/// Ekte implementasjon: StripePaymentClient. I dev/test uten ekte konto: MockStripeClient.
/// </summary>
public interface IStripeClient
{
    /// <summary>
    /// <paramref name="referanse"/> lagres som Stripe-metadata ("referanse") på
    /// betalingsintensjonen — webhook-mottakeren (PaymentWebhooks.cs) leser den
    /// tilbake derfra for å vite HVILKEN TestTildeling betalingen gjelder,
    /// siden Stripe sin egen PaymentIntent-id ikke er noe vi velger selv.
    /// </summary>
    Task<StripeOpprettetBetaling> OpprettBetalingsintensjonAsync(
        decimal belopNok, string beskrivelse, string referanse, CancellationToken cancellationToken = default);

    Task<StripeStatusResultat> HentStatusAsync(string betalingsId, CancellationToken cancellationToken = default);
}
