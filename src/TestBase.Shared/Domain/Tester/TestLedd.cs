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
    /// Hjemmeoppgaver (2026-10-04): sant for et ledd som MÅ besvares før innsending. KUN
    /// håndhevet av den egne hjemmeoppgave-utfyllingsflyten (se TestService.
    /// ValiderPaakrevdeLeddForHjemmeoppgaveAsync) — IKKE i den delte TestService.LagreSvarAsync,
    /// som fortsatt lar ALLE andre tester hoppe stille over ubesvarte ledd (se
    /// Test.MaksUbesvartProsent/GADIT-fallgruven i CLAUDE.md for hvorfor dette bevisst IKKE ble
    /// lagt i det delte laget). Alltid false for et Bilde-ledd (rent visningsinnhold, kan ikke
    /// "besvares") og for ethvert ledd på en ikke-hjemmeoppgave-test.
    /// </summary>
    public bool ErPaakrevd { get; set; }

    /// <summary>
    /// Hjemmeoppgaver (2026-10-04): base64-kodet, klient-squashet bilde for et Bilde-ledd — rent
    /// visningsinnhold forfatteren (behandleren) legger inn, ALDRI noe pasienten laster opp eller
    /// svarer på (se ErPaakrevd, alltid false her). Samme pragmatiske "base64 direkte i databasen"-
    /// mønster som Tilbakemelding.Skjermbilde — ingen egen blob-lagringsinfrastruktur finnes i
    /// prosjektet. Squashing (nedskalering + JPEG-rekoding) skjer i NETTLESEREN før opplasting
    /// (wwwroot/js/hjemmeoppgave-editor.js), ikke server-side, slik at en rå mobilbilde-original
    /// aldri når serveren. Null for ethvert annet ledd.
    /// </summary>
    public string? BildeData { get; set; }

    /// <summary>MIME-type for BildeData (alltid "image/jpeg" fra squashing-skriptet i dag) — lagret eksplisitt fremfor å anta, i tilfelle et fremtidig format legges til.</summary>
    public string? BildeContentType { get; set; }

    /// <summary>
    /// Bugliste 2026-10-05 punkt 13: en valgfri lenke forfatteren legger ved et Bilde-ledd (f.eks.
    /// en video/lydfil/ekstern ressurs), vist som en klikkbar lenke UNDER selve bildet. Atskilt fra
    /// TestSvartype.Url (der PASIENTEN selv skriver inn en lenke som sitt svar) — dette feltet er
    /// forfatterens EGET innhold, akkurat som BildeData, aldri noe pasienten fyller ut. Null for
    /// ethvert annet ledd.
    /// </summary>
    public string? BildeUrl { get; set; }
}
