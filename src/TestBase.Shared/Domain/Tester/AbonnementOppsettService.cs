using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;

namespace TestBase.Shared.Domain.Tester;

/// <summary>Leser/skriver den ENE AbonnementOppsett-raden (Id=1, opprettes lat) — samme mønster som AdminOkonomiOppsettService.</summary>
public sealed class AbonnementOppsettService
{
    private readonly AppDbContext _db;

    public AbonnementOppsettService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AbonnementOppsett> HentAsync(CancellationToken cancellationToken = default)
    {
        var rad = await _db.AbonnementOppsett.FirstOrDefaultAsync(cancellationToken);
        if (rad is not null)
        {
            return rad;
        }

        rad = new AbonnementOppsett();
        _db.AbonnementOppsett.Add(rad);
        await _db.SaveChangesAsync(cancellationToken);
        return rad;
    }

    public async Task LagreAsync(AbonnementOppsett innkommende, long endretAvUserId, CancellationToken cancellationToken = default)
    {
        var rad = await HentAsync(cancellationToken);
        rad.SmsKostnadKr = innkommende.SmsKostnadKr;
        rad.EpostKostnadKr = innkommende.EpostKostnadKr;
        rad.EkstraPaaminnelseSmsKostnadKr = innkommende.EkstraPaaminnelseSmsKostnadKr;
        rad.BetalingsgebyrKr = innkommende.BetalingsgebyrKr;
        rad.TerapeutGratisProsent = innkommende.TerapeutGratisProsent;
        rad.PasientenGratisFlatKr = innkommende.PasientenGratisFlatKr;
        rad.PasientenGratisProsent = innkommende.PasientenGratisProsent;
        rad.ForutsigbartFlatKr = innkommende.ForutsigbartFlatKr;
        rad.SistEndretAvUserId = endretAvUserId;
        rad.SistEndretUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
