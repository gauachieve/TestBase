namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>Hvilken av de to instrumentene ett punkt i grafen tilhører — styrer farge/serie i _KorFitGraf.cshtml.</summary>
public enum KorFitSerie
{
    Swls,
    Sai
}

/// <summary>Ett datapunkt i den kombinerte "tilbakemelding over tid"-grafen, allerede omregnet til SVG-koordinater — se KorFitGrafBeregner.Beregn.</summary>
public sealed record KorFitGrafPunkt(double X, double Y, string DatoKort, int RaaSkaar, int RaaSkaarMaks, int ProsentSkaar, KorFitSerie Serie, bool VisDatoLabel);

/// <summary>Alt en SVG-visning trenger for å tegne den kombinerte "tilbakemelding over tid"-grafen — se Pages/Shared/_KorFitGraf.cshtml.</summary>
public sealed record KorFitGrafData(
    IReadOnlyList<KorFitGrafPunkt> SwlsPunkter,
    IReadOnlyList<KorFitGrafPunkt> SaiPunkter,
    string SwlsPolylinePunkter,
    string SaiPolylinePunkter,
    double SwlsNoytraltPunktY,
    double PlotVenstre,
    double PlotHoyre,
    double PlotTopp,
    double PlotBunn);

/// <summary>
/// Regner ut geometrien for den kombinerte "tilbakemelding over tid"-grafen (SWLS + SAI i ÉN graf,
/// PROSENT av respektive instruments maks-skår på Y-aksen — ikke råskår, siden SWLS (maks 35) og
/// SAI (maks 30) har ulik skala og ikke kan deles på samme råskår-akse, i motsetning til den
/// tidligere ORS/SRS-varianten som delte 0-40-skalaen). Bevisst EGEN, purpose-built graf (navnet
/// "KorFit" er et internt, historisk klassenavn — ingen bruker ser det, se
/// docs/beslutningslogg.md "Fjerning av diagnostiske tester / MDR-opprydding" 2026-10-22), ikke en
/// utvidelse av den generiske UtviklingsGrafBeregner (1-test-per-graf, brukes uendret av alle
/// andre tester). Begge instrumentenes besvarelser slås sammen til ÉN kronologisk tidslinje og
/// gis ordinale X-posisjoner over DEN UNIONEN (ikke hver sin egen indeks) — en SWLS- og en
/// SAI-besvarelse fra samme reelle sesjon lander dermed naturlig tett ved siden av hverandre.
///
/// Den tidligere "samtale-tid"-varselmekanismen (skåren mellom ORS/SRS-cutoffene) er BEVISST
/// fjernet her, ikke videreført med nye terskler — SWLS/SAI har ingen publiserte kliniske
/// grenseverdier å basere et tilsvarende varsel på (se SwlsSkaaringsberegner/SaiSkaaringsberegner).
/// </summary>
public static class KorFitGrafBeregner
{
    public const double Bredde = 640;
    public const double Hoyde = 260;
    private const int SwlsRaaSkaarMaks = 35;
    private const int SwlsNoytraltPunktRaaskaar = 20;

    private const double MarginVenstre = 40;
    private const double MarginHoyre = 16;
    private const double MarginTopp = 16;
    private const double MarginBunn = 40;
    private const int MaksDatoLabels = 8;

    public static KorFitGrafData Beregn(
        IReadOnlyList<SkaaringHistorikkPunkt> swlsHistorikk,
        IReadOnlyList<SkaaringHistorikkPunkt> saiHistorikk)
    {
        var plotVenstre = MarginVenstre;
        var plotHoyre = Bredde - MarginHoyre;
        var plotTopp = MarginTopp;
        var plotBunn = Hoyde - MarginBunn;
        var plotBredde = plotHoyre - plotVenstre;
        var plotHoydeVerdi = plotBunn - plotTopp;

        double YForProsent(int prosentSkaar) => plotTopp + (1 - Math.Clamp(prosentSkaar, 0, 100) / 100.0) * plotHoydeVerdi;

        // Slå SWLS og SAI sammen til ÉN kronologisk union for X-posisjonering (se klassekommentar)
        // — hvert element bærer med seg hvilken serie det kom fra, slik at de to seriene kan
        // skilles ut igjen ETTER at ordinal-posisjonen er satt over den samlede tidslinjen.
        var union = swlsHistorikk.Select(p => (Punkt: p, Serie: KorFitSerie.Swls))
            .Concat(saiHistorikk.Select(p => (Punkt: p, Serie: KorFitSerie.Sai)))
            .OrderBy(t => t.Punkt.Tildeling.FullfortUtc)
            .ToList();

        var antall = union.Count;
        double XFor(int i) => antall <= 1 ? plotVenstre : plotVenstre + (double)i / (antall - 1) * plotBredde;
        var steg = antall <= MaksDatoLabels ? 1 : (int)Math.Ceiling((double)antall / MaksDatoLabels);

        var swlsPunkter = new List<KorFitGrafPunkt>();
        var saiPunkter = new List<KorFitGrafPunkt>();
        for (var i = 0; i < antall; i++)
        {
            var (punkt, serie) = union[i];
            var raaSkaar = punkt.Skaaring.RaaSkaar;
            var raaSkaarMaks = punkt.Skaaring.RaaSkaarMaks;
            var prosentSkaar = punkt.Skaaring.ProsentSkaar;
            var visLabel = i == 0 || i == antall - 1 || i % steg == 0;
            var dato = punkt.Tildeling.FullfortUtc?.ToString("dd.MM.yy") ?? "";

            var grafPunkt = new KorFitGrafPunkt(XFor(i), YForProsent(prosentSkaar), dato, raaSkaar, raaSkaarMaks, prosentSkaar, serie, visLabel);
            (serie == KorFitSerie.Swls ? swlsPunkter : saiPunkter).Add(grafPunkt);
        }

        string Polyline(IReadOnlyList<KorFitGrafPunkt> punkter) => string.Join(" ", punkter.Select(p =>
            $"{p.X.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)},{p.Y.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}"));

        var swlsNoytraltPunktProsent = (int)Math.Round(SwlsNoytraltPunktRaaskaar / (double)SwlsRaaSkaarMaks * 100, MidpointRounding.AwayFromZero);

        return new KorFitGrafData(
            swlsPunkter, saiPunkter, Polyline(swlsPunkter), Polyline(saiPunkter),
            YForProsent(swlsNoytraltPunktProsent),
            plotVenstre, plotHoyre, plotTopp, plotBunn);
    }
}
