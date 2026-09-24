using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Security;

/// <summary>Resultat av <see cref="ToFaktorService.StartAsync"/> — se <see cref="ToFaktorService.StartAsync"/> for hvorfor SendtSms kan være false uten at kallet selv kaster.</summary>
public sealed record ToFaktorStartResultat(string Kode, bool SendtSms);

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
    private readonly ILogger<ToFaktorService> _logger;

    public ToFaktorService(AppDbContext db, ISmsSender sms, ILogger<ToFaktorService> logger)
    {
        _db = db;
        _sms = sms;
        _logger = logger;
    }

    /// <summary>
    /// Returnerer den genererte koden slik at kalleren (Pages/Konto/LoggInn) kan
    /// vise den direkte i dev-UI-et — MockSmsSender logger den KUN til
    /// konsollen, som i praksis er ubrukelig for manuell nettleser-testing (se
    /// samme prinsipp for BehandlerInvitasjonResultat/PasientInvitasjonResultat).
    /// SMS-utsendingen er pakket i try/catch — et innloggingsforsøk (2FA er
    /// ESSENSIELT for å fullføre innloggingen, i motsetning til en fire-and-
    /// forget-varsling) skal ALDRI krasje med en 500-feil bare fordi
    /// SMS-leverandøren er nede/har lav saldo. Kalleren får i stedet
    /// SendtSms=false tilbake og kan vise en ærlig feilmelding — se reell
    /// hendelse (Vonage 402 "Low balance") i docs/beslutningslogg.md
    /// "2FA-SMS-utsending krasjet hele innloggingen".
    /// </summary>
    public async Task<ToFaktorStartResultat> StartAsync(ToFaktorPrincipalType principalType, long principalId, string mobilNr, CancellationToken cancellationToken = default)
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

        var sendtSms = true;
        try
        {
            await _sms.SendAsync(
                mobilNr,
                $"PsyTest-kode: {kode} (gyldig i {ToFaktorKodeLevetid.TotalMinutes:0} minutter).",
                cancellationToken);
        }
        catch (Exception ex)
        {
            sendtSms = false;
            _logger.LogError(ex, "2FA-SMS-utsending feilet for {PrincipalType} {PrincipalId} — innloggingen fortsetter uten SMS levert.", principalType, principalId);
        }

        return new ToFaktorStartResultat(kode, sendtSms);
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
