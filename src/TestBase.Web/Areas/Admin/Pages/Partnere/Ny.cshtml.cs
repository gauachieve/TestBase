using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Partnere;

public sealed class NyModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public NyModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public string? KontaktpersonNavn { get; set; }

    [BindProperty]
    public string? KontaktEpost { get; set; }

    [BindProperty]
    public string? KontaktMobilNr { get; set; }

    public string? Feilmelding { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Navn er obligatorisk.";
            return Page();
        }

        var administratorId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var aid) ? aid : 0;
        var partner = new Partner
        {
            Navn = Navn,
            KontaktpersonNavn = KontaktpersonNavn,
            KontaktEpost = KontaktEpost,
            KontaktMobilNr = KontaktMobilNr,
            OpprettetAvAdministratorId = administratorId,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        _db.Partnere.Add(partner);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OpprettPartner",
            nameof(Partner), partner.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }
}
