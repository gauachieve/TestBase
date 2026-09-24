namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// EDE-Q-skåring: 4 delskalaer (Restriksjon 5 ledd, Spisebekymring 5 ledd,
/// Figurbekymring 8 ledd, Vektbekymring 5 ledd — til sammen 23 skårede ledd,
/// pluss 5 ikke-skårede fritekst-atferdsspørsmål bakerst som ALDRI inngår).
/// Globalskår = gjennomsnittet av de 4 delskala-gjennomsnittene (offisiell
/// EDE-Q-konvensjon), IKKE et vektet snitt av enkeltledd. Krever ekte
/// TestLeddId-oppslag (ITestSkaaringsberegnerMedLedd) siden Restriksjon+
/// Spisebekymring deler samme skala, og Figurbekymring+Vektbekymring deler
/// en annen — se docs/beslutningslogg.md "Reell 500-feil i GADIT-skåring".
/// Cutoff ≥4 for globalskår er en mye sitert grense for klinisk signifikant
/// spiseforstyrrelsessymptomatologi (samfunnsnormer ligger typisk 1,5-2,5).
/// </summary>
public sealed class EdeqSkaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int AntallRestriksjon = 5;
    private const int AntallSpisebekymring = 5;
    private const int AntallFigurbekymring = 8;
    private const int AntallVektbekymring = 5;
    private const int AntallSkaaredeLedd = AntallRestriksjon + AntallSpisebekymring + AntallFigurbekymring + AntallVektbekymring;
    private const int Maks = AntallSkaaredeLedd * 6;
    private const decimal GlobalCutoff = 4.0m;

    public string TestKode => "edeq";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("EDE-Q krever ledd-informasjon for delskala-snitt — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);

        var restriksjonIds = alleLedd.Take(AntallRestriksjon).Select(l => l.Id).ToList();
        var spisebekymringIds = alleLedd.Skip(AntallRestriksjon).Take(AntallSpisebekymring).Select(l => l.Id).ToList();
        var figurbekymringIds = alleLedd.Skip(AntallRestriksjon + AntallSpisebekymring).Take(AntallFigurbekymring).Select(l => l.Id).ToList();
        var vektbekymringIds = alleLedd.Skip(AntallRestriksjon + AntallSpisebekymring + AntallFigurbekymring).Take(AntallVektbekymring).Select(l => l.Id).ToList();

        var restriksjonSnitt = Snitt(restriksjonIds, svarPerLeddId);
        var spisebekymringSnitt = Snitt(spisebekymringIds, svarPerLeddId);
        var figurbekymringSnitt = Snitt(figurbekymringIds, svarPerLeddId);
        var vektbekymringSnitt = Snitt(vektbekymringIds, svarPerLeddId);
        var globalSnitt = (restriksjonSnitt + spisebekymringSnitt + figurbekymringSnitt + vektbekymringSnitt) / 4m;

        var raaSkaar = restriksjonIds.Concat(spisebekymringIds).Concat(figurbekymringIds).Concat(vektbekymringIds)
            .Where(svarPerLeddId.ContainsKey)
            .Sum(id => int.Parse(svarPerLeddId[id]));
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var overGrense = globalSnitt >= GlobalCutoff;

        var fortolkning =
            $"Globalskår (snitt av 4 delskalaer) {globalSnitt:0.00} av 6 — " +
            (overGrense ? "OVER den mye siterte grensen 4,0, som indikerer klinisk signifikant spiseforstyrrelsessymptomatologi." : "under grensen 4,0.") +
            $" Delskalaer: Restriksjon {restriksjonSnitt:0.00}, Spisebekymring {spisebekymringSnitt:0.00}, " +
            $"Figurbekymring {figurbekymringSnitt:0.00}, Vektbekymring {vektbekymringSnitt:0.00}. " +
            "EDE-Q er et screeningverktøy, ikke tilstrekkelig alene for å stille diagnose.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Globalskår", overGrense ? "Over grense (≥4,0)" : "Under grense", !overGrense),
            new("Restriksjon (snitt)", restriksjonSnitt.ToString("0.00"), true),
            new("Spisebekymring (snitt)", spisebekymringSnitt.ToString("0.00"), true),
            new("Figurbekymring (snitt)", figurbekymringSnitt.ToString("0.00"), true),
            new("Vektbekymring (snitt)", vektbekymringSnitt.ToString("0.00"), true)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }

    private static decimal Snitt(IReadOnlyList<long> leddIder, IReadOnlyDictionary<long, string> svarPerLeddId)
    {
        var verdier = leddIder.Where(svarPerLeddId.ContainsKey).Select(id => int.Parse(svarPerLeddId[id])).ToList();
        return verdier.Count == 0 ? 0m : verdier.Sum() / (decimal)verdier.Count;
    }
}
