namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Offisiell WHO-5-skåring, jf. "VEILEDNING I BRUK AV WHO 5-WBQ" (Bakke,
/// 2004, norsk oversettelse publisert av WHO): råskår = sum av de fem svarene
/// (0–25), prosentskår = råskår × 4 (0–100). Nærmere undersøkelse anbefales
/// hvis råskår &lt; 13 ELLER pasienten har svart 0 eller 1 på noe
/// enkeltspørsmål. En endring i prosentskår over tid på 10 % regnes som
/// signifikant (ref. John Ware, 1996, sitert i samme veiledning).
///
/// De to indikatorene ("Velvære"/"Depresjon") er BEGGE avledet av samme
/// grenseverdi (råskår 13, jf. over) — WHO-5s prosentskår kan kun ta verdiene
/// 0/4/8/…/100 (råskår×4), så det finnes ingen mellomliggende skår som ville
/// gitt de to indikatorene ulikt utfall. De er bevisst separate, navngitte
/// verdier likevel (ikke duplikat tekst) fordi de svarer på to ulike kliniske
/// spørsmål — "har pasienten god velvære?" vs. "bør man gå videre med
/// depresjonsutredning?" — som i praksis alltid følger hverandre for denne
/// testen, men konseptuelt er forskjellige.
/// </summary>
public sealed class Who5Skaaringsberegner : ITestSkaaringsberegner
{
    public const int EndringSignifikantProsent = 10;
    private const int Grenseverdi = 13;

    public string TestKode => "who5";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var verdier = svar.Select(s => int.Parse(s.SvarVerdi)).ToList();
        var raaSkaar = verdier.Sum();
        var prosentSkaar = raaSkaar * 4;
        var lavtEnkeltsvar = verdier.Any(v => v <= 1);
        var trengerUtredning = raaSkaar < Grenseverdi || lavtEnkeltsvar;

        var fortolkning = trengerUtredning
            ? $"Råskår {raaSkaar}/25 (prosentskår {prosentSkaar}). Under grenseverdien ({Grenseverdi}) " +
              "eller lav skår på ett eller flere enkeltspørsmål — WHO-5-veiledningen anbefaler " +
              "å gå videre med nærmere undersøkelse (f.eks. diagnostiske kriterier for depressiv episode)."
            : $"Råskår {raaSkaar}/25 (prosentskår {prosentSkaar}). Over grenseverdien — " +
              "WHO-5-veiledningen anbefaler ikke å gå videre med nærmere undersøkelse på bakgrunn av denne testen alene.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Velvære", raaSkaar >= Grenseverdi ? "Velvære" : "Ikke velvære", raaSkaar >= Grenseverdi),
            new("Depresjon", trengerUtredning ? "Indikerer depresjon" : "Indikerer ikke depresjon", !trengerUtredning)
        };

        return new TestSkaaring(raaSkaar, 25, prosentSkaar, fortolkning, indikatorer);
    }
}
