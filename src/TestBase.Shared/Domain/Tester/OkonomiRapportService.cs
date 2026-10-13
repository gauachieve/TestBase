using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Shared.Domain.Tester;

public sealed record OkonomiTransaksjonsRad(
    DateTimeOffset Dato,
    long TestTildelingId,
    string TestNavn,
    string? BehandlerNavn,
    decimal PasientprisKr,
    decimal HonorarKr,
    decimal PlattformAndelKr,
    decimal? PartnerAndelKr,
    decimal RettighetshaverKostnadKr);

public sealed record AdminOkonomiRapport(
    int Aar, int Maaned,
    IReadOnlyList<OkonomiTransaksjonsRad> Rader,
    decimal SumPasientpris, decimal SumHonorar, decimal SumPartnerAndel, decimal SumPlattformAndel,
    decimal SumRettighetshaverKostnad)
{
    /// <summary>
    /// Plattformens REELLE margin — plattforminntekt minus kjente
    /// rettighetshaver-lisenskostnader (se Test.RettighetshaverKostnadKr og
    /// docs/beslutningslogg.md "Abonnement/subscription-system"). SumPlattformAndel
    /// alene overvurderer margin for tester med en reell lisenskostnad.
    /// </summary>
    public decimal NettoPlattformMargin => SumPlattformAndel - SumRettighetshaverKostnad;
}

public sealed record BehandlerHonorarRad(DateTimeOffset Dato, long TestTildelingId, string TestNavn, decimal HonorarKr);

public sealed record BehandlerOkonomiRapport(int Aar, int Maaned, IReadOnlyList<BehandlerHonorarRad> Rader, decimal SumHonorar);

public sealed record PartnerBehandlerRad(string BehandlerNavn, int AntallTester, decimal SumPartnerAndelKr);

public sealed record PartnerOkonomiRapport(int Aar, int Maaned, IReadOnlyList<PartnerBehandlerRad> Rader, int TotaltAntallTester, decimal TotalSumPartnerAndelKr);

/// <summary>
/// Beregner de tre periodiserte oppgjørsrapportene ("Periodiserte oppgjør",
/// designforslag 2026-10-10 — se docs/beslutningslogg.md) fra den allerede
/// eksisterende Pengebevegelse-loggen. Rene LES-operasjoner — ingen av disse
/// metodene skriver noe; de gjenbruker nøyaktig de samme radene som
/// UtbetalingsBatchService henter til selve utbetalingen. Periode er alltid
/// en kalendermåned (år+måned), samme periodisering som Admin/Okonomi/Index
/// og UtbetalingsBatch bruker.
/// </summary>
public sealed class OkonomiRapportService
{
    private readonly AppDbContext _db;

    public OkonomiRapportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminOkonomiRapport> HentAdminRapportAsync(int aar, int maaned, CancellationToken cancellationToken = default)
    {
        var (start, slutt) = PeriodeRange(aar, maaned);
        var bevegelser = await _db.Pengebevegelser
            .Where(p => p.OpprettetUtc >= start && p.OpprettetUtc < slutt && p.TestTildelingId != null)
            .ToListAsync(cancellationToken);

        var tildelingTilTest = await HentTildelingTilTestNavnAsync(bevegelser, cancellationToken);
        var tildelingTilKostnad = await HentTildelingTilRettighetshaverKostnadAsync(bevegelser, cancellationToken);
        var behandlerNavn = await HentBehandlerNavnAsync(
            bevegelser.Where(p => p.BehandlerId != null).Select(p => p.BehandlerId!.Value), cancellationToken);

        var rader = bevegelser
            .GroupBy(p => p.TestTildelingId!.Value)
            .Select(g =>
            {
                var behandlerId = g.Select(p => p.BehandlerId).FirstOrDefault(id => id != null);
                return new OkonomiTransaksjonsRad(
                    Dato: g.Min(p => p.OpprettetUtc),
                    TestTildelingId: g.Key,
                    TestNavn: tildelingTilTest.GetValueOrDefault(g.Key, "(ukjent test)"),
                    BehandlerNavn: behandlerId is long id ? behandlerNavn.GetValueOrDefault(id, "(ukjent behandler)") : null,
                    PasientprisKr: g.Where(p => p.Type == PengebevegelseType.PasientBetalingMottatt).Sum(p => p.BelopKr),
                    HonorarKr: g.Where(p => p.Type == PengebevegelseType.BehandlerHonorar).Sum(p => p.BelopKr),
                    PlattformAndelKr: g.Where(p => p.Type == PengebevegelseType.PlattformInntekt).Sum(p => p.BelopKr),
                    PartnerAndelKr: g.Any(p => p.Type == PengebevegelseType.PartnerAndel)
                        ? g.Where(p => p.Type == PengebevegelseType.PartnerAndel).Sum(p => p.BelopKr)
                        : null,
                    RettighetshaverKostnadKr: tildelingTilKostnad.GetValueOrDefault(g.Key, 0m));
            })
            .OrderBy(r => r.Dato)
            .ToList();

        return new AdminOkonomiRapport(
            aar, maaned, rader,
            SumPasientpris: rader.Sum(r => r.PasientprisKr),
            SumHonorar: rader.Sum(r => r.HonorarKr),
            SumPartnerAndel: rader.Sum(r => r.PartnerAndelKr ?? 0),
            SumPlattformAndel: rader.Sum(r => r.PlattformAndelKr),
            SumRettighetshaverKostnad: rader.Sum(r => r.RettighetshaverKostnadKr));
    }

