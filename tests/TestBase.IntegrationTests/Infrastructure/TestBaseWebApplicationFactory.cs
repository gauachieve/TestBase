using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TestBase.IntegrationTests.TestDoubles;
using TestBase.Shared.Data;
using TestBase.Shared.Providers;
using Xunit;

namespace TestBase.IntegrationTests.Infrastructure;

/// <summary>
/// Kjører en ekte in-process instans av TestBase.Web mot en EGEN MySQL-database
/// (<see cref="TestConnectionString"/> — samme Docker-container som lokal dev,
/// men annet databasenavn enn testbase_dev, slik at automatiserte tester aldri
/// rører dataene en utvikler tester manuelt med i nettleseren samtidig).
///
/// Bruker root-tilkobling KUN for denne test-databasen (lokalt Docker-only,
/// ingen ekte hemmeligheter) — appens egen "testbase"-bruker har kun rettigheter
/// på testbase_dev, ikke på en vilkårlig ny database.
///
/// Mock-leverandørene for SMS/e-post byttes ut med Capturing-variantene slik at
/// tester kan hente ut 2FA-koder/invitasjonslenker direkte. BankID-mocken
/// (MockBankIdProvider) beholdes uendret — den er allerede deterministisk (returnerer
/// alltid samme faste testperson), noe som er akkurat det automatiserte tester vil ha.
///
/// IAsyncLifetime.InitializeAsync sletter og migrerer databasen på nytt FØR
/// noe rører Services/CreateClient (som ville trigget appens dev-seed) — se
/// kommentar i InitializeAsync for hvorfor rekkefølgen er kritisk.
/// </summary>
public sealed class TestBaseWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestConnectionString =
        "Server=localhost;Port=3306;Database=testbase_test;User=root;Password=rootpassword_ikke_bruk_i_prod;";

    public CapturingSmsSender Sms { get; } = new();
    public CapturingEmailSender Epost { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISmsSender>();
            services.AddSingleton<ISmsSender>(Sms);

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Epost);
        });
    }

    /// <summary>
    /// Sletter og migrerer testdatabasen på nytt via en frittstående AppDbContext
    /// — IKKE via denne fabrikkens egen Services — fordi det å røre Services (t.d.
    /// via CreateClient()) starter selve appen, inkludert dev-seed-blokken i
    /// Program.cs som oppretter "dev-admin". Slettes databasen ETTER at dev-seed
    /// alt har kjørt mot en gammel/annen database-tilstand, forsvinner den
    /// seedede kontoen igjen uten å bli gjenopprettet. Ved å gjøre sletting+
    /// migrering FØRST, mot en helt frittstående tilkobling, får vi en garantert
    /// tom database før appen (og dermed dev-seed) i det hele tatt starter.
    /// </summary>
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(TestConnectionString, ServerVersion.AutoDetect(TestConnectionString))
            .Options;

        var dataProtectionProvider = DataProtectionProvider.Create("TestBase.IntegrationTests");
        await using var db = new AppDbContext(options, dataProtectionProvider);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
