using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers.Mock;

/// <summary>
/// Logger e-postinnhold i stedet for å faktisk sende noe. KUN til bruk i
/// lokalt utviklingsmiljø.
/// </summary>
public sealed class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK E-post] Til {To}, emne '{Subject}': {Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
