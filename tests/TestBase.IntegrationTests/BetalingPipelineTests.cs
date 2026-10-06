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

        // PartnerTestTilgang opprettes nå automatisk av OpprettTestAsync siden
        // partneren allerede fantes (se TestService.GiAllePartnereTilgangTilTestAsync,
        // 2026-09-15) — kun andelen må settes opp eksplisitt her.
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
            baseUrl: "https://localhost", cancellationToken: CancellationToken.None);

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
    public async Task PartnerbehandlerKanIkkeTildeleTestUtenforAllowList()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();

        var partner = new Partner { Navn = "Allow-list-partner AS", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Partnere.Add(partner);
        await db.SaveChangesAsync();

        var behandler = new Behandler
        {
            MobilNr = "+4790000003",
            Email = "allowlist-test@example.test",
            Fornavn = "Allowlist",
            Etternavn = "Testbehandler",
            Status = BehandlerStatus.Aktiv,
            PartnerId = partner.Id,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        // To tester — begge får automatisk tilgang ved opprettelse siden partneren
        // allerede fantes (se TestService.GiAllePartnereTilgangTilTestAsync,
        // 2026-09-15). Fjerner tilgangen til den ene eksplisitt her for å simulere
        // en Superadmin som har kuratert allow-listen ned igjen — akkurat som å
        // fjerne en avkrysning på Admin/Partnere/Tester.
        var tillattTest = await testService.OpprettTestAsync("Tillatt test", null, null, kode: null);
        var ikkeTillattTest = await testService.OpprettTestAsync("Ikke tillatt test", null, null, kode: null);
        var ikkeTillattTilgang = await db.PartnerTestTilganger
            .SingleAsync(t => t.PartnerId == partner.Id && t.TestId == ikkeTillattTest.Id);
        db.PartnerTestTilganger.Remove(ikkeTillattTilgang);
        await db.SaveChangesAsync();

        var pasient = new Pasient
        {
            Personnummer = "01019077777",
            MobilNr = "+4792000001",
            Email = "allowlist-pasient@example.test",
            Navn = "Allowlist Pasient",
            BehandlerId = behandler.Id,
            Status = PasientStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(pasient);
        await db.SaveChangesAsync();

        // Prøver å tildele BEGGE tester i én batch — akkurat som en rå POST som
        // omgår tre-visningens filtrering ville sett ut, se
        // TestTildelingsService.TildelOgVarsleAsync.
        var resultat = await tildelingsService.TildelOgVarsleAsync(
            new[] { pasient.Id }, new[] { tillattTest.Id, ikkeTillattTest.Id }, behandlerId: behandler.Id, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(),
            baseUrl: "https://localhost", cancellationToken: CancellationToken.None);

        // Kun den tillatte testen skal faktisk ha blitt tildelt.
        var lenker = resultat.PerPasient.Single().Lenker;
        Assert.Single(lenker);
        Assert.Equal("Tillatt test", lenker[0].TestNavn);

        var tildelinger = await db.TestTildelinger.Where(t => t.PasientId == pasient.Id).ToListAsync();
        Assert.Single(tildelinger);
        Assert.Equal(tillattTest.Id, tildelinger[0].TestId);

        // Kategori-tre-visningen (steg 2 i tildelingsflyten) skal heller aldri
        // vise den ikke-tillatte testen til denne partnerens behandlere.
        await testService.SikreStandardkategorierAsync();
        await testService.KoblTestTilKategoriAsync(tillattTest.Id, "Diagnostikk, tverrgående og øvrige verktøy");
        await testService.KoblTestTilKategoriAsync(ikkeTillattTest.Id, "Diagnostikk, tverrgående og øvrige verktøy");
        var kategoriTre = await testService.HentKategoriTreAsync(partnerId: partner.Id);
        var synligeTestNavn = kategoriTre.SelectMany(k => k.Tester).Select(t => t.Navn).ToList();
        Assert.Contains("Tillatt test", synligeTestNavn);
        Assert.DoesNotContain("Ikke tillatt test", synligeTestNavn);
    }

    /// <summary>
    /// Reell bug rapportert av brukeren på live 2026-10-06: "assigns a homework test to a patient,
    /// says sent, never goes out". Rotårsak: TildelOgVarsleAsync sin PartnerTestTilgang-allow-
    /// list-håndhevelse (lagt til for det admin-forfattede testkatalog-biblioteket) filtrerte
    /// STILLE bort enhver hjemmeoppgave for en partner-tilknyttet behandler, siden
    /// HjemmeoppgaveService.OpprettAsync BEVISST aldri gir noen partner automatisk
    /// PartnerTestTilgang (se klassens egen XML-doc) — testIder endte tomt FØR selve
    /// tildelingsløkken, så INGEN TestTildeling ble opprettet og INGEN "Ikke tildelt"-forklaring
    /// ble vist (den mekanismen dekker kun KreverBiologiskKjonn/FyllesUtAvBehandler), bare en
    /// tom, men "vellykket" TildelingsBatchResultat. Se docs/beslutningslogg.md for full analyse.
    /// </summary>
    [Fact]
    public async Task PartnerbehandlerKanTildeleEgenHjemmeoppgaveSelvOmDenIkkeErPaaAllowList()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hjemmeoppgaveService = scope.ServiceProvider.GetRequiredService<HjemmeoppgaveService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();

        var partner = new Partner { Navn = "Hjemmeoppgave-partner AS", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Partnere.Add(partner);
        await db.SaveChangesAsync();

        var behandler = new Behandler
        {
            MobilNr = "+4790000004",
            Email = "hjemmeoppgave-partner-test@example.test",
            Fornavn = "Hjemmeoppgave",
            Etternavn = "Partnerbehandler",
            Status = BehandlerStatus.Aktiv,
            PartnerId = partner.Id,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        // Opprettet via HjemmeoppgaveService (IKKE TestService.OpprettTestAsync) — får derfor
        // BEVISST ingen PartnerTestTilgang-rad, se klassens XML-doc.
        var hjemmeoppgave = await hjemmeoppgaveService.OpprettAsync(
            behandler.Id, "Pusteøvelse", null, null, null,
            new List<HjemmeoppgaveLeddInput> { new("Hvordan føltes det?", null, TestSvartype.LikertSkala, "1:Dårlig,2:Bra", true, null, null) });

        Assert.False(await db.PartnerTestTilganger.AnyAsync(t => t.PartnerId == partner.Id && t.TestId == hjemmeoppgave.Id));

        var pasient = new Pasient
        {
            Personnummer = "01019088888",
            MobilNr = "+4793000001",
            Email = "hjemmeoppgave-pasient@example.test",
            Navn = "Hjemmeoppgave Pasient",
            BehandlerId = behandler.Id,
            Status = PasientStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(pasient);
        await db.SaveChangesAsync();

        var resultat = await tildelingsService.TildelOgVarsleAsync(
            new[] { pasient.Id }, new[] { hjemmeoppgave.Id }, behandlerId: behandler.Id, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(),
            baseUrl: "https://localhost", cancellationToken: CancellationToken.None);

        var lenker = resultat.PerPasient.Single().Lenker;
        Assert.Single(lenker);
        Assert.Equal("Pusteøvelse", lenker[0].TestNavn);

        var tildelinger = await db.TestTildelinger.Where(t => t.PasientId == pasient.Id).ToListAsync();
        Assert.Single(tildelinger);
        Assert.Equal(hjemmeoppgave.Id, tildelinger[0].TestId);
    }

    /// <summary>
    /// Brukeren ba eksplisitt om bekreftelse på at fiksen over dekker ALLE hjemmeoppgaver en
    /// behandler faktisk kan se/velge i tildelingsflyten — ikke bare egne. Siden
    /// TildelOgVarsleAsync sin nye hjemmeoppgave-ekskludering kun sjekker Test.ErHjemmeoppgave
    /// (aldri eierskap), skal dette gjelde uendret for en KOLLEGAS hjemmeoppgave delt med alle ELLER
    /// delt kun med partneren — begge verifisert her i samme test siden de deler rotårsaken.
    /// </summary>
    [Fact]
    public async Task PartnerbehandlerKanTildeleKollegasDelteHjemmeoppgaver()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hjemmeoppgaveService = scope.ServiceProvider.GetRequiredService<HjemmeoppgaveService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();

        var partner = new Partner { Navn = "Delt-hjemmeoppgave-partner AS", OpprettetAvAdministratorId = 1, OpprettetUtc = DateTimeOffset.UtcNow };
        db.Partnere.Add(partner);
        await db.SaveChangesAsync();

        var eier = new Behandler
        {
            MobilNr = "+4790000005", Email = "hjemmeoppgave-eier@example.test",
            Fornavn = "Eier", Etternavn = "Behandler", Status = BehandlerStatus.Aktiv,
            PartnerId = partner.Id, OpprettetUtc = DateTimeOffset.UtcNow
        };
        var tildelendeBehandler = new Behandler
        {
            MobilNr = "+4790000006", Email = "hjemmeoppgave-tildeler@example.test",
            Fornavn = "Tildeler", Etternavn = "Behandler", Status = BehandlerStatus.Aktiv,
            PartnerId = partner.Id, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.AddRange(eier, tildelendeBehandler);
        await db.SaveChangesAsync();

        var ledd = new List<HjemmeoppgaveLeddInput> { new("Hvordan føltes det?", null, TestSvartype.LikertSkala, "1:Dårlig,2:Bra", true, null, null) };
        var deltMedAlle = await hjemmeoppgaveService.OpprettAsync(eier.Id, "Delt med alle-oppgave", null, null, null, ledd);
        deltMedAlle.ErDeltMedAlle = true;
        var deltMedPartner = await hjemmeoppgaveService.OpprettAsync(eier.Id, "Delt med partner-oppgave", null, null, null, ledd);
        deltMedPartner.ErDeltMedPartner = true;
        await db.SaveChangesAsync();

        var pasient = new Pasient
        {
            Personnummer = "01019066666", MobilNr = "+4793000002", Email = "hjemmeoppgave-pasient2@example.test",
            Navn = "Hjemmeoppgave Pasient 2", BehandlerId = tildelendeBehandler.Id, Status = PasientStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(pasient);
        await db.SaveChangesAsync();

        var resultat = await tildelingsService.TildelOgVarsleAsync(
            new[] { pasient.Id }, new[] { deltMedAlle.Id, deltMedPartner.Id },
            behandlerId: tildelendeBehandler.Id, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(),
            baseUrl: "https://localhost", cancellationToken: CancellationToken.None);

        var lenker = resultat.PerPasient.Single().Lenker;
        Assert.Equal(2, lenker.Count);
        Assert.Contains(lenker, l => l.TestNavn == "Delt med alle-oppgave");
        Assert.Contains(lenker, l => l.TestNavn == "Delt med partner-oppgave");

        var tildelteTestIder = await db.TestTildelinger.Where(t => t.PasientId == pasient.Id).Select(t => t.TestId).ToListAsync();
        Assert.Contains(deltMedAlle.Id, tildelteTestIder);
        Assert.Contains(deltMedPartner.Id, tildelteTestIder);
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
            baseUrl: "https://localhost", cancellationToken: CancellationToken.None);

        var tildeling = await db.TestTildelinger.FirstAsync(t => t.PasientId == pasient.Id && t.TestId == test.Id);
        var betaling = await testService.HentBetalingAsync(tildeling.Id);

        Assert.NotNull(betaling);
        Assert.Equal(BetalingStatus.IkkePakrevd, betaling!.Status);
        Assert.Equal(0m, betaling.PasientTotalprisKr);

        var bevegelser = await db.Pengebevegelser.Where(p => p.TestTildelingId == tildeling.Id).CountAsync();
        Assert.Equal(0, bevegelser);
    }
}
