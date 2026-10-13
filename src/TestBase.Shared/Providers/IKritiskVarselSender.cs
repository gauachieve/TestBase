namespace TestBase.Shared.Providers;

/// <summary>
/// Grensesnitt for push-varsel ved kritisk driftsfeil (Admin/Driftsstatus, brukerbeslutning
/// 2026-10-13 — valgte Pushover for Tier 1-varsling i stedet for en egen Android-app). Ekte
/// implementasjon er <see cref="PushoverKritiskVarselSender"/>, mock-implementasjonen logger kun.
/// </summary>
public interface IKritiskVarselSender
{
    Task SendAsync(string mottakerBrukerKey, string tittel, string melding, CancellationToken cancellationToken = default);
}
