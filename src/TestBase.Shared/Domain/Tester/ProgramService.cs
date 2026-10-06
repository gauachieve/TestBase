using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Tester;

/// <summary>Ett ledd (test + rekkefølge) i en drop, slik forfatteren bygget den i editoren.</summary>
public sealed record ProgramDropInput(int DagerEtterForrige, TimeSpan FraKlokkeslett, TimeSpan TilKlokkeslett, bool UnngaaNatt, IReadOnlyList<long> TestIder);

/// <summary>
/// Programmer (2026-10-04, fase 3 — se docs/beslutningslogg.md "Hjemmeoppgaver og programmer").
/// Egen klasse fra TestService/TestTildelingsService av samme grunn som HjemmeoppgaveService:
/// programmer skal starte PRIVAT (ikke automatisk delt), og selve kjøremotoren
/// (drop-fyring/randomisering/pause/meld-ut) er en HELT NY bekymring uten noe naturlig hjem i de
/// eksisterende tjenestene. TestService/LagreSvarAsync er BEVISST uvitende om programmer —
/// koblingen går andre veien, via ProgramTildeling + et eksplisitt kall til
/// HaandterFullfortTestAsync FRA Pasientportal/Tester/Fyll.cshtml.cs rett etter at en test
/// markeres fullført, akkurat som hjemmeoppgavenes mandatory-validering er et eksternt kall i
/// stedet for en utvidelse av den delte metoden.
/// </summary>
public sealed class ProgramService
{
    private static readonly TimeZoneInfo NorskTid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
    private static readonly TimeSpan NattStart = new(22, 0, 0);
    private static readonly TimeSpan NattSlutt = new(7, 0, 0);

    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly TestPrisberegner _prisberegner;
    private readonly BehandlerMeldingService _meldingService;
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;
    private readonly ILogger<ProgramService> _logger;

    public ProgramService(
        AppDbContext db, TestService testService, TestPrisberegner prisberegner, BehandlerMeldingService meldingService,
        ISmsSender sms, IEmailSender email, ILogger<ProgramService> logger)
    {
        _db = db;
        _testService = testService;
        _prisberegner = prisberegner;
        _meldingService = meldingService;
        _sms = sms;
        _email = email;
        _logger = logger;
    }

    // =========================================================================================
    // Forfatning (samme mønster som HjemmeoppgaveService)
    // =========================================================================================

