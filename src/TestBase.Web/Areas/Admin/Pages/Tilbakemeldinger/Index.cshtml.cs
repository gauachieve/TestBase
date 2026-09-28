using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tilbakemeldinger;

namespace TestBase.Web.Areas.Admin.Pages.Tilbakemeldinger;

/// <summary>
/// Manuell gjennomgang av tilbakemeldinger fra widgeten (se
/// wwwroot/js/tilbakemelding-widget.js) — AdminOmrade-policyen (Program.cs).
/// Supplerer den daglige agent-rapporten (se TilbakemeldingApi.cs "/api/agent/*"),
/// er ikke avhengig av den — admin kan lese/oppdatere status uansett om agenten
/// noensinne kjører. MERK: en tilbakemelding kan inneholde et skjermbilde med
/// pasientdata på skjermen — samme tilgangsnivå som resten av pasientdata, ikke
/// del videre.
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly TilbakemeldingService _service;

    public IndexModel(TilbakemeldingService service)
    {
        _service = service;
    }

    public List<Tilbakemelding> Alle { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Alle = await _service.HentAlleAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostOppdaterStatusAsync(long id, TilbakemeldingStatus status, string? notat, CancellationToken cancellationToken)
    {
        await _service.OppdaterStatusAsync(id, status, notat, cancellationToken);
        return RedirectToPage();
    }
}
