using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers;

/// <summary>
/// Ekte e-postutsending via Azure Communication Services Email — Azure-native,
/// samme abonnement som resten av infrastrukturen, ingen egen leverandøravtale
/// nødvendig (se docs/beslutningslogg.md). Registreres kun når
/// "Acs:ConnectionString" er satt (App Service-innstilling i Azure via
/// infra/resources.bicep, aldri lokalt) — ellers brukes MockEmailSender.
/// </summary>
public sealed class AzureEmailSender : IEmailSender
{
    private readonly EmailClient _client;
    private readonly string _senderAddress;
    private readonly ILogger<AzureEmailSender> _logger;

    public AzureEmailSender(string connectionString, string senderAddress, ILogger<AzureEmailSender> logger)
    {
        _client = new EmailClient(connectionString);
        _senderAddress = senderAddress;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var melding = new EmailMessage(_senderAddress, toEmail, new EmailContent(subject) { PlainText = body });

        try
        {
            await _client.SendAsync(WaitUntil.Completed, melding, cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "E-postutsending til {To} feilet (ACS-status {Status}).", toEmail, ex.Status);
            throw;
        }
    }
}