    public async Task<Behandlingsprogram> OpprettAsync(
        long behandlerId, string navn, string? forklaring, DayOfWeek startUkedag, TimeSpan startKlokkeslett,
        IReadOnlyList<ProgramDropInput> drops, CancellationToken cancellationToken = default)
    {
        var program = new Behandlingsprogram
        {
            Navn = navn,
            Forklaring = forklaring,
            StartUkedag = startUkedag,
            StartKlokkeslett = startKlokkeslett,
            OpprettetAvBehandlerId = behandlerId,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        _db.Behandlingsprogrammer.Add(program);
        await _db.SaveChangesAsync(cancellationToken);

        await LeggTilDropsAsync(program.Id, drops, cancellationToken);
        return program;
    }

    private async Task LeggTilDropsAsync(long programId, IReadOnlyList<ProgramDropInput> drops, CancellationToken cancellationToken)
    {
        var rekkefolge = 1;
        foreach (var input in drops)
        {
            var drop = new ProgramDrop
            {
                ProgramId = programId,
                Rekkefolge = rekkefolge++,
                DagerEtterForrige = input.DagerEtterForrige,
                FraKlokkeslett = input.FraKlokkeslett,
                TilKlokkeslett = input.TilKlokkeslett,
                UnngaaNatt = input.UnngaaNatt
            };
            _db.ProgramDrops.Add(drop);
            await _db.SaveChangesAsync(cancellationToken);

            var testRekkefolge = 1;
            foreach (var testId in input.TestIder)
            {
                _db.ProgramDropTester.Add(new ProgramDropTest { ProgramDropId = drop.Id, TestId = testId, Rekkefolge = testRekkefolge++ });
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(bool Lykkes, bool DropsLaast, string? Feilmelding)> OppdaterAsync(
        long programId, long behandlerId, string navn, string? forklaring, DayOfWeek startUkedag, TimeSpan startKlokkeslett,
        IReadOnlyList<ProgramDropInput> drops, CancellationToken cancellationToken = default)
    {
        var program = await _db.Behandlingsprogrammer.FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);
        if (program is null || program.OpprettetAvBehandlerId != behandlerId)
        {
            return (false, false, "Fant ikke programmet, eller du eier det ikke.");
        }

        program.Navn = navn;
        program.Forklaring = forklaring;
        program.StartUkedag = startUkedag;
        program.StartKlokkeslett = startKlokkeslett;

        var harDeltakelser = await _db.ProgramDeltakelser.AnyAsync(d => d.ProgramId == programId, cancellationToken);
        if (harDeltakelser)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return (true, true, "Dette programmet er allerede tildelt minst én pasient, så selve " +
                "drop-/testlisten kan ikke endres. Navn/forklaring/starttidspunkt er likevel lagret. " +
                "Bruk \"Kopier\" for å lage en ny versjon.");
        }

        var eksisterendeDrops = await _db.ProgramDrops.Where(d => d.ProgramId == programId).ToListAsync(cancellationToken);
        var dropIder = eksisterendeDrops.Select(d => d.Id).ToList();
        _db.ProgramDropTester.RemoveRange(_db.ProgramDropTester.Where(t => dropIder.Contains(t.ProgramDropId)));
        _db.ProgramDrops.RemoveRange(eksisterendeDrops);
        await _db.SaveChangesAsync(cancellationToken);
        await LeggTilDropsAsync(programId, drops, cancellationToken);

        return (true, false, null);
    }

    public async Task<bool> SlettAsync(long programId, long behandlerId, CancellationToken cancellationToken = default)
    {
        var program = await _db.Behandlingsprogrammer.FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);
        if (program is null || program.OpprettetAvBehandlerId != behandlerId)
        {
            return false;
        }

        var harDeltakelser = await _db.ProgramDeltakelser.AnyAsync(d => d.ProgramId == programId, cancellationToken);
        if (harDeltakelser)
        {
            program.ErArkivert = true;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var drops = await _db.ProgramDrops.Where(d => d.ProgramId == programId).ToListAsync(cancellationToken);
        var dropIder = drops.Select(d => d.Id).ToList();
        _db.ProgramDropTester.RemoveRange(_db.ProgramDropTester.Where(t => dropIder.Contains(t.ProgramDropId)));
        _db.ProgramDrops.RemoveRange(drops);
        _db.Behandlingsprogrammer.Remove(program);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Behandlingsprogram?> KopierAsync(long programId, long behandlerId, CancellationToken cancellationToken = default)
    {
        var original = await _db.Behandlingsprogrammer.FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);
        if (original is null)
        {
            return null;
        }

        var originalDrops = await _db.ProgramDrops.Where(d => d.ProgramId == programId).OrderBy(d => d.Rekkefolge).ToListAsync(cancellationToken);
        var dropInputs = new List<ProgramDropInput>();
        foreach (var drop in originalDrops)
        {
            var testIder = await _db.ProgramDropTester.Where(t => t.ProgramDropId == drop.Id).OrderBy(t => t.Rekkefolge).Select(t => t.TestId).ToListAsync(cancellationToken);
            dropInputs.Add(new ProgramDropInput(drop.DagerEtterForrige, drop.FraKlokkeslett, drop.TilKlokkeslett, drop.UnngaaNatt, testIder));
        }

        var kopi = new Behandlingsprogram
        {
            Navn = $"Kopi av {original.Navn}",
            Forklaring = original.Forklaring,
            StartUkedag = original.StartUkedag,
            StartKlokkeslett = original.StartKlokkeslett,
            OpprettetAvBehandlerId = behandlerId,
            KopiertFraProgramId = original.Id,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        _db.Behandlingsprogrammer.Add(kopi);
        await _db.SaveChangesAsync(cancellationToken);
        await LeggTilDropsAsync(kopi.Id, dropInputs, cancellationToken);

        var eksisterendeLiking = await _db.ProgramLikinger.FirstOrDefaultAsync(l => l.BehandlerId == behandlerId && l.ProgramId == programId, cancellationToken);
        if (eksisterendeLiking is not null)
        {
            _db.ProgramLikinger.Remove(eksisterendeLiking);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return kopi;
    }

    public async Task LikAsync(long behandlerId, long programId, CancellationToken cancellationToken = default)
    {
        var finnesAllerede = await _db.ProgramLikinger.AnyAsync(l => l.BehandlerId == behandlerId && l.ProgramId == programId, cancellationToken);
        if (finnesAllerede)
        {
            return;
        }
        _db.ProgramLikinger.Add(new ProgramLiking { BehandlerId = behandlerId, ProgramId = programId, OpprettetUtc = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task FjernLikingAsync(long behandlerId, long programId, CancellationToken cancellationToken = default)
    {
        var liking = await _db.ProgramLikinger.FirstOrDefaultAsync(l => l.BehandlerId == behandlerId && l.ProgramId == programId, cancellationToken);
        if (liking is not null)
        {
            _db.ProgramLikinger.Remove(liking);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> SettDeltMedAlleAsync(long programId, long behandlerId, bool verdi, CancellationToken cancellationToken = default)
    {
        var program = await _db.Behandlingsprogrammer.FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);
        if (program is null || program.OpprettetAvBehandlerId != behandlerId)
        {
            return false;
        }
        program.ErDeltMedAlle = verdi;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SettDeltMedPartnerAsync(long programId, long behandlerId, bool verdi, CancellationToken cancellationToken = default)
    {
        var program = await _db.Behandlingsprogrammer.FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);
        if (program is null || program.OpprettetAvBehandlerId != behandlerId)
        {
            return false;
        }
        var harPartner = await _db.Behandlere.Where(b => b.Id == behandlerId).Select(b => b.PartnerId).FirstOrDefaultAsync(cancellationToken) is not null;
        if (!harPartner)
        {
            return false;
        }
        program.ErDeltMedPartner = verdi;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Behandlingsprogram>> HentPersonligAsync(long behandlerId, CancellationToken cancellationToken = default)
    {
        var likte = await _db.ProgramLikinger.Where(l => l.BehandlerId == behandlerId).Select(l => l.ProgramId).ToListAsync(cancellationToken);
        return await _db.Behandlingsprogrammer
            .Where(p => !p.ErArkivert && (p.OpprettetAvBehandlerId == behandlerId || likte.Contains(p.Id)))
            .OrderByDescending(p => p.OpprettetUtc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// INKLUDERER egne delte programmer (samme fiks/begrunnelse som HjemmeoppgaveService sin
    /// HentDeltMedAlleAsync, bugliste 2026-10-06 punkt 17 — samme bug fantes her også, funnet ved
    /// kodegjennomgang). Programmer/Index.cshtml skiller egen rad via erEgen, samme mønster.
    /// </summary>
    public Task<List<Behandlingsprogram>> HentDeltMedAlleAsync(long behandlerId, CancellationToken cancellationToken = default) =>
        _db.Behandlingsprogrammer.Where(p => !p.ErArkivert && p.ErDeltMedAlle)
            .OrderByDescending(p => p.OpprettetUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Behandlingsprogram>> HentDeltMedPartnerAsync(long behandlerId, CancellationToken cancellationToken = default)
    {
        var partnerId = await _db.Behandlere.Where(b => b.Id == behandlerId).Select(b => b.PartnerId).FirstOrDefaultAsync(cancellationToken);
        if (partnerId is null)
        {
            return Array.Empty<Behandlingsprogram>();
        }
        var partnerBehandlerIder = await _db.Behandlere.Where(b => b.PartnerId == partnerId).Select(b => b.Id).ToListAsync(cancellationToken);
        return await _db.Behandlingsprogrammer
            .Where(p => !p.ErArkivert && p.ErDeltMedPartner && partnerBehandlerIder.Contains(p.OpprettetAvBehandlerId))
            .OrderByDescending(p => p.OpprettetUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<long>> HentLikteProgramIderAsync(long behandlerId, CancellationToken cancellationToken = default) =>
        (await _db.ProgramLikinger.Where(l => l.BehandlerId == behandlerId).Select(l => l.ProgramId).ToListAsync(cancellationToken)).ToHashSet();

    public sealed record ProgramMedDrops(Behandlingsprogram Program, IReadOnlyList<ProgramDrop> Drops, IReadOnlyDictionary<long, List<long>> TestIderPerDropId);

    public async Task<ProgramMedDrops?> HentMedDropsAsync(long programId, CancellationToken cancellationToken = default)
    {
        var program = await _db.Behandlingsprogrammer.FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);
        if (program is null)
        {
            return null;
        }
        var drops = await _db.ProgramDrops.Where(d => d.ProgramId == programId).OrderBy(d => d.Rekkefolge).ToListAsync(cancellationToken);
        var testIderPerDrop = new Dictionary<long, List<long>>();
        foreach (var drop in drops)
        {
            testIderPerDrop[drop.Id] = await _db.ProgramDropTester.Where(t => t.ProgramDropId == drop.Id).OrderBy(t => t.Rekkefolge).Select(t => t.TestId).ToListAsync(cancellationToken);
        }
        return new ProgramMedDrops(program, drops, testIderPerDrop);
    }

    // =========================================================================================
    // Tildeling + kjøremotor
    // =========================================================================================

    /// <summary>Beregner et randomisert, forpliktet tidspunkt innenfor [fra,til] på en gitt dato, norsk tid — unngåNatt KLEMMER inn mot 07:00 i stedet for å kaste vinduet (enkelt, forutsigbart, aldri en uendelig løkke selv om HELE vinduet er natt).</summary>
    public static DateTimeOffset RandomiserTidspunkt(DateOnly dag, TimeSpan fra, TimeSpan til, bool unngaaNatt, Random? rng = null)
    {
        rng ??= Random.Shared;
        var fraSek = fra.TotalSeconds;
        var tilSek = Math.Max(til.TotalSeconds, fraSek);
        var valgtSek = fraSek + rng.NextDouble() * (tilSek - fraSek);
        var valgtKlokkeslett = TimeSpan.FromSeconds(valgtSek);

        if (unngaaNatt && (valgtKlokkeslett >= NattStart || valgtKlokkeslett < NattSlutt))
        {
            valgtKlokkeslett = NattSlutt;
        }

        var lokalDatoTid = dag.ToDateTime(TimeOnly.FromTimeSpan(valgtKlokkeslett));
        return new DateTimeOffset(lokalDatoTid, NorskTid.GetUtcOffset(lokalDatoTid));
    }

    /// <summary>Dagen (norsk lokal dato) en gitt drop (0-basert indeks i Rekkefolge-sortert liste) faktisk fyres av på, gitt programmets dag-0-anker.</summary>
    private static DateOnly BeregnDropDag(DateOnly dag0, IReadOnlyList<ProgramDrop> dropsSortert, int droppIndeks)
    {
        var dag = dag0;
        for (var i = 1; i <= droppIndeks; i++)
        {
            dag = dag.AddDays(dropsSortert[i].DagerEtterForrige);
        }
        return dag;
    }

    /// <summary>
    /// Tildeler et program til én eller flere pasienter (direkte, eller fra en gruppe — se
    /// gruppeId). ALLE deltakere får SAMME ProgramStartUtc (brukerens eksplisitte svar: "same day
    /// for all"), men hver sin egen ProgramDeltakelse-rad sporet uavhengig deretter. Oppretter
    /// INGEN TestTildeling ennå — kun selve deltakelsen + det forpliktede tidspunktet for drop 0,
    /// se ProgramBakgrunnstjeneste for selve fyringen.
    /// </summary>
    public async Task<int> TildelAsync(
        long programId, IReadOnlyList<long> pasientIder, long? gruppeId, long? behandlerId, long? administratorId,
        CancellationToken cancellationToken = default)
    {
        if (behandlerId is null == administratorId is null)
        {
            throw new ArgumentException("Nøyaktig én av behandlerId/administratorId skal være satt.");
        }

        var program = await _db.Behandlingsprogrammer.FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);
        if (program is null)
        {
            return 0;
        }
        var drops = await _db.ProgramDrops.Where(d => d.ProgramId == programId).OrderBy(d => d.Rekkefolge).ToListAsync(cancellationToken);
        if (drops.Count == 0)
        {
            return 0;
        }

        var naaUtc = DateTimeOffset.UtcNow;
        var programStartUtc = PlanlagtTildelingService.BeregnNesteForekomstUtc(naaUtc, program.StartUkedag, program.StartKlokkeslett);
        var dag0 = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(programStartUtc, NorskTid).DateTime);
        var forsteDroppTidspunkt = RandomiserTidspunkt(dag0, drops[0].FraKlokkeslett, drops[0].TilKlokkeslett, drops[0].UnngaaNatt);

        var antall = 0;
        foreach (var pasientId in pasientIder)
        {
            _db.ProgramDeltakelser.Add(new ProgramDeltakelse
            {
                ProgramId = programId,
                PasientId = pasientId,
                GruppeId = gruppeId,
                TildeltAvBehandlerId = behandlerId,
                TildeltAvAdministratorId = administratorId,
                ProgramStartUtc = programStartUtc,
                NaavaerendeDroppIndeks = 0,
                NesteDroppPlanlagtUtc = forsteDroppTidspunkt,
                OpprettetUtc = naaUtc
            });
            antall++;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return antall;
    }

    /// <summary>Kalt av ProgramBakgrunnstjeneste — fyrer av drops hvis forpliktede tidspunkt har passert for aktive (ikke pauset/meldt ut) deltakelser.</summary>
    public async Task<int> FyrAvDueAsync(DateTimeOffset naaUtc, string baseUrl, CancellationToken cancellationToken = default)
    {
        var due = await _db.ProgramDeltakelser
            .Where(d => d.NesteDroppPlanlagtUtc != null && d.NesteDroppPlanlagtUtc <= naaUtc
                        && d.PauseUtc == null && d.MeldtUtUtc == null && d.FullfortUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var deltakelse in due)
        {
            await FyrAvDroppAsync(deltakelse, baseUrl, cancellationToken);
        }
        return due.Count;
    }

    /// <summary>Oppretter TestTildeling for KUN det FØRSTE testen i gjeldende drop — resten av droppen kjedes via HaandterFullfortTestAsync etter hvert som pasienten fullfører.</summary>
    private async Task FyrAvDroppAsync(ProgramDeltakelse deltakelse, string baseUrl, CancellationToken cancellationToken)
    {
        var drops = await _db.ProgramDrops.Where(d => d.ProgramId == deltakelse.ProgramId).OrderBy(d => d.Rekkefolge).ToListAsync(cancellationToken);
        if (deltakelse.NaavaerendeDroppIndeks >= drops.Count)
        {
            deltakelse.FullfortUtc = DateTimeOffset.UtcNow;
            deltakelse.NesteDroppPlanlagtUtc = null;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var gjeldendeDrop = drops[deltakelse.NaavaerendeDroppIndeks];
        var testIder = await _db.ProgramDropTester.Where(t => t.ProgramDropId == gjeldendeDrop.Id).OrderBy(t => t.Rekkefolge).Select(t => t.TestId).ToListAsync(cancellationToken);
        if (testIder.Count == 0)
        {
            // Tom drop (forfatter la ikke til noen tester) — hopp rett videre til neste drop sin planlegging.
            await PlanleggNesteDroppEllerFullforAsync(deltakelse, drops, cancellationToken);
            return;
        }

        await OpprettOgVarsleTestIDroppAsync(deltakelse, gjeldendeDrop.Id, testIder[0], rekkefolgeIDrop: 1, erFoersteIProgrammet: deltakelse.NaavaerendeDroppIndeks == 0, baseUrl, cancellationToken);

        // Selve droppens "fyringstidspunkt" er brukt opp — videre fremdrift styres av
        // HaandterFullfortTestAsync (kjeding innad i droppen) / PlanleggNesteDroppEllerFullforAsync
        // (når droppen er helt ferdig), IKKE flere polling-treff på denne raden.
        deltakelse.NesteDroppPlanlagtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Kalt FRA Pasientportal/Tester/Fyll.cshtml.cs rett etter TestService.LagreSvarAsync med
    /// markerFullfort=true — stille no-op hvis denne tildelingen ikke stammer fra et program.
    /// </summary>
    public async Task HaandterFullfortTestAsync(long tildelingId, string baseUrl, CancellationToken cancellationToken = default)
    {
        var kobling = await _db.ProgramTildelinger.FirstOrDefaultAsync(k => k.TestTildelingId == tildelingId, cancellationToken);
        if (kobling is null)
        {
            return;
        }

        var deltakelse = await _db.ProgramDeltakelser.FirstOrDefaultAsync(d => d.Id == kobling.ProgramDeltakelseId, cancellationToken);
        if (deltakelse is null || deltakelse.MeldtUtUtc is not null)
        {
            return;
        }

        var testIderIDrop = await _db.ProgramDropTester.Where(t => t.ProgramDropId == kobling.ProgramDropId).OrderBy(t => t.Rekkefolge).Select(t => t.TestId).ToListAsync(cancellationToken);
        var nesteIndeksIDrop = kobling.RekkefolgeIDrop; // 1-basert lagret, så dette ER indeksen til NESTE test (0-basert i listen)
        if (nesteIndeksIDrop < testIderIDrop.Count)
        {
            await OpprettOgVarsleTestIDroppAsync(deltakelse, kobling.ProgramDropId, testIderIDrop[nesteIndeksIDrop], nesteIndeksIDrop + 1, erFoersteIProgrammet: false, baseUrl, cancellationToken);
            return;
        }

        // Droppen er ferdig — planlegg neste, eller marker hele programmet fullført.
        var drops = await _db.ProgramDrops.Where(d => d.ProgramId == deltakelse.ProgramId).OrderBy(d => d.Rekkefolge).ToListAsync(cancellationToken);
        deltakelse.NaavaerendeDroppIndeks++;
        await PlanleggNesteDroppEllerFullforAsync(deltakelse, drops, cancellationToken);
    }

    private async Task PlanleggNesteDroppEllerFullforAsync(ProgramDeltakelse deltakelse, IReadOnlyList<ProgramDrop> dropsSortert, CancellationToken cancellationToken)
    {
        if (deltakelse.NaavaerendeDroppIndeks >= dropsSortert.Count)
        {
            deltakelse.FullfortUtc = DateTimeOffset.UtcNow;
            deltakelse.NesteDroppPlanlagtUtc = null;
        }
        else
        {
            var dag0 = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(deltakelse.ProgramStartUtc, NorskTid).DateTime);
            var dropDag = BeregnDropDag(dag0, dropsSortert, deltakelse.NaavaerendeDroppIndeks);
            var drop = dropsSortert[deltakelse.NaavaerendeDroppIndeks];
            deltakelse.NesteDroppPlanlagtUtc = RandomiserTidspunkt(dropDag, drop.FraKlokkeslett, drop.TilKlokkeslett, drop.UnngaaNatt);
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task OpprettOgVarsleTestIDroppAsync(
        ProgramDeltakelse deltakelse, long dropId, long testId, int rekkefolgeIDrop, bool erFoersteIProgrammet, string baseUrl, CancellationToken cancellationToken)
    {
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == deltakelse.PasientId, cancellationToken);
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (pasient is null || test is null)
        {
            return;
        }

        var tildeling = await _testService.TildelAsync(
            testId, pasient.Id, behandlerId: deltakelse.TildeltAvBehandlerId, administratorId: deltakelse.TildeltAvAdministratorId,
            frist: null, varighetMinutter: null, cancellationToken: cancellationToken);

        _db.ProgramTildelinger.Add(new ProgramTildeling
        {
            ProgramDeltakelseId = deltakelse.Id,
            ProgramDropId = dropId,
            TestTildelingId = tildeling.Id,
            RekkefolgeIDrop = rekkefolgeIDrop
        });

        // Betalingsregel (brukerens eksplisitte krav): KUN første drops FØRSTE test kan noensinne
        // koste pasienten noe — alt annet (senere tester i samme drop, ALLE senere drops) er
        // ALLTID 0/IkkePåkrevd. Samme "prøvepasient betaler aldri"-unntak som ellers i systemet.
        var erProvepasient = string.IsNullOrWhiteSpace(pasient.Personnummer);
        var kanKoste = erFoersteIProgrammet && rekkefolgeIDrop == 1 && !erProvepasient;
        var pris = kanKoste && test.StorstePrisKr > 0
            ? _prisberegner.Beregn(test, dekketAvAbonnement: false, onsketHonorarKr: null, effektivPartnerAndelKr: null)
            : null;

        _db.TestTildelingBetalinger.Add(new TestTildelingBetaling
        {
            TestTildelingId = tildeling.Id,
            PasientTotalprisKr = pris?.PasientTotalprisKr ?? 0m,
            BehandlerHonorarKr = pris?.BehandlerHonorarKr ?? 0m,
            PlattformAndelKr = pris?.PlattformAndelKr ?? 0m,
            PartnerAndelKr = pris?.PartnerAndelKr,
            DekketAvAbonnement = pris?.DekketAvAbonnement ?? false,
            Status = pris is null || pris.PasientTotalprisKr <= 0m ? BetalingStatus.IkkePakrevd : BetalingStatus.Venter,
            OpprettetUtc = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        var lenke = $"{baseUrl.TrimEnd('/')}/Pasientportal/Tester/Fyll/{tildeling.Id}";
        var melding = $"En ny test venter på deg: {lenke}";
        await VarsleBestEffortAsync(pasient, melding, "Ny test tilgjengelig i PsyTest", cancellationToken);
    }

    /// <summary>Samme "feiltolerant varsling"-prinsipp som TestTildelingsService/PasientInvitasjonService — en SMS-/e-postfeil skal ALDRI velte selve drop-fyringen, som allerede er lagret i databasen.</summary>
    private async Task VarsleBestEffortAsync(Pasient pasient, string meldingstekst, string epostEmne, CancellationToken cancellationToken)
    {
        var vilSms = pasient.Varslingspreferanse is Varslingspreferanse.Sms or Varslingspreferanse.Begge;
        var vilEpost = pasient.Varslingspreferanse is Varslingspreferanse.Epost or Varslingspreferanse.Begge;
        var harMobil = !string.IsNullOrWhiteSpace(pasient.MobilNr);
        var harEpost = !string.IsNullOrWhiteSpace(pasient.Email);
        if (!(vilSms && harMobil) && !(vilEpost && harEpost))
        {
            vilSms = harMobil;
            vilEpost = harEpost;
        }

        if (vilSms && harMobil)
        {
            try { await _sms.SendAsync(pasient.MobilNr, meldingstekst, cancellationToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Kunne ikke sende program-SMS til pasient {PasientId}.", pasient.Id);
            }
        }
        if (vilEpost && harEpost)
        {
            try { await _email.SendAsync(pasient.Email, epostEmne, meldingstekst, cancellationToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Kunne ikke sende program-e-post til pasient {PasientId}.", pasient.Id);
            }
        }
    }

    // =========================================================================================
    // Pasientopplevelse: pause/meld ut
    // =========================================================================================

    /// <summary>Finner den AKTIVE ProgramDeltakelse en gitt TestTildeling stammer fra — null hvis tildelingen ikke er program-generert.</summary>
    public async Task<ProgramDeltakelse?> FinnDeltakelseForTildelingAsync(long tildelingId, CancellationToken cancellationToken = default)
    {
        var kobling = await _db.ProgramTildelinger.FirstOrDefaultAsync(k => k.TestTildelingId == tildelingId, cancellationToken);
        if (kobling is null)
        {
            return null;
        }
        return await _db.ProgramDeltakelser.FirstOrDefaultAsync(d => d.Id == kobling.ProgramDeltakelseId, cancellationToken);
    }

    /// <summary>Stopper FREMTIDIGE drops — allerede igangsatte tester i gjeldende drop fullføres normalt (brukerens eksplisitte svar).</summary>
    public async Task PauseAsync(long deltakelseId, CancellationToken cancellationToken = default)
    {
        var deltakelse = await _db.ProgramDeltakelser.FirstOrDefaultAsync(d => d.Id == deltakelseId, cancellationToken);
        if (deltakelse is null || deltakelse.MeldtUtUtc is not null)
        {
            return;
        }
        deltakelse.PauseUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await OpprettBehandlerOppgaveAsync(deltakelse, "pauset", cancellationToken);
    }

    public async Task GjenopptaAsync(long deltakelseId, CancellationToken cancellationToken = default)
    {
        var deltakelse = await _db.ProgramDeltakelser.FirstOrDefaultAsync(d => d.Id == deltakelseId, cancellationToken);
        if (deltakelse is null || deltakelse.MeldtUtUtc is not null || deltakelse.PauseUtc is null)
        {
            return;
        }
        deltakelse.PauseUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>TERMINAL — ingen flere drops noensinne. Allerede igangsatte tester fullføres normalt.</summary>
    public async Task MeldUtAsync(long deltakelseId, CancellationToken cancellationToken = default)
    {
        var deltakelse = await _db.ProgramDeltakelser.FirstOrDefaultAsync(d => d.Id == deltakelseId, cancellationToken);
        if (deltakelse is null || deltakelse.MeldtUtUtc is not null)
        {
            return;
        }
        deltakelse.MeldtUtUtc = DateTimeOffset.UtcNow;
        deltakelse.NesteDroppPlanlagtUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
        await OpprettBehandlerOppgaveAsync(deltakelse, "meldte seg ut av", cancellationToken);
    }

    private async Task OpprettBehandlerOppgaveAsync(ProgramDeltakelse deltakelse, string handling, CancellationToken cancellationToken)
    {
        if (deltakelse.TildeltAvBehandlerId is null)
        {
            return; // Admin-tildelt — ingen behandler-oppgaveliste å varsle (se bevisst scope-avgrensning i beslutningsloggen).
        }
        var pasientNavn = await _db.Pasienter.Where(p => p.Id == deltakelse.PasientId).Select(p => p.Navn).FirstOrDefaultAsync(cancellationToken);
        var programNavn = await _db.Behandlingsprogrammer.Where(p => p.Id == deltakelse.ProgramId).Select(p => p.Navn).FirstOrDefaultAsync(cancellationToken);
        var tekst = $"{pasientNavn ?? "(ikke fullført registrering)"} {handling} programmet \"{programNavn}\" kl. {DateTimeOffset.UtcNow:g}";
        await _meldingService.OpprettFritekstAsync(deltakelse.TildeltAvBehandlerId.Value, tekst, cancellationToken);
    }

    // =========================================================================================
    // "Kjørende"-oversikt (behandler/admin/superadmin/partner-admin)
    // =========================================================================================

    public sealed record KjorendeRad(
        long ProgramId, string ProgramNavn, long? GruppeId, string? GruppeNavn, string? PasientNavn,
        int AntallDeltakere, DateTimeOffset Startet, int AntallGjenstaendeDrops, string? TreaterNavn,
        IReadOnlyList<long> DeltakelseIder);

    /// <summary>Behandlers EGNE kjørende programmer (tildelt av DEM). Aggregert PER (Program,Gruppe) — se brukerens svar "aggregated count", ikke per-medlem.</summary>
    public async Task<IReadOnlyList<KjorendeRad>> HentKjorendeForBehandlerAsync(long behandlerId, CancellationToken cancellationToken = default)
    {
        var deltakelser = await _db.ProgramDeltakelser
            .Where(d => d.TildeltAvBehandlerId == behandlerId && d.FullfortUtc == null && d.MeldtUtUtc == null)
            .ToListAsync(cancellationToken);
        return await AggregerKjorendeAsync(deltakelser, cancellationToken);
    }

    /// <summary>Admin/superadmin/partner-admin sitt syn — ALLE kjørende programmer, uansett hvem som tildelte (vist med "(behandlernavn) Programnavn", brukerens eksplisitte format).</summary>
    public async Task<IReadOnlyList<KjorendeRad>> HentAlleKjorendeAsync(CancellationToken cancellationToken = default)
    {
        var deltakelser = await _db.ProgramDeltakelser.Where(d => d.FullfortUtc == null && d.MeldtUtUtc == null).ToListAsync(cancellationToken);
        return await AggregerKjorendeAsync(deltakelser, cancellationToken);
    }

    private async Task<IReadOnlyList<KjorendeRad>> AggregerKjorendeAsync(List<ProgramDeltakelse> deltakelser, CancellationToken cancellationToken)
    {
        var programNavn = await _db.Behandlingsprogrammer.ToDictionaryAsync(p => p.Id, p => p.Navn, cancellationToken);
        var gruppeNavn = await _db.Grupper.ToDictionaryAsync(g => g.Id, g => g.Navn, cancellationToken);
        var pasientNavn = await _db.Pasienter.ToDictionaryAsync(p => p.Id, p => p.Navn, cancellationToken);
        var behandlerNavn = await _db.Behandlere.ToListAsync(cancellationToken);
        var dropAntallPerProgram = await _db.ProgramDrops.GroupBy(d => d.ProgramId).Select(g => new { g.Key, Antall = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Antall, cancellationToken);

        var grupper = deltakelser.GroupBy(d => (d.ProgramId, d.GruppeId));
        var resultat = new List<KjorendeRad>();
        foreach (var gruppe in grupper)
        {
            var forste = gruppe.First();
            var treater = behandlerNavn.FirstOrDefault(b => b.Id == forste.TildeltAvBehandlerId);
            resultat.Add(new KjorendeRad(
                forste.ProgramId, programNavn.GetValueOrDefault(forste.ProgramId, "(ukjent program)"),
                forste.GruppeId, forste.GruppeId is null ? null : gruppeNavn.GetValueOrDefault(forste.GruppeId.Value),
                forste.GruppeId is null ? pasientNavn.GetValueOrDefault(forste.PasientId) : null,
                gruppe.Count(), forste.ProgramStartUtc,
                Math.Max(0, dropAntallPerProgram.GetValueOrDefault(forste.ProgramId) - forste.NaavaerendeDroppIndeks),
                treater?.Visningsnavn, gruppe.Select(d => d.Id).ToList()));
        }
        return resultat.OrderByDescending(r => r.Startet).ToList();
    }

    /// <summary>Pauser/fjerner ALLE deltakelser i en aggregert "kjørende"-rad på én gang (admin/superadmin/partner-admin sin handling, brukerens eksplisitte krav).</summary>
    public async Task PauseFlereAsync(IReadOnlyList<long> deltakelseIder, CancellationToken cancellationToken = default)
    {
        foreach (var id in deltakelseIder)
        {
            await PauseAsync(id, cancellationToken);
        }
    }

    public async Task FjernFlereAsync(IReadOnlyList<long> deltakelseIder, CancellationToken cancellationToken = default)
    {
        foreach (var id in deltakelseIder)
        {
            await MeldUtAsync(id, cancellationToken);
        }
    }
}
