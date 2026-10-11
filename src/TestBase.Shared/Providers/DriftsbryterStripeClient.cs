using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Shared.Providers;

/// <summary>
/// KUN brukt på live/lokal dev (ikke beta, se DriftsbryterVippsClient). Skiller BEVISST mellom
/// to uavhengige brytere på samme IStripeClient, siden metodene tjener to ulike pengeflyter:
/// OpprettBetalingsintensjonAsync/HentStatusAsync (PASIENTBETALING) styres av
/// DriftsbryterInnstilling.BetalingErAktiv, mens OpprettConnectKontoAsync/OpprettOnboardingLenkeAsync/
/// OpprettOverforingAsync (STRIPE CONNECT-UTBETALING til behandler/klinikk) styres av
/// DriftsbryterInnstilling.UtbetalingErAktiv — se der for begrunnelse.
///
/// Registrert Scoped (IStripeClient har alltid vært det i denne grenen) — DriftsbryterInnstillingService
/// kan derfor injiseres direkte, samme som BetaSwitchingStripeClient.
/// </summary>
public sealed class DriftsbryterStripeClient : IStripeClient
{
    private readonly DriftsbryterInnstillingService _innstillinger;
    private readonly IStripeClient _mock;
    private readonly IStripeClient? _ekte;

    public DriftsbryterStripeClient(DriftsbryterInnstillingService innstillinger, IStripeClient mock, IStripeClient? ekte)
    {
        _innstillinger = innstillinger;
        _mock = mock;
        _ekte = ekte;
    }

    public async Task<StripeOpprettetBetaling> OpprettBetalingsintensjonAsync(
        decimal belopNok, string beskrivelse, string referanse, CancellationToken cancellationToken = default) =>
        await (await BetalingsklientAsync(cancellationToken)).OpprettBetalingsintensjonAsync(belopNok, beskrivelse, referanse, cancellationToken);

    public async Task<StripeStatusResultat> HentStatusAsync(string betalingsId, CancellationToken cancellationToken = default) =>
        await (await BetalingsklientAsync(cancellationToken)).HentStatusAsync(betalingsId, cancellationToken);

    public async Task<StripeConnectKontoResultat> OpprettConnectKontoAsync(string epost, CancellationToken cancellationToken = default) =>
        await (await UtbetalingsklientAsync(cancellationToken)).OpprettConnectKontoAsync(epost, cancellationToken);

    public async Task<StripeOnboardingLenke> OpprettOnboardingLenkeAsync(
        string stripeAccountId, string returnUrl, string refreshUrl, CancellationToken cancellationToken = default) =>
        await (await UtbetalingsklientAsync(cancellationToken)).OpprettOnboardingLenkeAsync(stripeAccountId, returnUrl, refreshUrl, cancellationToken);

    public async Task<StripeOverforingResultat> OpprettOverforingAsync(
        string stripeAccountId, decimal belopNok, string referanse, CancellationToken cancellationToken = default) =>
        await (await UtbetalingsklientAsync(cancellationToken)).OpprettOverforingAsync(stripeAccountId, belopNok, referanse, cancellationToken);

    private async Task<IStripeClient> BetalingsklientAsync(CancellationToken cancellationToken)
    {
        var innstilling = await _innstillinger.HentAsync(cancellationToken);
        return innstilling.BetalingErAktiv && _ekte is not null ? _ekte : _mock;
    }

    private async Task<IStripeClient> UtbetalingsklientAsync(CancellationToken cancellationToken)
    {
        var innstilling = await _innstillinger.HentAsync(cancellationToken);
        return innstilling.UtbetalingErAktiv && _ekte is not null ? _ekte : _mock;
    }
}
