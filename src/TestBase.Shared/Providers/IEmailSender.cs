namespace TestBase.Shared.Providers;

/// <summary>
/// Grensesnitt for e-postutsending (invitasjoner, rapporter, kvitteringer).
/// Ekte implementasjon velges i fase 2/6. I dev/test brukes MockEmailSender,
/// som kun logger meldingen i stedet for å faktisk sende den.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
