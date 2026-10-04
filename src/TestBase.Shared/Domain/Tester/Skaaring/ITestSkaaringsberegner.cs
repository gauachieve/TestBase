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

    /// <summary>
    /// Skal grupperapportens histogram ("Spredning") og statistikk-tall (snitt/
    /// median/standardavvik) vises i PROSENT (0-100) for denne testen? Standard
    /// FALSK — de fleste standardiserte tester rapporterer og siterer cutoffs på
    /// RÅSKÅR-skalaen (f.eks. GADIT ≥5 av 8, PHQ-9 5/10/15/20 av 27), og en
    /// prosentkonvertering ville skjult akkurat de tallene klinikere kjenner
    /// igjen. Satt til SANN kun for de få testene der litteraturen SELV
    /// rapporterer i prosent (WHO-5/WHO-5 VAS) — se docs/beslutningslogg.md
    /// "Grupperapportens histogram: råskår eller prosent, og cutoff-linjer".
    /// </summary>
    bool VisSomProsentIHistogram => false;

    /// <summary>
    /// Valgfrie, navngitte cutoff-verdier å tegne som vertikale linjer i
    /// grupperapportens histogram, i SAMME enhet som <see cref="VisSomProsentIHistogram"/>
    /// velger (råskår eller prosent) — se TestSkaaringGrenseverdi. Tom liste som
    /// standard; de fleste tester har enten ingen enkelt cutoff eller flere
    /// sammensatte domene-grenser som ikke er meningsfulle å tegne som én linje.
    /// </summary>
    IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser => Array.Empty<TestSkaaringGrenseverdi>();

    /// <summary>
    /// Hvor stor en endring i prosentskår mellom to besvarelser av SAMME test må være for å
    /// markeres som "signifikant endring" i "utvikling over tid"-tabellen (se
    /// Behandlerportal/Pasienter/Rapport.cshtml). Standard NULL — ingen markering vises med
    /// mindre testen eksplisitt setter denne, basert på en EKTE sitert terskel. Satt KUN for
    /// WHO-5/WHO-5 VAS (10 prosentpoeng, WHO-5-manualens egen offisielle terskel) — en tidligere
    /// versjon av koden viste denne 10%-regelen for ALLE tester uansett, selv om den kun noensinne
    /// var sitert/gyldig for WHO-5 (bugliste 2026-10-04, se docs/beslutningslogg.md). BEVISST IKKE
    /// erstattet med "1 standardavvik fra forskningen" for andre tester: søk i litteraturen viser
    /// publiserte SD-tall for disse instrumentene varierer 2-3× avhengig av populasjon (f.eks.
    /// PHQ-9 ~6,5 i en generell befolkning mot 8-15 i kliniske utvalg; CORE-OM 4,3 mot 7,1) — å
    /// hardkode ÉN "forskningsbasert" terskel ville vært like misvisende som 10%-regelen den
    /// erstatter, så funksjonen er bevisst sovende for andre tester inntil en reell, sitert
    /// terskel for akkurat DEN populasjonen/bruken faktisk legges inn.
    /// </summary>
    double? SignifikantEndringProsentpoeng => null;
}
