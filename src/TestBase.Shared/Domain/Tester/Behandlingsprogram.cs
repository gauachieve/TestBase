namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Programmer (2026-10-04, se docs/beslutningslogg.md "Hjemmeoppgaver og programmer") — en
/// GJENBRUKBAR tidsbasert leveringsplan av tester/hjemmeoppgaver over flere "drops" (se
/// ProgramDrop). "Gjenbrukbar" betyr her at selve malen kan tildeles på vilkårlig mange ulike
/// kalenderdatoer for ulike pasienter/grupper — StartUkedag/StartKlokkeslett er ETT fast
/// ukedag+klokkeslett-PUNKT (IKKE et vindu, i motsetning til en drops egen tidsplan), og hver
/// faktiske tildeling (se ProgramDeltakelse) beregner sin EGEN konkrete starttid fra denne malen,
/// samme "neste forekomst av ukedag X kl. Y"-beregning som
/// PlanlagtTildelingService.BeregnNesteForekomstUtc.
/// </summary>
public sealed class Behandlingsprogram
{
    public long Id { get; set; }
    public required string Navn { get; set; }
    public string? Forklaring { get; set; }

    public DayOfWeek StartUkedag { get; set; }
    public TimeSpan StartKlokkeslett { get; set; }

    public long OpprettetAvBehandlerId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
    public bool ErArkivert { get; set; }

    // --- Deling (samme mønster som Test sine hjemmeoppgave-felt) ---
    public bool ErDeltMedAlle { get; set; }
    public bool ErDeltMedPartner { get; set; }
    public long? KopiertFraProgramId { get; set; }
}
