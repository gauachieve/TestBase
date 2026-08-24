namespace TestBase.Shared.Domain.Tester;

/// <summary>Ett spørsmål/ledd på en testside, jf. "Definisjon av en test" i kravdokumentet.</summary>
public sealed class TestLedd
{
    public long Id { get; set; }
    public long TestSideId { get; set; }
    public int Rekkefolge { get; set; }
    public required string Sporsmalstekst { get; set; }
    public string? Instruksjon { get; set; }
    public TestSvartype Svartype { get; set; }

    /// <summary>Kommaseparerte labels — f.eks. Likert-tekster ("Aldri,Sjelden,Av og til,Ofte,Alltid").</summary>
    public string? Svaralternativer { get; set; }
}
