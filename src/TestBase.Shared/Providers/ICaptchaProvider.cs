namespace TestBase.Shared.Providers;

public sealed record CaptchaUtfordring(string SporsmalTekst, string SignertFasit);

/// <summary>
/// Grensesnitt mot CAPTCHA/bot-vern på offentlige innloggings- og
/// registreringsskjemaer. Ekte implementasjon (fremtidig leverandørvalg,
/// f.eks. hCaptcha eller Cloudflare Turnstile — se beslutningsloggen) kobles
/// mot en ekstern widget + serverside-verifisering. I dev/test brukes
/// MockCaptchaProvider, som løser utfordringen lokalt uten ekstern tjeneste
/// eller server-side sesjonstilstand (fasiten signeres med DataProtection og
/// sendes tur-retur i et skjult skjemafelt, samme mønster som ellers i
/// prosjektet unngår HttpContext-avhengighet i sikkerhetstjenester).
/// </summary>
public interface ICaptchaProvider
{
    CaptchaUtfordring LagUtfordring();

    bool Verifiser(string signertFasit, string? brukerSvar);
}
