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
/// Ende-til-ende-verifisering av selve prisings-/betalingsrørledningen (se
/// docs/beslutningslogg.md "Partner System + Test Monetization"), mot en ekte
/// migrert database — men via direkte tjenestekall (TestTildelingsService/
/// TestService) fremfor full HTTP-skjema-scraping, siden dette dekker en helt
/// annen del av systemet enn HeleFlytenTests (autentisering/registrering) og
/// ikke trenger å gjenta den ceremonien. IVippsClient/IStripeClient er
/// Mock*-variantene her (ingen ekte leverandørkonfigurasjon i testmiljøet),
/// som alltid rapporterer vellykket — akkurat riktig for å teste selve
/// rørledningen (pris → TestTildelingBetaling → MarkerBetalingBetaltAsync →
/// Pengebevegelse) uten å avhenge av en ekte tredjepart.
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class BetalingPipelineTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public BetalingPipelineTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TildelMedHonorar_SkaperKorrektBetalingOgLedger_VedBekreftelse()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();

        // --- Oppsett: en partner, en behandler i partneren, en test med prising,
        // en pasient, og partnerens tilgang/andel for testen. ---
        var behandler = new Behandler
        {
            MobilNr = "+4790000001",
            Email = "betaling-test@example.test",
            Fornavn = "Betaling",
            Etternavn = "Testbehandler",
            Status = BehandlerStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        var partner = new Partner { Navn = "Pipeline-partner AS", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Partnere.Add(partner);
        await db.SaveChangesAsync();

        behandler.PartnerId = partner.Id;
        await db.SaveChangesAsync();

        var test = await testService.OpprettTestAsync("Pipeline-test", null, null, kode: null);
        test.MinstePrisKr = 20m;
        test.StorstePrisKr = 500m;
        test.TypiskBehandlerHonorarKr = 100m;
        test.MinstePartnerAndelKr = 10m;
        await db.SaveChangesAsync();

        db.PartnerTestTilganger.Add(new PartnerTestTilgang { PartnerId = partner.Id, TestId = test.Id, GittAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow });
        db.PartnerTestAndeler.Add(new PartnerTestAndel { PartnerId = partner.Id, TestId = test.Id, AndelKr = 15m, SistEndretAvBehandlerId = behandler.Id, SistEndretUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var pasient = new Pasient
        {
            Personnummer = "01019099999",
            MobilNr = "+4791000001",
            Email = "pipeline-pasient@example.test",
            Navn = "Pipeline Pasient",
            BehandlerId = behandler.Id,
            Status = PasientStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(pasient);
        await db.SaveChangesAsync();

        // --- Tildel med et eksplisitt behandler-honorar. ---
        var resultat = await tildelingsService.TildelOgVarsleAsync(
            new[] { pasient.Id }, new[] { test.Id }, behandlerId: behandler.Id, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?> { [test.Id] = 120m },
            baseUrl: "https://localhost", CancellationToken.None);

        var tildelingId = resultat.PerPasient.Single().Lenker.Single().Lenke.Split('/').Last();
        var tildeling = await db.TestTildelinger.FirstAsync(t => t.PasientId == pasient.Id && t.TestId == test.Id);
        Assert.Equal(tildelingId, tildeling.Id.ToString());

        var betaling = await testService.HentBetalingAsync(tildeling.Id);
        Assert.NotNull(betaling);
        // plattform 20 + partner 15 (over gulvet 10, brukt direkte) + honorar 120 = 155, innenfor maks 500.
        Assert.Equal(20m, betaling!.PlattformAndelKr);
        Assert.Equal(15m, betaling.PartnerAndelKr);
        Assert.Equal(120m, betaling.BehandlerHonorarKr);
        Assert.Equal(155m, betaling.PasientTotalprisKr);
        Assert.Equal(BetalingStatus.Venter, betaling.Status);
        Assert.Equal(partner.Id, betaling.PartnerId);

        // --- Bekreft betaling (som webhook/retur-side ville gjort) og sjekk ledger. ---
        var markert = await testService.MarkerBetalingBetaltAsync(tildeling.Id, BetalingMetode.Stripe, "pi_test_123");
        Assert.True(markert);

        var oppdatertBetaling = await testService.HentBetalingAsync(tildeling.Id);
        Assert.Equal(BetalingStatus.Betalt, oppdatertBetaling!.Status);
        Assert.NotNull(oppdatertBetaling.BetaltUtc);

        var bevegelser = await db.Pengebevegelser.Where(p => p.TestTildelingId == tildeling.Id).ToListAsync();
        Assert.Equal(4, bevegelser.Count);
        Assert.Equal(155m, bevegelser.Single(p => p.Type == PengebevegelseType.PasientBetalingMottatt).BelopKr);
        Assert.Equal(20m, bevegelser.Single(p => p.Type == PengebevegelseType.PlattformInntekt).BelopKr);
        Assert.Equal(15m, bevegelser.Single(p => p.Type == PengebevegelseType.PartnerAndel).BelopKr);
        Assert.Equal(120m, bevegelser.Single(p => p.Type == PengebevegelseType.BehandlerHonorar).BelopKr);

        // --- Idempotens: å markere betalt på nytt skal IKKE doble opp ledger-radene. ---
        var markertToGang = await testService.MarkerBetalingBetaltAsync(tildeling.Id, BetalingMetode.Stripe, "pi_test_123");
        Assert.False(markertToGang);
        var bevegelserEtterEnGangTil = await db.Pengebevegelser.Where(p => p.TestTildelingId == tildeling.Id).CountAsync();
        Assert.Equal(4, bevegelserEtterEnGangTil);
    }

    [Fact]
    public async Task TildelUtenPrising_ErIkkePakrevdOgSkaperIngenLedger()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();

        var behandler = new Behandler
        {
            MobilNr = "+4790000002",
            Email = "ingen-prising@example.test",
            Fornavn = "Ingen",
            Etternavn = "Prisingbehandler",
            Status = BehandlerStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);

        var test = await testService.OpprettTestAsync("Uprissatt test", null, null, kode: null);

        var pasient = new Pasient
        {
            Personnummer = "01019088888",
            MobilNr = "+4791000002",
            Email = "ingen-prising-pasient@example.test",
            Navn = "Uprissatt Pasient",
            Status = PasientStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(pasient);
        await db.SaveChangesAsync();
        pasient.BehandlerId = behandler.Id;
        await db.SaveChangesAsync();

        var resultat = await tildelingsService.TildelOgVarsleAsync(
            new[] { pasient.Id }, new[] { test.Id }, behandlerId: behandler.Id, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(),
            baseUrl: "https://localhost", CancellationToken.None);

        var tildeling = await db.TestTildelinger.FirstAsync(t => t.PasientId == pasient.Id && t.TestId == test.Id);
        var betaling = await testService.HentBetalingAsync(tildeling.Id);

        Assert.NotNull(betaling);
        Assert.Equal(BetalingStatus.IkkePakrevd, betaling!.Status);
        Assert.Equal(0m, betaling.PasientTotalprisKr);

        var bevegelser = await db.Pengebevegelser.Where(p => p.TestTildelingId == tildeling.Id).CountAsync();
        Assert.Equal(0, bevegelser);
    }
}
