using System.Net;
using System.Text.RegularExpressions;

namespace TestBase.IntegrationTests.Infrastructure;

/// <summary>
/// Enkle hjelpemetoder for å drive Razor Pages-skjemaer i tester: hente ut
/// antiforgery-token fra en gjengitt side og poste skjemaer, uten å ta inn en
/// full HTML-parser-avhengighet (regex er tilstrekkelig for de faste
/// mønstrene ASP.NET Core genererer). Speiler mønsteret brukt i manuell
/// curl-basert verifisering under utvikling av fase 2–4.
/// </summary>
public static partial class SkjemaHjelper
{
    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"")]
    private static partial Regex TokenRegex();

    // Unngår "ø" i selve mønsteret (se LesHtmlAsync sin kommentar om
    // HTML-entities for norske bokstaver) — matcher kun den ASCII-trygge
    // delen av MockCaptchaProvider sitt spørsmål, "hva er X + Y?".
    [GeneratedRegex(@"hva er (\d+) \+ (\d+)\?")]
    private static partial Regex CaptchaSporsmalRegex();

    [GeneratedRegex("name=\"CaptchaSignertFasit\"[^>]*value=\"([^\"]*)\"")]
    private static partial Regex CaptchaSignertFasitRegex();

    /// <summary>
    /// Henter FØRSTE antiforgery-token på siden. På sider med flere skjemaer
    /// (f.eks. én rad per tabellrad) er alle tokenene identiske for samme
    /// respons, så "første" er alltid trygt — se kjente fallgruver i
    /// CLAUDE.md om hvorfor man ALDRI skal fange opp flere linjer i én variabel.
    /// </summary>
    public static string HentToken(string html)
    {
        var match = TokenRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException("Fant ikke __RequestVerificationToken i responsen.");
        }

        return match.Groups[1].Value;
    }

    public static async Task<string> HentTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        return HentToken(html);
    }

    /// <summary>GET + HTML-dekoding — se LesHtmlAsync for hvorfor dette trengs for tekst-assertions.</summary>
    public static async Task<string> GetHtmlAsync(HttpClient client, string url) =>
        WebUtility.HtmlDecode(await client.GetStringAsync(url));

    /// <summary>Henter token fra <paramref name="url"/> og poster <paramref name="felter"/> + token dit.</summary>
    public static async Task<HttpResponseMessage> PostSkjemaAsync(
        HttpClient client, string url, IEnumerable<KeyValuePair<string, string>> felter)
    {
        var token = await HentTokenAsync(client, url);
        return await PostMedTokenAsync(client, url, felter, token);
    }

    /// <summary>Poster <paramref name="felter"/> + et allerede kjent token — for flerstegsskjemaer hvor forrige respons ga tokenet.</summary>
    public static Task<HttpResponseMessage> PostMedTokenAsync(
        HttpClient client, string url, IEnumerable<KeyValuePair<string, string>> felter, string token)
    {
        var innhold = felter.ToList();
        innhold.Add(new KeyValuePair<string, string>("__RequestVerificationToken", token));
        return client.PostAsync(url, new FormUrlEncodedContent(innhold));
    }

    public static Dictionary<string, string> Felter(params (string Navn, string Verdi)[] par) =>
        par.ToDictionary(p => p.Navn, p => p.Verdi);

    /// <summary>
    /// Løser MockCaptchaProvider sin regnestykke-utfordring ved å lese
    /// spørsmålsteksten og den signerte fasiten direkte fra HTML-en — samme
    /// prinsipp som et menneske ville brukt, bare uten øynene.
    /// </summary>
    public static (string Svar, string SignertFasit) LosCaptcha(string html)
    {
        var sporsmal = CaptchaSporsmalRegex().Match(html);
        if (!sporsmal.Success)
        {
            throw new InvalidOperationException("Fant ikke CAPTCHA-spørsmål i responsen.");
        }

        var fasit = CaptchaSignertFasitRegex().Match(html);
        if (!fasit.Success)
        {
            throw new InvalidOperationException("Fant ikke CaptchaSignertFasit i responsen.");
        }

        var svar = int.Parse(sporsmal.Groups[1].Value) + int.Parse(sporsmal.Groups[2].Value);
        return (svar.ToString(), fasit.Groups[1].Value);
    }

    /// <summary>
    /// GET av <paramref name="url"/>, og returnerer HTML + antiforgery-token + løst
    /// CAPTCHA i ett. HTML-dekodes (se LesHtmlAsync sin kommentar) — Razors
    /// standard HTML-enkoder skriver om CAPTCHA-spørsmålets "+" til "&amp;#x2B;",
    /// som ellers ville gjort regex-matchingen i LosCaptcha usynlig treffsikker.
    /// </summary>
    public static async Task<(string Html, string Token, string CaptchaSvar, string CaptchaSignertFasit)> LastInnloggingsskjemaAsync(
        HttpClient client, string url)
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync(url));
        var (captchaSvar, captchaSignertFasit) = LosCaptcha(html);
        return (html, HentToken(html), captchaSvar, captchaSignertFasit);
    }

    /// <summary>
    /// Leser responsen som tekst OG HTML-dekoder den (Razor koder norske
    /// bokstaver som &amp;#xF8; osv. i output). Bruk denne — IKKE
    /// <c>response.Content.ReadAsStringAsync()</c> direkte — når du skal
    /// gjøre Assert.Contains på menneskelesbar tekst med æ/ø/å. Fant vi
    /// under bygging av dette testverktøyet: "fullført" i responsen var
    /// faktisk "fullf&amp;#xF8;rt" på rå byte-nivå, som gjorde et tilsynelatende
    /// korrekt Assert.Contains-kall til en falsk negativ.
    /// </summary>
    public static async Task<string> LesHtmlAsync(HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
}
