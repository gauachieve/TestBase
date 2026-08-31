namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Én tildeling av en test til en pasient — også forsøket/besvarelsen selv i
/// dette skjelettet (ett forsøk per tildeling). <see cref="Frist"/> og
/// <see cref="VarighetMinutter"/> lagres men håndheves/varsles IKKE i denne
/// slicen (jf. beslutningsloggen — påminnelser er utsatt). Nøyaktig én av
/// <see cref="TildeltAvBehandlerId"/>/<see cref="TildeltAvAdministratorId"/>
/// er satt — samme dobbelt-aktør-mønster som BehandlerInvitasjon, siden både
/// behandler og admin kan tildele tester (jf. tildelingsflyten).
/// </summary>
public sealed class TestTildeling
{
    public long Id { get; set; }
    public long TestId { get; set; }
    public long PasientId { get; set; }
    public long? TildeltAvBehandlerId { get; set; }
    public long? TildeltAvAdministratorId { get; set; }
    public DateTimeOffset TildeltUtc { get; set; }
    public DateTimeOffset? Frist { get; set; }
    public int? VarighetMinutter { get; set; }
    public TestTildelingStatus Status { get; set; } = TestTildelingStatus.Tildelt;
    public DateTimeOffset? StartetUtc { get; set; }
    public DateTimeOffset? FullfortUtc { get; set; }

    /// <summary>
    /// Behandler må eksplisitt godkjenne en fullført rapport (se
    /// TestService.GodkjennRapportAsync) — dette er en FORUTSETNING for, men
    /// ikke det samme som, at pasienten faktisk kan se den. Populerer
    /// behandlers oppgaveliste (se TestService.HentUgodkjenteFullforteForBehandlerAsync)
    /// inntil satt.
    /// </summary>
    public DateTimeOffset? RapportGodkjentUtc { get; set; }

    /// <summary>
    /// Egen, valgfri deling-bryter — kun tilgjengelig/betydningsfull etter at
    /// rapporten er godkjent (se RapportGodkjentUtc). Standard false: pasienten
    /// ser ALDRI en rapport med mindre behandler aktivt har delt den, selv
    /// etter godkjenning.
    /// </summary>
    public bool RapportSynligForPasient { get; set; }

    /// <summary>
    /// Behandler forkastet denne besvarelsen i stedet for å godkjenne (f.eks.
    /// åpenbart feilbesvart) — se TestService.ForkastRapportAsync. Status
    /// forblir Fullfort (historisk faktum: testen BLE besvart), dette er en
    /// egen, separat beslutning lagt oppå, som RapportGodkjentUtc. Svarene
    /// står urørt for sporbarhet; en NY tildeling opprettes og sendes til
    /// pasienten (se Behandlerportal/Pasienter/Rapport.cshtml.cs).
    /// </summary>
    public DateTimeOffset? RapportForkastetUtc { get; set; }
}
