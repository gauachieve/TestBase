using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using TestBase.Shared.Data;

namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Kjører en rekke lette, skrivebeskyttede helsesjekker mot eksterne leverandører og egen
/// database — se Admin/Driftsstatus og docs/beslutningslogg.md "Driftsstatus-dashboard"
/// (brukerbeslutning 2026-10-13). Går BEVISST direkte mot konfigurasjon/rå HTTP-kall i stedet
/// for å gå via IVippsClient/IStripeClient/ISmsSender — disse grensesnittene er bygget for
/// FAKTISKE forretningsoperasjoner (opprette en betaling, sende en SMS), og en helsesjekk skal
/// ALDRI utføre en slik operasjon (ingen ekte SMS sendt, ingen ekte betaling opprettet) kun for
/// å bekrefte at en nøkkel er gyldig. Hver sjekk er uavhengig og feiler aldri de andre (try/catch
/// rundt hver enkelt), og har en kort timeout slik at én hengende ekstern tjeneste ikke henger
/// hele siden.
///
/// IKKE dekket i denne første versjonen (bevisst utelatt, se beslutningsloggen for begrunnelse):
/// Azure-ressurshelse (CPU/minne/disk/skalering — krever Azure Management API-tilgang, en egen
/// infrastrukturendring), og "er selve siden nede" (et paradoks — en side PÅ appen kan per
/// definisjon ikke varsle om at hele appen er nede, krever ekte ekstern overvåking som Azure
/// Application Insights Availability Tests eller en tredjeparts uptime-sjekker mot /health).
/// </summary>
public sealed class DriftsstatusService
{
    private static readonly TimeSpan SjekkTimeout = TimeSpan.FromSeconds(6);

    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DriftsstatusService> _logger;

    public DriftsstatusService(AppDbContext db, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<DriftsstatusService> logger)
    {
        _db = db;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DriftsstatusSjekk>> KjorAlleSjekkerAsync(CancellationToken cancellationToken = default)
    {
        var oppgaver = new[]
        {
            SjekkDatabaseAsync(cancellationToken),
            SjekkVippsAsync(cancellationToken),
            SjekkStripeAsync(cancellationToken),
            SjekkSmsKredittAsync(cancellationToken),
            SjekkEpostAsync(cancellationToken),
            SjekkBankIdAsync(cancellationToken),
            SjekkUtbetalingerAsync(cancellationToken)
        };

        var resultater = await Task.WhenAll(oppgaver);
        return resultater;
    }

    private async Task<DriftsstatusSjekk> SjekkDatabaseAsync(CancellationToken cancellationToken)
    {
        return await KjorMedTimeoutAsync("Database (MySQL)", cancellationToken, async ct =>
        {
            var tilkoblet = await _db.Database.CanConnectAsync(ct);
            return tilkoblet
                ? (DriftsstatusLys.Gronn, "Tilkobling OK.")
                : (DriftsstatusLys.Rod, "Fikk IKKE kontakt med databasen.");
        });
    }

    private async Task<DriftsstatusSjekk> SjekkVippsAsync(CancellationToken cancellationToken)
    {
        var clientId = _configuration["Vipps:ClientId"];
        var clientSecret = _configuration["Vipps:ClientSecret"];
        var subscriptionKey = _configuration["Vipps:SubscriptionKey"];
        var merchantSerialNumber = _configuration["Vipps:MerchantSerialNumber"];
        var miljo = _configuration["Vipps:Miljo"] == "Produksjon" ? "Produksjon" : "Test";

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(subscriptionKey) || string.IsNullOrWhiteSpace(merchantSerialNumber))
        {
            return new DriftsstatusSjekk("Vipps", DriftsstatusLys.Graa, "Ingen Vipps-nøkler konfigurert i dette miljøet (mock brukes).", null);
        }

        return await KjorMedTimeoutAsync($"Vipps ({miljo})", cancellationToken, async ct =>
        {
            var baseUrl = miljo == "Produksjon" ? "https://api.vipps.no" : "https://apitest.vipps.no";
            var http = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/accesstoken/get");
            request.Headers.Add("client_id", clientId);
            request.Headers.Add("client_secret", clientSecret);
            request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            request.Headers.Add("Merchant-Serial-Number", merchantSerialNumber);

            using var response = await http.SendAsync(request, ct);
            return response.IsSuccessStatusCode
                ? (DriftsstatusLys.Gronn, "Fikk gyldig tilgangstoken fra Vipps.")
                : (DriftsstatusLys.Rod, $"Vipps svarte {(int)response.StatusCode} ved henting av tilgangstoken — nøkler kan være ugyldige/utløpt.");
        });
    }

    private async Task<DriftsstatusSjekk> SjekkStripeAsync(CancellationToken cancellationToken)
    {
        var secretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return new DriftsstatusSjekk("Stripe", DriftsstatusLys.Graa, "Ingen Stripe-nøkkel konfigurert i dette miljøet (mock brukes).", null);
        }

