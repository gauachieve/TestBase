namespace TestBase.Shared.Security;

/// <summary>
/// Utviklingsimplementasjon av ICurrentUserContext — later som en fast,
/// fiktiv "utvikler"-bruker er innlogget, uten noen faktisk autentisering.
/// KUN for lokalt utviklingsmiljø. Fase 2 legger til en ekte implementasjon
/// (BankID for prod, passord for dev-modus admin) registrert i stedet for
/// denne i Program.cs når den er klar.
/// </summary>
public sealed class DevCurrentUserContext : ICurrentUserContext
{
    public string UserId => "dev-utvikler-1";
    public string DisplayName => "Utvikler (lokalt dev-miljø)";
    public UserRole Role => UserRole.Utvikler;
    public bool IsAuthenticated => true;
}
