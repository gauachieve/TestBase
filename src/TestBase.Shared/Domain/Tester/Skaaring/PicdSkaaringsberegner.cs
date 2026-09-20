using TestBase.Shared.Domain.Tester.InnebygdeTester;

namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// PiCD-skåring: sum og gjennomsnitt per trekkdomene (se PicdTestSeeder.Domener,
/// 1-indeksert leddnummer). Kilden oppgir ingen kliniske grenseverdier — dette
/// er en dimensjonal trekkprofil, ikke et pass/fail-screeninginstrument, så
/// alle indikatorer er nøytralt fremstilt (Positiv=true, ingen alarmfarge).
/// </summary>
public sealed class PicdSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 300; // 60 ledd × 5 poeng

    public string TestKode => "picd";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var poengPerLedd = svar.Select(s => int.Parse(s.SvarVerdi)).ToList();
        var raaSkaar = poengPerLedd.Sum();
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var indikatorer = new List<TestSkaaringIndikator>();
        var domeneOppsummering = new List<string>();
        foreach (var (domene, leddnumre) in PicdTestSeeder.Domener)
        {
            var sum = leddnumre.Sum(n => poengPerLedd[n - 1]);
            var snitt = sum / (decimal)leddnumre.Length;
            indikatorer.Add(new TestSkaaringIndikator(domene, $"Snitt {snitt:0.0} (sum {sum}/60)", true));
            domeneOppsummering.Add($"{domene}: snitt {snitt:0.0}");
        }

        var fortolkning = $"Totalskår {raaSkaar}/{Maks}. Domeneprofil (gjennomsnitt 1-5): {string.Join(", ", domeneOppsummering)}. " +
                           "PiCD gir en dimensjonal trekkprofil, ikke en diagnose — kilden oppgir ingen kliniske grenseverdier.";

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
