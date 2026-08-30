using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;

namespace TestBase.Web.Areas.Admin.Pages.Tildel;

/// <summary>
/// Steg 1 av tildelingsflyten for admin (jf. beslutningsloggen
/// "Tildelingsflyt for tester") — samme flyt som behandlers, men admin ser
/// ALLE ikke-arkiverte pasienter på tvers av behandlere, se
/// TestTildelingsService.HentTilgjengeligePasienterAsync(null).
/// </summary>
public sealed class PasienterModel : PageModel
{
    private readonly TestTildelingsService _tildelingsService;

    public PasienterModel(TestTildelingsService tildelingsService)
    {
        _tildelingsService = tildelingsService;
    }

    public IReadOnlyList<PasientMedBehandlernavn> Pasienter { get; private set; } = Array.Empty<PasientMedBehandlernavn>();

    [BindProperty]
    public List<long> PasientIder { get; set; } = new();

    public string? Feilmelding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Pasienter = await _tildelingsService.HentTilgjengeligePasienterAsync(null, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (PasientIder.Count == 0)
        {
            Feilmelding = "Velg minst én pasient.";
            Pasienter = await _tildelingsService.HentTilgjengeligePasienterAsync(null, cancellationToken);
            return Page();
        }

        TempData["TildelPasientIder"] = string.Join(",", PasientIder.Distinct());
        return RedirectToPage("Tester");
    }
}
