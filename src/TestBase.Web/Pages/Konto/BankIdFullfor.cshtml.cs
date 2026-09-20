using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.Konto;

/// <summary>
/// Landingsside etter en EKTE Idura BankID-innlogging for admin/behandler
/// (KUN aktiv på beta, se Program.cs "BankIdInnlogging"-schema sin
/// OnTokenValidated, som legger personnummer/huskMeg/returnUrl i TempData og
/// redirecter hit i stedet for å fullføre selve p.NET-cookien direkte — vi
/// skal først gjennom SAMME oppslag/betrodd enhet/2FA-logikk som
/// MockBankIdProvider-veien, se ProfesjonellInnloggingService). Rendrer
/// aldri noe UI selv i normalfallet — kun en teknisk mellomstasjon.
/// </summary>
public sealed class BankIdFullforModel : PageModel
{
    private readonly ProfesjonellInnloggingService _profesjonellInnlogging;

    public BankIdFullforModel(ProfesjonellInnloggingService profesjonellInnlogging)
    {
        _profesjonellInnlogging = profesjonellInnlogging;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (TempData["EktBankIdPersonnummer"] is not string personnummer)
        {
            TempData["BankIdFeilmelding"] = "Fant ingen BankID-identitet å fortsette med. Prøv å logge inn på nytt.";
            return RedirectToPage("LoggInn");
        }

        var huskMeg = TempData["EktBankIdHuskMeg"] as string == bool.TrueString;
        var returnUrl = TempData["EktBankIdReturnUrl"] as string;

        var resultat = await _profesjonellInnlogging.FullforAsync(personnummer, huskMeg, returnUrl, "Ekte BankID (Idura)", HttpContext, cancellationToken);
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

        TempData["BankIdFeilmelding"] = resultat.Feilmelding;
        return RedirectToPage("LoggInn");
    }
}
