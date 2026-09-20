using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace TestBase.Web.Security;

/// <summary>
/// Enkel tilgangssperre foran HELE appen, til bruk i offentlig nåbare
/// testmiljøer der IP-allowlisting er upraktisk (f.eks. mobil-/nettbrett-
/// testing med skiftende IP). Uten denne måtte et slikt testmiljø enten stå
/// åpent for hele internett — noe som fikk test-App Service-en phishing-
/// flagget av Chrome/Google Safe Browsing pga. BankID-lignende innloggingstekst
/// kombinert med et reelt auth-bypass (se beslutningsloggens "Google Chrome/
/// Safe Browsing flagget test-appen" og "PersonnummerOverride er et ubetinget
/// auth-bypass") — eller stole på en stadig voksende IP-allowliste.
///
/// Aktiveres KUN når konfigurasjonsnøkkelen "StagingGate:AccessKey" er satt
/// (en App Service-innstilling i Azure, ALDRI satt lokalt eller i reell
/// produksjon) — helt fraværende/no-op ellers, så lokal utvikling og en
/// fremtidig ekte produksjonssetting påvirkes ikke.
/// </summary>
public static class StagingGate
{
    private const string CookieNavn = ".TestBase.StagingGate";
    private const string FormFeltNavn = "tilgangsnokkel";

    // OIDC-callbacken for BankID-testintegrasjonen (Program.cs, CallbackPath) svarer med en
    // cross-site POST fra Idura sitt domene (response_mode=form_post) — StagingGate-cookien er
    // SameSite=Lax og blir da IKKE sendt av nettleseren, så disse to faste stiene må unntas fra
    // sperren (den andre er "BankIdInnlogging"-schemaet, ekte admin/behandler-pålogging på beta —
    // se Program.cs). Trygt: stiene er hardkodet (ingen wildcard), og selve OIDC-håndteringen
    // validerer state/nonce/PKCE uansett — en vilkårlig POST hit uten en ekte
    // Idura-autorisasjonskode gir ingenting.
    private static readonly string[] BankIdCallbackStier = ["/signin-bankid-test", "/signin-bankid-innlogging"];

    // QR-basert pasientselvregistrering (se Pages/BliPasient og
    // docs/beslutningslogg.md "Invitasjons- og gruppesystem, fase 2") MÅ nås
    // av en fullstendig ukjent besøker — hele poenget er at noen skanner en
    // kode uten noen forhåndstilgang. Samme resonnement som BankID-callbacken:
    // stien er hardkodet (ingen wildcard), og den reelle sikkerheten ligger i
    // selve siden (gyldig behandler-/gruppe-QR-token kreves, bot-vern, og
    // resultatet er uansett bare en ny, uverifisert pasientrad hos ÉN bestemt
    // behandler).
    //
    // OPPDAGET SAMTIDIG (2026-09-20), IKKE unntatt her ennå — flagget til
    // bruker i samtalen i stedet for stille utvidet: StagingGate sperrer i
    // praksis HELE appen for enhver besøker uten cookien fra før, inkludert
    // /PasientRegistrering/Fullfor, /Inviter/Fullfor, /Inviter/Verifiser og
    // /Pasientportal/Konto/LoggInn — altså ville en ekte invitert pasient
    // eller behandler på LIVE i dag møtt "skriv inn nøkkelen"-veggen FØR de
    // noensinne når sin egen invitasjonslenke. Dette er ikke noe denne
    // omgangen introduserer; det gjelder allerede eksisterende offentlige
    // sider, og bør vurderes helhetlig, ikke side for side.
    private static readonly string[] OffentligeSelvregistreringsStier = ["/BliPasient"];

    // Samme resonnement som BankIdCallbackSti, men for server-til-server-kall i
    // stedet for en nettleser-redirect: Vipps/Stripe sine servere har ingen
    // mulighet til å sende vår StagingGate-cookie i det hele tatt. Den reelle
    // sikkerheten er HMAC-signaturverifisering inni selve handleren, se
    // Security/PaymentWebhooks.cs.
    private static readonly string[] BetalingsWebhookStier =
    [
        PaymentWebhooks.VippsWebhookSti,
        PaymentWebhooks.StripeWebhookSti
    ];

