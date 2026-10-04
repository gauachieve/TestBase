namespace TestBase.Shared.Domain.Tester;

/// <summary>Én test/hjemmeoppgave i en ProgramDrop, i den rekkefølgen pasienten skal fylle dem ut (ett om gangen, se ProgramService.HaandterFullfortTestAsync).</summary>
public sealed class ProgramDropTest
{
    public long Id { get; set; }
    public long ProgramDropId { get; set; }
    public long TestId { get; set; }
    public int Rekkefolge { get; set; }
}
