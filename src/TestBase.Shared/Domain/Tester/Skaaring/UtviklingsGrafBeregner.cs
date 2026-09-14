namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>Ett datapunkt i grafen, allerede omregnet til SVG-koordinater — se UtviklingsGrafBeregner.Beregn.</summary>
public sealed record UtviklingsGrafPunkt(double X, double Y, string DatoKort, int ProsentSkaar, bool VisDatoLabel);

/// <summary>Én normert referanselinje, med Y allerede omregnet til SVG-koordinat.</summary>
public sealed record UtviklingsGrafReferanselinje(string Navn, int ProsentVerdi, double Y);

/// <summary>Alt en SVG-visning trenger for å tegne grafen — se Pages/Shared/_UtviklingsGraf.cshtml.</summary>
public sealed record UtviklingsGrafData(
    IReadOnlyList<UtviklingsGrafPunkt> Punkter,
    IReadOnlyList<UtviklingsGrafReferanselinje> Referanselinjer,
    string PolylinePunkter,
    double PlotVenstre,
    double PlotHoyre,
    double PlotTopp,
    double PlotBunn);

/// <summary>
/// Regner ut geometrien for "utvikling over tid"-grafen (prosentskår 0–100 på
/// Y-aksen, dato på X-aksen) — ren, testbar C#, uavhengig av selve
/// SVG-rendringen (se Pages/Shared/_UtviklingsGraf.cshtml). Bevisst laget
/// GENERISK (kun avhengig av TestSkaaringReferanselinje, ikke noen spesifikk
/// test) slik at ENHVER test med et registrert ITestSkaaringsberegner og
/// minst to fullførte besvarelser automatisk får denne grafen i rapporten,
/// jf. ønsket om gjenbruk på tvers av flere tester (WHO-5 VAS var første,
/// 2026-09-14).
/// </summary>
public static class UtviklingsGrafBeregner
{
    public const double Bredde = 640;
    public const double Hoyde = 260;
    private const double MarginVenstre = 40;
    private const double MarginHoyre = 16;
    private const double MarginTopp = 16;
    private const double MarginBunn = 40;

    /// <summary>Vis høyst så mange dato-ETIKETTER (ikke punkter — alle punkter tegnes alltid) for å unngå overlappende tekst ved mange målinger.</summary>
    private const int MaksDatoLabels = 8;

    public static UtviklingsGrafData Beregn(
        IReadOnlyList<SkaaringHistorikkPunkt> historikk,
        IReadOnlyList<TestSkaaringReferanselinje> referanselinjer)
    {
        var plotVenstre = MarginVenstre;
        var plotHoyre = Bredde - MarginHoyre;
        var plotTopp = MarginTopp;
        var plotBunn = Hoyde - MarginBunn;
        var plotBredde = plotHoyre - plotVenstre;
        var plotHoydeVerdi = plotBunn - plotTopp;

        var antall = historikk.Count;

        double XFor(int i) => antall <= 1 ? plotVenstre : plotVenstre + (double)i / (antall - 1) * plotBredde;
        double YFor(int prosentSkaar) => plotTopp + (1 - Math.Clamp(prosentSkaar, 0, 100) / 100.0) * plotHoydeVerdi;

        // Vis alltid første og siste dato, og ellers hver N-te — aldri flere enn
        // MaksDatoLabels totalt, uansett hvor mange besvarelser det faktisk er.
        var steg = antall <= MaksDatoLabels ? 1 : (int)Math.Ceiling((double)antall / MaksDatoLabels);

        var punkter = new List<UtviklingsGrafPunkt>();
        for (var i = 0; i < antall; i++)
        {
            var visLabel = i == 0 || i == antall - 1 || i % steg == 0;
            var dato = historikk[i].Tildeling.FullfortUtc?.ToString("dd.MM.yy") ?? "";
            punkter.Add(new UtviklingsGrafPunkt(XFor(i), YFor(historikk[i].Skaaring.ProsentSkaar), dato, historikk[i].Skaaring.ProsentSkaar, visLabel));
        }

        var referanser = referanselinjer
            .Select(r => new UtviklingsGrafReferanselinje(r.Navn, r.ProsentVerdi, YFor(r.ProsentVerdi)))
            .ToList();

        var polyline = string.Join(" ", punkter.Select(p => $"{p.X.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)},{p.Y.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}"));

        return new UtviklingsGrafData(punkter, referanser, polyline, plotVenstre, plotHoyre, plotTopp, plotBunn);
    }
}