    public static void UseStagingGate(this WebApplication app)
    {
        var tilgangsnokkel = app.Configuration["StagingGate:AccessKey"];
        if (string.IsNullOrEmpty(tilgangsnokkel))
        {
            return;
        }

        var beskytter = app.Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("TestBase.StagingGate.v1");

        // Midlertidig HTTP Basic Auth-vei i TILLEGG til nøkkel-skjemaet under — kun for
        // automatiserte tredjeparts nettsted-verifiseringer (f.eks. Vipps sin
        // merchant-registrering "Verifiser nettstedet", som ber om brukernavn+passord,
        // ikke vårt egendefinerte nøkkelfelt). Aktiveres KUN når BEGGE
        // StagingGate:BasicAuthUsername/BasicAuthPassword er satt — fravær = av, samme
        // mønster som resten av StagingGate. Fjern konfigurasjonen igjen når
        // verifiseringen er fullført (se docs/beslutningslogg.md) — mens den er aktiv,
        // ser ALLE besøkende uten cookie en nettleser-native Basic Auth-dialog i
        // stedet for vår egen HTML-side, fordi WWW-Authenticate-headeren trigger det
        // uansett innhold i responsen.
        var basicAuthBrukernavn = app.Configuration["StagingGate:BasicAuthUsername"];
        var basicAuthPassord = app.Configuration["StagingGate:BasicAuthPassword"];
        // IsNullOrWhiteSpace, ikke IsNullOrEmpty: Key Vault-plassholderen for "ikke satt"
        // er ett mellomrom (" "), ikke tom streng — se mønsteret i infra/resources.bicep.
        var basicAuthAktiv = !string.IsNullOrWhiteSpace(basicAuthBrukernavn) && !string.IsNullOrWhiteSpace(basicAuthPassord);

        app.Use(async (context, next) =>
        {
            if (BankIdCallbackStier.Any(sti => context.Request.Path.StartsWithSegments(sti)) ||
                BetalingsWebhookStier.Any(sti => context.Request.Path.StartsWithSegments(sti)) ||
                OffentligeSelvregistreringsStier.Any(sti => context.Request.Path.StartsWithSegments(sti)) ||
                HarGyldigCookie(context, beskytter) ||
                (basicAuthAktiv && HarGyldigBasicAuth(context, basicAuthBrukernavn!, basicAuthPassord!)))
            {
                await next();
                return;
            }

            if (HttpMethods.IsPost(context.Request.Method) &&
                context.Request.HasFormContentType &&
                context.Request.Form[FormFeltNavn] == tilgangsnokkel)
            {
                context.Response.Cookies.Append(CookieNavn, beskytter.Protect("ok"), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(90)
                });
                context.Response.Redirect(context.Request.Path + context.Request.QueryString);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            if (basicAuthAktiv)
            {
                context.Response.Headers.WWWAuthenticate = "Basic realm=\"TestBase\"";
            }
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync($$"""
                <!doctype html>
                <html lang="no"><head><meta charset="utf-8" /><title>Testmiljø — tilgangssperre</title></head>
                <body style="font-family: sans-serif; max-width: 30rem; margin: 4rem auto;">
                    <h1>Testmiljø — tilgangssperre</h1>
                    <p>Dette er et internt testmiljø, ikke offentlig tilgjengelig. Skriv inn nøkkelen for å fortsette.</p>
                    <form method="post">
                        <input type="password" name="{{FormFeltNavn}}" autofocus />
                        <button type="submit">Fortsett</button>
                    </form>
                </body></html>
                """);
        });
    }

    private static bool HarGyldigBasicAuth(HttpContext context, string brukernavn, string passord)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var dekodet = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..]));
            var delt = dekodet.Split(':', 2);
            if (delt.Length != 2)
            {
                return false;
            }

            var brukernavnOk = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(delt[0]), Encoding.UTF8.GetBytes(brukernavn));
            var passordOk = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(delt[1]), Encoding.UTF8.GetBytes(passord));
            return brukernavnOk && passordOk;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool HarGyldigCookie(HttpContext context, IDataProtector beskytter)
    {
        var cookieVerdi = context.Request.Cookies[CookieNavn];
        if (string.IsNullOrEmpty(cookieVerdi))
        {
            return false;
        }

        try
        {
            return beskytter.Unprotect(cookieVerdi) == "ok";
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
