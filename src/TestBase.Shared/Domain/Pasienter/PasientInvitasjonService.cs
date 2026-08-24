using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Pasienter;

public sealed record GruppeimportResultat(IReadOnlyList<Pasient> Opprettet, IReadOnlyList<string> HoppetOverLinjer);

/// <summary>
/// Behandlers pasientadministrasjon: legge til enkeltpasienter eller
/// importere en gruppe (jf. Del 3 i kravdokumentet), og pasientens egen
/// fullføring av registreringen (jf. Del 4 — se
/// <see cref="FullforRegistreringAsync"/>). Sender invitasjon via mock
/// SMS/e-post med lenke til fullføringssiden.
/// </summary>
public sealed class PasientInvitasjonService
{
    private static readonly TimeSpan InvitasjonLevetid = TimeSpan.FromDays(7);

    private readonly AppDbContext _db;
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;

    public PasientInvitasjonService(AppDbContext db, ISmsSender sms, IEmailSender email)
    {
        _db = db;
        _sms = sms;
        _email = email;
    }

    public async Task<Pasient> LeggTilAsync(
        string personnummer,
        string mobilNr,
        string epost,
        long behandlerId,
        KontaktMetode varslingskanal,
        string baseUrl,
        string? navn = null,
        string? gruppenavn = null,
        CancellationToken cancellationToken = default)
    {
        var pasient = new Pasient
        {
            Personnummer = personnummer,
            MobilNr = mobilNr,
            Email = epost,
            Navn = navn,
            Gruppenavn = gruppenavn,
            BehandlerId = behandlerId,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        _db.Pasienter.Add(pasient);
        await _db.SaveChangesAsync(cancellationToken);

        var token = RandomNumberGenerator.GetHexString(40);
        var kontaktVerdi = varslingskanal == KontaktMetode.Sms ? mobilNr : epost;

        _db.PasientInvitasjoner.Add(new PasientInvitasjon
        {
            PasientId = pasient.Id,
            Token = token,
            KontaktMetode = varslingskanal,
            KontaktVerdi = kontaktVerdi,
            UtlopUtc = DateTimeOffset.UtcNow.Add(InvitasjonLevetid),
            OpprettetAvBehandlerId = behandlerId
        });
        await _db.SaveChangesAsync(cancellationToken);

        var lenke = $"{baseUrl.TrimEnd('/')}/PasientRegistrering/Fullfor/{token}";
        var melding = $"Du er invitert til å bruke TestBase av din behandler. Fullfør registreringen din her: {lenke}";

        if (varslingskanal == KontaktMetode.Sms)
        {
            await _sms.SendAsync(kontaktVerdi, melding, cancellationToken);
        }
        else
        {
            await _email.SendAsync(kontaktVerdi, "Invitasjon til TestBase", melding, cancellationToken);
        }

        return pasient;
    }

    public Task<PasientInvitasjon?> FinnGyldigInvitasjonAsync(string token, CancellationToken cancellationToken = default) =>
        _db.PasientInvitasjoner.FirstOrDefaultAsync(
            i => i.Token == token && i.BruktUtc == null && i.UtlopUtc > DateTimeOffset.UtcNow,
            cancellationToken);

    /// <summary>
    /// Pasientens egen fullføring av registreringen (Del 4) — i motsetning til
    /// behandler (Del 3) er det INGEN egen kontaktverifisering (SMS/e-post-kode)
    /// her; BankID-innlogging etterpå er identitetsbekreftelsen.
    /// </summary>
    public async Task<Pasient> FullforRegistreringAsync(
        PasientInvitasjon invitasjon,
        string navn,
        string personnummer,
        string mobilNr,
        string epost,
        BiologiskKjonn biologiskKjonnVedFodsel,
        Kjonnsidentitet? kjonnsidentitet,
        string? kjonnsidentitetSpesifisert,
        string? adresse,
        bool godtarLagringAvData,
        bool godtarMuligVippsBetaling,
        CancellationToken cancellationToken = default)
    {
        var pasient = await _db.Pasienter.FirstAsync(p => p.Id == invitasjon.PasientId, cancellationToken);
        pasient.Navn = navn;
        pasient.Personnummer = personnummer;
        pasient.MobilNr = mobilNr;
        pasient.Email = epost;
        pasient.BiologiskKjonnVedFodsel = biologiskKjonnVedFodsel;
        pasient.Kjonnsidentitet = kjonnsidentitet;
        pasient.KjonnsidentitetSpesifisert = kjonnsidentitet == Kjonnsidentitet.Annet ? kjonnsidentitetSpesifisert : null;
        pasient.Adresse = adresse;
        pasient.BrukeravtaleGodkjentVersjon = PasientBrukeravtale.GjeldendeVersjon;
        pasient.BrukeravtaleGodkjentUtc = DateTimeOffset.UtcNow;
        pasient.GodtarLagringAvData = godtarLagringAvData;
        pasient.GodtarMuligVippsBetaling = godtarMuligVippsBetaling;
        pasient.RegistrertUtc = DateTimeOffset.UtcNow;
        pasient.Status = PasientStatus.Aktiv;

        invitasjon.BruktUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return pasient;
    }

    /// <summary>
    /// Parser kommaseparerte linjer "gruppenavn,navn,email,sms,pnr" (jf. kravet
    /// ordrett). Linjer som ikke har alle fem feltene hoppes over og rapporteres
    /// tilbake til brukeren i stedet for å feile stille.
    /// </summary>
    public async Task<GruppeimportResultat> ImporterGruppeAsync(string kommasepartListe, long behandlerId, string baseUrl, CancellationToken cancellationToken = default)
    {
        var opprettet = new List<Pasient>();
        var hoppetOver = new List<string>();

        var linjer = kommasepartListe.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var linje in linjer)
        {
            var deler = linje.Split(',', StringSplitOptions.TrimEntries);
            if (deler.Length < 5 || deler.Any(string.IsNullOrWhiteSpace))
            {
                hoppetOver.Add(linje);
                continue;
            }

            var (gruppenavn, navn, epost, mobil, personnummer) = (deler[0], deler[1], deler[2], deler[3], deler[4]);
            var varslingskanal = !string.IsNullOrWhiteSpace(mobil) ? KontaktMetode.Sms : KontaktMetode.Epost;
            var pasient = await LeggTilAsync(personnummer, mobil, epost, behandlerId, varslingskanal, baseUrl, navn, gruppenavn, cancellationToken);
            opprettet.Add(pasient);
        }

        return new GruppeimportResultat(opprettet, hoppetOver);
    }
}
