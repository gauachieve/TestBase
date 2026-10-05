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
    Bilde,

    /// <summary>
    /// Bugliste 2026-10-05 punkt 13: PASIENTEN skriver inn en lenke som sitt svar (f.eks. et bilde
    /// de har lastet opp et annet sted, en video de har tatt opp selv) — lagret som vanlig fritekst
    /// i TestSvar.SvarVerdi, kun forskjellig ved at Fyll.cshtml rendrer et &lt;input type="url"&gt;
    /// med URL-validering i stedet for en fri tekstboks. IKKE det samme som TestLedd.BildeUrl
    /// (forfatterens EGEN lenke under et Bilde-ledd).
    /// </summary>
    Url
}
