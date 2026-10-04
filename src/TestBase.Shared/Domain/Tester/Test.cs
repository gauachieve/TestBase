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

    /// <summary>
    /// Sant for tester som skal fylles ut AV BEHANDLER (om pasienten), ikke av
    /// pasienten selv — f.eks. kliniker-administrerte intervjuer/observasjons-
    /// skalaer (YGTSS-R, MADRS klinikkversjon, SCID-5-PF). Slike tildelinger
    /// sendes ALDRI til pasienten (ingen SMS/e-post, ingen lenke pasienten kan
    /// åpne), prises alltid 0/IkkePakrevd (se TestTildelingsService.
    /// TildelOgVarsleAsync), og fylles ut av behandleren selv på
    /// Behandlerportal/Pasienter/FyllForPasient — se docs/beslutningslogg.md
    /// "Behandler-utfylte tester". Standard false for ALLE eksisterende tester.
    /// </summary>
    public bool FyllesUtAvBehandler { get; set; }

    /// <summary>
    /// Sant for tester som påfører PRAKSISEN/behandleren en reell kostnad per
    /// gjennomføring (f.eks. en lisensavgift til testens rettighetshaver) —
    /// ATSKILT fra <see cref="StorstePrisKr"/>, som er PASIENTENS pris. Rent
    /// visningsfelt (dollar-ikon i tildelingsflyten, se
    /// docs/beslutningslogg.md "Forenkling av Tildel/Tester") — per 2026-10-02
    /// kun satt sann for M.I.N.I.-strukturdemoen. Standard false for ALLE
    /// eksisterende tester. Ingen admin-UI ennå, samme mønster som
    /// RapportIntroduksjon — kun satt av innebygde testers seedere foreløpig.
    /// </summary>
    public bool HarKostnadPerGjennomforing { get; set; }

    /// <summary>
    /// Sant for tester med KJØNNSSPESIFIKK skåring/normering (f.eks. MPFI-24 —
    /// se Mpfi24Skaaringsberegner) — krever Pasient.BiologiskKjonnVedFodsel satt
    /// for å kunne velge riktig normtabell. Håndhevet i
    /// TestTildelingsService.TildelOgVarsleAsync: en pasient UTEN registrert
    /// biologisk kjønn (f.eks. en "prøv systemet"-pasient som ikke har fullført
    /// profilen sin) får IKKE tildelt en slik test i det hele tatt — tildelingen
    /// hoppes over med en tydelig feilmelding til behandler/admin, i stedet for
    /// å opprettes og senere feile/gi feil resultat ved skåring. Standard false
    /// for ALLE eksisterende tester.
    /// </summary>
    public bool KreverBiologiskKjonn { get; set; }

    // --- Hjemmeoppgaver (2026-10-04, se docs/beslutningslogg.md "Hjemmeoppgaver og programmer") ---
    // En behandler-forfattet test ("egenprodusert"), atskilt fra admin-forfattede tester ved at
    // den har en eier og KUN gir gruppenivå-rapportering, aldri en TestSkaaring per pasient.

    /// <summary>Sann for en behandler-forfattet "hjemmeoppgave" — ALDRI skåret per pasient (se TestService.BeregnSkaaringAsync, som hopper over slike tester), kun rapportert på gruppenivå. Standard false for alle admin-forfattede tester.</summary>
    public bool ErHjemmeoppgave { get; set; }

    /// <summary>Behandleren som opprettet denne hjemmeoppgaven — null for enhver admin-forfattet test. Eieren ser testen i sin "Personlig"-liste og kan redigere/slette/dele den.</summary>
    public long? OpprettetAvBehandlerId { get; set; }

    /// <summary>Sann når eieren har delt hjemmeoppgaven med ALLE behandlere i systemet (deres "Delt"-fane) — uavhengig av partnerskap, se ErDeltMedPartner for den separate partner-only-delingen.</summary>
    public bool ErDeltMedAlle { get; set; }

    /// <summary>Sann når eieren har delt hjemmeoppgaven med sin egen partners øvrige behandlere (deres "Partner"-fane) — kun meningsfylt når eieren faktisk har en PartnerId, se Behandler.PartnerId.</summary>
    public bool ErDeltMedPartner { get; set; }

    /// <summary>
    /// Peker til hjemmeoppgaven denne ble kopiert FRA (ved "Rediger" på en likt/delt rad, eller en
    /// eksplisitt "Kopier"-handling på egen rad) — rent sporingsfelt, ingen logikk leser den i dag.
    /// Null for en original, egenforfattet hjemmeoppgave.
    /// </summary>
    public long? KopiertFraTestId { get; set; }

    /// <summary>Headline for "takk"-siden som vises etter innsending av en hjemmeoppgave (se Belonningstekst for selve meldingsteksten) — KUN brukt av hjemmeoppgaver, null/ubrukt for admin-forfattede tester.</summary>
    public string? BelonningsTittel { get; set; }
}
