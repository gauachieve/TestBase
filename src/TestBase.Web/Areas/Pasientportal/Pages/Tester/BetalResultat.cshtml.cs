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
/// ennå, se docs/beslutningslogg.md. For Vipps deles selve "bekreft og fang"-
/// logikken med webhooken via VippsBetalingBekrefterService (idempotent), så
/// det spiller ingen rolle om webhooken eller denne siden kommer først.
/// </summary>
[Authorize(Policy = "PasientOmrade")]
public sealed class BetalResultatModel : PageModel
{
    private readonly TestService _testService;
    private readonly VippsBetalingBekrefterService _vippsBekrefter;
    private readonly IVippsClient _vipps;
    private readonly IStripeClient _stripe;
    private readonly ICurrentUserContext _currentUser;

    public BetalResultatModel(
        TestService testService, VippsBetalingBekrefterService vippsBekrefter, IVippsClient vipps,
        IStripeClient stripe, ICurrentUserContext currentUser)
    {
        _testService = testService;
        _vippsBekrefter = vippsBekrefter;
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
            // Selvhelbredende: en tidligere versjon av denne siden markerte en
            // Vipps-betaling som "Betalt" i VÅR database straks Vipps rapporterte
            // AUTORISERT, UTEN noensinne å fange (capture) beløpet hos Vipps — se
            // VippsBetalingBekrefterService sin doc-kommentar. Et nytt besøk her
            // etter fiksen fanger derfor opp og retter en tidligere ufanget
            // reservasjon også, ikke bare nye betalinger. Går direkte mot
            // IVippsClient (ikke den delte tjenesten) siden raden allerede er
            // Betalt i vår database — det er kun selve Vipps-siden som ev. må rettes.
            if (betaling.Metode == BetalingMetode.Vipps && betaling.BetalingsleverandorReferanse is not null)
            {
                await SelvhelbredVippsFangingAsync(betaling.BetalingsleverandorReferanse, betaling.PasientTotalprisKr, cancellationToken);
            }

            ErBetalt = true;
            return Page();
        }

        if (betaling.Metode is null || betaling.BetalingsleverandorReferanse is null)
        {
            Feilmelding = "Fant ikke noe betalingsforsøk å sjekke status på.";
            return Page();
        }

        if (betaling.Metode == BetalingMetode.Vipps)
        {
            var resultat = await _vippsBekrefter.BekreftOgFangAsync(id, betaling.BetalingsleverandorReferanse, cancellationToken);
            ErBetalt = resultat.Betalt;
            Feilmelding = resultat.Feilmelding
                ?? (resultat.Betalt ? null : "Betalingen er ikke bekreftet ennå. Prøv igjen om et øyeblikk, eller gå tilbake og forsøk på nytt.");
            return Page();
        }

        var erBetaltHosStripe = (await _stripe.HentStatusAsync(betaling.BetalingsleverandorReferanse, cancellationToken)).ErBetalt;
        if (erBetaltHosStripe)
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

    private async Task SelvhelbredVippsFangingAsync(string referanse, decimal belopNok, CancellationToken cancellationToken)
    {
        var status = await _vipps.HentStatusAsync(referanse, cancellationToken);
        if (status.Status != VippsBetalingsstatus.Autorisert)
        {
            return;
        }

        var fanget = await _vipps.FangBetalingAsync(referanse, belopNok, cancellationToken);
        if (!fanget.Success)
        {
            Feilmelding = $"Betalingen ble reservert hos Vipps, men kunne ikke fanges: {fanget.ErrorMessage}. Kontakt administrator — pengene er IKKE tapt, kun fortsatt reservert.";
        }
    }

    private long HentPasientId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
