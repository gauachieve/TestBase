using Microsoft.Extensions.Logging;

namespace TestBase.Shared.Providers;

/// <summary>
/// Ekte push-varsel via Pushover (pushover.net) sitt enkle REST-API. Appens eget "app token"
/// (identifiserer TestBase som avsender hos Pushover, delt for alle miljøer/mottakere) kommer fra
/// "Pushover:AppToken"-konfigurasjon. Mottakerens personlige "user key" lagres per miljø i
/// DriftsbryterInnstilling.KritiskVarslingPushoverBrukerKey (Admin/Driftsstatus) — samme mønster
/// som ISmsSender sitt mobilnummer-parameter, se docs/beslutningslogg.md "Driftsstatus-dashboard".
/// </summary>
public sealed class PushoverKritiskVarselSender : IKritiskVarselSender
{
    private readonly HttpClient _http;
    private readonly string _appToken;
    private readonly ILogger<PushoverKritiskVarselSender> _logger;

    public PushoverKritiskVarselSender(HttpClient http, string appToken, ILogger<PushoverKritiskVarselSender> logger)
    {
        _http = http;
        _appToken = appToken;
        _logger = logger;
    }

    public async Task SendAsync(string mottakerBrukerKey, string tittel, string melding, CancellationToken cancellationToken = default)
    {
        var innhold = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = _appToken,
            ["user"] = mottakerBrukerKey,
            ["title"] = tittel,
            ["message"] = melding,
            ["priority"] = "1"
        });

        using var response = await _http.PostAsync("https://api.pushover.net/1/messages.json", innhold, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Pushover-varsel til {BrukerKey} feilet (status {Status}): {Body}", mottakerBrukerKey, response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }
}
