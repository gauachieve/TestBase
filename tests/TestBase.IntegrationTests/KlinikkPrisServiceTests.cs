using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestBase.IntegrationTests.Infrastructure;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using Xunit;

namespace TestBase.IntegrationTests;

/// <summary>
/// Verifiserer KlinikkPrisService sin sete-tier-oppslag og rabatt-beregning
/// (se docs/beslutningslogg.md "Abonnement/subscription-system") mot en ekte
/// migrert database — hver test rydder opp sine egne KlinikkSeterTier-rader
/// FØR seeding, siden tabellen ellers kan ha stående rader fra Admin/Abonnement
/// sin lat-seeding (delt DB per collection, se TestBaseCollection).
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class KlinikkPrisServiceTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public KlinikkPrisServiceTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task SettOppStandardTiersAsync(AppDbContext db)
    {
        db.KlinikkSeterTiers.RemoveRange(db.KlinikkSeterTiers);
        await db.SaveChangesAsync();
        db.KlinikkSeterTiers.AddRange(
            new KlinikkSeterTier { FraAntallSeter = 1, TilAntallSeter = 5, KrPerSeteManed = 199m },
            new KlinikkSeterTier { FraAntallSeter = 6, TilAntallSeter = 15, KrPerSeteManed = 169m },
            new KlinikkSeterTier { FraAntallSeter = 16, TilAntallSeter = null, KrPerSeteManed = 139m });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task IngenBehandlere_GirNullpris()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<KlinikkPrisService>();
        await SettOppStandardTiersAsync(db);

        var partner = new Partner { Navn = "Tom klinikk", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Partnere.Add(partner);
        await db.SaveChangesAsync();

        var resultat = await service.BeregnAsync(partner.Id);

        Assert.NotNull(resultat);
        Assert.Equal(0, resultat!.AntallSeter);
        Assert.Equal(0m, resultat.NettoManedsprisKr);
    }

    [Fact]
    public async Task InnenforLavesteSjikt_BrukerKorrektKrPerSete()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<KlinikkPrisService>();
        await SettOppStandardTiersAsync(db);

        var partner = new Partner { Navn = "Liten klinikk", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Partnere.Add(partner);
        await db.SaveChangesAsync();

        for (var i = 0; i < 3; i++)
        {
            db.Behandlere.Add(new Behandler
            {
                MobilNr = $"+4799{i:000000}", Email = $"liten-klinikk-{i}@example.test",
                Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow, PartnerId = partner.Id
            });
        }
        await db.SaveChangesAsync();

        var resultat = await service.BeregnAsync(partner.Id);

        Assert.Equal(3, resultat!.AntallSeter);
        Assert.Equal(199m, resultat.KrPerSeteManed);
        Assert.Equal(597m, resultat.BruttoManedsprisKr);
        Assert.Equal(597m, resultat.NettoManedsprisKr);
    }

    [Fact]
    public async Task ArkiverteBehandlereTellerIkke()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<KlinikkPrisService>();
        await SettOppStandardTiersAsync(db);

        var partner = new Partner { Navn = "Klinikk med arkiverte", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Partnere.Add(partner);
        await db.SaveChangesAsync();

        db.Behandlere.Add(new Behandler { MobilNr = "+4799000010", Email = "aktiv@example.test", Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow, PartnerId = partner.Id });
        db.Behandlere.Add(new Behandler { MobilNr = "+4799000011", Email = "arkivert@example.test", Status = BehandlerStatus.Arkivert, OpprettetUtc = DateTimeOffset.UtcNow, PartnerId = partner.Id });
        await db.SaveChangesAsync();

        var resultat = await service.BeregnAsync(partner.Id);

        Assert.Equal(1, resultat!.AntallSeter);
    }

    [Fact]
    public async Task RabattProsentTrekkesFraBruttoprisen()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<KlinikkPrisService>();
        await SettOppStandardTiersAsync(db);

        var partner = new Partner { Navn = "Rabattert klinikk", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow, RabattProsent = 20m };
        db.Partnere.Add(partner);
        await db.SaveChangesAsync();

        for (var i = 0; i < 8; i++)
        {
            db.Behandlere.Add(new Behandler
            {
                MobilNr = $"+4798{i:000000}", Email = $"rabattert-klinikk-{i}@example.test",
                Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow, PartnerId = partner.Id
            });
        }
        await db.SaveChangesAsync();

        var resultat = await service.BeregnAsync(partner.Id);

        // 8 seter i sjiktet 6-15 (169 kr/sete) = 1352 kr brutto, 20 % rabatt = 1081,60 kr netto.
        Assert.Equal(8, resultat!.AntallSeter);
        Assert.Equal(169m, resultat.KrPerSeteManed);
        Assert.Equal(1352m, resultat.BruttoManedsprisKr);
        Assert.Equal(1081.60m, resultat.NettoManedsprisKr);
    }
}
