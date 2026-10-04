namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Kobler en ekte TestTildeling tilbake til nøyaktig hvilken ProgramDeltakelse+ProgramDrop+
/// posisjon-i-droppen den stammer fra — ALDRI opprettet for en vanlig, program-uavhengig
/// tildeling. Brukt av ProgramService.HaandterFullfortTestAsync (kalt fra
/// Pasientportal/Tester/Fyll.cshtml.cs rett etter LagreSvarAsync) til å avgjøre om NESTE test i
/// SAMME drop skal opprettes nå, eller om droppen er ferdig og neste drops tidspunkt skal
/// beregnes i stedet — holder TestService/LagreSvarAsync HELT uvitende om programmer, se
/// ProgramService sin XML-doc for hvorfor dette bevisst ikke er en utvidelse av TestTildeling
/// selv.
/// </summary>
public sealed class ProgramTildeling
{
    public long Id { get; set; }
    public long ProgramDeltakelseId { get; set; }
    public long ProgramDropId { get; set; }
    public long TestTildelingId { get; set; }
    public int RekkefolgeIDrop { get; set; }
}
