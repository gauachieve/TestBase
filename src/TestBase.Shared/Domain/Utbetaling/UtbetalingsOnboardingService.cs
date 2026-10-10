using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Utbetaling;

/// <summary>
/// Felles logikk for å koble en behandler/partner til en Stripe Connect
/// Express-konto for utbetaling — delt mellom Behandlerportal/MinSide (egen
/// konto) og Behandlerportal/MinPartner (partnerens konto), se
/// docs/beslutningslogg.md "Monthly Stripe Connect payout system". Selve
/// bankkontoen samles og oppbevares KUN av Stripe gjennom deres vertede
/// onboarding-skjema — denne tjenesten lagrer aldri noe bankkontonummer.
/// </summary>
public sealed class UtbetalingsOnboardingService
{
    private readonly AppDbContext _db;
    private readonly IStripeClient _stripeClient;

    public UtbetalingsOnboardingService(AppDbContext db, IStripeClient stripeClient)
    {
        _db = db;
        _stripeClient = stripeClient;
    }

    public Task<UtbetalingsMottakerKonto?> HentForBehandlerAsync(long behandlerId, CancellationToken cancellationToken = default) =>
        _db.UtbetalingsMottakerKontoer.FirstOrDefaultAsync(
            k => k.MottakerType == MottakerType.Behandler && k.BehandlerId == behandlerId, cancellationToken);

    public Task<UtbetalingsMottakerKonto?> HentForPartnerAsync(long partnerId, CancellationToken cancellationToken = default) =>
        _db.UtbetalingsMottakerKontoer.FirstOrDefaultAsync(
            k => k.MottakerType == MottakerType.Partner && k.PartnerId == partnerId, cancellationToken);

    /// <summary>
    /// Finner eksisterende konto eller oppretter en ny Stripe Connect-konto,
    /// deretter en engangs onboarding-lenke å sende brukeren til. Returnerer
    /// null (ingen unntak kastet til siden) hvis Stripe-kallet feiler — siden
    /// kaller må vise en feilmelding, ikke krasje.
    /// </summary>
    public async Task<string?> StartOnboardingAsync(
        MottakerType type, long mottakerId, string epost, string returnUrl, string refreshUrl,
        CancellationToken cancellationToken = default)
    {
        var konto = type == MottakerType.Behandler
            ? await HentForBehandlerAsync(mottakerId, cancellationToken)
            : await HentForPartnerAsync(mottakerId, cancellationToken);

        if (konto is null)
        {
            var kontoResultat = await _stripeClient.OpprettConnectKontoAsync(epost, cancellationToken);
            if (!kontoResultat.Success)
            {
                return null;
            }

            konto = new UtbetalingsMottakerKonto
            {
                MottakerType = type,
                BehandlerId = type == MottakerType.Behandler ? mottakerId : null,
                PartnerId = type == MottakerType.Partner ? mottakerId : null,
                StripeAccountId = kontoResultat.StripeAccountId,
                OpprettetUtc = DateTimeOffset.UtcNow
            };
            _db.UtbetalingsMottakerKontoer.Add(konto);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var lenke = await _stripeClient.OpprettOnboardingLenkeAsync(konto.StripeAccountId, returnUrl, refreshUrl, cancellationToken);
        return lenke.Success ? lenke.Url : null;
    }

    /// <summary>
    /// Kalt KUN fra account.updated-webhooken (PaymentWebhooks.cs) — aldri fra
    /// retur-URL-en alene, se klassekommentaren på UtbetalingsMottakerKonto.
    /// </summary>
    public async Task OppdaterFraWebhookAsync(
        string stripeAccountId, bool payoutsEnabled, bool detailsSubmitted, CancellationToken cancellationToken = default)
    {
        var konto = await _db.UtbetalingsMottakerKontoer
            .FirstOrDefaultAsync(k => k.StripeAccountId == stripeAccountId, cancellationToken);
        if (konto is null)
        {
            return;
        }

        konto.PayoutsEnabled = payoutsEnabled;
        konto.DetailsSubmitted = detailsSubmitted;
        konto.SistSynkronisertUtc = DateTimeOffset.UtcNow;

        if (payoutsEnabled && detailsSubmitted && konto.Status != StripeConnectStatus.OnboardingFullfort)
        {
            konto.Status = StripeConnectStatus.OnboardingFullfort;
            konto.OnboardingFullfortUtc = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
