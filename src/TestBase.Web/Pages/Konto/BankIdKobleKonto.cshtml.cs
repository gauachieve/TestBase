using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Providers;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.Konto;

/// <summary>
/// Vises ÉN gang per administrator-/behandlerkonto, første gang en ekte BankID
/// (Idura)-identitet logger inn og ikke er koblet til noen konto ennå — se
/// ProfesjonellInnloggingService.FullforMedBankIdSubjektAsync/KoblOgFullforAsync
/// og docs/beslutningslogg.md "Tilbakemeldingsverktøy, del 3"/ekte BankID-
/// aktivering. Foretaket er KUN godkjent for openid+profile (ikke
/// nnin/nnin_altsub), så BankID gir oss aldri personnummeret — brukeren
/// bekrefter det selv HER, ÉN gang, og den (ikke-krypterte) BankID-"sub"-en
/// kobles permanent til kontoen slik at dette aldri trengs igjen.
/// </summary>
public sealed class BankIdKobleKontoModel : PageModel
{
    private readonly ProfesjonellInnloggingService _profesjonellInnlogging;
    private readonly ICaptchaProvider _captcha;

    public BankIdKobleKontoModel(ProfesjonellInnloggingService profesjonellInnlogging, ICaptchaProvider captcha)
    {
        _profesjonellInnlogging = profesjonellInnlogging;
        _captcha = captcha;
    }

    [BindProperty]
    public string BankIdSubjekt { get; set; } = string.Empty;

    [BindProperty]
    public bool HuskMeg { get; set; }

    [BindProperty]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string? Personnummer { get; set; }

    [BindProperty]
    public string CaptchaSignertFasit { get; set; } = string.Empty;

    [BindProperty]
    public string? CaptchaSvar { get; set; }

    public string CaptchaSporsmal { get; private set; } = string.Empty;
    public string? Feilmelding { get; private set; }

    /// <summary>
    /// Leser BankID-identiteten fra TempData (satt av BankIdFullfor.cshtml.cs) KUN her på GET,
    /// og render den videre som skjulte felt i skjemaet — TempData overlever ikke pålitelig
    /// forbi denne ene lesingen (se kjent TempData-fallgruve i CLAUDE.md), så selve POST-en
    /// må bære verdiene videre selv, akkurat som ReturnUrl på Pages/Konto/LoggInn.
    /// </summary>
    public IActionResult OnGet()
    {
        if (TempData["KoblBankIdSubjekt"] is not string bankIdSubjekt)
        {
            TempData["BankIdFeilmelding"] = "Fant ingen BankID-identitet å koble. Prøv å logge inn på nytt.";
            return RedirectToPage("LoggInn");
        }

        BankIdSubjekt = bankIdSubjekt;
        HuskMeg = TempData["KoblHuskMeg"] as string == bool.TrueString;
        ReturnUrl = TempData["KoblReturnUrl"] as string;
        NyCaptcha();
        return Page();
    }

    private void NyCaptcha()
    {
        // Se tilsvarende kommentar i Pages/Konto/LoggInn.cshtml.cs — samme fallgruve, samme fiks.
        ModelState.Clear();
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

        if (string.IsNullOrWhiteSpace(Personnummer))
        {
            Feilmelding = "Du må oppgi personnummeret du er registrert med.";
            NyCaptcha();
            return Page();
        }

        var resultat = await _profesjonellInnlogging.KoblOgFullforAsync(
            BankIdSubjekt, Personnummer.Trim(), HuskMeg, ReturnUrl, "Ekte BankID (Idura) — førstegangskobling", HttpContext, cancellationToken);

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
            TempData["ToFaktorSmsFeilet"] = resultat.ToFaktorSmsFeilet;
            return RedirectToPage("BekreftKode");
        }

        Feilmelding = resultat.Feilmelding;
        NyCaptcha();
        return Page();
    }
}
