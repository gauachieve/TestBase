using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;

namespace TestBase.Shared.Domain.Tester;

public sealed record TestMedInnhold(
    TestTildeling Tildeling,
    Test Test,
    IReadOnlyList<TestSide> Sider,
    IReadOnlyList<TestLedd> AlleLedd,
    IReadOnlyDictionary<long, string> EksisterendeSvar);

/// <summary>
/// Testmotor-skjelettet: forfatning av tester (Test/TestSide/TestLedd),
/// tildeling til pasienter, og utfylling (lagring av TestSvar side for side).
/// Ingen skåring eller rapportgenerering ennå — se beslutningsloggen "Del 4
/// (slice 1)" for begrunnelse (bevises ut med WHO-5 i fase 5).
/// </summary>
public sealed class TestService
{
    private readonly AppDbContext _db;

    public TestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Test> OpprettTestAsync(string navn, string? beskrivelse, string? belonningstekst, CancellationToken cancellationToken = default)
    {
        var test = new Test
        {
            Navn = navn,
            Beskrivelse = beskrivelse,
            Belonningstekst = belonningstekst,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        _db.Tester.Add(test);
        await _db.SaveChangesAsync(cancellationToken);
        return test;
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

    public async Task<TestTildeling> TildelAsync(
        long testId, long pasientId, long behandlerId, DateTimeOffset? frist, int? varighetMinutter,
        CancellationToken cancellationToken = default)
    {
        var tildeling = new TestTildeling
        {
            TestId = testId,
            PasientId = pasientId,
            TildeltAvBehandlerId = behandlerId,
            TildeltUtc = DateTimeOffset.UtcNow,
            Frist = frist,
            VarighetMinutter = varighetMinutter
        };
        _db.TestTildelinger.Add(tildeling);
        await _db.SaveChangesAsync(cancellationToken);
        return tildeling;
    }

    public Task<List<TestTildeling>> HentTildelingerForPasientAsync(long pasientId, CancellationToken cancellationToken = default) =>
        _db.TestTildelinger.Where(t => t.PasientId == pasientId).OrderByDescending(t => t.TildeltUtc).ToListAsync(cancellationToken);

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
    }
}
