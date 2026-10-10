namespace TestBase.Shared.Domain.Utbetaling;

public enum MottakerType
{
    Behandler,
    Partner
}

public enum StripeConnectStatus
{
    OnboardingStartet,
    OnboardingFullfort,
    Restriktert,
    Deaktivert
}

/// <summary>
/// Én rad per behandler/partner sin Stripe Connect Express-konto (se
/// IStripeClient.OpprettConnectKontoAsync) — selve bankkontoen samles og
/// oppbevares KUN av Stripe gjennom deres eget vertede onboarding-skjema,
/// ALDRI i denne databasen. <see cref="PayoutsEnabled"/>/<see cref="DetailsSubmitted"/>
/// speiler Stripe sin egen account.payouts_enabled/account.details_submitted
/// og oppdateres KUN av account.updated-webhooken (se PaymentWebhooks.cs) —
/// en retur til vår onboarding-side alene er ALDRI nok til å stole på at
/// onboarding faktisk er fullført.
/// </summary>
public sealed class UtbetalingsMottakerKonto
{
    public long Id { get; set; }
    public MottakerType MottakerType { get; set; }

    /// <summary>Nøyaktig én av BehandlerId/PartnerId er satt, avhengig av MottakerType.</summary>
    public long? BehandlerId { get; set; }
    public long? PartnerId { get; set; }

    public required string StripeAccountId { get; set; }
    public StripeConnectStatus Status { get; set; } = StripeConnectStatus.OnboardingStartet;

    public bool PayoutsEnabled { get; set; }
    public bool DetailsSubmitted { get; set; }

    public DateTimeOffset OpprettetUtc { get; set; }
    public DateTimeOffset? OnboardingFullfortUtc { get; set; }
    public DateTimeOffset? SistSynkronisertUtc { get; set; }
}
