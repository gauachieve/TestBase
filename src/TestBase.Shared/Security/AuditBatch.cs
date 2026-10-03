namespace TestBase.Shared.Security;

/// <summary>
/// Bygger en trygg EntityId-verdi for en audit-loggrad som dekker FLERE entiteter på én gang
/// (bulk-tildeling, bulk-prising, bulk-godkjenning). En rå `string.Join(",", ider)` passer IKKE
/// inn i AuditLogEntry.EntityId (HasMaxLength(64) i AppDbContext) så snart antallet blir stort nok
/// — skjedde reelt ved "tildel alle tester" på live (2026-10-03, se docs/beslutningslogg.md "Reell
/// 500-feil ved bulk-tildeling"): `DbUpdateException`/`MySqlException: Data too long for column
/// 'EntityId'`. Den fulle listen hører uansett bedre hjemme i `details` (2000 tegns grense, og
/// EntityId sin kolonne er indeksert for oppslag på ÉN entitet, ikke en kommaseparert liste).
/// </summary>
public static class AuditBatch
{
    public static string EntityId(IReadOnlyCollection<long> ider) =>
        ider.Count == 1 ? ider.Single().ToString() : $"batch:{ider.Count}";
}
