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
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;

    public PasienterModel(TestTildelingsService tildelingsService, TestService testService, ICurrentUserContext currentUser)
    {
        _tildelingsService = tildelingsService;
        _testService = testService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<PasientMedBehandlernavn> Pasienter { get; private set; } = Array.Empty<PasientMedBehandlernavn>();

    [BindProperty]
    public List<long> PasientIder { get; set; } = new();

    public string? Feilmelding { get; private set; }

    /// <summary>
    /// Bugliste 2026-10-11, oppfølging: samme "Tildelte tester"-fane som Tester.cshtml (steg 2) —
    /// lagt til HER OGSÅ fordi dette (steg 1) er siden navigasjonslenken "Tildel tester" faktisk
    /// peker til. Fanen fantes opprinnelig KUN på steg 2, usynlig for en bruker som aldri valgte
    /// pasienter og trykket videre — se docs/beslutningslogg.md.
    /// </summary>
    public IReadOnlyList<TestService.TildelingMedTestOgPasient> IkkeFullforteTildelinger { get; private set; } = Array.Empty<TestService.TildelingMedTestOgPasient>();
    public string? PaaminnelseMelding { get; private set; }

    /// <summary>
    /// Satt fra "Tildel tester"-ikonknappen på Pasienter/Index for ÉN bestemt
    /// pasient — hopper direkte til steg 2 (samme sluttresultat som å huke av
    /// akkurat denne ene raden og trykke Neste i steg 1), i stedet for å tvinge
    /// brukeren gjennom en ett-rad-lang seleksjon de allerede har gjort ved å
    /// klikke ikonet.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(long? forhaandsvalgtId, CancellationToken cancellationToken)
    {
        PaaminnelseMelding = TempData["PaaminnelseMelding"] as string;
        await LastIkkeFullforteAsync(cancellationToken);
        Pasienter = await _tildelingsService.HentTilgjengeligePasienterAsync(HentBehandlerId(), cancellationToken);

        if (forhaandsvalgtId is not null && Pasienter.Any(p => p.Pasient.Id == forhaandsvalgtId))
        {
            TempData["TildelPasientIder"] = forhaandsvalgtId.Value.ToString();
            return RedirectToPage("Tester");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (PasientIder.Count == 0)
        {
            Feilmelding = "Velg minst én pasient.";
            Pasienter = await _tildelingsService.HentTilgjengeligePasienterAsync(HentBehandlerId(), cancellationToken);
            await LastIkkeFullforteAsync(cancellationToken);
            return Page();
        }

        TempData["TildelPasientIder"] = string.Join(",", PasientIder.Distinct());
        return RedirectToPage("Tester");
    }

    /// <summary>Se Tildel/Tester.cshtml.cs sin motstykke for full begrunnelse av redirect+TempData-mønsteret.</summary>
    public async Task<IActionResult> OnPostPaaminnAsync(long tildelingId, CancellationToken cancellationToken)
    {
        var sendt = await _tildelingsService.PaaminnUbesvartTestAsync(
            tildelingId, baseUrl: $"{Request.Scheme}://{Request.Host}", behandlerId: HentBehandlerId(), cancellationToken);
        TempData["PaaminnelseMelding"] = sendt ? "Påminnelse sendt." : "Kunne ikke sende påminnelse (tildelingen er allerede fullført).";
        return Redirect(Url.Page("./Pasienter")! + "#tildelte");
    }

    public async Task<IActionResult> OnPostSlettIkkeFullfortAsync(long tildelingId, CancellationToken cancellationToken)
    {
        await _testService.SlettIkkeFullfortTildelingAsync(tildelingId, HentBehandlerId(), cancellationToken);
        return Redirect(Url.Page("./Pasienter")! + "#tildelte");
    }

    private async Task LastIkkeFullforteAsync(CancellationToken cancellationToken)
    {
        var raw = await _testService.HentIkkeFullforteForBehandlerAsync(HentBehandlerId(), cancellationToken);
        IkkeFullforteTildelinger = raw
            .OrderBy(r => r.PasientNavn, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Tildeling.TildeltUtc)
            .ToList();
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
