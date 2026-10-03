namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>Ett delskala-punkt i én av MPFI-24-radarene, med X/Y allerede omregnet til SVG-koordinater.</summary>
public sealed record MpfiRadarPunkt(string Navn, double DataX, double DataY, double NormX, double NormY, double LabelX, double LabelY, string LabelAnker, double Skaare, double Norm);

/// <summary>Alt en SVG-visning trenger for å tegne ÉN av MPFI-24 sine to hexagon-radarer (fleksibilitet ELLER rigiditet) — se Rapport.cshtml (begge Areas).</summary>
public sealed record MpfiRadarData(
    IReadOnlyList<MpfiRadarPunkt> Punkter,
    string DataPolygonPunkter,
    string NormPolygonPunkter,
    string YtreRingPunkter,
    double Senter,
    double MaksRadius);

/// <summary>
/// Regner ut geometrien for TO separate 6-aksede radar-("spindelvev"-)diagrammer av MPFI-24 sine
/// delskalaer — ett for de 6 fleksibilitetsprosessene, ett for de 6 rigiditetsprosessene (se
/// Mpfi24Skaaringsberegner). Hver radar tegner BÅDE pasientens egen delskåre OG det
/// kjønnsspesifikke normtallet som en egen referanse-polygon, i MOTSETNING til
/// Sipp118RadarBeregner sin FASTE cutoff-ring — normen her VARIERER både per delskala og per
/// pasientens kjønn, så den må beregnes på nytt per rapport, ikke være en konstant.
///
/// Leser fra <see cref="TestSkaaring.Indikatorer"/>, format "skåre/norm" i Verdi-feltet (satt av
/// Mpfi24Skaaringsberegner) — IKKE samme betydning som Sipp118RadarBeregner sitt "verdi/maks", hver
/// radar-beregner tolker kun sin egen tests indikator-format. Skala-taket er FAST 6 (øvre grense på
/// MPFI-24 sin Likert-skala), ikke utledet fra normtallet, siden normen er et
/// sammenligningspunkt — ikke aksens faktiske tak.
/// </summary>
public static class MpfiRadarBeregner
{
    public const double Bredde = 320;
    public const double Hoyde = 320;
    private const double Senter = 160;
    private const double MaksRadius = 105;
    private const double LabelRadius = MaksRadius + 34;
    private const double SkalaTak = 6.0;

    /// <param name="indikatorer">De 6 delskala-indikatorene for ÉN gruppe (fleksibilitet ELLER rigiditet) — hopp over globalskåren, som ikke er en egen akse.</param>
    public static MpfiRadarData? Beregn(IReadOnlyList<TestSkaaringIndikator>? indikatorer, string navnPrefiks)
    {
        // "— global"-indikatoren (satt av Mpfi24Skaaringsberegner) deler SAMME prefiks som de 6
        // delskalaene, men er ikke en egen akse — ekskluderes eksplisitt, ellers blir det 7 treff
        // og { Count: 6 }-sjekken under feiler alltid.
        var delskalaer = indikatorer?
            .Where(i => i.Navn.StartsWith(navnPrefiks, StringComparison.Ordinal) && !i.Navn.EndsWith("global", StringComparison.Ordinal))
            .ToList();
        if (delskalaer is not { Count: 6 })
        {
            return null;
        }

        var antall = delskalaer.Count;
        var punkter = new List<MpfiRadarPunkt>();
        var dataPunkter = new List<string>();
        var normPunkter = new List<string>();
        var ytreRingPunkter = new List<string>();

        for (var i = 0; i < antall; i++)
        {
            var delene = delskalaer[i].Verdi.Split('/');
            if (delene.Length != 2
                || !double.TryParse(delene[0], System.Globalization.CultureInfo.InvariantCulture, out var skaare)
                || !double.TryParse(delene[1], System.Globalization.CultureInfo.InvariantCulture, out var norm))
            {
                return null;
            }

            var navn = delskalaer[i].Navn[navnPrefiks.Length..].TrimStart(' ', '—', '-');

            var vinkelGrader = -90.0 + i * (360.0 / antall);
            var vinkelRadianer = vinkelGrader * Math.PI / 180.0;
            var cosV = Math.Cos(vinkelRadianer);
            var sinV = Math.Sin(vinkelRadianer);

            var dataR = Math.Clamp(skaare / SkalaTak, 0, 1) * MaksRadius;
            var dataX = Senter + dataR * cosV;
            var dataY = Senter + dataR * sinV;

            var normR = Math.Clamp(norm / SkalaTak, 0, 1) * MaksRadius;
            var normX = Senter + normR * cosV;
            var normY = Senter + normR * sinV;

            var ytreX = Senter + MaksRadius * cosV;
            var ytreY = Senter + MaksRadius * sinV;

            var labelX = Senter + LabelRadius * cosV;
            var labelY = Senter + LabelRadius * sinV;
            var labelAnker = Math.Abs(cosV) < 0.15 ? "middle" : (cosV > 0 ? "start" : "end");

            punkter.Add(new MpfiRadarPunkt(navn, dataX, dataY, normX, normY, labelX, labelY, labelAnker, skaare, norm));
            dataPunkter.Add($"{F(dataX)},{F(dataY)}");
            normPunkter.Add($"{F(normX)},{F(normY)}");
            ytreRingPunkter.Add($"{F(ytreX)},{F(ytreY)}");
        }

        return new MpfiRadarData(
            punkter,
            string.Join(" ", dataPunkter),
            string.Join(" ", normPunkter),
            string.Join(" ", ytreRingPunkter),
            Senter, MaksRadius);
    }

    private static string F(double tall) => tall.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
