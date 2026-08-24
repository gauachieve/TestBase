using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages.Konto;

public sealed class LoggInnModel : PageModel
{
    private readonly PasientAuthenticationService _authService;
    private readonly IAuditLogger _auditLogger;

    public LoggInnModel(PasientAuthenticationService authService, IAuditLogger auditLogger)
    {
        _authService = authService;
        _auditLogger = auditLogger;
    }

    [BindProperty]
    public bool HuskMeg { get; set; }

    public string? Feilmelding { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var bankIdResultat = await _authService.StartBankIdAsync(cancellationToken);
        if (!bankIdResultat.Success || bankIdResultat.PersonNummer is null)
        {
            Feilmelding = bankIdResultat.ErrorMessage ?? "BankID-innlogging feilet.";
            return Page();
        }

        var pasient = await _authService.FinnVedPersonnummerAsync(bankIdResultat.PersonNummer, cancellationToken);
        if (pasient is null)
        {
            Feilmelding = "Fant ingen pasientkonto for denne BankID-personen. Be behandleren din om en invitasjon.";
            return Page();
        }

        switch (pasient.Status)
        {
            case PasientStatus.Invitert:
                Feilmelding = "Du har ikke fullført registreringen ennå. Bruk lenken du mottok på SMS/e-post.";
                return Page();
            case PasientStatus.Arkivert:
                Feilmelding = "Kontoen din er arkivert. Kontakt behandleren din.";
                return Page();
        }

        await AuthSignIn.LoggInnAsync(HttpContext, "pasient", pasient.Id, pasient.Navn ?? "Pasient", UserRole.Pasient, HuskMeg);
        await _auditLogger.LogAsync(
            $"pasient:{pasient.Id}", nameof(UserRole.Pasient), "InnloggingOk",
            nameof(Pasient), pasient.Id.ToString(), "BankID", cancellationToken);

        return RedirectToPage("/MinSide", new { area = "Pasientportal" });
    }
}
