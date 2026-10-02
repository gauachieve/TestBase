using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;

namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>Leser/skriver den ENE EktBankIdInnstilling-raden (Id=1, opprettes lat ved første lesing/skriving) — samme mønster som BetaInnstillingService.</summary>
public sealed class EktBankIdInnstillingService
{
    private readonly AppDbContext _db;

    public EktBankIdInnstillingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EktBankIdInnstilling> HentAsync(CancellationToken cancellationToken = default)
    {
        var rad = await _db.EktBankIdInnstillinger.FirstOrDefaultAsync(cancellationToken);
        if (rad is not null)
        {
            return rad;
        }

        rad = new EktBankIdInnstilling();
        _db.EktBankIdInnstillinger.Add(rad);
        await _db.SaveChangesAsync(cancellationToken);
        return rad;
    }

    public async Task SettErAktivAsync(bool erAktiv, long endretAvUserId, CancellationToken cancellationToken = default)
    {
        var rad = await HentAsync(cancellationToken);
        rad.ErAktiv = erAktiv;
        rad.SistEndretAvUserId = endretAvUserId;
        rad.SistEndretUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
