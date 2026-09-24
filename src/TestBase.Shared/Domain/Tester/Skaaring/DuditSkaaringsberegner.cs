namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// DUDIT-skåring: enkel sum over alle 11 ledd (0-44) — trygt som ren
/// listesum, samme begrunnelse som AuditSkaaringsberegner. Cutoffs (Berman et
/// al. 2003) er KJØNNSAVHENGIGE (menn ≥6, kvinner ≥2 for mulig rusrelatert
/// problem; ≥25 for begge kjønn indikerer sannsynlig avhengighet uansett
/// kjønn) — skåringsberegneren har INGEN tilgang til pasientens kjønn (ren
/// funksjon på TestSvar alene), så begge kjønnsspesifikke grenser oppgis i
/// fortolkningsteksten i stedet for å hardkode én av dem inn i en enkelt
/// positiv/negativ-indikator.
/// </summary>
public sealed class DuditSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 44;

    public string TestKode => "dudit";

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = new[]
    {
        new TestSkaaringGrenseverdi("Kvinner: mulig problem", 2),
        new TestSkaaringGrenseverdi("Menn: mulig problem", 6),
        new TestSkaaringGrenseverdi("Sannsynlig avhengighet", 25)
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = svar.Sum(s => int.Parse(s.SvarVerdi));
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var sannsynligAvhengighet = raaSkaar >= 25;
        var fortolkning =
            $"Sumskår {raaSkaar}/{Maks}. Cutoff for mulig rusrelatert problem: ≥6 for menn, ≥2 for kvinner — " +
            "vurder opp mot pasientens kjønn. En skår ≥25 indikerer sannsynlig avhengighet uansett kjønn" +
            (sannsynligAvhengighet ? " — DENNE PASIENTEN er over 25." : ".") +
            " DUDIT er et screeningverktøy, ikke tilstrekkelig alene for å stille diagnose.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Sannsynlig avhengighet (≥25, uavhengig av kjønn)", sannsynligAvhengighet ? "Ja" : "Nei", !sannsynligAvhengighet)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
