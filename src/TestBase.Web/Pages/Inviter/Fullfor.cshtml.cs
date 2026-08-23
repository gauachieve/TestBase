using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Web.Pages.Inviter;

/// <summary>
/// Offentlig, uautentisert side en behandler åpner fra invitasjonslenken sin
/// for å fullføre egne stamdata (fullt navn, HPR-nr). Selve behandler-
/// innlogging/-portal er Del 3 og finnes ikke ennå.
/// </summary>
public sealed class FullforModel : PageModel
{
    private readonly BehandlerInvitasjonService _invitasjonService;

    public FullforModel(BehandlerInvitasjonService invitasjonService)
    {
        _invitasjonService = invitasjonService;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public string FulltNavn { get; set; } = string.Empty;

    [BindProperty]
    public string HprNr { get; set; } = string.Empty;

    public bool GyldigInvitasjon { get; private set; }
    public bool Fullfort { get; private set; }
    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var invitasjon = await _invitasjonService.FinnGyldigInvitasjonAsync(Token, cancellationToken);
        GyldigInvitasjon = invitasjon is not null;
        if (!GyldigInvitasjon)
        {
            Feilmelding = "Invitasjonslenken er ugyldig eller utløpt.";
        }

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

        if (string.IsNullOrWhiteSpace(FulltNavn) || string.IsNullOrWhiteSpace(HprNr))
        {
            Feilmelding = "Fullt navn og HPR-nummer må fylles ut.";
            return Page();
        }

        await _invitasjonService.FullforAsync(invitasjon, FulltNavn, HprNr, cancellationToken);
        Fullfort = true;

        return Page();
    }
}
