using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers.Mock;

/// <summary>
/// Logger SMS-innhold i stedet for å faktisk sende noe. KUN til bruk i
/// lokalt utviklingsmiljø.
/// </summary>
public sealed class MockSmsSender : ISmsSender
{
    private readonly ILogger<MockSmsSender> _logger;

    public MockSmsSender(ILogger<MockSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toMobileNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK SMS] Til {To}: {Message}", toMobileNumber, message);
        return Task.CompletedTask;
    }
}
