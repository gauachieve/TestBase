using System.Security.Cryptography;
using System.Text;
using TestBase.Shared.Domain.Tilbakemeldinger;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Security;

/// <summary>
/// To ting samlet i én fil, begge minimal-API-endepunkter (IKKE Razor Pages, samme
/// begrunnelse som PaymentWebhooks.cs — ingen antiforgery-cookie å validere mot):
///
/// 1) <c>POST /api/tilbakemelding</c> — det offentlige innsendingsendepunktet widgeten
///    (wwwroot/js/tilbakemelding-widget.js) fetch()-er mot. Ingen [Authorize] — skal
///    virke for ALLE, innlogget eller ikke, på enhver side. Leser ICurrentUserContext
///    for å auto-fylle rolle/bruker-ID når avsenderen faktisk er innlogget.
///
/// 2) <c>/api/agent/*</c> — et lite, delt-nøkkel-beskyttet API (samme mønster som
///    StagingGate sin AccessKey) for den daglige rapport-agenten (se docs/beslutningslogg.md
///    "Tilbakemeldingsverktøy") som kjører UTENFOR appens egen innloggingscookie —
///    en ekte BankID-/passord-sesjon gir ingen mening for en autonom, planlagt jobb.
///    Aktiveres KUN når "Tilbakemelding:AgentNokkel" er satt (App Service-innstilling,
///    ALDRI i kildekontroll) — helt fraværende (404) ellers, samme "absent = av"-mønster
///    som resten av appens eksterne integrasjoner. MÅ unntas StagingGate på beta (se
///    StagingGate.cs) siden agenten ikke har noen nettleser-cookie å sende.
/// </summary>
public static class TilbakemeldingApi
{
    public const string InnsendingSti = "/api/tilbakemelding";
    public const string AgentDigestSti = "/api/agent/tilbakemeldinger";
    public const string AgentSkjermbildeSti = "/api/agent/tilbakemelding";
    public const string AgentRapportSti = "/api/agent/rapport";

    public sealed record InnsendingRequest(
        string Melding,
        string? Url,
        string? BrukerAgent,
        int? SkjermBredde,
        int? SkjermHoyde,
        int? VindaugBredde,
        int? VindaugHoyde,
        string? TekniskFeilInfo,
        string? ScreenshotDataUrl);

    public sealed record TilbakemeldingDigestPost(
        long Id, DateTimeOffset OpprettetUtc, string Melding, string? Url, string? BrukerAgent,
        int? SkjermBredde, int? SkjermHoyde, int? VindaugBredde, int? VindaugHoyde,
        string? InnloggetRolle, long? InnloggetBrukerId, string? TekniskFeilInfo, bool ErKrasjRapport,
        bool HarScreenshot, string Status, string? Notat);

    public sealed record RapportRequest(string Emne, string HtmlInnhold);

    public static void MapTilbakemeldingApi(this WebApplication app)
    {
        app.MapPost(InnsendingSti, HandleInnsendingAsync);
        app.MapGet(AgentDigestSti, HandleAgentDigestAsync);
        app.MapGet(AgentSkjermbildeSti + "/{id:long}/skjermbilde", HandleAgentSkjermbildeAsync);
        app.MapPost(AgentRapportSti, HandleAgentRapportAsync);
    }

    private static async Task<IResult> HandleInnsendingAsync(
        InnsendingRequest body, ICurrentUserContext currentUser, TilbakemeldingService service, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Melding))
        {
            return Results.BadRequest(new { feil = "Melding kan ikke være tom." });
        }

        long? brukerId = null;
        if (currentUser.IsAuthenticated)
        {
            var siste = currentUser.UserId.Split(':').LastOrDefault();
            if (long.TryParse(siste, out var id))
            {
                brukerId = id;
            }
        }

        var lagret = await service.RegistrerAsync(
            body.Melding,
            body.Url,
            body.BrukerAgent,
            body.SkjermBredde,
            body.SkjermHoyde,
            body.VindaugBredde,
            body.VindaugHoyde,
            currentUser.IsAuthenticated ? currentUser.Role.ToString() : null,
            brukerId,
            body.TekniskFeilInfo,
            body.ScreenshotDataUrl,
            ct);

        return Results.Ok(new { id = lagret.Id });
    }

    private static bool ErGyldigAgentNokkel(HttpContext context, IConfiguration config)
    {
        var forventet = config["Tilbakemelding:AgentNokkel"];
        if (string.IsNullOrEmpty(forventet))
        {
            return false;
        }

        var oppgitt = context.Request.Headers["X-Agent-Nokkel"].ToString();
        if (string.IsNullOrEmpty(oppgitt))
        {
            oppgitt = context.Request.Query["nokkel"].ToString();
        }
        if (string.IsNullOrEmpty(oppgitt))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(oppgitt), Encoding.UTF8.GetBytes(forventet));
    }

    private static async Task<IResult> HandleAgentDigestAsync(
        HttpContext context, IConfiguration config, TilbakemeldingService service, DateTimeOffset? siden, CancellationToken ct)
    {
        if (!ErGyldigAgentNokkel(context, config))
        {
            return Results.NotFound();
        }

        var fra = siden ?? DateTimeOffset.UtcNow.AddDays(-1);
        var liste = await service.HentSidenAsync(fra, ct);

        var resultat = liste.Select(t => new TilbakemeldingDigestPost(
            t.Id, t.OpprettetUtc, t.Melding, t.Url, t.BrukerAgent,
            t.SkjermBredde, t.SkjermHoyde, t.VindaugBredde, t.VindaugHoyde,
            t.InnloggetRolle, t.InnloggetBrukerId, t.TekniskFeilInfo, t.ErKrasjRapport,
            !string.IsNullOrEmpty(t.ScreenshotDataUrl), t.Status.ToString(), t.Notat));

        return Results.Ok(resultat);
    }

    private static async Task<IResult> HandleAgentSkjermbildeAsync(
        long id, HttpContext context, IConfiguration config, TilbakemeldingService service, CancellationToken ct)
    {
        if (!ErGyldigAgentNokkel(context, config))
        {
            return Results.NotFound();
        }

        var alle = await service.HentAlleAsync(ct);
        var funnet = alle.FirstOrDefault(t => t.Id == id);
        if (funnet?.ScreenshotDataUrl is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new { screenshotDataUrl = funnet.ScreenshotDataUrl });
    }

    private static async Task<IResult> HandleAgentRapportAsync(
        RapportRequest body, HttpContext context, IConfiguration config, IEmailSender emailSender, ILogger<TilbakemeldingService> logger, CancellationToken ct)
    {
        if (!ErGyldigAgentNokkel(context, config))
        {
            return Results.NotFound();
        }

        var mottaker = config["Tilbakemelding:RapportMottakerEpost"] ?? "gauteg@gmail.com";
        await emailSender.SendAsync(mottaker, body.Emne, body.HtmlInnhold, ct);
        logger.LogInformation("Daglig tilbakemeldingsrapport sendt til {Mottaker}.", mottaker);
        return Results.Ok();
    }
}
