using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Hjemmeoppgaver;

/// <summary>
/// Fase 1 (se docs/beslutningslogg.md "Hjemmeoppgaver og programmer"): KUN "Personlig"-listen —
/// Delt/Partner-fanene og like/del-knappene kommer i fase 2. Egen/likt skilles via
/// HjemmeoppgaveService.HentPersonligAsync (begge dukker opp her, se Test.OpprettetAvBehandlerId).
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
    public string? Melding { get; private set; }
    public long EgenBehandlerId { get; private set; }

    public async Task OnGetAsync(long? opprettet, CancellationToken cancellationToken)
    {
        EgenBehandlerId = HentBehandlerId();
        Personlige = await _hjemmeoppgaveService.HentPersonligAsync(EgenBehandlerId, cancellationToken);
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
        EgenBehandlerId = HentBehandlerId();
        Personlige = await _hjemmeoppgaveService.HentPersonligAsync(EgenBehandlerId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostKopierAsync(long testId, CancellationToken cancellationToken)
    {
        var kopi = await _hjemmeoppgaveService.KopierAsync(testId, HentBehandlerId(), cancellationToken);
        if (kopi is null)
        {
            Melding = "Fant ikke hjemmeoppgaven.";
            EgenBehandlerId = HentBehandlerId();
            Personlige = await _hjemmeoppgaveService.HentPersonligAsync(EgenBehandlerId, cancellationToken);
            return Page();
        }
        return RedirectToPage("Rediger", new { id = kopi.Id });
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
