using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.Konto;

public sealed class BekreftKodeModel : PageModel
{
    private readonly AdminAuthenticationService _adminAuth;
    private readonly BehandlerAuthenticationService _behandlerAuth;
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly IConfiguration _configuration;

    public BekreftKodeModel(
        AdminAuthenticationService adminAuth,
        BehandlerAuthenticationService behandlerAuth,
        AppDbContext db,
        IAuditLogger auditLogger,
        IConfiguration configuration)
    {
        _adminAuth = adminAuth;
        _behandlerAuth = behandlerAuth;
        _db = db;
        _auditLogger = auditLogger;
        _configuration = configuration;
    }

    [BindProperty]
    public string Kode { get; set; } = string.Empty;

    public string? Feilmelding { get; private set; }

    /// <summary>Kun satt i Development — se ToFaktorService.StartAsync/Pages/Konto/LoggInn.cshtml.cs.</summary>
    public string? DevKode { get; private set; }

    public IActionResult OnGet()
    {
        if (TempData.Peek("ToFaktorRolle") is null)
        {
            return RedirectToPage("LoggInn");
        }

        DevKode = TempData.Peek("DevToFaktorKode") as string;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (TempData["ToFaktorRolle"] is not string rolleVerdi ||
            !Enum.TryParse<UserRole>(rolleVerdi, out var rolle) ||
            TempData["ToFaktorId"] is not string idVerdi ||
            !long.TryParse(idVerdi, out var id))
        {
            return RedirectToPage("LoggInn");
        }

        var huskMeg = TempData["ToFaktorHuskMeg"] as bool? ?? false;
        var returnUrl = TempData["ToFaktorReturnUrl"] as string;
        DevKode = TempData["DevToFaktorKode"] as string;

        return rolle == UserRole.Administrator
            ? await BekreftAdministratorAsync(id, huskMeg, returnUrl, rolleVerdi, idVerdi, cancellationToken)
            : await BekreftBehandlerAsync(id, huskMeg, returnUrl, rolleVerdi, idVerdi, cancellationToken);
    }

    private async Task<IActionResult> BekreftAdministratorAsync(
        long id, bool huskMeg, string? returnUrl, string rolleVerdi, string idVerdi, CancellationToken cancellationToken)
    {
        var administrator = await _db.Administratorer.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (administrator is null)
        {
            return RedirectToPage("LoggInn");
        }

        var gyldig = await _adminAuth.VerifiserToFaktorAsync(administrator, Kode, cancellationToken);
        if (!gyldig)
        {
            Feilmelding = "Feil eller utløpt kode.";
            TempData["ToFaktorRolle"] = rolleVerdi;
            TempData["ToFaktorId"] = idVerdi;
            TempData["ToFaktorHuskMeg"] = huskMeg;
            TempData["ToFaktorReturnUrl"] = returnUrl;
            TempData["DevToFaktorKode"] = DevKode;
            await _auditLogger.LogAsync(
                administrator.AdminId, nameof(UserRole.Administrator), "ToFaktorFeilet",
                nameof(Administrator), administrator.Id.ToString(), cancellationToken: cancellationToken);
            return Page();
        }

        BetroddEnhet.Marker(HttpContext, ToFaktorPrincipalType.Administrator, administrator.Id, BetroddEnhetLevetid());

        await AuthSignIn.LoggInnAsync(HttpContext, "administrator", administrator.Id, administrator.FulltNavn, UserRole.Administrator, huskMeg);
        await _auditLogger.LogAsync(
            administrator.AdminId, nameof(UserRole.Administrator), "InnloggingOk",
            nameof(Administrator), administrator.Id.ToString(), "BankID+2FA", cancellationToken);

        return TilMaalEtterInnlogging(returnUrl, "Admin", "/Administratorer/Index");
    }

    private async Task<IActionResult> BekreftBehandlerAsync(
        long id, bool huskMeg, string? returnUrl, string rolleVerdi, string idVerdi, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is null)
        {
            return RedirectToPage("LoggInn");
        }

        var gyldig = await _behandlerAuth.VerifiserToFaktorAsync(behandler, Kode, cancellationToken);
        if (!gyldig)
        {
            Feilmelding = "Feil eller utløpt kode.";
            TempData["ToFaktorRolle"] = rolleVerdi;
            TempData["ToFaktorId"] = idVerdi;
            TempData["ToFaktorHuskMeg"] = huskMeg;
            TempData["ToFaktorReturnUrl"] = returnUrl;
            TempData["DevToFaktorKode"] = DevKode;
            await _auditLogger.LogAsync(
                $"behandler:{behandler.Id}", nameof(UserRole.Behandler), "ToFaktorFeilet",
                nameof(Behandler), behandler.Id.ToString(), cancellationToken: cancellationToken);
            return Page();
        }

        BetroddEnhet.Marker(HttpContext, ToFaktorPrincipalType.Behandler, behandler.Id, BetroddEnhetLevetid());

        await AuthSignIn.LoggInnAsync(HttpContext, "behandler", behandler.Id, behandler.Visningsnavn ?? "Behandler", UserRole.Behandler, huskMeg);
        await _auditLogger.LogAsync(
            $"behandler:{behandler.Id}", nameof(UserRole.Behandler), "InnloggingOk",
            nameof(Behandler), behandler.Id.ToString(), "BankID+2FA", cancellationToken);

        if (behandler.BrukeravtaleGodkjentVersjon != Brukeravtale.GjeldendeVersjon)
        {
            return RedirectToPage("/Konto/GodkjennAvtale", new { area = "Behandlerportal" });
        }

        return TilMaalEtterInnlogging(returnUrl, "Behandlerportal", "/Pasienter/Index");
    }

    private TimeSpan BetroddEnhetLevetid() =>
        TimeSpan.FromDays(_configuration.GetValue("Auth:BetroddEnhetDager", 30));

    private IActionResult TilMaalEtterInnlogging(string? returnUrl, string fallbackArea, string fallbackPage) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToPage(fallbackPage, new { area = fallbackArea });
}
