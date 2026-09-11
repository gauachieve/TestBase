namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// TRAPS I-skåring: kun del 2 (PCL-5, de 20 siste av 36 ledd — del 1 sine 16
/// ledd, 15 JaNei + 1 Fritekst, teller ikke) summeres, skala 0-80. Cutoff
/// &gt;33 er sitert DIREKTE fra selve kildedokumentet ("Totalskåre over 33
/// indikerer PTSD. Skåre under 33 kan likevel indikere PTSD så lenge alle
/// kriteriene over er til stede.") — se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\TRAPS.md. Krever
/// at TestService.BeregnSkaaringAsync leverer svar sortert etter (side, ledd)
/// Rekkefolge, se der.
/// </summary>
public sealed class TrapsISkaaringsberegner : ITestSkaaringsberegner
{
    private const int AntallDel1Ledd = 16;
    private const int AntallDel2Ledd = 20;
    private const int Maks = AntallDel2Ledd * 4;
    private const int Grenseverdi = 33;

    public string TestKode => "traps_i";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var del2Svar = svar.Skip(AntallDel1Ledd).Take(AntallDel2Ledd);
        var raaSkaar = del2Svar.Sum(s => int.Parse(s.SvarVerdi));
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);
        var overGrense = raaSkaar > Grenseverdi;

        var fortolkning = overGrense
            ? $"Råskår {raaSkaar}/{Maks} — over grenseverdien ({Grenseverdi}). Totalskåre over {Grenseverdi} " +
              "indikerer PTSD."
            : $"Råskår {raaSkaar}/{Maks} — under grenseverdien ({Grenseverdi}). Skåre under grenseverdien kan " +
              "likevel indikere PTSD så lenge de strukturelle DSM-5-kriteriene er til stede — se del 1 for " +
              "hvilken hendelse pasienten har hatt i tankene.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("PTSD", overGrense ? "Over grenseverdi" : "Under grenseverdi", !overGrense)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
