using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;

namespace TestBase.Shared.Domain.Utbetaling;

/// <summary>
/// Genererer det månedlige utbetalingsoppgjøret — ren lesning/skriving mot
/// databasen, ALDRI noe Stripe-kall (penger flyttes først når en
/// administrator eksplisitt godkjenner en linje, se Admin/Utbetalinger). Kalt
/// fra UtbetalingsBatchBakgrunnstjeneste, samme "sjekk om det trengs"-mønster
/// som PlanlagtTildelingService/ProgramService.
///
/// Pengebevegelse-rader av type PartnerAndel/BehandlerHonorar skrives KUN fra
/// TestService.MarkerBetalingBetaltAsync, som kun kjører etter at
/// TestTildelingBetaling.Status er satt til Betalt — enhver slik rad som
/// finnes er derfor per definisjon allerede betalt av pasienten, ingen egen
/// statussjekk trengs her.
/// </summary>
public sealed class UtbetalingsBatchService
{
    private readonly AppDbContext _db;

    public UtbetalingsBatchService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Due når vi har rullet inn i en ny kalendermåned OG ingen batch ennå
    /// finnes for FORRIGE måned — genererer da KUN for forrige måned (aldri
    /// inneværende, ennå-ikke-avsluttede måned).
    /// </summary>
    public async Task<bool> ErDueAsync(DateTimeOffset naaUtc, CancellationToken cancellationToken = default)
    {
        var (aar, maned) = ForrigeManed(naaUtc);
        return !await _db.UtbetalingsBatcher.AnyAsync(b => b.Aar == aar && b.Maned == maned, cancellationToken);
    }

    /// <summary>
    /// Genererer batchen for forrige måned — returnerer null (ingen feil) hvis
    /// en batch allerede finnes (idempotent, unik indeks på (Aar, Maned) er
    /// det reelle vernet) eller hvis det ikke finnes noe å betale ut.
    /// </summary>
    public async Task<UtbetalingsBatch?> GenererForrigeManedAsync(DateTimeOffset naaUtc, CancellationToken cancellationToken = default)
    {
        var (aar, maned) = ForrigeManed(naaUtc);
        if (await _db.UtbetalingsBatcher.AnyAsync(b => b.Aar == aar && b.Maned == maned, cancellationToken))
        {
            return null;
        }

        var periodeStart = new DateTimeOffset(aar, maned, 1, 0, 0, 0, TimeSpan.Zero);
        var periodeSlutt = periodeStart.AddMonths(1);

        var alleredeInkludertIder = _db.UtbetalingsLinjePengebevegelser.Select(x => x.PengebevegelseId);

        var kvalifiserte = await _db.Pengebevegelser
            .Where(p =>
                (p.Type == PengebevegelseType.PartnerAndel || p.Type == PengebevegelseType.BehandlerHonorar) &&
                p.OpprettetUtc >= periodeStart && p.OpprettetUtc < periodeSlutt &&
                !alleredeInkludertIder.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (kvalifiserte.Count == 0)
        {
            return null;
        }

        var batch = new UtbetalingsBatch
        {
            Aar = aar,
            Maned = maned,
            Status = UtbetalingsBatchStatus.Utkast,
            GenerertUtc = DateTimeOffset.UtcNow
        };
        _db.UtbetalingsBatcher.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        var grupper = kvalifiserte.GroupBy(p => p.Type == PengebevegelseType.BehandlerHonorar
            ? (MottakerType: MottakerType.Behandler, MottakerId: p.BehandlerId)
            : (MottakerType: MottakerType.Partner, MottakerId: p.PartnerId));

        var totalBelop = 0m;
        foreach (var gruppe in grupper)
        {
            if (gruppe.Key.MottakerId is null)
            {
                // Bør aldri forekomme (BehandlerHonorar/PartnerAndel skrives alltid med en mottaker-id),
                // men én rar rad skal ikke velte hele batch-genereringen.
                continue;
            }

            var belop = gruppe.Sum(p => p.BelopKr);
            var linje = new UtbetalingsLinje
            {
                UtbetalingsBatchId = batch.Id,
                MottakerType = gruppe.Key.MottakerType,
                BehandlerId = gruppe.Key.MottakerType == MottakerType.Behandler ? gruppe.Key.MottakerId : null,
                PartnerId = gruppe.Key.MottakerType == MottakerType.Partner ? gruppe.Key.MottakerId : null,
                BelopKr = belop,
                AntallUnderliggendeTransaksjoner = gruppe.Count(),
                Status = UtbetalingsLinjeStatus.Utkast
            };
            _db.UtbetalingsLinjer.Add(linje);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var pengebevegelse in gruppe)
            {
                _db.UtbetalingsLinjePengebevegelser.Add(new UtbetalingsLinjePengebevegelse
                {
                    UtbetalingsLinjeId = linje.Id,
                    PengebevegelseId = pengebevegelse.Id
                });
            }

            totalBelop += belop;
        }

        batch.TotalBelopKr = totalBelop;
        await _db.SaveChangesAsync(cancellationToken);

        return batch;
    }

