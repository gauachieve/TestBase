namespace TestBase.Shared.Domain.Tester;

/// <summary>Samme mønster som HjemmeoppgaveLiking, for Behandlingsprogram i stedet for Test — se den sin XML-doc for full begrunnelse (ren referanse, ikke en kopi).</summary>
public sealed class ProgramLiking
{
    public long Id { get; set; }
    public long BehandlerId { get; set; }
    public long ProgramId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
}
