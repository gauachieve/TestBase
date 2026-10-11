using Microsoft.Extensions.DependencyInjection;
using TestBase.IntegrationTests.Infrastructure;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using Xunit;

namespace TestBase.IntegrationTests;

/// <summary>
/// Verifiserer de tre periodiserte oppgjørsrapportene ("Periodiserte oppgjør",
/// designforslag 2026-10-10, koblet til appen 2026-10-11 — se
/// docs/beslutningslogg.md) mot en ekte migrert database. Setter
/// Pengebevegelse/TestTildeling-rader direkte (samme mønster som
/// UtbetalingsBatchServiceTests) siden tjenesten kun LESER allerede-skrevne
/// rader, ikke selve betalingskjeden (dekket av BetalingPipelineTests).
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class OkonomiRapportServiceTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public OkonomiRapportServiceTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static DateTimeOffset IManeden(int aar, int maaned, int dag) => new(aar, maaned, dag, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HentAdminRapportAsync_PivotererPerTestTildelingOgSummererRiktig()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<OkonomiRapportService>();

        var behandler = new Behandler { MobilNr = "+4790000201", Email = "okonomi-admin-test@example.test", Fornavn = "Kari", Etternavn = "Nordmann", Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Behandlere.Add(behandler);
        var test = new Test { Navn = "WHO-5", OpprettetUtc = DateTimeOffset.UtcNow };
        db.Tester.Add(test);
        await db.SaveChangesAsync();

        var tildeling = new TestTildeling { TestId = test.Id, PasientId = 0, TildeltAvBehandlerId = behandler.Id, TildeltUtc = DateTimeOffset.UtcNow, Status = TestTildelingStatus.Fullfort };
        db.TestTildelinger.Add(tildeling);
        await db.SaveChangesAsync();

        var periode = IManeden(2019, 9, 3);
        db.Pengebevegelser.AddRange(
            new Pengebevegelse { Type = PengebevegelseType.PasientBetalingMottatt, BelopKr = 250m, TestTildelingId = tildeling.Id, BehandlerId = behandler.Id, OpprettetUtc = periode },
            new Pengebevegelse { Type = PengebevegelseType.PlattformInntekt, BelopKr = 100m, TestTildelingId = tildeling.Id, OpprettetUtc = periode },
            new Pengebevegelse { Type = PengebevegelseType.BehandlerHonorar, BelopKr = 150m, TestTildelingId = tildeling.Id, BehandlerId = behandler.Id, OpprettetUtc = periode });
        await db.SaveChangesAsync();

        var rapport = await service.HentAdminRapportAsync(2019, 9, CancellationToken.None);

        var rad = Assert.Single(rapport.Rader);
        Assert.Equal(tildeling.Id, rad.TestTildelingId);
        Assert.Equal("WHO-5", rad.TestNavn);
        Assert.Equal("Kari Nordmann", rad.BehandlerNavn);
        Assert.Equal(250m, rad.PasientprisKr);
        Assert.Equal(150m, rad.HonorarKr);
        Assert.Equal(100m, rad.PlattformAndelKr);
        Assert.Null(rad.PartnerAndelKr);

        Assert.Equal(250m, rapport.SumPasientpris);
        Assert.Equal(150m, rapport.SumHonorar);
        Assert.Equal(100m, rapport.SumPlattformAndel);
        Assert.Equal(0m, rapport.SumPartnerAndel);
    }

    [Fact]
    public async Task HentBehandlerRapportAsync_FiltrererPaaBehandlerOgPeriode()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<OkonomiRapportService>();

        var behandlerA = new Behandler { MobilNr = "+4790000202", Email = "okonomi-behandler-a@example.test", Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow };
        var behandlerB = new Behandler { MobilNr = "+4790000203", Email = "okonomi-behandler-b@example.test", Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Behandlere.AddRange(behandlerA, behandlerB);
        var test = new Test { Navn = "PHQ-9", OpprettetUtc = DateTimeOffset.UtcNow };
        db.Tester.Add(test);
        await db.SaveChangesAsync();

        var tildelingA1 = new TestTildeling { TestId = test.Id, PasientId = 0, TildeltAvBehandlerId = behandlerA.Id, TildeltUtc = DateTimeOffset.UtcNow, Status = TestTildelingStatus.Fullfort };
        var tildelingA2 = new TestTildeling { TestId = test.Id, PasientId = 0, TildeltAvBehandlerId = behandlerA.Id, TildeltUtc = DateTimeOffset.UtcNow, Status = TestTildelingStatus.Fullfort };
        db.TestTildelinger.AddRange(tildelingA1, tildelingA2);
        await db.SaveChangesAsync();

        var iPerioden = IManeden(2019, 10, 5);
        var utenforPerioden = IManeden(2019, 11, 5);
        db.Pengebevegelser.AddRange(
            new Pengebevegelse { Type = PengebevegelseType.BehandlerHonorar, BelopKr = 100m, TestTildelingId = tildelingA1.Id, BehandlerId = behandlerA.Id, OpprettetUtc = iPerioden },
            // Samme behandler, men UTENFOR valgt periode - skal ikke telle med:
            new Pengebevegelse { Type = PengebevegelseType.BehandlerHonorar, BelopKr = 999m, TestTildelingId = tildelingA2.Id, BehandlerId = behandlerA.Id, OpprettetUtc = utenforPerioden },
            // Annen behandler, samme periode - skal ikke telle med:
            new Pengebevegelse { Type = PengebevegelseType.BehandlerHonorar, BelopKr = 999m, TestTildelingId = tildelingA1.Id, BehandlerId = behandlerB.Id, OpprettetUtc = iPerioden });
        await db.SaveChangesAsync();

        var rapport = await service.HentBehandlerRapportAsync(behandlerA.Id, 2019, 10, CancellationToken.None);

        var rad = Assert.Single(rapport.Rader);
        Assert.Equal(100m, rad.HonorarKr);
        Assert.Equal("PHQ-9", rad.TestNavn);
        Assert.Equal(100m, rapport.SumHonorar);
    }

    [Fact]
    public async Task HentPartnerRapportAsync_GrupperesPerBehandlerViaTildelingenSelv()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<OkonomiRapportService>();

        var partner = new Partner { Navn = "Nordic Psykologi AS", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Partnere.Add(partner);
        var perVik = new Behandler { MobilNr = "+4790000204", Email = "okonomi-per-vik@example.test", Fornavn = "Per", Etternavn = "Vik", Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow };
        var annaSkog = new Behandler { MobilNr = "+4790000205", Email = "okonomi-anna-skog@example.test", Fornavn = "Anna", Etternavn = "Skog", Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Behandlere.AddRange(perVik, annaSkog);
        var test = new Test { Navn = "CORE-10", OpprettetUtc = DateTimeOffset.UtcNow };
        db.Tester.Add(test);
        await db.SaveChangesAsync();

        // PartnerAndel-raden selv bærer IKKE BehandlerId (se TestService.MarkerBetalingBetaltAsync)
        // — behandleren må slås opp via selve tildelingen, nøyaktig det denne testen verifiserer.
        var tildelingPerVik1 = new TestTildeling { TestId = test.Id, PasientId = 0, TildeltAvBehandlerId = perVik.Id, TildeltUtc = DateTimeOffset.UtcNow, Status = TestTildelingStatus.Fullfort };
        var tildelingPerVik2 = new TestTildeling { TestId = test.Id, PasientId = 0, TildeltAvBehandlerId = perVik.Id, TildeltUtc = DateTimeOffset.UtcNow, Status = TestTildelingStatus.Fullfort };
        var tildelingAnnaSkog = new TestTildeling { TestId = test.Id, PasientId = 0, TildeltAvBehandlerId = annaSkog.Id, TildeltUtc = DateTimeOffset.UtcNow, Status = TestTildelingStatus.Fullfort };
        db.TestTildelinger.AddRange(tildelingPerVik1, tildelingPerVik2, tildelingAnnaSkog);
        await db.SaveChangesAsync();

        var periode = IManeden(2019, 12, 1);
        db.Pengebevegelser.AddRange(
            new Pengebevegelse { Type = PengebevegelseType.PartnerAndel, BelopKr = 25m, TestTildelingId = tildelingPerVik1.Id, PartnerId = partner.Id, OpprettetUtc = periode },
            new Pengebevegelse { Type = PengebevegelseType.PartnerAndel, BelopKr = 25m, TestTildelingId = tildelingPerVik2.Id, PartnerId = partner.Id, OpprettetUtc = periode },
            new Pengebevegelse { Type = PengebevegelseType.PartnerAndel, BelopKr = 25m, TestTildelingId = tildelingAnnaSkog.Id, PartnerId = partner.Id, OpprettetUtc = periode });
        await db.SaveChangesAsync();

        var rapport = await service.HentPartnerRapportAsync(partner.Id, 2019, 12, CancellationToken.None);

        Assert.Equal(2, rapport.Rader.Count);
        var perVikRad = Assert.Single(rapport.Rader, r => r.BehandlerNavn == "Per Vik");
        Assert.Equal(2, perVikRad.AntallTester);
        Assert.Equal(50m, perVikRad.SumPartnerAndelKr);
        var annaSkogRad = Assert.Single(rapport.Rader, r => r.BehandlerNavn == "Anna Skog");
        Assert.Equal(1, annaSkogRad.AntallTester);
        Assert.Equal(25m, annaSkogRad.SumPartnerAndelKr);

        Assert.Equal(3, rapport.TotaltAntallTester);
        Assert.Equal(75m, rapport.TotalSumPartnerAndelKr);
    }
}
