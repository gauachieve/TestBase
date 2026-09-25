namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// YGTSS-R-skåring: motorisk delskår = sum av de 5 alvorlighetsdimensjonene
/// for motoriske tics (0-25), fonatorisk delskår tilsvarende (0-25), total
/// tic-alvorlighet = motorisk + fonatorisk (0-50), pluss funksjonsnedsettelse
/// (0-50) lagt til for en total YGTSS-skår (0-100). Sjekklistene (ikke
/// skåret) kan ha ULIKT antall avkryssede ledd fra pasient til pasient — de 5
/// rangeringsleddene identifiseres derfor via ekte TestLeddId-oppslag
/// (ITestSkaaringsberegnerMedLedd), ikke listeposisjon i selve svarlisten,
/// se docs/beslutningslogg.md "Reell 500-feil i GADIT-skåring".
/// </summary>
public sealed class YgtssRSkaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int AntallMotoriskSjekkliste = 10;
    private const int AntallFonatoriskSjekkliste = 8;
    private const int AntallDimensjoner = 5;
    private const int MaksPerDelskaar = AntallDimensjoner * 5; // 25
    private const int MaksFunksjon = 50;
    private const int Maks = MaksPerDelskaar * 2 + MaksFunksjon; // 100

    public string TestKode => "ygtss_r";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("YGTSS-R krever ledd-informasjon for å skille sjekkliste fra rangeringsledd — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);
        int Verdi(TestLedd ledd) =>
            svarPerLeddId.TryGetValue(ledd.Id, out var v) && int.TryParse(v, out var tall) ? tall : 0;

        var motoriskRatingIds = alleLedd.Skip(AntallMotoriskSjekkliste).Take(AntallDimensjoner).ToList();
        var fonatoriskRatingIds = alleLedd.Skip(AntallMotoriskSjekkliste + AntallDimensjoner + AntallFonatoriskSjekkliste).Take(AntallDimensjoner).ToList();
        var funksjonLedd = alleLedd.Skip(AntallMotoriskSjekkliste + AntallDimensjoner + AntallFonatoriskSjekkliste + AntallDimensjoner).Take(1).FirstOrDefault();

        var motoriskSum = motoriskRatingIds.Sum(Verdi);
        var fonatoriskSum = fonatoriskRatingIds.Sum(Verdi);
        var ticAlvorlighet = motoriskSum + fonatoriskSum;
        var funksjonsverdi = funksjonLedd is null ? 0 : Verdi(funksjonLedd);
        var totalSkaar = ticAlvorlighet + funksjonsverdi;
        var prosentSkaar = (int)Math.Round(totalSkaar * 100m / Maks);

        var fortolkning =
            $"Total YGTSS-skår {totalSkaar}/{Maks} (tic-alvorlighet {ticAlvorlighet}/{MaksPerDelskaar * 2} + " +
            $"funksjonsnedsettelse {funksjonsverdi}/{MaksFunksjon}). Motorisk delskår {motoriskSum}/{MaksPerDelskaar}, " +
            $"fonatorisk delskår {fonatoriskSum}/{MaksPerDelskaar}. Klinisk vurdering (ikke bare tallskåren) " +
            "avgjør alvorlighetsgrad og behandlingsbehov.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Motorisk delskår", $"{motoriskSum}/{MaksPerDelskaar}", motoriskSum < MaksPerDelskaar / 2),
            new("Fonatorisk delskår", $"{fonatoriskSum}/{MaksPerDelskaar}", fonatoriskSum < MaksPerDelskaar / 2),
            new("Funksjonsnedsettelse", $"{funksjonsverdi}/{MaksFunksjon}", funksjonsverdi < MaksFunksjon / 2)
        };

        return new TestSkaaring(totalSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
