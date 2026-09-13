namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// IPDS-skåring: antall "Ja"-svar (0-11). Cutoff &gt;4 (sensitivitet 77 %,
/// spesifisitet 71 % ifølge OUS sin bakgrundsside for instrumentet) — IKKE
/// hentet direkte fra selve spørreskjema-PDF-en, som manglet skåringstekst,
/// se C:\Users\gaute\AppData\Local\Temp\claude\test-research\IPDS.md.
///
/// Ledd→klynge-indikatorer (bugliste 2026-09-13 punkt 28): originalen (Pfohl
/// &amp; Langbehn 1999) har ETT ledd per DSM-IV personlighetsforstyrrelse.
/// Koblingen under er hentet fra Table 1 i Ottosson m.fl., "A cross-sectional
/// testing of The Iowa Personality Disorder Screen in a psychiatric outpatient
/// setting" (PMC3151206, https://pmc.ncbi.nlm.nih.gov/articles/PMC3151206/),
/// og verifisert ved direkte innholdsmatching ledd for ledd mot den norske
/// oversettelsen i IpdsTestSeeder.cs (alle 11 ledd fant et entydig treff i
/// samme rekkefølge) — samme metode og samme forbehold som EQ40/WURS denne
/// uken: litteraturbasert og innholdsmatchet, IKKE bekreftet av Pfohl/Langbehn
/// eller OUS selv for denne spesifikke norske oversettelsen.
/// </summary>
public sealed class IpdsSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Grenseverdi = 4;
    private const int Maks = 11;

    public string TestKode => "ipds";

    /// <summary>0-basert leddindeks → hvilken DSM-IV-personlighetsforstyrrelse leddet er hentet fra, se klassekommentaren.</summary>
    private static readonly IReadOnlyList<string> KlyngePerLedd = new[]
    {
        "Emosjonelt ustabil PF (borderline type)",
        "Histrionisk PF",
        "Histrionisk PF",
        "Paranoid PF",
        "Unnvikende PF",
        "Unnvikende PF",
        "Emosjonelt ustabil PF (borderline type)",
        "Narsissistisk PF",
        "Paranoid PF",
        "Paranoid PF",
        "Narsissistisk PF"
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = svar.Count(s => s.SvarVerdi == "Ja");
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);
        var overGrense = raaSkaar > Grenseverdi;

        var fortolkning = overGrense
            ? $"Råskår {raaSkaar}/{Maks} — over grenseverdien ({Grenseverdi}), som ifølge validering gir " +
              "sensitivitet 77 % og spesifisitet 71 % for personlighetsforstyrrelse. Anbefaler videre utredning."
            : $"Råskår {raaSkaar}/{Maks} — under grenseverdien ({Grenseverdi}). Lavere sannsynlighet for " +
              "personlighetsforstyrrelse basert på denne screeningen alene.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Personlighetsforstyrrelse", overGrense ? "Over grenseverdi" : "Under grenseverdi", !overGrense)
        };

        // Grupper Ja-besvarte ledd per klynge — "Indikerer: <klynge> - Ledd <n,m>" (1-indekserte
        // leddnummer, slik en behandler naturlig ville referere til spørreskjemaet).
        var jaLeddPerKlynge = svar
            .Select((s, i) => (Ledd: i + 1, ErJa: s.SvarVerdi == "Ja", Klynge: i < KlyngePerLedd.Count ? KlyngePerLedd[i] : null))
            .Where(x => x.ErJa && x.Klynge is not null)
            .GroupBy(x => x.Klynge!)
            .OrderBy(g => g.Key);

        foreach (var gruppe in jaLeddPerKlynge)
        {
            var leddliste = string.Join(",", gruppe.Select(x => x.Ledd));
            indikatorer.Add(new TestSkaaringIndikator(
                "Mulig personlighetsklynge (uverifisert)", $"Indikerer: {gruppe.Key} – Ledd {leddliste}", Positiv: false));
        }

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
