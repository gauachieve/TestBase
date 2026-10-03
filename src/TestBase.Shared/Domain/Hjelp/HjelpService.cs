using TestBase.Shared.Security;

namespace TestBase.Shared.Domain.Hjelp;

/// <summary>
/// Leser det statiske innholdet i HjelpInnhold — ingen database, så ingen async/DB-kall
/// nødvendig. Registrert som Singleton i Program.cs (innholdet er immutabelt per prosess).
/// </summary>
public sealed class HjelpService
{
    /// <summary>
    /// Superadmin og Utvikler regnes som "Admin" for hjelpeformål — de ser alt en
    /// Administrator ser, og trenger ikke egne artikler for det. IsAuthenticated==false
    /// gir ALLTID Anonym her, UANSETT hva Role skulle returnere — ICurrentUserContext sin
    /// egen Role-property faller tilbake til UserRole.Pasient når ikke innlogget (se
    /// AuthenticatedCurrentUserContext), som ville vist pasient-hjelp til en ikke-innlogget
    /// besøkende hvis vi ikke sjekket IsAuthenticated FØRST.
    /// </summary>
    public static HjelpRolle TilHjelpRolle(ICurrentUserContext currentUser)
    {
        if (!currentUser.IsAuthenticated)
        {
            return HjelpRolle.Anonym;
        }

        return currentUser.Role switch
        {
            UserRole.Pasient => HjelpRolle.Pasient,
            UserRole.Behandler => HjelpRolle.Behandler,
            _ => HjelpRolle.Admin // Administrator, Superadmin, Utvikler
        };
    }

    public IReadOnlyList<HjelpArtikkel> HentForRolle(HjelpRolle rolle) =>
        HjelpInnhold.Artikler.Where(a => a.Roller.Contains(rolle)).ToList();
}
