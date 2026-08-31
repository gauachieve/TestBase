using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Administrasjon;

public sealed record PaaminnelseResultat(int AntallBehandlereVarslet);

/// <summary>
/// Daglig påminnelse til behandlere om fullførte tester som venter på
/// godkjenning (se TestTildeling.RapportGodkjentUtc) — jf. beslutningsloggen
/// "Meldinger og oppgaveliste". Kjøres enten fra DagligPaaminnelseBakgrunnstjeneste
/// (TestBase.Web, én gang i døgnet) eller manuelt fra
/// Behandlerportal/Innstillinger ("Send test-påminnelse nå").
///
/// VIKTIG personvernhensyn: meldingsteksten bruker ALDRI pasientnavn — kun
/// pasient-ID — siden SMS/e-post ikke er sikre kanaler. Fullt navn vises
/// først inne i systemet, etter innlogging (se lenken i meldingen, som går
/// via vanlig BankID+2FA-innlogging).
/// </summary>
public sealed class PaaminnelseService
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;

    public PaaminnelseService(AppDbContext db, TestService testService, ISmsSender sms, IEmailSender email)
    {
        _db = db;
        _testService = testService;
        _sms = sms;
        _email = email;
    }

    /// <summary>Alle behandlere som ønsker påminnelse og ikke allerede har fått en i dag (UTC-dato).</summary>
    public async Task<PaaminnelseResultat> SendPaaminnelserAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var kandidater = await _db.Behandlere
            .Where(b => b.OnskerDagligPaaminnelse && b.Status == BehandlerStatus.Aktiv)
            .ToListAsync(cancellationToken);

        var idagUtc = DateTimeOffset.UtcNow.Date;
        var behandlere = kandidater.Where(b => b.SistPaaminnetUtc is null || b.SistPaaminnetUtc.Value.Date < idagUtc).ToList();

        var antallVarslet = 0;
        foreach (var behandler in behandlere)
        {
            var sendt = await SendTilBehandlerAsync(behandler, baseUrl, cancellationToken);
            if (sendt)
            {
                antallVarslet++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new PaaminnelseResultat(antallVarslet);
    }

    /// <summary>Manuell/test-utsending for én behandler — ignorerer "allerede sendt i dag", men krever fortsatt at det finnes noe å varsle om.</summary>
    public async Task<bool> SendTilEnkeltBehandlerAsync(long behandlerId, string baseUrl, CancellationToken cancellationToken = default)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
        if (behandler is null)
        {
            return false;
        }

        var sendt = await SendTilBehandlerAsync(behandler, baseUrl, cancellationToken);
        if (sendt)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        return sendt;
    }

    private async Task<bool> SendTilBehandlerAsync(Behandler behandler, string baseUrl, CancellationToken cancellationToken)
    {
        var ugodkjente = await _testService.HentUgodkjenteFullforteForBehandlerAsync(behandler.Id, cancellationToken);
        if (ugodkjente.Count == 0)
        {
            return false;
        }

        var lenke = $"{baseUrl.TrimEnd('/')}/Behandlerportal/Oppgaver";
        var linjer = ugodkjente.Select(u => $"- Pasient {u.PasientId}: {u.TestNavn}");
        var melding =
            $"Du har {ugodkjente.Count} fullført(e) test(er) som venter på godkjenning:\n" +
            string.Join("\n", linjer) +
            $"\nSe oppgavelisten: {lenke}";

        var harMobil = !string.IsNullOrWhiteSpace(behandler.MobilNr);
        var harEpost = !string.IsNullOrWhiteSpace(behandler.Email);
        var vilSms = behandler.PaaminnelseKanal is Varslingspreferanse.Sms or Varslingspreferanse.Begge;
        var vilEpost = behandler.PaaminnelseKanal is Varslingspreferanse.Epost or Varslingspreferanse.Begge;

        if (vilSms && harMobil)
        {
            await _sms.SendAsync(behandler.MobilNr, melding, cancellationToken);
        }
        if (vilEpost && harEpost)
        {
            await _email.SendAsync(behandler.Email, "Ventende testrapporter i TestBase", melding, cancellationToken);
        }

        behandler.SistPaaminnetUtc = DateTimeOffset.UtcNow;
        return true;
    }
}
