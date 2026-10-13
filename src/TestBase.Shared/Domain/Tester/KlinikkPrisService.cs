using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Shared.Domain.Tester;

/// <summary>Resultatet av en sete-basert prisberegning for ÉN klinikk — se KlinikkPrisService.BeregnAsync.</summary>
public sealed record KlinikkPrisResultat(int AntallSeter, decimal KrPerSeteManed, decimal BruttoManedsprisKr, decimal RabattProsent, decimal NettoManedsprisKr);

/// <summary>
/// Beregner klinikkens (Partner) månedlige sete-baserte abonnementspris —
/// se docs/beslutningslogg.md "Abonnement/subscription-system". HELE
/// klinikken prises til SAMME kr/sete, bestemt av antall AKTIVE (ikke
/// arkiverte) behandlere knyttet til partneren (ikke gradert/skattetrinn-
/// stil) — se KlinikkSeterTier. Faktisk MÅNEDLIG BELASTNING (Stripe
/// Subscription/faktura) er IKKE bygget ennå, kun selve prisberegningen.
/// </summary>
public sealed class KlinikkPrisService
{
    private readonly AppDbContext _db;

    public KlinikkPrisService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<KlinikkPrisResultat?> BeregnAsync(long partnerId, CancellationToken cancellationToken = default)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == partnerId, cancellationToken);
        if (partner is null)
        {
            return null;
        }

        var antallSeter = await _db.Behandlere.CountAsync(
            b => b.PartnerId == partnerId && b.Status != BehandlerStatus.Arkivert, cancellationToken);

        if (antallSeter == 0)
        {
            return new KlinikkPrisResultat(0, 0m, 0m, partner.RabattProsent, 0m);
        }

        var tiers = await _db.KlinikkSeterTiers.OrderBy(t => t.FraAntallSeter).ToListAsync(cancellationToken);
        var tier = tiers.FirstOrDefault(t => antallSeter >= t.FraAntallSeter && (t.TilAntallSeter is null || antallSeter <= t.TilAntallSeter))
            ?? tiers.OrderByDescending(t => t.FraAntallSeter).FirstOrDefault();

        var krPerSete = tier?.KrPerSeteManed ?? 0m;
        var brutto = antallSeter * krPerSete;
        var netto = Math.Round(brutto * (1 - partner.RabattProsent / 100m), 2);

        return new KlinikkPrisResultat(antallSeter, krPerSete, brutto, partner.RabattProsent, netto);
    }
}
