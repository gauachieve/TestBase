using Xunit;

namespace TestBase.IntegrationTests.Infrastructure;

/// <summary>
/// Alle testklasser i denne samlingen deler ÉN TestBaseWebApplicationFactory
/// (og dermed én database) og kjøres SEKVENSIELT i forhold til hverandre —
/// xUnit parallelliserer aldri klasser i samme collection. Det er nødvendig
/// her siden testene bygger videre på en delt, tilbakestilt database
/// (se TestBaseWebApplicationFactory.InitializeAsync).
/// </summary>
[CollectionDefinition(Navn)]
public sealed class TestBaseCollection : ICollectionFixture<TestBaseWebApplicationFactory>
{
    public const string Navn = "TestBase";
}
