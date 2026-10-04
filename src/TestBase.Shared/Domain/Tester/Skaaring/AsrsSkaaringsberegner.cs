namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// ASRS v1.1-screenerskåring: kun de 6 første leddene (Del A) telles, med
/// ASYMMETRISKE per-ledd-terskler (offisiell WHO/Kessler-nøkkel) — ledd 1-3
/// terskel "Noen ganger"(2)+, ledd 4-6 terskel "Ofte"(3)+. 4 eller flere ledd
/// over egen terskel = "sterkt konsistent med voksen-ADHD, anbefaler videre
/// utredning". Bruker ITestSkaaringsberegnerMedLedd (ikke ren listeposisjon i
/// svar) siden Del B (ledd 7-18) deler NØYAKTIG samme 0-4-skala som Del A —
/// et hoppet-over Del A-spørsmål ville ellers kunnet forskyve hvilke svar som
/// faktisk telles, se docs/beslutningslogg.md "Reell 500-feil i GADIT-skåring"
/// og ITestSkaaringsberegnerMedLedd sin klassekommentar.
/// </summary>
public sealed class AsrsSkaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int AntallDelALedd = 6;

    public string TestKode => "asrs";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("ASRS krever ledd-informasjon for korrekt Del A/Del B-skille — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var delAIds = alleLedd.Take(AntallDelALedd).Select(l => l.Id).ToList();
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => int.Parse(s.SvarVerdi));

        var antallOverTerskel = 0;
        for (var i = 0; i < delAIds.Count; i++)
        {
            if (!svarPerLeddId.TryGetValue(delAIds[i], out var verdi))
            {
                continue;
            }

            // Ledd 1-3 (indeks 0-2): terskel "Noen ganger" (2). Ledd 4-6 (indeks 3-5): terskel "Ofte" (3).
            var terskel = i < 3 ? 2 : 3;
            if (verdi >= terskel)
            {
                antallOverTerskel++;
            }
        }

        var positivScreening = antallOverTerskel >= 4;
        var fortolkning = positivScreening
            ? $"{antallOverTerskel} av 6 spørsmål i Del A er over sin terskel — sterkt konsistent med voksen-ADHD. " +
              "Anbefaler videre klinisk utredning."
            : $"{antallOverTerskel} av 6 spørsmål i Del A er over sin terskel — mindre konsistent med voksen-ADHD " +
              "basert på screeneren alene, men klinisk vurdering bør uansett ta hensyn til Del B og øvrig anamnese.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("ASRS Del A-screening", positivScreening ? "Positiv" : "Negativ", !positivScreening)
        };

        return new TestSkaaring(antallOverTerskel, AntallDelALedd, (int)Math.Round(antallOverTerskel * 100m / AntallDelALedd), fortolkning, indikatorer);
    }
}
