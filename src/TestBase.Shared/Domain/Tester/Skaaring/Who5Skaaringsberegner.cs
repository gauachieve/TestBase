namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Offisiell WHO-5-skåring, jf. "VEILEDNING I BRUK AV WHO 5-WBQ" (Bakke,
/// 2004, norsk oversettelse publisert av WHO): råskår = sum av de fem svarene
/// (0–25), prosentskår = råskår × 4 (0–100). Nærmere undersøkelse anbefales
/// hvis råskår &lt; 13 ELLER pasienten har svart 0 eller 1 på noe
/// enkeltspørsmål. En endring i prosentskår over tid på 10 % regnes som
/// signifikant (ref. John Ware, 1996, sitert i samme veiledning).
/// </summary>
public sealed class Who5Skaaringsberegner : ITestSkaaringsberegner
{
    public const int EndringSignifikantProsent = 10;

    public string TestKode => "who5";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var verdier = svar.Select(s => int.Parse(s.SvarVerdi)).ToList();
        var raaSkaar = verdier.Sum();
        var prosentSkaar = raaSkaar * 4;
        var lavtEnkeltsvar = verdier.Any(v => v <= 1);

        var fortolkning = raaSkaar < 13 || lavtEnkeltsvar
            ? $"Råskår {raaSkaar}/25 (prosentskår {prosentSkaar}). Under grenseverdien (13) " +
              "eller lav skår på ett eller flere enkeltspørsmål — WHO-5-veiledningen anbefaler " +
              "å gå videre med nærmere undersøkelse (f.eks. diagnostiske kriterier for depressiv episode)."
            : $"Råskår {raaSkaar}/25 (prosentskår {prosentSkaar}). Over grenseverdien — " +
              "indikerer ikke i seg selv behov for videre undersøkelse.";

        return new TestSkaaring(raaSkaar, 25, prosentSkaar, fortolkning);
    }
}
