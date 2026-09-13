using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Behandlere;

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

    public List<Behandler> Behandlere { get; private set; } = new();
    public bool ErSuperadmin => _currentUser.Role == UserRole.Superadmin;
    public bool VisSlettede { get; private set; }
    public string CaptchaSporsmal { get; private set; } = string.Empty;

    [BindProperty]
    public string CaptchaSignertFasit { get; set; } = string.Empty;

    [BindProperty]
    public string? CaptchaSvar { get; set; }

    public string? Feilmelding { get; private set; }

    public async Task OnGetAsync(bool visSlettede, CancellationToken cancellationToken)
    {
        VisSlettede = visSlettede && ErSuperadmin;

        var sporring = _db.Behandlere.AsQueryable();
        if (!VisSlettede)
        {
            sporring = sporring.Where(b => !b.ErSlettet);
        }

        Behandlere = await sporring.OrderByDescending(b => b.OpprettetUtc).ToListAsync(cancellationToken);

        var utfordring = _captcha.LagUtfordring();
        CaptchaSporsmal = utfordring.SporsmalTekst;
        CaptchaSignertFasit = utfordring.SignertFasit;
    }

    public async Task<IActionResult> OnPostFrysAsync(long id, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null && behandler.Status != BehandlerStatus.Arkivert)
        {
            behandler.Status = behandler.Status == BehandlerStatus.Fryst ? BehandlerStatus.Aktiv : BehandlerStatus.Fryst;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "EndreBehandlerStatus",
                nameof(Behandler), behandler.Id.ToString(), behandler.Status.ToString(), cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostArkiverAsync(long id, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.Status = BehandlerStatus.Arkivert;
            behandler.ArkivertUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "ArkiverBehandler",
                nameof(Behandler), behandler.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGodkjennHprAsync(long id, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.HprGodkjent = !behandler.HprGodkjent;
            var administratorId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var aid) ? aid : (long?)null;
            behandler.HprGodkjentAvAdministratorId = behandler.HprGodkjent ? administratorId : null;
            behandler.HprGodkjentUtc = behandler.HprGodkjent ? DateTimeOffset.UtcNow : null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(),
                behandler.HprGodkjent ? "GodkjennHpr" : "TilbakekallHprGodkjenning",
                nameof(Behandler), behandler.Id.ToString(), $"HPR-nr {behandler.HprNr}", cancellationToken);
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Utvider HPR-prøveperioden med HprPolicy.ForlengelseDager dager — kun mulig ÉN
    /// gang per behandler (typisk behov: ferie), se docs/beslutningslogg.md
    /// "Bugliste 2026-09-13".
    /// </summary>
    public async Task<IActionResult> OnPostForlengHprAsync(long id, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null && !behandler.HprGodkjent && behandler.HprForlengetTilUtc is null)
        {
            var gjeldendeFrist = HprPolicy.BeregnFrist(behandler) ?? DateTimeOffset.UtcNow;
            behandler.HprForlengetTilUtc = gjeldendeFrist.AddDays(HprPolicy.ForlengelseDager);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "ForlengHprFrist",
                nameof(Behandler), behandler.Id.ToString(), behandler.HprForlengetTilUtc.ToString(), cancellationToken);
        }

        return RedirectToPage();
    }

    /// <summary>Kun mulig når behandleren allerede er arkivert — håndhevet server-side.</summary>
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

        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null && behandler.Status == BehandlerStatus.Arkivert)
        {
            behandler.ErSlettet = true;
            behandler.SlettetUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "SlettBehandler",
                nameof(Behandler), behandler.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGjenopprettFraSlettetAsync(long id, CancellationToken cancellationToken)
    {
        if (!ErSuperadmin)
        {
            return Forbid();
        }

        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.ErSlettet = false;
            behandler.SlettetUtc = null;
            behandler.Status = BehandlerStatus.Aktiv;
            behandler.ArkivertUtc = null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GjenopprettSlettetBehandler",
                nameof(Behandler), behandler.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage(new { visSlettede = true });
    }
}
