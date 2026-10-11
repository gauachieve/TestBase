using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.MinPartner;

/// <summary>
/// "Behandlere i min klinikk" — flyttet ut av MinPartner/Behandlere.cshtml (som nå er en ren
/// "Min klinikk"-hub med knapper, se "Klinikk-oppdatering" i docs/beslutningslogg.md). LESING
/// (kollegalisten) er åpen for ENHVER behandler i klinikken (se PartnerMedlemOmrade i
/// Program.cs), men selve MUTASJONENE (invitere/fjerne en kollega — kan også frata noen
/// partner-admin-status) er fortsatt EKSPLISITT begrenset til klinikk-admin her i koden, siden
/// folder-policyen alene ikke lenger håndhever det.
/// </summary>
public sealed class KollegerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly BehandlerInvitasjonService _invitasjonService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public KollegerModel(AppDbContext db, BehandlerInvitasjonService invitasjonService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
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
    public bool ErPartnerAdministrator => _currentUser.ErPartnerAdministrator;

    public long EgenBehandlerId => HentBehandlerId();

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
        if (_currentUser.PartnerId is null || !_currentUser.ErPartnerAdministrator)
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

    /// <summary>
    /// Fjerner (unlinker) en kollega fra partneren — arkiverer IKKE kontoen, se
    /// docs/beslutningslogg.md. Selvfjerning er TILLATT selv for en klinikk-admin (bugliste
    /// 2026-09-13 punkt 14) — logger i så fall ut med det samme, siden PartnerId/
    /// ErPartnerAdministrator er innloggingscookie-claims som ellers ville forblitt utdaterte
    /// resten av økten.
    /// </summary>
    public async Task<IActionResult> OnPostFjernAsync(long behandlerId, CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null || !_currentUser.ErPartnerAdministrator)
        {
            return Forbid();
        }

        var erSelvfjerning = behandlerId == HentBehandlerId();

        var kollega = await _db.Behandlere.FirstOrDefaultAsync(
            b => b.Id == behandlerId && b.PartnerId == _currentUser.PartnerId, cancellationToken);
        if (kollega is not null)
        {
            kollega.PartnerId = null;
            kollega.ErPartnerAdministrator = false;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(),
                erSelvfjerning ? "FjernMegSelvFraPartner" : "FjernKollegaFraPartner",
                nameof(Behandler), kollega.Id.ToString(), cancellationToken: cancellationToken);
        }

        if (erSelvfjerning)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Konto/LoggInn", new { area = "" });
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
