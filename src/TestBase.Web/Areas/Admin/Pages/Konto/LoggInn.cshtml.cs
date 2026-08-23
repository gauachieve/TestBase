using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Areas.Admin.Pages.Konto;

public sealed class LoggInnModel : PageModel
{
    private readonly AdminAuthenticationService _authService;
    private readonly IAuditLogger _auditLogger;

    public LoggInnModel(AdminAuthenticationService authService, IAuditLogger auditLogger)
    {
        _authService = authService;
        _auditLogger = auditLogger;
    }

    [BindProperty]
    public string AdminId { get; set; } = string.Empty;

    [BindProperty]
    public string? Passord { get; set; }

    [BindProperty]
    public bool HuskMeg { get; set; }

    public bool VisPassordFelt { get; private set; }
    public string? Feilmelding { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(AdminId))
        {
            Feilmelding = "AdminId må fylles ut.";
            return Page();
        }

        var administrator = await _authService.FinnVedAdminIdAsync(AdminId, cancellationToken);
        if (administrator is null)
        {
            Feilmelding = "Fant ingen administrator med denne AdminId-en.";
            return Page();
        }

        if (AdminAuthenticationService.HarPassordPalogging(administrator))
        {
            return await HandterPassordPaloggingAsync(administrator, cancellationToken);
        }

        return await HandterBankIdPaloggingAsync(administrator, cancellationToken);
    }

    private async Task<IActionResult> HandterPassordPaloggingAsync(Administrator administrator, CancellationToken cancellationToken)
    {
        VisPassordFelt = true;

        if (string.IsNullOrEmpty(Passord))
        {
            return Page();
        }

        var resultat = _authService.VerifiserPassord(administrator, Passord);
        if (resultat == PasswordVerificationResult.Failed)
        {
            Feilmelding = "Feil passord.";
            await _auditLogger.LogAsync(
                administrator.AdminId, nameof(UserRole.Utvikler), "InnloggingFeilet",
                nameof(Administrator), administrator.Id.ToString(), "Feil passord", cancellationToken);
            return Page();
        }

        await AdminSignIn.LoggInnAsync(HttpContext, administrator, UserRole.Utvikler, HuskMeg);
        await _auditLogger.LogAsync(
            administrator.AdminId, nameof(UserRole.Utvikler), "InnloggingOk",
            nameof(Administrator), administrator.Id.ToString(), "Passord (utviklingsmodus)", cancellationToken);

        return RedirectToPage("/Index", new { area = "" });
    }

    private async Task<IActionResult> HandterBankIdPaloggingAsync(Administrator administrator, CancellationToken cancellationToken)
    {
        var bankIdResultat = await _authService.StartBankIdAsync(cancellationToken);
        if (!bankIdResultat.Success || bankIdResultat.PersonNummer is null)
        {
            Feilmelding = bankIdResultat.ErrorMessage ?? "BankID-innlogging feilet.";
            return Page();
        }

        var funnetAdministrator = await _authService.FinnVedPersonnummerAsync(bankIdResultat.PersonNummer, cancellationToken);
        if (funnetAdministrator is null || funnetAdministrator.Id != administrator.Id)
        {
            Feilmelding = "BankID-personen samsvarer ikke med denne AdminId-en.";
            return Page();
        }

        await _authService.StartToFaktorAsync(administrator, cancellationToken);
        // TempData sin standardserialisering støtter ikke long — lagre som string.
        TempData["ToFaktorAdministratorId"] = administrator.Id.ToString();
        TempData["ToFaktorHuskMeg"] = HuskMeg;

        return RedirectToPage("BekreftKode");
    }
}
