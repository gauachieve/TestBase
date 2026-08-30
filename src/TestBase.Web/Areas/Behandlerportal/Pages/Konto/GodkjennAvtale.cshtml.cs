using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Konto;

/// <summary>
/// Vises kun når en allerede innlogget behandler har godtatt en eldre versjon
/// av brukeravtalen enn <see cref="Brukeravtale.GjeldendeVersjon"/> — jf.
/// kravdokumentets Del 3: "system for at de må regodkjenne den hver gang den
/// endrer seg". Ved førstegangsregistrering godtas avtalen allerede som del av
/// Fullfor-skjemaet (se BehandlerInvitasjonService.FullforProfilAsync).
/// </summary>
[Authorize]
public sealed class GodkjennAvtaleModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public GodkjennAvtaleModel(AppDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    [BindProperty]
    public bool GodtarAvtale { get; set; }

    public string AvtaleTekst => Brukeravtale.Tekst;
    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!GodtarAvtale)
        {
            Feilmelding = "Du må godta brukeravtalen for å fortsette.";
            return Page();
        }

        var behandlerId = HentBehandlerId();
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
        if (behandler is null)
        {
            return RedirectToPage("/Konto/LoggInn", new { area = "" });
        }

        behandler.BrukeravtaleGodkjentVersjon = Brukeravtale.GjeldendeVersjon;
        behandler.BrukeravtaleGodkjentUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            $"behandler:{behandler.Id}", nameof(UserRole.Behandler), "GodkjentBrukeravtale",
            nameof(Behandler), behandler.Id.ToString(), $"Versjon {Brukeravtale.GjeldendeVersjon}", cancellationToken);

        return RedirectToPage("/Pasienter/Index", new { area = "Behandlerportal" });
    }

    private long HentBehandlerId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)?.Split(':').LastOrDefault(), out var id) ? id : 0;
}