        var modus = secretKey.StartsWith("sk_live_", StringComparison.Ordinal) ? "Live" : "Test";
        return await KjorMedTimeoutAsync($"Stripe ({modus})", cancellationToken, async ct =>
        {
            var requestOptions = new RequestOptions { ApiKey = secretKey };
            var service = new BalanceService();
            var balanse = await service.GetAsync(requestOptions: requestOptions, cancellationToken: ct);
            return (DriftsstatusLys.Gronn, $"Nøkkel gyldig, {balanse.Available.Count} saldo-rad(er) hentet.");
        });
    }

    private async Task<DriftsstatusSjekk> SjekkSmsKredittAsync(CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Vonage:ApiKey"];
        var apiSecret = _configuration["Vonage:ApiSecret"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        {
            return new DriftsstatusSjekk("SMS-kreditt (Vonage)", DriftsstatusLys.Graa, "Ingen Vonage-nøkler konfigurert i dette miljøet (mock brukes).", null);
        }

        return await KjorMedTimeoutAsync("SMS-kreditt (Vonage)", cancellationToken, async ct =>
        {
            var http = _httpClientFactory.CreateClient();
            var url = $"https://rest.nexmo.com/account/get-balance?api_key={Uri.EscapeDataString(apiKey)}&api_secret={Uri.EscapeDataString(apiSecret)}";
            using var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                return (DriftsstatusLys.Rod, $"Vonage svarte {(int)response.StatusCode} ved saldo-oppslag.");
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var dokument = JsonDocument.Parse(json);
            var saldo = dokument.RootElement.TryGetProperty("value", out var verdi) ? verdi.GetDouble() : (double?)null;
            if (saldo is null)
            {
                return (DriftsstatusLys.Gul, "Klarte ikke å lese saldo fra Vonage sitt svar.");
            }

            // Grenseverdier er et rimelig utgangspunkt, ikke hentet fra noen avtale — juster ved behov.
            return saldo <= 0
                ? (DriftsstatusLys.Rod, $"Saldo er {saldo:0.00} EUR — SMS vil trolig feile.")
                : saldo < 5
                    ? (DriftsstatusLys.Gul, $"Saldo er {saldo:0.00} EUR — lavt, bør fylles på snart.")
                    : (DriftsstatusLys.Gronn, $"Saldo er {saldo:0.00} EUR.");
        });
    }

    private Task<DriftsstatusSjekk> SjekkEpostAsync(CancellationToken cancellationToken)
    {
        // Ingen billig/gratis live-sjekk tilgjengelig uten å faktisk sende en e-post (Azure
        // Communication Services har ingen enkel "ping"-endepunkt via den vanlige SDK-en) —
        // kun en konfigurasjonssjekk inntil videre, se klassekommentaren.
        var konfigurert = !string.IsNullOrWhiteSpace(_configuration["Acs:ConnectionString"]);
        var resultat = konfigurert
            ? new DriftsstatusSjekk("E-post (Azure Communication Services)", DriftsstatusLys.Gronn, "Konfigurert (kun konfigurasjonssjekk, ingen live-test).", null)
            : new DriftsstatusSjekk("E-post (Azure Communication Services)", DriftsstatusLys.Graa, "Ikke konfigurert i dette miljøet (mock brukes).", null);
        return Task.FromResult(resultat);
    }

    private async Task<DriftsstatusSjekk> SjekkBankIdAsync(CancellationToken cancellationToken)
    {
        var authority = _configuration["BankId:IduraProduksjon:Authority"];
        if (string.IsNullOrWhiteSpace(authority))
        {
            return new DriftsstatusSjekk("BankID (Idura, produksjon)", DriftsstatusLys.Graa, "Ikke konfigurert i dette miljøet.", null);
        }

        return await KjorMedTimeoutAsync("BankID (Idura, produksjon)", cancellationToken, async ct =>
        {
            var http = _httpClientFactory.CreateClient();
            using var response = await http.GetAsync($"{authority.TrimEnd('/')}/.well-known/openid-configuration", ct);
            return response.IsSuccessStatusCode
                ? (DriftsstatusLys.Gronn, "Idura sitt OIDC-discovery-dokument svarer normalt.")
                : (DriftsstatusLys.Rod, $"Idura svarte {(int)response.StatusCode} på discovery-dokumentet.");
        });
    }

    private async Task<DriftsstatusSjekk> SjekkUtbetalingerAsync(CancellationToken cancellationToken)
    {
        return await KjorMedTimeoutAsync("Stripe Connect-utbetalinger", cancellationToken, async ct =>
        {
            var feilede = await _db.UtbetalingsLinjer.CountAsync(l => l.Status == Utbetaling.UtbetalingsLinjeStatus.Feilet, ct);
            return feilede > 0
                ? (DriftsstatusLys.Rod, $"{feilede} utbetalingslinje(r) står som Feilet — se Admin/Utbetalinger.")
                : (DriftsstatusLys.Gronn, "Ingen feilede utbetalingslinjer.");
        });
    }

    private async Task<DriftsstatusSjekk> KjorMedTimeoutAsync(
        string navn, CancellationToken cancellationToken, Func<CancellationToken, Task<(DriftsstatusLys Lys, string Detalj)>> sjekk)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(SjekkTimeout);
        var klokke = Stopwatch.StartNew();

        try
        {
            var (lys, detalj) = await sjekk(timeoutCts.Token);
            klokke.Stop();
            return new DriftsstatusSjekk(navn, lys, detalj, klokke.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            klokke.Stop();
            return new DriftsstatusSjekk(navn, DriftsstatusLys.Rod, $"Tidsavbrudd (over {SjekkTimeout.TotalSeconds:0}s uten svar).", klokke.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            klokke.Stop();
            _logger.LogWarning(ex, "Driftsstatus-sjekk '{Navn}' feilet uventet.", navn);
            return new DriftsstatusSjekk(navn, DriftsstatusLys.Rod, $"Uventet feil: {ex.Message}", klokke.ElapsedMilliseconds);
        }
    }
}
