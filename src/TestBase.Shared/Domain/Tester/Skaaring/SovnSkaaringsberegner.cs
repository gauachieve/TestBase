using TestBase.Shared.Domain.Tester.InnebygdeTester;

namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Søvnskjemaet har INGEN offisiell sumskår i kildedokumentet (rent
/// kartleggingsskjema, se SovnTestSeeder). RaaSkaar her er derfor en enkel,
/// egendefinert sum av de 14 hovedsymptomene (0-56) — IKKE en klinisk
/// validert skår — supplert med indikator-flagg for mønstre som er kjent
/// klinisk relevante (søvnapné-symptomer: snorking/pustepauser;
/// narkolepsi-lignende symptomer: søvnparalyse/kataplexi-lignende anfall).
/// Krever at TestService.BeregnSkaaringAsync leverer svar sortert etter
/// (side, ledd) Rekkefolge, se der — de 14 første svarene er nettopp
/// SovnTestSeeder.Symptomledd i samme rekkefølge.
/// </summary>
public sealed class SovnSkaaringsberegner : ITestSkaaringsberegner
{
    private static readonly int AntallSymptomledd = SovnTestSeeder.Symptomledd.Length;
    private static readonly int Maks = AntallSymptomledd * 4;

    // 0-baserte posisjoner inn i Symptomledd — se rekkefølgen der.
    private const int PosSnorking = 5;
    private const int PosPustepauser = 6;
    private const int PosTrettPaaJobb = 7;
    private const int PosInnsovningJobb = 8;
    private const int PosInnsovningFritid = 9;
    private const int PosSovnparalyse = 12;
    private const int PosKataplexi = 13;

    public string TestKode => "sovn";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var symptomVerdier = svar.Take(AntallSymptomledd).Select(s => int.Parse(s.SvarVerdi)).ToList();
        var raaSkaar = symptomVerdier.Sum();
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var soovnapneMistanke = symptomVerdier.ElementAtOrDefault(PosSnorking) >= 3 || symptomVerdier.ElementAtOrDefault(PosPustepauser) >= 3;
        var narkolepsiMistanke = symptomVerdier.ElementAtOrDefault(PosSovnparalyse) >= 1 || symptomVerdier.ElementAtOrDefault(PosKataplexi) >= 1;
        var betydeligDagtretthet = symptomVerdier.ElementAtOrDefault(PosTrettPaaJobb) >= 3
            || symptomVerdier.ElementAtOrDefault(PosInnsovningJobb) >= 3
            || symptomVerdier.ElementAtOrDefault(PosInnsovningFritid) >= 3;

        var fortolkning =
            $"Sum av hovedsymptomer: {raaSkaar}/{Maks}. Dette er et rent kartleggingsskjema uten fastsatt " +
            "klinisk cutoff — vurder svarene (inkludert døgnrytme, medisinbruk og fritekstsvarene på de øvrige " +
            "sidene) klinisk, ikke bare denne summen.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Mulig søvnapné-mistanke", soovnapneMistanke ? "Snorking/pustepauser hyppig rapportert" : "Ikke hyppig rapportert", !soovnapneMistanke),
            new("Mulig narkolepsi-lignende symptomer", narkolepsiMistanke ? "Søvnparalyse/kataplexi-lignende symptomer rapportert" : "Ikke rapportert", !narkolepsiMistanke),
            new("Betydelig dagtretthet", betydeligDagtretthet ? "Hyppig rapportert" : "Ikke hyppig rapportert", !betydeligDagtretthet)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
