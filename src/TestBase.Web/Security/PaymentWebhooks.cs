using System.Security.Cryptography;
using System.Text;
using Stripe;

namespace TestBase.Web.Security;

/// <summary>
/// Webhook-mottakere for Vipps og Stripe. Registrert som rene minimal-API-
/// endepunkter (IKKE Razor Pages) med vilje — Razor Pages sin automatiske
/// antiforgery-validering på POST-handlere ville avvist disse, siden Vipps/
/// Stripe sine servere naturligvis ikke har vår antiforgery-cookie. Den
/// reelle sikkerheten her er signaturverifisering av selve forespørselen
/// (HMAC), ikke noe cookie-basert.
///
/// Disse stiene må også unntas StagingGate (se StagingGate.cs) av samme
/// grunn som BankID-callbacken: et server-til-server-kall fra Vipps/Stripe
/// har ingen mulighet til å sende vår StagingGate-cookie.
///
/// MERK: Vipps sin signaturverifisering under er implementert etter
/// Vipps sin offentlige dokumentasjon (kanonisk streng + HMAC-SHA256), men
/// er IKKE bekreftet mot et ekte, levende Vipps-webhook-kall ennå — Vipps sitt
/// sandkasse-miljø krever et godkjent partner-/kundeforhold (se
/// docs/beslutningslogg.md "Vipps + Stripe (Apple Pay/Google Pay)"). Stripe-
/// verifiseringen bruker derimot Stripe sin egen offisielle SDK-metode
/// (EventUtility.ConstructEvent) og kan verifiseres umiddelbart siden Stripe
/// sitt testmiljø er selvbetjent.
/// </summary>
public static class PaymentWebhooks
{
    public const string VippsWebhookSti = "/webhooks/vipps";
    public const string StripeWebhookSti = "/webhooks/stripe";

    public static void MapPaymentWebhooks(this WebApplication app)
    {
        var logger = app.Logger;
        app.MapPost(VippsWebhookSti, (HttpContext context, IConfiguration config) => HandleVippsWebhookAsync(context, config, logger));
        app.MapPost(StripeWebhookSti, (HttpContext context, IConfiguration config) => HandleStripeWebhookAsync(context, config, logger));
    }

    private static async Task<IResult> HandleVippsWebhookAsync(HttpContext context, IConfiguration config, ILogger logger)
    {
        var webhookSecret = config["Vipps:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            logger.LogWarning("Vipps-webhook mottatt, men Vipps:WebhookSecret er ikke konfigurert — avviser.");
            return Results.Unauthorized();
        }

        using var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms);
        var body = ms.ToArray();

        if (!ErVippsSignaturGyldig(context.Request, body, webhookSecret, out var feilmelding))
        {
            logger.LogWarning("Vipps-webhook med ugyldig signatur avvist: {Feilmelding}", feilmelding);
            return Results.Unauthorized();
        }

        var json = Encoding.UTF8.GetString(body);
        logger.LogInformation("Vipps-webhook mottatt og verifisert: {Json}", json);

        // TODO (når ekte "betal for test"-flyt designes, se beslutningsloggen):
        // oppdater ordre-/tildelingsstatus i databasen basert på hendelsen her.
        return Results.Ok();
    }

    private static bool ErVippsSignaturGyldig(HttpRequest request, byte[] body, string secret, out string feilmelding)
    {
        if (!request.Headers.TryGetValue("x-ms-date", out var datoVerdier) ||
            !request.Headers.TryGetValue("x-ms-content-sha256", out var innholdshashVerdier) ||
            !request.Headers.TryGetValue("Authorization", out var autorisasjonVerdier))
        {
            feilmelding = "Mangler en eller flere signatur-headere (x-ms-date/x-ms-content-sha256/Authorization).";
            return false;
        }

        var oppgittHash = innholdshashVerdier.ToString();
        var beregnetHash = Convert.ToBase64String(SHA256.HashData(body));
        if (!string.Equals(oppgittHash, beregnetHash, StringComparison.Ordinal))
        {
            feilmelding = "Innholds-hash (x-ms-content-sha256) stemmer ikke med faktisk request-body.";
            return false;
        }

        var autorisasjonHeader = autorisasjonVerdier.ToString();
        const string signaturPrefiks = "Signature=";
        var signaturIndeks = autorisasjonHeader.IndexOf(signaturPrefiks, StringComparison.Ordinal);
        if (signaturIndeks < 0)
        {
            feilmelding = "Fant ikke 'Signature=' i Authorization-headeren.";
            return false;
        }
        var oppgittSignatur = autorisasjonHeader[(signaturIndeks + signaturPrefiks.Length)..].Trim();

        var dato = datoVerdier.ToString();
        var host = request.Host.Value;
        var stiOgSporrestreng = request.Path + request.QueryString;
        var kanoniskStreng = $"{request.Method}\n{stiOgSporrestreng}\n{dato};{host};{oppgittHash}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var beregnetSignatur = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(kanoniskStreng)));

        var likhet = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(beregnetSignatur), Encoding.UTF8.GetBytes(oppgittSignatur));

        feilmelding = likhet ? string.Empty : "Beregnet HMAC-signatur stemmer ikke med oppgitt signatur.";
        return likhet;
    }

    private static Task<IResult> HandleStripeWebhookAsync(HttpContext context, IConfiguration config, ILogger logger) =>
        HandleStripeWebhookCoreAsync(context, config, logger);

    private static async Task<IResult> HandleStripeWebhookCoreAsync(HttpContext context, IConfiguration config, ILogger logger)
    {
        var webhookSecret = config["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            logger.LogWarning("Stripe-webhook mottatt, men Stripe:WebhookSecret er ikke konfigurert — avviser.");
            return Results.Unauthorized();
        }

        var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var signaturHeader = context.Request.Headers["Stripe-Signature"].ToString();

        try
        {
            var hendelse = EventUtility.ConstructEvent(json, signaturHeader, webhookSecret);
            logger.LogInformation("Stripe-webhook mottatt og verifisert: {Type} ({Id})", hendelse.Type, hendelse.Id);

            // TODO (når ekte "betal for test"-flyt designes, se beslutningsloggen):
            // oppdater ordre-/tildelingsstatus i databasen basert på hendelsen her.
            return Results.Ok();
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe-webhook med ugyldig signatur avvist.");
            return Results.Unauthorized();
        }
    }
}
