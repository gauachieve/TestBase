namespace TestBase.Shared.Domain.Tester;

public enum PengebevegelseType
{
    PasientBetalingMottatt,
    PlattformInntekt,
    PartnerAndel,
    BehandlerHonorar
}

/// <summary>
/// Regnskapsloggen — kilden til sannhet for alle pengebevegelser i
/// systemet. Skrives (opptil 4 rader) når en tildeling betales/dekkes av
/// abonnement (se PaymentWebhooks.cs). Grunnlaget for finansrapporten
/// ("Inntekt fra salg"/"Utgift til salg") og en fremtidig Stripe Connect-
/// utbetalingsmotor (ikke bygget i denne fasen, se docs/beslutningslogg.md).
/// </summary>
public sealed class Pengebevegelse
{
    public long Id { get; set; }
    public PengebevegelseType Type { get; set; }
    public decimal BelopKr { get; set; }
    public long? TestTildelingId { get; set; }
    public long? BehandlerId { get; set; }
    public long? PartnerId { get; set; }
    public string? Beskrivelse { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
}
