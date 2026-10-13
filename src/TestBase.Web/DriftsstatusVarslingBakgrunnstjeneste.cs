using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;

namespace TestBase.Web;

/// <summary>
/// Kjører DriftsstatusService periodisk og sender et Pushover-push (IKritiskVarselSender) når en
/// sjekk blir rødt, og et "friskt igjen"-varsel når den blir normal igjen (brukerbeslutning
/// 2026-10-13, Tier 1-valg — se docs/beslutningslogg.md "Driftsstatus-dashboard"). Varsler kun på
/// OVERGANG (av/på rødt), ikke på hver eneste kjøring — se DriftsstatusVarselBeregner, som holder
/// settet av sist varslede sjekknavn (instansfelt her, singleton-levetid som resten av disse
/// bakgrunnsjobbene, se AutomatiskPaaminnelseBakgrunnstjeneste for samme mønster).
/// </summary>
public sealed class DriftsstatusVarslingBakgrunnstjeneste : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DriftsstatusVarslingBakgrunnstjeneste> _logger;
    private readonly HashSet<string> _sistVarsletRoedeSjekker = new();

    public DriftsstatusVarslingBakgrunnstjeneste(IServiceScopeFactory scopeFactory, ILogger<DriftsstatusVarslingBakgrunnstjeneste> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await KjorDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sjekk av kritisk-varsling (Driftsstatus) feilet.");
            }

            try
            {
                await Task.Delay(SjekkIntervall, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal ved shutdown.
            }
        }
    }

    private async Task KjorDueAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var driftsbrytere = scope.ServiceProvider.GetRequiredService<DriftsbryterInnstillingService>();
        var innstilling = await driftsbrytere.HentAsync(cancellationToken);

        if (!innstilling.KritiskVarslingErAktiv || string.IsNullOrWhiteSpace(innstilling.KritiskVarslingPushoverBrukerKey))
        {
            return;
        }

        var driftsstatus = scope.ServiceProvider.GetRequiredService<DriftsstatusService>();
        var sjekker = await driftsstatus.KjorAlleSjekkerAsync(cancellationToken);

        var (nyeRoede, nyFriske) = DriftsstatusVarselBeregner.BeregnDiff(_sistVarsletRoedeSjekker, sjekker);
        if (nyeRoede.Count == 0 && nyFriske.Count == 0)
        {
            return;
        }

        var varselSender = scope.ServiceProvider.GetRequiredService<IKritiskVarselSender>();
        var brukerKey = innstilling.KritiskVarslingPushoverBrukerKey!;

        foreach (var navn in nyeRoede)
        {
            var detalj = sjekker.First(s => s.Navn == navn).Detalj;
            await varselSender.SendAsync(brukerKey, $"🔴 TestBase: {navn}", detalj, cancellationToken);
        }

        foreach (var navn in nyFriske)
        {
            await varselSender.SendAsync(brukerKey, $"🟢 TestBase: {navn} er friskt igjen", "Lyset er ikke lenger rødt.", cancellationToken);
        }

        _logger.LogInformation(
            "Kritisk-varsling: {NyeRoede} ny(e) rød(e) sjekk(er), {NyFriske} friske igjen.", nyeRoede.Count, nyFriske.Count);
    }
}
