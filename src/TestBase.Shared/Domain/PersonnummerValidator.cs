namespace TestBase.Shared.Domain;

/// <summary>
/// Enkel formatsjekk av norsk fødselsnummer/D-nummer (11 siffer) — ren
/// sanity-sjekk (lengde + kun tall), IKKE en MOD11-kontrollsifferalgoritme.
/// Brukes overalt et personnummer tas imot fra et skjema, se
/// docs/beslutningslogg.md.
/// </summary>
public static class PersonnummerValidator
{
    public const int KrevdLengde = 11;

    public static bool ErGyldigFormat(string? personnummer) =>
        !string.IsNullOrWhiteSpace(personnummer)
        && personnummer.Length == KrevdLengde
        && personnummer.All(char.IsAsciiDigit);
}
