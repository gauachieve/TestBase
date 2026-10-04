namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// En "melding" (varsel) til en behandler om at en pasient har fullført en
/// test — jf. beslutningsloggen "Meldinger og oppgaveliste". Opprettes
/// automatisk av TestService.LagreSvarAsync når en tildeling markeres
/// fullført. Fungerer som et enkelt innboks-/oppgavesystem: uleste meldinger
/// vises som en teller ved "Min side" i navigasjonen (se _Layout.cshtml) og i
/// behandlers oppgaveliste, og markeres lest når behandler åpner rapporten
/// for den aktuelle tildelingen (se Rapport.cshtml.cs).
/// </summary>
public sealed class BehandlerMelding
{
    public long Id { get; set; }
    public long BehandlerId { get; set; }
    public long TestTildelingId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
    public DateTimeOffset? LestUtc { get; set; }
}
