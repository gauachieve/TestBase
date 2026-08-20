using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Security;

namespace TestBase.Shared.Data;

/// <summary>
/// Felles EF Core-kontekst. Utvides med flere DbSet-er etter hvert som
/// fase 2–4 legger til administrator-, behandler-, pasient- og testdata.
/// All tilgang til databasen skal gå gjennom denne (eller et repository-lag
/// bygget på den) — ikke rå SQL spredt i applikasjonskoden — nettopp for at
/// kryptering/tilgangsstyring kan legges til ett sted senere uten å måtte
/// endre kall-steder overalt.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("audit_log_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActorUserId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ActorRole).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(64).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Details).HasMaxLength(2000);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.TimestampUtc);
        });
    }
}
