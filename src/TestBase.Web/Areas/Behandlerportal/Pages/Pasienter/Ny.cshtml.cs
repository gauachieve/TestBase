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
        if (string.IsNullOrWhiteSpace(MobilNr) || string.IsNullOrWhiteSpace(Epost))
        {
            Feilmelding = "Mobilnummer og e-post er obligatoriske.";
            return Page();
        }

        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
        if (behandler is null)
        {
            return RedirectToPage("/Konto/LoggInn", new { area = "Behandlerportal" });
        }

        // HPR-gate: jf. kravdokumentet håndheves den KUN her (ikke ved innlogging) —
        // se HprPolicy for prøveperiodens lengde og evt. admin-innvilget forlengelse.
        if (HprPolicy.ErUtlopt(behandler, DateTimeOffset.UtcNow))
        {
            Feilmelding = $"HPR-nummeret ditt er ikke godkjent ennå, og prøveperioden på {HprPolicy.ProveperiodeDager} dager er utløpt. " +
                          "Kontakt administrator for godkjenning.";
            return Page();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        // Personnummer samles bevisst IKKE inn her (bugliste 2026-09-13 punkt 13) —
        // pasienten oppgir det selv via invitasjonslenken (FullforRegistreringAsync),
        // samme prinsipp som den offentlige selvregistreringen.
        var resultat = await _pasientService.LeggTilAsync(
            personnummer: null, MobilNr, Epost, behandlerId, Varslingskanal, baseUrl, cancellationToken: cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "LeggTilPasient",
            nameof(Pasient), resultat.Pasient.Id.ToString(), cancellationToken: cancellationToken);

        Opprettet = true;
        Lenke = resultat.Lenke;
        return Page();
    }
}
