using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages;

/// <summary>
/// Administratorens eneste selvbetjeningsside i dag — kontosletting (bugliste
/// 2026-09-13 punkt 6). Admin har ingen "Min side" ennå (kommer først med
/// oppgave-sammenslåingen, se docs/beslutningslogg.md), så dette er en egen,
/// minimal side i stedet.
/// </summary>
[Authorize(Policy = "AdminOmrade")]
public sealed class MinKontoModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly ICaptchaProvider _captcha;
    private readonly IAuditLogger _auditLogger;

    public MinKontoModel(AppDbContext db, ICurrentUserContext currentUser, ICaptchaProvider captcha, IAuditLogger auditLogger)
    {
        _db = db;
        _currentUser = currentUser;
        _captcha = captcha;
        _auditLogger = auditLogger;
    }

    public string CaptchaSporsmal { get; private set; } = string.Empty;
    public string? Feilmelding { get; private set; }
    public bool ErSuperadmin { get; private set; }

    [BindProperty]
    public string CaptchaSignertFasit { get; set; } = string.Empty;

    [BindProperty]
    public string? CaptchaSvar { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var administrator = await HentAdministratorAsync(cancellationToken);
        ErSuperadmin = administrator?.ErSuperadmin ?? false;

        var utfordring = _captcha.LagUtfordring();
        CaptchaSporsmal = utfordring.SporsmalTekst;
        CaptchaSignertFasit = utfordring.SignertFasit;
    }

    /// <summary>Se Behandlerportal/Innstillinger sitt motstykke for full begrunnelse — samme mønster.</summary>
    public async Task<IActionResult> OnPostSlettMinKontoAsync(CancellationToken cancellationToken)
    {
        var administrator = await HentAdministratorAsync(cancellationToken);
        if (administrator is null)
        {
            return NotFound();
        }

        // Se docs/beslutningslogg.md "Produksjonsutfall: krasj ved oppstart pga.
        // arkivert seed-admin-konto" — Superadmin-kontoen kan derfor ALDRI slette
        // seg selv her, samme sperre som i Administratorer/Index.cshtml.cs.
        if (administrator.ErSuperadmin)
        {
            await OnGetAsync(cancellationToken);
            Feilmelding = "Superadmin-kontoen kan ikke slettes.";
            return Page();
        }

        if (!_captcha.Verifiser(CaptchaSignertFasit, CaptchaSvar))
        {
            // Se Administratorer/Index.cshtml.cs for hvorfor ModelState.Clear() må skje
            // FØR OnGetAsync gjenoppfrisker CaptchaSporsmal/CaptchaSignertFasit.
            ModelState.Clear();
            await OnGetAsync(cancellationToken);
            Feilmelding = "Feil svar på sikkerhetsspørsmålet — sletting avbrutt.";
            return Page();
        }

        administrator.ErArkivert = true;
        administrator.ArkivertUtc = DateTimeOffset.UtcNow;
        administrator.ErSlettet = true;
        administrator.SlettetUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "SlettEgenAdministratorKonto",
            nameof(Administrator), administrator.Id.ToString(), cancellationToken: cancellationToken);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index", new { area = "" });
    }

    private async Task<Administrator?> HentAdministratorAsync(CancellationToken cancellationToken)
    {
        var administratorId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
        return await _db.Administratorer.FirstOrDefaultAsync(a => a.Id == administratorId, cancellationToken);
    }
}