    public async Task<BehandlerOkonomiRapport> HentBehandlerRapportAsync(long behandlerId, int aar, int maaned, CancellationToken cancellationToken = default)
    {
        var (start, slutt) = PeriodeRange(aar, maaned);
        var bevegelser = await _db.Pengebevegelser
            .Where(p => p.Type == PengebevegelseType.BehandlerHonorar && p.BehandlerId == behandlerId
                && p.OpprettetUtc >= start && p.OpprettetUtc < slutt && p.TestTildelingId != null)
            .ToListAsync(cancellationToken);

        var tildelingTilTest = await HentTildelingTilTestNavnAsync(bevegelser, cancellationToken);

        var rader = bevegelser
            .Select(p => new BehandlerHonorarRad(p.OpprettetUtc, p.TestTildelingId!.Value, tildelingTilTest.GetValueOrDefault(p.TestTildelingId!.Value, "(ukjent test)"), p.BelopKr))
            .OrderBy(r => r.Dato)
            .ToList();

        return new BehandlerOkonomiRapport(aar, maaned, rader, rader.Sum(r => r.HonorarKr));
    }

    public async Task<PartnerOkonomiRapport> HentPartnerRapportAsync(long partnerId, int aar, int maaned, CancellationToken cancellationToken = default)
    {
        var (start, slutt) = PeriodeRange(aar, maaned);
        var bevegelser = await _db.Pengebevegelser
            .Where(p => p.Type == PengebevegelseType.PartnerAndel && p.PartnerId == partnerId
                && p.OpprettetUtc >= start && p.OpprettetUtc < slutt && p.TestTildelingId != null)
            .ToListAsync(cancellationToken);

        // PartnerAndel-raden selv bærer ikke BehandlerId (se TestService.MarkerBetalingBetaltAsync)
        // — behandleren må slås opp via tildelingen selv.
        var tildelingIder = bevegelser.Select(p => p.TestTildelingId!.Value).Distinct().ToList();
        var tildelinger = await _db.TestTildelinger
            .Where(t => tildelingIder.Contains(t.Id))
            .Select(t => new { t.Id, t.TildeltAvBehandlerId })
            .ToListAsync(cancellationToken);
        var tildelingTilBehandler = tildelinger.ToDictionary(t => t.Id, t => t.TildeltAvBehandlerId);

        var behandlerNavn = await HentBehandlerNavnAsync(
            tildelinger.Where(t => t.TildeltAvBehandlerId != null).Select(t => t.TildeltAvBehandlerId!.Value), cancellationToken);

        var rader = bevegelser
            .GroupBy(p => tildelingTilBehandler.GetValueOrDefault(p.TestTildelingId!.Value))
            .Select(g => new PartnerBehandlerRad(
                BehandlerNavn: g.Key is long id ? behandlerNavn.GetValueOrDefault(id, "(ukjent behandler)") : "(ukjent behandler)",
                AntallTester: g.Count(),
                SumPartnerAndelKr: g.Sum(p => p.BelopKr)))
            .OrderByDescending(r => r.SumPartnerAndelKr)
            .ToList();

        return new PartnerOkonomiRapport(aar, maaned, rader, rader.Sum(r => r.AntallTester), rader.Sum(r => r.SumPartnerAndelKr));
    }

    private async Task<Dictionary<long, string>> HentTildelingTilTestNavnAsync(List<Pengebevegelse> bevegelser, CancellationToken cancellationToken)
    {
        var tildelingIder = bevegelser.Where(p => p.TestTildelingId != null).Select(p => p.TestTildelingId!.Value).Distinct().ToList();
        var tildelinger = await _db.TestTildelinger
            .Where(t => tildelingIder.Contains(t.Id))
            .Select(t => new { t.Id, t.TestId })
            .ToListAsync(cancellationToken);

        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var testNavn = await _db.Tester
            .Where(t => testIder.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);

        return tildelinger.ToDictionary(t => t.Id, t => testNavn.GetValueOrDefault(t.TestId, "(ukjent test)"));
    }

    private async Task<Dictionary<long, decimal>> HentTildelingTilRettighetshaverKostnadAsync(List<Pengebevegelse> bevegelser, CancellationToken cancellationToken)
    {
        var tildelingIder = bevegelser.Where(p => p.TestTildelingId != null).Select(p => p.TestTildelingId!.Value).Distinct().ToList();
        var tildelinger = await _db.TestTildelinger
            .Where(t => tildelingIder.Contains(t.Id))
            .Select(t => new { t.Id, t.TestId })
            .ToListAsync(cancellationToken);

        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var kostnadPerTest = await _db.Tester
            .Where(t => testIder.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.RettighetshaverKostnadKr, cancellationToken);

        return tildelinger.ToDictionary(t => t.Id, t => kostnadPerTest.GetValueOrDefault(t.TestId, 0m));
    }

    private async Task<Dictionary<long, string>> HentBehandlerNavnAsync(IEnumerable<long> behandlerIder, CancellationToken cancellationToken)
    {
        var ider = behandlerIder.Distinct().ToList();
        var behandlere = await _db.Behandlere.Where(b => ider.Contains(b.Id)).ToListAsync(cancellationToken);
        return behandlere.ToDictionary(b => b.Id, b => b.Visningsnavn ?? b.Email);
    }

    private static (DateTimeOffset Start, DateTimeOffset Slutt) PeriodeRange(int aar, int maaned)
    {
        var start = new DateTimeOffset(aar, maaned, 1, 0, 0, 0, TimeSpan.Zero);
        return (start, start.AddMonths(1));
    }
}
