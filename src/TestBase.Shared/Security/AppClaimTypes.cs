namespace TestBase.Shared.Security;

/// <summary>
/// Egendefinerte claim-typer utover de fra <see cref="System.Security.Claims.ClaimTypes"/>.
/// Brukes på tvers av administrator- og behandler-pålogging (se AuthSignIn).
/// </summary>
public static class AppClaimTypes
{
    /// <summary>
    /// Rollen brukeren faktisk logget inn med (før evt. rollebytte via
    /// Bytt-modus). Kun satt til "Utvikler" hvis vedkommende logget inn med
    /// AdminId+passord. Brukes til å avgjøre om rollebytte er tillatt —
    /// selve <see cref="System.Security.Claims.ClaimTypes.Role"/>-claimen kan
    /// være byttet til noe annet for testing.
    /// </summary>
    public const string BaseRolle = "testbase:base-rolle";

    /// <summary>
    /// "true" når en innlogget Behandler har lov til å administrere kolleger
    /// innenfor sin egen Partner (se Behandler.ErPartnerAdministrator og
    /// docs/beslutningslogg.md "Partner System + Test Monetization") — Partner-
    /// admin er bevisst IKKE en egen UserRole, kun en claim på Behandler-rollen.
    /// </summary>
    public const string ErPartnerAdministrator = "testbase:er-partner-administrator";

    /// <summary>Partnerens Id for en innlogget Behandler, tom streng hvis ingen (se Behandler.PartnerId).</summary>
    public const string PartnerId = "testbase:partner-id";
}
