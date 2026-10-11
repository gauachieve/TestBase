using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.MinPartner;

/// <summary>
/// Klinikk-admin sin egen, selvbetjente redigering av klinikkens egne felt
/// (Navn/kontaktperson/kontakt-e-post/kontakt-mobil) — se "Klinikk-oppdatering,
/// del 3" i docs/beslutningslogg.md. BEVISST IKKE
/// <see cref="Partner.HarAktivtAbonnement"/> her — det er et fakturerings-
/// relatert felt, fortsatt kun redigerbart av Superadmin på
/// Admin/Partnere/Rediger. Admin-only (klinikk-admin), samme eksplisitte
/// ErPartnerAdministrator-sjekk som de andre fire admin-sidene under
/// MinPartner, siden folder-policyen alene ikke lenger håndhever det.
/// </summary>
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
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public string? KontaktpersonNavn { get; set; }

    [BindProperty]
    public string? KontaktEpost { get; set; }

    [BindProperty]
    public string? KontaktMobilNr { get; set; }

    public string? Feilmelding { get; private set; }
    public bool Lagret { get; private set; }
    public bool ErPartnerAdministrator => _currentUser.ErPartnerAdministrator;
    public int AntallTilgjengeligeTester { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null || !_currentUser.ErPartnerAdministrator)
        {
            return Forbid();
        }

        var partnerId = _currentUser.PartnerId.Value;
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == partnerId, cancellationToken);
        if (partner is null)
        {
            return Forbid();
        }

        Navn = partner.Navn;
        KontaktpersonNavn = partner.KontaktpersonNavn;
        KontaktEpost = partner.KontaktEpost;
        KontaktMobilNr = partner.KontaktMobilNr;
        AntallTilgjengeligeTester = await _db.PartnerTestTilganger.CountAsync(t => t.PartnerId == partnerId, cancellationToken);
        Lagret = TempData["Lagret"] as bool? ?? false;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null || !_currentUser.ErPartnerAdministrator)
        {
            return Forbid();
        }

        var partnerId = _currentUser.PartnerId.Value;
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == partnerId, cancellationToken);
        if (partner is null)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Navn er obligatorisk.";
            AntallTilgjengeligeTester = await _db.PartnerTestTilganger.CountAsync(t => t.PartnerId == partnerId, cancellationToken);
            return Page();
        }

        partner.Navn = Navn.Trim();
        partner.KontaktpersonNavn = string.IsNullOrWhiteSpace(KontaktpersonNavn) ? null : KontaktpersonNavn.Trim();
        partner.KontaktEpost = string.IsNullOrWhiteSpace(KontaktEpost) ? null : KontaktEpost.Trim();
        partner.KontaktMobilNr = string.IsNullOrWhiteSpace(KontaktMobilNr) ? null : KontaktMobilNr.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "KlinikkAdminOppdaterPartner",
            nameof(Partner), partnerId.ToString(), cancellationToken: cancellationToken);

        TempData["Lagret"] = true;
        return RedirectToPage();
    }
}
