namespace TestBase.Shared.Domain.Tester;

/// <summary>Én side i en test — grupperer ledd, jf. "Definisjon av en test" i kravdokumentet.</summary>
public sealed class TestSide
{
    public long Id { get; set; }
    public long TestId { get; set; }
    public int Rekkefolge { get; set; }
    public required string Navn { get; set; }
    public string? Instruksjon { get; set; }
}
