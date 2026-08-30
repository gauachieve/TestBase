namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Regenererer en innebygd, kode-definert test idempotent — jf. kravdokumentet:
/// "Husk alltid å lage regenerering av tester". Kalles fra dev-seed i
/// Program.cs OG fra en admin-knapp (Admin/Tester/Index), slik at mekanismen
/// fungerer i alle miljøer, ikke bare lokalt.
/// </summary>
public interface IInnebygdTestSeeder
{
    /// <summary>Matcher Test.Kode for testen denne seederen lager.</summary>
    string Kode { get; }

    /// <summary>Ingen effekt hvis en Test med denne Koden allerede finnes.</summary>
    Task SeedAsync(TestService testService, CancellationToken cancellationToken = default);
}
