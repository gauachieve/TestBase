using TestBase.Shared.Domain.Tester.InnebygdeTester;

namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// WURS-skåring: summerer KUN de 25 leddene som utgjør WURS-25 (se
/// WursTestSeeder.Wurs25Posisjoner — identifisert ved innholdsmatching mot
/// Ward, Wender &amp; Reimherr 1993 sin offisielle engelske 25-item-liste,
/// IKKE bekreftet mot NK/OUS sin egen norske manual, som kun deles ut på
/// forespørsel til fagfolk). Skala 0-100, cutoff 46 ("predictive of
/// childhood ADHD", 86 % sensitivitet / 99 % spesifisitet ifølge samme
/// kilde) — se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\WURS.md.
/// </summary>
public sealed class WursSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 100;
    private const int Grenseverdi = 46;

    public string TestKode => "wurs";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = WursTestSeeder.Wurs25Posisjoner
            .Where(pos => pos < svar.Count)
            .Sum(pos => int.Parse(svar[pos].SvarVerdi));

        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);
        var overGrense = raaSkaar >= Grenseverdi;

        var fortolkning = overGrense
            ? $"WURS-25-skår {raaSkaar}/{Maks} — på eller over grenseverdien ({Grenseverdi}), som ifølge Ward, " +
              "Wender & Reimherr (1993) korrekt identifiserte 86 % av voksne med ADHD og 99 % av kontrollpersoner. " +
              "Denne utvelgelsen/cutoffen er hentet fra internasjonal litteratur, ikke bekreftet mot NK/OUS sin " +
              "egen manual for denne norske oversettelsen."
            : $"WURS-25-skår {raaSkaar}/{Maks} — under grenseverdien ({Grenseverdi}).";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("ADHD (retrospektiv barndom, WURS-25 ≥46)", overGrense ? "Over grenseverdi" : "Under grenseverdi", !overGrense)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
