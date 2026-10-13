using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestBase.IntegrationTests.Infrastructure;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using Xunit;

namespace TestBase.IntegrationTests;

/// <summary>
/// Verifiserer "test-modus"-sperren (se docs/beslutningslogg.md "Abonnement/
/// subscription-system") i TestTildelingsService.TildelOgVarsleAsync — en
/// uavhengig behandler UTEN lagret kort kan kun sende tester til en pasient
/// med samme e-postadresse som sin egen innlogging ("seg selv").
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class TestModusTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public TestModusTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<(Behandler Behandler, Pasient SegSelv, Pasient AnnenPasient, Test Test)> SettOppAsync(
        AppDbContext db, TestService testService, string unikSuffiks, bool harKortLagret, long? partnerId = null)
    {
        var behandlerEpost = $"{unikSuffiks}-behandler@example.test";
        var behandler = new Behandler
        {
            MobilNr = $"+4790{unikSuffiks.GetHashCode() % 1000000:000000}",
            Email = behandlerEpost,
            Status = BehandlerStatus.Aktiv,
            HarKortLagret = harKortLagret,
            PartnerId = partnerId,
            OpprettetViaSelvregistrering = true,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        var segSelv = new Pasient
        {
            Personnummer = null,
            MobilNr = $"+4791{unikSuffiks.GetHashCode() % 1000000:000000}",
            Email = behandlerEpost,
            Navn = "Meg Selv",
            BehandlerId = behandler.Id,
            Status = PasientStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        var annenPasient = new Pasient
        {
            Personnummer = null,
            MobilNr = $"+4792{unikSuffiks.GetHashCode() % 1000000:000000}",
            Email = $"{unikSuffiks}-annen@example.test",
            Navn = "En Annen Pasient",
            BehandlerId = behandler.Id,
            Status = PasientStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.AddRange(segSelv, annenPasient);
        await db.SaveChangesAsync();

        var test = await testService.OpprettTestAsync($"Testmodus-test {unikSuffiks}", null, null, kode: null);

        return (behandler, segSelv, annenPasient, test);
    }

    [Fact]
    public async Task UtenKort_KanKunSendeTilSegSelv()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();

        var (behandler, segSelv, annenPasient, test) = await SettOppAsync(db, testService, "utenkort", harKortLagret: false);

        var resultat = await tildelingsService.TildelOgVarsleAsync(
            new[] { segSelv.Id, annenPasient.Id }, new[] { test.Id }, behandlerId: behandler.Id, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(), baseUrl: "https://localhost", cancellationToken: CancellationToken.None);

        var segSelvResultat = resultat.PerPasient.Single(p => p.PasientId == segSelv.Id);
        var annenResultat = resultat.PerPasient.Single(p => p.PasientId == annenPasient.Id);

        Assert.Single(segSelvResultat.Lenker);
        Assert.Null(segSelvResultat.IkkeTildelteTesterGrunnet);

        Assert.Empty(annenResultat.Lenker);
        Assert.NotNull(annenResultat.IkkeTildelteTesterGrunnet);
        Assert.Contains("test-modus", annenResultat.IkkeTildelteTesterGrunnet!.Single());

        var faktiskeTildelinger = await db.TestTildelinger.Where(t => t.TestId == test.Id).CountAsync();
        Assert.Equal(1, faktiskeTildelinger);
    }

    [Fact]
    public async Task IkkeSelvregistrertUtenKort_ErALDRIBegrensetAvTestmodus()
    {
        // Regresjon: HarKortLagret defaulter til false for ALLE eksisterende rader (migrering) —
        // uten OpprettetViaSelvregistrering-betingelsen ville dette låst HVER ENESTE admin-/
        // kollega-invitert behandler (og enhver test/dev-seed-konstruert rad) til test-modus
        // retroaktivt. Fanget av full regresjonssuite (6 feilende tester) før denne testen ble lagt til.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();

        var (behandler, segSelv, annenPasient, test) = await SettOppAsync(db, testService, "ikkeselvreg", harKortLagret: false);
        behandler.OpprettetViaSelvregistrering = false;
        await db.SaveChangesAsync();

        var resultat = await tildelingsService.TildelOgVarsleAsync(
            new[] { segSelv.Id, annenPasient.Id }, new[] { test.Id }, behandlerId: behandler.Id, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(), baseUrl: "https://localhost", cancellationToken: CancellationToken.None);

        Assert.All(resultat.PerPasient, p => Assert.Single(p.Lenker));
        Assert.All(resultat.PerPasient, p => Assert.Null(p.IkkeTildelteTesterGrunnet));
    }

    [Fact]
    public async Task MedKort_KanSendeTilHvemSomHelst()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();

        var (behandler, segSelv, annenPasient, test) = await SettOppAsync(db, testService, "medkort", harKortLagret: true);

        var resultat = await tildelingsService.TildelOgVarsleAsync(
            new[] { segSelv.Id, annenPasient.Id }, new[] { test.Id }, behandlerId: behandler.Id, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(), baseUrl: "https://localhost", cancellationToken: CancellationToken.None);

        Assert.All(resultat.PerPasient, p => Assert.Single(p.Lenker));
        Assert.All(resultat.PerPasient, p => Assert.Null(p.IkkeTildelteTesterGrunnet));
    }

    [Fact]
    public async Task KlinikktilknyttetUtenKort_KanSendeTilHvemSomHelst()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();

        var partner = new Partner { Navn = "Testmodus-klinikk", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Partnere.Add(partner);
        await db.SaveChangesAsync();

        var (behandler, segSelv, annenPasient, test) = await SettOppAsync(db, testService, "klinikk", harKortLagret: false, partnerId: partner.Id);

        var resultat = await tildelingsService.TildelOgVarsleAsync(
            new[] { segSelv.Id, annenPasient.Id }, new[] { test.Id }, behandlerId: behandler.Id, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(), baseUrl: "https://localhost", cancellationToken: CancellationToken.None);

        Assert.All(resultat.PerPasient, p => Assert.Single(p.Lenker));
        Assert.All(resultat.PerPasient, p => Assert.Null(p.IkkeTildelteTesterGrunnet));
    }
}
