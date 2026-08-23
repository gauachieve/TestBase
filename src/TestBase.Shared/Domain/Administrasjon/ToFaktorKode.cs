namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Én engangskode for SMS-basert 2. faktor i BankID-innloggingsflyten.
/// Kun hash av koden lagres. <see cref="Forsok"/> er en enkel brems mot gjetting.
/// </summary>
public sealed class ToFaktorKode
{
    public long Id { get; set; }
    public long AdministratorId { get; set; }
    public required string KodeHash { get; set; }
    public DateTimeOffset UtlopUtc { get; set; }
    public DateTimeOffset? BruktUtc { get; set; }
    public int Forsok { get; set; }
}
