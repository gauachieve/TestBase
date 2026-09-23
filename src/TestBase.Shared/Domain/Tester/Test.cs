namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Definisjonen av en psykologisk test (skjelett — jf. "Definisjon av en
/// test" i kravdokumentet). Skåringsmetodikk og rapportoppsett er bevisst
/// IKKE del av dette skjelettet — bevises ut med et konkret eksempel (WHO-5)
/// i fase 5. Lokalisering er heller ikke bygget ennå, se beslutningsloggen.
/// </summary>
public sealed class Test
{
    public long Id { get; set; }

    /// <summary>
    /// Stabil identifikator for innebygde tester (f.eks. "who5") — brukt til
    /// idempotent regenerering (se IInnebygdTestSeeder) og til å slå opp
    /// riktig skåringsberegner (se ITestSkaaringsberegner). Null for
    /// tester opprettet fritt av admin uten tilknyttet skåringslogikk.
    /// </summary>
    public string? Kode { get; set; }

    public required string Navn { get; set; }

    /// <summary>Instruksjon på test-nivå, vist før første side.</summary>
    public string? Beskrivelse { get; set; }

    /// <summary>Vist på belønningssiden når pasienten fullfører testen.</summary>
    public string? Belonningstekst { get; set; }

    /// <summary>
    /// Kort, klinisk beskrivelse av HVA testen måler — vist i rapportens
    /// sammendrag (se Behandlerportal/Pasienter/Rapport.cshtml). Bevisst
    /// EGET felt fra <see cref="Beskrivelse"/>, som er pasientvendte
    /// utfyllingsinstruksjoner ("sett en sirkel rundt..."), ikke noe en
    /// behandler/pasient bør lese i en ferdig rapport.
    /// </summary>
    public string? RapportIntroduksjon { get; set; }

    public bool ErAktiv { get; set; } = true;
    public DateTimeOffset OpprettetUtc { get; set; }

    /// <summary>
    /// Viser et "ICD-11 klar"-merke i test-oversikten — testen er forfattet/
    /// verifisert mot ICD-11s diagnostiske kriterier (se
    /// docs/beslutningslogg.md "ICD-11-tester"). Rent visningsfelt, påvirker
    /// ingen logikk. Ingen admin-UI for dette feltet ennå, samme mønster som
    /// RapportIntroduksjon — kun satt av innebygde testers seedere foreløpig.
    /// </summary>
    public bool IcdElleveKlar { get; set; }

    /// <summary>
    /// Behandler-/admin-synlig merknad om oversettelsesstatus (f.eks. "Ikke
    /// offisielt oversatt – kun til uttesting") — VISES ALDRI til pasienten
    /// (se Pasientportal/*, som kun leser <see cref="Navn"/>), kun i
    /// Admin/Tester/Index og tildelingsflytens sjekkliste, slik at den som
    /// SENDER testen kan gjøre et informert valg. Testnavnet (Navn) skal
    /// derfor alltid være kort og pasientvennlig, uten en slik merknad
    /// bakt inn — se docs/beslutningslogg.md "ICD-11-tester, del 2".
    /// Null for offisielt oversatte/ferdig godkjente tester.
    /// </summary>
    public string? OversettelseNotat { get; set; }

    // --- Prising (Superadmin-only, se docs/beslutningslogg.md "Partner System +
    // Test Monetization") — default 0 for alle eksisterende/nye tester inntil en
    // Superadmin faktisk konfigurerer dem, slik at ingenting endrer oppførsel
    // før noen aktivt velger å prise en test.

    /// <summary>Nedre grense for pasientens TOTALPRIS for denne testen — 0 betyr "kan tilbys gratis" (se TestPrisberegner).</summary>
    public decimal MinstePrisKr { get; set; }

    /// <summary>Øvre grense for pasientens TOTALPRIS for denne testen (plattform + partner + behandler-honorar til sammen).</summary>
    public decimal StorstePrisKr { get; set; }

    /// <summary>Foreslått standard behandler-honorar, vist i tildelingsskjemaet før behandler har noen egen historikk på denne testen.</summary>
    public decimal TypiskBehandlerHonorarKr { get; set; }

    /// <summary>Superadmin-satt gulv for hvor lite en partner-admin kan sette sin egen PartnerTestAndel til.</summary>
    public decimal MinstePartnerAndelKr { get; set; }

    /// <summary>
    /// Gyldighetsgrense (2026-09-23): maks andel (0-100) av testens ledd som kan
    /// stå ubesvart FØR resultatet flagges som en gyldighetsadvarsel til
    /// behandler — se TestService.BeregnSkaaringAsync og
    /// TestLedd.NormertGjennomsnitt (brukt til å likevel kunne beregne et tall
    /// for ubesvarte ledd som har et kjent normert gjennomsnitt). Null (default
    /// for ALLE eksisterende tester) betyr at funksjonen er AV for den testen —
    /// vi har bevisst IKKE fylt inn en verdi for noen innebygd test ennå, siden
    /// riktig grense/normert gjennomsnitt krever ekte, sitert normeringslitteratur
    /// per test (se docs/beslutningslogg.md), ikke en oppdiktet tommelfingerregel.
    /// Testen kan ALLTID leveres inn uansett — dette gir KUN en advarsel i
    /// rapporten, aldri en sperre for pasienten (se Pasientportal/Tester/Fyll).
    /// </summary>
    public int? MaksUbesvartProsent { get; set; }
}
