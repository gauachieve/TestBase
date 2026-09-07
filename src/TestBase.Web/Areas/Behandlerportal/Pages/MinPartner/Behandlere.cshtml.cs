using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.MinPartner;

/// <summary>
/// Partner-admin sin EGEN, snevrere variant av Admin/Partnere/Rediger — kan
/// invitere/fjerne kolleger INNENFOR egen partner (se PartnerAdminOmrade i
/// Program.cs), men kan IKKE gjøre noen til/fra partner-admin selv (kun
/// Superadmin, se docs/beslutningslogg.md "Partner System + Test
/// Monetization" — bevisst forskjellig navn fra domeneklassen Partner for å
/// unngå navneroms-skyggelegging, se CLAUDE.md sine kjente fallgruver).
/// </summary>
public sealed class BehandlereModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly BehandlerInvitasjonService _invitasjonService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public BehandlereModel(AppDbContext db, BehandlerInvitasjonService invitasjonService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _invitasjonService = invitasjonService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string? MobilNr { get; set; }

    [BindProperty]
    public string? Epost { get; set; }

    public List<Behandler> Kolleger { get; private set; } = new();
    public string? Feilmelding { get; private set; }
    public string? InvitasjonsLenke { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        await LastKollegerAsync(_currentUser.PartnerId.Value, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostInviterAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(MobilNr) && string.IsNullOrWhiteSpace(Epost))
        {
            Feilmelding = "Må ha enten mobilnummer eller e-post.";
            await LastKollegerAsync(_currentUser.PartnerId.Value, cancellationToken);
            return Page();
        }

        var behandlerId = HentBehandlerId();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var resultat = await _invitasjonService.InviterAsync(
            MobilNr, Epost, administratorId: null, behandlerId: behandlerId,
            baseUrl: baseUrl, partnerId: _currentUser.PartnerId, cancellationToken: cancellationToken);

        InvitasjonsLenke = resultat.Lenke;

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "InviterPartnerBehandler",
            nameof(Behandler), resultat.Behandler.Id.ToString(), $"PartnerId {_currentUser.PartnerId}", cancellationToken);

        await LastKollegerAsync(_currentUser.PartnerId.Value, cancellationToken);
        return Page();
    }

    /// <summary>Fjerner (unlinker) kollegaen fra partneren — arkiverer IKKE kontoen, se docs/beslutningslogg.md.</summary>
    public async Task<IActionResult> OnPostFjernAsync(long behandlerId, CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        if (behandlerId == HentBehandlerId())
        {
            Feilmelding = "Du kan ikke fjerne deg selv fra partneren.";
            await LastKollegerAsync(_currentUser.PartnerId.Value, cancellationToken);
            return Page();
        }

        var kollega = await _db.Behandlere.FirstOrDefaultAsync(
            b => b.Id == behandlerId && b.PartnerId == _currentUser.PartnerId, cancellationToken);
        if (kollega is not null)
        {
            kollega.PartnerId = null;
            kollega.ErPartnerAdministrator = false;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "FjernKollegaFraPartner",
                nameof(Behandler), kollega.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    private async Task LastKollegerAsync(long partnerId, CancellationToken cancellationToken)
    {
        Kolleger = await _db.Behandlere.Where(b => b.PartnerId == partnerId).OrderBy(b => b.Etternavn).ToListAsync(cancellationToken);
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
