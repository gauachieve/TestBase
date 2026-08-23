using System.Security.Cryptography;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Pasienter;

public sealed record GruppeimportResultat(IReadOnlyList<Pasient> Opprettet, IReadOnlyList<string> HoppetOverLinjer);

/// <summary>
/// Behandlers pasientadministrasjon: legge til enkeltpasienter eller
/// importere en gruppe (jf. Del 3 i kravdokumentet). Sender en invitasjon via
/// mock SMS/e-post, men BEVISST uten lenke — pasientens egen fullføringsside
/// er Del 4 og finnes ikke ennå (se PasientInvitasjon).
/// </summary>
public sealed class PasientInvitasjonService
{
    private static readonly TimeSpan InvitasjonLevetid = TimeSpan.FromDays(7);
    private const string VenterPaaRegistreringMelding =
        "Du er invitert til å bruke TestBase av din behandler. Registrering for pasienter kommer i en senere fase — du vil da motta en lenke på dette kontaktpunktet.";

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

        if (varslingskanal == KontaktMetode.Sms)
        {
            await _sms.SendAsync(kontaktVerdi, VenterPaaRegistreringMelding, cancellationToken);
        }
        else
        {
            await _email.SendAsync(kontaktVerdi, "Invitasjon til TestBase", VenterPaaRegistreringMelding, cancellationToken);
        }

        return pasient;
    }

    /// <summary>
    /// Parser kommaseparerte linjer "gruppenavn,navn,email,sms,pnr" (jf. kravet
    /// ordrett). Linjer som ikke har alle fem feltene hoppes over og rapporteres
    /// tilbake til brukeren i stedet for å feile stille.
    /// </summary>
    public async Task<GruppeimportResultat> ImporterGruppeAsync(string kommasepartListe, long behandlerId, CancellationToken cancellationToken = default)
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
            var pasient = await LeggTilAsync(personnummer, mobil, epost, behandlerId, varslingskanal, navn, gruppenavn, cancellationToken);
            opprettet.Add(pasient);
        }

        return new GruppeimportResultat(opprettet, hoppetOver);
    }
}
