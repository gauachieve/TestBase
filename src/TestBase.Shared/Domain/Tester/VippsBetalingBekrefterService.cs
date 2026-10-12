using Microsoft.Extensions.Logging;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Tester;

public sealed record VippsBekreftelseResultat(bool Betalt, bool VarAlleredeBetalt, string? Feilmelding);

/// <summary>
/// Delt "sjekk status hos Vipps, fang (capture) om nødvendig, marker betalt"-logikk
/// — brukt BÅDE fra betalings-retur-siden (Pasientportal/Tester/BetalResultat, synkron
/// fallback) OG fra Vipps-webhooken (Security/PaymentWebhooks.cs). Begge MÅ gå via
/// nøyaktig denne metoden, ALDRI kalle TestService.MarkerBetalingBetaltAsync direkte for
/// Vipps — ellers kan en AUTORISERT (kun reservert, ikke tatt) betaling bli markert
/// betalt i vår database uten at pengene noensinne ble fanget hos Vipps. Dette er
/// nøyaktig samme bug-klasse som "Kritisk bugfiks: Vipps-betaling ble aldri fanget"
/// (2026-09-15, docs/beslutningslogg.md) — den gangen oppdaget på retur-siden, her
/// forebygget på forhånd for webhook-veien før den noensinne ble tatt i bruk.
/// Idempotent (TestService.MarkerBetalingBetaltAsync er det).
/// </summary>
public sealed class VippsBetalingBekrefterService
{
    private readonly TestService _testService;
    private readonly IVippsClient _vipps;
    private readonly ILogger<VippsBetalingBekrefterService> _logger;

    public VippsBetalingBekrefterService(TestService testService, IVippsClient vipps, ILogger<VippsBetalingBekrefterService> logger)
    {
        _testService = testService;
        _vipps = vipps;
        _logger = logger;
    }

    public async Task<VippsBekreftelseResultat> BekreftOgFangAsync(
        long tildelingId, string referanse, CancellationToken cancellationToken = default)
    {
        var betaling = await _testService.HentBetalingAsync(tildelingId, cancellationToken);
        if (betaling is null)
        {
            return new VippsBekreftelseResultat(false, false, "Fant ingen betaling for denne tildelingen.");
        }
        if (betaling.Status == BetalingStatus.Betalt)
        {
            return new VippsBekreftelseResultat(true, true, null);
        }

        var status = await _vipps.HentStatusAsync(referanse, cancellationToken);
        if (!status.Success)
        {
            return new VippsBekreftelseResultat(false, false, status.ErrorMessage);
        }

        var erFanget = status.Status == VippsBetalingsstatus.Fanget;
        if (status.Status == VippsBetalingsstatus.Autorisert)
        {
            var fanget = await _vipps.FangBetalingAsync(referanse, betaling.PasientTotalprisKr, cancellationToken);
            if (!fanget.Success)
            {
                _logger.LogError(
                    "Vipps-betaling {Referanse} (tildeling {TildelingId}) ble reservert men IKKE fanget: {Feil}. Pengene er fortsatt reservert hos Vipps, ikke tapt.",
                    referanse, tildelingId, fanget.ErrorMessage);
                return new VippsBekreftelseResultat(
                    false, false,
                    $"Betalingen ble reservert hos Vipps, men kunne ikke fanges: {fanget.ErrorMessage}. Pengene er IKKE tapt, kun fortsatt reservert.");
            }
            erFanget = true;
        }

        if (!erFanget)
        {
            return new VippsBekreftelseResultat(false, false, null);
        }

        await _testService.MarkerBetalingBetaltAsync(tildelingId, BetalingMetode.Vipps, referanse, cancellationToken);
        return new VippsBekreftelseResultat(true, false, null);
    }
}
