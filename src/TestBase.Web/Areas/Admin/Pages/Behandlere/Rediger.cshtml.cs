using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Behandlere;

/// <summary>
/// Lar admin rette opp en behandlers profilfelt — særlig nyttig for
/// personnummer, som ellers kun settes av behandleren selv ved
/// egenregistrering og ikke kan slås opp/tilbakestilles noe annet sted (jf.
/// beslutningsloggen). Samme mønster som Administratorer/Rediger.cshtml.cs.
/// </summary>
public sealed class RedigerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly BehandlerAuthenticationService _authService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public RedigerModel(AppDbContext db, BehandlerAuthenticationService authService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _authService = authService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public long Id { get; set; }

    [BindProperty]
    public string? Fornavn { get; set; }

    [BindProperty]
    public string? Etternavn { get; set; }

    [BindProperty]
    public string MobilNr { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string? Personnummer { get; set; }

    [BindProperty]
    public string? HprNr { get; set; }

    [BindProperty]
    public string? Kontonummer { get; set; }

    [BindProperty]
    public string? Arbeidsadresse { get; set; }

    [BindProperty]
    public string? Tittel { get; set; }

    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is null)
        {
            return RedirectToPage("Index");
        }

        Id = behandler.Id;
        Fornavn = behandler.Fornavn;
        Etternavn = behandler.Etternavn;
        MobilNr = behandler.MobilNr;
        Email = behandler.Email;
        Personnummer = behandler.Personnummer;
        HprNr = behandler.HprNr;
        Kontonummer = behandler.Kontonummer;
        Arbeidsadresse = behandler.Arbeidsadresse;
        Tittel = behandler.Tittel;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == Id, cancellationToken);
        if (behandler is null)
        {
            return RedirectToPage("Index");
        }

        if (string.IsNullOrWhiteSpace(MobilNr) || string.IsNullOrWhiteSpace(Email))
        {
            Feilmelding = "Mobilnummer og e-post er obligatoriske.";
            return Page();
        }

        // Personnummer er kryptert (se AppDbContext) og kan derfor ikke håndheves
        // unikt med en SQL-indeks — sammenlign i minnet, ekskluder denne
        // behandleren selv (samme mønster som Administratorer/Rediger.cshtml.cs).
        // Valgfritt her (i motsetning til Administrator) — en behandler som ikke
        // har fullført egenregistrering ennå har ikke satt et personnummer.
        if (!string.IsNullOrWhiteSpace(Personnummer))
        {
            var eksisterendeMedPersonnummer = await _authService.FinnVedPersonnummerAsync(Personnummer, cancellationToken);
            if (eksisterendeMedPersonnummer is not null && eksisterendeMedPersonnummer.Id != Id)
            {
                Feilmelding = "Det finnes allerede en annen behandler med dette personnummeret.";
                return Page();
            }
        }

        behandler.Fornavn = Fornavn;
        behandler.Etternavn = Etternavn;
        behandler.MobilNr = MobilNr;
        behandler.Email = Email;
        behandler.Personnummer = string.IsNullOrWhiteSpace(Personnummer) ? null : Personnummer;
        behandler.HprNr = HprNr;
        behandler.Kontonummer = Kontonummer;
        behandler.Arbeidsadresse = Arbeidsadresse;
        behandler.Tittel = Tittel;

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterBehandler",
            nameof(Behandler), behandler.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }
}
