using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;

namespace TestBase.Shared.Domain.Tester;

/// <summary>Leser/skriver den ENE KorFitOppsett-raden (Id=1, opprettes lat) — samme mønster som AbonnementOppsettService.</summary>
public sealed class KorFitOppsettService
{
    private readonly AppDbContext _db;

    public KorFitOppsettService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<KorFitOppsett> HentAsync(CancellationToken cancellationToken = default)
    {
        var rad = await _db.KorFitOppsett.FirstOrDefaultAsync(cancellationToken);
        if (rad is not null)
        {
            return rad;
        }

        rad = new KorFitOppsett();
        _db.KorFitOppsett.Add(rad);
        await _db.SaveChangesAsync(cancellationToken);
        return rad;
    }

    public async Task LagreAsync(KorFitOppsett innkommende, long endretAvUserId, CancellationToken cancellationToken = default)
    {
        var rad = await HentAsync(cancellationToken);
        rad.OrsTimerForTimeOffset = innkommende.OrsTimerForTimeOffset;
        rad.SrsMinutterEtterTimeOffset = innkommende.SrsMinutterEtterTimeOffset;
        rad.SistEndretAvUserId = endretAvUserId;
        rad.SistEndretUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
