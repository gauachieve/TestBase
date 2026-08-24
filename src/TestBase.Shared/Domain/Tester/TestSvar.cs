namespace TestBase.Shared.Domain.Tester;

/// <summary>Ett besvart ledd innenfor en TestTildeling.</summary>
public sealed class TestSvar
{
    public long Id { get; set; }
    public long TestTildelingId { get; set; }
    public long TestLeddId { get; set; }

    /// <summary>Tallverdi (Likert/VAS), "Ja"/"Nei", eller fritekst — avhengig av TestLedd.Svartype.</summary>
    public required string SvarVerdi { get; set; }

    public DateTimeOffset BesvartUtc { get; set; }
}
