using System.Text.RegularExpressions;

namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Parser TestLedd.Svaralternativer for TestSvartype.LikertSkala: kommaseparerte
/// "verdi:tekst"-par, i den rekkefølgen de skal vises. Rekkefølgen er bevisst
/// IKKE sortert etter verdi — WHO-5 viser f.eks. høyeste verdi ("Hele tiden" = 5)
/// først og laveste ("Aldri" = 0) sist, samme rekkefølge som originaldokumentet.
///
/// Splitter KUN på et komma etterfulgt av "tall:" (neste par), ikke på ethvert
/// komma — nødvendig fra og med MADRS-S/søvnskjema-testene (fase 6+), hvor de
/// offisielle svartekstene selv inneholder komma (f.eks. "Jeg kjenner meg for
/// det meste nedstemt, men iblant kjennes det lettere."). Bakoverkompatibelt:
/// WHO-5 sin skala har ingen komma inni tekstene, så resultatet er identisk.
/// </summary>
public static class TestLeddSvaralternativer
{
    private static readonly Regex ParDelimiter = new(@",(?=-?\d+:)", RegexOptions.Compiled);

    public sealed record Punkt(int Verdi, string Tekst);

    public static IReadOnlyList<Punkt> Parse(string? svaralternativer)
    {
        if (string.IsNullOrWhiteSpace(svaralternativer))
        {
            return Array.Empty<Punkt>();
        }

        var punkter = new List<Punkt>();
        foreach (var del in ParDelimiter.Split(svaralternativer))
        {
            var trimmet = del.Trim();
            if (trimmet.Length == 0)
            {
                continue;
            }

            var deler = trimmet.Split(':', 2);
            if (deler.Length == 2 && int.TryParse(deler[0], out var verdi))
            {
                punkter.Add(new Punkt(verdi, deler[1]));
            }
        }

        return punkter;
    }
}
