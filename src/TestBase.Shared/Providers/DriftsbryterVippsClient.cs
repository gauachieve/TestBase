using Microsoft.Extensions.DependencyInjection;
using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Shared.Providers;

/// <summary>
/// KUN brukt på live/lokal dev (ikke beta — beta har sin egen, mer granulære Mock/Test/Produksjon-
/// bryter via BetaSwitchingVippsClient, se Program.cs). Leser DriftsbryterInnstilling.BetalingErAktiv
/// FOR HVERT KALL og faller tilbake til mock når bryteren er av, selv om en ekte produksjonsklient
/// er konfigurert — se DriftsbryterInnstilling.
///
/// Registrert som Singleton (IVippsClient har alltid vært det her, siden VippsPaymentClient cacher
/// tilgangstoken i minnet), men DriftsbryterInnstillingService er Scoped — samme "kan ikke injisere
/// Scoped i Singleton"-problem som BetaSwitchingVippsClient allerede løste med en kortlevd
/// DI-scope per kall, se der for full begrunnelse.
/// </summary>
public sealed class DriftsbryterVippsClient : IVippsClient
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IVippsClient _mock;
    private readonly IVippsClient? _ekte;

    public DriftsbryterVippsClient(IServiceScopeFactory scopeFactory, IVippsClient mock, IVippsClient? ekte)
    {
        _scopeFactory = scopeFactory;
        _mock = mock;
        _ekte = ekte;
    }

    public async Task<VippsOpprettetBetaling> OpprettBetalingAsync(
        string referanse, decimal belopNok, string beskrivelse, string returUrl, CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).OpprettBetalingAsync(referanse, belopNok, beskrivelse, returUrl, cancellationToken);

    public async Task<VippsStatusResultat> HentStatusAsync(string referanse, CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).HentStatusAsync(referanse, cancellationToken);

    public async Task<VippsFangetResultat> FangBetalingAsync(string referanse, decimal belopNok, CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).FangBetalingAsync(referanse, belopNok, cancellationToken);

    public async Task<VippsWebhookRegistrertResultat> RegistrerWebhookAsync(string url, CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).RegistrerWebhookAsync(url, cancellationToken);

    public async Task<VippsWebhookListeResultat> HentWebhookerAsync(CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).HentWebhookerAsync(cancellationToken);

    public async Task<bool> SlettWebhookAsync(string webhookId, CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).SlettWebhookAsync(webhookId, cancellationToken);

    private async Task<IVippsClient> AktivKlientAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var innstillinger = scope.ServiceProvider.GetRequiredService<DriftsbryterInnstillingService>();
        var innstilling = await innstillinger.HentAsync(cancellationToken);
        return innstilling.BetalingErAktiv && _ekte is not null ? _ekte : _mock;
    }
}
