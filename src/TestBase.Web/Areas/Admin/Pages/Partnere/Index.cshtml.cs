using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Partnere;

/// <summary>
/// Superadmin-only (se SuperadminOmrade i Program.cs) — se
/// docs/beslutningslogg.md "Partner System + Test Monetization".
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;
    private readonly ICaptchaProvider _captcha;

    public IndexModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser, ICaptchaProvider captcha)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _captcha = captcha;
    }

    public List<Partner> Partnere { get; private set; } = new();
    public bool VisSlettede { get; private set; }
    public string CaptchaSporsmal { get; private set; } = string.Empty;

    [BindProperty]
    public string CaptchaSignertFasit { get; set; } = string.Empty;

    [BindProperty]
    public string? CaptchaSvar { get; set; }

    public string? Feilmelding { get; private set; }

    public async Task OnGetAsync(bool visSlettede, CancellationToken cancellationToken)
    {
        VisSlettede = visSlettede;

        var sporring = _db.Partnere.AsQueryable();
        if (!VisSlettede)
        {
            sporring = sporring.Where(p => !p.ErSlettet);
        }

        Partnere = await sporring.OrderByDescending(p => p.OpprettetUtc).ToListAsync(cancellationToken);

        var utfordring = _captcha.LagUtfordring();
        CaptchaSporsmal = utfordring.SporsmalTekst;
        CaptchaSignertFasit = utfordring.SignertFasit;
    }

    public async Task<IActionResult> OnPostArkiverAsync(long id, CancellationToken cancellationToken)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is not null)
        {
            partner.ErArkivert = !partner.ErArkivert;
            partner.ArkivertUtc = partner.ErArkivert ? DateTimeOffset.UtcNow : null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "EndreArkivertPartner",
                nameof(Partner), partner.Id.ToString(), partner.ErArkivert.ToString(), cancellationToken);
        }

        return RedirectToPage();
    }

    /// <summary>Kun mulig når partneren allerede er arkivert — håndhevet server-side.</summary>
    public async Task<IActionResult> OnPostSlettAsync(long id, CancellationToken cancellationToken)
    {
        if (!_captcha.Verifiser(CaptchaSignertFasit, CaptchaSvar))
        {
            // Se Administratorer/Index.cshtml.cs for hvorfor ModelState.Clear() må skje
            // FØR OnGetAsync gjenoppfrisker CaptchaSporsmal/CaptchaSignertFasit.
            ModelState.Clear();
            await OnGetAsync(false, cancellationToken);
            Feilmelding = "Feil svar på sikkerhetsspørsmålet — sletting avbrutt.";
            return Page();
        }

        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is not null && partner.ErArkivert)
        {
            partner.ErSlettet = true;
            partner.SlettetUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "SlettPartner",
                nameof(Partner), partner.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGjenopprettFraSlettetAsync(long id, CancellationToken cancellationToken)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is not null)
        {
            partner.ErSlettet = false;
            partner.SlettetUtc = null;
            partner.ErArkivert = false;
            partner.ArkivertUtc = null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GjenopprettSlettetPartner",
                nameof(Partner), partner.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage(new { visSlettede = true });
    }
}
