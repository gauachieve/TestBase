using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly PartnerAutomatiskUtsendelseService _automatiskUtsendelse;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;
    private readonly ICaptchaProvider _captcha;

    public IndexModel(
        AppDbContext db, TestService testService, PartnerAutomatiskUtsendelseService automatiskUtsendelse,
        IAuditLogger auditLogger, ICurrentUserContext currentUser, ICaptchaProvider captcha)
    {
        _db = db;
        _testService = testService;
        _automatiskUtsendelse = automatiskUtsendelse;
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
    /// Satt fra "Pasienter i min klinikk"-knappen (_MinKlinikkNav.cshtml) — tvinger EGNE
    /// pasienter selv for en klinikk-admin, se "Klinikk-oppdatering, del 4" i
    /// docs/beslutningslogg.md. Den vanlige topp-nav "Pasienter"-lenken (uten dette
    /// parameteret) beholder den opprinnelige, bredere ViserHelePartnerskapet-oppførselen
    /// uendret (bulk-flytt-funksjonen under er fortsatt avhengig av den).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool KunEgne { get; set; }

    /// <summary>
    /// True for en partner-admin (og ikke eksplisitt bedt om KunEgne) — da vises ALLE pasienter
    /// for ALLE behandlere i partnerskapet (med Behandler-kolonne), ikke bare egne, samme prinsipp
    /// som Admin/Pasienter viser på tvers av alle behandlere. En vanlig
    /// partner-tilknyttet (men ikke partner-admin) behandler ser fortsatt kun
    /// egne pasienter.
    /// </summary>
    public bool ViserHelePartnerskapet { get; private set; }
    public string? PartnerNavn { get; private set; }
    public bool ErPartnerAdministrator => _currentUser.ErPartnerAdministrator;
    public bool ErPartnerMedlem => _currentUser.PartnerId is not null;
    public int AntallTilgjengeligeTester { get; private set; }

    /// <summary>Bugliste 2026-10-11 punkt 4 — ALLE behandlere i klinikken, for "Bytt behandler for valgte"-nedtrekksmenyen. Tom/ubrukt når !ViserHelePartnerskapet.</summary>
    public IReadOnlyList<Behandler> BehandlereIKlinikken { get; private set; } = Array.Empty<Behandler>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViserHelePartnerskapet = _currentUser.ErPartnerAdministrator && _currentUser.PartnerId is not null && !KunEgne;

        if (_currentUser.PartnerId is not null)
        {
            AntallTilgjengeligeTester = await _db.PartnerTestTilganger.CountAsync(t => t.PartnerId == _currentUser.PartnerId, cancellationToken);
        }

        List<Pasient> pasienter;
        Dictionary<long, string?> behandlerNavnById = new();

        if (ViserHelePartnerskapet)
        {
            var partnerId = _currentUser.PartnerId!.Value;
            var behandlereIPartner = await _db.Behandlere.Where(b => b.PartnerId == partnerId).ToListAsync(cancellationToken);
            behandlerNavnById = behandlereIPartner.ToDictionary(b => b.Id, b => b.Visningsnavn);
            var behandlerIder = behandlereIPartner.Select(b => b.Id).ToList();
            BehandlereIKlinikken = behandlereIPartner.OrderBy(b => b.Etternavn).ToList();

            pasienter = await _db.Pasienter
                .Include(p => p.Gruppe)
                .Where(p => behandlerIder.Contains(p.BehandlerId) && !p.ErSlettet)
                .OrderByDescending(p => p.OpprettetUtc)
                .ToListAsync(cancellationToken);

            PartnerNavn = (await _db.Partnere.FirstOrDefaultAsync(p => p.Id == partnerId, cancellationToken))?.Navn;
        }
        else
        {
            var behandlerId = HentBehandlerId();
            pasienter = await _db.Pasienter
                .Include(p => p.Gruppe)
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

    /// <summary>
    /// Arkiverer flere pasienter i ett steg (bugliste 2026-10-10 punkt 6) — i
    /// motsetning til enkelt-raden sin OnPostArkiverAsync er dette ALDRI en
    /// vippe: allerede arkiverte pasienter i utvalget hoppes stille over,
    /// aldri gjenopprettet, siden en blandet batch (noen arkiverte, noen
    /// ikke) ellers ville gitt uforutsigbare per-rad-utfall.
    /// </summary>
    public async Task<IActionResult> OnPostArkiverValgteAsync(long[] pasientId, CancellationToken cancellationToken)
    {
        if (pasientId.Length == 0)
        {
            return RedirectToPage(new { KunEgne });
        }

        var behandlerId = HentBehandlerId();
        var pasienter = await _db.Pasienter
            .Where(p => pasientId.Contains(p.Id) && p.BehandlerId == behandlerId && p.Status != PasientStatus.Arkivert)
            .ToListAsync(cancellationToken);

        foreach (var pasient in pasienter)
        {
            pasient.Status = PasientStatus.Arkivert;
            pasient.ArkivertUtc = DateTimeOffset.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);

        if (pasienter.Count > 0)
        {
            var ider = pasienter.Select(p => p.Id).ToList();
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "ArkiverFlerePasienter",
                nameof(Pasient), AuditBatch.EntityId(ider), details: $"PasientIder {string.Join(",", ider)}", cancellationToken: cancellationToken);

            // Bugliste 2026-10-11 punkt 4 — klinikkens "ved Avsluttet"-liste, se Ny.cshtml.cs sitt motstykke.
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            foreach (var pasient in pasienter)
            {
                await _automatiskUtsendelse.UtforTriggerAsync(pasient, PartnerAutomatiskUtsendelseUtloser.VedAvsluttet, baseUrl, cancellationToken);
            }
        }

        return RedirectToPage(new { KunEgne });
    }

    /// <summary>
    /// Bugliste 2026-10-11 punkt 4 — klinikk-admin kan masseflytte en liste pasienter til en annen
    /// behandler i SAMME klinikk i ett steg (samme flervalg-infrastruktur som
    /// OnPostArkiverValgteAsync, men med en ekstra mål-behandler-verdi). KUN partner-admin — en
    /// vanlig behandler ser aldri denne knappen (ViserHelePartnerskapet styrer visning i viewet),
    /// håndhevet HER også, ikke bare i viewet (samme prinsipp som alle andre AuthorizeAreaFolder-
    /// uavhengige sjekker i dette prosjektet). I MOTSETNING til OnPostArkiverValgteAsync filtreres
    /// IKKE til EGNE pasienter — poenget med denne knappen er nettopp å flytte ANDRE behandleres
    /// pasienter, avgrenset til SAMME klinikk i stedet (nyBehandlerId sin PartnerId MÅ matche).
    /// </summary>
    public async Task<IActionResult> OnPostByttBehandlerValgteAsync(long[] pasientId, long nyBehandlerId, CancellationToken cancellationToken)
    {
        if (!_currentUser.ErPartnerAdministrator || _currentUser.PartnerId is null || pasientId.Length == 0)
        {
            return RedirectToPage(new { KunEgne });
        }

        var partnerId = _currentUser.PartnerId.Value;
        var nyBehandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == nyBehandlerId && b.PartnerId == partnerId, cancellationToken);
        if (nyBehandler is null)
        {
            Feilmelding = "Ugyldig valg av ny behandler.";
            await OnGetAsync(cancellationToken);
            return Page();
        }

        var behandlerIderIKlinikken = await _db.Behandlere.Where(b => b.PartnerId == partnerId).Select(b => b.Id).ToListAsync(cancellationToken);
        var pasienter = await _db.Pasienter
            .Where(p => pasientId.Contains(p.Id) && behandlerIderIKlinikken.Contains(p.BehandlerId) && p.Status != PasientStatus.Arkivert)
            .ToListAsync(cancellationToken);

        foreach (var pasient in pasienter)
        {
            pasient.BehandlerId = nyBehandlerId;
        }
        await _db.SaveChangesAsync(cancellationToken);

        if (pasienter.Count > 0)
        {
            var ider = pasienter.Select(p => p.Id).ToList();
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "ByttBehandlerFlerePasienter",
                nameof(Pasient), AuditBatch.EntityId(ider), details: $"PasientIder {string.Join(",", ider)}; NyBehandlerId {nyBehandlerId}", cancellationToken: cancellationToken);
        }

        return RedirectToPage(new { KunEgne });
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

            // Bugliste 2026-10-11 punkt 4 — KUN ved faktisk arkivering, ikke ved gjenopptak.
            if (arkiveres)
            {
                await _automatiskUtsendelse.UtforTriggerAsync(
                    pasient, PartnerAutomatiskUtsendelseUtloser.VedAvsluttet, $"{Request.Scheme}://{Request.Host}", cancellationToken);
            }
        }

        return RedirectToPage(new { KunEgne });
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

        return RedirectToPage(new { KunEgne });
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
