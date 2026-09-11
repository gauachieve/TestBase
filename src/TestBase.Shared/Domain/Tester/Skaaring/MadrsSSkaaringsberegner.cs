namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// MADRS-S-skåring: sum av de 9 ledd (0-6 hver, skala 0-54). Cutoff-tabellen
/// er sitert direkte fra MADRS-brukerveiledningen (Ulrik Fredrik Malt), som
/// selv presiserer at grensene er en grov veiledning, ikke en absolutt
/// sannhet — se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\MADRS_S.md.
/// Veiledningens tabell er gitt for full MADRS (0-60, med et 10. klinikerledd
/// MADRS-S mangler); grensene under er skalert til MADRS-S sitt maks (54).
/// </summary>
public sealed class MadrsSSkaaringsberegner : ITestSkaaringsberegner
{
    private const int Maks = 54;

    public string TestKode => "madrs_s";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var raaSkaar = svar.Sum(s => int.Parse(s.SvarVerdi));
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / Maks);

        var (kategori, grad) = raaSkaar switch
        {
            <= 6 => ("ikke deprimert", 0),
            <= 11 => ("kan uttrykke lett forstemning", 1),
            <= 22 => ("lett deprimert", 2),
            <= 29 => ("moderat deprimert", 3),
            <= 34 => ("alvorlig deprimert", 4),
            _ => ("svært alvorlig deprimert", 5)
        };

        var fortolkning =
            $"Råskår {raaSkaar}/{Maks} — {kategori} (basert på MADRS-brukerveiledningens grove kategorier, " +
            "skalert til MADRS-S sitt maks på 54). Brukerveiledningen presiserer at det ikke finnes noen " +
            "absolutt \"sannhet\" for skjæringspunkter — klinisk skjønn er alltid nødvendig.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Alvorlighetsgrad", kategori, grad == 0)
        };

        return new TestSkaaring(raaSkaar, Maks, prosentSkaar, fortolkning, indikatorer);
    }
}
