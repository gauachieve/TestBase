using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using TestBase.Shared.Security;

namespace TestBase.Web.Security;

/// <summary>
/// "Husk denne enheten"-cookie for BankID+SMS-2FA (administrator/behandler,
/// jf. bruker-tilbakemelding: "bank id, no sms — then for some time"): etter
/// en vellykket 2FA-verifisering markeres nettleseren som betrodd for akkurat
/// DEN kontoen i et konfigurerbart antall dager (config "Auth:BetroddEnhetDager",
/// standard 30, se Pages/Konto/BekreftKode.cshtml.cs). En påfølgende
/// BankID-innlogging fra samme nettleser for SAMME konto hopper da over
/// SMS-steget helt (se Pages/Konto/LoggInn.cshtml.cs), inntil cookien utløper
/// — deretter kreves SMS-koden igjen. Bruker en tidsbegrenset
/// DataProtection-nøkkel (ikke bare en vanlig cookie-verdi) slik at både
/// forfalskning og utløp håndheves kryptografisk. Egen cookie per
/// prinsipaltype (Administrator/Behandler) siden samme nettleser i dev ofte
/// brukes til å teste begge roller (se Admin/Konto/ByttModus).
/// </summary>
public static class BetroddEnhet
{
    private const string BeskyttelsesFormaal = "TestBase.BetroddEnhet.v1";

    public static void Marker(HttpContext httpContext, ToFaktorPrincipalType type, long principalId, TimeSpan levetid)
    {
        var beskytter = HentBeskytter(httpContext).ToTimeLimitedDataProtector();
        var verdi = beskytter.Protect(principalId.ToString(), levetid);

        httpContext.Response.Cookies.Append(CookieNavn(type), verdi, new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(levetid)
        });
    }

    public static bool ErBetrodd(HttpContext httpContext, ToFaktorPrincipalType type, long principalId)
    {
        if (!httpContext.Request.Cookies.TryGetValue(CookieNavn(type), out var verdi) || string.IsNullOrEmpty(verdi))
        {
            return false;
        }

        try
        {
            var beskytter = HentBeskytter(httpContext).ToTimeLimitedDataProtector();
            return beskytter.Unprotect(verdi) == principalId.ToString();
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static string CookieNavn(ToFaktorPrincipalType type) => $"testbase_betrodd_{type.ToString().ToLowerInvariant()}";

    private static IDataProtector HentBeskytter(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector(BeskyttelsesFormaal);
}
