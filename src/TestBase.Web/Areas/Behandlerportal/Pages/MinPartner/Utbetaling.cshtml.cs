using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Utbetaling;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.MinPartner;

/// <summary>
/// Partnerens egen Stripe Connect-tilkobling for månedlig utbetaling av
/// partnerandel — samme mønster som Behandlerportal/Utbetaling.cshtml.cs, men
/// for partneren som helhet (ÉN konto per Partner, ikke per behandler). Fikk
/// to flere faner (2026-10-11, "Periodiserte oppgjør, del 2"): "Partneroppgjør"
/// (periodisert rapport fra OkonomiRapportService) og "Oppsett" (partnerens
/// egen kontoplan-etikett/-konto). Folder er nå kun "PartnerMedlemOmrade"-
/// beskyttet (se Program.cs — åpen for ENHVER i klinikken, se
/// "Klinikk-oppdatering" i docs/beslutningslogg.md), så hver handler under
/// sjekker EKSPLISITT ErPartnerAdministrator selv — denne siden er fortsatt
/// klinikk-admin-only, bare ikke lenger håndhevet på folder-nivå alene.
/// </summary>
public sealed class UtbetalingModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UtbetalingsOnboardingService _onboarding;
    private readonly OkonomiRapportService _rapportService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _auditLogger;

    public UtbetalingModel(
        AppDbContext db, UtbetalingsOnboardingService onboarding, OkonomiRapportService rapportService,
        ICurrentUserContext currentUser, IAuditLogger auditLogger)
    {
        _db = db;
        _onboarding = onboarding;
        _rapportService = rapportService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public UtbetalingsMottakerKonto? Konto { get; private set; }
    public string? Feilmelding { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int? Aar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Maaned { get; set; }

    public int ValgtAar { get; private set; }
    public int ValgtManed { get; private set; }
    public PartnerOkonomiRapport Rapport { get; private set; } = null!;

    public Partner Oppsett { get; private set; } = null!;
    public bool OppsettLagret { get; private set; }
    public bool ErPartnerAdministrator => _currentUser.ErPartnerAdministrator;
    public int AntallTilgjengeligeTester { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null || !_currentUser.ErPartnerAdministrator)
        {
            return Forbid();
        }

        var partnerId = _currentUser.PartnerId.Value;
        Konto = await _onboarding.HentForPartnerAsync(partnerId, cancellationToken);
        AntallTilgjengeligeTester = await _db.PartnerTestTilganger.CountAsync(t => t.PartnerId == partnerId, cancellationToken);

        var naa = DateTimeOffset.UtcNow;
        ValgtAar = Aar ?? naa.Year;
        ValgtManed = Maaned ?? naa.Month;
        Rapport = await _rapportService.HentPartnerRapportAsync(partnerId, ValgtAar, ValgtManed, cancellationToken);

        Oppsett = await _db.Partnere.FindAsync([partnerId], cancellationToken) ?? throw new InvalidOperationException("Fant ikke partner.");
        OppsettLagret = TempData["OppsettLagret"] as bool? ?? false;
        return Page();
    }

    public async Task<IActionResult> OnPostStartOnboardingAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null || !_currentUser.ErPartnerAdministrator)
        {
            return Forbid();
        }

        var partnerId = _currentUser.PartnerId.Value;
        var partner = await _db.Partnere.FindAsync([partnerId], cancellationToken);
        if (partner is null)
        {
            return Forbid();
        }

        var behandlerId = HentBehandlerId();
        var behandler = await _db.Behandlere.FindAsync([behandlerId], cancellationToken);
        var epost = partner.KontaktEpost ?? behandler?.Email ?? string.Empty;

        var returUrl = Url.Page("/MinPartner/Utbetaling", pageHandler: null, values: null, protocol: Request.Scheme) ?? string.Empty;
        var lenke = await _onboarding.StartOnboardingAsync(
            MottakerType.Partner, partnerId, epost, returUrl, returUrl, cancellationToken);

        if (lenke is null)
        {
            Feilmelding = "Kunne ikke starte tilkobling til Stripe akkurat nå. Prøv igjen senere.";
            Konto = await _onboarding.HentForPartnerAsync(partnerId, cancellationToken);
            return Page();
        }

        return Redirect(lenke);
    }

    public async Task<IActionResult> OnPostLagreOppsettAsync(
        string? organisasjonsnummer, string? kontonummer, string? partnerandelEtikett, string? partnerandelKonto, CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null || !_currentUser.ErPartnerAdministrator)
        {
            return Forbid();
        }

        var partnerId = _currentUser.PartnerId.Value;
        var partner = await _db.Partnere.FindAsync([partnerId], cancellationToken);
        if (partner is null)
        {
            return Forbid();
        }

        partner.Organisasjonsnummer = string.IsNullOrWhiteSpace(organisasjonsnummer) ? null : organisasjonsnummer.Trim();
        partner.Kontonummer = string.IsNullOrWhiteSpace(kontonummer) ? null : kontonummer.Trim();
        partner.PartnerandelEtikett = string.IsNullOrWhiteSpace(partnerandelEtikett) ? null : partnerandelEtikett.Trim();
        partner.PartnerandelKonto = string.IsNullOrWhiteSpace(partnerandelKonto) ? null : partnerandelKonto.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "LagrePartnerOkonomiOppsett",
            nameof(Partner), partnerId.ToString(), cancellationToken: cancellationToken);

        TempData["OppsettLagret"] = true;
        return Redirect(Url.Page("./Utbetaling")! + "#oppsett");
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
