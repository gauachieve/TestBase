using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers;

/// <summary>
/// Ekte implementasjon mot Vipps ePayment API (https://developer.vippsmobilepay.com).
/// Registreres kun når Vipps:ClientId/ClientSecret/SubscriptionKey/MerchantSerialNumber
/// alle er satt (se Program.cs) — ellers brukes MockVippsClient.
///
/// Tilgangstoken hentes via /accesstoken/get og caches i minnet (tokenet varer normalt
/// ca. 1 time) — denne klassen er derfor registrert som Singleton i DI, med en
/// SemaphoreSlim for å hindre at flere samtidige forespørsler henter token på likt.
/// </summary>
public sealed class VippsPaymentClient : IVippsClient
{
    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _subscriptionKey;
    private readonly string _merchantSerialNumber;
    private readonly ILogger<VippsPaymentClient> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _tokenUtlopUtc = DateTimeOffset.MinValue;

    public VippsPaymentClient(
        HttpClient http,
        string baseUrl,
        string clientId,
        string clientSecret,
        string subscriptionKey,
        string merchantSerialNumber,
        ILogger<VippsPaymentClient> logger)
    {
        _http = http;
        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + '/');
        _clientId = clientId;
        _clientSecret = clientSecret;
        _subscriptionKey = subscriptionKey;
        _merchantSerialNumber = merchantSerialNumber;
        _logger = logger;
    }

    public async Task<VippsOpprettetBetaling> OpprettBetalingAsync(
        string referanse, decimal belopNok, string beskrivelse, string returUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await HentTokenAsync(cancellationToken);

            using var request = NyForespørsel(HttpMethod.Post, "epayment/v1/payments", token);
            request.Content = JsonContent.Create(new
            {
                amount = new { currency = "NOK", value = (long)Math.Round(belopNok * 100m, MidpointRounding.AwayFromZero) },
                paymentMethod = new { type = "WALLET" },
                reference = referanse,
                returnUrl = returUrl,
                userFlow = "WEB_REDIRECT",
                paymentDescription = beskrivelse
            });

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var feilInnhold = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Vipps OpprettBetaling feilet ({Status}): {Innhold}", response.StatusCode, feilInnhold);
                return new VippsOpprettetBetaling(false, referanse, null, $"Vipps svarte {(int)response.StatusCode}: {feilInnhold}");
            }

            var svar = await response.Content.ReadFromJsonAsync<VippsOpprettPayload>(cancellationToken: cancellationToken);
            if (svar?.RedirectUrl is null)
            {
                return new VippsOpprettetBetaling(false, referanse, null, "Vipps-svaret manglet redirectUrl.");
            }

            return new VippsOpprettetBetaling(true, referanse, svar.RedirectUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uventet feil ved opprettelse av Vipps-betaling {Referanse}.", referanse);
            return new VippsOpprettetBetaling(false, referanse, null, ex.Message);
        }
    }

    public async Task<VippsStatusResultat> HentStatusAsync(string referanse, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await HentTokenAsync(cancellationToken);
            using var request = NyForespørsel(HttpMethod.Get, $"epayment/v1/payments/{Uri.EscapeDataString(referanse)}", token);
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var feilInnhold = await response.Content.ReadAsStringAsync(cancellationToken);
                return new VippsStatusResultat(false, null, $"Vipps svarte {(int)response.StatusCode}: {feilInnhold}");
            }

            var svar = await response.Content.ReadFromJsonAsync<VippsStatusPayload>(cancellationToken: cancellationToken);
            var status = svar?.State switch
            {
                "CREATED" => VippsBetalingsstatus.Opprettet,
                "AUTHORIZED" => VippsBetalingsstatus.Autorisert,
                "TERMINATED" => VippsBetalingsstatus.Kansellert,
                "EXPIRED" => VippsBetalingsstatus.Utlopt,
                "ABORTED" => VippsBetalingsstatus.Avbrutt,
                _ => (VippsBetalingsstatus?)null
            };

            // Vipps skiller AUTHORIZED (reservert) fra faktisk CAPTURE (fanget) via
            // aggregate.capturedAmount — vi behandler et fullt fanget beløp som "Fanget".
            if (status == VippsBetalingsstatus.Autorisert && svar?.Aggregate?.CapturedAmount?.Value > 0)
            {
                status = VippsBetalingsstatus.Fanget;
            }

            return new VippsStatusResultat(true, status, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uventet feil ved statusoppslag for Vipps-betaling {Referanse}.", referanse);
            return new VippsStatusResultat(false, null, ex.Message);
        }
    }

    private HttpRequestMessage NyForespørsel(HttpMethod metode, string sti, string token)
    {
        var request = new HttpRequestMessage(metode, sti);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
        request.Headers.Add("Merchant-Serial-Number", _merchantSerialNumber);
        request.Headers.Add("Vipps-System-Name", "TestBase");
        request.Headers.Add("Vipps-System-Version", "1.0.0");
        return request;
    }

    private async Task<string> HentTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenUtlopUtc)
        {
            return _cachedToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenUtlopUtc)
            {
                return _cachedToken;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "accesstoken/get");
            request.Headers.Add("client_id", _clientId);
            request.Headers.Add("client_secret", _clientSecret);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Merchant-Serial-Number", _merchantSerialNumber);

            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var svar = await response.Content.ReadFromJsonAsync<VippsTokenPayload>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tomt svar fra Vipps sitt accesstoken-endepunkt.");

            _cachedToken = svar.AccessToken;
            // Trekker fra 60 sekunder som sikkerhetsmargin før faktisk utløp.
            _tokenUtlopUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(svar.ExpiresIn - 60, 60));
            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private sealed class VippsTokenPayload
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class VippsOpprettPayload
    {
        [JsonPropertyName("redirectUrl")]
        public string? RedirectUrl { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }
    }

    private sealed class VippsStatusPayload
    {
        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("aggregate")]
        public VippsAggregatePayload? Aggregate { get; set; }
    }

    private sealed class VippsAggregatePayload
    {
        [JsonPropertyName("capturedAmount")]
        public VippsBelopPayload? CapturedAmount { get; set; }
    }

    private sealed class VippsBelopPayload
    {
        [JsonPropertyName("value")]
        public long Value { get; set; }
    }
}
