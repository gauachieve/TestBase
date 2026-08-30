using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

public sealed class RedigerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public RedigerModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public long Id { get; set; }

    [BindProperty]
    public string? Navn { get; set; }

    [BindProperty]
    public string? Gruppenavn { get; set; }

    [BindProperty]
    public string Personnummer { get; set; } = string.Empty;

    [BindProperty]
    public string MobilNr { get; set; } = string.Empty;

    [BindProperty]
    public string Epost { get; set; } = string.Empty;

    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id && p.BehandlerId == behandlerId, cancellationToken);
        if (pasient is null)
        {
            return RedirectToPage("Index");
        }

        Id = pasient.Id;
        Navn = pasient.Navn;
        Gruppenavn = pasient.Gruppenavn;
        Personnummer = pasient.Personnummer;
        MobilNr = pasient.MobilNr;
        Epost = pasient.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == Id && p.BehandlerId == behandlerId, cancellationToken);
        if (pasient is null)
        {
            return RedirectToPage("Index");
        }

        if (string.IsNullOrWhiteSpace(Personnummer) || string.IsNullOrWhiteSpace(MobilNr) || string.IsNullOrWhiteSpace(Epost))
        {
            Feilmelding = "Personnummer, mobilnummer og e-post er alle obligatoriske.";
            return Page();
        }

        // Personnummer er kryptert (se AppDbContext) og kan derfor ikke håndheves
        // unikt med en SQL-indeks — sammenlign i minnet, ekskluder denne pasienten selv.
        var andrePasienter = await _db.Pasienter
            .Where(p => p.Id != Id && p.Status != PasientStatus.Arkivert)
            .ToListAsync(cancellationToken);
        if (andrePasienter.Any(p => p.Personnummer == Personnummer))
        {
            Feilmelding = "Det finnes allerede en annen pasient med dette personnummeret.";
            return Page();
        }

        pasient.Navn = string.IsNullOrWhiteSpace(Navn) ? null : Navn;
        pasient.Gruppenavn = string.IsNullOrWhiteSpace(Gruppenavn) ? null : Gruppenavn;
        pasient.Personnummer = Personnummer;
        pasient.MobilNr = MobilNr;
        pasient.Email = Epost;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterPasient",
            nameof(Pasient), pasient.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
