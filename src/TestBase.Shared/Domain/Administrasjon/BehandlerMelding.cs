namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// En "melding" (varsel) til en behandler — jf. beslutningsloggen "Meldinger og oppgaveliste".
/// Opprinnelig KUN "en pasient fullførte en test" (TestTildelingId, opprettet automatisk av
/// TestService.LagreSvarAsync); generalisert 2026-10-04 (se "Hjemmeoppgaver og programmer") til
/// ALTERNATIVT å være en ren fritekst-hendelse UTEN noen tilknyttet tildeling — f.eks. "Kari
/// Nordmann meldte seg ut av programmet kl. 14:32" — se Fritekst. Nøyaktig ÉN av
/// TestTildelingId/Fritekst er satt, aldri begge, aldri ingen. Fungerer som et enkelt innboks-/
/// oppgavesystem: uleste meldinger vises som en teller ved "Min side" i navigasjonen (se
/// _Layout.cshtml) og i behandlers oppgaveliste, og markeres lest når behandler åpner rapporten
/// for tildelingen (Rapport.cshtml.cs) eller eksplisitt kvitterer en fritekst-melding.
/// </summary>
public sealed class BehandlerMelding
{
    public long Id { get; set; }
    public long BehandlerId { get; set; }

    /// <summary>Null for en fritekst-melding (se Fritekst) — ellers tildelingen pasienten fullførte.</summary>
    public long? TestTildelingId { get; set; }

    /// <summary>Selve meldingsteksten for en hendelse UTEN tilknyttet tildeling (f.eks. program-pause/meld-ut) — null for en vanlig "fullført test"-melding.</summary>
    public string? Fritekst { get; set; }

    public DateTimeOffset OpprettetUtc { get; set; }
    public DateTimeOffset? LestUtc { get; set; }
}
