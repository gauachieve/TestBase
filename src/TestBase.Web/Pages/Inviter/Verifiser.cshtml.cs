using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Web.Pages.Inviter;

/// <summary>
/// Offentlig, uautentisert side hvor behandleren taster de to
/// verifiseringskodene (mobil + e-post) som ble sendt fra Fullfor-siden.
/// Rutet på invitasjonstoken (ikke behandler-id) for å ikke eksponere
/// sekvensielle databaseId-er offentlig.
/// </summary>
public sealed class VerifiserModel : PageModel
{
    private readonly BehandlerInvitasjonService _invitasjonService;

    public VerifiserModel(BehandlerInvitasjonService invitasjonService)
    {
        _invitasjonService = invitasjonService;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public string MobilKode { get; set; } = string.Empty;

    [BindProperty]
    public string EpostKode { get; set; } = string.Empty;

    public bool GyldigInvitasjon { get; private set; }
    public bool Fullfort { get; private set; }
    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        GyldigInvitasjon = await _invitasjonService.FinnGyldigInvitasjonAsync(Token, cancellationToken) is not null;
        if (!GyldigInvitasjon)
        {
            Feilmelding = "Lenken er ugyldig eller utløpt. Be om en ny invitasjon fra administrator/kollega.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var invitasjon = await _invitasjonService.FinnGyldigInvitasjonAsync(Token, cancellationToken);
        if (invitasjon is null)
        {
            Feilmelding = "Lenken er ugyldig eller utløpt. Be om en ny invitasjon fra administrator/kollega.";
            return Page();
        }

        GyldigInvitasjon = true;

        var bekreftet = await _invitasjonService.BekreftKontaktAsync(invitasjon, MobilKode, EpostKode, cancellationToken);
        if (!bekreftet)
        {
            Feilmelding = "Én eller begge kodene var feil eller utløpt.";
            return Page();
        }

        Fullfort = true;
        return Page();
    }
}
