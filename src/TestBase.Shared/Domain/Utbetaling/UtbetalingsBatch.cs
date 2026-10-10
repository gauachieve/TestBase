namespace TestBase.Shared.Domain.Utbetaling;

public enum UtbetalingsBatchStatus
{
    Utkast,
    DelvisGodkjent,
    Godkjent,
    Avvist
}

/// <summary>
/// Ett samlet utbetalingsoppgjør for én kalendermåned, dekker BÅDE behandlere
/// og partnere (se UtbetalingsLinje.MottakerType for hvilken). Genereres av
/// UtbetalingsBatchService/UtbetalingsBatchBakgrunnstjeneste i status Utkast —
/// ALDRI noe annet enn Utkast fra selve genereringen, ingen penger flyttes før
/// en administrator eksplisitt godkjenner (se Admin/Utbetalinger). Unik indeks
/// på (Aar, Maned) er BÅDE den naturlige nøkkelen og bakgrunnsjobbens
/// idempotens-vern mot å generere samme måned to ganger.
/// </summary>
public sealed class UtbetalingsBatch
{
    public long Id { get; set; }
    public int Aar { get; set; }
    public int Maned { get; set; }
    public UtbetalingsBatchStatus Status { get; set; } = UtbetalingsBatchStatus.Utkast;

    public DateTimeOffset GenerertUtc { get; set; }
    public long? GodkjentAvAdministratorId { get; set; }
    public DateTimeOffset? GodkjentUtc { get; set; }

    /// <summary>Denormalisert sum av linjenes BelopKr — kun for rask listevisning, aldri kilden til sannhet.</summary>
    public decimal TotalBelopKr { get; set; }
}
