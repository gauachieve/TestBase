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

    /// <summary>
    /// Behandlerens fritekstkommentar til DETTE spesifikke leddet — kun
    /// meningsfullt for behandler-utfylte tester (se Test.FyllesUtAvBehandler),
    /// f.eks. SCID-5-PF sitt behov for en klinisk begrunnelse per spørsmål.
    /// Alltid null for pasient-utfylte tester. Vises i rapporten sammen med
    /// selve svaret når satt. Se docs/beslutningslogg.md "Behandler-utfylte tester".
    /// </summary>
    public string? BehandlerKommentar { get; set; }
}
