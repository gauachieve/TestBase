using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

public sealed class GruppeimportModel : PageModel
{
    private static readonly TimeSpan HprPrøveperiode = TimeSpan.FromDays(7);

    private readonly AppDbContext _db;
    private readonly PasientInvitasjonService _pasientService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public GruppeimportModel(AppDbContext db, PasientInvitasjonService pasientService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _pasientService = pasientService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string Liste { get; set; } = string.Empty;

    public string? Feilmelding { get; private set; }
    public int? AntallOpprettet { get; private set; }
    public IReadOnlyList<string> HoppetOverLinjer { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<PasientInvitasjonResultat> OpprettedeMedLenke { get; private set; } = Array.Empty<PasientInvitasjonResultat>();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Liste))
        {
            Feilmelding = "Lim inn minst én linje.";
            return Page();
        }

        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
        if (behandler is null)
        {
            return RedirectToPage("/Konto/LoggInn", new { area = "Behandlerportal" });
        }

        if (!behandler.HprGodkjent && behandler.RegistrertUtc is not null &&
            DateTimeOffset.UtcNow > behandler.RegistrertUtc.Value.Add(HprPrøveperiode))
        {
            Feilmelding = "HPR-nummeret ditt er ikke godkjent ennå, og prøveperioden på 7 dager er utløpt. " +
                          "Kontakt administrator for godkjenning.";
            return Page();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var resultat = await _pasientService.ImporterGruppeAsync(Liste, behandlerId, baseUrl, cancellationToken);
        AntallOpprettet = resultat.Opprettet.Count;
        HoppetOverLinjer = resultat.HoppetOverLinjer;
        OpprettedeMedLenke = resultat.Opprettet;

        foreach (var opprettelse in resultat.Opprettet)
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "LeggTilPasient",
                nameof(Pasient), opprettelse.Pasient.Id.ToString(), "Gruppeimport", cancellationToken);
        }

        return Page();
    }
}
