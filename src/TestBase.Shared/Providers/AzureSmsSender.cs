using Azure;
using Azure.Communication.Sms;
using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers;

/// <summary>
/// Ekte SMS-utsending via Azure Communication Services — samme
/// Communication Services-ressurs (og tilkoblingsstreng) som
/// <see cref="AzureEmailSender"/> allerede bruker for e-post, siden SMS
/// er en frittstående kapabilitet på samme ressurs, ikke en egen
/// underressurs slik e-postdomenet er (se docs/beslutningslogg.md).
/// Krever et forhåndsregistrert alfanumerisk avsendernavn (f.eks.
/// "PsyTest") — Norge tillater IKKE dynamisk/uregistrert avsender-ID,
/// søknad sendes manuelt via Azure Portal (ingen ARM/Bicep-ressurs for
/// dette), 6–8 ukers behandlingstid. Registreres kun når
/// "Sms:SenderId" er satt, ellers brukes MockSmsSender.
/// </summary>
public sealed class AzureSmsSender : ISmsSender
{
    private readonly SmsClient _client;
    private readonly string _senderId;
    private readonly ILogger<AzureSmsSender> _logger;

    public AzureSmsSender(string connectionString, string senderId, ILogger<AzureSmsSender> logger)
    {
        _client = new SmsClient(connectionString);
        _senderId = senderId;
        _logger = logger;
    }

    public async Task SendAsync(string toMobileNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var resultat = await _client.SendAsync(_senderId, toMobileNumber, message, cancellationToken: cancellationToken);
            if (!resultat.Value.Successful)
            {
                _logger.LogError(
                    "SMS-utsending til {To} feilet (ACS-feilkode {ErrorMessage}).", toMobileNumber, resultat.Value.ErrorMessage);
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "SMS-utsending til {To} feilet (ACS-status {Status}).", toMobileNumber, ex.Status);
            throw;
        }
    }
}
