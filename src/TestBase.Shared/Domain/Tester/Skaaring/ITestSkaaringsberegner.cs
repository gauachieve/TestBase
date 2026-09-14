namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Skåringsmetodikk for én bestemt test (identifisert via Test.Kode), jf.
/// "Definisjon av en test" i kravdokumentet: "En skåringsmetodikk som
/// genererer noen variabler med en spesiell formel/metodikk". Bevisst
/// test-spesifikk kode, ikke et generisk formelspråk — kravet sier selv at
/// det ikke er nødvendig å bygge et system for å generere tester utenom
/// hovedsystemet.
/// </summary>
public interface ITestSkaaringsberegner
{
    string TestKode { get; }
    TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar);

    /// <summary>
    /// Valgfrie, navngitte horisontale referanselinjer fra normeringslitteraturen
    /// (f.eks. WHO-5 VAS sine grenseverdier for velvære/depresjon) — brukt av
    /// "utvikling over tid"-grafen (se Pages/Shared/_UtviklingsGraf.cshtml).
    /// Tom liste som standard slik at eksisterende skåringsberegnere ikke må
    /// endres for å ta i bruk grafinfrastrukturen.
    /// </summary>
    IReadOnlyList<TestSkaaringReferanselinje> Referanselinjer => Array.Empty<TestSkaaringReferanselinje>();
}
