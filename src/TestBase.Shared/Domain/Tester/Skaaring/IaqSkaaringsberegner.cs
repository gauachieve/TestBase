namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// IAQ-skåring: diagnostisk algoritme sitert direkte fra kildeartikkelen
/// (se IaqTestSeeder) — ikke bare en terskelsum. Ledd 0-7 er symptomledd
/// (0-4), ledd 8 er funksjonsspørsmålet (Ja/Nei).
/// </summary>
public sealed class IaqSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 32; // 8 ledd × 4 poeng
    private const int MinstEndossertForDiagnose = 4;

    public string TestKode => "iaq";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var symptomPoeng = Enumerable.Range(0, 8).Select(i => int.Parse(svar[i].SvarVerdi)).ToList();
        var raaSkaar = symptomPoeng.Sum();
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var antallEndossert = symptomPoeng.Count(p => p >= 3);
        var kjerneEndossert = symptomPoeng[0] >= 3 || symptomPoeng[1] >= 3;
        var funksjonstap = svar[8].SvarVerdi == "Ja";
        var kriterierOppfylt = antallEndossert >= MinstEndossertForDiagnose && kjerneEndossert && funksjonstap;

        var fortolkning = kriterierOppfylt
            ? $"Råskår {raaSkaar}/{Maks} — kriteriene for ICD-11 generalisert angstlidelse er oppfylt ut fra denne " +
              "algoritmen (minst 4 ledd endossert, hvorav minst ett kjernesymptom, samt funksjonstap bekreftet)."
            : $"Råskår {raaSkaar}/{Maks} — kriteriene for ICD-11 generalisert angstlidelse er IKKE oppfylt ut fra " +
              "denne algoritmen. IAQ kan ikke alene brukes for å sette en diagnose; klinisk vurdering skal ha forrang.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("ICD-11 generalisert angstlidelse", kriterierOppfylt ? "Kriterier oppfylt" : "Kriterier ikke oppfylt", !kriterierOppfylt)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
