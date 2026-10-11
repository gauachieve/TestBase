using TestBase.Shared.Domain.Tester;

namespace TestBase.Web;

/// <summary>
/// Bugliste 2026-10-11 punkt 6: sender én automatisk "du har ikke fullført testen ennå"-
/// påminnelse til pasienter med en ikke-fullført tildeling som har stått lenger enn
/// TestTildeling.AutomatiskPaaminnelseEtterDager (satt på tildelingstidspunktet når
/// behandler/admin krysset av bryteren i tildelingsdialogen, eller kopiert fra et
/// Behandlingsprogram sin egen bryter, se ProgramService). Samme "sjekk om det trengs"-
/// polling-mønster som PlanlagtTildelingBakgrunnstjeneste, men et grovere intervall — en
/// dag-granularitet-terskel trenger ikke minutt-presisjon.
/// </summary>
public sealed class AutomatiskPaaminnelseBakgrunnstjeneste : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutomatiskPaaminnelseBakgrunnstjeneste> _logger;

    public AutomatiskPaaminnelseBakgrunnstjeneste(
        IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<AutomatiskPaaminnelseBakgrunnstjeneste> logger)
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
                _logger.LogError(ex, "Sjekk av automatiske test-påminnelser feilet.");
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
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();

        var due = await tildelingsService.HentDuePaaminnelserAsync(DateTimeOffset.UtcNow, cancellationToken);
        if (due.Count == 0)
        {
            return;
        }

        var baseUrl = _configuration.GetValue<string>("Varsling:BaseUrl") ?? "https://localhost:7257";
        foreach (var tildelingId in due)
        {
            await tildelingsService.PaaminnUbesvartTestAsync(tildelingId, baseUrl, behandlerId: null, cancellationToken);
        }

        _logger.LogInformation("Sendt {Antall} automatisk(e) test-påminnelse(r).", due.Count);
    }
}
