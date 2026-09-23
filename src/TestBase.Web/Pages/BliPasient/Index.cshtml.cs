using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.BliPasient;

/// <summary>
/// Offentlig, uautentisert side åpnet fra en behandlers ELLER en gruppes
/// QR-kode/lenke (se GruppeService, Behandlerportal/Grupper/Rediger og
/// Behandlerportal/MinSide) — se docs/beslutningslogg.md "Invitasjons- og
/// gruppesystem, fase 2". I MOTSETNING til Pages/PasientRegistrering/Fullfor
/// (som fullfører en FORHÅNDSOPPRETTET invitasjon) oppretter denne siden en
/// helt ny pasient i ÉTT steg — se PasientInvitasjonService.RegistrerViaQrAsync.
/// Route: /BliPasient/b/{token} (behandler) eller /BliPasient/g/{token} (gruppe).
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly GruppeService _grupper;
    private readonly PasientInvitasjonService _pasientService;
    private readonly TestTildelingsService _tildelingsService;
    private readonly IAuditLogger _auditLogger;

    public IndexModel(
        AppDbContext db, GruppeService grupper, PasientInvitasjonService pasientService,
        TestTildelingsService tildelingsService, IAuditLogger auditLogger)
    {
        _db = db;
        _grupper = grupper;
        _pasientService = pasientService;
        _tildelingsService = tildelingsService;
        _auditLogger = auditLogger;
    }

    [BindProperty(SupportsGet = true)]
    public string Type { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    // Nullable, IKKE string med default "" — ASP.NET Cores modellbinding konverterer
    // et TOMT skjemafelt til null for en string-property (se kjente fallgruver i
    // CLAUDE.md), aldri til "". Begge er valgfrie her (kun én av MobilNr/Epost kreves).
    [BindProperty]
    public string? MobilNr { get; set; }

    [BindProperty]
    public string? Epost { get; set; }

    [BindProperty]
    public bool GodtarLagringAvData { get; set; }

    // Bot-vern (se BotVern) — Nettside er et honeypot-felt som skal stå tomt. Ekstra
    // viktig her siden dette er den ENESTE helt åpne, ugatede skjema-siden i systemet
    // (verken invitasjon-token eller innlogging kreves før innsending).
    [BindProperty]
    public string? Nettside { get; set; }

    [BindProperty]
    public string Vist { get; set; } = string.Empty;

    public bool GyldigLenke { get; private set; }
    public bool Fullfort { get; private set; }
    public string? BehandlerNavn { get; private set; }
    public string? GruppeNavn { get; private set; }
    public string? Feilmelding { get; private set; }
    public string AvtaleTekst => PasientBrukeravtale.Tekst;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LastMaalAsync(cancellationToken))
        {
            Feilmelding = "Lenken er ugyldig.";
            return Page();
        }

        Vist = BotVern.NyttVisningstidspunkt();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await LastMaalAsync(cancellationToken))
        {
            Feilmelding = "Lenken er ugyldig.";
            return Page();
        }

        if (BotVern.ErSannsynligvisBot(Nettside, Vist))
        {
            Feilmelding = "Noe gikk galt. Prøv igjen.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(MobilNr) && string.IsNullOrWhiteSpace(Epost))
        {
            Feilmelding = "Oppgi enten mobilnummer eller e-post (minst én av dem).";
            return Page();
        }

        if (!GodtarLagringAvData)
        {
            Feilmelding = "Du må godta lagring av opplysningene dine for å fortsette.";
            return Page();
        }

        var (behandlerId, gruppeId) = await LosTokenAsync(cancellationToken);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // Personnummer samles bevisst IKKE inn her lenger (2026-09-23) — flyttet
        // utelukkende til PasientRegistrering/FullforProfil ("neste steg"), etter at
        // en reell konferanse viste at feltet her bremset/forvirret rask
        // selvregistrering. Blank personnummer er fortsatt "prøv systemet"-terskelen,
        // se PasientInvitasjonService.RegistrerViaQrAsync.
        var pasient = await _pasientService.RegistrerViaQrAsync(
            behandlerId!.Value, gruppeId, personnummer: null, MobilNr ?? string.Empty, Epost ?? string.Empty,
            GodtarLagringAvData, baseUrl, cancellationToken);

        if (gruppeId is not null)
        {
            var gruppeInnhold = await _grupper.HentMedTesterAsync(gruppeId.Value, cancellationToken);
            if (gruppeInnhold is not null && gruppeInnhold.Tester.Count > 0)
            {
                var resultat = await _tildelingsService.TildelOgVarsleAsync(
                    new List<long> { pasient.Id },
                    gruppeInnhold.Tester.Select(t => t.Id).ToList(),
                    behandlerId: behandlerId,
                    administratorId: null,
                    onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(),
                    baseUrl: baseUrl,
                    varslingsmetode: pasient.Varslingspreferanse,
                    cancellationToken: cancellationToken);

                // Pasienten står allerede midt i registreringen på egen enhet — send
                // rett videre til utfylling i stedet for å tvinge frem en omvei via
                // SMS/e-post-lenken (den sendes fortsatt, som reserve for senere
                // besøk fra en annen enhet). Innloggingen her er bevisst den samme
                // som BankID-innlogging ville satt opp (se AuthSignIn), siden
                // personnummeret allerede er egen-oppgitt i skjemaet over.
                var forsteTildelingId = resultat.PerPasient.FirstOrDefault()?.Lenker.FirstOrDefault()?.TildelingId;
                if (forsteTildelingId is not null)
                {
                    await AuthSignIn.LoggInnAsync(HttpContext, "pasient", pasient.Id, pasient.Navn ?? "Pasient", UserRole.Pasient, huskMeg: false);
                    await _auditLogger.LogAsync(
                        $"pasient:{pasient.Id}", nameof(UserRole.Pasient), "InnloggingOk",
                        nameof(Pasient), pasient.Id.ToString(), "QR-registrering (gruppe, rett til utfylling)", cancellationToken);
                    return RedirectToPage("/Tester/Fyll", new { area = "Pasientportal", id = forsteTildelingId });
                }
            }
        }

        Fullfort = true;
        return Page();
    }

    /// <summary>Slår opp behandler-/gruppenavn KUN til visning — selve token→id-oppløsningen skjer på nytt ved POST i LosTokenAsync (aldri stol på skjult skjemafelt for dette).</summary>
    private async Task<bool> LastMaalAsync(CancellationToken cancellationToken)
    {
        var (behandlerId, gruppeId) = await LosTokenAsync(cancellationToken);
        if (behandlerId is null)
        {
            return false;
        }

        GyldigLenke = true;
        return true;
    }

    private async Task<(long? BehandlerId, long? GruppeId)> LosTokenAsync(CancellationToken cancellationToken)
    {
        if (string.Equals(Type, "g", StringComparison.OrdinalIgnoreCase))
        {
            var gruppe = await _grupper.FinnVedQrTokenAsync(Token, cancellationToken);
            if (gruppe is null)
            {
                return (null, null);
            }

            GruppeNavn = gruppe.Navn;
            BehandlerNavn = (await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == gruppe.BehandlerId, cancellationToken))?.Visningsnavn;
            return (gruppe.BehandlerId, gruppe.Id);
        }

        if (string.Equals(Type, "b", StringComparison.OrdinalIgnoreCase))
        {
            var behandler = await _grupper.FinnBehandlerVedQrTokenAsync(Token, cancellationToken);
            if (behandler is null)
            {
                return (null, null);
            }

            BehandlerNavn = behandler.Visningsnavn;
            return (behandler.Id, null);
        }

        return (null, null);
    }
}
