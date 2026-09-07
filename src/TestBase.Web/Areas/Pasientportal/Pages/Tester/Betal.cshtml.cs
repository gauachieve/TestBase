using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages.Tester;

/// <summary>
/// Betalingssteget som gater /Pasientportal/Tester/Fyll når en tildeling
/// krever betaling (se TestTildelingBetaling.Status), jf. kravdokumentet:
/// "systemet må få bekreftelse for betalt... for å fortsette med
/// innfylling". Gjenbruker EXAKT samme mønster som /BetalingTest (allerede
/// verifisert ende-til-ende for Stripe) — bare med en ekte TestTildelingId i
/// stedet for en fast diagnostisk sum. Se docs/beslutningslogg.md "Partner
/// System + Test Monetization".
/// </summary>
[Authorize(Policy = "PasientOmrade")]
public sealed class BetalModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly IVippsClient _vipps;
    private readonly IStripeClient _stripe;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserContext _currentUser;

    public BetalModel(
        AppDbContext db, TestService testService, IVippsClient vipps, IStripeClient stripe,
        IConfiguration configuration, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _vipps = vipps;
        _stripe = stripe;
        _configuration = configuration;
        _currentUser = currentUser;
    }

    public long TildelingId { get; private set; }
    public string TestNavn { get; private set; } = string.Empty;
    public decimal TotalprisKr { get; private set; }
    public bool VippsTilgjengelig { get; private set; }
    public bool StripeTilgjengelig { get; private set; }
    public string? StripeClientSecret { get; private set; }
    public string? StripePublishableKey { get; private set; }
    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var lastResultat = await LastBetalingAsync(id, cancellationToken);
        if (lastResultat is not null)
        {
            return lastResultat;
        }

        StripePublishableKey = _configuration["Stripe:PublishableKey"];
        VippsTilgjengelig = !string.IsNullOrWhiteSpace(_configuration["Vipps:ClientId"]);
        StripeTilgjengelig = !string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]) && !string.IsNullOrWhiteSpace(StripePublishableKey);

        if (StripeTilgjengelig)
        {
            var referanse = PaymentWebhookReferanse(id);
            var resultat = await _stripe.OpprettBetalingsintensjonAsync(TotalprisKr, $"Test: {TestNavn}", referanse, cancellationToken);
            if (resultat.Success && resultat.ClientSecret is not null)
            {
                StripeClientSecret = resultat.ClientSecret;
                await OppdaterBetalingsforsokAsync(id, BetalingMetode.Stripe, resultat.BetalingsId, cancellationToken);
            }
            else
            {
                StripeTilgjengelig = false;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostVippsAsync(long id, CancellationToken cancellationToken)
    {
        var lastResultat = await LastBetalingAsync(id, cancellationToken);
        if (lastResultat is not null)
        {
            return lastResultat;
        }

        var referanse = PaymentWebhookReferanse(id);
        var returUrl = Url.Page("/Tester/BetalResultat", pageHandler: null, values: new { area = "Pasientportal", id }, protocol: Request.Scheme)
            ?? throw new InvalidOperationException("Klarte ikke å generere returUrl for BetalResultat.");

        var resultat = await _vipps.OpprettBetalingAsync(referanse, TotalprisKr, $"Test: {TestNavn}", returUrl, cancellationToken);
        if (!resultat.Success || resultat.RedirectUrl is null)
        {
            Feilmelding = resultat.ErrorMessage ?? "Vipps-betaling feilet.";
            return Page();
        }

        await OppdaterBetalingsforsokAsync(id, BetalingMetode.Vipps, referanse, cancellationToken);
        return Redirect(resultat.RedirectUrl);
    }

    /// <summary>
    /// Laster og validerer tildelingen. Returnerer et IActionResult å svare med
    /// hvis noe er galt/ferdig (NotFound, eller redirect videre til Fyll når
    /// betaling ikke — eller ikke lenger — er påkrevd), ellers null når
    /// kalleren skal fortsette normalt (TildelingId/TestNavn/TotalprisKr er da satt).
    /// </summary>
    private async Task<IActionResult?> LastBetalingAsync(long id, CancellationToken cancellationToken)
    {
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null || innhold.Tildeling.PasientId != HentPasientId())
        {
            return NotFound();
        }

        var betaling = await _testService.HentBetalingAsync(id, cancellationToken);
        if (betaling is null || betaling.Status != BetalingStatus.Venter)
        {
            return RedirectToPage("Fyll", new { id });
        }

        TildelingId = id;
        TestNavn = innhold.Test.Navn;
        TotalprisKr = betaling.PasientTotalprisKr;
        return null;
    }

    private async Task OppdaterBetalingsforsokAsync(long tildelingId, BetalingMetode metode, string leverandorReferanse, CancellationToken cancellationToken)
    {
        var betaling = await _db.TestTildelingBetalinger.FirstAsync(b => b.TestTildelingId == tildelingId, cancellationToken);
        betaling.Metode = metode;
        betaling.BetalingsleverandorReferanse = leverandorReferanse;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string PaymentWebhookReferanse(long tildelingId) => $"{Security.PaymentWebhooks.TildelingReferansePrefiks}{tildelingId}";

    private long HentPasientId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
