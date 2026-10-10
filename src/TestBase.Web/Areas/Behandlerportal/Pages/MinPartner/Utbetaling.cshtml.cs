using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Utbetaling;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.MinPartner;

/// <summary>
/// Partnerens egen Stripe Connect-tilkobling for månedlig utbetaling av
/// partnerandel — samme mønster som Behandlerportal/Utbetaling.cshtml.cs, men
/// for partneren som helhet (ÉN konto per Partner, ikke per behandler). Folder
/// allerede "PartnerAdminOmrade"-beskyttet i Program.cs sin
/// AuthorizeAreaFolder-liste, ingen egen [Authorize] nødvendig her.
/// </summary>
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

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        Konto = await _onboarding.HentForPartnerAsync(_currentUser.PartnerId.Value, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostStartOnboardingAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
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

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
