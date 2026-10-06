namespace TestBase.Shared.Providers;

/// <summary>
/// Grensesnitt for e-postutsending (invitasjoner, rapporter, kvitteringer).
/// AzureEmailSender (Azure Communication Services) brukes når "Acs:ConnectionString"
/// er satt (Azure test-App Service), ellers MockEmailSender (lokal utvikling), som
/// kun logger meldingen i stedet for å faktisk sende den — se Program.cs.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// <paramref name="body"/> er alltid ren tekst (fallback for e-postklienter uten HTML-støtte).
    /// <paramref name="htmlBody"/> er valgfri — når satt, sendes den som den rikere HTML-varianten
    /// av meldingen (f.eks. en fargelagt knapp, se bugliste 2026-10-06 punkt 8/9). Lagt til SIST i
    /// signaturen MED default-verdi slik at eksisterende positional-kall ikke knekker (se kjent
    /// fallgruve i CLAUDE.md om å aldri sette inn en ny parameter midt i en signatur).
    /// </summary>
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default, string? htmlBody = null);
}
