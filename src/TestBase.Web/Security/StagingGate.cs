using System.Security.Cryptography;
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
    // SameSite=Lax og blir da IKKE sendt av nettleseren, så denne ene, faste stien må unntas fra
    // sperren. Trygt: stien er hardkodet (ingen wildcard), og selve OIDC-håndteringen validerer
    // state/nonce/PKCE uansett — en vilkårlig POST hit uten en ekte Idura-autorisasjonskode gir
    // ingenting.
    private const string BankIdCallbackSti = "/signin-bankid-test";

    public static void UseStagingGate(this WebApplication app)
    {
        var tilgangsnokkel = app.Configuration["StagingGate:AccessKey"];
        if (string.IsNullOrEmpty(tilgangsnokkel))
        {
            return;
        }

        var beskytter = app.Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("TestBase.StagingGate.v1");

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(BankIdCallbackSti) || HarGyldigCookie(context, beskytter))
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
