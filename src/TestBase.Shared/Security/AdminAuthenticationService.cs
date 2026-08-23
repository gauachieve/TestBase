using System.Security.Cryptography;
using System.Text;
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
/// pålogging-logikken er lett å teste isolert.
/// </summary>
public sealed class AdminAuthenticationService
{
    private static readonly TimeSpan ToFaktorKodeLevetid = TimeSpan.FromMinutes(10);
    private const int MaksForsokToFaktorKode = 5;

    private readonly AppDbContext _db;
    private readonly IBankIdProvider _bankId;
    private readonly ISmsSender _sms;
    private readonly PasswordHasher<Administrator> _passordHasher = new();

    public AdminAuthenticationService(AppDbContext db, IBankIdProvider bankId, ISmsSender sms)
    {
        _db = db;
        _bankId = bankId;
        _sms = sms;
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

    public Task<BankIdResult> StartBankIdAsync(CancellationToken cancellationToken = default) =>
        _bankId.AuthenticateAsync(cancellationToken);

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

    public async Task StartToFaktorAsync(Administrator administrator, CancellationToken cancellationToken = default)
    {
        var kode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        _db.ToFaktorKoder.Add(new ToFaktorKode
        {
            AdministratorId = administrator.Id,
            KodeHash = HashKode(kode),
            UtlopUtc = DateTimeOffset.UtcNow.Add(ToFaktorKodeLevetid)
        });
        await _db.SaveChangesAsync(cancellationToken);

        await _sms.SendAsync(
            administrator.MobilNr,
            $"TestBase-kode: {kode} (gyldig i {ToFaktorKodeLevetid.TotalMinutes:0} minutter).",
            cancellationToken);
    }

    public async Task<bool> VerifiserToFaktorAsync(Administrator administrator, string kode, CancellationToken cancellationToken = default)
    {
        var aktivKode = await _db.ToFaktorKoder
            .Where(k => k.AdministratorId == administrator.Id && k.BruktUtc == null && k.UtlopUtc > DateTimeOffset.UtcNow)
            .OrderByDescending(k => k.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (aktivKode is null || aktivKode.Forsok >= MaksForsokToFaktorKode)
        {
            return false;
        }

        aktivKode.Forsok++;

        if (aktivKode.KodeHash != HashKode(kode))
        {
            await _db.SaveChangesAsync(cancellationToken);
            return false;
        }

        aktivKode.BruktUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string HashKode(string kode) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(kode)));
}
