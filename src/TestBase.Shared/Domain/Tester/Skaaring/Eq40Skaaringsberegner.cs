using TestBase.Shared.Domain.Tester.InnebygdeTester;

namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// EQ-40-skåring: for "enig-keyed" ledd gir Helt enig 2 poeng, Litt enig 1,
/// uenig-alternativene 0; for "uenig-keyed" ledd omvendt. Totalskår 0-40. Se
/// Eq40TestSeeder.ErEnigKeyed for retning per ledd (verifisert mot Baron-Cohen
/// &amp; Wheelwright 2004 sin offisielle nøkkel) og
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\EQ.md for full
/// begrunnelse. Ingen offisiell klinisk cutoff er oppgitt i det norske
/// kildedokumentet — fortolkningen er derfor bevisst beskrivende, ikke en
/// klinisk konklusjon.
/// </summary>
public sealed class Eq40Skaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 40;

    public string TestKode => "eq40";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var enigKeyed = Eq40TestSeeder.ErEnigKeyed;
        var raaSkaar = 0;

        for (var i = 0; i < svar.Count && i < enigKeyed.Count; i++)
        {
            var skalaVerdi = int.Parse(svar[i].SvarVerdi);
            var poeng = enigKeyed[i] ? Math.Max(0, skalaVerdi - 1) : Math.Max(0, 2 - skalaVerdi);
            raaSkaar += poeng;
        }

        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var fortolkning =
            $"Råskår {raaSkaar}/{Maks} — beskrivende mål på selvrapportert empati (høyere = mer empati). " +
            "Kildedokumentet oppgir ingen offisiell klinisk cutoff; skåringsnøkkelen (hvilke svar som gir poeng " +
            "per ledd) er matchet mot den engelske originalkilden, ikke bekreftet av rettighetshaver for denne " +
            "norske oversettelsen. Bruk sammen med klinisk vurdering, ikke alene.";

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning);
    }
}
