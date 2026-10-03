namespace TestBase.Shared.Domain.Hjelp;

/// <summary>
/// Hvem en HjelpArtikkel er relevant for. IKKE 1:1 med UserRole — Superadmin og Utvikler
/// regnes som "Admin" her (de ser alt en Administrator ser, pluss noen få sider til, men
/// trenger ikke egne hjelpeartikler for det), se HjelpService.TilHjelpRolle.
/// </summary>
public enum HjelpRolle
{
    /// <summary>Ikke innlogget — se HjelpService: dette er en EGEN, SNEVRERE bøtte enn Pasient, ikke en "laveste rolle"-fallback.</summary>
    Anonym,
    Pasient,
    Behandler,
    Admin
}

/// <summary>
/// Én hjelpeartikkel — statisk innhold (se HjelpInnhold), ingen database. Samme
/// "kodeforfattet innhold, ingen admin-UI"-mønster som IInnebygdTestSeeder sine tester.
/// </summary>
/// <param name="Id">Stabil, unik nøkkel — brukt som HTML-id for ankerlenker/utvidelse.</param>
/// <param name="Tittel">Vist i listen og i søketreff.</param>
/// <param name="HtmlInnhold">Enkel HTML (avsnitt/lister) — IKKE brukerinput, trygt å rendre rått.</param>
/// <param name="Roller">Hvilke roller som ser denne artikkelen — en artikkel kan gjelde flere roller.</param>
/// <param name="KontekstPrefikser">URL-sti-prefikser denne artikkelen er ekstra relevant for (kontekstsensitivitet) — tom liste betyr "kun generell/FAQ", ikke kontekst-fremhevet noe sted.</param>
/// <param name="ErFaq">Vises i "Vanlige spørsmål" når ingen kontekst-treff finnes og søkefeltet er tomt.</param>
/// <param name="Kategori">Gruppering i "Bla i alle emner".</param>
public sealed record HjelpArtikkel(
    string Id,
    string Tittel,
    string HtmlInnhold,
    IReadOnlyList<HjelpRolle> Roller,
    IReadOnlyList<string> KontekstPrefikser,
    bool ErFaq,
    string Kategori);
