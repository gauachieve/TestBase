namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// En pasient lagt til av en behandler, jf. Del 3 i kravdokumentet, og som
/// selv fullfører registreringen (navn hvis ikke gruppeimportert, biologisk
/// kjønn, kjønnsidentitet, adresse, brukeravtale) jf. Del 4 — se
/// PasientInvitasjonService.FullforRegistreringAsync. Status blir
/// <see cref="PasientStatus.Aktiv"/> først når egenregistreringen er fullført.
/// </summary>
public sealed class Pasient
{
    public long Id { get; set; }

    /// <summary>Lagres kryptert i databasen via AppDbContext — se derfor aldri ubehandlet i logger.</summary>
    public required string Personnummer { get; set; }

    public required string MobilNr { get; set; }
    public required string Email { get; set; }

    /// <summary>Kun satt ved gruppeimport — ellers fylles navnet inn av pasienten selv ved egenregistrering.</summary>
    public string? Navn { get; set; }

    public string? Gruppenavn { get; set; }

    public BiologiskKjonn? BiologiskKjonnVedFodsel { get; set; }
    public Kjonnsidentitet? Kjonnsidentitet { get; set; }
    public string? KjonnsidentitetSpesifisert { get; set; }
    public string? Adresse { get; set; }

    public int? BrukeravtaleGodkjentVersjon { get; set; }
    public DateTimeOffset? BrukeravtaleGodkjentUtc { get; set; }
    public bool GodtarLagringAvData { get; set; }
    public bool GodtarMuligVippsBetaling { get; set; }

    /// <summary>Når egenregistreringen ble fullført.</summary>
    public DateTimeOffset? RegistrertUtc { get; set; }

    public PasientStatus Status { get; set; } = PasientStatus.Invitert;
    public long BehandlerId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
    public DateTimeOffset? ArkivertUtc { get; set; }
}
