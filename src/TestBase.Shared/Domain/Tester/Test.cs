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
}
