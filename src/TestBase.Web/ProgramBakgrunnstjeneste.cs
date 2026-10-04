using TestBase.Shared.Domain.Tester;

namespace TestBase.Web;

/// <summary>
/// Fyrer av drops for Behandlingsprogram-deltakelser (2026-10-04, fase 3 — se
/// docs/beslutningslogg.md "Hjemmeoppgaver og programmer") — samme 2-minutters
/// selvhelbredende poll-mønster som PlanlagtTildelingBakgrunnstjeneste.
/// </summary>
public sealed class ProgramBakgrunnstjeneste : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProgramBakgrunnstjeneste> _logger;

    public ProgramBakgrunnstjeneste(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<ProgramBakgrunnstjeneste> logger)
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
                await FyrAvDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sjekk av program-drops feilet.");
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

    private async Task FyrAvDueAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var programService = scope.ServiceProvider.GetRequiredService<ProgramService>();
        var baseUrl = _configuration.GetValue<string>("Varsling:BaseUrl") ?? "https://localhost:7257";

        var antall = await programService.FyrAvDueAsync(DateTimeOffset.UtcNow, baseUrl, cancellationToken);
        if (antall > 0)
        {
            _logger.LogInformation("Fyrte av {Antall} program-drop(s).", antall);
        }
    }
}
