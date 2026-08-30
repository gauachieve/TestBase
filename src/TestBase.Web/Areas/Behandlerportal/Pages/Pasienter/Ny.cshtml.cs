using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

public sealed class NyModel : PageModel
{
    private static readonly TimeSpan HprPrøveperiode = TimeSpan.FromDays(7);

    private readonly AppDbContext _db;
    private readonly PasientInvitasjonService _pasientService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public NyModel(AppDbContext db, PasientInvitasjonService pasientService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _pasientService = pasientService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string Personnummer { get; set; } = string.Empty;

    [BindProperty]
    public string MobilNr { get; set; } = string.Empty;

    [BindProperty]
    public string Epost { get; set; } = string.Empty;

    [BindProperty]
    public KontaktMetode Varslingskanal { get; set; } = KontaktMetode.Sms;

    public string? Feilmelding { get; private set; }
    public bool Opprettet { get; private set; }
    public string? Lenke { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Personnummer) || string.IsNullOrWhiteSpace(MobilNr) || string.IsNullOrWhiteSpace(Epost))
        {
            Feilmelding = "Personnummer, mobilnummer og e-post er alle obligatoriske.";
            return Page();
        }

        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
        if (behandler is null)
        {
            return RedirectToPage("/Konto/LoggInn", new { area = "Behandlerportal" });
        }

        // HPR-gate: jf. kravdokumentet håndheves den KUN her (ikke ved innlogging) —
        // 7 dagers prøveperiode fra fullført registrering, deretter kreves godkjenning.
        if (!behandler.HprGodkjent && behandler.RegistrertUtc is not null &&
            DateTimeOffset.UtcNow > behandler.RegistrertUtc.Value.Add(HprPrøveperiode))
        {
            Feilmelding = "HPR-nummeret ditt er ikke godkjent ennå, og prøveperioden på 7 dager er utløpt. " +
                          "Kontakt administrator for godkjenning.";
            return Page();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var resultat = await _pasientService.LeggTilAsync(
            Personnummer, MobilNr, Epost, behandlerId, Varslingskanal, baseUrl, cancellationToken: cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "LeggTilPasient",
            nameof(Pasient), resultat.Pasient.Id.ToString(), cancellationToken: cancellationToken);

        Opprettet = true;
        Lenke = resultat.Lenke;
        return Page();
    }
}
