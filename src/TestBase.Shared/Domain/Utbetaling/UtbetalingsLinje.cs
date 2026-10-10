namespace TestBase.Shared.Domain.Utbetaling;

public enum UtbetalingsLinjeStatus
{
    Utkast,
    Godkjent,
    Avvist,
    Overfort,
    Feilet
}

/// <summary>
/// Én mottakers andel innad i en UtbetalingsBatch. <see cref="BelopKr"/> og
/// <see cref="AntallUnderliggendeTransaksjoner"/> er aggregert fra de
/// underliggende Pengebevegelse-radene (se UtbetalingsLinjePengebevegelse) —
/// KUN et antall, ALDRI pasientnavn/testidentifikatorer her, samme
/// personvernprinsipp som de periodiserte oppgjørsrapportene. Avvist-status
/// er PERMANENT (admin må eksplisitt be om ny linje senere, ingen stille
/// gjeninkludering neste måned) — se docs/beslutningslogg.md.
/// </summary>
public sealed class UtbetalingsLinje
{
    public long Id { get; set; }
    public long UtbetalingsBatchId { get; set; }

    public MottakerType MottakerType { get; set; }
    public long? BehandlerId { get; set; }
    public long? PartnerId { get; set; }

    public decimal BelopKr { get; set; }
    public int AntallUnderliggendeTransaksjoner { get; set; }

    public UtbetalingsLinjeStatus Status { get; set; } = UtbetalingsLinjeStatus.Utkast;

    public string? StripeTransferId { get; set; }
    public string? SisteFeilmelding { get; set; }
    public DateTimeOffset? OverfortUtc { get; set; }
    public int AntallForsok { get; set; }
}
