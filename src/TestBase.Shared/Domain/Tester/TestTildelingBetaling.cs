namespace TestBase.Shared.Domain.Tester;

public enum BetalingStatus
{
    IkkePakrevd,
    Venter,
    Betalt,
    Feilet,
    Refundert
}

public enum BetalingMetode
{
    Vipps,
    Stripe
}

/// <summary>
/// Finansiell "satellitt"-tabell til <see cref="TestTildeling"/> — bevisst
/// IKKE ekstra kolonner direkte på TestTildeling, både for å unngå EF sin
/// kjente fallgruve med feiltolkede kolonne-endringer når mange kolonner
/// endres samtidig på en tungt brukt tabell (se CLAUDE.md), og for å holde
/// "gjennomføring av test" og "penger" som separate bekymringer — samme
/// mønster som TestSvar allerede er splittet ut fra TestTildeling.
///
/// Beløpene under er en SNAPSHOT tatt på tildelingstidspunktet (se
/// TestPrisberegner) — de skal ALDRI regnes om senere selv om
/// Test/PartnerTestAndel-konfigurasjon endres i mellomtiden. Pasienten ser
/// KUN <see cref="PasientTotalprisKr"/> — de andre feltene er interne.
/// </summary>
public sealed class TestTildelingBetaling
{
    public long Id { get; set; }
    public long TestTildelingId { get; set; }

    public decimal PasientTotalprisKr { get; set; }
    public decimal BehandlerHonorarKr { get; set; }
    public decimal PlattformAndelKr { get; set; }
    public decimal? PartnerAndelKr { get; set; }

    /// <summary>
    /// Denormalisert snapshot av behandlerens PartnerId på tildelingstidspunktet
    /// — behandleren kan bytte/forlate partner senere, historikken skal ikke følge det.
    /// </summary>
    public long? PartnerId { get; set; }

    public bool DekketAvAbonnement { get; set; }

    public BetalingStatus Status { get; set; } = BetalingStatus.IkkePakrevd;
    public BetalingMetode? Metode { get; set; }
    public string? BetalingsleverandorReferanse { get; set; }

    public DateTimeOffset OpprettetUtc { get; set; }
    public DateTimeOffset? BetaltUtc { get; set; }
}
