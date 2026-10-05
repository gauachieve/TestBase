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
/// Bugliste 2026-10-05 punkt 38 ("NO info was sent when i sent out homework to patient on
/// sms/email"): rotårsaken viste seg å være at å legge en NY test til i en ALLEREDE EKSISTERENDE
/// gruppes testliste (Grupper/Rediger) kun opprettet en GruppeTestTilordning-rad — INGEN
/// TestTildeling og INGEN varsel gikk til gruppens allerede eksisterende, aktive medlemmer. Kun et
/// helt NYTT medlem (via BliPasient-QR-registrering) fikk noensinne en reell tildeling for en
/// gruppes tester. Se GruppeService.SettTilordnedeTesterAsync for fiksen.
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class GruppeServiceTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public GruppeServiceTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LeggTilNyTestIEksisterendeGruppe_TildelerOgVarslerAlleredeAktiveMedlemmer()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var grupper = scope.ServiceProvider.GetRequiredService<GruppeService>();

        var behandler = new Behandler
        {
            MobilNr = "+4790000097", Email = "gruppe-test@example.test",
            Fornavn = "Gruppe", Etternavn = "Testbehandler",
            Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        // Gruppen opprettes UTEN tester tilordnet ennå.
        var gruppe = await grupper.OpprettAsync("Testgruppe uten tester", behandler.Id, Array.Empty<long>());

        // Et EKSISTERENDE, AKTIVT medlem av gruppen — meldt inn FØR noen test ble lagt til.
        var medlem = new Pasient
        {
            Personnummer = "01019066666", MobilNr = "+4791000066", Email = "gruppe-medlem@example.test",
            Navn = "Gruppe Medlem", BehandlerId = behandler.Id, GruppeId = gruppe.Id,
            Status = PasientStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(medlem);
        await db.SaveChangesAsync();

        var test = await testService.OpprettTestAsync("Gruppetest A", null, null, kode: null);
        var side = await testService.LeggTilSideAsync(test.Id, "Side 1", null);
        await testService.LeggTilLeddAsync(side.Id, "Spørsmål", null, TestSvartype.JaNei, null);

        var oppdatertOk = await grupper.OppdaterAsync(
            gruppe.Id, gruppe.Navn, new[] { test.Id }, baseUrl: "https://localhost");
        Assert.True(oppdatertOk);

        var tildeling = await db.TestTildelinger.FirstOrDefaultAsync(t => t.PasientId == medlem.Id && t.TestId == test.Id);
        Assert.NotNull(tildeling);

        var sendteEposter = _factory.Epost.AlleSendte();
        Assert.Contains(sendteEposter, e => e.Til == medlem.Email);

        // --- Å lagre MED SAMME test på nytt (ingen reell endring) skal IKKE opprette en ny, duplikat tildeling. ---
        await grupper.OppdaterAsync(gruppe.Id, gruppe.Navn, new[] { test.Id }, baseUrl: "https://localhost");
        var antallTildelinger = await db.TestTildelinger.CountAsync(t => t.PasientId == medlem.Id && t.TestId == test.Id);
        Assert.Equal(1, antallTildelinger);
    }

    [Fact]
    public async Task NyGruppeMedTesterValgtVedOpprettelse_TildelerIkkeNoenEnnaSidenIngenMedlemmerFinnes()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var grupper = scope.ServiceProvider.GetRequiredService<GruppeService>();

        var behandler = new Behandler
        {
            MobilNr = "+4790000096", Email = "gruppe-ny-test@example.test",
            Fornavn = "GruppeNy", Etternavn = "Testbehandler",
            Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        var test = await testService.OpprettTestAsync("Gruppetest B", null, null, kode: null);

        // OpprettAsync (baseUrl: null internt) skal IKKE kaste selv om testIder ikke er tom —
        // en helt ny gruppe har aldri medlemmer ennå, så etterfyllingsgrenen skal rett og slett
        // aldri bli tatt.
        var gruppe = await grupper.OpprettAsync("Testgruppe med tester", behandler.Id, new[] { test.Id });

        var antallTildelinger = await db.TestTildelinger.CountAsync(t => t.TestId == test.Id);
        Assert.Equal(0, antallTildelinger);

        var tilordning = await db.GruppeTestTilordninger.SingleAsync(t => t.GruppeId == gruppe.Id);
        Assert.Equal(test.Id, tilordning.TestId);
    }
}
