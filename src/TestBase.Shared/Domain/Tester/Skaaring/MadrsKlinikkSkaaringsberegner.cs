namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// MADRS klinikkversjon-skåring: sum av alle 10 ledd (0-6 hver, skala 0-60).
/// Alvorlighetsgrensene er en mye brukt, men omtrentlig konvensjon (jf. bl.a.
/// Snaith m.fl. 1986) — ingen absolutt fasit, se
/// <see cref="TestBase.Shared.Domain.Tester.InnebygdeTester.MadrsKlinikkTestSeeder"/>.
/// Ledd 10 (selvmordstanker) flagges alltid separat når besvart over 0,
/// UAVHENGIG av totalskår — samme prinsipp som CORE-A/SCL-25 sine
/// sikkerhetsindikatorer, siden ett enkelt bekymringsfullt svar krever klinisk
/// oppfølging uansett hvor lav totalskåren ellers er. Bruker
/// <see cref="ITestSkaaringsberegnerMedLedd"/> for å identifisere selvmords-
/// leddet via ekte TestLeddId (siste ledd på siden), ikke listeposisjon i
/// svar-listen — robust selv om ett eller flere andre ledd er hoppet over.
/// </summary>
public sealed class MadrsKlinikkSkaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int Maks = 60;

    public string TestKode => "madrs_klinikk";

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = new[]
    {
        new TestSkaaringGrenseverdi("Mild depresjon", 7),
        new TestSkaaringGrenseverdi("Moderat depresjon", 20),
        new TestSkaaringGrenseverdi("Alvorlig depresjon", 35)
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        BeregnSkaaringMedLedd(svar, Array.Empty<TestLedd>());

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var raaSkaar = svar.Sum(s => int.Parse(s.SvarVerdi));
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var (kategori, ingenDepresjon) = raaSkaar switch
        {
            <= 6 => ("ikke deprimert", true),
            <= 19 => ("lett deprimert", false),
            <= 34 => ("moderat deprimert", false),
            _ => ("alvorlig deprimert", false)
        };

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Alvorlighetsgrad", kategori, ingenDepresjon)
        };

        // Alle 10 ledd ligger på én og samme side, seedet i rekkefølge — det
        // ledd-ID-messig SISTE leddet er alltid "10. Selvmordstanker".
        var selvmordsledd = alleLedd.OrderBy(l => l.Id).LastOrDefault();

        if (selvmordsledd is not null)
        {
            var selvmordsverdi = svar.FirstOrDefault(s => s.TestLeddId == selvmordsledd.Id);
            if (selvmordsverdi is not null && int.Parse(selvmordsverdi.SvarVerdi) > 0)
            {
                indikatorer.Add(new TestSkaaringIndikator("Selvmordstanker", "Ledd 10 besvart over 0 — krever klinisk oppfølging uavhengig av totalskår", false));
            }
        }

        var fortolkning =
            $"Råskår {raaSkaar}/{Maks} — {kategori} (basert på en mye brukt, men omtrentlig konvensjon for " +
            "kliniker-administrert MADRS). Ingen absolutt \"sannhet\" for skjæringspunkter — klinisk skjønn er " +
            "alltid nødvendig, spesielt ved forhøyet selvmordsledd.";

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
