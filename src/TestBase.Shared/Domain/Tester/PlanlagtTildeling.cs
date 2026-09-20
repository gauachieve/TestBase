using TestBase.Shared.Domain.Pasienter;

namespace TestBase.Shared.Domain.Tester;

public enum PlanlagtTildelingStatus
{
    Venter,
    Sendt,
    Feilet
}

/// <summary>
/// En utsatt/gjentagende tildelingsbatch (bugliste 2026-09-15 punkt 8) — samme
/// inputer som TestTildelingsService.TildelOgVarsleAsync tar direkte i dag
/// (Tildel/Tester.cshtml, "Nå"), men lagret for senere utførelse av
/// PlanlagtTildelingBakgrunnstjeneste i stedet for å sendes synkront i
/// requesten. Pasient-/test-id-er og honorar lagres som enkle
/// tekst-/JSON-kolonner (samme pragmatiske mønster som PasientIderCsv andre
/// steder i tildelingsflyten) — ingen egne kobletabeller, dette er en
/// kortlevd kø-rad, ikke en varig relasjon.
/// </summary>
public sealed class PlanlagtTildeling
{
    public long Id { get; set; }
    public required string PasientIderCsv { get; set; }
    public required string TestIderCsv { get; set; }

    /// <summary>JSON-serialisert Dictionary&lt;long, decimal?&gt; (testId → ønsket honorar) — null/tom for admin-tildelinger uten prising.</summary>
    public string? HonorarKrJson { get; set; }

    public Varslingspreferanse Varslingsmetode { get; set; }
    public long? BehandlerId { get; set; }
    public long? AdministratorId { get; set; }

    /// <summary>Neste (eller eneste) tidspunkt utsendingen skal skje, UTC.</summary>
    public DateTimeOffset PlanlagtUtc { get; set; }

    /// <summary>Satt sammen (ikke null) kun for en gjentagende plan — se PlanlagtTildelingBakgrunnstjeneste.BeregnNesteForekomst.</summary>
    public DayOfWeek? GjentaUkedag { get; set; }
    public TimeSpan? GjentaKlokkeslett { get; set; }

    /// <summary>Antall GJENSTÅENDE utsendinger inkludert denne — dekrementeres per kjøring, raden er ferdig ved 0 (eller når feltet er null, dvs. ingen gjentagelse).</summary>
    public int? GjentaAntallGjenstaende { get; set; }

    public PlanlagtTildelingStatus Status { get; set; } = PlanlagtTildelingStatus.Venter;
    public string? Feilmelding { get; set; }
    public long OpprettetAvUserId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
}
