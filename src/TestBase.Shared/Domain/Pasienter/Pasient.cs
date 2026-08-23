namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// En pasient lagt til av en behandler, jf. Del 3 i kravdokumentet. Kun
/// stamdata + status i denne slicen — selve pasientens egen registrering
/// (navn hvis ikke gruppeimportert, biologisk kjønn, adresse, brukeravtale,
/// Vipps-betalingsgate) er Del 4 og finnes ikke ennå.
/// </summary>
public sealed class Pasient
{
    public long Id { get; set; }

    /// <summary>Lagres kryptert i databasen via AppDbContext — se derfor aldri ubehandlet i logger.</summary>
    public required string Personnummer { get; set; }

    public required string MobilNr { get; set; }
    public required string Email { get; set; }

    /// <summary>Kun satt ved gruppeimport — ellers fylles navnet inn av pasienten selv i Del 4.</summary>
    public string? Navn { get; set; }

    public string? Gruppenavn { get; set; }
    public PasientStatus Status { get; set; } = PasientStatus.Invitert;
    public long BehandlerId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
    public DateTimeOffset? ArkivertUtc { get; set; }
}
