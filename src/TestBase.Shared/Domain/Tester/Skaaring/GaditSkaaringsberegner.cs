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

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = 0;
        for (var i = 0; i < 6; i++)
        {
            var verdi = int.Parse(svar[i].SvarVerdi);
            raaSkaar += verdi >= 3 ? 1 : 0;
        }
        for (var i = 6; i < 8; i++)
        {
            raaSkaar += svar[i].SvarVerdi == "Ja" ? 1 : 0;
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
