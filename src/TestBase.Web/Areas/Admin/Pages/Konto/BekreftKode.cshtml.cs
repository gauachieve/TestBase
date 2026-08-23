using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Areas.Admin.Pages.Konto;

public sealed class BekreftKodeModel : PageModel
{
    private readonly AdminAuthenticationService _authService;
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public BekreftKodeModel(AdminAuthenticationService authService, AppDbContext db, IAuditLogger auditLogger)
    {
        _authService = authService;
        _db = db;
        _auditLogger = auditLogger;
    }

    [BindProperty]
    public string Kode { get; set; } = string.Empty;

    public string? Feilmelding { get; private set; }

    public IActionResult OnGet()
    {
        if (TempData.Peek("ToFaktorAdministratorId") is null)
        {
            return RedirectToPage("LoggInn");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (TempData["ToFaktorAdministratorId"] is not string administratorIdVerdi ||
            !long.TryParse(administratorIdVerdi, out var administratorId))
        {
            return RedirectToPage("LoggInn");
        }

        var huskMeg = TempData["ToFaktorHuskMeg"] as bool? ?? false;

        var administrator = await _db.Administratorer.FirstOrDefaultAsync(a => a.Id == administratorId, cancellationToken);
        if (administrator is null)
        {
            return RedirectToPage("LoggInn");
        }

        var gyldig = await _authService.VerifiserToFaktorAsync(administrator, Kode, cancellationToken);
        if (!gyldig)
        {
            Feilmelding = "Feil eller utløpt kode.";
            TempData.Keep("ToFaktorAdministratorId");
            TempData.Keep("ToFaktorHuskMeg");
            await _auditLogger.LogAsync(
                administrator.AdminId, nameof(UserRole.Administrator), "ToFaktorFeilet",
                nameof(Administrator), administrator.Id.ToString(), cancellationToken: cancellationToken);
            return Page();
        }

        await AdminSignIn.LoggInnAsync(HttpContext, administrator, UserRole.Administrator, huskMeg);
        await _auditLogger.LogAsync(
            administrator.AdminId, nameof(UserRole.Administrator), "InnloggingOk",
            nameof(Administrator), administrator.Id.ToString(), "BankID+2FA", cancellationToken);

        return RedirectToPage("/Index", new { area = "" });
    }
}
