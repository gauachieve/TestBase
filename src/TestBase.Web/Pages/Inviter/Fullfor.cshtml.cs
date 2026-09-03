using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.Inviter;

/// <summary>
/// Offentlig, uautentisert side en behandler åpner fra invitasjonslenken sin
/// for å fullføre egen registrering: profilfelt + brukeravtale-aksept, jf.
/// Del 3 i kravdokumentet. Ved suksess sendes to verifiseringskoder
/// (mobil + e-post), og brukeren sendes videre til Verifiser-siden.
/// </summary>
public sealed class FullforModel : PageModel
{
    private readonly BehandlerInvitasjonService _invitasjonService;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public FullforModel(BehandlerInvitasjonService invitasjonService, AppDbContext db, IWebHostEnvironment env)
    {
        _invitasjonService = invitasjonService;
        _db = db;
        _env = env;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public string Fornavn { get; set; } = string.Empty;

    [BindProperty]
    public string Etternavn { get; set; } = string.Empty;

    [BindProperty]
    public string Personnummer { get; set; } = string.Empty;

    [BindProperty]
    public string MobilNr { get; set; } = string.Empty;

    [BindProperty]
    public string Epost { get; set; } = string.Empty;

    [BindProperty]
    public string HprNr { get; set; } = string.Empty;

    [BindProperty]
    public string Kontonummer { get; set; } = string.Empty;

    [BindProperty]
    public string? Arbeidsadresse { get; set; }

    [BindProperty]
    public string? Tittel { get; set; }

    [BindProperty]
    public bool GodtarAvtale { get; set; }

    // Bot-vern (se BotVern) — Nettside er et honeypot-felt som skal stå tomt.
    [BindProperty]
    public string? Nettside { get; set; }

    [BindProperty]
    public string Vist { get; set; } = string.Empty;

    public bool GyldigInvitasjon { get; private set; }
    public string? Feilmelding { get; private set; }
    public string AvtaleTekst => Brukeravtale.Tekst;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var invitasjon = await _invitasjonService.FinnGyldigInvitasjonAsync(Token, cancellationToken);
        GyldigInvitasjon = invitasjon is not null;
        if (!GyldigInvitasjon)
        {
            Feilmelding = "Invitasjonslenken er ugyldig eller utløpt.";
            return Page();
        }

        var behandler = await _db.Behandlere.FirstAsync(b => b.Id == invitasjon!.BehandlerId, cancellationToken);
        MobilNr = behandler.MobilNr;
        Epost = behandler.Email;
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
            // Later som skjemaet ble tatt imot, uten å faktisk behandle det — gir
            // ikke bort at det ble oppdaget som (sannsynligvis) automatisert.
            Feilmelding = "Noe gikk galt. Prøv igjen.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Fornavn) || string.IsNullOrWhiteSpace(Etternavn) ||
            string.IsNullOrWhiteSpace(Personnummer) || string.IsNullOrWhiteSpace(MobilNr) ||
            string.IsNullOrWhiteSpace(Epost) || string.IsNullOrWhiteSpace(HprNr) ||
            string.IsNullOrWhiteSpace(Kontonummer))
        {
            Feilmelding = "Alle felt unntatt arbeidsadresse og tittel er obligatoriske.";
            return Page();
        }

        if (!GodtarAvtale)
        {
            Feilmelding = "Du må godta brukeravtalen for å fullføre registreringen.";
            return Page();
        }

        var (_, mobilKode) = await _invitasjonService.FullforProfilAsync(
            invitasjon, Fornavn, Etternavn, Personnummer, MobilNr, Epost, HprNr, Kontonummer,
            Arbeidsadresse, Tittel, cancellationToken);

        if (_env.IsDevelopment())
        {
            TempData["DevMobilKode"] = mobilKode;
        }

        return RedirectToPage("Verifiser", new { token = Token });
    }
}
