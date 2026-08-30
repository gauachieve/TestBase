using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Security;

/// <summary>
/// Ren pålogging-logikk for behandlere: BankID+SMS-2FA — INGEN passord-vei,
/// jf. kravdokumentets Del 3 (i motsetning til administrator har behandler
/// ikke noe utviklingsmodus-unntak). Har bevisst ingen avhengighet til
/// HttpContext/cookies, se Areas/Behandler/Pages/Konto.
/// </summary>
public sealed class BehandlerAuthenticationService
{
    private readonly AppDbContext _db;
    private readonly IBankIdProvider _bankId;
    private readonly ToFaktorService _toFaktor;

    public BehandlerAuthenticationService(AppDbContext db, IBankIdProvider bankId, ToFaktorService toFaktor)
    {
        _db = db;
        _bankId = bankId;
        _toFaktor = toFaktor;
    }

    public Task<BankIdResult> StartBankIdAsync(string? personnummerOverride = null, CancellationToken cancellationToken = default) =>
        _bankId.AuthenticateAsync(personnummerOverride, cancellationToken);

    /// <summary>
    /// Personnummer er kryptert i databasen (se AppDbContext) og kan derfor
    /// ikke slås opp direkte i SQL — sammenligning skjer i minnet, som for
    /// administrator (se AdminAuthenticationService.FinnVedPersonnummerAsync).
    /// Kun behandlere som har fullført egenregistrering (Personnummer satt) er
    /// aktuelle å matche mot BankID.
    /// </summary>
    public async Task<Behandler?> FinnVedPersonnummerAsync(string personnummer, CancellationToken cancellationToken = default)
    {
        var behandlere = await _db.Behandlere
            .Where(b => b.Status != BehandlerStatus.Arkivert && b.Personnummer != null)
            .ToListAsync(cancellationToken);
        return behandlere.FirstOrDefault(b => b.Personnummer == personnummer);
    }

    public Task<string> StartToFaktorAsync(Behandler behandler, CancellationToken cancellationToken = default) =>
        _toFaktor.StartAsync(ToFaktorPrincipalType.Behandler, behandler.Id, behandler.MobilNr, cancellationToken);

    public Task<bool> VerifiserToFaktorAsync(Behandler behandler, string kode, CancellationToken cancellationToken = default) =>
        _toFaktor.VerifiserAsync(ToFaktorPrincipalType.Behandler, behandler.Id, kode, cancellationToken);
}
