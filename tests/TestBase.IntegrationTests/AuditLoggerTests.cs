using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestBase.IntegrationTests.Infrastructure;
using TestBase.Shared.Data;
using TestBase.Shared.Security;
using Xunit;

namespace TestBase.IntegrationTests;

/// <summary>
/// Regresjonstest for en reell 500-krasj på live (2026-10-03, se docs/beslutningslogg.md "Reell
/// 500-feil ved bulk-tildeling"): å tildele et stort antall tester (f.eks. "velg alle") til en
/// pasient kalte `_auditLogger.LogAsync` med en rå `string.Join(",", testIder)` som EntityId —
/// `AuditLogEntry.EntityId` har `HasMaxLength(64)` i AppDbContext, og med ~85 tester i systemet
/// ble denne strengen lett over 64 tegn, noe som kastet `DbUpdateException`/`MySqlException:
/// Data too long for column 'EntityId'` og veltet HELE forespørselen (Admin/Behandlerportal
/// Tildel/Tester, Prising/Index, og MinSide sin bulk-godkjenning av test-tilgangsforespørsler —
/// se AuditBatch.cs for alle berørte kallsteder). Fikset på to nivåer: (1) kallstedene bruker nå
/// AuditBatch.EntityId (kort, alltid under 64 tegn) + flytter den fulle ID-listen til `details`,
/// (2) EfAuditLogger.LogAsync trunkerer defensivt ALLE felt mot kolonnegrensene som et
/// sikkerhetsnett, slik at en logging-detalj aldri kan velte en reell brukerhandling.
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class AuditLoggerTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public AuditLoggerTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void AuditBatch_EntityId_EnkeltId_ReturnererIdSelv()
    {
        Assert.Equal("42", AuditBatch.EntityId(new List<long> { 42 }));
    }

    [Fact]
    public void AuditBatch_EntityId_ManyIder_ReturnererKortBatchBeskrivelse()
    {
        var mangeIder = Enumerable.Range(1, 200).Select(i => (long)i).ToList();
        var entityId = AuditBatch.EntityId(mangeIder);

        Assert.Equal("batch:200", entityId);
        Assert.True(entityId.Length <= 64);
    }

    [Fact]
    public async Task LogAsync_MedMangeTildelteTestIderSomTidligereKrasjet_LagresUtenException()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();

        // Simulerer eksakt scenarioet som krasjet på live: "velg alle" ga en liste på ~85
        // test-IDer, som en rå string.Join(",", ...) ikke fikk plass til i 64 tegn.
        var mangeTestIder = Enumerable.Range(1, 90).Select(i => (long)i).ToList();

        var exception = await Record.ExceptionAsync(() => auditLogger.LogAsync(
            "999", "Administrator", "TildelTesterBatch",
            "TestTildeling", AuditBatch.EntityId(mangeTestIder),
            $"TestIder {string.Join(",", mangeTestIder)}; PasientIder 1,2,3"));

        Assert.Null(exception);

        var rad = await db.AuditLogEntries
            .Where(a => a.Action == "TildelTesterBatch" && a.ActorUserId == "999")
            .OrderByDescending(a => a.Id)
            .FirstAsync();
        Assert.Equal("batch:90", rad.EntityId);
        Assert.Contains("TestIder 1,2,3,4,5", rad.Details);
    }

    [Fact]
    public async Task LogAsync_MedForLangEntityIdUtenomAuditBatch_TrunkererIStedenForAKrasje()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();

        // Sikkerhetsnettet i EfAuditLogger.LogAsync skal fange ETHVERT fremtidig kallsted som
        // (feilaktig) sender en for lang verdi direkte, ikke bare dem som allerede er fikset.
        var forLangEntityId = string.Join(",", Enumerable.Range(1, 50));
        Assert.True(forLangEntityId.Length > 64);

        var exception = await Record.ExceptionAsync(() => auditLogger.LogAsync(
            "998", "Administrator", "TestAvSikkerhetsnett", "Test", forLangEntityId));

        Assert.Null(exception);

        var rad = await db.AuditLogEntries
            .Where(a => a.Action == "TestAvSikkerhetsnett" && a.ActorUserId == "998")
            .OrderByDescending(a => a.Id)
            .FirstAsync();
        Assert.True(rad.EntityId.Length <= 64);
    }
}
