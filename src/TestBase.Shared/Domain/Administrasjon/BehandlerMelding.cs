namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// En "melding" (varsel) til en behandler — jf. beslutningsloggen "Meldinger og
/// oppgaveliste". Opprinnelig KUN "en pasient har fullført en test"
/// (TestTildelingId satt, opprettet automatisk av TestService.LagreSvarAsync).
/// Generalisert (se docs/beslutningslogg.md "Hjemmeoppgaver og programmer",
/// fase 0) til også å dekke program-hendelser (pasient satte et program på
/// pause/meldte seg ut) som IKKE er knyttet til noen enkelt TestTildeling —
/// TestTildelingId er derfor nullable, og <see cref="Fritekst"/> brukes i
/// stedet for en tildelings-basert visningstekst når TestTildelingId er null.
/// Nøyaktig ÉN av de to skal være satt (håndhevet av den opprettende tjenesten,
/// ikke her). Fungerer som et enkelt innboks-/oppgavesystem: uleste meldinger
/// vises som en teller ved "Min side" i navigasjonen (se _Layout.cshtml) og i
/// behandlers oppgaveliste, og markeres lest når behandler åpner rapporten
/// for den aktuelle tildelingen (se Rapport.cshtml.cs) — en Fritekst-melding
/// markeres lest direkte (ingen tilhørende rapport å åpne).
/// </summary>
public sealed class BehandlerMelding
{
    public long Id { get; set; }
    public long BehandlerId { get; set; }
    public long? TestTildelingId { get; set; }

    /// <summary>Kun satt når TestTildelingId er null — en ferdig formatert visningstekst for en program-hendelse (f.eks. "Kari Nordmann satte 'Søvnprogram' på pause 04.10.2026 14:32").</summary>
    public string? Fritekst { get; set; }

    public DateTimeOffset OpprettetUtc { get; set; }
    public DateTimeOffset? LestUtc { get; set; }
}
