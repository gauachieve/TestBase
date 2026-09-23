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
}
