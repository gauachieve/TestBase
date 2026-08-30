using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Inviterer nye behandlere (av en administrator ELLER en annen behandler,
/// jf. Del 3 i kravdokumentet) og lar dem fullføre egen registrering:
/// profilfelt + brukeravtale (<see cref="FullforProfilAsync"/>), deretter
/// bekreftelse av e-post- og mobilkode (<see cref="BekreftKontaktAsync"/>) før
/// kontoen blir Aktiv og administratorene varsles om å sjekke HPR-nummeret.
/// </summary>
/// <summary>
/// Returneres fra <see cref="BehandlerInvitasjonService.InviterAsync"/> — <c>Lenke</c> er
/// invitasjonslenken som (i mock-modus) kun logges via <c>ISmsSender</c>/<c>IEmailSender</c>,
/// ikke faktisk sendes. Kalleren viser den direkte i UI slik at man kan fullføre
/// invitasjonsflyten uten å måtte lete i konsoll-loggen.
/// </summary>
public sealed record BehandlerInvitasjonResultat(Behandler Behandler, string Lenke);

public sealed class BehandlerInvitasjonService
{
    private static readonly TimeSpan InvitasjonLevetid = TimeSpan.FromDays(7);
    private static readonly TimeSpan VerifiseringLevetid = TimeSpan.FromMinutes(10);
    private const int MaksForsokVerifisering = 5;

    private readonly AppDbContext _db;
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;

    public BehandlerInvitasjonService(AppDbContext db, ISmsSender sms, IEmailSender email)
    {
        _db = db;
        _sms = sms;
        _email = email;
    }

    public async Task<BehandlerInvitasjonResultat> InviterAsync(
        string? mobilNr,
        string? epost,
        long? administratorId,
        long? behandlerId,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mobilNr) && string.IsNullOrWhiteSpace(epost))
        {
            throw new ArgumentException("Må ha enten mobilnummer eller e-post for å invitere en behandler.");
        }

        if (administratorId is null == behandlerId is null)
        {
            throw new ArgumentException("Nøyaktig én av administratorId/behandlerId skal være satt.");
        }

        var behandler = new Behandler
        {
            MobilNr = mobilNr ?? string.Empty,
            Email = epost ?? string.Empty,
            Status = BehandlerStatus.Invitert,
            InvitertAvAdministratorId = administratorId,
            InvitertAvBehandlerId = behandlerId,
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
            OpprettetAvAdministratorId = administratorId,
            OpprettetAvBehandlerId = behandlerId
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

        return new BehandlerInvitasjonResultat(behandler, lenke);
    }

    public Task<BehandlerInvitasjon?> FinnGyldigInvitasjonAsync(string token, CancellationToken cancellationToken = default) =>
        _db.BehandlerInvitasjoner.FirstOrDefaultAsync(
            i => i.Token == token && i.BruktUtc == null && i.UtlopUtc > DateTimeOffset.UtcNow,
            cancellationToken);

    /// <summary>
    /// Lagrer profilfeltene fra egenregistreringsskjemaet + brukeravtale-aksept,
    /// og sender to verifiseringskoder (mobil + e-post). Kontoen er IKKE aktiv
    /// før begge kodene er bekreftet, se <see cref="BekreftKontaktAsync"/>.
    /// </summary>
    public async Task<Behandler> FullforProfilAsync(
        BehandlerInvitasjon invitasjon,
        string fornavn,
        string etternavn,
        string personnummer,
        string mobilNr,
        string epost,
        string hprNr,
        string kontonummer,
        string? arbeidsadresse,
        string? tittel,
        CancellationToken cancellationToken = default)
    {
        var behandler = await _db.Behandlere.FirstAsync(b => b.Id == invitasjon.BehandlerId, cancellationToken);
        behandler.Fornavn = fornavn;
        behandler.Etternavn = etternavn;
        behandler.Personnummer = personnummer;
        behandler.MobilNr = mobilNr;
        behandler.Email = epost;
        behandler.HprNr = hprNr;
        behandler.Kontonummer = kontonummer;
        behandler.Arbeidsadresse = arbeidsadresse;
        behandler.Tittel = tittel;
        behandler.BrukeravtaleGodkjentVersjon = Brukeravtale.GjeldendeVersjon;
        behandler.BrukeravtaleGodkjentUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await SendVerifiseringskodeAsync(behandler, KontaktMetode.Sms, cancellationToken);
        await SendVerifiseringskodeAsync(behandler, KontaktMetode.Epost, cancellationToken);

        return behandler;
    }

    /// <summary>
    /// Bekrefter BEGGE koder samtidig — ingen av dem markeres brukt før begge
    /// stemmer, slik at en riktig kode ikke "låses" hvis den andre var feil i
    /// samme innsending (bruker kan da prøve på nytt med begge).
    /// </summary>
    public async Task<bool> BekreftKontaktAsync(BehandlerInvitasjon invitasjon, string mobilKode, string epostKode, CancellationToken cancellationToken = default)
    {
        var behandlerId = invitasjon.BehandlerId;
        var mobilRad = await HentAktivKodeAsync(behandlerId, KontaktMetode.Sms, cancellationToken);
        var epostRad = await HentAktivKodeAsync(behandlerId, KontaktMetode.Epost, cancellationToken);

        if (mobilRad is null || epostRad is null ||
            mobilRad.Forsok >= MaksForsokVerifisering || epostRad.Forsok >= MaksForsokVerifisering)
        {
            return false;
        }

        mobilRad.Forsok++;
        epostRad.Forsok++;

        var mobilRiktig = mobilRad.KodeHash == HashKode(mobilKode);
        var epostRiktig = epostRad.KodeHash == HashKode(epostKode);

        if (!mobilRiktig || !epostRiktig)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return false;
        }

        var na = DateTimeOffset.UtcNow;
        mobilRad.BruktUtc = na;
        epostRad.BruktUtc = na;
        invitasjon.BruktUtc = na;

        var behandler = await _db.Behandlere.FirstAsync(b => b.Id == behandlerId, cancellationToken);
        behandler.MobilVerifisertUtc = na;
        behandler.EpostVerifisertUtc = na;
        behandler.RegistrertUtc = na;
        behandler.Status = BehandlerStatus.Aktiv;
        await _db.SaveChangesAsync(cancellationToken);

        await VarsleAdministratorerOmHprAsync(behandler, cancellationToken);
        return true;
    }

    private Task<BehandlerKontaktVerifisering?> HentAktivKodeAsync(long behandlerId, KontaktMetode kanal, CancellationToken cancellationToken) =>
        _db.BehandlerKontaktVerifiseringer
            .Where(k => k.BehandlerId == behandlerId && k.Kanal == kanal && k.BruktUtc == null && k.UtlopUtc > DateTimeOffset.UtcNow)
            .OrderByDescending(k => k.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task SendVerifiseringskodeAsync(Behandler behandler, KontaktMetode kanal, CancellationToken cancellationToken)
    {
        var kode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        _db.BehandlerKontaktVerifiseringer.Add(new BehandlerKontaktVerifisering
        {
            BehandlerId = behandler.Id,
            Kanal = kanal,
            KodeHash = HashKode(kode),
            UtlopUtc = DateTimeOffset.UtcNow.Add(VerifiseringLevetid)
        });
        await _db.SaveChangesAsync(cancellationToken);

        var melding = $"TestBase-bekreftelseskode: {kode} (gyldig i {VerifiseringLevetid.TotalMinutes:0} minutter).";

        if (kanal == KontaktMetode.Sms)
        {
            await _sms.SendAsync(behandler.MobilNr, melding, cancellationToken);
        }
        else
        {
            await _email.SendAsync(behandler.Email, "Bekreft kontaktinfo", melding, cancellationToken);
        }
    }

    private async Task VarsleAdministratorerOmHprAsync(Behandler behandler, CancellationToken cancellationToken)
    {
        var adminEposter = await _db.Administratorer.Where(a => !a.ErArkivert).Select(a => a.Email).ToListAsync(cancellationToken);
        const string emne = "Ny behandler venter HPR-godkjenning";
        var melding =
            $"Behandler {behandler.Visningsnavn} (HPR-nr {behandler.HprNr}) har fullført registrering og venter på " +
            $"at HPR-nummeret sjekkes i Helsepersonellregisteret. Godkjenn i Administratorer > Behandlere. " +
            $"7-dagers prøveperiode gjelder fra {behandler.RegistrertUtc:d}.";

        foreach (var epost in adminEposter)
        {
            await _email.SendAsync(epost, emne, melding, cancellationToken);
        }
    }

    private static string HashKode(string kode) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(kode)));
}
