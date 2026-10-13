using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers.Mock;

/// <summary>Logger varselet i stedet for å faktisk sende noe via Pushover. Brukes når "Pushover:AppToken" ikke er konfigurert i dette miljøet.</summary>
public sealed class MockKritiskVarselSender : IKritiskVarselSender
{
    private readonly ILogger<MockKritiskVarselSender> _logger;

    public MockKritiskVarselSender(ILogger<MockKritiskVarselSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string mottakerBrukerKey, string tittel, string melding, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK PUSHOVER] Til {BrukerKey}: {Tittel} — {Melding}", mottakerBrukerKey, tittel, melding);
        return Task.CompletedTask;
    }
}
