namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Faste (ikke kjøretids-genererte) fargeklasser per kategori, til rapportens
/// fargekoding (bugliste 2026-09-13 punkt 27) — én liste, alfabetisk etter
/// TestService.StandardKategorier, slik at fargene er stabile og forhåndsdefinerte
/// i stedet for hashet ved kjøretid (som ville gitt tilfeldige, potensielt
/// grelle/utydelige farger).
/// </summary>
public static class TestKategoriFarge
{
    private static readonly IReadOnlyDictionary<string, string> SlugPerKategori = new Dictionary<string, string>
    {
        ["Kognisjon, demens og nevropsykologisk screening"] = "kognisjon",
        ["ADHD, autisme og nevroutvikling"] = "adhd",
        ["Søvn og døgnrytme"] = "sovn",
        ["Rus og avhengighet"] = "rus",
        ["Spiseforstyrrelser og kroppsbilde"] = "spiseforstyrrelser",
        ["Traumer, dissosiasjon og belastninger"] = "traumer",
        ["Angst, tvang og relaterte plager"] = "angst",
        ["Depresjon og bipolaritet"] = "depresjon",
        ["Psykose og alvorlige psykiske lidelser"] = "psykose",
        ["Personlighet, relasjoner og sosial fungering"] = "personlighet",
        ["Vold, selvmord og risikovurdering"] = "vold",
        ["Seksuell helse og kjønn"] = "seksuell",
        ["Barn og unges psykiske helse – generelt"] = "barn",
        ["Funksjon, livskvalitet og behandlingsutfall"] = "funksjon",
        ["Somatiske symptomer, smerte og utmattelse"] = "somatisk",
        ["Diagnostikk, tverrgående og øvrige verktøy"] = "diagnostikk"
    };

    /// <summary>CSS-klassenavn ("kategori-adhd" osv.) eller null hvis kategorien ikke er en av standardkategoriene.</summary>
    public static string? CssKlasse(string? kategoriNavn) =>
        kategoriNavn is not null && SlugPerKategori.TryGetValue(kategoriNavn, out var slug) ? $"kategori-{slug}" : null;
}
