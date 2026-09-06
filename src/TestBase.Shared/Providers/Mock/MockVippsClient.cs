using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers.Mock;

/// <summary>
/// Later som en Vipps-betaling opprettes og umiddelbart er fanget, uten å
/// kalle noen ekstern tjeneste og uten at penger noensinne flyttes. KUN til
/// bruk i lokalt utviklingsmiljø/test uten ekte Vipps-avtale.
/// </summary>
public sealed class MockVippsClient : IVippsClient
{
    private readonly ILogger<MockVippsClient> _logger;

    public MockVippsClient(ILogger<MockVippsClient> logger)
    {
        _logger = logger;
    }

    public Task<VippsOpprettetBetaling> OpprettBetalingAsync(
        string referanse, decimal belopNok, string beskrivelse, string returUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[MOCK Vipps] Simulerer opprettelse av betaling {Referanse} på {Belop} kr for '{Beskrivelse}'.",
            referanse, belopNok, beskrivelse);

        // Simulerer Vipps sin redirect-flyt ved å sende brukeren rett til returUrl
        // med et fiktivt "vellykket"-signal — se BetalingTest-sidene.
        var fiktivRedirectUrl = returUrl.Contains('?')
            ? $"{returUrl}&mockVippsReferanse={Uri.EscapeDataString(referanse)}"
            : $"{returUrl}?mockVippsReferanse={Uri.EscapeDataString(referanse)}";

        return Task.FromResult(new VippsOpprettetBetaling(Success: true, referanse, fiktivRedirectUrl, ErrorMessage: null));
    }

    public Task<VippsStatusResultat> HentStatusAsync(string referanse, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Vipps] Simulerer statusoppslag for {Referanse}: Fanget.", referanse);
        return Task.FromResult(new VippsStatusResultat(Success: true, VippsBetalingsstatus.Fanget, ErrorMessage: null));
    }
}
