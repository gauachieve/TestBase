using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester.Skaaring;

namespace TestBase.Shared.Domain.Tester;

public sealed record TestMedInnhold(
    TestTildeling Tildeling,
    Test Test,
    IReadOnlyList<TestSide> Sider,
    IReadOnlyList<TestLedd> AlleLedd,
    IReadOnlyDictionary<long, string> EksisterendeSvar);

public sealed record SkaaringHistorikkPunkt(TestTildeling Tildeling, TestSkaaring Skaaring);

/// <summary>
/// Testmotoren: forfatning av tester (Test/TestSide/TestLedd), tildeling til
/// pasienter, utfylling (lagring av TestSvar side for side), og — fra fase 5 —
/// skåring via registrerte ITestSkaaringsberegner-implementasjoner (se
/// Domain/Tester/Skaaring/), bevist ut med WHO-5.
/// </summary>
public sealed class TestService
{
    private readonly AppDbContext _db;
    private readonly IReadOnlyList<ITestSkaaringsberegner> _skaaringsberegnere;
    private readonly BehandlerMeldingService _meldingService;

    public TestService(AppDbContext db, IEnumerable<ITestSkaaringsberegner> skaaringsberegnere, BehandlerMeldingService meldingService)
    {
        _db = db;
        _skaaringsberegnere = skaaringsberegnere.ToList();
        _meldingService = meldingService;
    }

    public async Task<Test> OpprettTestAsync(
        string navn, string? beskrivelse, string? belonningstekst, string? kode = null,
        CancellationToken cancellationToken = default)
    {
        var test = new Test
        {
            Navn = navn,
            Kode = kode,
            Beskrivelse = beskrivelse,
            Belonningstekst = belonningstekst,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        _db.Tester.Add(test);
        await _db.SaveChangesAsync(cancellationToken);
        return test;
    }

    public Task<bool> FinnesTestMedKodeAsync(string kode, CancellationToken cancellationToken = default) =>
        _db.Tester.AnyAsync(t => t.Kode == kode, cancellationToken);

    public Task<Test?> HentTestVedKodeAsync(string kode, CancellationToken cancellationToken = default) =>
        _db.Tester.FirstOrDefaultAsync(t => t.Kode == kode, cancellationToken);

    public Task<Test?> HentTestAsync(long testId, CancellationToken cancellationToken = default) =>
        _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);

