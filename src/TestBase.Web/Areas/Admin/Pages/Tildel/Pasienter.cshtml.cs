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
    private readonly TestService _testService;

    public PasienterModel(TestTildelingsService tildelingsService, TestService testService)
    {
        _tildelingsService = tildelingsService;
        _testService = testService;
    }

    public IReadOnlyList<PasientMedBehandlernavn> Pasienter { get; private set; } = Array.Empty<PasientMedBehandlernavn>();

    [BindProperty]
    public List<long> PasientIder { get; set; } = new();

    public string? Feilmelding { get; private set; }

    /// <summary>Bugliste 2026-10-11, oppfølging — se Behandlerportal-motstykket for full begrunnelse.</summary>
    public IReadOnlyList<TestService.TildelingMedTestOgPasient> IkkeFullforteTildelinger { get; private set; } = Array.Empty<TestService.TildelingMedTestOgPasient>();
    public string? PaaminnelseMelding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PaaminnelseMelding = TempData["PaaminnelseMelding"] as string;
        IkkeFullforteTildelinger = await _testService.HentIkkeFullforteForAdminAsync(cancellationToken);
        Pasienter = await _tildelingsService.HentTilgjengeligePasienterAsync(null, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (PasientIder.Count == 0)
        {
            Feilmelding = "Velg minst én pasient.";
            Pasienter = await _tildelingsService.HentTilgjengeligePasienterAsync(null, cancellationToken);
            IkkeFullforteTildelinger = await _testService.HentIkkeFullforteForAdminAsync(cancellationToken);
            return Page();
        }

        TempData["TildelPasientIder"] = string.Join(",", PasientIder.Distinct());
        return RedirectToPage("Tester");
    }

    /// <summary>Se Behandlerportal-motstykket. behandlerId: null = admin, ingen eierskapssjekk.</summary>
    public async Task<IActionResult> OnPostPaaminnAsync(long tildelingId, CancellationToken cancellationToken)
    {
        var sendt = await _tildelingsService.PaaminnUbesvartTestAsync(
            tildelingId, baseUrl: $"{Request.Scheme}://{Request.Host}", behandlerId: null, cancellationToken);
        TempData["PaaminnelseMelding"] = sendt ? "Påminnelse sendt." : "Kunne ikke sende påminnelse (tildelingen er allerede fullført).";
        return Redirect(Url.Page("./Pasienter")! + "#tildelte");
    }

    public async Task<IActionResult> OnPostSlettIkkeFullfortAsync(long tildelingId, CancellationToken cancellationToken)
    {
        await _testService.SlettIkkeFullfortTildelingAsync(tildelingId, behandlerId: null, cancellationToken);
        return Redirect(Url.Page("./Pasienter")! + "#tildelte");
    }

    /// <summary>Bugliste 2026-10-11, oppfølging del 3 — multiselect-variant av OnPostPaaminnAsync.</summary>
    public async Task<IActionResult> OnPostPaaminnValgteAsync(List<long> tildelingId, CancellationToken cancellationToken)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var antallSendt = 0;
        foreach (var id in tildelingId.Distinct())
        {
            if (await _tildelingsService.PaaminnUbesvartTestAsync(id, baseUrl, behandlerId: null, cancellationToken))
            {
                antallSendt++;
            }
        }
        TempData["PaaminnelseMelding"] = $"Påminnelse sendt for {antallSendt} av {tildelingId.Distinct().Count()} valgte tildelinger.";
        return Redirect(Url.Page("./Pasienter")! + "#tildelte");
    }

    /// <summary>Bugliste 2026-10-11, oppfølging del 3 — multiselect-variant av OnPostSlettIkkeFullfortAsync.</summary>
    public async Task<IActionResult> OnPostSlettValgteAsync(List<long> tildelingId, CancellationToken cancellationToken)
    {
        foreach (var id in tildelingId.Distinct())
        {
            await _testService.SlettIkkeFullfortTildelingAsync(id, behandlerId: null, cancellationToken);
        }
        return Redirect(Url.Page("./Pasienter")! + "#tildelte");
    }
}
