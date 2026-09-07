using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages.Tester;

/// <summary>
/// Vipps/Stripe sender pasienten tilbake hit etter et betalingsforsøk.
/// Bekreftelsen skjer ved å SPØRRE leverandøren om status på nytt — ALDRI ved
/// å stole på at brukeren kom tilbake alene. Dette er en synkron FALLBACK til
/// webhooken (Security/PaymentWebhooks.cs), som er den autoritative kilden —
/// spesielt viktig for Vipps siden webhook-signaturen ikke er live-verifisert
/// ennå, se docs/beslutningslogg.md. MarkerBetalingBetaltAsync er idempotent,
/// så det spiller ingen rolle om webhooken eller denne siden kommer først.
/// </summary>
[Authorize(Policy = "PasientOmrade")]
public sealed class BetalResultatModel : PageModel
{
    private readonly TestService _testService;
    private readonly IVippsClient _vipps;
    private readonly IStripeClient _stripe;
    private readonly ICurrentUserContext _currentUser;

    public BetalResultatModel(TestService testService, IVippsClient vipps, IStripeClient stripe, ICurrentUserContext currentUser)
    {
        _testService = testService;
        _vipps = vipps;
        _stripe = stripe;
        _currentUser = currentUser;
    }

    public long TildelingId { get; private set; }
    public bool ErBetalt { get; private set; }
    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null || innhold.Tildeling.PasientId != HentPasientId())
        {
            return NotFound();
        }

        TildelingId = id;
        var betaling = await _testService.HentBetalingAsync(id, cancellationToken);
        if (betaling is null)
        {
            return NotFound();
        }

        if (betaling.Status == BetalingStatus.Betalt)
        {
            ErBetalt = true;
            return Page();
        }

        if (betaling.Metode is null || betaling.BetalingsleverandorReferanse is null)
        {
            Feilmelding = "Fant ikke noe betalingsforsøk å sjekke status på.";
            return Page();
        }

        var erBetaltHosLeverandor = betaling.Metode switch
        {
            BetalingMetode.Vipps => (await _vipps.HentStatusAsync(betaling.BetalingsleverandorReferanse, cancellationToken)).Status
                is VippsBetalingsstatus.Autorisert or VippsBetalingsstatus.Fanget,
            BetalingMetode.Stripe => (await _stripe.HentStatusAsync(betaling.BetalingsleverandorReferanse, cancellationToken)).ErBetalt,
            _ => false
        };

        if (erBetaltHosLeverandor)
        {
            await _testService.MarkerBetalingBetaltAsync(id, betaling.Metode.Value, betaling.BetalingsleverandorReferanse, cancellationToken);
            ErBetalt = true;
        }
        else
        {
            Feilmelding = "Betalingen er ikke bekreftet ennå. Prøv igjen om et øyeblikk, eller gå tilbake og forsøk på nytt.";
        }

        return Page();
    }

    private long HentPasientId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
