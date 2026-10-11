using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Utbetaling;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages;

/// <summary>
/// Behandlerens egen Stripe Connect-tilkobling for månedlig honorarutbetaling
/// — se docs/beslutningslogg.md "Monthly Stripe Connect payout system". Fikk
/// to flere faner (2026-10-11, "Periodiserte oppgjør, del 2"): "Honorarutskrift"
/// (periodisert rapport fra OkonomiRapportService, det "fine" designforslaget
/// fra 2026-10-10) og "Oppsett" (egen kontoplan-etikett/-konto, HPR/konto).
/// Status for selve Connect-tilkoblingen vises optimistisk rett etter retur
/// fra Stripe sin onboarding, men regnes KUN som bekreftet fullført etter
/// account.updated-webhooken (se UtbetalingsOnboardingService.
/// OppdaterFraWebhookAsync) — denne siden stoler aldri på retur-URL-en alene.
/// </summary>
[Authorize(Policy = "BehandlerOmrade")]
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
    public BehandlerOkonomiRapport Rapport { get; private set; } = null!;

    public Behandler Oppsett { get; private set; } = null!;
    public bool OppsettLagret { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        Konto = await _onboarding.HentForBehandlerAsync(behandlerId, cancellationToken);

        var naa = DateTimeOffset.UtcNow;
        ValgtAar = Aar ?? naa.Year;
        ValgtManed = Maaned ?? naa.Month;
        Rapport = await _rapportService.HentBehandlerRapportAsync(behandlerId, ValgtAar, ValgtManed, cancellationToken);

        Oppsett = await _db.Behandlere.FindAsync([behandlerId], cancellationToken) ?? throw new InvalidOperationException("Fant ikke innlogget behandler.");
        OppsettLagret = TempData["OppsettLagret"] as bool? ?? false;
    }

    public async Task<IActionResult> OnPostStartOnboardingAsync(CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var behandler = await _db.Behandlere.FindAsync([behandlerId], cancellationToken);
        if (behandler is null)
        {
            return Forbid();
        }

        var returUrl = Url.Page("/Utbetaling", pageHandler: null, values: null, protocol: Request.Scheme) ?? string.Empty;
        var lenke = await _onboarding.StartOnboardingAsync(
            MottakerType.Behandler, behandlerId, behandler.Email, returUrl, returUrl, cancellationToken);

        if (lenke is null)
        {
            Feilmelding = "Kunne ikke starte tilkobling til Stripe akkurat nå. Prøv igjen senere.";
            Konto = await _onboarding.HentForBehandlerAsync(behandlerId, cancellationToken);
            return Page();
        }

        return Redirect(lenke);
    }

    public async Task<IActionResult> OnPostLagreOppsettAsync(
        string? hprNr, string? kontonummer, string? honorarinntektEtikett, string? honorarinntektKonto, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var behandler = await _db.Behandlere.FindAsync([behandlerId], cancellationToken);
        if (behandler is null)
        {
            return Forbid();
        }

        behandler.HprNr = string.IsNullOrWhiteSpace(hprNr) ? null : hprNr.Trim();
        behandler.Kontonummer = string.IsNullOrWhiteSpace(kontonummer) ? null : kontonummer.Trim();
        behandler.HonorarinntektEtikett = string.IsNullOrWhiteSpace(honorarinntektEtikett) ? null : honorarinntektEtikett.Trim();
        behandler.HonorarinntektKonto = string.IsNullOrWhiteSpace(honorarinntektKonto) ? null : honorarinntektKonto.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "LagreEgenOkonomiOppsett",
            nameof(Behandler), behandlerId.ToString(), cancellationToken: cancellationToken);

        TempData["OppsettLagret"] = true;
        return Redirect(Url.Page("./Utbetaling")! + "#oppsett");
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
