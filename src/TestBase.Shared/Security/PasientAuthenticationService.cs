using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Security;

/// <summary>
/// Ren pålogging-logikk for pasienter: BankID KUN — ingen 2FA, i motsetning
/// til administrator/behandler. Kravdokumentet nevner ikke 2FA for pasient;
/// BankID-treffet mot personnummer er identitetsbekreftelsen. Har bevisst
/// ingen avhengighet til HttpContext/cookies, se Areas/Pasientportal/Pages/Konto.
/// </summary>
public sealed class PasientAuthenticationService
{
    private readonly AppDbContext _db;
    private readonly IBankIdProvider _bankId;

    public PasientAuthenticationService(AppDbContext db, IBankIdProvider bankId)
    {
        _db = db;
        _bankId = bankId;
    }

    public Task<BankIdResult> StartBankIdAsync(CancellationToken cancellationToken = default) =>
        _bankId.AuthenticateAsync(cancellationToken);

    /// <summary>
    /// Personnummer er kryptert i databasen (se AppDbContext) og kan derfor
    /// ikke slås opp direkte i SQL — sammenligning skjer i minnet, som for
    /// administrator/behandler. Kun ÉN pasient bør ha det faste
    /// mock-personnummeret om gangen (samme kjente fallgruve).
    /// </summary>
    public async Task<Pasient?> FinnVedPersonnummerAsync(string personnummer, CancellationToken cancellationToken = default)
    {
        var pasienter = await _db.Pasienter
            .Where(p => p.Status != PasientStatus.Arkivert)
            .ToListAsync(cancellationToken);
        return pasienter.FirstOrDefault(p => p.Personnummer == personnummer);
    }
}
