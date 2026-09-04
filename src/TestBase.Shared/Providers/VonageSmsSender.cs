using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers;

/// <summary>
/// Ekte SMS-utsending via Vonage Messages API. Valgt fremfor Azure
/// Communication Services fordi Norge krever forhåndsregistrert
/// alfanumerisk avsender-ID der (6–8 uker, delvis kun via support-sak),
/// mens Vonage aksepterer et fritt avsendernavn til Norge umiddelbart —
/// verifisert manuelt 2026-09 (se docs/beslutningslogg.md). Registreres
/// kun når "Vonage:ApiKey"/"Vonage:ApiSecret"/"Sms:SenderId" alle er
/// satt, ellers brukes MockSmsSender.
/// </summary>
public sealed class VonageSmsSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly string _senderId;
    private readonly ILogger<VonageSmsSender> _logger;

    public VonageSmsSender(HttpClient http, string apiKey, string apiSecret, string senderId, ILogger<VonageSmsSender> logger)
    {
        _http = http;
        _senderId = senderId;
        _logger = logger;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
        _http.BaseAddress = new Uri("https://api.nexmo.com/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task SendAsync(string toMobileNumber, string message, CancellationToken cancellationToken = default)
    {
        var request = new VonageMeldingRequest(NormaliserNorskNummer(toMobileNumber), _senderId, message);

        using var response = await _http.PostAsJsonAsync("v1/messages", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "SMS-utsending til {To} feilet (Vonage-status {Status}): {Body}", toMobileNumber, response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// MobilNr er et fritekstfelt uten formatvalidering — normaliserer til
    /// det Vonage forventer (kun siffer, ingen "+", norsk landkode).
    /// </summary>
    private static string NormaliserNorskNummer(string mobilNr)
    {
        var siffer = new string(mobilNr.Where(char.IsDigit).ToArray());
        return siffer.Length == 8 ? $"47{siffer}" : siffer;
    }

    private sealed record VonageMeldingRequest(
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("channel")] string Channel = "sms",
        [property: JsonPropertyName("message_type")] string MessageType = "text");
}
