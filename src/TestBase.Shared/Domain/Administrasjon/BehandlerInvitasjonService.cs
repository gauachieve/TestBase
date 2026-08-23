using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Inviterer nye behandlere og lar dem fullføre egne stamdata via en
/// tidsbegrenset, engangs invitasjonslenke — jf. Del 2 i kravdokumentet.
/// Selve behandler-innlogging/-portal er Del 3 og bygges ikke her.
/// </summary>
public sealed class BehandlerInvitasjonService
{
    private static readonly TimeSpan InvitasjonLevetid = TimeSpan.FromDays(7);

    private readonly AppDbContext _db;
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;

    public BehandlerInvitasjonService(AppDbContext db, ISmsSender sms, IEmailSender email)
    {
        _db = db;
        _sms = sms;
        _email = email;
    }

    public async Task<Behandler> InviterAsync(
        string? mobilNr,
        string? epost,
        long administratorId,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mobilNr) && string.IsNullOrWhiteSpace(epost))
        {
            throw new ArgumentException("Må ha enten mobilnummer eller e-post for å invitere en behandler.");
        }

        var behandler = new Behandler
        {
            MobilNr = mobilNr ?? string.Empty,
            Email = epost ?? string.Empty,
            Status = BehandlerStatus.Invitert,
            InvitertAvAdministratorId = administratorId,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        _db.Behandlere.Add(behandler);
        await _db.SaveChangesAsync(cancellationToken);

        var kontaktMetode = !string.IsNullOrWhiteSpace(mobilNr) ? KontaktMetode.Sms : KontaktMetode.Epost;
        var kontaktVerdi = kontaktMetode == KontaktMetode.Sms ? mobilNr! : epost!;
        var token = RandomNumberGenerator.GetHexString(40);

        _db.BehandlerInvitasjoner.Add(new BehandlerInvitasjon
        {
            BehandlerId = behandler.Id,
            Token = token,
            KontaktMetode = kontaktMetode,
            KontaktVerdi = kontaktVerdi,
            UtlopUtc = DateTimeOffset.UtcNow.Add(InvitasjonLevetid),
            OpprettetAvAdministratorId = administratorId
        });
        await _db.SaveChangesAsync(cancellationToken);

        var lenke = $"{baseUrl.TrimEnd('/')}/Inviter/Fullfor/{token}";
        var melding = $"Du er invitert som behandler i TestBase. Fullfør registreringen din her: {lenke}";

        if (kontaktMetode == KontaktMetode.Sms)
        {
            await _sms.SendAsync(kontaktVerdi, melding, cancellationToken);
        }
        else
        {
            await _email.SendAsync(kontaktVerdi, "Invitasjon til TestBase", melding, cancellationToken);
        }

        return behandler;
    }

    public Task<BehandlerInvitasjon?> FinnGyldigInvitasjonAsync(string token, CancellationToken cancellationToken = default) =>
        _db.BehandlerInvitasjoner.FirstOrDefaultAsync(
            i => i.Token == token && i.BruktUtc == null && i.UtlopUtc > DateTimeOffset.UtcNow,
            cancellationToken);

    public async Task FullforAsync(
        BehandlerInvitasjon invitasjon,
        string fulltNavn,
        string hprNr,
        CancellationToken cancellationToken = default)
    {
        var behandler = await _db.Behandlere.FirstAsync(b => b.Id == invitasjon.BehandlerId, cancellationToken);
        behandler.FulltNavn = fulltNavn;
        behandler.HprNr = hprNr;
        behandler.Status = BehandlerStatus.Aktiv;
        invitasjon.BruktUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
