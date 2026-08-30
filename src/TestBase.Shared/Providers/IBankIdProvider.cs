namespace TestBase.Shared.Providers;

public sealed record BankIdResult(bool Success, string? PersonNummer, string? FullName, string? ErrorMessage);

/// <summary>
/// Grensesnitt mot BankID-innlogging/-signering. Ekte implementasjon
/// (fase 2) kobles mot en leverandør som Signicat eller Criipto — se
/// beslutningsloggen. I dev/test brukes MockBankIdProvider, som ALDRI
/// kaller noen ekstern tjeneste.
/// </summary>
public interface IBankIdProvider
{
    /// <summary>
    /// <paramref name="personnummerOverride"/> brukes KUN av MockBankIdProvider
    /// (et dev-only felt på innloggingssidene, se beslutningsloggen "BankID
    /// personnummer-overstyring") for å kunne bytte mellom flere test-personer
    /// uten en ekte BankID-sesjon. En ekte leverandørimplementasjon vil ignorere
    /// parameteren — ekte BankID bestemmer alltid personen selv.
    /// </summary>
    Task<BankIdResult> AuthenticateAsync(string? personnummerOverride = null, CancellationToken cancellationToken = default);
}
