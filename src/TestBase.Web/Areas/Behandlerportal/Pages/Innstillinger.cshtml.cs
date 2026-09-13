using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages;

/// <summary>
/// Behandlers varslingsinnstillinger — i dag kun den daglige påminnelsen om
/// ugodkjente fullførte rapporter (se PaaminnelseService og
/// DagligPaaminnelseBakgrunnstjeneste i TestBase.Web). "Send test-påminnelse
/// nå" lar behandler se meldingen med det samme i dev, samme prinsipp som
/// "Regenerer innebygde tester" i Admin/Tester/Index.
/// </summary>
[Authorize(Policy = "BehandlerOmrade")]
public sealed class InnstillingerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly PaaminnelseService _paaminnelseService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ICaptchaProvider _captcha;
    private readonly IAuditLogger _auditLogger;

    public InnstillingerModel(AppDbContext db, PaaminnelseService paaminnelseService, ICurrentUserContext currentUser, ICaptchaProvider captcha, IAuditLogger auditLogger)
    {
        _db = db;
        _paaminnelseService = paaminnelseService;
        _currentUser = currentUser;
        _captcha = captcha;
        _auditLogger = auditLogger;
    }

    public string CaptchaSporsmal { get; private set; } = string.Empty;

    [BindProperty]
    public string CaptchaSignertFasit { get; set; } = string.Empty;

    [BindProperty]
    public string? CaptchaSvar { get; set; }

    [BindProperty]
    public bool OnskerDagligPaaminnelse { get; set; }

    [BindProperty]
    public Varslingspreferanse PaaminnelseKanal { get; set; } = Varslingspreferanse.Begge;

    public DateTimeOffset? SistPaaminnetUtc { get; private set; }
    public string? Melding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var behandler = await HentBehandlerAsync(cancellationToken);
        if (behandler is null)
        {
            return;
        }

        OnskerDagligPaaminnelse = behandler.OnskerDagligPaaminnelse;
        PaaminnelseKanal = behandler.PaaminnelseKanal;
        SistPaaminnetUtc = behandler.SistPaaminnetUtc;

        var utfordring = _captcha.LagUtfordring();
        CaptchaSporsmal = utfordring.SporsmalTekst;
        CaptchaSignertFasit = utfordring.SignertFasit;
    }

    public async Task<IActionResult> OnPostLagreAsync(CancellationToken cancellationToken)
    {
        var behandler = await HentBehandlerAsync(cancellationToken);
        if (behandler is null)
        {
            return NotFound();
        }

        behandler.OnskerDagligPaaminnelse = OnskerDagligPaaminnelse;
        behandler.PaaminnelseKanal = PaaminnelseKanal;
        await _db.SaveChangesAsync(cancellationToken);

        Melding = "Lagret.";
        SistPaaminnetUtc = behandler.SistPaaminnetUtc;
        return Page();
    }

    public async Task<IActionResult> OnPostSendNaaAsync(CancellationToken cancellationToken)
    {
        var behandler = await HentBehandlerAsync(cancellationToken);
        if (behandler is null)
        {
            return NotFound();
        }

        OnskerDagligPaaminnelse = behandler.OnskerDagligPaaminnelse;
        PaaminnelseKanal = behandler.PaaminnelseKanal;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sendt = await _paaminnelseService.SendTilEnkeltBehandlerAsync(behandler.Id, baseUrl, cancellationToken);
        Melding = sendt
            ? "Påminnelse sendt (mock — se konsollen for SMS/e-post-innhold, eller sjekk kanalvalget ditt under)."
            : "Ingen fullførte tester venter på godkjenning akkurat nå — ingenting å sende.";

        SistPaaminnetUtc = behandler.SistPaaminnetUtc;
        return Page();
    }

    /// <summary>
    /// Selvbetjent kontosletting (bugliste 2026-09-13 punkt 6) — går rett til
    /// slettet (samme sluttilstand som admin sin arkiver+slett i to steg),
    /// siden brukeren selv ber om å bli borte umiddelbart. Logger ut med det
    /// samme; kontoen forblir synlig for Superadmin (samme skjulingsmodell som
    /// admin-utført sletting), som kan gjenopprette den ved en feil.
    /// </summary>
    public async Task<IActionResult> OnPostSlettMinKontoAsync(CancellationToken cancellationToken)
    {
        var behandler = await HentBehandlerAsync(cancellationToken);
        if (behandler is null)
        {
            return NotFound();
        }

        if (!_captcha.Verifiser(CaptchaSignertFasit, CaptchaSvar))
        {
            // Se Administratorer/Index.cshtml.cs for hvorfor ModelState.Clear() må skje
            // FØR OnGetAsync gjenoppfrisker CaptchaSporsmal/CaptchaSignertFasit.
            ModelState.Clear();
            await OnGetAsync(cancellationToken);
            Melding = "Feil svar på sikkerhetsspørsmålet — sletting avbrutt.";
            return Page();
        }

        behandler.Status = BehandlerStatus.Arkivert;
        behandler.ArkivertUtc = DateTimeOffset.UtcNow;
        behandler.ErSlettet = true;
        behandler.SlettetUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "SlettEgenBehandlerKonto",
            nameof(Behandler), behandler.Id.ToString(), cancellationToken: cancellationToken);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index", new { area = "" });
    }

    private async Task<Behandler?> HentBehandlerAsync(CancellationToken cancellationToken)
    {
        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
        return await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
    }
}
