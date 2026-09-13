using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;
    private readonly ICaptchaProvider _captcha;

    public IndexModel(AppDbContext db, TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser, ICaptchaProvider captcha)
    {
        _db = db;
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _captcha = captcha;
    }

    public string CaptchaSporsmal { get; private set; } = string.Empty;

    [BindProperty]
    public string CaptchaSignertFasit { get; set; } = string.Empty;

    [BindProperty]
    public string? CaptchaSvar { get; set; }

    public string? Feilmelding { get; private set; }

    public sealed record PasientRad(Pasient Pasient, string? BehandlerNavn, int Tildelt, int Besvart);

    public List<PasientRad> Rader { get; private set; } = new();

    /// <summary>
    /// True for en partner-admin — da vises ALLE pasienter for ALLE behandlere
    /// i partnerskapet (med Behandler-kolonne), ikke bare egne, samme prinsipp
    /// som Admin/Pasienter viser på tvers av alle behandlere. En vanlig
    /// partner-tilknyttet (men ikke partner-admin) behandler ser fortsatt kun
    /// egne pasienter.
    /// </summary>
    public bool ViserHelePartnerskapet { get; private set; }
    public string? PartnerNavn { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViserHelePartnerskapet = _currentUser.ErPartnerAdministrator && _currentUser.PartnerId is not null;

        List<Pasient> pasienter;
        Dictionary<long, string?> behandlerNavnById = new();

        if (ViserHelePartnerskapet)
        {
            var partnerId = _currentUser.PartnerId!.Value;
            var behandlereIPartner = await _db.Behandlere.Where(b => b.PartnerId == partnerId).ToListAsync(cancellationToken);
            behandlerNavnById = behandlereIPartner.ToDictionary(b => b.Id, b => b.Visningsnavn);
            var behandlerIder = behandlereIPartner.Select(b => b.Id).ToList();

            pasienter = await _db.Pasienter
                .Where(p => behandlerIder.Contains(p.BehandlerId) && !p.ErSlettet)
                .OrderByDescending(p => p.OpprettetUtc)
                .ToListAsync(cancellationToken);

            PartnerNavn = (await _db.Partnere.FirstOrDefaultAsync(p => p.Id == partnerId, cancellationToken))?.Navn;
        }
        else
        {
            var behandlerId = HentBehandlerId();
            pasienter = await _db.Pasienter
                .Where(p => p.BehandlerId == behandlerId && !p.ErSlettet)
                .OrderByDescending(p => p.OpprettetUtc)
                .ToListAsync(cancellationToken);
        }

        var tellinger = await _testService.HentTildelingTellingerAsync(pasienter.Select(p => p.Id).ToList(), cancellationToken);
        Rader = pasienter.Select(p =>
        {
            var telling = tellinger.GetValueOrDefault(p.Id, new TestService.TildelingTelling(0, 0));
            return new PasientRad(p, behandlerNavnById.GetValueOrDefault(p.BehandlerId), telling.Tildelt, telling.Besvart);
        }).ToList();

        var utfordring = _captcha.LagUtfordring();
        CaptchaSporsmal = utfordring.SporsmalTekst;
        CaptchaSignertFasit = utfordring.SignertFasit;
    }

    public async Task<IActionResult> OnPostArkiverAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id && p.BehandlerId == behandlerId, cancellationToken);
        if (pasient is not null)
        {
            var arkiveres = pasient.Status != PasientStatus.Arkivert;
            // Gjenopprettes til Invitert, ikke Aktiv — ingen pasient kan bli Aktiv før
            // Del 4 bygger pasientens egen fullføringsside (se PasientStatus).
            pasient.Status = arkiveres ? PasientStatus.Arkivert : PasientStatus.Invitert;
            pasient.ArkivertUtc = arkiveres ? DateTimeOffset.UtcNow : null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(),
                arkiveres ? "ArkiverPasient" : "GjenopprettPasient",
                nameof(Pasient), pasient.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    /// <summary>Kun mulig når pasienten allerede er arkivert — håndhevet server-side.</summary>
    public async Task<IActionResult> OnPostSlettAsync(long id, CancellationToken cancellationToken)
    {
        if (!_captcha.Verifiser(CaptchaSignertFasit, CaptchaSvar))
        {
            // Se Administratorer/Index.cshtml.cs for hvorfor ModelState.Clear() må skje
            // FØR OnGetAsync gjenoppfrisker CaptchaSporsmal/CaptchaSignertFasit.
            ModelState.Clear();
            await OnGetAsync(cancellationToken);
            Feilmelding = "Feil svar på sikkerhetsspørsmålet — sletting avbrutt.";
            return Page();
        }

        var behandlerId = HentBehandlerId();
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id && p.BehandlerId == behandlerId, cancellationToken);
        if (pasient is not null && pasient.Status == PasientStatus.Arkivert)
        {
            pasient.ErSlettet = true;
            pasient.SlettetUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "SlettPasient",
                nameof(Pasient), pasient.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
