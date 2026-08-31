using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestBase.Web.Areas.Admin.Pages;

/// <summary>
/// Admins oppgaveliste — placeholder inntil feedback-systemet bygges (se
/// beslutningsloggen "Meldinger og oppgaveliste"). Samme URL-mønster
/// (/Admin/Oppgaver) som de to andre rollene, slik at "Oppgaver" i
/// navigasjonen alltid peker til riktig sted uansett rolle.
/// </summary>
[Authorize(Policy = "AdminOmrade")]
public sealed class OppgaverModel : PageModel
{
    public void OnGet()
    {
    }
}