    /// <summary>
    /// Sekundær bekreftelse fra transfer.created-webhooken (se PaymentWebhooks.cs)
    /// — selve linjen settes normalt allerede synkront fra API-responsen i
    /// Admin/Utbetalinger/Detaljer.cshtml.cs sitt godkjenn/gjenta-kall, så dette
    /// er KUN et sikkerhetsnett, idempotent (no-op hvis linjen allerede er
    /// Overfort eller ikke finnes i det hele tatt).
    /// </summary>
    public async Task BekreftOverforingAsync(string stripeTransferId, CancellationToken cancellationToken = default)
    {
        var linje = await _db.UtbetalingsLinjer.FirstOrDefaultAsync(l => l.StripeTransferId == stripeTransferId, cancellationToken);
        if (linje is null || linje.Status == UtbetalingsLinjeStatus.Overfort)
        {
            return;
        }

        linje.Status = UtbetalingsLinjeStatus.Overfort;
        linje.OverfortUtc ??= DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// payout.failed dekker et HELT ANNET feilpunkt enn en mislykket Transfer:
    /// Transfer-en til mottakerens Connect-konto kan ha LYKTES (linjen allerede
    /// Overfort), men Stripes EGEN etterfølgende utbetaling fra den kontoen til
    /// mottakerens bank feiler likevel senere (f.eks. lukket bankkonto) — se
    /// UtbetalingsMottakerKonto sin klassekommentar. Stripe sine automatiske
    /// kontoutbetalinger feier OFTE med flere transfers samlet, så en
    /// payout.failed-hendelse kan IKKE pålitelig spores tilbake til nøyaktig
    /// hvilken UtbetalingsLinje den gjaldt uten en dypere
    /// balance-transaction-oppslag (ikke bygget her) — denne metoden flagger
    /// derfor KUN mottakeren generelt (høylytt logging) i stedet for å gjette
    /// feil linje og sette en korrekt Overfort-linje tilbake til Feilet.
    /// Admin må undersøke i Stripe sitt eget dashbord og eventuelt bruke
    /// "Prøv igjen" manuelt på riktig linje.
    /// </summary>
    public async Task<UtbetalingsMottakerKonto?> FinnMottakerVedStripeAccountIdAsync(string stripeAccountId, CancellationToken cancellationToken = default) =>
        await _db.UtbetalingsMottakerKontoer.FirstOrDefaultAsync(k => k.StripeAccountId == stripeAccountId, cancellationToken);

    private static (int Aar, int Maned) ForrigeManed(DateTimeOffset naaUtc)
    {
        var forrige = naaUtc.UtcDateTime.AddMonths(-1);
        return (forrige.Year, forrige.Month);
    }
}
