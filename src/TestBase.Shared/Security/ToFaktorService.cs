using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Security;

/// <summary>
/// Delt SMS-2FA-logikk brukt av både administrator- og behandler-pålogging
/// (se AdminAuthenticationService/BehandlerAuthenticationService) — løftet ut
/// hit under fase 3 for å unngå å duplisere sikkerhetskritisk kode (hash,
/// utløp, forsøksbrems) for hver ny prinsipaltype som trenger 2FA.
/// </summary>
public sealed class ToFaktorService
{
    private static readonly TimeSpan ToFaktorKodeLevetid = TimeSpan.FromMinutes(10);
    private const int MaksForsokToFaktorKode = 5;

    private readonly AppDbContext _db;
    private readonly ISmsSender _sms;

    public ToFaktorService(AppDbContext db, ISmsSender sms)
    {
        _db = db;
        _sms = sms;
    }

    /// <summary>
    /// Returnerer den genererte koden slik at kalleren (Pages/Konto/LoggInn) kan
    /// vise den direkte i dev-UI-et — MockSmsSender logger den KUN til
    /// konsollen, som i praksis er ubrukelig for manuell nettleser-testing (se
    /// samme prinsipp for BehandlerInvitasjonResultat/PasientInvitasjonResultat).
    /// </summary>
    public async Task<string> StartAsync(ToFaktorPrincipalType principalType, long principalId, string mobilNr, CancellationToken cancellationToken = default)
    {
        var kode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        _db.ToFaktorKoder.Add(new ToFaktorKode
        {
            PrincipalType = principalType,
            PrincipalId = principalId,
            KodeHash = HashKode(kode),
            UtlopUtc = DateTimeOffset.UtcNow.Add(ToFaktorKodeLevetid)
        });
        await _db.SaveChangesAsync(cancellationToken);

        await _sms.SendAsync(
            mobilNr,
            $"TestBase-kode: {kode} (gyldig i {ToFaktorKodeLevetid.TotalMinutes:0} minutter).",
            cancellationToken);

        return kode;
    }

    public async Task<bool> VerifiserAsync(ToFaktorPrincipalType principalType, long principalId, string kode, CancellationToken cancellationToken = default)
    {
        var aktivKode = await _db.ToFaktorKoder
            .Where(k => k.PrincipalType == principalType && k.PrincipalId == principalId &&
                        k.BruktUtc == null && k.UtlopUtc > DateTimeOffset.UtcNow)
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
