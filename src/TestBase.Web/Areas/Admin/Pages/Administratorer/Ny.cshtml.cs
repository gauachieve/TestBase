using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Administratorer;

public sealed class NyModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AdminAuthenticationService _authService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public NyModel(AppDbContext db, AdminAuthenticationService authService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _authService = authService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string AdminId { get; set; } = string.Empty;

    [BindProperty]
    public string MobilNr { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string FulltNavn { get; set; } = string.Empty;

    [BindProperty]
    public string Personnummer { get; set; } = string.Empty;

    [BindProperty]
    public string HprNr { get; set; } = string.Empty;

    [BindProperty]
    public string? Passord { get; set; }

    public string? Feilmelding { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(AdminId) || string.IsNullOrWhiteSpace(MobilNr) ||
            string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(FulltNavn) ||
            string.IsNullOrWhiteSpace(Personnummer) || string.IsNullOrWhiteSpace(HprNr))
        {
            Feilmelding = "AdminId, mobilnr, e-post, fullt navn, personnummer og HPR-nr er obligatoriske. Passord er valgfritt.";
            return Page();
        }

        if (await _db.Administratorer.AnyAsync(a => a.AdminId == AdminId, cancellationToken))
        {
            Feilmelding = "AdminId er allerede i bruk.";
            return Page();
        }

        // Personnummer er kryptert i databasen (se AppDbContext) og kan derfor ikke
        // håndheves unikt med en SQL-indeks — sjekk i minnet i stedet (se også
        // AdminAuthenticationService.FinnVedPersonnummerAsync). To administratorer
        // med samme personnummer ville gjort BankID-oppslag ved innlogging tvetydig.
        if (await _authService.FinnVedPersonnummerAsync(Personnummer, cancellationToken) is not null)
        {
            Feilmelding = "Det finnes allerede en administrator med dette personnummeret.";
            return Page();
        }

        var administrator = new Administrator
        {
            AdminId = AdminId,
            MobilNr = MobilNr,
            Email = Email,
            FulltNavn = FulltNavn,
            Personnummer = Personnummer,
            HprNr = HprNr,
            OpprettetUtc = DateTimeOffset.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(Passord))
        {
            administrator.PasswordHash = _authService.HashPassord(administrator, Passord);
        }

        _db.Administratorer.Add(administrator);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OpprettAdministrator",
            nameof(Administrator), administrator.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }
}
