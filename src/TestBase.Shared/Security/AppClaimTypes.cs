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

    /// <summary>
    /// Administrator.Id til den EKTE, BankID/2FA-verifiserte Superadmin-kontoen som opprinnelig
    /// logget inn — satt KUN ved ekte innlogging som Superadmin (se AuthSignIn.LoggInnAsync), og
    /// bevart UENDRET gjennom enhver senere rollebytte via Areas/Admin/Pages/Konto/ByttIdentitet.
    /// Formålet er å la en Superadmin med ÉN ekte BankID-identitet teste hele produksjonsløpet som
    /// Administrator/Behandler/Pasient (tre separate kontoer, samme personnummer) uten å måtte
    /// logge ut og inn igjen mellom hver — se docs/beslutningslogg.md "Superadmin-identitetsbytte".
    /// I MOTSETNING TIL BaseRolle (som kun styrer DEV-ByttModus sin rene rolle-claim-forfalskning)
    /// peker denne på en EKTE konto, og selve ByttIdentitet-siden logger reelt inn som den
    /// matchende Behandler-/Pasient-raden (ekte NameIdentifier, ikke bare en annen rolle-tekst).
    /// </summary>
    public const string EktSuperadminId = "testbase:ekt-superadmin-id";
}
