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
        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            TimestampUtc = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
