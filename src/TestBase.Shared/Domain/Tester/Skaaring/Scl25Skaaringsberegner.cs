namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// SCL-25-skåring: totalskår er en TRYGG ren listesum (1-4 per ledd, sum
/// 25-100) — offisiell cutoff er et GJENNOMSNITT ≥1,75, tilsvarende sum ≥44
/// av 100 (25×1,75=43,75, rundet opp siden skår er heltall). Delskala-snitt
/// (angst: ledd 1-10, depresjon: ledd 11-25) krever ekte TestLeddId-oppslag
/// (ITestSkaaringsberegnerMedLedd, se docs/beslutningslogg.md "Reell
/// 500-feil i GADIT-skåring") siden begge deler samme 1-4-skala. Ledd 24
/// ("Tanker om å avslutte livet") er et selvmordsscreeningledd — svar over 1
/// flagges alltid som en EGEN, fremhevet indikator, uavhengig av totalskår.
/// </summary>
public sealed class Scl25Skaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int AntallAngstLedd = 10;
    private const int AntallDepresjonLedd = 15;
    private const int SelvmordsleddIndeks = 23; // Ledd 24 (0-indeksert): "Tanker om å avslutte livet"
    private const int Maks = (AntallAngstLedd + AntallDepresjonLedd) * 4;

    public string TestKode => "scl25";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("SCL-25 krever ledd-informasjon for delskala-snitt — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => int.Parse(s.SvarVerdi));
        var raaSkaar = svarPerLeddId.Values.Sum();
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var angstIds = alleLedd.Take(AntallAngstLedd).Select(l => l.Id).ToList();
        var depresjonIds = alleLedd.Skip(AntallAngstLedd).Take(AntallDepresjonLedd).Select(l => l.Id).ToList();

        var angstSnitt = Snitt(angstIds, svarPerLeddId);
        var depresjonSnitt = Snitt(depresjonIds, svarPerLeddId);
        var totalSnitt = raaSkaar / (decimal)(AntallAngstLedd + AntallDepresjonLedd);

        var overGrense = totalSnitt >= 1.75m;

        var fortolkning =
            $"Gjennomsnittsskår {totalSnitt:0.00} (av skala 1-4) — " +
            (overGrense ? "OVER grensen 1,75, som indikerer klinisk signifikant symptomtrykk." : "under grensen 1,75.") +
            "\n\nDelskalaer:\n" +
            $"• Angst: {angstSnitt:0.00}\n" +
            $"• Depresjon: {depresjonSnitt:0.00}";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Samlet symptomtrykk", overGrense ? "Over grense (≥1,75)" : "Under grense", !overGrense),
            new("Angst-delskala (snitt)", angstSnitt.ToString("0.00"), angstSnitt < 1.75m),
            new("Depresjons-delskala (snitt)", depresjonSnitt.ToString("0.00"), depresjonSnitt < 1.75m)
        };

        if (alleLedd.Count > SelvmordsleddIndeks)
        {
            var selvmordsleddId = alleLedd[SelvmordsleddIndeks].Id;
            if (svarPerLeddId.TryGetValue(selvmordsleddId, out var selvmordsverdi) && selvmordsverdi > 1)
            {
                indikatorer.Insert(0, new TestSkaaringIndikator(
                    "OBS: Selvmordsscreening (ledd 24)", $"Besvart {selvmordsverdi}/4 — IKKE «Ikke i det hele tatt»", false));
            }
        }

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }

    private static decimal Snitt(IReadOnlyList<long> leddIder, IReadOnlyDictionary<long, int> svarPerLeddId)
    {
        var verdier = leddIder.Where(svarPerLeddId.ContainsKey).Select(id => svarPerLeddId[id]).ToList();
        return verdier.Count == 0 ? 0m : verdier.Sum() / (decimal)verdier.Count;
    }
}
