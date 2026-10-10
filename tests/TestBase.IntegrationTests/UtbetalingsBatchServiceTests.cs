using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestBase.IntegrationTests.Infrastructure;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Utbetaling;
using Xunit;

namespace TestBase.IntegrationTests;

/// <summary>
/// Verifiserer UtbetalingsBatchService mot en ekte migrert database — spesielt
/// de to garantiene som faktisk hindrer feil med ekte penger: unik indeks på
/// (Aar, Maned) hindrer dobbel batch-generering for samme måned, og unik
/// indeks på PengebevegelseId hindrer at samme pengebevegelse havner i to
/// utbetalingslinjer noensinne. Setter Pengebevegelse-rader direkte (hopper
/// over hele betalings-/tildelingskjeden, som BetalingPipelineTests.cs
/// allerede dekker) siden denne tjenesten kun leser/grupperer ferdige rader.
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class UtbetalingsBatchServiceTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public UtbetalingsBatchServiceTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static DateTimeOffset ForsteIManeden(int aar, int maned) => new(aar, maned, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GenererForrigeManedAsync_GrupperesPerMottakerOgSummererRiktig()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var batchService = scope.ServiceProvider.GetRequiredService<UtbetalingsBatchService>();

        var behandler = new Behandler
        {
            MobilNr = "+4790000101", Email = "utbetaling-test1@example.test",
            Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        var partner = new Partner { Navn = "Utbetaling-test-partner AS", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Partnere.Add(partner);
        await db.SaveChangesAsync();

        // Unik måned (langt tilbake i tid) for å unngå kollisjon med andre tester/kjøringer i samme DB.
        var periode = ForsteIManeden(2019, 3);
        db.Pengebevegelser.AddRange(
            new Pengebevegelse { Type = PengebevegelseType.BehandlerHonorar, BelopKr = 150m, BehandlerId = behandler.Id, OpprettetUtc = periode },
            new Pengebevegelse { Type = PengebevegelseType.BehandlerHonorar, BelopKr = 150m, BehandlerId = behandler.Id, OpprettetUtc = periode.AddDays(5) },
            new Pengebevegelse { Type = PengebevegelseType.PartnerAndel, BelopKr = 25m, PartnerId = partner.Id, OpprettetUtc = periode.AddDays(10) },
            // Plattforminntekt skal ALDRI havne i en utbetalingslinje, kun Honorar/Andel:
            new Pengebevegelse { Type = PengebevegelseType.PlattformInntekt, BelopKr = 1000m, OpprettetUtc = periode });
        await db.SaveChangesAsync();

        var naaUtc = ForsteIManeden(2019, 4); // "nå" = rett etter periodens måned
        var batch = await batchService.GenererForrigeManedAsync(naaUtc);

        Assert.NotNull(batch);
        Assert.Equal(2019, batch!.Aar);
        Assert.Equal(3, batch.Maned);
        Assert.Equal(325m, batch.TotalBelopKr); // 150+150+25

        var linjer = await db.UtbetalingsLinjer.Where(l => l.UtbetalingsBatchId == batch.Id).ToListAsync();
        Assert.Equal(2, linjer.Count);

        var behandlerlinje = Assert.Single(linjer, l => l.MottakerType == MottakerType.Behandler);
        Assert.Equal(behandler.Id, behandlerlinje.BehandlerId);
        Assert.Equal(300m, behandlerlinje.BelopKr);
        Assert.Equal(2, behandlerlinje.AntallUnderliggendeTransaksjoner);

        var partnerlinje = Assert.Single(linjer, l => l.MottakerType == MottakerType.Partner);
        Assert.Equal(partner.Id, partnerlinje.PartnerId);
        Assert.Equal(25m, partnerlinje.BelopKr);
        Assert.Equal(1, partnerlinje.AntallUnderliggendeTransaksjoner);
    }

    [Fact]
    public async Task GenererForrigeManedAsync_ErIdempotent_IngenDuplikatBatchForSammeManed()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var batchService = scope.ServiceProvider.GetRequiredService<UtbetalingsBatchService>();

        var behandler = new Behandler
        {
            MobilNr = "+4790000102", Email = "utbetaling-test2@example.test",
            Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        var periode = ForsteIManeden(2019, 5);
        db.Pengebevegelser.Add(new Pengebevegelse
        {
            Type = PengebevegelseType.BehandlerHonorar, BelopKr = 100m, BehandlerId = behandler.Id, OpprettetUtc = periode
        });
        await db.SaveChangesAsync();

        var naaUtc = ForsteIManeden(2019, 6);

        Assert.True(await batchService.ErDueAsync(naaUtc));
        var forste = await batchService.GenererForrigeManedAsync(naaUtc);
        Assert.NotNull(forste);

        Assert.False(await batchService.ErDueAsync(naaUtc));
        var andre = await batchService.GenererForrigeManedAsync(naaUtc);
        Assert.Null(andre); // allerede generert - ingen duplikat

        var antallBatcher = await db.UtbetalingsBatcher.CountAsync(b => b.Aar == 2019 && b.Maned == 5);
        Assert.Equal(1, antallBatcher);
    }

    [Fact]
    public async Task GenererForrigeManedAsync_InkludererAldriEnPengebevegelseSomAlleredeErIEnUtbetalingslinje()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var batchService = scope.ServiceProvider.GetRequiredService<UtbetalingsBatchService>();

        var behandler = new Behandler
        {
            MobilNr = "+4790000103", Email = "utbetaling-test3@example.test",
            Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        var periode = ForsteIManeden(2019, 7);
        var alleredeDekket = new Pengebevegelse
        {
            Type = PengebevegelseType.BehandlerHonorar, BelopKr = 150m, BehandlerId = behandler.Id, OpprettetUtc = periode
        };
        var nyNok = new Pengebevegelse
        {
            Type = PengebevegelseType.BehandlerHonorar, BelopKr = 50m, BehandlerId = behandler.Id, OpprettetUtc = periode.AddDays(1)
        };
        db.Pengebevegelser.AddRange(alleredeDekket, nyNok);
        await db.SaveChangesAsync();

        // Simulerer at "alleredeDekket" allerede ble betalt ut i en tidligere (nå fjernet/håndtert) sammenheng,
        // ved å sette inn selve koblingsraden direkte — en vilkårlig eksisterende UtbetalingsLinjeId holder,
        // siden det kun er PengebevegelseId-unikheten som testes her.
        var dummyBatch = new UtbetalingsBatch { Aar = 2019, Maned = 1, Status = UtbetalingsBatchStatus.Godkjent, GenerertUtc = DateTimeOffset.UtcNow };
        db.UtbetalingsBatcher.Add(dummyBatch);
        await db.SaveChangesAsync();
        var dummyLinje = new UtbetalingsLinje
        {
            UtbetalingsBatchId = dummyBatch.Id, MottakerType = MottakerType.Behandler, BehandlerId = behandler.Id,
            BelopKr = 150m, AntallUnderliggendeTransaksjoner = 1, Status = UtbetalingsLinjeStatus.Overfort
        };
        db.UtbetalingsLinjer.Add(dummyLinje);
        await db.SaveChangesAsync();
        db.UtbetalingsLinjePengebevegelser.Add(new UtbetalingsLinjePengebevegelse
        {
            UtbetalingsLinjeId = dummyLinje.Id, PengebevegelseId = alleredeDekket.Id
        });
        await db.SaveChangesAsync();

        var naaUtc = ForsteIManeden(2019, 8);
        var batch = await batchService.GenererForrigeManedAsync(naaUtc);

        Assert.NotNull(batch);
        Assert.Equal(50m, batch!.TotalBelopKr); // KUN nyNok, ikke de 150 kr som allerede var dekket

        var linje = Assert.Single(await db.UtbetalingsLinjer.Where(l => l.UtbetalingsBatchId == batch.Id).ToListAsync());
        Assert.Equal(1, linje.AntallUnderliggendeTransaksjoner);
    }
}
