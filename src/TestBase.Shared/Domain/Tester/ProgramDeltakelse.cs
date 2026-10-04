namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Én PASIENTS faktiske deltakelse i et Behandlingsprogram — opprettes én rad PER PASIENT selv
/// for en gruppetildeling (alle medlemmer starter SAMME kalenderdag, brukerens eksplisitte svar),
/// men hver rad spores UAVHENGIG deretter (en pasient kan pause/melde seg ut uten å påvirke andre
/// i samme gruppe). Se ProgramBakgrunnstjeneste for hvordan NesteDroppPlanlagtUtc faktisk utløses.
/// </summary>
public sealed class ProgramDeltakelse
{
    public long Id { get; set; }
    public long ProgramId { get; set; }
    public long PasientId { get; set; }

    /// <summary>Satt KUN for en gruppetildeling — rent informasjonsfelt (hvilken gruppe denne deltakelsen kom fra), ingen egen logikk leser den i dag.</summary>
    public long? GruppeId { get; set; }

    public long? TildeltAvBehandlerId { get; set; }
    public long? TildeltAvAdministratorId { get; set; }

    /// <summary>Den beregnede, konkrete starttiden for DENNE deltakelsen (fra Behandlingsprogram sin ukedag-mal) — samme for alle medlemmer av én gruppetildeling.</summary>
    public DateTimeOffset ProgramStartUtc { get; set; }

    /// <summary>0-basert — hvilken ProgramDrop (etter Rekkefolge) som er NESTE til å fyres av.</summary>
    public int NaavaerendeDroppIndeks { get; set; }

    /// <summary>Det FORPLIKTEDE, allerede randomiserte tidspunktet neste drop skal fyres av — beregnes ÉN gang (ikke på nytt per poll, se ProgramBakgrunnstjeneste), null når alle drops er fyrt av eller deltakelsen er avsluttet.</summary>
    public DateTimeOffset? NesteDroppPlanlagtUtc { get; set; }

    /// <summary>Satt når pasienten trykker "Pause" — stopper fremtidige drops til MeldtUtUtc/gjenopptak, se Behandlerportal/Programmer sin "Kjørende"-fane.</summary>
    public DateTimeOffset? PauseUtc { get; set; }

    /// <summary>Satt når pasienten trykker "Meld ut" — TERMINAL, ingen flere drops noensinne (allerede igangsatte tester fullføres normalt, brukerens eksplisitte svar).</summary>
    public DateTimeOffset? MeldtUtUtc { get; set; }

    /// <summary>Satt når siste drop er fullført normalt (ikke meldt ut/pauset).</summary>
    public DateTimeOffset? FullfortUtc { get; set; }

    public DateTimeOffset OpprettetUtc { get; set; }
}
