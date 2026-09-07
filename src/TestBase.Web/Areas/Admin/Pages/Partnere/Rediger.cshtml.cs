using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Partnere;

/// <summary>
/// Rediger partnerens egne felt/abonnement, og administrer hvilke behandlere
/// som er tilknyttet partneren (kun Superadmin gjør dette — partner-admin sin
/// egen, snevrere variant er Behandlerportal/MinPartner/Behandlere.cshtml).
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

    [BindProperty]
    public bool HarAktivtAbonnement { get; set; }

    public long PartnerId { get; private set; }
    public string PartnerNavn { get; private set; } = string.Empty;
    public List<Behandler> TilknyttedeBehandlere { get; private set; } = new();
    public List<Behandler> UavhengigeBehandlere { get; private set; } = new();
    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is null)
        {
            return NotFound();
        }

        PartnerId = partner.Id;
        PartnerNavn = partner.Navn;
        Navn = partner.Navn;
        KontaktpersonNavn = partner.KontaktpersonNavn;
        KontaktEpost = partner.KontaktEpost;
        KontaktMobilNr = partner.KontaktMobilNr;
        HarAktivtAbonnement = partner.HarAktivtAbonnement;

        await LastBehandlerlisterAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Navn er obligatorisk.";
            PartnerId = id;
            PartnerNavn = partner.Navn;
            await LastBehandlerlisterAsync(id, cancellationToken);
            return Page();
        }

        partner.Navn = Navn;
        partner.KontaktpersonNavn = KontaktpersonNavn;
        partner.KontaktEpost = KontaktEpost;
        partner.KontaktMobilNr = KontaktMobilNr;
        partner.HarAktivtAbonnement = HarAktivtAbonnement;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterPartner",
            nameof(Partner), partner.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostLeggTilBehandlerAsync(long id, long behandlerId, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId && b.PartnerId == null, cancellationToken);
        if (behandler is not null)
        {
            behandler.PartnerId = id;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "LeggBehandlerTilPartner",
                nameof(Behandler), behandler.Id.ToString(), $"PartnerId {id}", cancellationToken);
        }

        return RedirectToPage(new { id });
    }

    /// <summary>Fjerner (unlinker) behandleren fra partneren — arkiverer IKKE kontoen, se docs/beslutningslogg.md.</summary>
    public async Task<IActionResult> OnPostFjernBehandlerAsync(long id, long behandlerId, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId && b.PartnerId == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.PartnerId = null;
            behandler.ErPartnerAdministrator = false;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "FjernBehandlerFraPartner",
                nameof(Behandler), behandler.Id.ToString(), $"PartnerId {id}", cancellationToken);
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostTogglePartnerAdminAsync(long id, long behandlerId, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId && b.PartnerId == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.ErPartnerAdministrator = !behandler.ErPartnerAdministrator;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "EndrePartnerAdministrator",
                nameof(Behandler), behandler.Id.ToString(), behandler.ErPartnerAdministrator.ToString(), cancellationToken);
        }

        return RedirectToPage(new { id });
    }

    private async Task LastBehandlerlisterAsync(long partnerId, CancellationToken cancellationToken)
    {
        TilknyttedeBehandlere = await _db.Behandlere.Where(b => b.PartnerId == partnerId).OrderBy(b => b.Etternavn).ToListAsync(cancellationToken);
        UavhengigeBehandlere = await _db.Behandlere.Where(b => b.PartnerId == null && b.Status != BehandlerStatus.Arkivert)
            .OrderBy(b => b.Etternavn).ToListAsync(cancellationToken);
    }
}
