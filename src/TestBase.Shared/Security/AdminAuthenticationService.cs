using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Security;

/// <summary>
/// Ren pålogging-logikk for administratorer: oppslag, passordverifisering
/// (utviklingsmodus) og BankID+SMS-2FA (produksjonsmodus). Har bevisst ingen
/// avhengighet til HttpContext/cookies — det håndteres av Razor Page-koden i
/// Areas/Admin/Pages/Konto som bruker denne tjenesten, slik at selve
/// pålogging-logikken er lett å teste isolert. 2FA-logikken selv ligger i den
/// delte ToFaktorService (gjenbrukt av BehandlerAuthenticationService).
/// </summary>
public sealed class AdminAuthenticationService
{
    private readonly AppDbContext _db;
    private readonly IBankIdProvider _bankId;
    private readonly ToFaktorService _toFaktor;
    private readonly PasswordHasher<Administrator> _passordHasher = new();

    public AdminAuthenticationService(AppDbContext db, IBankIdProvider bankId, ToFaktorService toFaktor)
    {
        _db = db;
        _bankId = bankId;
        _toFaktor = toFaktor;
    }

    public Task<Administrator?> FinnVedAdminIdAsync(string adminId, CancellationToken cancellationToken = default) =>
        _db.Administratorer.FirstOrDefaultAsync(a => a.AdminId == adminId && !a.ErArkivert, cancellationToken);

    /// <summary>
    /// Jf. kravdokumentet: "Hvis administrator har satt passord, er produktet
    /// i utviklingsmodus" — passord-tilstedeværelse er signalet, ikke miljøet.
    /// </summary>
    public static bool HarPassordPalogging(Administrator administrator) => administrator.PasswordHash is not null;

    public PasswordVerificationResult VerifiserPassord(Administrator administrator, string passord) =>
        _passordHasher.VerifyHashedPassword(administrator, administrator.PasswordHash!, passord);

    public string HashPassord(Administrator administrator, string passord) =>
        _passordHasher.HashPassword(administrator, passord);

    public Task<BankIdResult> StartBankIdAsync(string? personnummerOverride = null, CancellationToken cancellationToken = default) =>
        _bankId.AuthenticateAsync(personnummerOverride, cancellationToken);

    /// <summary>
    /// Personnummer er kryptert i databasen (se AppDbContext) og kan derfor
    /// ikke slås opp direkte i SQL — administrator-tabellen er uansett svært
    /// liten (én psykolog + kontorfellesskap), så sammenligning i minnet er
    /// uproblematisk.
    /// </summary>
    public async Task<Administrator?> FinnVedPersonnummerAsync(string personnummer, CancellationToken cancellationToken = default)
    {
        var administratorer = await _db.Administratorer.Where(a => !a.ErArkivert).ToListAsync(cancellationToken);
        return administratorer.FirstOrDefault(a => a.Personnummer == personnummer);
    }

    public Task<string> StartToFaktorAsync(Administrator administrator, CancellationToken cancellationToken = default) =>
        _toFaktor.StartAsync(ToFaktorPrincipalType.Administrator, administrator.Id, administrator.MobilNr, cancellationToken);

    public Task<bool> VerifiserToFaktorAsync(Administrator administrator, string kode, CancellationToken cancellationToken = default) =>
        _toFaktor.VerifiserAsync(ToFaktorPrincipalType.Administrator, administrator.Id, kode, cancellationToken);
}
