namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// CORE-10-skåring: sum 0-40 der ledd 1 og 5 (positivt formulert) REVERSE-
/// SKÅRES (4 - rå verdi) før summering — offisiell CORE-10-konvensjon.
/// Krever ekte TestLeddId-oppslag (ITestSkaaringsberegnerMedLedd) for å
/// identifisere HVILKE ledd som skal reverse-skåres og hvilke som er
/// risikoledd, UAVHENGIG av om et tidligere ledd ble hoppet over — samme
/// rotårsak-klasse som GADIT-krasjen, se docs/beslutningslogg.md. Cutoff ≥11
/// er den mye siterte kliniske grensen (Barkham et al./CORE System Trust).
/// Ledd 3 (selvskadingstanker) og ledd 10 ("livet ikke verdt å leve") flagges
/// ALLTID separat når besvart over laveste alternativ, uavhengig av totalskår.
/// </summary>
public sealed class Core10Skaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int AntallLedd = 10;
    private const int Maks = AntallLedd * 4;
    private const int KlinuskCutoff = 11;

    public string TestKode => "core10";

    public IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser { get; } = new[]
    {
        new TestSkaaringGrenseverdi("Klinisk grense", KlinuskCutoff)
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("CORE-10 krever ledd-informasjon for reverse-skåring/risikoledd — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);
        int RaVerdi(int indeks) =>
            indeks < alleLedd.Count && svarPerLeddId.TryGetValue(alleLedd[indeks].Id, out var v) && int.TryParse(v, out var tall) ? tall : 0;

        var reverserteIndekser = new HashSet<int> { 0, 4 }; // Ledd 1 og 5 (0-indeksert)
        var raaSkaar = 0;
        for (var i = 0; i < AntallLedd && i < alleLedd.Count; i++)
        {
            var verdi = RaVerdi(i);
            raaSkaar += reverserteIndekser.Contains(i) ? 4 - verdi : verdi;
        }

        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);
        var overGrense = raaSkaar >= KlinuskCutoff;

        var fortolkning =
            $"Sumskår {raaSkaar}/{Maks} — " +
            (overGrense ? $"OVER den kliniske grensen ({KlinuskCutoff}), som indikerer klinisk signifikant distress." : $"under den kliniske grensen ({KlinuskCutoff}).") +
            " CORE-10 er et bredt distressmål, ikke tilstrekkelig alene for å stille diagnose.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Klinisk signifikant distress", overGrense ? "Over grense" : "Under grense", !overGrense)
        };

        var selvskadingVerdi = RaVerdi(2); // Ledd 3
        if (selvskadingVerdi > 0)
        {
            indikatorer.Insert(0, new TestSkaaringIndikator("OBS: Selvskadingstanker (ledd 3)", $"Besvart {selvskadingVerdi}/4 — IKKE «Ikke i det hele tatt»", false));
        }

        var livetVerdtVerdi = RaVerdi(9); // Ledd 10
        if (livetVerdtVerdi > 0)
        {
            indikatorer.Insert(0, new TestSkaaringIndikator("OBS: «Livet ikke verdt å leve» (ledd 10)", $"Besvart {livetVerdtVerdi}/4 — IKKE «Ikke i det hele tatt»", false));
        }

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
