namespace TestBase.Shared.Domain.Utbetaling;

/// <summary>
/// Koblingstabell mellom én UtbetalingsLinje og de underliggende
/// Pengebevegelse-radene den dekker. Unik indeks på PengebevegelseId er selve
/// vernet mot dobbel utbetaling — én Pengebevegelse-rad kan havne i HØYST ÉN
/// utbetalingslinje noensinne. Batch-generering velger Pengebevegelse-rader
/// som IKKE har noen rad her ennå (se UtbetalingsBatchService) — en hoppet
/// over/avvist rad blir dermed naturlig med i en senere måned, bortsett fra
/// rader tilhørende en eksplisitt Avvist linje (se UtbetalingsLinje).
/// </summary>
public sealed class UtbetalingsLinjePengebevegelse
{
    public long Id { get; set; }
    public long UtbetalingsLinjeId { get; set; }
    public long PengebevegelseId { get; set; }
}
