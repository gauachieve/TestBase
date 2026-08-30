using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace TestBase.Shared.Providers.Mock;

/// <summary>
/// Løser CAPTCHA-utfordringen med et enkelt regnestykke, uten å kalle noen
/// ekstern tjeneste — KUN til bruk i lokalt utviklingsmiljø. Fasiten
/// signeres med DataProtection (samme mekanisme som krypterer personnummer,
/// se AppDbContext) slik at den kan sendes tur-retur i et skjult skjemafelt
/// uten server-side sesjonstilstand, og uten at brukeren kan forfalske svaret
/// ved å lese skjemaet.
/// </summary>
public sealed class MockCaptchaProvider : ICaptchaProvider
{
    private readonly IDataProtector _beskytter;

    public MockCaptchaProvider(IDataProtectionProvider dataProtectionProvider)
    {
        _beskytter = dataProtectionProvider.CreateProtector("TestBase.Captcha.v1");
    }

    public CaptchaUtfordring LagUtfordring()
    {
        var a = RandomNumberGenerator.GetInt32(1, 10);
        var b = RandomNumberGenerator.GetInt32(1, 10);
        var fasit = (a + b).ToString();

        return new CaptchaUtfordring($"Sikkerhetsspørsmål: hva er {a} + {b}?", _beskytter.Protect(fasit));
    }

    public bool Verifiser(string signertFasit, string? brukerSvar)
    {
        if (string.IsNullOrWhiteSpace(brukerSvar))
        {
            return false;
        }

        try
        {
            return _beskytter.Unprotect(signertFasit) == brukerSvar.Trim();
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
