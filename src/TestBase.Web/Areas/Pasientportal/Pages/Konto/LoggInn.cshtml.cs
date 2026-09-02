using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages.Konto;

public sealed class LoggInnModel : PageModel
{
    private readonly PasientAuthenticationService _authService;
    private readonly ICaptchaProvider _captcha;
    private readonly IAuditLogger _auditLogger;
    private readonly IWebHostEnvironment _env;

    public LoggInnModel(PasientAuthenticationService authService, ICaptchaProvider captcha, IAuditLogger auditLogger, IWebHostEnvironment env)
    {
        _authService = authService;
        _captcha = captcha;
        _auditLogger = auditLogger;
        _env = env;
    }

    [BindProperty]
    public bool HuskMeg { get; set; }

    /// <summary>Kun utviklingsmiljø — overstyrer MockBankIdProvider slik at man kan bytte mellom flere test-pasienter, se IBankIdProvider.</summary>
    [BindProperty]
    public string? PersonnummerOverride { get; set; }

    /// <summary>
    /// Hvor man skal videre etter vellykket innlogging. Satt av Program.cs'
    /// OnRedirectToLogin når en beskyttet side (f.eks. en tildelt test) ble
    /// forsøkt besøkt uten å være innlogget — se InnloggingsstiForAsync, som
    /// også slår opp riktig personnummer for akkurat DEN tildelingen (kun i
    /// Development) og sender det med som forhåndsutfylling nedenfor, slik at
    /// en pasient som klikker lenken sin i en SMS/e-post ikke selv må vite
    /// hvilket (mock-)personnummer hen skal logge inn med.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Personnummer { get; set; }

    [BindProperty]
    public string CaptchaSignertFasit { get; set; } = string.Empty;

    [BindProperty]
    public string? CaptchaSvar { get; set; }

    public string CaptchaSporsmal { get; private set; } = string.Empty;
    public string? Feilmelding { get; private set; }

    public void OnGet()
    {
        if (_env.IsDevelopment() && !string.IsNullOrWhiteSpace(Personnummer))
        {
            PersonnummerOverride = Personnummer;
        }

        NyCaptcha();
    }

    private void NyCaptcha()
    {
        var utfordring = _captcha.LagUtfordring();
        CaptchaSporsmal = utfordring.SporsmalTekst;
        CaptchaSignertFasit = utfordring.SignertFasit;
        CaptchaSvar = null;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!_captcha.Verifiser(CaptchaSignertFasit, CaptchaSvar))
        {
            Feilmelding = "Feil svar på sikkerhetsspørsmålet.";
            NyCaptcha();
            return Page();
        }

        // Gates ved bruk, ikke bare i viewet — en rå POST kan sette denne uansett synlighet.
        var bankIdResultat = await _authService.StartBankIdAsync(
            personnummerOverride: _env.IsDevelopment() ? PersonnummerOverride : null, cancellationToken: cancellationToken);
        if (!bankIdResultat.Success || bankIdResultat.PersonNummer is null)
        {
            Feilmelding = bankIdResultat.ErrorMessage ?? "BankID-innlogging feilet.";
            NyCaptcha();
            return Page();
        }

        var pasient = await _authService.FinnVedPersonnummerAsync(bankIdResultat.PersonNummer, cancellationToken);
        if (pasient is null)
        {
            Feilmelding = "Fant ingen pasientkonto for denne BankID-personen. Be behandleren din om en invitasjon.";
            NyCaptcha();
            return Page();
        }

        switch (pasient.Status)
        {
            case PasientStatus.Invitert:
                Feilmelding = "Du har ikke fullført registreringen ennå. Bruk lenken du mottok på SMS/e-post.";
                NyCaptcha();
                return Page();
            case PasientStatus.Arkivert:
                Feilmelding = "Kontoen din er arkivert. Kontakt behandleren din.";
                NyCaptcha();
                return Page();
        }

        await AuthSignIn.LoggInnAsync(HttpContext, "pasient", pasient.Id, pasient.Navn ?? "Pasient", UserRole.Pasient, HuskMeg);
        await _auditLogger.LogAsync(
            $"pasient:{pasient.Id}", nameof(UserRole.Pasient), "InnloggingOk",
            nameof(Pasient), pasient.Id.ToString(), "BankID", cancellationToken);

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/MinSide", new { area = "Pasientportal" });
    }
}
