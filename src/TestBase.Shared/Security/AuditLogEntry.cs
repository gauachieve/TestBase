namespace TestBase.Shared.Security;

/// <summary>
/// Én rad i audit-loggen: hvem gjorde hva, mot hvilken data, når.
/// Dette er et kjernekrav i Normen og avgjørende ved avvikssaker
/// (se compliance-risikovurdering-utkast.md i prosjektet).
/// Loggen er append-only: det finnes bevisst ingen Update/Delete-metoder
/// noe sted i koden som peker på denne tabellen.
/// </summary>
public sealed class AuditLogEntry
{
    public long Id { get; set; }
    public required string ActorUserId { get; set; }
    public required string ActorRole { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public string? Details { get; set; }
}
