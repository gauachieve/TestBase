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
    Task<BankIdResult> AuthenticateAsync(CancellationToken cancellationToken = default);
}
