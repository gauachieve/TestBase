using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Utbetaling;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages;

/// <summary>
/// Behandlerens egen Stripe Connect-tilkobling for månedlig honorarutbetaling
/// — se docs/beslutningslogg.md "Monthly Stripe Connect payout system".
/// Status vises optimistisk rett etter retur fra Stripe sin onboarding, men
/// regnes KUN som bekreftet fullført etter account.updated-webhooken (se
/// UtbetalingsOnboardingService.OppdaterFraWebhookAsync) — denne siden stoler
/// aldri på retur-URL-en alene.
/// </summary>
[Authorize(Policy = "BehandlerOmrade")]
public sealed class UtbetalingModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UtbetalingsOnboardingService _onboarding;
    private readonly ICurrentUserContext _currentUser;

    public UtbetalingModel(AppDbContext db, UtbetalingsOnboardingService onboarding, ICurrentUserContext currentUser)
    {
        _db = db;
        _onboarding = onboarding;
        _currentUser = currentUser;
    }

    public UtbetalingsMottakerKonto? Konto { get; private set; }
    public string? Feilmelding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Konto = await _onboarding.HentForBehandlerAsync(HentBehandlerId(), cancellationToken);
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

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
