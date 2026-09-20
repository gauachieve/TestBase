namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// IDQ-skåring: diagnostisk algoritme sitert direkte fra kildeartikkelen
/// (se IdqTestSeeder) — ikke bare en terskelsum. Ledd 0-8 er symptomledd
/// (0-4), ledd 9 er funksjonsspørsmålet (Ja/Nei).
/// </summary>
public sealed class IdqSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 36; // 9 ledd × 4 poeng
    private const int MinstEndossertForDiagnose = 5;

    public string TestKode => "idq";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var symptomPoeng = Enumerable.Range(0, 9).Select(i => int.Parse(svar[i].SvarVerdi)).ToList();
        var raaSkaar = symptomPoeng.Sum();
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var antallEndossert = symptomPoeng.Count(p => p >= 3);
        var kjerneEndossert = symptomPoeng[0] >= 3 || symptomPoeng[1] >= 3;
        var funksjonstap = svar[9].SvarVerdi == "Ja";
        var kriterierOppfylt = antallEndossert >= MinstEndossertForDiagnose && kjerneEndossert && funksjonstap;

        var fortolkning = kriterierOppfylt
            ? $"Råskår {raaSkaar}/{Maks} — kriteriene for ICD-11 depressiv episode (6A70) er oppfylt ut fra denne " +
              "algoritmen (minst 5 ledd endossert, hvorav minst ett kjernesymptom, samt funksjonstap bekreftet)."
            : $"Råskår {raaSkaar}/{Maks} — kriteriene for ICD-11 depressiv episode er IKKE oppfylt ut fra denne " +
              "algoritmen. IDQ kan ikke alene brukes for å sette en diagnose; klinisk vurdering skal ha forrang.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("ICD-11 depressiv episode (6A70)", kriterierOppfylt ? "Kriterier oppfylt" : "Kriterier ikke oppfylt", !kriterierOppfylt)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
