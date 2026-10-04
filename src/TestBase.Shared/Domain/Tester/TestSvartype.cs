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
    /// Hjemmeoppgaver (se Test.OpprettetAvBehandlerId): rent illustrativt
    /// innhold (TestLedd.BildeData), kan "ekspanderes" (vises større) av
    /// pasienten — IKKE et spørsmål med et svar. Et Bilde-ledd kan derfor
    /// aldri være TestLedd.ErPaakrevd, og TestService.LagreSvarAsync oppretter
    /// ALDRI en TestSvar-rad for det (ingenting å lagre et svar om).
    /// </summary>
    Bilde
}
