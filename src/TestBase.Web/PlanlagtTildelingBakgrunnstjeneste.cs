using TestBase.Shared.Domain.Tester;

namespace TestBase.Web;

/// <summary>
/// Utfører planlagte/gjentagende tildelingsbatcher (bugliste 2026-09-15 punkt
/// 8) — sjekker hvert 2. minutt om noen PlanlagtTildeling-rader har passert
/// sitt PlanlagtUtc-tidspunkt, samme selvhelbredende "sjekk om det trengs"-
/// mønster som DagligPaaminnelseBakgrunnstjeneste (i motsetning til en presis
/// engangs-timer, som ikke tåler nedetid rundt selve tidspunktet). Kortere
/// intervall enn den daglige påminnelsen siden dette er brukerplanlagte
/// klokkeslett ("kl. 10.00 nøyaktig") som forventes truffet noenlunde presist,
/// ikke bare "en gang i løpet av dagen". "Varsling:BaseUrl" gjenbrukes fra
/// samme konfigurasjon som den daglige påminnelsen.
/// </summary>
public sealed class PlanlagtTildelingBakgrunnstjeneste : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PlanlagtTildelingBakgrunnstjeneste> _logger;

    public PlanlagtTildelingBakgrunnstjeneste(
        IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<PlanlagtTildelingBakgrunnstjeneste> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
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
                _logger.LogError(ex, "Sjekk av planlagte tildelinger feilet.");
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
        var planlagtService = scope.ServiceProvider.GetRequiredService<PlanlagtTildelingService>();

        var due = await planlagtService.HentDueAsync(DateTimeOffset.UtcNow, cancellationToken);
        if (due.Count == 0)
        {
            return;
        }

        var baseUrl = _configuration.GetValue<string>("Varsling:BaseUrl") ?? "https://localhost:7257";
        foreach (var rad in due)
        {
            await planlagtService.UtforAsync(rad, baseUrl, cancellationToken);
        }

        _logger.LogInformation("Utført {Antall} planlagt(e) tildelingsbatch(er).", due.Count);
    }
}
