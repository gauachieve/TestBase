using TestBase.Shared.Domain.Pasienter;

namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// En behandler, jf. Del 2/3 i kravdokumentet. Opprettes av administrator ELLER en annen
/// behandler med kun kontaktinfo (status <see cref="BehandlerStatus.Invitert"/>) — nøyaktig én
/// av <see cref="InvitertAvAdministratorId"/>/<see cref="InvitertAvBehandlerId"/> er satt (se
/// BehandlerInvitasjonService). Resten av feltene fylles inn av behandleren selv via
/// invitasjonslenken. <see cref="Status"/> blir <see cref="BehandlerStatus.Aktiv"/> først når
/// egenregistrering, brukeravtale-aksept og kontaktverifisering (e-post + mobil) er fullført —
/// se BehandlerInvitasjonService.BekreftKontaktAsync.
/// </summary>
public sealed class Behandler
{
    public long Id { get; set; }
    public required string MobilNr { get; set; }
    public required string Email { get; set; }
    public string? Fornavn { get; set; }
    public string? Etternavn { get; set; }

    /// <summary>Lagres kryptert i databasen via AppDbContext — se derfor aldri ubehandlet i logger.</summary>
    public string? Personnummer { get; set; }

    public string? HprNr { get; set; }
    public string? Kontonummer { get; set; }
    public string? Arbeidsadresse { get; set; }
    public string? Tittel { get; set; }

    public bool HprGodkjent { get; set; }
    public DateTimeOffset? HprGodkjentUtc { get; set; }
    public long? HprGodkjentAvAdministratorId { get; set; }

    /// <summary>Når egenregistrering (skjema + avtale + verifisering) ble fullført — starter 7-dagers HPR-frist.</summary>
    public DateTimeOffset? RegistrertUtc { get; set; }
    public DateTimeOffset? EpostVerifisertUtc { get; set; }
    public DateTimeOffset? MobilVerifisertUtc { get; set; }

    public int? BrukeravtaleGodkjentVersjon { get; set; }
    public DateTimeOffset? BrukeravtaleGodkjentUtc { get; set; }

    public BehandlerStatus Status { get; set; } = BehandlerStatus.Invitert;

    public long? InvitertAvAdministratorId { get; set; }
    public long? InvitertAvBehandlerId { get; set; }

    public DateTimeOffset OpprettetUtc { get; set; }
    public DateTimeOffset? ArkivertUtc { get; set; }

    /// <summary>Daglig påminnelse om ugodkjente fullførte rapporter — se PaaminnelseService, satt under Behandlerportal/Innstillinger.</summary>
    public bool OnskerDagligPaaminnelse { get; set; }
    public Varslingspreferanse PaaminnelseKanal { get; set; } = Varslingspreferanse.Begge;
    public DateTimeOffset? SistPaaminnetUtc { get; set; }

    public string? Visningsnavn => Fornavn is null && Etternavn is null ? null : $"{Fornavn} {Etternavn}".Trim();
}
