namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// SCID-5-PF-skåring: for hver av de 10 DSM-5 personlighetsforstyrrelsene,
/// tell antall kriterier besvart "Tydelig oppfylt" (verdi 2) og sammenlign mot
/// forstyrrelsens EGEN terskelverdi (se
/// <see cref="TestBase.Shared.Domain.Tester.InnebygdeTester.Scid5PfTestSeeder"/>
/// for terskelverdier og forbehold om at dette er en forenklet tilpasning).
/// Antisosial personlighetsforstyrrelse krever i tillegg BEGGE portvaktleddene
/// (atferdsforstyrrelse før 15 år OG alder ≥18) besvart "Ja".
///
/// Leddene grupperes etter TestSideId (10 sider, én per forstyrrelse) —
/// gruppene sorteres etter LAVESTE TestLeddId i hver gruppe (en ekte,
/// garantert monotont stigende auto-increment-PK), IKKE etter Rekkefolge
/// (som starter på 1 for HVER side og derfor ikke gir en pålitelig
/// side-til-side-rekkefølge på tvers av testen som helhet). Portvaktleddene
/// på antisosial-siden identifiseres via Svartype (JaNei), ikke posisjon —
/// samme robusthetsprinsipp som resten av natten.
/// </summary>
public sealed class Scid5PfSkaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private sealed record ForstyrrelseMeta(string Navn, int Terskel);

    // Rekkefølgen MÅ matche Forstyrrelser-arrayet i Scid5PfTestSeeder (seedet i denne rekkefølgen).
    private static readonly ForstyrrelseMeta[] Meta =
    {
        new("Paranoid personlighetsforstyrrelse", 4),
        new("Schizoid personlighetsforstyrrelse", 4),
        new("Schizotyp personlighetsforstyrrelse", 5),
        new("Antisosial personlighetsforstyrrelse", 3),
        new("Emosjonelt ustabil personlighetsforstyrrelse (Borderline)", 5),
        new("Histrionisk personlighetsforstyrrelse", 5),
        new("Narsissistisk personlighetsforstyrrelse", 5),
        new("Unnvikende personlighetsforstyrrelse", 4),
        new("Avhengig personlighetsforstyrrelse", 5),
        new("Tvangspreget personlighetsforstyrrelse", 4)
    };

    public string TestKode => "scid5_pf";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("SCID-5-PF krever ledd-informasjon for å skille forstyrrelser fra hverandre — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);

        var sider = alleLedd
            .GroupBy(l => l.TestSideId)
            .OrderBy(g => g.Min(l => l.Id))
            .Select(g => g.OrderBy(l => l.Id).ToList())
            .ToList();

        var indikatorer = new List<TestSkaaringIndikator>();
        var oppfylteForstyrrelser = new List<string>();
        int totalOppfylt = 0;
        int totalKriterier = 0;

        for (var i = 0; i < Meta.Length && i < sider.Count; i++)
        {
            var meta = Meta[i];
            var sideLedd = sider[i];

            var portvakter = sideLedd.Where(l => l.Svartype == TestSvartype.JaNei).ToList();
            var kriterieLedd = sideLedd.Where(l => l.Svartype != TestSvartype.JaNei).ToList();

            var antallOppfylt = kriterieLedd.Count(l =>
                svarPerLeddId.TryGetValue(l.Id, out var v) && v == "2");

            totalOppfylt += antallOppfylt;
            totalKriterier += kriterieLedd.Count;

            var terskelNaadd = antallOppfylt >= meta.Terskel;

            if (portvakter.Count > 0)
            {
                // Antisosial: krever i tillegg begge portvaktledd besvart "Ja".
                var beggePortvakterJa = portvakter.All(p =>
                    svarPerLeddId.TryGetValue(p.Id, out var v) && v == "Ja");
                terskelNaadd = terskelNaadd && beggePortvakterJa;
            }

            indikatorer.Add(new TestSkaaringIndikator(
                meta.Navn, $"{antallOppfylt}/{kriterieLedd.Count} kriterier oppfylt (terskel {meta.Terskel})", !terskelNaadd));

            if (terskelNaadd)
            {
                oppfylteForstyrrelser.Add(meta.Navn);
            }
        }

        var prosentSkaar = totalKriterier == 0 ? 0 : (int)Math.Round(totalOppfylt * 100m / totalKriterier);

        var fortolkning = oppfylteForstyrrelser.Count > 0
            ? $"Diagnostisk terskel (basert på denne forenklede screeningen) er nådd for: {string.Join(", ", oppfylteForstyrrelser)}. " +
              "Dette er en STØTTE til klinisk vurdering, ikke en automatisk diagnose — full SCID-5-PD-vurdering og klinisk skjønn kreves."
            : "Ingen personlighetsforstyrrelse når diagnostisk terskel basert på denne screeningen. " +
              "Subklinisk trekk (verdi 1) på enkeltkriterier kan likevel være klinisk relevant.";

        return new TestSkaaring(totalOppfylt, totalKriterier, prosentSkaar, fortolkning, indikatorer);
    }
}
