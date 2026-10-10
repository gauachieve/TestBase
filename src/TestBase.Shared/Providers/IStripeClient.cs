namespace TestBase.Shared.Providers;

public sealed record StripeOpprettetBetaling(bool Success, string BetalingsId, string? ClientSecret, string? ErrorMessage);

public sealed record StripeStatusResultat(bool Success, bool ErBetalt, string? ErrorMessage);

/// <summary>Opprettet Stripe Connect Express-konto — <see cref="StripeAccountId"/> ("acct_...") lagres på UtbetalingsMottakerKonto, aldri selve bankkontoen (Stripe holder den).</summary>
public sealed record StripeConnectKontoResultat(bool Success, string StripeAccountId, string? ErrorMessage);

/// <summary>Engangslenke til Stripes vertede onboarding-skjema (AccountLink) — utløper etter kort tid, generer en ny per forsøk.</summary>
public sealed record StripeOnboardingLenke(bool Success, string? Url, string? ErrorMessage);

/// <summary>Resultat av én Transfer fra plattformens Stripe-saldo til en mottakers Connect-konto — IKKE selve bankutbetalingen (den skjer senere, på Stripes egen utbetalingsplan for kontoen, se payout.failed-webhook).</summary>
public sealed record StripeOverforingResultat(bool Success, string? StripeTransferId, string? ErrorMessage);

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

    /// <summary>
    /// Oppretter en ny Stripe Connect Express-konto for en behandler/partner som
    /// skal motta utbetaling — selve kontoen må deretter fullføres via en
    /// onboarding-lenke (<see cref="OpprettOnboardingLenkeAsync"/>) før den kan
    /// motta penger. Ingen bankkontoinformasjon samles her eller noensinne hos oss.
    /// </summary>
    Task<StripeConnectKontoResultat> OpprettConnectKontoAsync(string epost, CancellationToken cancellationToken = default);

    /// <summary>
    /// En engangslenke til Stripes eget vertede onboarding-skjema (identitet +
    /// bankkonto, samles og oppbevares KUN av Stripe). <paramref name="returnUrl"/>
    /// brukes når onboarding fullføres, <paramref name="refreshUrl"/> hvis lenken
    /// utløper underveis — begge peker tilbake til samme side i vår app, som selv
    /// ALDRI stoler blindt på retur-URL-en alene, kun på account.updated-webhooken.
    /// </summary>
    Task<StripeOnboardingLenke> OpprettOnboardingLenkeAsync(
        string stripeAccountId, string returnUrl, string refreshUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Overfører penger fra plattformens egen Stripe-saldo til mottakerens
    /// Connect-konto — KUN kalt fra en eksplisitt admin-godkjent
    /// utbetalingslinje, aldri automatisk. Selve bankutbetalingen fra
    /// mottakerens Connect-konto til deres bank skjer deretter på Stripes egen
    /// plan, utenfor vår kontroll — se payout.failed-håndtering i PaymentWebhooks.
    /// </summary>
    Task<StripeOverforingResultat> OpprettOverforingAsync(
        string stripeAccountId, decimal belopNok, string referanse, CancellationToken cancellationToken = default);
}
