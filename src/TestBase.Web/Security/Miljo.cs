namespace TestBase.Web.Security;

/// <summary>
/// "Miljo:TillatUtviklingsSnarveier" — skiller AUTH-RELATERTE utviklingssnarveier
/// (PersonnummerOverride-bypass, 2FA-kode vist rett i nettleseren, AdminId+passord-
/// innlogging, diagnostiske BankID-/betalingstestsider) fra ASPNETCORE_ENVIRONMENT
/// alene. Live kjører bevisst "Development" KUN for automatisk databasemigrering ved
/// oppstart (se dev-seed-blokken i Program.cs) — ALDRI for disse snarveiene, siden
/// StagingGate ikke lenger står foran live og gjør dem offentlig nåbare. Sant lokalt
/// og på beta (Miljo__TillatUtviklingsSnarveier=true i infra/resources.bicep), usant
/// på live. Se docs/beslutningslogg.md "StagingGate fjernet fra live".
/// </summary>
public static class Miljo
{
    public static bool TillatUtviklingsSnarveier(IConfiguration configuration) =>
        configuration.GetValue("Miljo:TillatUtviklingsSnarveier", false);

    /// <summary>
    /// Snevrere enn TillatUtviklingsSnarveier over — styrer KUN PersonnummerOverride
    /// (admin/behandler OG pasient-innlogging). Nødvendig fordi MockBankIdProvider er
    /// den ENESTE IBankIdProvider som noensinne registreres (se Program.cs) — uten
    /// denne overstyringen returnerer den alltid samme faste fiktive personnummer
    /// ("01019012345"), så INGEN ekte administrator/behandler/pasient kan logge inn i
    /// det hele tatt før ekte BankID (Miljo:EktBankIdProfesjonell) er skrudd på. Satt
    /// midlertidig til "true" på live 2026-09-20 (13 dager til ekte BankID-avtale er
    /// klar, se docs/beslutningslogg.md) — MEN TillatUtviklingsSnarveier holdes usann på
    /// live, så AdminId+passord-bypasset, 2FA-kode-visning og de diagnostiske testsidene
    /// forblir stengt. Fjern denne innstillingen (sett til "false"/fjern) igjen så snart
    /// ekte BankID er verifisert fungerende på live.
    /// </summary>
    public static bool TillatPersonnummerOverride(IConfiguration configuration) =>
        TillatUtviklingsSnarveier(configuration) || configuration.GetValue("Miljo:TillatPersonnummerOverride", false);
}
