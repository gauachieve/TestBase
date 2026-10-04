using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Hjemmeoppgaver;

/// <summary>
/// Fase 2 (se docs/beslutningslogg.md "Hjemmeoppgaver og programmer"): fire faner —
/// Personlig/Delt/Partner/Opprett, samme generiske fane+søk-mønster som Grupper/MinSide (se
/// wwwroot/js/faner.js). "Opprett" er ikke en tabell, bare en lenke til editoren (Rediger.cshtml
/// har ALLEREDE full opprett+rediger-funksjonalitet siden fase 1).
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly HjemmeoppgaveService _hjemmeoppgaveService;
    private readonly ICurrentUserContext _currentUser;

    public IndexModel(HjemmeoppgaveService hjemmeoppgaveService, ICurrentUserContext currentUser)
    {
        _hjemmeoppgaveService = hjemmeoppgaveService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<Test> Personlige { get; private set; } = Array.Empty<Test>();
    public IReadOnlyList<Test> DeltMedAlle { get; private set; } = Array.Empty<Test>();
    public IReadOnlyList<Test> DeltMedPartner { get; private set; } = Array.Empty<Test>();
    public HashSet<long> LikteTestIder { get; private set; } = new();
    public bool HarPartner { get; private set; }
    public string? Melding { get; private set; }
    public long EgenBehandlerId { get; private set; }

    public async Task OnGetAsync(long? opprettet, CancellationToken cancellationToken)
    {
        await LastAltAsync(cancellationToken);
        if (opprettet is not null)
        {
            Melding = "Hjemmeoppgaven er opprettet.";
        }
    }

    public async Task<IActionResult> OnPostSlettAsync(long testId, CancellationToken cancellationToken)
    {
        var resultat = await _hjemmeoppgaveService.SlettAsync(testId, HentBehandlerId(), cancellationToken);
        Melding = resultat switch
        {
            HjemmeoppgaveSlettResultat.Slettet => "Hjemmeoppgaven er slettet.",
            HjemmeoppgaveSlettResultat.ArkivertIStedet => "Hjemmeoppgaven er allerede brukt av minst én pasient, så den ble arkivert i stedet for slettet.",
            HjemmeoppgaveSlettResultat.IngenTilgang => "Du eier ikke denne hjemmeoppgaven.",
            _ => "Fant ikke hjemmeoppgaven."
        };
        await LastAltAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostKopierAsync(long testId, CancellationToken cancellationToken)
    {
        var kopi = await _hjemmeoppgaveService.KopierAsync(testId, HentBehandlerId(), cancellationToken);
        if (kopi is null)
        {
            Melding = "Fant ikke hjemmeoppgaven.";
            await LastAltAsync(cancellationToken);
            return Page();
        }
        return RedirectToPage("Rediger", new { id = kopi.Id });
    }

    public async Task<IActionResult> OnPostLikAsync(long testId, CancellationToken cancellationToken)
    {
        await _hjemmeoppgaveService.LikAsync(HentBehandlerId(), testId, cancellationToken);
        await LastAltAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostFjernLikingAsync(long testId, CancellationToken cancellationToken)
    {
        await _hjemmeoppgaveService.FjernLikingAsync(HentBehandlerId(), testId, cancellationToken);
        await LastAltAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDelMedAlleAsync(long testId, bool verdi, CancellationToken cancellationToken)
    {
        await _hjemmeoppgaveService.SettDeltMedAlleAsync(testId, HentBehandlerId(), verdi, cancellationToken);
        await LastAltAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDelMedPartnerAsync(long testId, bool verdi, CancellationToken cancellationToken)
    {
        await _hjemmeoppgaveService.SettDeltMedPartnerAsync(testId, HentBehandlerId(), verdi, cancellationToken);
        await LastAltAsync(cancellationToken);
        return Page();
    }

    private async Task LastAltAsync(CancellationToken cancellationToken)
    {
        EgenBehandlerId = HentBehandlerId();
        Personlige = await _hjemmeoppgaveService.HentPersonligAsync(EgenBehandlerId, cancellationToken);
        DeltMedAlle = await _hjemmeoppgaveService.HentDeltMedAlleAsync(EgenBehandlerId, cancellationToken);
        DeltMedPartner = await _hjemmeoppgaveService.HentDeltMedPartnerAsync(EgenBehandlerId, cancellationToken);
        LikteTestIder = await _hjemmeoppgaveService.HentLikteTestIderAsync(EgenBehandlerId, cancellationToken);
        HarPartner = _currentUser.PartnerId is not null;
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
