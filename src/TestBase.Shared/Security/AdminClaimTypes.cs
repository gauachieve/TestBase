namespace TestBase.Shared.Security;

/// <summary>
/// Egendefinerte claim-typer utover de fra <see cref="System.Security.Claims.ClaimTypes"/>.
/// </summary>
public static class AdminClaimTypes
{
    /// <summary>
    /// Rollen brukeren faktisk logget inn med (før evt. rollebytte via
    /// Bytt-modus). Kun satt til "Utvikler" hvis vedkommende logget inn med
    /// AdminId+passord. Brukes til å avgjøre om rollebytte er tillatt —
    /// selve <see cref="System.Security.Claims.ClaimTypes.Role"/>-claimen kan
    /// være byttet til noe annet for testing.
    /// </summary>
    public const string BaseRolle = "testbase:base-rolle";
}
