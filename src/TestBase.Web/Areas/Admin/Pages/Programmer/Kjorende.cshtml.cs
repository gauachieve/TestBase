using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;

namespace TestBase.Web.Areas.Admin.Pages.Programmer;

/// <summary>
/// Admin/superadmin sitt syn over ALLE kjørende programmer, uansett hvilken behandler som
/// tildelte (brukerens eksplisitte krav: "show the name of the treater in the running list").
/// Partner-admin sin tilsvarende, snevrere (kun eget partner-fellesskap) visning er BEVISST IKKE
/// bygget denne runden — se docs/beslutningslogg.md.
/// </summary>
public sealed class KjorendeModel : PageModel
{
    private readonly ProgramService _programService;

    public KjorendeModel(ProgramService programService)
    {
        _programService = programService;
    }

    public IReadOnlyList<ProgramService.KjorendeRad> Kjorende { get; private set; } = Array.Empty<ProgramService.KjorendeRad>();
    public string? Melding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Kjorende = await _programService.HentAlleKjorendeAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostPauseAsync(long[] deltakelseIder, CancellationToken cancellationToken)
    {
        await _programService.PauseFlereAsync(deltakelseIder, cancellationToken);
        Melding = "Pauset.";
        Kjorende = await _programService.HentAlleKjorendeAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostFjernAsync(long[] deltakelseIder, CancellationToken cancellationToken)
    {
        await _programService.FjernFlereAsync(deltakelseIder, cancellationToken);
        Melding = "Fjernet.";
        Kjorende = await _programService.HentAlleKjorendeAsync(cancellationToken);
        return Page();
    }
}
