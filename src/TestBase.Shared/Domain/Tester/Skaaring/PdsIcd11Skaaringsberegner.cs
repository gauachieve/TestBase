namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// PDS-ICD-11-skåring (selvrapportering): ledd 1-10 lagrer VISNINGSINDEKS
/// (0..4), som oversettes til poeng 2-1-0-1-2 (bipolar); ledd 11-14 lagrer
/// indeks (0..3) som er identisk med poengverdien. Se PdsIcd11TestSeeder for
/// hvorfor indeks lagres i stedet for endelig poengverdi, og for
/// grenseverdi-/siteringskilder.
/// </summary>
public sealed class PdsIcd11Skaaringsberegner : ITestSkaaringsberegner
{
    private static readonly int[] PoengForBipolarIndeks = { 2, 1, 0, 1, 2 };
    private const int Maks = 32;

    public string TestKode => "pds_icd11";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = 0;
        for (var i = 0; i < 10; i++)
        {
            var indeks = int.Parse(svar[i].SvarVerdi);
            raaSkaar += PoengForBipolarIndeks[indeks];
        }
        for (var i = 10; i < 14; i++)
        {
            raaSkaar += int.Parse(svar[i].SvarVerdi);
        }

        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        string alvorlighet;
        bool positivIndikator;
        if (raaSkaar >= 20)
        {
            alvorlighet = "svært alvorlige vansker";
            positivIndikator = false;
        }
        else if (raaSkaar >= 16)
        {
            alvorlighet = "moderate til alvorlige vansker";
            positivIndikator = false;
        }
        else if (raaSkaar >= 12)
        {
            alvorlighet = "milde til moderate vansker";
            positivIndikator = false;
        }
        else
        {
            alvorlighet = "ingen holdepunkter for betydelig personlighetsproblematikk ut fra denne grenseverdien";
            positivIndikator = true;
        }

        var fortolkning = $"Råskår {raaSkaar}/{Maks}, som ut fra Bo Bachs foreslåtte grenseverdier (foredrag sept. 2022) " +
                           $"indikerer {alvorlighet}. PDS-ICD-11 kan ikke alene brukes som grunnlag for å sette en " +
                           "personlighetsforstyrrelse-diagnose, men kan gi nyttige indikasjoner for videre utforsking. " +
                           "Klinisk vurdering skal alltid ha forrang.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Alvorlighetsgrad (Bach 2022)", alvorlighet, positivIndikator)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
