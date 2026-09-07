namespace TestBase.Shared.Security;

/// <summary>
/// Roller i systemet. Speiler prosjektbeskrivelsens fire delsystemer
/// (administrasjon, behandler, pasient) pluss "utvikler" som er admin
/// i utviklingsmodus (jf. beslutningsloggen: passord-pålogging i dev,
/// BankID i produksjon — begge ender opp som en av disse rollene).
/// </summary>
public enum UserRole
{
    Utvikler,
    Administrator,
    Behandler,
    Pasient,

    /// <summary>
    /// Strengt supersett av Administrator — ser alt en Administrator ser,
    /// pluss partner-/prisingssider ingen andre admin-kontoer når. Kun ment
    /// for ÉN reell konto, se Administrator.ErSuperadmin og
    /// docs/beslutningslogg.md "Partner System + Test Monetization".
    /// </summary>
    Superadmin
}

/// <summary>
/// Representerer den innloggede brukeren for gjeldende forespørsel.
/// Dette grensesnittet er det ENESTE stedet applikasjonskode skal spørre
/// "hvem er innlogget, og med hvilken rolle" — sentralisert med vilje,
/// slik at ekte BankID/2FA-pålogging (fase 2) kan byttes inn uten å
/// røre koden som bruker grensesnittet.
/// </summary>
public interface ICurrentUserContext
{
    string UserId { get; }
    string DisplayName { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }

    /// <summary>Se AppClaimTypes.PartnerId — null hvis ikke innlogget som en partner-tilknyttet Behandler.</summary>
    long? PartnerId { get; }

    /// <summary>Se AppClaimTypes.ErPartnerAdministrator.</summary>
    bool ErPartnerAdministrator { get; }
}
