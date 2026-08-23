using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
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
    private readonly IDataProtector _personnummerBeskytter;

    public AppDbContext(DbContextOptions<AppDbContext> options, IDataProtectionProvider dataProtectionProvider)
        : base(options)
    {
        _personnummerBeskytter = dataProtectionProvider.CreateProtector("TestBase.Personnummer.v1");
    }

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<Administrator> Administratorer => Set<Administrator>();
    public DbSet<Behandler> Behandlere => Set<Behandler>();
    public DbSet<BehandlerInvitasjon> BehandlerInvitasjoner => Set<BehandlerInvitasjon>();
    public DbSet<BehandlerKontaktVerifisering> BehandlerKontaktVerifiseringer => Set<BehandlerKontaktVerifisering>();
    public DbSet<ToFaktorKode> ToFaktorKoder => Set<ToFaktorKode>();
    public DbSet<Pasient> Pasienter => Set<Pasient>();
    public DbSet<PasientInvitasjon> PasientInvitasjoner => Set<PasientInvitasjon>();

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

        // Personnummer krypteres i hvile med Data Protection selv i dev (lokal
        // filbasert nøkkelring) — se prinsippet "arkitektur nå, infrastruktur
        // senere" i beslutningsloggen. Tabellen er uansett svært liten (én
        // psykolog + kontorfellesskap), så oppslag på personnummer skjer i
        // minnet (se AdminAuthenticationService/BehandlerAuthenticationService),
        // ikke via SQL WHERE, fordi Data Protection ikke er deterministisk.
        var personnummerConverter = new ValueConverter<string, string>(
            klartekst => _personnummerBeskytter.Protect(klartekst),
            kryptert => _personnummerBeskytter.Unprotect(kryptert));

        var personnummerConverterNullable = new ValueConverter<string?, string?>(
            klartekst => klartekst == null ? null : _personnummerBeskytter.Protect(klartekst),
            kryptert => kryptert == null ? null : _personnummerBeskytter.Unprotect(kryptert));

        modelBuilder.Entity<Administrator>(entity =>
        {
            entity.ToTable("administratorer");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.AdminId).IsUnique();
            entity.Property(a => a.AdminId).HasMaxLength(64).IsRequired();
            entity.Property(a => a.MobilNr).HasMaxLength(32).IsRequired();
            entity.Property(a => a.Email).HasMaxLength(256).IsRequired();
            entity.Property(a => a.FulltNavn).HasMaxLength(256).IsRequired();
            entity.Property(a => a.Personnummer).HasConversion(personnummerConverter).HasMaxLength(500).IsRequired();
            entity.Property(a => a.HprNr).HasMaxLength(32).IsRequired();
            entity.Property(a => a.PasswordHash).HasMaxLength(512);
        });

        modelBuilder.Entity<Behandler>(entity =>
        {
            entity.ToTable("behandlere");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.MobilNr).HasMaxLength(32).IsRequired();
            entity.Property(b => b.Email).HasMaxLength(256).IsRequired();
            entity.Property(b => b.Fornavn).HasMaxLength(128);
            entity.Property(b => b.Etternavn).HasMaxLength(128);
            entity.Property(b => b.Personnummer).HasConversion(personnummerConverterNullable).HasMaxLength(500);
            entity.Property(b => b.HprNr).HasMaxLength(32);
            entity.Property(b => b.Kontonummer).HasMaxLength(64);
            entity.Property(b => b.Arbeidsadresse).HasMaxLength(256);
            entity.Property(b => b.Tittel).HasMaxLength(128);
            entity.Property(b => b.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Ignore(b => b.Visningsnavn);
        });

        modelBuilder.Entity<BehandlerInvitasjon>(entity =>
        {
            entity.ToTable("behandler_invitasjoner");
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.Token).IsUnique();
            entity.Property(i => i.Token).HasMaxLength(128).IsRequired();
            entity.Property(i => i.KontaktVerdi).HasMaxLength(256).IsRequired();
            entity.Property(i => i.KontaktMetode).HasConversion<string>().HasMaxLength(16).IsRequired();
        });

        modelBuilder.Entity<BehandlerKontaktVerifisering>(entity =>
        {
            entity.ToTable("behandler_kontakt_verifiseringer");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.KodeHash).HasMaxLength(128).IsRequired();
            entity.Property(k => k.Kanal).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(k => new { k.BehandlerId, k.Kanal });
        });

        modelBuilder.Entity<ToFaktorKode>(entity =>
        {
            entity.ToTable("to_faktor_koder");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.KodeHash).HasMaxLength(128).IsRequired();
            entity.Property(k => k.PrincipalType).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(k => new { k.PrincipalType, k.PrincipalId });
        });

        modelBuilder.Entity<Pasient>(entity =>
        {
            entity.ToTable("pasienter");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Personnummer).HasConversion(personnummerConverter).HasMaxLength(500).IsRequired();
            entity.Property(p => p.MobilNr).HasMaxLength(32).IsRequired();
            entity.Property(p => p.Email).HasMaxLength(256).IsRequired();
            entity.Property(p => p.Navn).HasMaxLength(256);
            entity.Property(p => p.Gruppenavn).HasMaxLength(128);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(p => p.BehandlerId);
        });

        modelBuilder.Entity<PasientInvitasjon>(entity =>
        {
            entity.ToTable("pasient_invitasjoner");
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.Token).IsUnique();
            entity.Property(i => i.Token).HasMaxLength(128).IsRequired();
            entity.Property(i => i.KontaktVerdi).HasMaxLength(256).IsRequired();
            entity.Property(i => i.KontaktMetode).HasConversion<string>().HasMaxLength(16).IsRequired();
        });
    }
}
