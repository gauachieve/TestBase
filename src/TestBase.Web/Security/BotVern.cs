namespace TestBase.Web.Security;

/// <summary>
/// Enkelt bot-/spam-vern for offentlige, uautentiserte skjemaer (jf.
/// kravdokumentets Del 3: "system for å stoppe botter og spammere"). To
/// heuristikker, ingen ekte CAPTCHA-tjeneste: et skjult honeypot-felt (botter
/// fyller ofte ut alle felt) og et minstekrav til tid fra skjemaet ble vist
/// til det ble sendt inn (scriptede innsendinger er ofte umiddelbare). Reell
/// CAPTCHA (hCaptcha/Turnstile) er en fremtidig leverandørbeslutning på linje
/// med BankID/Vipps — se beslutningsloggen.
/// </summary>
public static class BotVern
{
    private static readonly TimeSpan MinimumsTid = TimeSpan.FromSeconds(2);

    public static string NyttVisningstidspunkt() => DateTimeOffset.UtcNow.ToString("O");

    public static bool ErSannsynligvisBot(string? honeypotVerdi, string? visningstidspunkt)
    {
        if (!string.IsNullOrEmpty(honeypotVerdi))
        {
            return true;
        }

        if (!DateTimeOffset.TryParse(visningstidspunkt, out var vist))
        {
            return true;
        }

        return DateTimeOffset.UtcNow - vist < MinimumsTid;
    }
}
