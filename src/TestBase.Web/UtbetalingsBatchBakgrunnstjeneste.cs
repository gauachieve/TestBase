using TestBase.Shared.Domain.Utbetaling;

namespace TestBase.Web;

/// <summary>
/// Genererer det månedlige utbetalingsoppgjøret automatisk — samme
/// selvhelbredende "sjekk om det trengs"-mønster som
/// PlanlagtTildelingBakgrunnstjeneste/ProgramBakgrunnstjeneste, men med et
/// grovere sjekkeintervall siden månedlig "due-het" ikke trenger
/// minuttpresisjon slik brukerplanlagte klokkeslett gjør. Genererer ALLTID
/// kun en Utkast-batch — ingen penger flyttes her, se Admin/Utbetalinger for
/// selve godkjenningen/utbetalingen.
/// </summary>
public sealed class UtbetalingsBatchBakgrunnstjeneste : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UtbetalingsBatchBakgrunnstjeneste> _logger;

    public UtbetalingsBatchBakgrunnstjeneste(IServiceScopeFactory scopeFactory, ILogger<UtbetalingsBatchBakgrunnstjeneste> logger)
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
                _logger.LogError(ex, "Generering av utbetalingsbatch feilet.");
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
        var batchService = scope.ServiceProvider.GetRequiredService<UtbetalingsBatchService>();

        var naaUtc = DateTimeOffset.UtcNow;
        if (!await batchService.ErDueAsync(naaUtc, cancellationToken))
        {
            return;
        }

        var batch = await batchService.GenererForrigeManedAsync(naaUtc, cancellationToken);
        if (batch is not null)
        {
            _logger.LogInformation(
                "Generert utbetalingsbatch for {Aar}-{Maned:D2}, total {Belop} kr.", batch.Aar, batch.Maned, batch.TotalBelopKr);
        }
    }
}