    public async Task<bool> OppdaterTestAsync(
        long testId, string navn, string? beskrivelse, string? belonningstekst, bool erAktiv,
        CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (test is null)
        {
            return false;
        }

        test.Navn = navn;
        test.Beskrivelse = beskrivelse;
        test.Belonningstekst = belonningstekst;
        test.ErAktiv = erAktiv;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Egen metode fremfor et nytt parameter på OppdaterTestAsync — kun brukt
    /// av innebygde testers seedere (se IInnebygdTestSeeder) foreløpig, ingen
    /// admin-UI for dette feltet ennå (bevisst utsatt, jf. beslutningsloggen).
    /// </summary>
    public async Task<bool> SettRapportIntroduksjonAsync(long testId, string? rapportIntroduksjon, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (test is null)
        {
            return false;
        }

        test.RapportIntroduksjon = rapportIntroduksjon;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TestSide> LeggTilSideAsync(long testId, string navn, string? instruksjon, CancellationToken cancellationToken = default)
    {
        var nesteRekkefolge = (await _db.TestSider.Where(s => s.TestId == testId).Select(s => (int?)s.Rekkefolge).MaxAsync(cancellationToken)) ?? 0;
        var side = new TestSide { TestId = testId, Navn = navn, Instruksjon = instruksjon, Rekkefolge = nesteRekkefolge + 1 };
        _db.TestSider.Add(side);
        await _db.SaveChangesAsync(cancellationToken);
        return side;
    }

    public async Task<TestLedd> LeggTilLeddAsync(
        long testSideId, string sporsmalstekst, string? instruksjon, TestSvartype svartype, string? svaralternativer,
        CancellationToken cancellationToken = default)
    {
        var nesteRekkefolge = (await _db.TestLedd.Where(l => l.TestSideId == testSideId).Select(l => (int?)l.Rekkefolge).MaxAsync(cancellationToken)) ?? 0;
        var ledd = new TestLedd
        {
            TestSideId = testSideId,
            Sporsmalstekst = sporsmalstekst,
            Instruksjon = instruksjon,
            Svartype = svartype,
            Svaralternativer = svaralternativer,
            Rekkefolge = nesteRekkefolge + 1
        };
        _db.TestLedd.Add(ledd);
        await _db.SaveChangesAsync(cancellationToken);
        return ledd;
    }

    public Task<List<Test>> HentAktiveTesterAsync(CancellationToken cancellationToken = default) =>
        _db.Tester.Where(t => t.ErAktiv).OrderBy(t => t.Navn).ToListAsync(cancellationToken);

    /// <summary>
    /// De faste kategoriene i tildelingsflytens tre-visning, alfabetisk. Ingen
    /// admin-UI for å opprette/slette kategorier ennå — se beslutningsloggen.
    /// Idempotent: kalles trygt ved hver oppstart, som IInnebygdTestSeeder.
    /// Byttet ut i sin helhet 2026-09 fra de opprinnelige syv (Allianse/Angst/
    /// Depresjon/Funksjon/Kjerne/Nevropsykologiske/Utredning) til Helsebibliotekets
    /// 16 praktiske kategorier for psykologiske/nevropsykologiske skåringsverktøy
    /// (se docs/beslutningslogg.md og kildearket
    /// Helsebiblioteket_psykologiske_og_nevropsykologiske_tester.xlsx, fanen
    /// "Kategorier") — IKKE bare et tillegg, se SikreStandardkategorierAsync sin
    /// opprydding av de gamle navnene.
    /// </summary>
    public static readonly IReadOnlyList<string> StandardKategorier = new[]
    {
        "Kognisjon, demens og nevropsykologisk screening",
        "ADHD, autisme og nevroutvikling",
        "Søvn og døgnrytme",
        "Rus og avhengighet",
        "Spiseforstyrrelser og kroppsbilde",
        "Traumer, dissosiasjon og belastninger",
        "Angst, tvang og relaterte plager",
        "Depresjon og bipolaritet",
        "Psykose og alvorlige psykiske lidelser",
        "Personlighet, relasjoner og sosial fungering",
        "Vold, selvmord og risikovurdering",
        "Seksuell helse og kjønn",
        "Barn og unges psykiske helse – generelt",
        "Funksjon, livskvalitet og behandlingsutfall",
        "Somatiske symptomer, smerte og utmattelse",
        "Diagnostikk, tverrgående og øvrige verktøy"
    };

    /// <summary>
    /// Legger til manglende standardkategorier OG fjerner enhver
    /// TestKategori som IKKE lenger er i StandardKategorier (med sine
    /// TestKategoriKobling-rader) — en reell "bytt ut", ikke bare et tillegg,
    /// se klassekommentaren på StandardKategorier. Trygt: dette er kun
    /// gruppering i tildelingsflytens tre-visning, ikke selve testene eller
    /// tildelingene, som begge lever videre uendret.
    /// </summary>
    public async Task SikreStandardkategorierAsync(CancellationToken cancellationToken = default)
    {
        var eksisterende = await _db.TestKategorier.ToListAsync(cancellationToken);
        var eksisterendeNavn = eksisterende.Select(k => k.Navn).ToList();

        foreach (var navn in StandardKategorier.Except(eksisterendeNavn))
        {
            _db.TestKategorier.Add(new TestKategori { Navn = navn, OpprettetUtc = DateTimeOffset.UtcNow });
        }

        var foreldede = eksisterende.Where(k => !StandardKategorier.Contains(k.Navn)).ToList();
        if (foreldede.Count > 0)
        {
            var foreldedeIder = foreldede.Select(k => k.Id).ToList();
            var koblinger = await _db.TestKategoriKoblinger.Where(kob => foreldedeIder.Contains(kob.TestKategoriId)).ToListAsync(cancellationToken);
            _db.TestKategoriKoblinger.RemoveRange(koblinger);
            _db.TestKategorier.RemoveRange(foreldede);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Idempotent: oppretter ikke en duplikatkobling om testen allerede er i kategorien.</summary>
    public async Task KoblTestTilKategoriAsync(long testId, string kategoriNavn, CancellationToken cancellationToken = default)
    {
        var kategori = await _db.TestKategorier.FirstAsync(k => k.Navn == kategoriNavn, cancellationToken);
        var finnes = await _db.TestKategoriKoblinger.AnyAsync(
            k => k.TestId == testId && k.TestKategoriId == kategori.Id, cancellationToken);
        if (!finnes)
        {
            _db.TestKategoriKoblinger.Add(new TestKategoriKobling { TestId = testId, TestKategoriId = kategori.Id });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public sealed record KategoriMedTester(TestKategori Kategori, IReadOnlyList<Test> Tester);

    /// <summary>
    /// Alle standardkategorier (alfabetisk) med sine aktive tester, til
    /// tildelingsflytens tre-visning. <paramref name="partnerId"/> null (admin,
    /// uavhengig behandler, eller Superadmin sin allow-list-konfigurasjon som
    /// nettopp trenger ALLE tester å velge blant) → ingen filtrering. Satt
    /// (en partner-tilknyttet behandler skal tildele) → viser KUN tester på
    /// partnerens PartnerTestTilgang-allow-list, se
    /// docs/beslutningslogg.md "Partner System + Test Monetization".
    /// </summary>
    public async Task<IReadOnlyList<KategoriMedTester>> HentKategoriTreAsync(
        long? partnerId = null, CancellationToken cancellationToken = default)
    {
        var kategorier = await _db.TestKategorier.OrderBy(k => k.Navn).ToListAsync(cancellationToken);
        var koblinger = await _db.TestKategoriKoblinger.ToListAsync(cancellationToken);
        var aktiveTester = await _db.Tester.Where(t => t.ErAktiv).ToDictionaryAsync(t => t.Id, cancellationToken);

        HashSet<long>? tillatteTestIder = null;
        if (partnerId is not null)
        {
            tillatteTestIder = (await _db.PartnerTestTilganger
                .Where(t => t.PartnerId == partnerId.Value)
                .Select(t => t.TestId)
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        return kategorier.Select(k =>
        {
            var testIder = koblinger.Where(kob => kob.TestKategoriId == k.Id).Select(kob => kob.TestId);
            var tester = testIder.Select(id => aktiveTester.GetValueOrDefault(id)).Where(t => t is not null)
                .Select(t => t!)
                .Where(t => tillatteTestIder is null || tillatteTestIder.Contains(t.Id))
                .OrderBy(t => t.Navn).ToList();
            return new KategoriMedTester(k, tester);
        }).ToList();
    }

    public async Task<TestTildeling> TildelAsync(
        long testId, long pasientId, long? behandlerId, long? administratorId, DateTimeOffset? frist, int? varighetMinutter,
        CancellationToken cancellationToken = default)
    {
        if (behandlerId is null == administratorId is null)
        {
            throw new ArgumentException("Nøyaktig én av behandlerId/administratorId skal være satt.");
        }

        var tildeling = new TestTildeling
        {
            TestId = testId,
            PasientId = pasientId,
            TildeltAvBehandlerId = behandlerId,
            TildeltAvAdministratorId = administratorId,
            TildeltUtc = DateTimeOffset.UtcNow,
            Frist = frist,
            VarighetMinutter = varighetMinutter
        };
        _db.TestTildelinger.Add(tildeling);
        await _db.SaveChangesAsync(cancellationToken);
        return tildeling;
    }

    /// <summary>
    /// Siste honorar denne behandleren selv valgte for denne testen (se
    /// TestTildelingBetaling.BehandlerHonorarKr) — brukt til å forhåndsutfylle
    /// honorarfeltet i tildelingsflyten. Faller tilbake til
    /// Test.TypiskBehandlerHonorarKr når behandleren aldri har tildelt denne
    /// testen før.
    /// </summary>
    public async Task<decimal> HentSisteHonorarAsync(long behandlerId, long testId, CancellationToken cancellationToken = default)
    {
        var sisteHonorar = await _db.TestTildelinger
            .Where(t => t.TildeltAvBehandlerId == behandlerId && t.TestId == testId)
            .OrderByDescending(t => t.TildeltUtc)
            .Join(_db.TestTildelingBetalinger, t => t.Id, b => b.TestTildelingId, (t, b) => (decimal?)b.BehandlerHonorarKr)
            .FirstOrDefaultAsync(cancellationToken);

        if (sisteHonorar is not null)
        {
            return sisteHonorar.Value;
        }

        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        return test?.TypiskBehandlerHonorarKr ?? 0m;
    }

    public Task<TestTildelingBetaling?> HentBetalingAsync(long tildelingId, CancellationToken cancellationToken = default) =>
        _db.TestTildelingBetalinger.FirstOrDefaultAsync(b => b.TestTildelingId == tildelingId, cancellationToken);

    /// <summary>
    /// Markerer en tildelings betaling som fullført og skriver
    /// regnskapsloggen (Pengebevegelse) — kalt fra BÅDE webhook-mottakeren
    /// (autoritativ, se PaymentWebhooks.cs) OG betalings-retur-siden (synkron
    /// fallback, viktig for Vipps siden webhook-signaturen ikke er
    /// live-verifisert ennå, se docs/beslutningslogg.md). Idempotent — andre
    /// kall etter det første er en no-op, slik at webhook og retur-side ikke
    /// dobbeltfører pengebevegelser uansett rekkefølge de skjer i.
    /// </summary>
    public async Task<bool> MarkerBetalingBetaltAsync(
        long tildelingId, BetalingMetode metode, string? leverandorReferanse, CancellationToken cancellationToken = default)
    {
        var betaling = await _db.TestTildelingBetalinger.FirstOrDefaultAsync(b => b.TestTildelingId == tildelingId, cancellationToken);
        if (betaling is null || betaling.Status == BetalingStatus.Betalt)
        {
            return false;
        }

        betaling.Status = BetalingStatus.Betalt;
        betaling.Metode = metode;
        betaling.BetalingsleverandorReferanse = leverandorReferanse;
        betaling.BetaltUtc = DateTimeOffset.UtcNow;

        var tildeling = await _db.TestTildelinger.FirstAsync(t => t.Id == tildelingId, cancellationToken);
        var na = DateTimeOffset.UtcNow;

        _db.Pengebevegelser.Add(new Pengebevegelse
        {
            Type = PengebevegelseType.PasientBetalingMottatt,
            BelopKr = betaling.PasientTotalprisKr,
            TestTildelingId = tildelingId,
            BehandlerId = tildeling.TildeltAvBehandlerId,
            PartnerId = betaling.PartnerId,
            OpprettetUtc = na
        });
        _db.Pengebevegelser.Add(new Pengebevegelse
        {
            Type = PengebevegelseType.PlattformInntekt,
            BelopKr = betaling.PlattformAndelKr,
            TestTildelingId = tildelingId,
            OpprettetUtc = na
        });

        if (betaling.PartnerAndelKr is > 0)
        {
            _db.Pengebevegelser.Add(new Pengebevegelse
            {
                Type = PengebevegelseType.PartnerAndel,
                BelopKr = betaling.PartnerAndelKr.Value,
                TestTildelingId = tildelingId,
                PartnerId = betaling.PartnerId,
                OpprettetUtc = na
            });
        }

        if (betaling.BehandlerHonorarKr > 0)
        {
            _db.Pengebevegelser.Add(new Pengebevegelse
            {
                Type = PengebevegelseType.BehandlerHonorar,
                BelopKr = betaling.BehandlerHonorarKr,
                TestTildelingId = tildelingId,
                BehandlerId = tildeling.TildeltAvBehandlerId,
                OpprettetUtc = na
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<List<TestTildeling>> HentTildelingerForPasientAsync(long pasientId, CancellationToken cancellationToken = default) =>
        _db.TestTildelinger.Where(t => t.PasientId == pasientId).OrderByDescending(t => t.TildeltUtc).ToListAsync(cancellationToken);

    public sealed record TildelingTelling(int Tildelt, int Besvart);

    /// <summary>Antall tildelte og antall besvarte (Fullfort) tester per pasient — til pasientlistene (behandler/admin), ikke bare én pasient om gangen.</summary>
    public async Task<IReadOnlyDictionary<long, TildelingTelling>> HentTildelingTellingerAsync(
        IReadOnlyCollection<long> pasientIder, CancellationToken cancellationToken = default)
    {
        if (pasientIder.Count == 0)
        {
            return new Dictionary<long, TildelingTelling>();
        }

        var tildelinger = await _db.TestTildelinger
            .Where(t => pasientIder.Contains(t.PasientId))
            .Select(t => new { t.PasientId, t.Status })
            .ToListAsync(cancellationToken);

        return tildelinger
            .GroupBy(t => t.PasientId)
            .ToDictionary(g => g.Key, g => new TildelingTelling(g.Count(), g.Count(t => t.Status == TestTildelingStatus.Fullfort)));
    }

    public async Task<TestMedInnhold?> HentTildelingMedInnholdAsync(long tildelingId, CancellationToken cancellationToken = default)
    {
        var tildeling = await _db.TestTildelinger.FirstOrDefaultAsync(t => t.Id == tildelingId, cancellationToken);
        if (tildeling is null)
        {
            return null;
        }

        var test = await _db.Tester.FirstAsync(t => t.Id == tildeling.TestId, cancellationToken);
        var sider = await _db.TestSider.Where(s => s.TestId == test.Id).OrderBy(s => s.Rekkefolge).ToListAsync(cancellationToken);
        var sideIder = sider.Select(s => s.Id).ToList();
        var alleLedd = await _db.TestLedd.Where(l => sideIder.Contains(l.TestSideId)).OrderBy(l => l.Rekkefolge).ToListAsync(cancellationToken);
        var svar = await _db.TestSvar.Where(s => s.TestTildelingId == tildelingId)
            .ToDictionaryAsync(s => s.TestLeddId, s => s.SvarVerdi, cancellationToken);

        return new TestMedInnhold(tildeling, test, sider, alleLedd, svar);
    }

    /// <summary>
    /// Lagrer svarene for én side. Setter Status=Startet ved første lagring
    /// uansett side, og Status=Fullfort+FullfortUtc kun når
    /// <paramref name="markerFullfort"/> er true (Ferdig-knappen, kun vist på
    /// siste side) — IKKE bare fordi det tilfeldigvis er siste side, for å
    /// holde intensjon og posisjon adskilt.
    /// </summary>
    public async Task LagreSvarAsync(
        long tildelingId, IReadOnlyDictionary<long, string> svarPerLeddId, bool markerFullfort,
        CancellationToken cancellationToken = default)
    {
        var tildeling = await _db.TestTildelinger.FirstAsync(t => t.Id == tildelingId, cancellationToken);

        if (tildeling.Status == TestTildelingStatus.Tildelt)
        {
            tildeling.Status = TestTildelingStatus.Startet;
            tildeling.StartetUtc = DateTimeOffset.UtcNow;
        }

        foreach (var (leddId, verdi) in svarPerLeddId)
        {
            if (string.IsNullOrWhiteSpace(verdi))
            {
                continue;
            }

            var eksisterende = await _db.TestSvar.FirstOrDefaultAsync(
                s => s.TestTildelingId == tildelingId && s.TestLeddId == leddId, cancellationToken);

            if (eksisterende is not null)
            {
                eksisterende.SvarVerdi = verdi;
                eksisterende.BesvartUtc = DateTimeOffset.UtcNow;
            }
            else
            {
                _db.TestSvar.Add(new TestSvar
                {
                    TestTildelingId = tildelingId,
                    TestLeddId = leddId,
                    SvarVerdi = verdi,
                    BesvartUtc = DateTimeOffset.UtcNow
                });
            }
        }

        if (markerFullfort)
        {
            tildeling.Status = TestTildelingStatus.Fullfort;
            tildeling.FullfortUtc = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (markerFullfort)
        {
            // Varsler pasientens FAKTISKE behandler (ikke nødvendigvis den som
            // tildelte testen — en admin kan ha tildelt den, se TildelAsync).
            var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == tildeling.PasientId, cancellationToken);
            if (pasient is not null)
            {
                await _meldingService.OpprettAsync(pasient.BehandlerId, tildeling.Id, cancellationToken);
            }
        }
    }

    /// <summary>Krever at tildelingen faktisk er fullført og ikke allerede forkastet.</summary>
    public async Task<bool> GodkjennRapportAsync(long tildelingId, CancellationToken cancellationToken = default)
    {
        var tildeling = await _db.TestTildelinger.FirstOrDefaultAsync(t => t.Id == tildelingId, cancellationToken);
        if (tildeling is null || tildeling.Status != TestTildelingStatus.Fullfort || tildeling.RapportForkastetUtc is not null)
        {
            return false;
        }

        tildeling.RapportGodkjentUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Forkaster en fullført besvarelse i stedet for å godkjenne den (kan ikke
    /// forkastes etter godkjenning — velg det ene eller det andre). Svarene
    /// står urørt for sporbarhet; kalleren (Rapport.cshtml.cs) oppretter og
    /// varsler om en NY tildeling via TestTildelingsService.TildelOgVarsleAsync.
    /// </summary>
    public async Task<bool> ForkastRapportAsync(long tildelingId, CancellationToken cancellationToken = default)
    {
        var tildeling = await _db.TestTildelinger.FirstOrDefaultAsync(t => t.Id == tildelingId, cancellationToken);
        if (tildeling is null || tildeling.Status != TestTildelingStatus.Fullfort || tildeling.RapportGodkjentUtc is not null)
        {
            return false;
        }

        tildeling.RapportForkastetUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Krever at rapporten allerede er godkjent — se RapportGodkjentUtc.</summary>
    public async Task<bool> SettRapportSynlighetAsync(long tildelingId, bool synligForPasient, CancellationToken cancellationToken = default)
    {
        var tildeling = await _db.TestTildelinger.FirstOrDefaultAsync(t => t.Id == tildelingId, cancellationToken);
        if (tildeling is null || tildeling.RapportGodkjentUtc is null)
        {
            return false;
        }

        tildeling.RapportSynligForPasient = synligForPasient;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public sealed record TildelingMedTestOgPasient(TestTildeling Tildeling, string TestNavn, long PasientId, string? PasientNavn);

    /// <summary>Fullførte tester som venter på behandlers godkjenning — behandlers oppgaveliste, jf. beslutningsloggen.</summary>
    public async Task<IReadOnlyList<TildelingMedTestOgPasient>> HentUgodkjenteFullforteForBehandlerAsync(
        long behandlerId, CancellationToken cancellationToken = default)
    {
        var pasientIder = await _db.Pasienter.Where(p => p.BehandlerId == behandlerId).Select(p => p.Id).ToListAsync(cancellationToken);
        var tildelinger = await _db.TestTildelinger
            .Where(t => pasientIder.Contains(t.PasientId) && t.Status == TestTildelingStatus.Fullfort &&
                        t.RapportGodkjentUtc == null && t.RapportForkastetUtc == null)
            .OrderBy(t => t.FullfortUtc)
            .ToListAsync(cancellationToken);
        return await BerikMedTestOgPasientAsync(tildelinger, cancellationToken);
    }

    /// <summary>Tester tildelt behandlers pasienter som ennå ikke er besvart ferdig — kun til oversikt, ingen godkjenning her.</summary>
    public async Task<IReadOnlyList<TildelingMedTestOgPasient>> HentIkkeFullforteForBehandlerAsync(
        long behandlerId, CancellationToken cancellationToken = default)
    {
        var pasientIder = await _db.Pasienter.Where(p => p.BehandlerId == behandlerId).Select(p => p.Id).ToListAsync(cancellationToken);
        var tildelinger = await _db.TestTildelinger
            .Where(t => pasientIder.Contains(t.PasientId) && t.Status != TestTildelingStatus.Fullfort)
            .OrderBy(t => t.TildeltUtc)
            .ToListAsync(cancellationToken);
        return await BerikMedTestOgPasientAsync(tildelinger, cancellationToken);
    }

    private async Task<IReadOnlyList<TildelingMedTestOgPasient>> BerikMedTestOgPasientAsync(
        List<TestTildeling> tildelinger, CancellationToken cancellationToken)
    {
        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var testNavn = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);

        var pasientIder = tildelinger.Select(t => t.PasientId).Distinct().ToList();
        var pasientNavn = await _db.Pasienter.Where(p => pasientIder.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Navn, cancellationToken);

        return tildelinger
            .Select(t => new TildelingMedTestOgPasient(t, testNavn.GetValueOrDefault(t.TestId, "(ukjent test)"), t.PasientId, pasientNavn.GetValueOrDefault(t.PasientId)))
            .ToList();
    }

    /// <summary>
    /// Null hvis testen ikke har noen registrert skåringsberegner (de fleste
    /// admin-forfattede tester vil ikke ha det), eller tildelingen ikke finnes.
    /// </summary>
    public async Task<TestSkaaring?> BeregnSkaaringAsync(long tildelingId, CancellationToken cancellationToken = default)
    {
        var tildeling = await _db.TestTildelinger.FirstOrDefaultAsync(t => t.Id == tildelingId, cancellationToken);
        if (tildeling is null)
        {
            return null;
        }

        var test = await _db.Tester.FirstAsync(t => t.Id == tildeling.TestId, cancellationToken);
        var beregner = FinnBeregner(test.Kode);
        if (beregner is null)
        {
            return null;
        }

        // Sortert etter (side.Rekkefolge, ledd.Rekkefolge) — IKKE bare databasens
        // naturlige rekkefølge, som ikke er garantert, og IKKE bare ledd.Rekkefolge
        // alene (den telles PER SIDE, så side 2 sitt ledd 1 ville ellers sortert
        // før side 1 sitt ledd 5). Nødvendig for skåringsklasser som må
        // ekskludere ett bestemt (typisk siste) ledd fra sumskåren basert på
        // posisjon, se PHQ-9 sitt funksjonsspørsmål.
        var svar = await (
            from s in _db.TestSvar
            join l in _db.TestLedd on s.TestLeddId equals l.Id
            join side in _db.TestSider on l.TestSideId equals side.Id
            where s.TestTildelingId == tildelingId
            orderby side.Rekkefolge, l.Rekkefolge
            select s
        ).ToListAsync(cancellationToken);
        return beregner.BeregnSkaaring(svar);
    }

    /// <summary>
    /// Skåringshistorikk for alle fullførte tildelinger en pasient har av
    /// tester med samme Kode (f.eks. gjentatte WHO-5-administrasjoner over
    /// tid), eldst først — grunnlaget for "rapport over tid".
    /// </summary>
    public async Task<IReadOnlyList<SkaaringHistorikkPunkt>> HentSkaaringHistorikkAsync(
        long pasientId, string testKode, CancellationToken cancellationToken = default)
    {
        var beregner = FinnBeregner(testKode);
        if (beregner is null)
        {
            return Array.Empty<SkaaringHistorikkPunkt>();
        }

        var testIder = await _db.Tester.Where(t => t.Kode == testKode).Select(t => t.Id).ToListAsync(cancellationToken);
        var tildelinger = await _db.TestTildelinger
            .Where(t => t.PasientId == pasientId && testIder.Contains(t.TestId) && t.Status == TestTildelingStatus.Fullfort)
            .OrderBy(t => t.FullfortUtc)
            .ToListAsync(cancellationToken);

        var punkter = new List<SkaaringHistorikkPunkt>();
        foreach (var tildeling in tildelinger)
        {
            var svar = await _db.TestSvar.Where(s => s.TestTildelingId == tildeling.Id).ToListAsync(cancellationToken);
            punkter.Add(new SkaaringHistorikkPunkt(tildeling, beregner.BeregnSkaaring(svar)));
        }

        return punkter;
    }

    public bool HarSkaaringsberegner(string? testKode) => FinnBeregner(testKode) is not null;

    private ITestSkaaringsberegner? FinnBeregner(string? testKode) =>
        testKode is null ? null : _skaaringsberegnere.FirstOrDefault(b => b.TestKode == testKode);
}
