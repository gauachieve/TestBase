namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// M.I.N.I.-strukturdemo-"skåring": REN opptelling av "Ja"-svar per modul (TestSideId), IKKE et
/// forsøk på å etterligne det ekte M.I.N.I. sitt proprietære diagnose-algoritme-/hoppelogikk-tre —
/// se <see cref="TestBase.Shared.Domain.Tester.InnebygdeTester.MiniStrukturdemoTestSeeder"/> for
/// hvorfor. Moduler grupperes etter TestSideId, sortert etter LAVESTE TestLeddId per gruppe (samme
/// robusthetsmønster som resten av natten). Enhver modul med minst ett "Ja"-svar flagges som
/// "mulig positiv screening" — en bevisst grov forenkling, ikke en diagnose.
/// </summary>
public sealed class MiniStrukturdemoSkaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    // Rekkefølgen MÅ matche Moduler-arrayet i MiniStrukturdemoTestSeeder (seedet i denne rekkefølgen).
    private static readonly string[] ModulNavn =
    {
        "Depressivt episode", "Suicidalitet", "(Hypo)manisk episode", "Panikklidelse",
        "Sosial fobi", "Tvangslidelse (OCD)", "PTSD", "Rusmiddelbruk",
        "Generalisert angstlidelse", "Psykotiske symptomer"
    };

    public string TestKode => "mini_strukturdemo";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("M.I.N.I.-strukturdemo krever ledd-informasjon for å gruppere ledd per modul — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);

        var moduler = alleLedd
            .GroupBy(l => l.TestSideId)
            .OrderBy(g => g.Min(l => l.Id))
            .Select(g => g.OrderBy(l => l.Id).ToList())
            .ToList();

        // Kun de FLAGGEDE modulene blir egne indikatorer (2026-09-27, etter brukerønske om å
        // "isolere de diagnosene som faktisk er trigget" i stedet for å vise alle 10 moduler —
        // de fleste med "0/N" — som like fremtredende badges. Navn+telling kombinert i selve
        // Verdi-strengen ("Depressivt episode (1/3)"), siden den kompakte badge-visningen i
        // Rapport.cshtml kun rendrer Indikator.Verdi, ikke Navn.
        var indikatorer = new List<TestSkaaringIndikator>();
        var totaltJa = 0;
        var totaltSporsmal = 0;

        for (var i = 0; i < moduler.Count; i++)
        {
            var modul = moduler[i];
            var antallJa = modul.Count(l => svarPerLeddId.TryGetValue(l.Id, out var v) && v == "Ja");
            totaltJa += antallJa;
            totaltSporsmal += modul.Count;

            var modulNavn = i < ModulNavn.Length ? ModulNavn[i] : $"Modul {i + 1}";
            if (antallJa > 0)
            {
                indikatorer.Add(new TestSkaaringIndikator(modulNavn, $"{modulNavn} ({antallJa}/{modul.Count})", false));
            }
        }

        var prosentSkaar = totaltSporsmal == 0 ? 0 : (int)Math.Round(totaltJa * 100m / totaltSporsmal);

        var fortolkning = indikatorer.Count > 0
            ? $"{indikatorer.Count} av {moduler.Count} moduler har minst ett \"Ja\"-svar, listet over (ren opptelling, IKKE en " +
              "diagnose eller det ekte M.I.N.I. sin diagnostiske algoritme). I en lisensiert versjon ville dette utløst " +
              "modulens fulle oppfølgingsspørsmål/skip-logic. Dette er en strukturdemo — ikke klinisk gyldig."
            : "Ingen moduler har noe \"Ja\"-svar i denne strukturdemoen (ren opptelling, ikke en diagnostisk konklusjon).";

        return new TestSkaaring(totaltJa, totaltSporsmal, prosentSkaar, fortolkning, indikatorer, SkjulProsent: true);
    }
}
