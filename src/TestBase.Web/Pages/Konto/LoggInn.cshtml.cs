using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
    private readonly ProfesjonellInnloggingService _profesjonellInnlogging;
    private readonly ICaptchaProvider _captcha;
    private readonly IAuditLogger _auditLogger;
    private readonly IConfiguration _configuration;
    private readonly IAuthenticationSchemeProvider _schemes;

    public LoggInnModel(
        AdminAuthenticationService adminAuth,
        BehandlerAuthenticationService behandlerAuth,
        ProfesjonellInnloggingService profesjonellInnlogging,
        ICaptchaProvider captcha,
        IAuditLogger auditLogger,
        IConfiguration configuration,
        IAuthenticationSchemeProvider schemes)
    {
        _adminAuth = adminAuth;
        _behandlerAuth = behandlerAuth;
        _profesjonellInnlogging = profesjonellInnlogging;
        _captcha = captcha;
        _auditLogger = auditLogger;
        _configuration = configuration;
        _schemes = schemes;
    }

    /// <summary>Styrer AdminId+passord-skjemaet (view og handler) — se Security/Miljo.cs.</summary>
    public bool VisUtviklingsSnarveier => Miljo.TillatUtviklingsSnarveier(_configuration);

    /// <summary>Styrer PersonnummerOverride-feltet (view og handler) — snevrere enn VisUtviklingsSnarveier over, se Security/Miljo.cs.</summary>
    public bool VisPersonnummerOverride => Miljo.TillatPersonnummerOverride(_configuration);

    /// <summary>
    /// KUN sant på beta (se Program.cs "BankIdInnlogging"-schema, registrert
    /// bare når Miljo:ErBeta og Idura-nøklene begge er satt) — når sant,
    /// bytter OnPostAsync sin BankID-knapp fra MockBankIdProvider til en ekte
    /// Idura-redirect. Se docs/beslutningslogg.md "Ekte BankID for
    /// admin/behandler (beta)".
    /// </summary>
    public async Task<bool> HarEktBankIdAsync() => await _schemes.GetSchemeAsync("BankIdInnlogging") is not null;

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

    /// <summary>Satt av BankIdFullfor.cshtml.cs når en ekte Idura-innlogging ikke fant noen matchende konto — se der.</summary>
    public void OnGet()
    {
        NyCaptcha();
        if (TempData["BankIdFeilmelding"] is string feil)
        {
            Feilmelding = feil;
        }
    }

    private void NyCaptcha()
    {
        // ModelState.Clear() er nødvendig FØR vi setter nye verdier under: asp-for
        // rendrer den POSTEDE ModelState-verdien fremfor den ferske C#-verdien når
        // samme side redirendres via Page() (ikke RedirectToPage) i samme forespørsel.
        // Uten denne linjen viser skjulte CaptchaSignertFasit-feltet fortsatt forrige
        // (nå utdaterte) svar mens CaptchaSporsmal-teksten på skjermen har oppdatert
        // seg — ETHVERT svar på det nye, synlige spørsmålet blir da avvist som "feil
        // sikkerhetskode", selv et matematisk korrekt et. Samme fallgruve som ble
        // funnet og fikset i slette-bekreftelsesflytene (bugliste 2026-09-13 gruppe B),
        // men den gangen ikke fanget opp i disse to innloggingssidene. Se
        // docs/beslutningslogg.md.
        ModelState.Clear();

        var utfordring = _captcha.LagUtfordring();
        CaptchaSporsmal = utfordring.SporsmalTekst;
        CaptchaSignertFasit = utfordring.SignertFasit;
        CaptchaSvar = null;
    }

    /// <summary>
    /// Primærflyt: BankID → høyeste rolle (administrator før behandler). På
    /// beta erstattes selve identitetsbekreftelsen med en ekte Idura-redirect
    /// (se HarEktBankIdAsync) — resten av flyten (oppslag/betrodd enhet/2FA)
    /// er FELLES uansett, se ProfesjonellInnloggingService og
    /// Pages/Konto/BankIdFullfor.cshtml.cs.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!_captcha.Verifiser(CaptchaSignertFasit, CaptchaSvar))
        {
            Feilmelding = "Feil svar på sikkerhetsspørsmålet.";
            NyCaptcha();
            return Page();
        }

        if (await HarEktBankIdAsync())
        {
            var props = new AuthenticationProperties { RedirectUri = "/Konto/BankIdFullfor" };
            props.Items["huskMeg"] = HuskMeg.ToString();
            props.Items["returnUrl"] = ReturnUrl;
            return Challenge(props, "BankIdInnlogging");
        }

        // Gates ved bruk, ikke bare i viewet — en rå POST kan sette denne uansett synlighet.
        var bankIdResultat = await _adminAuth.StartBankIdAsync(
            personnummerOverride: VisPersonnummerOverride ? PersonnummerOverride : null, cancellationToken: cancellationToken);
        if (!bankIdResultat.Success || bankIdResultat.PersonNummer is null)
        {
            Feilmelding = bankIdResultat.ErrorMessage ?? "BankID-innlogging feilet.";
            NyCaptcha();
            return Page();
        }

        return await FullforOgOversettAsync(bankIdResultat.PersonNummer, "BankID", cancellationToken);
    }

    /// <summary>Felles oversettelse fra ProfesjonellInnloggingResultat til denne sidens IActionResult/TempData — brukt av både OnPostAsync og BankIdFullfor.cshtml.cs.</summary>
    private async Task<IActionResult> FullforOgOversettAsync(string personnummer, string auditlogKilde, CancellationToken cancellationToken)
    {
        var resultat = await _profesjonellInnlogging.FullforAsync(personnummer, HuskMeg, ReturnUrl, auditlogKilde, HttpContext, cancellationToken);
        if (resultat.ErFerdig)
        {
            return resultat.FerdigResultat!;
        }

        if (resultat.TrengerToFaktorFlagg)
        {
            if (resultat.DevToFaktorKode is not null)
            {
                TempData["DevToFaktorKode"] = resultat.DevToFaktorKode;
            }
            TempData["ToFaktorRolle"] = resultat.ToFaktorRolle!.Value.ToString();
            TempData["ToFaktorId"] = resultat.ToFaktorId!.Value.ToString();
            TempData["ToFaktorHuskMeg"] = resultat.ToFaktorHuskMeg;
            TempData["ToFaktorReturnUrl"] = resultat.ToFaktorReturnUrl;
            return RedirectToPage("BekreftKode");
        }

        Feilmelding = resultat.Feilmelding;
        NyCaptcha();
        return Page();
    }

    /// <summary>
    /// Sekundærflyt: AdminId + passord (kun utviklingssnarveier, jf.
    /// AdminAuthenticationService.HarPassordPalogging). Gates ved bruk, ikke bare
    /// via skjemaets synlighet i viewet — se kjent fallgruve i CLAUDE.md. Samme
    /// feilmelding som "fant ingen konto" ved avslag her, for å ikke avsløre om
    /// mekanismen i det hele tatt finnes på et miljø der den er skrudd av.
    /// </summary>
    public async Task<IActionResult> OnPostPassordAsync(CancellationToken cancellationToken)
    {
        if (!VisUtviklingsSnarveier)
        {
            Feilmelding = "Fant ingen administrator med denne AdminId-en og passord.";
            NyCaptcha();
            return Page();
        }

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
