namespace TestBase.Shared.Providers;

/// <summary>
/// Grensesnitt for SMS-utsending (invitasjoner, påminnelser). Ekte
/// implementasjon kommer i fase 2 (kandidater: Link Mobility, Twilio —
/// se beslutningsloggen). I dev/test brukes MockSmsSender, som kun
/// logger meldingen i stedet for å faktisk sende den.
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string toMobileNumber, string message, CancellationToken cancellationToken = default);
}
