using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;

namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>Leser/skriver den ENE DriftsbryterInnstilling-raden (Id=1, opprettes lat ved første lesing/skriving) — samme mønster som EktBankIdInnstillingService.</summary>
public sealed class DriftsbryterInnstillingService
{
    private readonly AppDbContext _db;

    public DriftsbryterInnstillingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DriftsbryterInnstilling> HentAsync(CancellationToken cancellationToken = default)
    {
        var rad = await _db.DriftsbryterInnstillinger.FirstOrDefaultAsync(cancellationToken);
        if (rad is not null)
        {
            return rad;
        }

        rad = new DriftsbryterInnstilling();
        _db.DriftsbryterInnstillinger.Add(rad);
        await _db.SaveChangesAsync(cancellationToken);
        return rad;
    }

    /// <summary>Egen setter per bryter (ikke én kombinert) — SMS vises/redigeres uavhengig av Betaling/Utbetaling (se Admin/MinSide, Betaling/Utbetaling er skjult på beta), og et skjema som kun poster ÉN av dem skal aldri risikere å nulle ut de andre.</summary>
    public async Task SettSmsErAktivAsync(bool erAktiv, long endretAvUserId, CancellationToken cancellationToken = default)
    {
        var rad = await HentAsync(cancellationToken);
        rad.SmsErAktiv = erAktiv;
        rad.SistEndretAvUserId = endretAvUserId;
        rad.SistEndretUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SettBetalingOgUtbetalingAsync(bool betalingErAktiv, bool utbetalingErAktiv, long endretAvUserId, CancellationToken cancellationToken = default)
    {
        var rad = await HentAsync(cancellationToken);
        rad.BetalingErAktiv = betalingErAktiv;
        rad.UtbetalingErAktiv = utbetalingErAktiv;
        rad.SistEndretAvUserId = endretAvUserId;
        rad.SistEndretUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SettKritiskVarslingAsync(bool erAktiv, string? pushoverBrukerKey, long endretAvUserId, CancellationToken cancellationToken = default)
    {
        var rad = await HentAsync(cancellationToken);
        rad.KritiskVarslingErAktiv = erAktiv;
        rad.KritiskVarslingPushoverBrukerKey = string.IsNullOrWhiteSpace(pushoverBrukerKey) ? null : pushoverBrukerKey.Trim();
        rad.SistEndretAvUserId = endretAvUserId;
        rad.SistEndretUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
