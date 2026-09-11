namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// PHQ-9-skåring: sum av de 9 første ledd (0-3 hver, skala 0-27) — DET 10.
/// LEDDET (funksjonsspørsmålet, "i hvor stor grad har problemene gjort det
/// vanskelig...") teller IKKE med, se Phq9TestSeeder. Krever at
/// TestService.BeregnSkaaringAsync leverer svar sortert etter (side, ledd)
/// Rekkefolge, IKKE databasens tilfeldige rekkefølge — se der.
///
/// Cutoffs (5/10/15/20 = mild/moderat/moderat til alvorlig/alvorlig) er
/// allment kjente, mye siterte PHQ-9-grenseverdier (Kroenke, Spitzer &amp;
/// Williams, 2001) — IKKE hentet fra selve det norske skjemaet (som manglet
/// interpretasjonstekst), se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\PHQ9.md.
/// </summary>
public sealed class Phq9Skaaringsberegner : ITestSkaaringsberegner
{
    private const int AntallSkaaredeLedd = 9;
    private const int Maks = AntallSkaaredeLedd * 3;

    public string TestKode => "phq9";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = svar.Take(AntallSkaaredeLedd).Sum(s => int.Parse(s.SvarVerdi));
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var (kategori, grad) = raaSkaar switch
        {
            <= 4 => ("ingen/minimal", 0),
            <= 9 => ("mild", 1),
            <= 14 => ("moderat", 2),
            <= 19 => ("moderat til alvorlig", 3),
            _ => ("alvorlig", 4)
        };

        var fortolkning =
            $"Råskår {raaSkaar}/{Maks} — {kategori} grad av depressive symptomer (basert på allment kjente " +
            "PHQ-9-cutoffs: 0-4 ingen/minimal, 5-9 mild, 10-14 moderat, 15-19 moderat til alvorlig, 20-27 alvorlig). " +
            "PHQ-9 er et kartleggingsverktøy, ikke tilstrekkelig alene for å stille diagnose.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Alvorlighetsgrad", kategori, grad == 0)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
