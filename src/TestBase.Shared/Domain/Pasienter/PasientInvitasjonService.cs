using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// Returneres fra <see cref="PasientInvitasjonService.LeggTilAsync"/> — <c>Lenke</c> er
/// invitasjonslenken som (i mock-modus) kun logges via <c>ISmsSender</c>/<c>IEmailSender</c>,
/// ikke faktisk sendes. Kalleren viser den direkte i UI slik at man kan fullføre
/// invitasjonsflyten uten å måtte lete i konsoll-loggen.
/// </summary>
public sealed record PasientInvitasjonResultat(Pasient Pasient, string Lenke);

public sealed record GruppeimportResultat(IReadOnlyList<PasientInvitasjonResultat> Opprettet, IReadOnlyList<string> HoppetOverLinjer);

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
    private readonly GruppeService _grupper;

    public PasientInvitasjonService(AppDbContext db, ISmsSender sms, IEmailSender email, GruppeService grupper)
    {
        _db = db;
        _sms = sms;
        _email = email;
        _grupper = grupper;
    }

    public async Task<PasientInvitasjonResultat> LeggTilAsync(
        string? personnummer,
        string mobilNr,
        string epost,
        long behandlerId,
        KontaktMetode varslingskanal,
        string baseUrl,
        string? navn = null,
        long? gruppeId = null,
        CancellationToken cancellationToken = default)
    {
        var pasient = new Pasient
        {
            Personnummer = personnummer,
            MobilNr = mobilNr,
            Email = epost,
            Navn = navn,
            GruppeId = gruppeId,
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
        var melding = $"Du er invitert til å bruke PsyTest av din behandler. Fullfør registreringen din her: {lenke}";

        if (varslingskanal == KontaktMetode.Sms)
        {
            await _sms.SendAsync(kontaktVerdi, melding, cancellationToken);
        }
        else
        {
            await _email.SendAsync(kontaktVerdi, "Invitasjon til PsyTest", melding, cancellationToken);
        }

        return new PasientInvitasjonResultat(pasient, lenke);
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
        CancellationToken cancellationToken = default,
        Varslingspreferanse varslingspreferanse = Varslingspreferanse.Begge)
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
        pasient.Varslingspreferanse = varslingspreferanse;
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

    /// <summary>Personnummer er kryptert og kan derfor ikke håndheves unikt med en databaseindeks — se samme mønster i Behandlerportal/Pasienter/Rediger.cshtml.cs.</summary>
    public async Task<bool> HarAnnenPasientMedPersonnummerAsync(string personnummer, CancellationToken cancellationToken = default) =>
        (await _db.Pasienter.Where(p => p.Status != PasientStatus.Arkivert).ToListAsync(cancellationToken))
        .Any(p => p.Personnummer == personnummer);

    /// <summary>
    /// Ett-stegs egenregistrering via en QR-kode/lenke (se GruppeService sine
    /// QrToken-metoder og docs/beslutningslogg.md "Invitasjons- og
    /// gruppesystem, fase 2") — i MOTSETNING til <see cref="FullforRegistreringAsync"/>
    /// finnes det ingen forhåndsopprettet Pasient-rad eller PasientInvitasjon
    /// å fullføre: personen som skanner koden oppretter OG fullfører sin egen
    /// registrering i ett steg. <paramref name="gruppeId"/> null ved en ren
    /// behandler-QR (ingen gruppe, ingen automatisk testutsending).
    ///
    /// BEVISST MINIMALT skjema (2026-09-20, se beslutningsloggen "Forenklet
    /// QR-registrering") — kun mobilnr/e-post (minst én av dem) og valgfritt
    /// personnummer samles inn her, for lavest mulig terskel til å komme i
    /// gang. Navn/kjønn/adresse/Vipps-samtykke er BEVISST UTELATT — pasienten
    /// fyller dem inn senere via lenken sendt i <see cref="SendFullforProfilLenkeAsync"/>,
    /// etter at de allerede har fått sin første test. Personnummer værende
    /// blankt er ikke en feiltilstand — det er selve poenget med "prøv
    /// systemet"-terskelen; se Pasient.ProfilFullforingToken.
    /// </summary>
    public async Task<Pasient> RegistrerViaQrAsync(
        long behandlerId,
        long? gruppeId,
        string? personnummer,
        string mobilNr,
        string epost,
        bool godtarLagringAvData,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var harMobil = !string.IsNullOrWhiteSpace(mobilNr);
        var harEpost = !string.IsNullOrWhiteSpace(epost);
        var varslingspreferanse = (harMobil, harEpost) switch
        {
            (true, true) => Varslingspreferanse.Begge,
            (true, false) => Varslingspreferanse.Sms,
            _ => Varslingspreferanse.Epost
        };

        var pasient = new Pasient
        {
            Personnummer = string.IsNullOrWhiteSpace(personnummer) ? null : personnummer,
            MobilNr = mobilNr,
            Email = epost,
            Varslingspreferanse = varslingspreferanse,
            BrukeravtaleGodkjentVersjon = PasientBrukeravtale.GjeldendeVersjon,
            BrukeravtaleGodkjentUtc = DateTimeOffset.UtcNow,
            GodtarLagringAvData = godtarLagringAvData,
            RegistrertUtc = DateTimeOffset.UtcNow,
            Status = PasientStatus.Aktiv,
            BehandlerId = behandlerId,
            GruppeId = gruppeId,
            ProfilFullforingToken = RandomNumberGenerator.GetHexString(32),
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        _db.Pasienter.Add(pasient);
        await _db.SaveChangesAsync(cancellationToken);

        await SendFullforProfilLenkeAsync(pasient, baseUrl, cancellationToken);
        return pasient;
    }

    /// <summary>
    /// Sender en "fullfør profilen din"-påminnelse til enhver kontaktkanal
    /// pasienten faktisk oppga ved QR-registrering — bevisst en myk oppfordring
    /// (de kan allerede bruke systemet/har fått sin første test), ikke et krav.
    /// </summary>
    private async Task SendFullforProfilLenkeAsync(Pasient pasient, string baseUrl, CancellationToken cancellationToken)
    {
        var lenke = $"{baseUrl.TrimEnd('/')}/PasientRegistrering/FullforProfil/{pasient.ProfilFullforingToken}";
        var melding = $"Velkommen til PsyTest! Du kan bruke systemet allerede nå. Når du har tid, fullfør profilen din (navn, kontaktinfo, personnummer) her: {lenke}";

        if (!string.IsNullOrWhiteSpace(pasient.MobilNr))
        {
            await _sms.SendAsync(pasient.MobilNr, melding, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(pasient.Email))
        {
            await _email.SendAsync(pasient.Email, "Fullfør profilen din i PsyTest", melding, cancellationToken);
        }
    }

    /// <summary>Se Pages/PasientRegistrering/FullforProfil — IKKE et engangstoken, se Pasient.ProfilFullforingToken.</summary>
    public Task<Pasient?> FinnVedProfilFullforingTokenAsync(string token, CancellationToken cancellationToken = default) =>
        _db.Pasienter.FirstOrDefaultAsync(p => p.ProfilFullforingToken == token && p.Status != PasientStatus.Arkivert, cancellationToken);

    /// <summary>Genererer token første gang den mangler (f.eks. en pasient fra den eldre behandler-invitasjonsflyten, ikke QR) — ellers returnerer eksisterende. Sender INGEN melding — se PaaminnFullforingAsync for det.</summary>
    public async Task<string> SikreProfilFullforingTokenAsync(Pasient pasient, CancellationToken cancellationToken = default)
    {
        if (pasient.ProfilFullforingToken is null)
        {
            pasient.ProfilFullforingToken = RandomNumberGenerator.GetHexString(32);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return pasient.ProfilFullforingToken;
    }

    /// <summary>
    /// Behandlers "Påminn fullføring"-knapp (Behandlerportal/Pasienter/Rediger) —
    /// sender SAMME påminnelse som ved QR-registrering, men kan trigges når som
    /// helst senere, og genererer et token først hvis pasienten aldri fikk ett.
    /// </summary>
    public async Task PaaminnFullforingAsync(Pasient pasient, string baseUrl, CancellationToken cancellationToken = default)
    {
        await SikreProfilFullforingTokenAsync(pasient, cancellationToken);
        await SendFullforProfilLenkeAsync(pasient, baseUrl, cancellationToken);
    }

    /// <summary>
    /// Fullfører de feltene som BEVISST ble hoppet over ved selve QR-registreringen
    /// (se RegistrerViaQrAsync) — navn, kjønn, adresse, og (om pasienten nå ønsker
    /// det) personnummer og/eller den andre kontaktkanalen. Personnummer valideres
    /// på nytt her (format + unikhet) siden det nå først kan bli satt.
    /// </summary>
    public async Task OppdaterProfilAsync(
        Pasient pasient,
        string? navn,
        string? personnummer,
        string mobilNr,
        string epost,
        BiologiskKjonn? biologiskKjonnVedFodsel,
        Kjonnsidentitet? kjonnsidentitet,
        string? kjonnsidentitetSpesifisert,
        string? adresse,
        CancellationToken cancellationToken = default)
    {
        pasient.Navn = navn;
        pasient.Personnummer = string.IsNullOrWhiteSpace(personnummer) ? pasient.Personnummer : personnummer;
        pasient.MobilNr = mobilNr;
        pasient.Email = epost;
        pasient.BiologiskKjonnVedFodsel = biologiskKjonnVedFodsel;
        pasient.Kjonnsidentitet = kjonnsidentitet;
        pasient.KjonnsidentitetSpesifisert = kjonnsidentitet == Kjonnsidentitet.Annet ? kjonnsidentitetSpesifisert : null;
        pasient.Adresse = adresse;
        pasient.Varslingspreferanse = (!string.IsNullOrWhiteSpace(mobilNr), !string.IsNullOrWhiteSpace(epost)) switch
        {
            (true, true) => Varslingspreferanse.Begge,
            (true, false) => Varslingspreferanse.Sms,
            _ => Varslingspreferanse.Epost
        };

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Parser kommaseparerte linjer "gruppenavn,navn,email,sms,pnr" (jf. kravet
    /// ordrett). Linjer som ikke har alle fem feltene hoppes over og rapporteres
    /// tilbake til brukeren i stedet for å feile stille.
    /// </summary>
    public async Task<GruppeimportResultat> ImporterGruppeAsync(string kommasepartListe, long behandlerId, string baseUrl, CancellationToken cancellationToken = default)
    {
        var opprettet = new List<PasientInvitasjonResultat>();
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
            var gruppe = await _grupper.FinnEllerOpprettAsync(gruppenavn, behandlerId, cancellationToken);
            var varslingskanal = !string.IsNullOrWhiteSpace(mobil) ? KontaktMetode.Sms : KontaktMetode.Epost;
            var resultat = await LeggTilAsync(personnummer, mobil, epost, behandlerId, varslingskanal, baseUrl, navn, gruppe.Id, cancellationToken);
            opprettet.Add(resultat);
        }

        return new GruppeimportResultat(opprettet, hoppetOver);
    }
}
