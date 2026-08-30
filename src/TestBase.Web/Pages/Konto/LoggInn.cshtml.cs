using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.Konto;

/// <summary>
/// Samlet innlogging for administrator og behandler ("profesjonelle
/// brukere") — ÉN BankID-knapp, ingen rollevalg. Systemet finner personen
/// via personnummer og logger inn på høyeste tilgjengelige rolle
/// (administrator før behandler), i stedet for å be brukeren velge portal
/// selv. Pasient har egen inngang (se Areas/Pasientportal), siden pasienter
/// er en helt separat gruppe uten overlapp med profesjonelle brukere.
/// AdminId+passord (kun utviklingsmiljø) er et sekundært alternativ på samme
/// side, for å bevare dev-snarveien uten å blande den inn i hovedflyten.
/// </summary>
public sealed class LoggInnModel : PageModel
{
    private readonly AdminAuthenticationService _adminAuth;
    private readonly BehandlerAuthenticationService _behandlerAuth;
    private readonly ICaptchaProvider _captcha;
    private readonly IAuditLogger _auditLogger;
    private readonly IWebHostEnvironment _env;

    public LoggInnModel(
        AdminAuthenticationService adminAuth,
        BehandlerAuthenticationService behandlerAuth,
        ICaptchaProvider captcha,
        IAuditLogger auditLogger,
        IWebHostEnvironment env)
    {
        _adminAuth = adminAuth;
        _behandlerAuth = behandlerAuth;
        _captcha = captcha;
        _auditLogger = auditLogger;
        _env = env;
    }

    [BindProperty]
    public string? AdminId { get; set; }

    [BindProperty]
    public string? Passord { get; set; }

    [BindProperty]
    public bool HuskMeg { get; set; }

    /// <summary>Kun utviklingsmiljø — overstyrer MockBankIdProvider slik at man kan bytte mellom flere test-personer, se IBankIdProvider.</summary>
    [BindProperty]
    public string? PersonnummerOverride { get; set; }

    /// <summary>
    /// Hvor man skal videre etter vellykket innlogging — satt av
    /// Program.cs' OnRedirectToLogin når en beskyttet side ble forsøkt
    /// besøkt uten å være innlogget. Må rundtures via skjult felt i skjemaet
    /// (query string overlever ikke automatisk et POST), og valideres med
    /// Url.IsLocalUrl før bruk for å unngå open redirect.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string CaptchaSignertFasit { get; set; } = string.Empty;

    [BindProperty]
    public string? CaptchaSvar { get; set; }

    public string CaptchaSporsmal { get; private set; } = string.Empty;
    public string? Feilmelding { get; private set; }

    public void OnGet() => NyCaptcha();

    private void NyCaptcha()
    {
        var utfordring = _captcha.LagUtfordring();
        CaptchaSporsmal = utfordring.SporsmalTekst;
        CaptchaSignertFasit = utfordring.SignertFasit;
        CaptchaSvar = null;
    }

    /// <summary>Primærflyt: BankID → høyeste rolle (administrator før behandler).</summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!_captcha.Verifiser(CaptchaSignertFasit, CaptchaSvar))
        {
            Feilmelding = "Feil svar på sikkerhetsspørsmålet.";
            NyCaptcha();
            return Page();
        }

        var bankIdResultat = await _adminAuth.StartBankIdAsync(
            personnummerOverride: PersonnummerOverride, cancellationToken: cancellationToken);
        if (!bankIdResultat.Success || bankIdResultat.PersonNummer is null)
        {
            Feilmelding = bankIdResultat.ErrorMessage ?? "BankID-innlogging feilet.";
            NyCaptcha();
            return Page();
        }

        var administrator = await _adminAuth.FinnVedPersonnummerAsync(bankIdResultat.PersonNummer, cancellationToken);
        if (administrator is not null)
        {
            if (BetroddEnhet.ErBetrodd(HttpContext, ToFaktorPrincipalType.Administrator, administrator.Id))
            {
                await AuthSignIn.LoggInnAsync(HttpContext, "administrator", administrator.Id, administrator.FulltNavn, UserRole.Administrator, HuskMeg);
                await _auditLogger.LogAsync(
                    administrator.AdminId, nameof(UserRole.Administrator), "InnloggingOk",
                    nameof(Administrator), administrator.Id.ToString(), "BankID (betrodd enhet — 2FA hoppet over)", cancellationToken);
                return TilMaalEtterInnlogging("Admin", "/Administratorer/Index");
            }

            var kode = await _adminAuth.StartToFaktorAsync(administrator, cancellationToken);
            if (_env.IsDevelopment())
            {
                TempData["DevToFaktorKode"] = kode;
            }
            TempData["ToFaktorRolle"] = nameof(UserRole.Administrator);
            TempData["ToFaktorId"] = administrator.Id.ToString();
            TempData["ToFaktorHuskMeg"] = HuskMeg;
            TempData["ToFaktorReturnUrl"] = ReturnUrl;
            return RedirectToPage("BekreftKode");
        }

