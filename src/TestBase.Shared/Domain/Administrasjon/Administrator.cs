namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// En administrator, jf. Del 2 i kravdokumentet. Feltene merket i kravet som
/// obligatoriske (adminid, mobilnr, email, fullt navn, personnummer, HPR-nr) er
/// "required" her. <see cref="PasswordHash"/> er bevisst valgfritt: er den satt,
/// er kontoen i "utviklingsmodus" og logger inn med AdminId+passord i stedet for
/// BankID+2FA — se AdminAuthenticationService og beslutningsloggen.
/// </summary>
public sealed class Administrator
{
    public long Id { get; set; }
    public required string AdminId { get; set; }
    public required string MobilNr { get; set; }
    public required string Email { get; set; }
    public required string FulltNavn { get; set; }

    /// <summary>Lagres kryptert i databasen via AppDbContext — se derfor aldri ubehandlet i logger.</summary>
    public required string Personnummer { get; set; }

    public required string HprNr { get; set; }
    public string? PasswordHash { get; set; }
    public bool ErArkivert { get; set; }
    public DateTimeOffset? ArkivertUtc { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
}
