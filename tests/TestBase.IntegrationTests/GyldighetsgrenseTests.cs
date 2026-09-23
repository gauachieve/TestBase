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
/// Ende-til-ende-verifisering av gyldighetsgrense + normert gjennomsnitt-imputering
/// (2026-09-23, se docs/beslutningslogg.md "Normert gjennomsnitt-imputering +
/// gyldighetsgrense") — mot en ekte migrert database, samme mønster som
/// BetalingPipelineTests. Bruker den ALLEREDE seedede WHO-5-testen (5
/// LikertSkala-ledd, 0-4) fremfor å opprette en helt ny test, siden det krever
/// en registrert ITestSkaaringsberegner — setter og NULLSTILLER testens/leddets
/// felt i et try/finally slik at andre tester i samme collection ikke påvirkes.
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class GyldighetsgrenseTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public GyldighetsgrenseTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForMangeUbesvarteLeddUtloserGyldighetsadvarsel_OgNormertGjennomsnittImputeresIRaaSkaar()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();

        var who5 = await testService.HentTestVedKodeAsync("who5");
        Assert.NotNull(who5);

        var alleLedd = await db.TestLedd
            .Join(db.TestSider, l => l.TestSideId, s => s.Id, (l, s) => new { Ledd = l, Side = s })
            .Where(x => x.Side.TestId == who5!.Id)
            .OrderBy(x => x.Ledd.Rekkefolge)
            .Select(x => x.Ledd)
            .ToListAsync();
        Assert.Equal(5, alleLedd.Count);

        var opprinneligGrense = who5!.MaksUbesvartProsent;
        var opprinneligNormert = alleLedd[4].NormertGjennomsnitt;
        try
        {
            // 20 % grense — WHO-5 har 5 ledd, så 2 ubesvarte (40 %) skal overskride
            // den, mens 1 ubesvart (20 %) IKKE skal (strengt større-enn, ikke >=).
            who5.MaksUbesvartProsent = 20;
            alleLedd[4].NormertGjennomsnitt = 2.6m; // avrundes til 3 (AwayFromZero) ved imputering
            await db.SaveChangesAsync();

            var behandler = new Behandler
            {
                MobilNr = "+4790000099",
                Email = "gyldighetsgrense-test@example.test",
                Fornavn = "Gyldighet",
                Etternavn = "Testbehandler",
                Status = BehandlerStatus.Aktiv,
                OpprettetUtc = DateTimeOffset.UtcNow
            };
            db.Behandlere.Add(behandler);
            await db.SaveChangesAsync();

            var pasient = new Pasient
            {
                Personnummer = "01019088888",
                MobilNr = "+4791000099",
                Email = "gyldighetsgrense-pasient@example.test",
                Navn = "Gyldighetsgrense Pasient",
                BehandlerId = behandler.Id,
                Status = PasientStatus.Aktiv,
                OpprettetUtc = DateTimeOffset.UtcNow
            };
            db.Pasienter.Add(pasient);
            await db.SaveChangesAsync();

            var tildeling = await testService.TildelAsync(
                who5.Id, pasient.Id, behandlerId: behandler.Id, administratorId: null, frist: null, varighetMinutter: null);

            // Besvarer KUN ledd 0, 1, 2 — ledd 3 og 4 forblir ubesvart (matcher
            // TestService.LagreSvarAsync sin reelle oppførsel: et tomt/uendret felt
            // sendes rett og slett ikke med i svarPerLeddId-dictionaryet, se
            // Pasientportal/Tester/Fyll.cshtml.cs sin OnPostAsync).
            var svar = new Dictionary<long, string>
            {
                [alleLedd[0].Id] = "3",
                [alleLedd[1].Id] = "4",
                [alleLedd[2].Id] = "2"
            };
            await testService.LagreSvarAsync(tildeling.Id, svar, markerFullfort: true);

            var skaaring = await testService.BeregnSkaaringAsync(tildeling.Id);

            Assert.NotNull(skaaring);
            Assert.NotNull(skaaring!.GyldighetsAdvarsel);
            Assert.Contains("2 av 5", skaaring.GyldighetsAdvarsel);
            // Rå skår: 3+4+2 (ekte svar) + 3 (imputert, 2.6 avrundet) = 12. Ledd 3
            // (ingen NormertGjennomsnitt) bidrar fortsatt med 0, som før denne funksjonen.
            Assert.Equal(12, skaaring.RaaSkaar);
            Assert.Equal(48, skaaring.ProsentSkaar); // 12 * 4, jf. Who5Skaaringsberegner
        }
        finally
        {
            who5.MaksUbesvartProsent = opprinneligGrense;
            alleLedd[4].NormertGjennomsnitt = opprinneligNormert;
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task UbesvartLeddUtenNormertGjennomsnitt_KraskerIkkeOgTellesFortsattSomUbesvart()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();

        var who5 = await testService.HentTestVedKodeAsync("who5");
        Assert.NotNull(who5);

        var opprinneligGrense = who5!.MaksUbesvartProsent;
        try
        {
            // Ingen grense satt (null, samme som ALLE eksisterende tester i dag) —
            // skal aldri gi noen advarsel, uansett hvor mange som er ubesvart.
            who5.MaksUbesvartProsent = null;
            await db.SaveChangesAsync();

            var behandler = new Behandler
            {
                MobilNr = "+4790000098",
                Email = "gyldighetsgrense-av-test@example.test",
                Fornavn = "GyldighetAv",
                Etternavn = "Testbehandler",
                Status = BehandlerStatus.Aktiv,
                OpprettetUtc = DateTimeOffset.UtcNow
            };
            db.Behandlere.Add(behandler);
            await db.SaveChangesAsync();

            var pasient = new Pasient
            {
                Personnummer = "01019088887",
                MobilNr = "+4791000098",
                Email = "gyldighetsgrense-av-pasient@example.test",
                Navn = "GyldighetAv Pasient",
                BehandlerId = behandler.Id,
                Status = PasientStatus.Aktiv,
                OpprettetUtc = DateTimeOffset.UtcNow
            };
            db.Pasienter.Add(pasient);
            await db.SaveChangesAsync();

            var tildeling = await testService.TildelAsync(
                who5.Id, pasient.Id, behandlerId: behandler.Id, administratorId: null, frist: null, varighetMinutter: null);

            var alleLedd = await db.TestLedd
                .Join(db.TestSider, l => l.TestSideId, s => s.Id, (l, s) => new { Ledd = l, Side = s })
                .Where(x => x.Side.TestId == who5.Id)
                .OrderBy(x => x.Ledd.Rekkefolge)
                .Select(x => x.Ledd.Id)
                .ToListAsync();

            var svar = new Dictionary<long, string> { [alleLedd[0]] = "1" };
            await testService.LagreSvarAsync(tildeling.Id, svar, markerFullfort: true);

            var skaaring = await testService.BeregnSkaaringAsync(tildeling.Id);

            Assert.NotNull(skaaring);
            Assert.Null(skaaring!.GyldighetsAdvarsel);
            Assert.Equal(1, skaaring.RaaSkaar); // kun det ene besvarte leddet teller
        }
        finally
        {
            who5.MaksUbesvartProsent = opprinneligGrense;
            await db.SaveChangesAsync();
        }
    }
}
