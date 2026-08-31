using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Web;

/// <summary>
/// Kjører PaaminnelseService.SendPaaminnelserAsync én gang i døgnet — sjekker
/// hvert 15. minutt om klokken har passert konfigurert klokkeslett
/// ("Varsling:PaaminnelseKlokkeslettUtc", standard 07 UTC) OG at det faktisk
/// finnes minst én behandler som ikke allerede har fått påminnelse i dag, før
/// den gjør noe — selvhelbredende ved omstart/nedetid rundt selve
/// klokkeslettet, i motsetning til en presis "våkne kl. 07:00"-timer.
/// "Varsling:BaseUrl" MÅ settes til ekte domene ved reell drift — en
/// bakgrunnstjeneste har ingen HTTP-forespørsel å lese den fra slik
/// TestTildelingsService/Program.cs' InnloggingsstiForAsync har.
/// </summary>
public sealed class DagligPaaminnelseBakgrunnstjeneste : BackgroundService
{
    private static readonly TimeSpan SjekkIntervall = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DagligPaaminnelseBakgrunnstjeneste> _logger;

    public DagligPaaminnelseBakgrunnstjeneste(
        IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<DagligPaaminnelseBakgrunnstjeneste> logger)
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
                await KjorOmDetTrengsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Daglig påminnelse-sjekk feilet.");
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

    private async Task KjorOmDetTrengsAsync(CancellationToken cancellationToken)
    {
        var klokkeslett = _configuration.GetValue("Varsling:PaaminnelseKlokkeslettUtc", 7);
        if (DateTimeOffset.UtcNow.Hour < klokkeslett)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var idagUtc = DateTimeOffset.UtcNow.Date;
        var trengerSending = await db.Behandlere.AnyAsync(
            b => b.OnskerDagligPaaminnelse && (b.SistPaaminnetUtc == null || b.SistPaaminnetUtc.Value < idagUtc),
            cancellationToken);
        if (!trengerSending)
        {
            return;
        }

        var baseUrl = _configuration.GetValue<string>("Varsling:BaseUrl") ?? "https://localhost:7257";
        var paaminnelseService = scope.ServiceProvider.GetRequiredService<PaaminnelseService>();
        var resultat = await paaminnelseService.SendPaaminnelserAsync(baseUrl, cancellationToken);

        if (resultat.AntallBehandlereVarslet > 0)
        {
            _logger.LogInformation("Daglig påminnelse sendt til {Antall} behandler(e).", resultat.AntallBehandlereVarslet);
        }
    }
}
