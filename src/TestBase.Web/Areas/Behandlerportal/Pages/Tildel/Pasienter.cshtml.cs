using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Tildel;

/// <summary>
/// Steg 1 av tildelingsflyten (jf. beslutningsloggen "Tildelingsflyt for
/// tester"): behandler velger én eller flere av sine egne, ikke-arkiverte
/// pasienter. Valget bæres videre til Tester.cshtml via TempData (kun en
/// kommaseparert streng med id-er — se CLAUDE.md-fallgruven om at TempData
/// ikke støtter long direkte).
/// </summary>
public sealed class PasienterModel : PageModel
{
    private readonly TestTildelingsService _tildelingsService;
    private readonly ICurrentUserContext _currentUser;

    public PasienterModel(TestTildelingsService tildelingsService, ICurrentUserContext currentUser)
    {
        _tildelingsService = tildelingsService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<PasientMedBehandlernavn> Pasienter { get; private set; } = Array.Empty<PasientMedBehandlernavn>();

    [BindProperty]
    public List<long> PasientIder { get; set; } = new();

    public string? Feilmelding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Pasienter = await _tildelingsService.HentTilgjengeligePasienterAsync(HentBehandlerId(), cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (PasientIder.Count == 0)
        {
            Feilmelding = "Velg minst én pasient.";
            Pasienter = await _tildelingsService.HentTilgjengeligePasienterAsync(HentBehandlerId(), cancellationToken);
            return Page();
        }

        TempData["TildelPasientIder"] = string.Join(",", PasientIder.Distinct());
        return RedirectToPage("Tester");
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
