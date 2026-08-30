namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Parser TestLedd.Svaralternativer for TestSvartype.LikertSkala: kommaseparerte
/// "verdi:tekst"-par, i den rekkefølgen de skal vises. Rekkefølgen er bevisst
/// IKKE sortert etter verdi — WHO-5 viser f.eks. høyeste verdi ("Hele tiden" = 5)
/// først og laveste ("Aldri" = 0) sist, samme rekkefølge som originaldokumentet.
/// </summary>
public static class TestLeddSvaralternativer
{
    public sealed record Punkt(int Verdi, string Tekst);

    public static IReadOnlyList<Punkt> Parse(string? svaralternativer)
    {
        if (string.IsNullOrWhiteSpace(svaralternativer))
        {
            return Array.Empty<Punkt>();
        }

        var punkter = new List<Punkt>();
        foreach (var del in svaralternativer.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var deler = del.Split(':', 2);
            if (deler.Length == 2 && int.TryParse(deler[0], out var verdi))
            {
                punkter.Add(new Punkt(verdi, deler[1]));
            }
        }

        return punkter;
    }
}
