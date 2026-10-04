namespace TestBase.Shared.Domain.Tester;

/// <summary>Svarmetodikk for et testledd, jf. "Definisjon av en test" i kravdokumentet.</summary>
public enum TestSvartype
{
    /// <summary>
    /// Likert-skala med et vilkårlig antall punkter, definert via
    /// TestLedd.Svaralternativer i formatet "verdi:tekst,verdi:tekst,..." (i
    /// visningsrekkefølge) — se TestLeddSvaralternativer. Generalisert fra en
    /// fast 5-punkts skala under fase 5 (WHO-5 er 6-punkts, 0–5).
    /// </summary>
    LikertSkala,
    VisuellAnalogSkala,
    JaNei,
    Fritekst,

    /// <summary>
    /// Hjemmeoppgaver (2026-10-04): rent visningsinnhold forfatteren (behandleren) legger inn —
    /// se TestLedd.BildeData/BildeContentType. ALDRI en TestSvar-rad, ALDRI ErPaakrevd=true (kan
    /// ikke "besvares"). Kun gyldig på en Test.ErHjemmeoppgave-test.
    /// </summary>
    Bilde
}
