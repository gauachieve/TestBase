using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Shared.Providers;

/// <summary>Stripe-motstykke til BetaSwitchingVippsClient — kun Mock/Test, se der for begrunnelse.</summary>
public sealed class BetaSwitchingStripeClient : IStripeClient
{
    private readonly BetaInnstillingService _innstillinger;
    private readonly IStripeClient _mock;
    private readonly IStripeClient? _test;

    public BetaSwitchingStripeClient(BetaInnstillingService innstillinger, IStripeClient mock, IStripeClient? test)
    {
        _innstillinger = innstillinger;
        _mock = mock;
        _test = test;
    }

    public async Task<StripeOpprettetBetaling> OpprettBetalingsintensjonAsync(
        decimal belopNok, string beskrivelse, string referanse, CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).OpprettBetalingsintensjonAsync(belopNok, beskrivelse, referanse, cancellationToken);

    public async Task<StripeStatusResultat> HentStatusAsync(string betalingsId, CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).HentStatusAsync(betalingsId, cancellationToken);

    private async Task<IStripeClient> AktivKlientAsync(CancellationToken cancellationToken)
    {
        var innstilling = await _innstillinger.HentAsync(cancellationToken);
        return innstilling.StripeModus switch
        {
            StripeDriftsmodus.Test when _test is not null => _test,
            _ => _mock
        };
    }
}
