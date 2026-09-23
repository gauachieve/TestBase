namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// GADIT-skåring: ledd 0-5 (frekvensspørsmål 1-6) skåres 1 hvis svaret er
/// "Hver dag" (verdi 4) eller "De fleste dager" (verdi 3), ellers 0. Ledd 6-7
/// (Ja/Nei-spørsmål 7-8) skåres 1 for "Ja", 0 for "Nei". Cutoff ≥5 sitert
/// direkte fra kildens skåringsveiledning (Chan et al., 2024) — se
/// GaditTestSeeder.
/// </summary>
public sealed class GaditSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 8;
    private const int Cutoff = 5;

    public string TestKode => "gadit";

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = new[]
    {
        new TestSkaaringGrenseverdi("Grenseverdi", Cutoff)
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        // Klassifiserer HVERT svar etter sin egen VERDI (tall vs. "Ja"/"Nei"),
        // ikke etter POSISJON i listen — `TestService.LagreSvarAsync` hopper
        // stille over tomme/uendrede felt (aldri en TestSvar-rad for et
        // ubesvart ledd), og pasienten kan likevel markere testen "Fullfort"
        // uten at alle 8 ledd er besvart. En posisjonsbasert antagelse
        // (ledd 0-5 = frekvens, 6-7 = Ja/Nei) forskyves da og feiler med
        // "input string was not in a correct format" på en helt vanlig,
        // ekte besvarelse — oppdaget 2026-09-23 på en reell konferanse-
        // rapportgenerering, se docs/beslutningslogg.md. Et ubesvart ledd
        // bidrar naturlig med 0 siden det rett og slett ikke finnes i `svar`.
        var raaSkaar = 0;
        foreach (var s in svar)
        {
            if (int.TryParse(s.SvarVerdi, out var frekvensverdi))
            {
                raaSkaar += frekvensverdi >= 3 ? 1 : 0;
            }
            else if (s.SvarVerdi == "Ja")
            {
                raaSkaar += 1;
            }
        }

        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);
        var overCutoff = raaSkaar >= Cutoff;

        var fortolkning = overCutoff
            ? $"Råskår {raaSkaar}/{Maks} — på eller over grenseverdien ({Cutoff}) som antyder at kriteriene for " +
              "ICD-11 Gaming Disorder (6C51) er oppfylt. GADIT kan ikke alene brukes for å sette en diagnose."
            : $"Råskår {raaSkaar}/{Maks} — under grenseverdien ({Cutoff}). Ved klinisk mistanke om problemfylt " +
              "gaming skal likevel klinisk vurdering ha forrang.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("ICD-11 Gaming Disorder (Chan 2024, ≥5)", overCutoff ? "Over grenseverdi" : "Under grenseverdi", !overCutoff)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
