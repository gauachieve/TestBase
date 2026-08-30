using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.PasientRegistrering;

/// <summary>
/// Offentlig, uautentisert side en pasient åpner fra invitasjonslenken sin
/// for å fullføre egen registrering, jf. Del 4 i kravdokumentet. I motsetning
/// til behandler (Del 3) er det INGEN kontaktverifisering her — BankID-
/// innlogging etterpå er identitetsbekreftelsen.
/// </summary>
public sealed class FullforModel : PageModel
{
    private readonly PasientInvitasjonService _invitasjonService;
    private readonly AppDbContext _db;

    public FullforModel(PasientInvitasjonService invitasjonService, AppDbContext db)
    {
        _invitasjonService = invitasjonService;
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public string Personnummer { get; set; } = string.Empty;

    [BindProperty]
    public string MobilNr { get; set; } = string.Empty;

    [BindProperty]
    public string Epost { get; set; } = string.Empty;

    [BindProperty]
    public BiologiskKjonn? BiologiskKjonnVedFodsel { get; set; }

    [BindProperty]
    public Kjonnsidentitet? Kjonnsidentitet { get; set; }

    [BindProperty]
    public string? KjonnsidentitetSpesifisert { get; set; }

    [BindProperty]
    public string? Adresse { get; set; }

    [BindProperty]
    public bool GodtarLagringAvData { get; set; }

    [BindProperty]
    public bool GodtarMuligVippsBetaling { get; set; }

    [BindProperty]
    public Varslingspreferanse Varslingspreferanse { get; set; } = Varslingspreferanse.Begge;

    // Bot-vern (se BotVern) — Nettside er et honeypot-felt som skal stå tomt.
    [BindProperty]
    public string? Nettside { get; set; }

    [BindProperty]
    public string Vist { get; set; } = string.Empty;

    public bool GyldigInvitasjon { get; private set; }
    public bool Fullfort { get; private set; }
    public string? Feilmelding { get; private set; }
    public string AvtaleTekst => PasientBrukeravtale.Tekst;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var invitasjon = await _invitasjonService.FinnGyldigInvitasjonAsync(Token, cancellationToken);
        GyldigInvitasjon = invitasjon is not null;
        if (!GyldigInvitasjon)
        {
            Feilmelding = "Invitasjonslenken er ugyldig eller utløpt.";
            return Page();
        }

        var pasient = await _db.Pasienter.FirstAsync(p => p.Id == invitasjon!.PasientId, cancellationToken);
        Navn = pasient.Navn ?? string.Empty;
        MobilNr = pasient.MobilNr;
        Epost = pasient.Email;
        Vist = BotVern.NyttVisningstidspunkt();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var invitasjon = await _invitasjonService.FinnGyldigInvitasjonAsync(Token, cancellationToken);
        if (invitasjon is null)
        {
            Feilmelding = "Invitasjonslenken er ugyldig eller utløpt.";
            return Page();
        }

        GyldigInvitasjon = true;

        if (BotVern.ErSannsynligvisBot(Nettside, Vist))
        {
            Feilmelding = "Noe gikk galt. Prøv igjen.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Navn) || string.IsNullOrWhiteSpace(Personnummer) ||
            string.IsNullOrWhiteSpace(MobilNr) || string.IsNullOrWhiteSpace(Epost) ||
            BiologiskKjonnVedFodsel is null)
        {
            Feilmelding = "Navn, personnummer, mobilnummer, e-post og biologisk kjønn ved fødsel er obligatoriske.";
            return Page();
        }

        if (!GodtarLagringAvData || !GodtarMuligVippsBetaling)
        {
            Feilmelding = "Du må godta begge punktene i samtykkeavtalen for å fullføre registreringen.";
            return Page();
        }

        await _invitasjonService.FullforRegistreringAsync(
            invitasjon, Navn, Personnummer, MobilNr, Epost, BiologiskKjonnVedFodsel.Value,
            Kjonnsidentitet, KjonnsidentitetSpesifisert, Adresse,
            GodtarLagringAvData, GodtarMuligVippsBetaling, cancellationToken,
            varslingspreferanse: Varslingspreferanse);

        Fullfort = true;
        return Page();
    }
}
