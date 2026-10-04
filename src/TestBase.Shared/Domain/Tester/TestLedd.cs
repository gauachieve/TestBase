namespace TestBase.Shared.Domain.Tester;

/// <summary>Ett spørsmål/ledd på en testside, jf. "Definisjon av en test" i kravdokumentet.</summary>
public sealed class TestLedd
{
    public long Id { get; set; }
    public long TestSideId { get; set; }
    public int Rekkefolge { get; set; }
    public required string Sporsmalstekst { get; set; }
    public string? Instruksjon { get; set; }
    public TestSvartype Svartype { get; set; }

    /// <summary>Kommaseparerte labels — f.eks. Likert-tekster ("Aldri,Sjelden,Av og til,Ofte,Alltid").</summary>
    public string? Svaralternativer { get; set; }

    /// <summary>
    /// Normert (populasjons-)gjennomsnittssvar for DETTE leddet, fra publisert
    /// normeringslitteratur — brukt av TestService.BeregnSkaaringAsync til å
    /// imputere en verdi for et ubesvart ledd, slik at testen fortsatt kan
    /// skåres selv om et fåtall svar mangler (se Test.MaksUbesvartProsent for
    /// selve gyldighetsgrensen). Kun meningsfullt for numeriske svartyper
    /// (LikertSkala/VisuellAnalogSkala) — irrelevant for JaNei/fritekst. Null
    /// (default) betyr "ingen kjent normert verdi" — et ubesvart ledd bidrar da
    /// rett og slett ikke til skåren, akkurat som før dette feltet fantes.
    /// </summary>
    public decimal? NormertGjennomsnitt { get; set; }

    /// <summary>
    /// Hjemmeoppgaver (se Test.OpprettetAvBehandlerId): sant betyr dette leddet
    /// MÅ være besvart før pasienten kan levere inn siden/testen — håndhevet i
    /// TestService.LagreSvarAsync (kaster hvis et påkrevd ledd mangler svar ved
    /// "Ferdig"). NY PRIMITIV, bevisst KUN brukt for hjemmeoppgaver foreløpig —
    /// ALLE eksisterende (innebygde/admin-forfattede) tester har denne false og
    /// er dermed helt upåvirket; "frivillig å svare, men en advarsel ved mange
    /// ubesvarte" (Test.MaksUbesvartProsent) er en annen, allerede eksisterende
    /// mekanisme og forblir uendret.
    /// </summary>
    public bool ErPaakrevd { get; set; }

    /// <summary>
    /// Kun for Svartype == TestSvartype.Bilde: selve bildet som en data-URL
    /// (f.eks. "data:image/png;base64,...") lagret direkte i databasen — samme
    /// pragmatiske mønster som Tilbakemelding.Skjermbilde (se den klassens
    /// doc), BEVISST valgt fremfor ny blob-lagringsinfrastruktur (ingen slik
    /// finnes i prosjektet i dag). Rent visningsinnhold, display-only — et
    /// Bilde-ledd kan ALDRI være ErPaakrevd (håndhevet i editoren), siden det
    /// ikke finnes noe "svar" å kreve.
    /// </summary>
    public string? BildeData { get; set; }
}
