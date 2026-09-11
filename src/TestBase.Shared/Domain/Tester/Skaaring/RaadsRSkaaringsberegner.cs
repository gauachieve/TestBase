using TestBase.Shared.Domain.Tester.InnebygdeTester;

namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// RAADS-R-skåring: 63 symptombaserte ledd skåres "Stemmer nå og da jeg var
/// ung"=3 … "Aldri stemt"=0; de 17 reverserte (normative) ledd skåres motsatt
/// (0…3). Totalskår 0-240. Cutoffs sitert direkte fra kildedokumentets
/// skåringsveiledning (Ritvo 2011: ≥65; Andersen et al. 2011: ≥72), se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\RAADS_R.md. Bruker
/// RaadsRTestSeeder.ErReversertPerLedd for å garantere at retning-per-ledd
/// aldri kommer ut av synk med selve spørsmålsteksten.
/// </summary>
public sealed class RaadsRSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 240;

    public string TestKode => "raads_r";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var reversert = RaadsRTestSeeder.ErReversertPerLedd;
        var raaSkaar = 0;

        for (var i = 0; i < svar.Count && i < reversert.Count; i++)
        {
            // Skalaverdi 1-4 (se RaadsRTestSeeder.Skala) -> poeng 0-3, reversert ved normative ledd.
            var skalaVerdi = int.Parse(svar[i].SvarVerdi);
            var poeng = reversert[i] ? skalaVerdi - 1 : 4 - skalaVerdi;
            raaSkaar += poeng;
        }

        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);
        var overRitvo = raaSkaar >= 65;
        var overAndersen = raaSkaar >= 72;

        var fortolkning = overRitvo
            ? $"Råskår {raaSkaar}/{Maks} — på eller over Ritvo (2011) sin grenseverdi (65) for holdepunkter om " +
              $"autismespekterforstyrrelse/Asperger syndrom{(overAndersen ? ", og på eller over Andersen et al. (2011) sin strengere grenseverdi (72)" : "")}. " +
              "Klinisk vurdering skal ha forrang ved uenighet med skåren."
            : $"Råskår {raaSkaar}/{Maks} — under grenseverdien (65). Ved klinisk mistanke om autismespekterforstyrrelse " +
              "skal likevel klinisk vurdering ha forrang.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Autismespekterforstyrrelse (Ritvo 2011, ≥65)", overRitvo ? "Over grenseverdi" : "Under grenseverdi", !overRitvo),
            new("Autismespekterforstyrrelse (Andersen 2011, ≥72)", overAndersen ? "Over grenseverdi" : "Under grenseverdi", !overAndersen)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