        var behandler = await _behandlerAuth.FinnVedPersonnummerAsync(bankIdResultat.PersonNummer, cancellationToken);
        if (behandler is not null)
        {
            switch (behandler.Status)
            {
                case BehandlerStatus.Invitert:
                    Feilmelding = "Du har ikke fullført registreringen ennå. Bruk invitasjonslenken du mottok på SMS/e-post.";
                    NyCaptcha();
                    return Page();
                case BehandlerStatus.Fryst:
                    Feilmelding = "Kontoen din er fryst. Kontakt administrator.";
                    NyCaptcha();
                    return Page();
                case BehandlerStatus.Arkivert:
                    Feilmelding = "Kontoen din er arkivert.";
                    NyCaptcha();
                    return Page();
            }

            if (BetroddEnhet.ErBetrodd(HttpContext, ToFaktorPrincipalType.Behandler, behandler.Id))
            {
                await AuthSignIn.LoggInnAsync(HttpContext, "behandler", behandler.Id, behandler.Visningsnavn ?? "Behandler", UserRole.Behandler, HuskMeg);
                await _auditLogger.LogAsync(
                    $"behandler:{behandler.Id}", nameof(UserRole.Behandler), "InnloggingOk",
                    nameof(Behandler), behandler.Id.ToString(), "BankID (betrodd enhet — 2FA hoppet over)", cancellationToken);

                if (behandler.BrukeravtaleGodkjentVersjon != Brukeravtale.GjeldendeVersjon)
                {
                    return RedirectToPage("/Konto/GodkjennAvtale", new { area = "Behandlerportal" });
                }
                return TilMaalEtterInnlogging("Behandlerportal", "/Pasienter/Index");
            }

            var kode = await _behandlerAuth.StartToFaktorAsync(behandler, cancellationToken);
            if (_env.IsDevelopment())
            {
                TempData["DevToFaktorKode"] = kode;
            }
            TempData["ToFaktorRolle"] = nameof(UserRole.Behandler);
            TempData["ToFaktorId"] = behandler.Id.ToString();
            TempData["ToFaktorHuskMeg"] = HuskMeg;
            TempData["ToFaktorReturnUrl"] = ReturnUrl;
            return RedirectToPage("BekreftKode");
        }

        Feilmelding = "Fant ingen administrator- eller behandlerkonto for denne BankID-personen.";
        NyCaptcha();
        return Page();
    }

    /// <summary>Sekundærflyt: AdminId + passord (kun utviklingsmiljø, jf. AdminAuthenticationService.HarPassordPalogging).</summary>
    public async Task<IActionResult> OnPostPassordAsync(CancellationToken cancellationToken)
    {
        if (!_captcha.Verifiser(CaptchaSignertFasit, CaptchaSvar))
        {
            Feilmelding = "Feil svar på sikkerhetsspørsmålet.";
            NyCaptcha();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(AdminId))
        {
            Feilmelding = "AdminId må fylles ut.";
            NyCaptcha();
            return Page();
        }

        var administrator = await _adminAuth.FinnVedAdminIdAsync(AdminId, cancellationToken);
        if (administrator is null || !AdminAuthenticationService.HarPassordPalogging(administrator))
        {
            Feilmelding = "Fant ingen administrator med denne AdminId-en og passord.";
            NyCaptcha();
            return Page();
        }

        var resultat = _adminAuth.VerifiserPassord(administrator, Passord ?? string.Empty);
        if (resultat == PasswordVerificationResult.Failed)
        {
            Feilmelding = "Feil passord.";
            await _auditLogger.LogAsync(
                administrator.AdminId, nameof(UserRole.Utvikler), "InnloggingFeilet",
                nameof(Administrator), administrator.Id.ToString(), "Feil passord", cancellationToken);
            NyCaptcha();
            return Page();
        }

        await AuthSignIn.LoggInnAsync(HttpContext, "administrator", administrator.Id, administrator.FulltNavn, UserRole.Utvikler, HuskMeg);
        await _auditLogger.LogAsync(
            administrator.AdminId, nameof(UserRole.Utvikler), "InnloggingOk",
            nameof(Administrator), administrator.Id.ToString(), "Passord (utviklingsmodus)", cancellationToken);

        return TilMaalEtterInnlogging("Admin", "/Administratorer/Index");
    }

    private IActionResult TilMaalEtterInnlogging(string fallbackArea, string fallbackPage) =>
        !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage(fallbackPage, new { area = fallbackArea });
}
