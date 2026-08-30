using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Administratorer;

public sealed class RedigerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AdminAuthenticationService _authService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public RedigerModel(AppDbContext db, AdminAuthenticationService authService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _authService = authService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public long Id { get; set; }

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
    public string? NyttPassord { get; set; }

    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var administrator = await _db.Administratorer.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (administrator is null)
        {
            return RedirectToPage("Index");
        }

        Id = administrator.Id;
        AdminId = administrator.AdminId;
        MobilNr = administrator.MobilNr;
        Email = administrator.Email;
        FulltNavn = administrator.FulltNavn;
        Personnummer = administrator.Personnummer;
        HprNr = administrator.HprNr;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var administrator = await _db.Administratorer.FirstOrDefaultAsync(a => a.Id == Id, cancellationToken);
        if (administrator is null)
        {
            return RedirectToPage("Index");
        }

        if (string.IsNullOrWhiteSpace(AdminId) || string.IsNullOrWhiteSpace(MobilNr) ||
            string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(FulltNavn) ||
            string.IsNullOrWhiteSpace(Personnummer) || string.IsNullOrWhiteSpace(HprNr))
        {
            Feilmelding = "AdminId, mobilnr, e-post, fullt navn, personnummer og HPR-nr er obligatoriske.";
            return Page();
        }

        if (await _db.Administratorer.AnyAsync(a => a.AdminId == AdminId && a.Id != Id, cancellationToken))
        {
            Feilmelding = "AdminId er allerede i bruk.";
            return Page();
        }

        // Personnummer er kryptert (se AppDbContext) og kan derfor ikke håndheves
        // unikt med en SQL-indeks — sammenlign i minnet, ekskluder denne
        // administratoren selv (samme mønster som Ny.cshtml.cs).
        var eksisterendeMedPersonnummer = await _authService.FinnVedPersonnummerAsync(Personnummer, cancellationToken);
        if (eksisterendeMedPersonnummer is not null && eksisterendeMedPersonnummer.Id != Id)
        {
            Feilmelding = "Det finnes allerede en administrator med dette personnummeret.";
            return Page();
        }

        administrator.AdminId = AdminId;
        administrator.MobilNr = MobilNr;
        administrator.Email = Email;
        administrator.FulltNavn = FulltNavn;
        administrator.Personnummer = Personnummer;
        administrator.HprNr = HprNr;

        if (!string.IsNullOrWhiteSpace(NyttPassord))
        {
            administrator.PasswordHash = _authService.HashPassord(administrator, NyttPassord);
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterAdministrator",
            nameof(Administrator), administrator.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }
}
