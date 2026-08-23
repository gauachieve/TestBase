using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Konto;

public sealed class LoggInnModel : PageModel
{
    private readonly BehandlerAuthenticationService _authService;
    private readonly IAuditLogger _auditLogger;

    public LoggInnModel(BehandlerAuthenticationService authService, IAuditLogger auditLogger)
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

        var behandler = await _authService.FinnVedPersonnummerAsync(bankIdResultat.PersonNummer, cancellationToken);
        if (behandler is null)
        {
            Feilmelding = "Fant ingen behandlerkonto for denne BankID-personen. Be administrator om en invitasjon.";
            return Page();
        }

        switch (behandler.Status)
        {
            case BehandlerStatus.Invitert:
                Feilmelding = "Du har ikke fullført registreringen ennå. Bruk invitasjonslenken du mottok på SMS/e-post.";
                return Page();
            case BehandlerStatus.Fryst:
                Feilmelding = "Kontoen din er fryst. Kontakt administrator.";
                return Page();
            case BehandlerStatus.Arkivert:
                Feilmelding = "Kontoen din er arkivert.";
                return Page();
        }

        await _authService.StartToFaktorAsync(behandler, cancellationToken);
        // TempData sin standardserialisering støtter ikke long — lagre som string.
        TempData["ToFaktorBehandlerId"] = behandler.Id.ToString();
        TempData["ToFaktorHuskMeg"] = HuskMeg;

        return RedirectToPage("BekreftKode");
    }
}
