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
}
