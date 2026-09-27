namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>Ett stolpe-segment for én personlighetsforstyrrelse — bredder allerede regnet ut i piksler.</summary>
public sealed record Scid5PfStolpe(
    string Navn, int Antall2, int Antall1, int Total, int Terskel, bool TerskelNaadd,
    double BarBredde, double Segment2Bredde, double Segment1Bredde, double RestBredde, double CutoffX);

/// <summary>Alt SCID-5-PF-rapporten trenger for å tegne "stolpediagram per PD"-seksjonen, inkl. Blandet PF-heuristikken.</summary>
public sealed record Scid5PfBarData(IReadOnlyList<Scid5PfStolpe> Stolper, bool VurderBlandetPf, int TotaltAntall2);

/// <summary>
/// Bygger stolpediagram-geometrien for SCID-5-PF-rapporten: ÉN horisontal stolpe per
/// personlighetsforstyrrelse, delt i tre segmenter (2-ere lengst til venstre, deretter 1-ere,
/// resten som en tom/omrisset rest) pluss en nedovervendt pil ved kriterieterskelen. Hver
/// forstyrrelses stolpe er BEVISST kun så lang som dens EGET antall kriterier tilsier
/// (<see cref="PikslerPerKriterium"/> per ledd — 2026-09-27, etter brukerønske om en kortere
/// stolpe som faktisk reflekterer antall ledd, ikke en generisk fast bredde for alle 10). Egen,
/// SELVSTENDIG grupperingslogikk (samme prinsipp som Scid5PfSkaaringsberegner — gruppert etter
/// TestSideId sortert på laveste TestLeddId, portvaktledd identifisert via Svartype) fremfor å
/// endre TestSkaaring-kontrakten, siden dette er en rapport-spesifikk visning kun for denne ene
/// testen. "Blandet PF"-heuristikken (<see cref="Scid5PfBarData.VurderBlandetPf"/>) er BEVISST
/// IKKE en offisiell DSM-5/ICD-11-cutoff — verken DSM-5 sin "Uspesifisert personlighetsforstyrrelse"
/// eller ICD-11 sin dimensjonale personlighetsforstyrrelse-modell har noen sitert numerisk
/// terskel for dette. Egen, tydelig merket tommelfingerregel: minst 10 kriterier "Tydelig oppfylt"
/// SAMLET på tvers av alle 10 forstyrrelser, men INGEN enkelt forstyrrelse når sin egen terskel.
/// </summary>
public static class Scid5PfBarBeregner
{
    /// <summary>Pikselbredde PER KRITERIUM — én stolpe blir dermed Total*denne bredden, ikke en fast lengde uansett antall ledd.</summary>
    public const double PikslerPerKriterium = 22;

    private const int VurderBlandetPfMinstAntall2 = 10;

    private sealed record ForstyrrelseMeta(string Navn, int Terskel);

    // Rekkefølgen MÅ matche Forstyrrelser-arrayet i Scid5PfTestSeeder / Meta i Scid5PfSkaaringsberegner.
    private static readonly ForstyrrelseMeta[] Meta =
    {
        new("Unnvikende personlighetsforstyrrelse", 4),
        new("Avhengig personlighetsforstyrrelse", 5),
        new("Tvangspreget personlighetsforstyrrelse", 4),
        new("Paranoid personlighetsforstyrrelse", 4),
        new("Schizotyp personlighetsforstyrrelse", 5),
        new("Schizoid personlighetsforstyrrelse", 4),
        new("Histrionisk personlighetsforstyrrelse", 5),
        new("Narsissistisk personlighetsforstyrrelse", 5),
        new("Emosjonelt ustabil personlighetsforstyrrelse (Borderline)", 5),
        new("Antisosial personlighetsforstyrrelse", 3)
    };

    public static Scid5PfBarData? Beregn(IReadOnlyList<TestLedd> alleLedd, IReadOnlyDictionary<long, string> svarPerLeddId)
    {
        var sider = alleLedd
            .GroupBy(l => l.TestSideId)
            .OrderBy(g => g.Min(l => l.Id))
            .Select(g => g.OrderBy(l => l.Id).ToList())
            .ToList();

        if (sider.Count != Meta.Length)
        {
            return null;
        }

        var stolper = new List<Scid5PfStolpe>();
        var totaltAntall2 = 0;
        var noenTerskelNaadd = false;

        for (var i = 0; i < Meta.Length; i++)
        {
            var meta = Meta[i];
            var sideLedd = sider[i];
            var portvakter = sideLedd.Where(l => l.Svartype == TestSvartype.JaNei).ToList();
            var kriterieLedd = sideLedd.Where(l => l.Svartype != TestSvartype.JaNei).ToList();

            var antall2 = kriterieLedd.Count(l => svarPerLeddId.TryGetValue(l.Id, out var v) && v == "2");
            var antall1 = kriterieLedd.Count(l => svarPerLeddId.TryGetValue(l.Id, out var v) && v == "1");
            var total = kriterieLedd.Count;

            var terskelNaadd = antall2 >= meta.Terskel;
            if (portvakter.Count > 0)
            {
                terskelNaadd = terskelNaadd && portvakter.All(p => svarPerLeddId.TryGetValue(p.Id, out var v) && v == "Ja");
            }

            totaltAntall2 += antall2;
            if (terskelNaadd)
            {
                noenTerskelNaadd = true;
            }

            // Bevisst posisjonert på ekte kriterie-GRENSER (antall2 * pikselbredde, ikke en
            // brøkdel av en fast total) — stolpen blir dermed nøyaktig så lang som antall
            // kriterier tilsier, og hvert "trinn" tilsvarer ett faktisk kriterium.
            var barBredde = total * PikslerPerKriterium;
            var segment2Bredde = antall2 * PikslerPerKriterium;
            var segment1Bredde = antall1 * PikslerPerKriterium;
            var restBredde = Math.Max(0, barBredde - segment2Bredde - segment1Bredde);
            var cutoffX = Math.Min(meta.Terskel * PikslerPerKriterium, barBredde);

            stolper.Add(new Scid5PfStolpe(meta.Navn, antall2, antall1, total, meta.Terskel, terskelNaadd, barBredde, segment2Bredde, segment1Bredde, restBredde, cutoffX));
        }

        var vurderBlandetPf = !noenTerskelNaadd && totaltAntall2 >= VurderBlandetPfMinstAntall2;

        return new Scid5PfBarData(stolper, vurderBlandetPf, totaltAntall2);
    }
}
