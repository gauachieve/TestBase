using TestBase.Shared.Data;

namespace TestBase.Shared.Security;

/// <summary>
/// Standard-implementasjon av IAuditLogger som skriver til AppDbContext.
/// Brukes i alle miljøer (dev, test, prod) — se IAuditLogger for begrunnelse.
/// </summary>
public sealed class EfAuditLogger : IAuditLogger
{
    private readonly AppDbContext _db;

    public EfAuditLogger(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        string actorUserId,
        string actorRole,
        string action,
        string entityType,
        string entityId,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        // Siste forsvarslinje mot AppDbContext sine HasMaxLength-grenser på AuditLogEntry — en
        // for lang verdi her skal ALDRI kaste en DbUpdateException og krasje selve forespørselen
        // (en logging-detalj skal aldri kunne velte en reell brukerhandling). Skjedde reelt for
        // EntityId ved "tildel alle tester" (se AuditBatch) — kallsteder bør likevel holde seg
        // innenfor grensene selv (AuditBatch.EntityId, kortere Details), dette er kun et sikkerhetsnett.
        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorUserId = Truncate(actorUserId, 64),
            ActorRole = Truncate(actorRole, 32),
            Action = Truncate(action, 64),
            EntityType = Truncate(entityType, 64),
            EntityId = Truncate(entityId, 64),
            Details = details is null ? null : Truncate(details, 2000),
            TimestampUtc = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
