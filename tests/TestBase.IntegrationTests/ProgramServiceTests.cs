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
/// Ende-til-ende-verifisering av Programmer sin kjøremotor (2026-10-04, fase 3 — se
/// docs/beslutningslogg.md "Hjemmeoppgaver og programmer"), mot en ekte migrert database. Dekker
/// det som er vanskeligst å stole på fra kun manuell browser-verifisering: betalingsregelen (KUN
/// aller første test i aller første drop kan koste noe — både for en ekte pasient OG for en
/// prøvepasient, som ALDRI skal belastes uansett), kjeding av flere tester INNAD i én drop via
/// HaandterFullfortTestAsync, og progresjon ON TVERS av drops frem til ProgramDeltakelse.FullfortUtc.
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class ProgramServiceTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public ProgramServiceTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void RandomiserTidspunkt_UnngaaNatt_KlemmerInnMotMorgenen()
    {
        var dag = new DateOnly(2026, 10, 12);
        // Hele vinduet er midt på natten (23:00-23:30) — selv med UnngaaNatt=true må metoden
        // returnere ET tidspunkt (klemt til 07:00), aldri kaste eller løkke uendelig.
        var resultat = ProgramService.RandomiserTidspunkt(dag, new TimeSpan(23, 0, 0), new TimeSpan(23, 30, 0), unngaaNatt: true, new Random(42));

        Assert.Equal(new TimeSpan(7, 0, 0), resultat.TimeOfDay);
        Assert.Equal(dag.ToDateTime(TimeOnly.MinValue).Date, resultat.Date);
    }

    [Fact]
    public void RandomiserTidspunkt_UtenUnngaaNatt_BeholderNattetidspunktet()
    {
        var dag = new DateOnly(2026, 10, 12);
        var resultat = ProgramService.RandomiserTidspunkt(dag, new TimeSpan(23, 0, 0), new TimeSpan(23, 30, 0), unngaaNatt: false, new Random(42));

        Assert.InRange(resultat.TimeOfDay, new TimeSpan(23, 0, 0), new TimeSpan(23, 30, 0));
    }

    [Fact]
    public async Task FullProgramflyt_KunForsteTestIForsteDropKosterNoe_KjederInnadIDrop_OgFullforesPaTversAvDrops()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var programService = scope.ServiceProvider.GetRequiredService<ProgramService>();

        var behandler = new Behandler
        {
            MobilNr = "+4790000099", Email = "program-test@example.test",
            Fornavn = "Program", Etternavn = "Testbehandler",
            Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        // --- Tre enkle, prisede tester (én side, ett JaNei-ledd hver — skåring er irrelevant her). ---
        async Task<Test> OpprettPrisetTestAsync(string navn)
        {
            var test = await testService.OpprettTestAsync(navn, null, null, kode: null);
            test.MinstePrisKr = 20m;
            test.StorstePrisKr = 100m;
            test.TypiskBehandlerHonorarKr = 0m;
            await db.SaveChangesAsync();
            var side = await testService.LeggTilSideAsync(test.Id, "Side 1", null);
            await testService.LeggTilLeddAsync(side.Id, "Spørsmål", null, TestSvartype.JaNei, null);
            return test;
        }

        var testA = await OpprettPrisetTestAsync("Programtest A");
        var testB = await OpprettPrisetTestAsync("Programtest B");
        var testC = await OpprettPrisetTestAsync("Programtest C");

        var ekteDag = new TimeSpan(10, 0, 0);
        var program = await programService.OpprettAsync(
            behandler.Id, "Testprogram", null, DayOfWeek.Monday, new TimeSpan(9, 0, 0),
            new[]
            {
                new ProgramDropInput(0, ekteDag, ekteDag, false, new[] { testA.Id, testB.Id }),
                new ProgramDropInput(3, ekteDag, ekteDag, false, new[] { testC.Id })
            });

        var ektePasient = new Pasient
        {
            Personnummer = "01019088888", MobilNr = "+4791000099", Email = "program-pasient@example.test",
            Navn = "Program Pasient", BehandlerId = behandler.Id, Status = PasientStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        var provePasient = new Pasient
        {
            Personnummer = null, MobilNr = "+4791000098", Email = "program-prove@example.test",
            Navn = "Program Prøvepasient", BehandlerId = behandler.Id, Status = PasientStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.AddRange(ektePasient, provePasient);
        await db.SaveChangesAsync();

        var antallTildelt = await programService.TildelAsync(program.Id, new[] { ektePasient.Id, provePasient.Id }, gruppeId: null, behandlerId: behandler.Id, administratorId: null);
        Assert.Equal(2, antallTildelt);

        var deltakelser = await db.ProgramDeltakelser.Where(d => d.ProgramId == program.Id).ToListAsync();
        Assert.Equal(2, deltakelser.Count);
        foreach (var d in deltakelser)
        {
            Assert.Equal(0, d.NaavaerendeDroppIndeks);
            Assert.NotNull(d.NesteDroppPlanlagtUtc);
            // Tvingfyr drop 0 ved å sette planlagt tidspunkt til fortiden (samme teknikk som
            // manuell verifisering via SQL) — vi tester selve motoren, ikke ekte ukedag-venting.
            d.NesteDroppPlanlagtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        }
        await db.SaveChangesAsync();

        var antallFyrt = await programService.FyrAvDueAsync(DateTimeOffset.UtcNow, "https://localhost");
        Assert.Equal(2, antallFyrt);

        var ekteDeltakelse = await db.ProgramDeltakelser.FirstAsync(d => d.PasientId == ektePasient.Id);
        var proveDeltakelse = await db.ProgramDeltakelser.FirstAsync(d => d.PasientId == provePasient.Id);

        var ekteTestATildeling = await db.ProgramTildelinger
            .Where(k => k.ProgramDeltakelseId == ekteDeltakelse.Id)
            .Join(db.TestTildelinger, k => k.TestTildelingId, t => t.Id, (k, t) => t)
            .SingleAsync(t => t.TestId == testA.Id);
        var ekteBetalingA = await testService.HentBetalingAsync(ekteTestATildeling.Id);

        Assert.NotNull(ekteBetalingA);
        Assert.True(ekteBetalingA!.PasientTotalprisKr > 0m, "Første test i første drop skal koste en ekte pasient noe.");
        Assert.Equal(BetalingStatus.Venter, ekteBetalingA.Status);

        var proveTestATildeling = await db.ProgramTildelinger
            .Where(k => k.ProgramDeltakelseId == proveDeltakelse.Id)
            .Join(db.TestTildelinger, k => k.TestTildelingId, t => t.Id, (k, t) => t)
            .SingleAsync(t => t.TestId == testA.Id);
        var proveBetalingA = await testService.HentBetalingAsync(proveTestATildeling.Id);

        Assert.NotNull(proveBetalingA);
        Assert.Equal(0m, proveBetalingA!.PasientTotalprisKr);
        Assert.Equal(BetalingStatus.IkkePakrevd, proveBetalingA.Status);

        // --- Fullfør test A for den ekte pasienten — skal kjede rett til test B i SAMME drop, GRATIS uansett pris. ---
        var leddA = await db.TestLedd.Where(l => l.TestSideId == db.TestSider.Where(s => s.TestId == testA.Id).Select(s => s.Id).First()).ToListAsync();
        await testService.LagreSvarAsync(ekteTestATildeling.Id, new Dictionary<long, string> { [leddA[0].Id] = "Ja" }, markerFullfort: true);
        await programService.HaandterFullfortTestAsync(ekteTestATildeling.Id, "https://localhost");

        var ekteDeltakelseEtterA = await db.ProgramDeltakelser.FirstAsync(d => d.Id == ekteDeltakelse.Id);
        Assert.Equal(0, ekteDeltakelseEtterA.NaavaerendeDroppIndeks); // Drop 0 er IKKE ferdig ennå — test B gjenstår.

        var ekteTestBTildeling = await db.ProgramTildelinger
            .Where(k => k.ProgramDeltakelseId == ekteDeltakelse.Id)
            .Join(db.TestTildelinger, k => k.TestTildelingId, t => t.Id, (k, t) => t)
            .SingleAsync(t => t.TestId == testB.Id);
        var ekteBetalingB = await testService.HentBetalingAsync(ekteTestBTildeling.Id);
        Assert.NotNull(ekteBetalingB);
        Assert.Equal(0m, ekteBetalingB!.PasientTotalprisKr); // Andre test i samme drop: ALLTID gratis, selv for en ekte pasient.
        Assert.Equal(BetalingStatus.IkkePakrevd, ekteBetalingB.Status);

        // --- Fullfør test B — drop 0 er nå helt ferdig, neste drop skal planlegges (ikke fyres ennå). ---
        var leddB = await db.TestLedd.Where(l => l.TestSideId == db.TestSider.Where(s => s.TestId == testB.Id).Select(s => s.Id).First()).ToListAsync();
        await testService.LagreSvarAsync(ekteTestBTildeling.Id, new Dictionary<long, string> { [leddB[0].Id] = "Nei" }, markerFullfort: true);
        await programService.HaandterFullfortTestAsync(ekteTestBTildeling.Id, "https://localhost");

        var ekteDeltakelseEtterDrop0 = await db.ProgramDeltakelser.FirstAsync(d => d.Id == ekteDeltakelse.Id);
        Assert.Equal(1, ekteDeltakelseEtterDrop0.NaavaerendeDroppIndeks);
        Assert.NotNull(ekteDeltakelseEtterDrop0.NesteDroppPlanlagtUtc); // Drop 1 (test C) planlagt, men ikke fyrt.
        Assert.Null(ekteDeltakelseEtterDrop0.FullfortUtc);

        // --- Tvingfyr drop 1 (test C) og fullfør den — hele programmet skal nå markeres fullført. ---
        ekteDeltakelseEtterDrop0.NesteDroppPlanlagtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        await programService.FyrAvDueAsync(DateTimeOffset.UtcNow, "https://localhost");

        var ekteTestCTildeling = await db.ProgramTildelinger
            .Where(k => k.ProgramDeltakelseId == ekteDeltakelse.Id)
            .Join(db.TestTildelinger, k => k.TestTildelingId, t => t.Id, (k, t) => t)
            .SingleAsync(t => t.TestId == testC.Id);
        var ekteBetalingC = await testService.HentBetalingAsync(ekteTestCTildeling.Id);
        Assert.Equal(0m, ekteBetalingC!.PasientTotalprisKr); // Senere drop: ALLTID gratis, uansett testens pris.

        var leddC = await db.TestLedd.Where(l => l.TestSideId == db.TestSider.Where(s => s.TestId == testC.Id).Select(s => s.Id).First()).ToListAsync();
        await testService.LagreSvarAsync(ekteTestCTildeling.Id, new Dictionary<long, string> { [leddC[0].Id] = "Ja" }, markerFullfort: true);
        await programService.HaandterFullfortTestAsync(ekteTestCTildeling.Id, "https://localhost");

        var ekteDeltakelseFerdig = await db.ProgramDeltakelser.FirstAsync(d => d.Id == ekteDeltakelse.Id);
        Assert.NotNull(ekteDeltakelseFerdig.FullfortUtc);
        Assert.Null(ekteDeltakelseFerdig.NesteDroppPlanlagtUtc);
    }

    [Fact]
    public async Task PauseOgMeldUt_OppdatererDeltakelseOgOppretterBehandlerOppgave()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var programService = scope.ServiceProvider.GetRequiredService<ProgramService>();
        var meldingService = scope.ServiceProvider.GetRequiredService<BehandlerMeldingService>();

        var behandler = new Behandler
        {
            MobilNr = "+4790000098", Email = "program-pause-test@example.test",
            Fornavn = "Pause", Etternavn = "Testbehandler",
            Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        var test = await testService.OpprettTestAsync("Pauseprogram-test", null, null, kode: null);
        var side = await testService.LeggTilSideAsync(test.Id, "Side 1", null);
        await testService.LeggTilLeddAsync(side.Id, "Spørsmål", null, TestSvartype.JaNei, null);

        var program = await programService.OpprettAsync(
            behandler.Id, "Pauseprogram", null, DayOfWeek.Monday, new TimeSpan(9, 0, 0),
            new[] { new ProgramDropInput(0, new TimeSpan(9, 0, 0), new TimeSpan(9, 0, 0), false, new[] { test.Id }) });

        var pasient = new Pasient
        {
            Personnummer = "01019077777", MobilNr = "+4791000077", Email = "program-pause-pasient@example.test",
            Navn = "Pause Pasient", BehandlerId = behandler.Id, Status = PasientStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(pasient);
        await db.SaveChangesAsync();

        await programService.TildelAsync(program.Id, new[] { pasient.Id }, gruppeId: null, behandlerId: behandler.Id, administratorId: null);
        var deltakelse = await db.ProgramDeltakelser.FirstAsync(d => d.ProgramId == program.Id && d.PasientId == pasient.Id);

        var antallFoer = await meldingService.TellUlesteAsync(behandler.Id);
        await programService.PauseAsync(deltakelse.Id);

        var deltakelseEtterPause = await db.ProgramDeltakelser.FirstAsync(d => d.Id == deltakelse.Id);
        Assert.NotNull(deltakelseEtterPause.PauseUtc);
        Assert.Null(deltakelseEtterPause.MeldtUtUtc);
        var antallEtterPause = await meldingService.TellUlesteAsync(behandler.Id);
        Assert.Equal(antallFoer + 1, antallEtterPause);

        await programService.MeldUtAsync(deltakelse.Id);
        var deltakelseEtterMeldUt = await db.ProgramDeltakelser.FirstAsync(d => d.Id == deltakelse.Id);
        Assert.NotNull(deltakelseEtterMeldUt.MeldtUtUtc);
        Assert.Null(deltakelseEtterMeldUt.NesteDroppPlanlagtUtc);
        var antallEtterMeldUt = await meldingService.TellUlesteAsync(behandler.Id);
        Assert.Equal(antallEtterPause + 1, antallEtterMeldUt);

        // Meldt ut er TERMINALT — et nytt pauseforsøk skal være en stille no-op.
        await programService.PauseAsync(deltakelse.Id);
        var deltakelseEtterNoOp = await db.ProgramDeltakelser.FirstAsync(d => d.Id == deltakelse.Id);
        Assert.NotNull(deltakelseEtterNoOp.MeldtUtUtc);
    }
}
