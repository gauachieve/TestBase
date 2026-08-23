using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Konto;

public sealed class BekreftKodeModel : PageModel
{
    private readonly BehandlerAuthenticationService _authService;
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public BekreftKodeModel(BehandlerAuthenticationService authService, AppDbContext db, IAuditLogger auditLogger)
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
        if (TempData.Peek("ToFaktorBehandlerId") is null)
        {
            return RedirectToPage("LoggInn");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (TempData["ToFaktorBehandlerId"] is not string behandlerIdVerdi ||
            !long.TryParse(behandlerIdVerdi, out var behandlerId))
        {
            return RedirectToPage("LoggInn");
        }

        var huskMeg = TempData["ToFaktorHuskMeg"] as bool? ?? false;

        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
        if (behandler is null)
        {
            return RedirectToPage("LoggInn");
        }

        var gyldig = await _authService.VerifiserToFaktorAsync(behandler, Kode, cancellationToken);
        if (!gyldig)
        {
            Feilmelding = "Feil eller utløpt kode.";
            TempData.Keep("ToFaktorBehandlerId");
            TempData.Keep("ToFaktorHuskMeg");
            await _auditLogger.LogAsync(
                $"behandler:{behandler.Id}", nameof(UserRole.Behandler), "ToFaktorFeilet",
                nameof(Behandler), behandler.Id.ToString(), cancellationToken: cancellationToken);
            return Page();
        }

        await AuthSignIn.LoggInnAsync(HttpContext, "behandler", behandler.Id, behandler.Visningsnavn ?? "Behandler", UserRole.Behandler, huskMeg);
        await _auditLogger.LogAsync(
            $"behandler:{behandler.Id}", nameof(UserRole.Behandler), "InnloggingOk",
            nameof(Behandler), behandler.Id.ToString(), "BankID+2FA", cancellationToken);

        if (behandler.BrukeravtaleGodkjentVersjon != Brukeravtale.GjeldendeVersjon)
        {
            return RedirectToPage("GodkjennAvtale");
        }

        return RedirectToPage("/Index", new { area = "" });
    }
}
