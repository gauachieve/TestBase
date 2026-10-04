using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;

    /// <summary>
    /// En tests EGEN struktur (spørsmål/sider) endres praktisk talt aldri mens
    /// den er i aktiv bruk — men lastes på nytt for HVER GET og HVER POST av
    /// utfyllingssiden, per deltaker, per side. Under en registreringsbølge er
    /// dette identiske spørringer mot SAMME test fra alle deltakerne samtidig,
    /// se docs/beslutningslogg.md "Optimalisering før skalering". 60 sekunder
    /// balanserer "praktisk talt alltid oppdatert nok" mot en reell
    /// ytelsesgevinst — ingen eksplisitt cache-invalidering ved redigering i
    /// Admin/Tester/Sider|Ledd (bevisst utelatt, lav risiko: verste fall er at
    /// en ENDRING i en tests struktur tar opptil 60 sekunder å slå igjennom).
    /// </summary>
    private static readonly TimeSpan TestStrukturCacheTid = TimeSpan.FromSeconds(60);

    public TestService(AppDbContext db, IEnumerable<ITestSkaaringsberegner> skaaringsberegnere, BehandlerMeldingService meldingService, IMemoryCache cache)
    {
        _db = db;
        _skaaringsberegnere = skaaringsberegnere.ToList();
        _meldingService = meldingService;
        _cache = cache;
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

        await GiAllePartnereTilgangTilTestAsync(test.Id, cancellationToken);

        return test;
    }

    /// <summary>
    /// I test perioden skal en test automatisk være tilgjengelig for ALLE
    /// partneres behandlere fra den opprettes. Uten dette var
    /// PartnerTestTilgang-allow-listen (se docs/beslutningslogg.md "Partner
    /// System + Test Monetization") i praksis en blokkeringsliste: en ny test
    /// var usynlig for enhver partner-tilknyttet behandler helt til Superadmin
    /// husket å krysse den av manuelt per partner på Admin/Partnere/Tester —
    /// oppdaget 2026-09-15. Uavhengige (ikke partnertilknyttede) behandlere
    /// berøres ikke av denne allow-listen i det hele tatt, se
    /// HentKategoriTreAsync — de ser alltid alle aktive tester uansett.
    /// GittAvAdministratorId=0 markerer raden som systemgenerert (samme
    /// sentinel-mønster som Areas/Admin/Pages/Partnere/Tester.cshtml.cs sin
    /// fallback når avsender-ID ikke kan parses).
    /// </summary>
    private async Task GiAllePartnereTilgangTilTestAsync(long testId, CancellationToken cancellationToken)
    {
        var partnerIderUtenTilgang = await _db.Partnere
            .Where(p => !p.ErArkivert && !p.ErSlettet)
            .Where(p => !_db.PartnerTestTilganger.Any(t => t.PartnerId == p.Id && t.TestId == testId))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (partnerIderUtenTilgang.Count == 0)
        {
            return;
        }

        var na = DateTimeOffset.UtcNow;
        _db.PartnerTestTilganger.AddRange(partnerIderUtenTilgang.Select(partnerId => new PartnerTestTilgang
        {
            PartnerId = partnerId,
            TestId = testId,
            GittAvAdministratorId = 0,
            OpprettetUtc = na
        }));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retter opp eksisterende hull: sikrer at ALLE partnere har tilgang til
    /// ALLE tester, ikke bare de som opprettes fra nå av (se
    /// GiAllePartnereTilgangTilTestAsync). Kjøres idempotent ved hver
    /// applikasjonsoppstart (Program.cs) — legger kun til manglende par.
    /// </summary>
    public async Task GiAllePartnereTilgangTilAlleTesterAsync(CancellationToken cancellationToken = default)
    {
        var testIder = await _db.Tester.Select(t => t.Id).ToListAsync(cancellationToken);
        foreach (var testId in testIder)
        {
            await GiAllePartnereTilgangTilTestAsync(testId, cancellationToken);
        }
    }

    public sealed record OpprettTestTilgangForespoerselResultat(bool Opprettet, string? Feilmelding);

    /// <summary>
    /// Partner-admins selvbetjente forespørsel om å legge til/fjerne en test
    /// (bugliste 2026-09-15) — se TestTilgangForespoersel. Trer ikke i kraft
    /// før BehandleTestTilgangForesporslerAsync godkjenner den.
    /// </summary>
    public async Task<OpprettTestTilgangForespoerselResultat> OpprettTestTilgangForespoerselAsync(
        long partnerId, long testId, TestTilgangHandling handling, long forespurtAvBehandlerId,
        CancellationToken cancellationToken = default)
    {
        var harAlleredeTilgang = await _db.PartnerTestTilganger.AnyAsync(t => t.PartnerId == partnerId && t.TestId == testId, cancellationToken);
        if (handling == TestTilgangHandling.LeggTil && harAlleredeTilgang)
        {
            return new OpprettTestTilgangForespoerselResultat(false, "Partneren har allerede tilgang til denne testen.");
        }
        if (handling == TestTilgangHandling.Fjern && !harAlleredeTilgang)
        {
            return new OpprettTestTilgangForespoerselResultat(false, "Partneren har ikke tilgang til denne testen fra før.");
        }

        var harVentendeForesporsel = await _db.TestTilgangForesporsler.AnyAsync(
            f => f.PartnerId == partnerId && f.TestId == testId && f.Status == TestTilgangForespoerselStatus.Venter,
            cancellationToken);
        if (harVentendeForesporsel)
        {
            return new OpprettTestTilgangForespoerselResultat(false, "Det finnes allerede en ventende forespørsel for denne testen.");
        }

        _db.TestTilgangForesporsler.Add(new TestTilgangForespoersel
        {
            PartnerId = partnerId,
            TestId = testId,
            Handling = handling,
            ForespurtAvBehandlerId = forespurtAvBehandlerId,
            ForespurtUtc = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new OpprettTestTilgangForespoerselResultat(true, null);
    }

    public Task<List<TestTilgangForespoersel>> HentVentendeTestTilgangForesporslerForPartnerAsync(long partnerId, CancellationToken cancellationToken = default) =>
        _db.TestTilgangForesporsler
            .Where(f => f.PartnerId == partnerId && f.Status == TestTilgangForespoerselStatus.Venter)
            .ToListAsync(cancellationToken);

    public sealed record VentendeTestTilgangForespoerselRad(TestTilgangForespoersel Forespoersel, string PartnerNavn, string TestNavn, string? ForespurtAvNavn);

    /// <summary>Alle ventende forespørsler på tvers av partnere, til Admin/MinSide sin bulk-godkjenningsliste.</summary>
    public async Task<IReadOnlyList<VentendeTestTilgangForespoerselRad>> HentVentendeTestTilgangForesporslerAsync(CancellationToken cancellationToken = default)
    {
        var foresporsler = await _db.TestTilgangForesporsler
            .Where(f => f.Status == TestTilgangForespoerselStatus.Venter)
            .OrderBy(f => f.ForespurtUtc)
            .ToListAsync(cancellationToken);
        if (foresporsler.Count == 0)
        {
            return Array.Empty<VentendeTestTilgangForespoerselRad>();
        }

        var partnerNavnById = await _db.Partnere
            .Where(p => foresporsler.Select(f => f.PartnerId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Navn, cancellationToken);
        var testNavnById = await _db.Tester
            .Where(t => foresporsler.Select(f => f.TestId).Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);
        // Visningsnavn er en ikke-mappet, beregnet C#-egenskap — kan IKKE inngå i en
        // EF-oversatt spørring (ToDictionaryAsync rett på IQueryable). Hent radene
        // først, bygg ordboken i minnet etterpå, se Admin/Pasienter/Index.cshtml.cs
        // sitt tilsvarende mønster.
        var behandlerIder = foresporsler.Select(f => f.ForespurtAvBehandlerId).ToList();
        var behandlerNavnById = (await _db.Behandlere
            .Where(b => behandlerIder.Contains(b.Id))
            .ToListAsync(cancellationToken))
            .ToDictionary(b => b.Id, b => b.Visningsnavn);

        return foresporsler.Select(f => new VentendeTestTilgangForespoerselRad(
            f,
            partnerNavnById.GetValueOrDefault(f.PartnerId, "(ukjent partner)"),
            testNavnById.GetValueOrDefault(f.TestId, "(ukjent test)"),
            behandlerNavnById.GetValueOrDefault(f.ForespurtAvBehandlerId))).ToList();
    }

    /// <summary>
    /// Bulk-godkjenning/avvisning (bugliste 2026-09-15 punkt 6). Godkjenning av
    /// en LeggTil-forespørsel oppretter PartnerTestTilgang, en Fjern-forespørsel
    /// fjerner den — samme effekt som Admin/Partnere/Tester sin direkte
    /// OnPostToggleAsync, bare med denne omveien om forespørsel+godkjenning.
    /// Ukjente/allerede behandlede id-er ignoreres stille (kan skje ved
    /// dobbel-innsending av bulk-skjemaet).
    /// </summary>
    public async Task BehandleTestTilgangForesporslerAsync(
        IReadOnlyCollection<long> foresporselIder, bool godkjenn, long administratorId, CancellationToken cancellationToken = default)
    {
        var foresporsler = await _db.TestTilgangForesporsler
            .Where(f => foresporselIder.Contains(f.Id) && f.Status == TestTilgangForespoerselStatus.Venter)
            .ToListAsync(cancellationToken);

        var na = DateTimeOffset.UtcNow;
        foreach (var f in foresporsler)
        {
            if (godkjenn)
            {
                var eksisterende = await _db.PartnerTestTilganger.FirstOrDefaultAsync(
                    t => t.PartnerId == f.PartnerId && t.TestId == f.TestId, cancellationToken);
                if (f.Handling == TestTilgangHandling.LeggTil && eksisterende is null)
                {
                    _db.PartnerTestTilganger.Add(new PartnerTestTilgang
                    {
                        PartnerId = f.PartnerId,
                        TestId = f.TestId,
                        GittAvAdministratorId = administratorId,
                        OpprettetUtc = na
                    });
                }
                else if (f.Handling == TestTilgangHandling.Fjern && eksisterende is not null)
                {
                    _db.PartnerTestTilganger.Remove(eksisterende);
                }
            }

            f.Status = godkjenn ? TestTilgangForespoerselStatus.Godkjent : TestTilgangForespoerselStatus.Avvist;
            f.BehandletAvAdministratorId = administratorId;
            f.BehandletUtc = na;
        }

        await _db.SaveChangesAsync(cancellationToken);
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

    /// <summary>Samme mønster/begrunnelse som SettRapportIntroduksjonAsync — se Test.IcdElleveKlar.</summary>
    public async Task<bool> SettIcdElleveKlarAsync(long testId, bool icdElleveKlar, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (test is null)
        {
            return false;
        }

        test.IcdElleveKlar = icdElleveKlar;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Se Test.FyllesUtAvBehandler — samme mønster som SettIcdElleveKlarAsync.</summary>
    public async Task<bool> SettFyllesUtAvBehandlerAsync(long testId, bool fyllesUtAvBehandler, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (test is null)
        {
            return false;
        }

        test.FyllesUtAvBehandler = fyllesUtAvBehandler;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Se Test.HarKostnadPerGjennomforing — samme mønster som SettIcdElleveKlarAsync.</summary>
    public async Task<bool> SettHarKostnadPerGjennomforingAsync(long testId, bool harKostnadPerGjennomforing, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (test is null)
        {
            return false;
        }

        test.HarKostnadPerGjennomforing = harKostnadPerGjennomforing;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Se Test.KreverBiologiskKjonn — samme mønster som SettIcdElleveKlarAsync.</summary>
    public async Task<bool> SettKreverBiologiskKjonnAsync(long testId, bool kreverBiologiskKjonn, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (test is null)
        {
            return false;
        }

        test.KreverBiologiskKjonn = kreverBiologiskKjonn;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Samme mønster/begrunnelse som SettRapportIntroduksjonAsync — lar en
    /// seeders "allerede finnes"-gren oppdatere et testnavn (f.eks. en
    /// forenkling gjort etter at testen først ble seedet) uten å opprette
    /// testen på nytt eller røre eksisterende tildelinger/svar.
    /// </summary>
    public async Task<bool> SettNavnAsync(long testId, string navn, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (test is null)
        {
            return false;
        }

        test.Navn = navn;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Se Test.OversettelseNotat — samme mønster som SettRapportIntroduksjonAsync.</summary>
    public async Task<bool> SettOversettelseNotatAsync(long testId, string? oversettelseNotat, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (test is null)
        {
            return false;
        }

        test.OversettelseNotat = oversettelseNotat;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Overskriver Svaralternativer-strengen for ALLE ledd i en test —
    /// brukt av seedere der samtlige ledd deler nøyaktig samme skala (f.eks.
    /// PiCD sin 5-punkts enig/uenig-skala), slik at en ordlydsjustering i
    /// kildekoden (se PicdTestSeeder) også slår igjennom for en test som
    /// allerede er seedet i et miljø. IKKE egnet for tester der ledd har
    /// individuelle svaralternativer (f.eks. PDS-ICD-11).
    /// </summary>
    public async Task<int> OppdaterSvaralternativerForAlleLeddAsync(long testId, string svaralternativer, CancellationToken cancellationToken = default)
    {
        var leddListe = await _db.TestLedd
            .Where(l => _db.TestSider.Where(s => s.TestId == testId).Select(s => s.Id).Contains(l.TestSideId))
            .ToListAsync(cancellationToken);

        foreach (var ledd in leddListe)
        {
            ledd.Svaralternativer = svaralternativer;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return leddListe.Count;
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

    /// <summary>
    /// Testens (primære) kategorinavn, til rapportens fargekoding per kategori
    /// (bugliste 2026-09-13 punkt 27) — en test er i praksis kun i ÉN kategori
    /// i dag (hver IInnebygdTestSeeder kobler kun til én), så "første" er trygt
    /// selv om modellen i prinsippet tillater flere.
    /// </summary>
    public async Task<string?> HentPrimaerKategoriNavnAsync(long testId, CancellationToken cancellationToken = default) =>
        await _db.TestKategoriKoblinger
            .Where(k => k.TestId == testId)
            .Join(_db.TestKategorier, k => k.TestKategoriId, kat => kat.Id, (k, kat) => kat.Navn)
            .FirstOrDefaultAsync(cancellationToken);

    public sealed record KategoriMedTester(TestKategori Kategori, IReadOnlyList<Test> Tester);

    /// <summary>
    /// Antall ledd per test — brukt til å ANSLÅ utfyllingstid i test-infoboksen
    /// (se wwwroot/js/testinfo.js), ca. 15 sekunder per spørsmål. Ingen eget
    /// "estimert tid"-felt på Test — et anslag utledet direkte fra faktisk
    /// spørsmålsantall holder seg automatisk korrekt når ledd endres, uten at
    /// noen må huske å oppdatere et manuelt tall.
    /// </summary>
    public async Task<IReadOnlyDictionary<long, int>> HentAntallLeddPerTestAsync(
        IReadOnlyCollection<long> testIder, CancellationToken cancellationToken = default)
    {
        var sider = await _db.TestSider.Where(s => testIder.Contains(s.TestId)).Select(s => s.Id).ToListAsync(cancellationToken);
        return await _db.TestLedd
            .Where(l => sider.Contains(l.TestSideId))
            .Join(_db.TestSider, l => l.TestSideId, s => s.Id, (l, s) => s.TestId)
            .GroupBy(testId => testId)
            .Select(g => new { TestId = g.Key, Antall = g.Count() })
            .ToDictionaryAsync(x => x.TestId, x => x.Antall, cancellationToken);
    }

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
        long? ansvarligBehandlerId = null,
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
            VarighetMinutter = varighetMinutter,
            AnsvarligBehandlerId = ansvarligBehandlerId
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

    /// <summary>
    /// Samme som <see cref="HentTildelingerForPasientAsync"/>, men EKSKLUDERER enhver tildeling av
    /// en behandler-utfylt test (Test.FyllesUtAvBehandler) — pasienten skal ALDRI se, telle eller
    /// kunne navigere til en slik tildeling (den er ikke deres å fylle ut). Bruk denne i ALL
    /// pasientvendt kode (Pasientportal/MinSide, "neste test"-navigasjon, badge-tellingen i
    /// _Layout) — behandler-/admin-vendt kode som skal se HELE pasientens historikk (f.eks.
    /// Behandlerportal/Pasienter/Detaljer) bruker fortsatt den rå
    /// <see cref="HentTildelingerForPasientAsync"/>. Selve utfyllingssiden (Pasientportal/Tester/
    /// Fyll.cshtml.cs) har i tillegg sin EGEN, uavhengige sperre — denne listefiltreringen er et
    /// UX-supplement, ikke selve sikkerhetsgrensen (se docs/beslutningslogg.md).
    /// </summary>
    public async Task<List<TestTildeling>> HentPasientSynligeTildelingerAsync(long pasientId, CancellationToken cancellationToken = default)
    {
        var behandlerUtfylteTestIder = await _db.Tester.Where(t => t.FyllesUtAvBehandler).Select(t => t.Id).ToListAsync(cancellationToken);
        return await _db.TestTildelinger
            .Where(t => t.PasientId == pasientId && !behandlerUtfylteTestIder.Contains(t.TestId))
            .OrderByDescending(t => t.TildeltUtc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Testens navn for én tildeling — brukt til å navngi "neste test"-knappen (se Pasientportal/Tester/Fyll).</summary>
    public async Task<string?> HentTestNavnForTildelingAsync(long tildelingId, CancellationToken cancellationToken = default)
    {
        var testId = await _db.TestTildelinger.Where(t => t.Id == tildelingId).Select(t => t.TestId).FirstOrDefaultAsync(cancellationToken);
        return testId == 0 ? null : await _db.Tester.Where(t => t.Id == testId).Select(t => t.Navn).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Godkjente rapporter for én pasient — til den samlede "Godkjente rapporter"-listen
    /// (bugliste 2026-09-15). <paramref name="kunSynligForPasient"/> skiller pasientens
    /// egen (kun det behandler aktivt har delt) fra behandler/admin sin (alt godkjent,
    /// uavhengig av delingsvalg).
    /// </summary>
    public Task<List<TestTildeling>> HentGodkjenteForPasientAsync(long pasientId, bool kunSynligForPasient, CancellationToken cancellationToken = default)
    {
        var sporring = _db.TestTildelinger.Where(t => t.PasientId == pasientId && t.RapportGodkjentUtc != null);
        if (kunSynligForPasient)
        {
            sporring = sporring.Where(t => t.RapportSynligForPasient);
        }

        return sporring.OrderByDescending(t => t.RapportGodkjentUtc).ToListAsync(cancellationToken);
    }

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

        var (test, sider, alleLedd) = await HentTestStrukturAsync(tildeling.TestId, cancellationToken);
        var svar = await _db.TestSvar.Where(s => s.TestTildelingId == tildelingId)
            .ToDictionaryAsync(s => s.TestLeddId, s => s.SvarVerdi, cancellationToken);

        return new TestMedInnhold(tildeling, test, sider, alleLedd, svar);
    }

    /// <summary>Kortlevd-cachet (se TestStrukturCacheTid) — se HentTildelingMedInnholdAsync.</summary>
    private async Task<(Test Test, List<TestSide> Sider, List<TestLedd> AlleLedd)> HentTestStrukturAsync(
        long testId, CancellationToken cancellationToken)
    {
        var cacheNokkel = $"test-struktur:{testId}";
        if (_cache.TryGetValue(cacheNokkel, out (Test, List<TestSide>, List<TestLedd>) cachet))
        {
            return cachet;
        }

        var test = await _db.Tester.AsNoTracking().FirstAsync(t => t.Id == testId, cancellationToken);
        var sider = await _db.TestSider.AsNoTracking().Where(s => s.TestId == testId).OrderBy(s => s.Rekkefolge).ToListAsync(cancellationToken);
        var sideIder = sider.Select(s => s.Id).ToList();
        var sideRekkefolgePerId = sider.ToDictionary(s => s.Id, s => s.Rekkefolge);
        // MÅ sorteres på (side.Rekkefolge, ledd.Rekkefolge) — IKKE ledd.Rekkefolge alene.
        // Rekkefolge nullstilles til 1 for HVER side (se LeggTilLeddAsync), så en test med
        // FLERE sider (f.eks. EDE-Q: 10+13+5 ledd på 3 sider) får ellers ledd fra ulike sider
        // med SAMME Rekkefolge-verdi — et rått "ORDER BY Rekkefolge" uten denne sekundære
        // sorteringsnøkkelen er ikke garantert å holde sidene samlet ved slike likheter (MySQL
        // gir ingen rekkefølgegaranti for uavgjorte ORDER BY-verdier). Skjedde reelt: EDE-Q sine
        // 5 ikke-skårede fritekstledd (siste side) havnet midt inni listen i stedet for til
        // slutt, som fikk EdeqSkaaringsberegner sin posisjonsbaserte delskala-inndeling til å
        // plukke opp et fritekst-svar som en tallskåret verdi -> FormatException ved
        // rapportvisning/-godkjenning (se docs/beslutningslogg.md "Reell 500-feil i EDE-Q").
        var alleLedd = await _db.TestLedd.AsNoTracking().Where(l => sideIder.Contains(l.TestSideId)).ToListAsync(cancellationToken);
        alleLedd = alleLedd.OrderBy(l => sideRekkefolgePerId.GetValueOrDefault(l.TestSideId)).ThenBy(l => l.Rekkefolge).ToList();

        var resultat = (test, sider, alleLedd);
        _cache.Set(cacheNokkel, resultat, TestStrukturCacheTid);
        return resultat;
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
        CancellationToken cancellationToken = default) =>
        await LagreSvarAsync(tildelingId, svarPerLeddId, markerFullfort, kommentarPerLeddId: null, cancellationToken);

    /// <summary>
    /// Overload med behandler-kommentar per ledd — brukt av Behandlerportal/
    /// Pasienter/FyllForPasient (se Test.FyllesUtAvBehandler). Et ledd kan ha
    /// KUN en kommentar uten selve svaret besvart ennå (f.eks. behandler
    /// begynner å notere før hen har bestemt seg for svarverdien), så
    /// kommentar og svarverdi oppdateres/opprettes UAVHENGIG av hverandre.
    /// </summary>
    public async Task LagreSvarAsync(
        long tildelingId, IReadOnlyDictionary<long, string> svarPerLeddId, bool markerFullfort,
        IReadOnlyDictionary<long, string>? kommentarPerLeddId, CancellationToken cancellationToken = default)
    {
        var tildeling = await _db.TestTildelinger.FirstAsync(t => t.Id == tildelingId, cancellationToken);

        if (tildeling.Status == TestTildelingStatus.Tildelt)
        {
            tildeling.Status = TestTildelingStatus.Startet;
            tildeling.StartetUtc = DateTimeOffset.UtcNow;
        }

        var beroerteLeddIder = svarPerLeddId.Keys.Union(kommentarPerLeddId?.Keys ?? Enumerable.Empty<long>());
        foreach (var leddId in beroerteLeddIder)
        {
            var verdi = svarPerLeddId.GetValueOrDefault(leddId);
            var kommentar = kommentarPerLeddId?.GetValueOrDefault(leddId);
            var harVerdi = !string.IsNullOrWhiteSpace(verdi);
            var harKommentar = !string.IsNullOrWhiteSpace(kommentar);
            if (!harVerdi && !harKommentar)
            {
                continue;
            }

            var eksisterende = await _db.TestSvar.FirstOrDefaultAsync(
                s => s.TestTildelingId == tildelingId && s.TestLeddId == leddId, cancellationToken);

            if (eksisterende is not null)
            {
                if (harVerdi)
                {
                    eksisterende.SvarVerdi = verdi!;
                    eksisterende.BesvartUtc = DateTimeOffset.UtcNow;
                }
                if (kommentarPerLeddId is not null)
                {
                    eksisterende.BehandlerKommentar = harKommentar ? kommentar : null;
                }
            }
            else if (harVerdi)
            {
                _db.TestSvar.Add(new TestSvar
                {
                    TestTildelingId = tildelingId,
                    TestLeddId = leddId,
                    SvarVerdi = verdi!,
                    BesvartUtc = DateTimeOffset.UtcNow,
                    BehandlerKommentar = harKommentar ? kommentar : null
                });
            }
            // Kommentar uten verdi og uten eksisterende rad: ingen rad å feste kommentaren til ennå
            // (TestSvar.SvarVerdi er required) — kommentaren går tapt inntil selve svaret gis. Akseptert
            // avveining fremfor å innføre en nullable SvarVerdi kun for dette sjeldne tilfellet.
        }

        if (markerFullfort)
        {
            tildeling.Status = TestTildelingStatus.Fullfort;
            tildeling.FullfortUtc = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (markerFullfort)
        {
            var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == tildeling.PasientId, cancellationToken);
            if (pasient is not null)
            {
                // "Prøv systemet"-pasient (intet personnummer ennå, se
                // PasientInvitasjonService.RegistrerViaQrAsync) — rapporten skal
                // ALDRI vente på behandlergodkjenning, jf. den opprinnelige
                // spesifikasjonen: vis den til pasienten med det samme, og send
                // ALDRI en godkjenningsforespørsel til behandler for denne. Kun
                // personnummer avgjør dette skillet — se docs/beslutningslogg.md
                // "Forenklet QR-registrering".
                if (string.IsNullOrWhiteSpace(pasient.Personnummer))
                {
                    tildeling.RapportGodkjentUtc = DateTimeOffset.UtcNow;
                    tildeling.RapportSynligForPasient = true;
                    await _db.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    // Varsler pasientens FAKTISKE behandler (ikke nødvendigvis den som
                    // tildelte testen — en admin kan ha tildelt den, se TildelAsync).
                    await _meldingService.OpprettAsync(pasient.BehandlerId, tildeling.Id, cancellationToken);
                }
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

    public sealed record TildelingMedTestOgPasient(TestTildeling Tildeling, string TestNavn, long PasientId, string? PasientNavn, bool FyllesUtAvBehandler = false);

    /// <summary>Fullførte tester som venter på behandlers godkjenning — behandlers oppgaveliste, jf. beslutningsloggen.</summary>
    public async Task<IReadOnlyList<TildelingMedTestOgPasient>> HentUgodkjenteFullforteForBehandlerAsync(
        long behandlerId, CancellationToken cancellationToken = default)
    {
        var tildelinger = await (
            from t in _db.TestTildelinger
            join p in _db.Pasienter on t.PasientId equals p.Id
            where (t.AnsvarligBehandlerId ?? p.BehandlerId) == behandlerId && t.Status == TestTildelingStatus.Fullfort &&
                  t.RapportGodkjentUtc == null && t.RapportForkastetUtc == null
            orderby t.FullfortUtc
            select t
        ).ToListAsync(cancellationToken);
        return await BerikMedTestOgPasientAsync(tildelinger, cancellationToken);
    }

    /// <summary>Tester tildelt behandlers pasienter som ennå ikke er besvart ferdig — kun til oversikt, ingen godkjenning her.</summary>
    public async Task<IReadOnlyList<TildelingMedTestOgPasient>> HentIkkeFullforteForBehandlerAsync(
        long behandlerId, CancellationToken cancellationToken = default)
    {
        // Bugliste punkt 13: AnsvarligBehandlerId (satt KUN for behandler-utfylte tester der noen
        // eksplisitt valgte en ANNEN behandler enn pasientens egen, se TestTildeling sin XML-doc)
        // vinner over Pasient.BehandlerId her — ellers ser VERKEN den valgte behandleren (feltet
        // peker bort fra pasientens egen BehandlerId) ELLER pasientens egen behandler (som ikke
        // lenger er ansvarlig) riktig status for akkurat denne tildelingen.
        var tildelinger = await (
            from t in _db.TestTildelinger
            join p in _db.Pasienter on t.PasientId equals p.Id
            where (t.AnsvarligBehandlerId ?? p.BehandlerId) == behandlerId && t.Status != TestTildelingStatus.Fullfort
            orderby t.TildeltUtc
            select t
        ).ToListAsync(cancellationToken);
        return await BerikMedTestOgPasientAsync(tildelinger, cancellationToken);
    }

    /// <summary>Godkjente rapporter for behandlers pasienter — "Min side" sin tredje fane, se docs/beslutningslogg.md.</summary>
    public async Task<IReadOnlyList<TildelingMedTestOgPasient>> HentGodkjenteFullforteForBehandlerAsync(
        long behandlerId, CancellationToken cancellationToken = default)
    {
        var tildelinger = await (
            from t in _db.TestTildelinger
            join p in _db.Pasienter on t.PasientId equals p.Id
            where (t.AnsvarligBehandlerId ?? p.BehandlerId) == behandlerId && t.RapportGodkjentUtc != null
            orderby t.RapportGodkjentUtc descending
            select t
        ).ToListAsync(cancellationToken);
        return await BerikMedTestOgPasientAsync(tildelinger, cancellationToken);
    }

    /// <summary>
    /// Sletter permanent en IKKE-fullført tildeling ("Ikke besvart"-listen på Min
    /// side, som ellers vokser uten grenser — se docs/beslutningslogg.md). Kan
    /// ALDRI slette en fullført tildeling (bruk arkivering/rapportflyten for
    /// den), og kun for behandlerens EGNE pasienter.
    /// </summary>
    public async Task<bool> SlettIkkeFullfortTildelingAsync(long tildelingId, long behandlerId, CancellationToken cancellationToken = default)
    {
        var tildeling = await _db.TestTildelinger.FirstOrDefaultAsync(t => t.Id == tildelingId, cancellationToken);
        if (tildeling is null || tildeling.Status == TestTildelingStatus.Fullfort)
        {
            return false;
        }

        var eierBehandlerId = await _db.Pasienter.Where(p => p.Id == tildeling.PasientId).Select(p => p.BehandlerId).FirstOrDefaultAsync(cancellationToken);
        if (eierBehandlerId != behandlerId)
        {
            return false;
        }

        _db.TestTildelingBetalinger.RemoveRange(_db.TestTildelingBetalinger.Where(b => b.TestTildelingId == tildelingId));
        _db.TestSvar.RemoveRange(_db.TestSvar.Where(s => s.TestTildelingId == tildelingId));
        await _db.SaveChangesAsync(cancellationToken);

        _db.TestTildelinger.Remove(tildeling);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Sletter en test HELT (alle sider/ledd/tildelinger/svar/betalinger/meldinger) slik at neste
    /// kjøring av samme <see cref="IInnebygdTestSeeder"/> bygger den fullstendig på nytt — BEVISST
    /// mer destruktivt enn den vanlige idempotente "hvis finnes, bare oppdater kategori/intro og
    /// returner"-oppførselen til <see cref="IInnebygdTestSeeder.SeedAsync"/>. Skal KUN kalles for
    /// tester som er under AKTIV strukturell iterasjon rett etter førstegangsbygging (f.eks.
    /// SCID-5-PF sin rekkefølge-/etikett-/veiledningsoppdatering, se docs/beslutningslogg.md) —
    /// ALDRI for en test som allerede har reelle pasientbesvarelser man vil beholde. Rydder i
    /// Gruppe-/Partner-tilknytninger og tilgangsforespørsler også, siden en fullstendig sletting av
    /// selve Test-raden ellers ville etterlatt disse pekende på en TestId som ikke lenger finnes.
    /// </summary>
    public async Task SlettTestHeltForRegenereringAsync(string testKode, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Kode == testKode, cancellationToken);
        if (test is null)
        {
            return;
        }

        var tildelingIder = await _db.TestTildelinger.Where(t => t.TestId == test.Id).Select(t => t.Id).ToListAsync(cancellationToken);
        _db.TestTildelingBetalinger.RemoveRange(_db.TestTildelingBetalinger.Where(b => tildelingIder.Contains(b.TestTildelingId)));
        _db.Pengebevegelser.RemoveRange(_db.Pengebevegelser.Where(p => p.TestTildelingId != null && tildelingIder.Contains(p.TestTildelingId.Value)));
        _db.BehandlerMeldinger.RemoveRange(_db.BehandlerMeldinger.Where(m => m.TestTildelingId != null && tildelingIder.Contains(m.TestTildelingId.Value)));
        _db.TestSvar.RemoveRange(_db.TestSvar.Where(s => tildelingIder.Contains(s.TestTildelingId)));
        await _db.SaveChangesAsync(cancellationToken);

        _db.TestTildelinger.RemoveRange(_db.TestTildelinger.Where(t => t.TestId == test.Id));
        _db.TestKategoriKoblinger.RemoveRange(_db.TestKategoriKoblinger.Where(k => k.TestId == test.Id));
        _db.GruppeTestTilordninger.RemoveRange(_db.GruppeTestTilordninger.Where(g => g.TestId == test.Id));
        _db.PartnerTestTilganger.RemoveRange(_db.PartnerTestTilganger.Where(p => p.TestId == test.Id));
        _db.PartnerTestAndeler.RemoveRange(_db.PartnerTestAndeler.Where(p => p.TestId == test.Id));
        _db.TestTilgangForesporsler.RemoveRange(_db.TestTilgangForesporsler.Where(f => f.TestId == test.Id));
        await _db.SaveChangesAsync(cancellationToken);

        var sideIder = await _db.TestSider.Where(s => s.TestId == test.Id).Select(s => s.Id).ToListAsync(cancellationToken);
        _db.TestLedd.RemoveRange(_db.TestLedd.Where(l => sideIder.Contains(l.TestSideId)));
        await _db.SaveChangesAsync(cancellationToken);

        _db.TestSider.RemoveRange(_db.TestSider.Where(s => s.TestId == test.Id));
        await _db.SaveChangesAsync(cancellationToken);

        _db.Tester.Remove(test);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<TildelingMedTestOgPasient>> BerikMedTestOgPasientAsync(
        List<TestTildeling> tildelinger, CancellationToken cancellationToken)
    {
        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var tester = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);

        var pasientIder = tildelinger.Select(t => t.PasientId).Distinct().ToList();
        var pasientNavn = await _db.Pasienter.Where(p => pasientIder.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Navn, cancellationToken);

        return tildelinger
            .Select(t => new TildelingMedTestOgPasient(
                t, tester.GetValueOrDefault(t.TestId)?.Navn ?? "(ukjent test)", t.PasientId, pasientNavn.GetValueOrDefault(t.PasientId),
                tester.GetValueOrDefault(t.TestId)?.FyllesUtAvBehandler ?? false))
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

        // ALLE testens ledd, sortert etter (side.Rekkefolge, ledd.Rekkefolge) — IKKE
        // bare de besvarte, og IKKE bare databasens naturlige rekkefølge (ikke
        // garantert), og IKKE bare ledd.Rekkefolge alene (telles PER SIDE, så side 2
        // sitt ledd 1 ville ellers sortert før side 1 sitt ledd 5). "Alle" (ikke bare
        // besvarte) er nødvendig fra 2026-09-23 for å vite den FAKTISKE nevneren når
        // vi beregner andel ubesvart under, se Test.MaksUbesvartProsent.
        var alleLedd = await (
            from l in _db.TestLedd
            join side in _db.TestSider on l.TestSideId equals side.Id
            where side.TestId == test.Id
            orderby side.Rekkefolge, l.Rekkefolge
            select l
        ).ToListAsync(cancellationToken);

        var besvarteSvar = await _db.TestSvar
            .Where(s => s.TestTildelingId == tildelingId)
            .ToListAsync(cancellationToken);

        // Fullstendig svarliste i riktig rekkefølge: et besvart ledd bruker sitt
        // ekte svar, et ubesvart ledd MED kjent normert gjennomsnitt får et
        // syntetisk (ALDRI lagret til databasen) TestSvar med den normerte
        // verdien — se TestLedd.NormertGjennomsnitt. Et ubesvart ledd UTEN normert
        // gjennomsnitt er rett og slett fraværende fra listen, akkurat som før
        // denne funksjonen fantes 2026-09-23 (se docs/beslutningslogg.md "Normert
        // gjennomsnitt-imputering + gyldighetsgrense").
        var fullstendigSvar = new List<TestSvar>(alleLedd.Count);
        var ubesvarteAntall = 0;
        foreach (var ledd in alleLedd)
        {
            var eksisterende = besvarteSvar.FirstOrDefault(s => s.TestLeddId == ledd.Id);
            if (eksisterende is not null)
            {
                fullstendigSvar.Add(eksisterende);
                continue;
            }

            ubesvarteAntall++;
            if (ledd.NormertGjennomsnitt is not null)
            {
                // Rundet til nærmeste hele tall — nesten alle skåringsberegnere
                // (WHO-5, GADIT, PHQ-9, ...) gjør et rått int.Parse på SvarVerdi,
                // siden selve svarskalaen alltid er heltallsbasert (f.eks. 0-4).
                // Et normert gjennomsnitt fra litteraturen er derimot ofte IKKE et
                // helt tall (f.eks. 2,3) — avrunding her, ikke i selve
                // skåringsberegneren, holder RaaSkaar/ProsentSkaar heltallsbasert
                // slik hele domenemodellen ellers forutsetter.
                var avrundetVerdi = (int)Math.Round(ledd.NormertGjennomsnitt.Value, MidpointRounding.AwayFromZero);
                fullstendigSvar.Add(new TestSvar
                {
                    TestTildelingId = tildelingId,
                    TestLeddId = ledd.Id,
                    SvarVerdi = avrundetVerdi.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    BesvartUtc = tildeling.FullfortUtc ?? DateTimeOffset.UtcNow
                });
            }
        }

        TestSkaaring skaaring;
        if (beregner is ITestSkaaringsberegnerMedBiologiskKjonn beregnerMedKjonn)
        {
            var pasientKjonn = (await _db.Pasienter.AsNoTracking().FirstOrDefaultAsync(p => p.Id == tildeling.PasientId, cancellationToken))?.BiologiskKjonnVedFodsel;
            skaaring = beregnerMedKjonn.BeregnSkaaringMedBiologiskKjonn(fullstendigSvar, alleLedd, pasientKjonn);
        }
        else if (beregner is ITestSkaaringsberegnerMedLedd beregnerMedLedd)
        {
            skaaring = beregnerMedLedd.BeregnSkaaringMedLedd(fullstendigSvar, alleLedd);
        }
        else
        {
            skaaring = beregner.BeregnSkaaring(fullstendigSvar);
        }

        if (test.MaksUbesvartProsent is not null && alleLedd.Count > 0)
        {
            var andelUbesvartProsent = ubesvarteAntall * 100m / alleLedd.Count;
            if (andelUbesvartProsent > test.MaksUbesvartProsent.Value)
            {
                var advarsel =
                    $"{ubesvarteAntall} av {alleLedd.Count} spørsmål ({Math.Round(andelUbesvartProsent)}%) ble ikke " +
                    $"besvart — over grensen på {test.MaksUbesvartProsent}% for denne testen. Manglende svar er " +
                    "erstattet med normert gjennomsnitt der det finnes; resultatet bør uansett tolkes med forsiktighet.";
                skaaring = skaaring with { GyldighetsAdvarsel = advarsel };
            }
        }

        return skaaring;
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

        var pasientKjonn = beregner is ITestSkaaringsberegnerMedBiologiskKjonn
            ? (await _db.Pasienter.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pasientId, cancellationToken))?.BiologiskKjonnVedFodsel
            : null;

        var punkter = new List<SkaaringHistorikkPunkt>();
        foreach (var tildeling in tildelinger)
        {
            var svar = await _db.TestSvar.Where(s => s.TestTildelingId == tildeling.Id).ToListAsync(cancellationToken);
            TestSkaaring skaaring;
            if (beregner is ITestSkaaringsberegnerMedBiologiskKjonn beregnerMedKjonn)
            {
                skaaring = beregnerMedKjonn.BeregnSkaaringMedBiologiskKjonn(svar, (await HentTestStrukturAsync(tildeling.TestId, cancellationToken)).AlleLedd, pasientKjonn);
            }
            else if (beregner is ITestSkaaringsberegnerMedLedd beregnerMedLedd)
            {
                skaaring = beregnerMedLedd.BeregnSkaaringMedLedd(svar, (await HentTestStrukturAsync(tildeling.TestId, cancellationToken)).AlleLedd);
            }
            else
            {
                skaaring = beregner.BeregnSkaaring(svar);
            }

            punkter.Add(new SkaaringHistorikkPunkt(tildeling, skaaring));
        }

        return punkter;
    }

    public bool HarSkaaringsberegner(string? testKode) => FinnBeregner(testKode) is not null;

    /// <summary>Se ITestSkaaringsberegner.Referanselinjer — tom liste for tester uten normerte grenseverdier.</summary>
    public IReadOnlyList<TestSkaaringReferanselinje> HentReferanselinjer(string? testKode) =>
        FinnBeregner(testKode)?.Referanselinjer ?? Array.Empty<TestSkaaringReferanselinje>();

    /// <summary>Se ITestSkaaringsberegner.VisSomProsentIHistogram — FALSK (råskår) som fallback for tester uten registrert beregner.</summary>
    public bool VisSomProsentIHistogram(string? testKode) => FinnBeregner(testKode)?.VisSomProsentIHistogram ?? false;

    /// <summary>Se ITestSkaaringsberegner.Histogramgrenser — tom liste for tester uten en enkel, navngitt cutoff.</summary>
    public IReadOnlyList<TestSkaaringGrenseverdi> HentHistogramgrenser(string? testKode) =>
        FinnBeregner(testKode)?.Histogramgrenser ?? Array.Empty<TestSkaaringGrenseverdi>();

    /// <summary>Se ITestSkaaringsberegner.SignifikantEndringProsentpoeng — NULL (ingen markering) som fallback for tester uten en sitert terskel.</summary>
    public double? HentSignifikantEndringProsentpoeng(string? testKode) =>
        FinnBeregner(testKode)?.SignifikantEndringProsentpoeng;

    private ITestSkaaringsberegner? FinnBeregner(string? testKode) =>
        testKode is null ? null : _skaaringsberegnere.FirstOrDefault(b => b.TestKode == testKode);
}
