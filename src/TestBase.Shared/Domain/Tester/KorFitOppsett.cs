namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Singleton-rad (alltid Id=1, opprettes lat — samme mønster som AbonnementOppsett/
/// AdminOkonomiOppsett) for tidsforskyvningene brukt av "KOR/FIT til neste time"-utsendingstypen
/// (se PlanlagtTildelingService/Tildel/Tester "OnPostPlanleggKorFitAsync") — ALDRI hardkodet i
/// selve utsendingslogikken, superadmin-redigerbar uten redeploy.
/// </summary>
public sealed class KorFitOppsett
{
    public long Id { get; set; }

    /// <summary>Antall timer FØR definert "neste time" ORS skal sendes (brukerens spesifikasjon: 2 timer).</summary>
    public double OrsTimerForTimeOffset { get; set; } = 2.0;

    /// <summary>Antall minutter ETTER definert "neste time" SRS skal sendes (brukerens spesifikasjon: 15 minutter).</summary>
    public int SrsMinutterEtterTimeOffset { get; set; } = 15;

    public long? SistEndretAvUserId { get; set; }
    public DateTimeOffset? SistEndretUtc { get; set; }
}
