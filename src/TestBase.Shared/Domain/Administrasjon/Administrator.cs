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

    /// <summary>
    /// Strengt supersett av vanlig Administrator — ser alt en Administrator
    /// ser, PLUSS partner-/prisingssider ingen andre admin-kontoer når (se
    /// docs/beslutningslogg.md "Partner System + Test Monetization"). Kun
    /// ment for ÉN reell konto — settes av den config-styrte admin-seeden i
    /// Program.cs, aldri via vanlig admin-CRUD.
    /// </summary>
    public bool ErSuperadmin { get; set; }

    public bool ErArkivert { get; set; }
    public DateTimeOffset? ArkivertUtc { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }

    /// <summary>
    /// Andre steg etter arkivering — skjuler raden for ALLE andre enn Superadmin
    /// (se Areas/Admin/Pages/Administratorer/Index.cshtml.cs). Kun tilgjengelig
    /// når allerede arkivert, se docs/beslutningslogg.md "Bugliste 2026-09-13".
    /// </summary>
    public bool ErSlettet { get; set; }
    public DateTimeOffset? SlettetUtc { get; set; }

    /// <summary>
    /// Ekte BankID (Idura) sin "sub"-claim — IKKE personnummer (se
    /// docs/beslutningslogg.md "STØ avviste fødselsnummer-bestilling" og
    /// "Tilbakemeldingsverktøy, del 3"/ekte BankID-aktivering): foretaket er
    /// kun godkjent for openid+profile, ALDRI nnin/nnin_altsub, så BankID gir
    /// oss aldri personnummeret tilbake. Null inntil kontoen er koblet én gang
    /// via Pages/Konto/BankIdKobleKonto (personnummer oppgitt manuelt DER,
    /// sammenlignet mot <see cref="Personnummer"/> i minnet som vanlig), se
    /// AdminAuthenticationService.FinnVedBankIdSubjektAsync/KoblBankIdSubjektAsync.
    /// IKKE kryptert (en ugjennomsiktig, klient-spesifikk Idura-ID er ikke i
    /// seg selv personopplysninger på samme måte som personnummer) og dermed
    /// trygt å slå opp direkte i SQL, i motsetning til Personnummer.
    /// </summary>
    public string? BankIdSubjekt { get; set; }
}
