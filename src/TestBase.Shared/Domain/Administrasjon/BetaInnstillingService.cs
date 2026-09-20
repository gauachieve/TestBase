using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;

namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>Leser/skriver den ENE BetaBetalingsinnstilling-raden (Id=1, opprettes lat ved første lesing/skriving).</summary>
public sealed class BetaInnstillingService
{
    private readonly AppDbContext _db;

    public BetaInnstillingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BetaBetalingsinnstilling> HentAsync(CancellationToken cancellationToken = default)
    {
        var rad = await _db.BetaBetalingsinnstillinger.FirstOrDefaultAsync(cancellationToken);
        if (rad is not null)
        {
            return rad;
        }

        rad = new BetaBetalingsinnstilling();
        _db.BetaBetalingsinnstillinger.Add(rad);
        await _db.SaveChangesAsync(cancellationToken);
        return rad;
    }

    public async Task SettModusAsync(VippsDriftsmodus vippsModus, StripeDriftsmodus stripeModus, long endretAvUserId, CancellationToken cancellationToken = default)
    {
        var rad = await HentAsync(cancellationToken);
        rad.VippsModus = vippsModus;
        rad.StripeModus = stripeModus;
        rad.SistEndretAvUserId = endretAvUserId;
        rad.SistEndretUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
