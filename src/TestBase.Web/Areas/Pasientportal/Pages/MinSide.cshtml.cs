using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages;

[Authorize(Policy = "PasientOmrade")]
public sealed class MinSideModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ICaptchaProvider _captcha;
    private readonly IAuditLogger _auditLogger;

    public MinSideModel(AppDbContext db, TestService testService, ICurrentUserContext currentUser, ICaptchaProvider captcha, IAuditLogger auditLogger)
    {
        _db = db;
        _testService = testService;
        _currentUser = currentUser;
        _captcha = captcha;
        _auditLogger = auditLogger;
    }

    public sealed record TildeltTestRad(TestTildeling Tildeling, string TestNavn);

    public List<TildeltTestRad> Tildelinger { get; private set; } = new();
    public string CaptchaSporsmal { get; private set; } = string.Empty;
    public string? Feilmelding { get; private set; }

    [BindProperty]
    public string CaptchaSignertFasit { get; set; } = string.Empty;

    [BindProperty]
    public string? CaptchaSvar { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var pasientId = HentPasientId();
        var tildelinger = await _testService.HentTildelingerForPasientAsync(pasientId, cancellationToken);

        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var testNavn = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);

        Tildelinger = tildelinger.Select(t => new TildeltTestRad(t, testNavn.GetValueOrDefault(t.TestId, "(ukjent test)"))).ToList();

        var utfordring = _captcha.LagUtfordring();
        CaptchaSporsmal = utfordring.SporsmalTekst;
        CaptchaSignertFasit = utfordring.SignertFasit;
    }

    /// <summary>Selvbetjent kontosletting (bugliste 2026-09-13 punkt 6) — se Behandlerportal/Innstillinger sitt motstykke for full begrunnelse.</summary>
    public async Task<IActionResult> OnPostSlettMinKontoAsync(CancellationToken cancellationToken)
    {
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == HentPasientId(), cancellationToken);
        if (pasient is null)
        {
            return NotFound();
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

        pasient.Status = PasientStatus.Arkivert;
        pasient.ArkivertUtc = DateTimeOffset.UtcNow;
        pasient.ErSlettet = true;
        pasient.SlettetUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "SlettEgenPasientKonto",
            nameof(Pasient), pasient.Id.ToString(), cancellationToken: cancellationToken);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index", new { area = "" });
    }

    private long HentPasientId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
