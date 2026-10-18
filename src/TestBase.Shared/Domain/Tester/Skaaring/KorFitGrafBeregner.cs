namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>Hvilken av de to KOR/FIT-instrumentene ett punkt i grafen tilhører — styrer farge/serie i _KorFitGraf.cshtml.</summary>
public enum KorFitSerie
{
    Ors,
    Srs
}

/// <summary>
/// Ett datapunkt i den kombinerte KOR/FIT-grafen, allerede omregnet til SVG-koordinater — se
/// KorFitGrafBeregner.Beregn. <paramref name="ErSamtaleTid"/> (2026-10-18, brukerens spesifikasjon)
/// er sann når råskåren lander STRENGT mellom ORS- og SRS-cutoffen (uavhengig av hvilken serie
/// punktet selv tilhører) — rendres større og rødt i _KorFitGraf.cshtml, et visuelt varsel om at
/// skåren ligger i en gråsone som bør tas opp i neste time.
/// </summary>
public sealed record KorFitGrafPunkt(double X, double Y, string DatoKort, int RaaSkaar, KorFitSerie Serie, bool ErSamtaleTid, bool VisDatoLabel);

/// <summary>Alt en SVG-visning trenger for å tegne den kombinerte KOR/FIT-grafen — se Pages/Shared/_KorFitGraf.cshtml.</summary>
public sealed record KorFitGrafData(
    IReadOnlyList<KorFitGrafPunkt> OrsPunkter,
    IReadOnlyList<KorFitGrafPunkt> SrsPunkter,
    string OrsPolylinePunkter,
    string SrsPolylinePunkter,
    double OrsCutoffY,
    double SrsCutoffY,
    double PlotVenstre,
    double PlotHoyre,
    double PlotTopp,
    double PlotBunn);

/// <summary>
/// Regner ut geometrien for den kombinerte KOR/FIT-grafen (ORS + SRS i ÉN graf, råskår 0-40 på
/// Y-aksen — IKKE prosent, se OrsSkaaringsberegner/SrsSkaaringsberegner sin klassekommentar for
/// hvorfor) — bevisst EGEN, purpose-built graf, ikke en utvidelse av den generiske
/// UtviklingsGrafBeregner (som er 1-test-per-graf/prosent-skala og brukes uendret av 30+ andre
/// tester, se docs/beslutningslogg.md 2026-10-18). Begge instrumentenes besvarelser slås sammen
/// til ÉN kronologisk tidslinje og gis ordinale X-posisjoner over DEN UNIONEN (ikke hver sin egen
/// indeks) — en ORS- og en SRS-besvarelse fra samme reelle sesjon lander dermed naturlig tett ved
/// siden av hverandre, ORS alltid litt til venstre siden den besvares tidligere (se
/// PlanlagtTildelingService.OpprettKorFitAsync).
/// </summary>
public static class KorFitGrafBeregner
{
    public const double Bredde = 640;
    public const double Hoyde = 260;
    public const int OrsCutoffRaaskaar = 25;
    public const int SrsCutoffRaaskaar = 36;
    public const int RaaSkaarMaks = 40;

    private const double MarginVenstre = 40;
    private const double MarginHoyre = 16;
    private const double MarginTopp = 16;
    private const double MarginBunn = 40;
    private const int MaksDatoLabels = 8;

    public static KorFitGrafData Beregn(
        IReadOnlyList<SkaaringHistorikkPunkt> orsHistorikk,
        IReadOnlyList<SkaaringHistorikkPunkt> srsHistorikk)
    {
        var plotVenstre = MarginVenstre;
        var plotHoyre = Bredde - MarginHoyre;
        var plotTopp = MarginTopp;
        var plotBunn = Hoyde - MarginBunn;
        var plotBredde = plotHoyre - plotVenstre;
        var plotHoydeVerdi = plotBunn - plotTopp;

        double YFor(int raaSkaar) => plotTopp + (1 - Math.Clamp(raaSkaar, 0, RaaSkaarMaks) / (double)RaaSkaarMaks) * plotHoydeVerdi;

        // Slå ORS og SRS sammen til ÉN kronologisk union for X-posisjonering (se klassekommentar)
        // — hvert element bærer med seg hvilken serie det kom fra, slik at de to seriene kan
        // skilles ut igjen ETTER at ordinal-posisjonen er satt over den samlede tidslinjen.
        var union = orsHistorikk.Select(p => (Punkt: p, Serie: KorFitSerie.Ors))
            .Concat(srsHistorikk.Select(p => (Punkt: p, Serie: KorFitSerie.Srs)))
            .OrderBy(t => t.Punkt.Tildeling.FullfortUtc)
            .ToList();

        var antall = union.Count;
        double XFor(int i) => antall <= 1 ? plotVenstre : plotVenstre + (double)i / (antall - 1) * plotBredde;
        var steg = antall <= MaksDatoLabels ? 1 : (int)Math.Ceiling((double)antall / MaksDatoLabels);

        var orsPunkter = new List<KorFitGrafPunkt>();
        var srsPunkter = new List<KorFitGrafPunkt>();
        for (var i = 0; i < antall; i++)
        {
            var (punkt, serie) = union[i];
            var raaSkaar = punkt.Skaaring.RaaSkaar;
            var erSamtaleTid = raaSkaar > OrsCutoffRaaskaar && raaSkaar < SrsCutoffRaaskaar;
            var visLabel = i == 0 || i == antall - 1 || i % steg == 0;
            var dato = punkt.Tildeling.FullfortUtc?.ToString("dd.MM.yy") ?? "";

            var grafPunkt = new KorFitGrafPunkt(XFor(i), YFor(raaSkaar), dato, raaSkaar, serie, erSamtaleTid, visLabel);
            (serie == KorFitSerie.Ors ? orsPunkter : srsPunkter).Add(grafPunkt);
        }

        string Polyline(IReadOnlyList<KorFitGrafPunkt> punkter) => string.Join(" ", punkter.Select(p =>
            $"{p.X.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)},{p.Y.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}"));

        return new KorFitGrafData(
            orsPunkter, srsPunkter, Polyline(orsPunkter), Polyline(srsPunkter),
            YFor(OrsCutoffRaaskaar), YFor(SrsCutoffRaaskaar),
            plotVenstre, plotHoyre, plotTopp, plotBunn);
    }
}
