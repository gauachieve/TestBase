namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// IPDS-skåring: antall "Ja"-svar (0-11). Cutoff &gt;4 (sensitivitet 77 %,
/// spesifisitet 71 % ifølge OUS sin bakgrundsside for instrumentet) — IKKE
/// hentet direkte fra selve spørreskjema-PDF-en, som manglet skåringstekst,
/// se C:\Users\gaute\AppData\Local\Temp\claude\test-research\IPDS.md.
/// </summary>
public sealed class IpdsSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Grenseverdi = 4;
    private const int Maks = 11;

    public string TestKode => "ipds";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = svar.Count(s => s.SvarVerdi == "Ja");
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);
        var overGrense = raaSkaar > Grenseverdi;

        var fortolkning = overGrense
            ? $"Råskår {raaSkaar}/{Maks} — over grenseverdien ({Grenseverdi}), som ifølge validering gir " +
              "sensitivitet 77 % og spesifisitet 71 % for personlighetsforstyrrelse. Anbefaler videre utredning."
            : $"Råskår {raaSkaar}/{Maks} — under grenseverdien ({Grenseverdi}). Lavere sannsynlighet for " +
              "personlighetsforstyrrelse basert på denne screeningen alene.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Personlighetsforstyrrelse", overGrense ? "Over grenseverdi" : "Under grenseverdi", !overGrense)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
