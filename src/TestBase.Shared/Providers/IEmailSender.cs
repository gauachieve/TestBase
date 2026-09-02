namespace TestBase.Shared.Providers;

/// <summary>
/// Grensesnitt for e-postutsending (invitasjoner, rapporter, kvitteringer).
/// AzureEmailSender (Azure Communication Services) brukes når "Acs:ConnectionString"
/// er satt (Azure test-App Service), ellers MockEmailSender (lokal utvikling), som
/// kun logger meldingen i stedet for å faktisk sende den — se Program.cs.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
