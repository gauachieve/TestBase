using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestBase.Web.Pages;

/// <summary>
/// Offentlig, lesbar side om TestRom sin regulatoriske status under MDR (EU) 2017/745 — lenket
/// fra footer og fra en liten "Beta"-merking ved logoen i header, se _Layout.cshtml. Bevisst
/// kort og generelt formulert (ingen interne detaljer, ingen spesifikke tidsangivelser) — se
/// docs/mdr-klassifiseringsvurdering-iib-begrunnelse.pdf og docs/beslutningslogg.md
/// "MDR-klassifisering av TestBase sin skåringsfunksjon" for den fulle, interne vurderingen.
/// </summary>
public sealed class RegulatoriskModel : PageModel
{
    public void OnGet()
    {
    }
}
