using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.Skaaring;

namespace TestBase.Shared.Domain.Pasienter;

public sealed record GruppeMedTester(Gruppe Gruppe, IReadOnlyList<Test> Tester);

/// <summary>Én rad i indikator-fordelingen for et test-aggregat — se GruppeService.HentAggregatAsync.</summary>
public sealed record IndikatorFordelingRad(string IndikatorNavn, string Verdi, int Antall);

/// <summary>
/// Aggregert resultat for ÉN test på tvers av en gruppes pasienter — fase 4,
/// se docs/beslutningslogg.md. Bevisst ÉN generisk visning for ALLE tester
/// (ikke egne visualiseringer per test, jf. avklaringen før byggingen startet):
/// N, gjennomsnitt/median prosentskår, og en kategorisk fordeling basert på
/// hver skåringsberegners egne TestSkaaringIndikator-er (fungerer for enhver
/// test som har en registrert ITestSkaaringsberegner, uten testspesifikk kode).
/// </summary>
public sealed record TestAggregatRad(
    long TestId,
    string TestNavn,
    int AntallFullfort,
    double? GjennomsnittProsent,
    double? MedianProsent,
    IReadOnlyList<IndikatorFordelingRad> IndikatorFordeling);

/// <summary>
/// Behandlers pasientgrupper (se Gruppe) — CRUD, test-tilordning ved
/// opprettelse (jf. docs/beslutningslogg.md "Invitasjons- og gruppesystem"),
/// QR-token-håndtering, og (fase 4) aggregert rapportering på tvers av en
/// gruppes besvarelser. Selve QR-BILDET genereres i Web-laget (fase 2,
/// QRCoder) — denne tjenesten eier kun token-en URL-en bygges rundt.
/// </summary>
public sealed class GruppeService
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;

    public GruppeService(AppDbContext db, TestService testService)
    {
        _db = db;
        _testService = testService;
    }

    private static string NyQrToken() => RandomNumberGenerator.GetHexString(24);

    public async Task<Gruppe> OpprettAsync(
        string navn, long behandlerId, IReadOnlyCollection<long> testIder,
        DateOnly? startDato = null, DateOnly? sluttDato = null, CancellationToken cancellationToken = default)
    {
        var gruppe = new Gruppe
        {
            Navn = navn,
            BehandlerId = behandlerId,
            OpprettetUtc = DateTimeOffset.UtcNow,
            StartDato = startDato,
            SluttDato = sluttDato,
            QrToken = NyQrToken()
        };
        _db.Grupper.Add(gruppe);
        await _db.SaveChangesAsync(cancellationToken);

        await SettTilordnedeTesterAsync(gruppe.Id, testIder, cancellationToken);
        return gruppe;
    }

    /// <summary>
    /// Finner en eksisterende gruppe med nøyaktig dette navnet for behandleren,
    /// ellers oppretter en ny (ingen tester tilordnet) — brukt av
    /// PasientInvitasjonService.ImporterGruppeAsync for å bevare CSV-formatet
    /// "gruppenavn,navn,email,sms,pnr" ordrett fra kravdokumentet uten å kreve
    /// at gruppen finnes fra før.
    /// </summary>
    public async Task<Gruppe> FinnEllerOpprettAsync(string navn, long behandlerId, CancellationToken cancellationToken = default)
    {
        var eksisterende = await _db.Grupper.FirstOrDefaultAsync(
            g => g.BehandlerId == behandlerId && g.Navn == navn && !g.ErArkivert, cancellationToken);
        return eksisterende ?? await OpprettAsync(navn, behandlerId, Array.Empty<long>(), cancellationToken: cancellationToken);
    }

    public Task<List<Gruppe>> HentForBehandlerAsync(long behandlerId, CancellationToken cancellationToken = default) =>
        _db.Grupper.Where(g => g.BehandlerId == behandlerId && !g.ErArkivert).OrderBy(g => g.Navn).ToListAsync(cancellationToken);

    public Task<List<Gruppe>> HentAlleAsync(CancellationToken cancellationToken = default) =>
        _db.Grupper.Where(g => !g.ErArkivert).OrderBy(g => g.Navn).ToListAsync(cancellationToken);

    /// <summary>Til "Arkivert"-fanen på Behandlerportal/Grupper/Index.</summary>
    public Task<List<Gruppe>> HentArkiverteForBehandlerAsync(long behandlerId, CancellationToken cancellationToken = default) =>
        _db.Grupper.Where(g => g.BehandlerId == behandlerId && g.ErArkivert).OrderByDescending(g => g.ArkivertUtc).ToListAsync(cancellationToken);

    /// <summary>Til "Arkivert"-fanen på Admin/Grupper/Index.</summary>
    public Task<List<Gruppe>> HentAlleArkiverteAsync(CancellationToken cancellationToken = default) =>
        _db.Grupper.Where(g => g.ErArkivert).OrderByDescending(g => g.ArkivertUtc).ToListAsync(cancellationToken);

    public async Task<Dictionary<long, int>> HentPasientAntallPerGruppeAsync(
        IReadOnlyList<long> gruppeIder, CancellationToken cancellationToken = default) =>
        (await _db.Pasienter
            .Where(p => p.GruppeId != null && gruppeIder.Contains(p.GruppeId.Value) && p.Status != PasientStatus.Arkivert)
            .GroupBy(p => p.GruppeId!.Value)
            .Select(g => new { GruppeId = g.Key, Antall = g.Count() })
            .ToListAsync(cancellationToken))
        .ToDictionary(x => x.GruppeId, x => x.Antall);

    public async Task<GruppeMedTester?> HentMedTesterAsync(long gruppeId, CancellationToken cancellationToken = default)
    {
        var gruppe = await _db.Grupper.FirstOrDefaultAsync(g => g.Id == gruppeId, cancellationToken);
        if (gruppe is null)
        {
            return null;
        }

        var testIder = await _db.GruppeTestTilordninger.Where(t => t.GruppeId == gruppeId).Select(t => t.TestId).ToListAsync(cancellationToken);
        var tester = await _db.Tester.Where(t => testIder.Contains(t.Id)).OrderBy(t => t.Navn).ToListAsync(cancellationToken);
        return new GruppeMedTester(gruppe, tester);
    }

    public Task<Gruppe?> FinnVedQrTokenAsync(string qrToken, CancellationToken cancellationToken = default) =>
        _db.Grupper.FirstOrDefaultAsync(g => g.QrToken == qrToken && !g.ErArkivert, cancellationToken);

    public async Task<bool> OppdaterAsync(
        long gruppeId, string navn, IReadOnlyCollection<long> testIder,
        DateOnly? startDato = null, DateOnly? sluttDato = null, CancellationToken cancellationToken = default)
    {
        var gruppe = await _db.Grupper.FirstOrDefaultAsync(g => g.Id == gruppeId, cancellationToken);
        if (gruppe is null)
        {
            return false;
        }

        gruppe.Navn = navn;
        gruppe.StartDato = startDato;
        gruppe.SluttDato = sluttDato;
        await _db.SaveChangesAsync(cancellationToken);
        await SettTilordnedeTesterAsync(gruppeId, testIder, cancellationToken);
        return true;
    }

    private async Task SettTilordnedeTesterAsync(long gruppeId, IReadOnlyCollection<long> testIder, CancellationToken cancellationToken)
    {
        var eksisterende = await _db.GruppeTestTilordninger.Where(t => t.GruppeId == gruppeId).ToListAsync(cancellationToken);
        _db.GruppeTestTilordninger.RemoveRange(eksisterende.Where(t => !testIder.Contains(t.TestId)));

        var eksisterendeTestIder = eksisterende.Select(t => t.TestId).ToHashSet();
        var na = DateTimeOffset.UtcNow;
        _db.GruppeTestTilordninger.AddRange(testIder.Where(id => !eksisterendeTestIder.Contains(id)).Select(testId => new GruppeTestTilordning
        {
            GruppeId = gruppeId,
            TestId = testId,
            OpprettetUtc = na
        }));

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Antall "prøv systemet"-pasienter (intet personnummer, se RegistrerViaQrAsync) i gruppen — til bruk på "Slett prøvedata"-knappen.</summary>
    public Task<int> TellProvedataAsync(long gruppeId, CancellationToken cancellationToken = default) =>
        _db.Pasienter.CountAsync(p => p.GruppeId == gruppeId && p.Personnummer == null, cancellationToken);

    /// <summary>
    /// Aggregert rapport på tvers av gruppens pasienter, per tilordnet test —
    /// fase 4, til bruk under konferanser/presentasjoner (prøvedata) ELLER
    /// oppfølging av ekte pasienter over en tidsperiode. <paramref name="provedata"/>
    /// styrer HELE utvalget: sant = alle pasienter UTEN personnummer (ingen
    /// tidsbegrensning — hele poenget er én samlet presentasjonsøkt), usant =
    /// alle pasienter MED personnummer, avgrenset til <paramref name="fraUtc"/>/
    /// <paramref name="tilUtc"/> (fullført-tidspunkt). Beregnes helt på nytt hver
    /// gang (ingen lagret rapport) — "vises umiddelbart ved klikk", jf. kravet.
    /// </summary>
    public async Task<IReadOnlyList<TestAggregatRad>> HentAggregatAsync(
        long gruppeId, bool provedata, DateTimeOffset? fraUtc, DateTimeOffset? tilUtc, CancellationToken cancellationToken = default)
    {
        var gruppeInnhold = await HentMedTesterAsync(gruppeId, cancellationToken);
        if (gruppeInnhold is null || gruppeInnhold.Tester.Count == 0)
        {
            return Array.Empty<TestAggregatRad>();
        }

        var pasientIder = await _db.Pasienter
            .Where(p => p.GruppeId == gruppeId && (provedata ? p.Personnummer == null : p.Personnummer != null))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (pasientIder.Count == 0)
        {
            return gruppeInnhold.Tester.Select(t => new TestAggregatRad(t.Id, t.Navn, 0, null, null, Array.Empty<IndikatorFordelingRad>())).ToList();
        }

        var resultat = new List<TestAggregatRad>();
        foreach (var test in gruppeInnhold.Tester)
        {
            var tildelingSporring = _db.TestTildelinger.Where(t =>
                t.TestId == test.Id && pasientIder.Contains(t.PasientId) && t.Status == TestTildelingStatus.Fullfort);

            if (!provedata)
            {
                if (fraUtc is not null)
                {
                    tildelingSporring = tildelingSporring.Where(t => t.FullfortUtc >= fraUtc);
                }
                if (tilUtc is not null)
                {
                    tildelingSporring = tildelingSporring.Where(t => t.FullfortUtc <= tilUtc);
                }
            }

            var tildelingIder = await tildelingSporring.Select(t => t.Id).ToListAsync(cancellationToken);

            var prosentSkaarer = new List<int>();
            var indikatorTellinger = new Dictionary<(string Navn, string Verdi), int>();
            foreach (var tildelingId in tildelingIder)
            {
                var skaaring = await _testService.BeregnSkaaringAsync(tildelingId, cancellationToken);
                if (skaaring is null)
                {
                    continue;
                }

                prosentSkaarer.Add(skaaring.ProsentSkaar);
                foreach (var indikator in skaaring.Indikatorer ?? Array.Empty<TestSkaaringIndikator>())
                {
                    var nokkel = (indikator.Navn, indikator.Verdi);
                    indikatorTellinger[nokkel] = indikatorTellinger.GetValueOrDefault(nokkel) + 1;
                }
            }

            var fordeling = indikatorTellinger
                .Select(kv => new IndikatorFordelingRad(kv.Key.Navn, kv.Key.Verdi, kv.Value))
                .OrderBy(r => r.IndikatorNavn).ThenBy(r => r.Verdi)
                .ToList();

            double? gjennomsnitt = prosentSkaarer.Count > 0 ? prosentSkaarer.Average() : null;
            double? median = prosentSkaarer.Count > 0 ? BeregnMedian(prosentSkaarer) : null;

            resultat.Add(new TestAggregatRad(test.Id, test.Navn, tildelingIder.Count, gjennomsnitt, median, fordeling));
        }

        return resultat;
    }

    private static double BeregnMedian(List<int> verdier)
    {
        var sortert = verdier.OrderBy(v => v).ToList();
        var midt = sortert.Count / 2;
        return sortert.Count % 2 == 0 ? (sortert[midt - 1] + sortert[midt]) / 2.0 : sortert[midt];
    }

    /// <summary>
    /// Sletter ALLE "prøv systemet"-pasienter (intet personnummer) i gruppen, inkludert
    /// deres tildelinger/svar/betalinger/meldinger — permanent, ikke arkivering. Rammer
    /// ALDRI en pasient som har fått personnummer (uansett hvordan de kom inn i gruppen),
    /// siden nettopp personnummer er skillet mellom prøvedata og en ekte pasient, se
    /// docs/beslutningslogg.md "Forenklet QR-registrering". Beholdt til behandler
    /// eksplisitt ber om dette (bevisst valg fra planleggingen — se samme seksjon),
    /// ikke slettet automatisk.
    /// </summary>
    public async Task<int> SlettProvedataAsync(long gruppeId, CancellationToken cancellationToken = default)
    {
        var provePasientIder = await _db.Pasienter
            .Where(p => p.GruppeId == gruppeId && p.Personnummer == null)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (provePasientIder.Count == 0)
        {
            return 0;
        }

        var tildelingIder = await _db.TestTildelinger
            .Where(t => provePasientIder.Contains(t.PasientId))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        _db.TestSvar.RemoveRange(_db.TestSvar.Where(s => tildelingIder.Contains(s.TestTildelingId)));
        _db.TestTildelingBetalinger.RemoveRange(_db.TestTildelingBetalinger.Where(b => tildelingIder.Contains(b.TestTildelingId)));
        _db.BehandlerMeldinger.RemoveRange(_db.BehandlerMeldinger.Where(m => tildelingIder.Contains(m.TestTildelingId)));
        await _db.SaveChangesAsync(cancellationToken);

        _db.TestTildelinger.RemoveRange(_db.TestTildelinger.Where(t => tildelingIder.Contains(t.Id)));
        _db.PasientInvitasjoner.RemoveRange(_db.PasientInvitasjoner.Where(i => provePasientIder.Contains(i.PasientId)));
        await _db.SaveChangesAsync(cancellationToken);

        _db.Pasienter.RemoveRange(_db.Pasienter.Where(p => provePasientIder.Contains(p.Id)));
        await _db.SaveChangesAsync(cancellationToken);

        return provePasientIder.Count;
    }

    public async Task<bool> ArkiverAsync(long gruppeId, CancellationToken cancellationToken = default)
    {
        var gruppe = await _db.Grupper.FirstOrDefaultAsync(g => g.Id == gruppeId, cancellationToken);
        if (gruppe is null)
        {
            return false;
        }

        gruppe.ErArkivert = true;
        gruppe.ArkivertUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> GjenopprettAsync(long gruppeId, CancellationToken cancellationToken = default)
    {
        var gruppe = await _db.Grupper.FirstOrDefaultAsync(g => g.Id == gruppeId, cancellationToken);
        if (gruppe is null)
        {
            return false;
        }

        gruppe.ErArkivert = false;
        gruppe.ArkivertUtc = null;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Permanent sletting av en ARKIVERT gruppe (kun kalt fra "Arkivert"-fanen,
    /// se Grupper/Index.cshtml.cs — ikke tilgjengelig for en aktiv gruppe).
    /// Pasienter som fortsatt peker på gruppen (via Pasient.GruppeId, en ekte
    /// FK-kolonne satt av EF Core sin konvensjon) løsrives fra gruppen FØRST
    /// (GruppeId settes til null) i stedet for å bli slettet — sletting av en
    /// gruppe skal ALDRI slette pasienter, kun selve gruppetilknytningen og
    /// testtilordningene.
    /// </summary>
    public async Task<bool> SlettGruppeAsync(long gruppeId, CancellationToken cancellationToken = default)
    {
        var gruppe = await _db.Grupper.FirstOrDefaultAsync(g => g.Id == gruppeId && g.ErArkivert, cancellationToken);
        if (gruppe is null)
        {
            return false;
        }

        var tilknyttedePasienter = await _db.Pasienter.Where(p => p.GruppeId == gruppeId).ToListAsync(cancellationToken);
        foreach (var pasient in tilknyttedePasienter)
        {
            pasient.GruppeId = null;
        }
        _db.GruppeTestTilordninger.RemoveRange(_db.GruppeTestTilordninger.Where(t => t.GruppeId == gruppeId));
        await _db.SaveChangesAsync(cancellationToken);

        _db.Grupper.Remove(gruppe);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Invaliderer eksisterende QR-koder/lenker for gruppen umiddelbart (se Gruppe.QrToken).</summary>
    public async Task<string?> RegenererQrTokenAsync(long gruppeId, CancellationToken cancellationToken = default)
    {
        var gruppe = await _db.Grupper.FirstOrDefaultAsync(g => g.Id == gruppeId, cancellationToken);
        if (gruppe is null)
        {
            return null;
        }

        gruppe.QrToken = NyQrToken();
        await _db.SaveChangesAsync(cancellationToken);
        return gruppe.QrToken;
    }

    // --- Behandlerens EGEN, gruppeuavhengige QR-invitasjon (Behandler.PasientInviteQrToken) ---

    /// <summary>Genererer token første gang den mangler (f.eks. første besøk på "Min side") — ellers returnerer eksisterende.</summary>
    public async Task<string> SikreBehandlerQrTokenAsync(long behandlerId, CancellationToken cancellationToken = default)
    {
        var behandler = await _db.Behandlere.FirstAsync(b => b.Id == behandlerId, cancellationToken);
        if (behandler.PasientInviteQrToken is null)
        {
            behandler.PasientInviteQrToken = NyQrToken();
            await _db.SaveChangesAsync(cancellationToken);
        }

        return behandler.PasientInviteQrToken;
    }

    public async Task<string?> RegenererBehandlerQrTokenAsync(long behandlerId, CancellationToken cancellationToken = default)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
        if (behandler is null)
        {
            return null;
        }

        behandler.PasientInviteQrToken = NyQrToken();
        await _db.SaveChangesAsync(cancellationToken);
        return behandler.PasientInviteQrToken;
    }

    public Task<Behandler?> FinnBehandlerVedQrTokenAsync(string qrToken, CancellationToken cancellationToken = default) =>
        _db.Behandlere.FirstOrDefaultAsync(b => b.PasientInviteQrToken == qrToken && b.Status != BehandlerStatus.Arkivert, cancellationToken);
}
