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

        var indikatorer = new List<TestSkaaringIndikator>();
        var flaggedeModuler = new List<string>();
        var totaltJa = 0;
        var totaltSporsmal = 0;

        for (var i = 0; i < moduler.Count; i++)
        {
            var modul = moduler[i];
            var antallJa = modul.Count(l => svarPerLeddId.TryGetValue(l.Id, out var v) && v == "Ja");
            totaltJa += antallJa;
            totaltSporsmal += modul.Count;

            var modulNavn = i < ModulNavn.Length ? ModulNavn[i] : $"Modul {i + 1}";
            var flagget = antallJa > 0;
            if (flagget)
            {
                flaggedeModuler.Add(modulNavn);
            }

            indikatorer.Add(new TestSkaaringIndikator(modulNavn, $"{antallJa}/{modul.Count} \"Ja\"", !flagget));
        }

        var prosentSkaar = totaltSporsmal == 0 ? 0 : (int)Math.Round(totaltJa * 100m / totaltSporsmal);

        var fortolkning = flaggedeModuler.Count > 0
            ? $"{flaggedeModuler.Count} av {moduler.Count} moduler har minst ett \"Ja\"-svar (ren opptelling, IKKE en diagnose eller " +
              "det ekte M.I.N.I. sin diagnostiske algoritme). I en lisensiert versjon ville dette utløst modulens fulle " +
              "oppfølgingsspørsmål/skip-logic. Dette er en strukturdemo — ikke klinisk gyldig."
            : "Ingen moduler har noe \"Ja\"-svar i denne strukturdemoen (ren opptelling, ikke en diagnostisk konklusjon).";

        return new TestSkaaring(totaltJa, totaltSporsmal, prosentSkaar, fortolkning, indikatorer);
    }
}
