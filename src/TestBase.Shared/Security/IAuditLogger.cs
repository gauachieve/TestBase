namespace TestBase.Shared.Security;

/// <summary>
/// Skriver til audit-loggen. Alt kode som leser eller endrer helseopplysninger
/// (testresultater, pasientdata) skal kalle denne — i alle miljøer, også lokalt
/// utviklingsmiljø (mot lokal, ukryptert dev-database). Se prinsippet
/// "arkitektur nå, infrastruktur senere" i prosjektets beslutningslogg:
/// selve loggingen skal alltid være aktiv; det som endres mellom dev og prod
/// er bare hvilken database og hvilke nøkler den peker på.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        string actorUserId,
        string actorRole,
        string action,
        string entityType,
        string entityId,
        string? details = null,
        CancellationToken cancellationToken = default);
}
