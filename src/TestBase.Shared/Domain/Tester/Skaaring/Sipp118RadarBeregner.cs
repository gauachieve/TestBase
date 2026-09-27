namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>Ett domene-punkt i radar-grafen, med X/Y allerede omregnet til SVG-koordinater.</summary>
public sealed record Sipp118RadarPunkt(string Navn, double X, double Y, double LabelX, double LabelY, string LabelAnker, int Verdi, int Maks);

/// <summary>Alt en SVG-visning trenger for å tegne SIPP-118-radaren — se Rapport.cshtml (begge Areas).</summary>
public sealed record Sipp118RadarData(
    IReadOnlyList<Sipp118RadarPunkt> Punkter,
    string DataPolygonPunkter,
    string CutoffPolygonPunkter,
    string YtreRingPunkter,
    double Senter,
    double MaksRadius);

/// <summary>
/// Regner ut geometrien for et 5-akset radar-("spindelvev"-)diagram av den forenklede
/// SIPP-118-inspirerte testens domenepoeng, PER BESVARELSE — se brukerens ønske om at denne
/// grafen "ikke kan vise data over tid" (i motsetning til _UtviklingsGraf.cshtml), siden hvert
/// domene er en egen dimensjon, ikke en enkelt skår som endrer seg med tiden. Leser domene-
/// verdiene fra <see cref="TestSkaaring.Indikatorer"/> (format "X/Y" i Verdi-feltet, satt av
/// Sipp118Skaaringsberegner — indikator 0 er det samlede "Samlet personlighetsfunksjon"-flagget
/// og hoppes over). Bevisst egen, kraftig forenklet visning av de 5 domenene denne testen faktisk
/// måler — IKKE et forsøk på å gjengi et radar av de 16 ekte SIPP-118-fasettene, som denne
/// forenklede testen ikke skiller mellom (se Sipp118TestSeeder og
/// docs/beslutningslogg.md "Natt-økt, del 16" for kildehenvisning:
/// https://pmc.ncbi.nlm.nih.gov/articles/PMC12287623/).
/// </summary>
public static class Sipp118RadarBeregner
{
    public const double Bredde = 320;
    public const double Hoyde = 320;
    private const double Senter = 160;
    private const double MaksRadius = 105;
    private const double LabelRadius = MaksRadius + 28;

    /// <summary>Samme forenklede lavfunksjon-grense som Sipp118Skaaringsberegner (snitt ≤2,5 av skala 1-4 → 62,5 % av maks).</summary>
    private const double CutoffAndelAvMaks = 2.5 / 4.0;

    public static Sipp118RadarData? Beregn(IReadOnlyList<TestSkaaringIndikator>? indikatorer)
    {
        // Indikator 0 er "Samlet personlighetsfunksjon" (ikke et domene) — de neste 5 er domenene,
        // i FAST rekkefølge satt av Sipp118Skaaringsberegner (Selvkontroll, Identitetsintegrasjon,
        // Relasjonell kapasitet, Ansvarlighet, Sosial harmoni).
        var domener = indikatorer?.Skip(1).ToList();
        if (domener is not { Count: 5 })
        {
            return null;
        }

        var antall = domener.Count;
        var punkter = new List<Sipp118RadarPunkt>();
        var dataPunkter = new List<string>();
        var cutoffPunkter = new List<string>();
        var ytreRingPunkter = new List<string>();

        for (var i = 0; i < antall; i++)
        {
            var delene = domener[i].Verdi.Split('/');
            if (delene.Length != 2 || !int.TryParse(delene[0], out var verdi) || !int.TryParse(delene[1], out var maks) || maks <= 0)
            {
                return null;
            }

            // Start øverst (-90°), med klokka, jevnt fordelt over de 5 aksene.
            var vinkelGrader = -90.0 + i * (360.0 / antall);
            var vinkelRadianer = vinkelGrader * Math.PI / 180.0;
            var cosV = Math.Cos(vinkelRadianer);
            var sinV = Math.Sin(vinkelRadianer);

            var dataR = verdi / (double)maks * MaksRadius;
            var dataX = Senter + dataR * cosV;
            var dataY = Senter + dataR * sinV;

            var cutoffR = MaksRadius * CutoffAndelAvMaks;
            var cutoffX = Senter + cutoffR * cosV;
            var cutoffY = Senter + cutoffR * sinV;

            var ytreX = Senter + MaksRadius * cosV;
            var ytreY = Senter + MaksRadius * sinV;

            var labelX = Senter + LabelRadius * cosV;
            var labelY = Senter + LabelRadius * sinV;
            var labelAnker = Math.Abs(cosV) < 0.15 ? "middle" : (cosV > 0 ? "start" : "end");

            punkter.Add(new Sipp118RadarPunkt(domener[i].Navn, dataX, dataY, labelX, labelY, labelAnker, verdi, maks));
            dataPunkter.Add($"{F(dataX)},{F(dataY)}");
            cutoffPunkter.Add($"{F(cutoffX)},{F(cutoffY)}");
            ytreRingPunkter.Add($"{F(ytreX)},{F(ytreY)}");
        }

        return new Sipp118RadarData(
            punkter,
            string.Join(" ", dataPunkter),
            string.Join(" ", cutoffPunkter),
            string.Join(" ", ytreRingPunkter),
            Senter, MaksRadius);
    }

    private static string F(double tall) => tall.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
