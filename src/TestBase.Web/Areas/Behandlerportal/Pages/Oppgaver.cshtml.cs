using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages;

/// <summary>
/// Behandlers oppgaveliste (jf. beslutningsloggen "Meldinger og oppgaveliste"):
/// fullførte tester som venter på godkjenning (krever handling — se
/// Pasienter/Rapport.cshtml.cs) og, til oversikt, tester tildelt egne pasienter
/// som ennå ikke er besvart.
/// </summary>
[Authorize(Policy = "BehandlerOmrade")]
public sealed class OppgaverModel : PageModel
{
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;

    public OppgaverModel(TestService testService, ICurrentUserContext currentUser)
    {
        _testService = testService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<TestService.TildelingMedTestOgPasient> VenterPaaGodkjenning { get; private set; } = Array.Empty<TestService.TildelingMedTestOgPasient>();
    public IReadOnlyList<TestService.TildelingMedTestOgPasient> IkkeFullfort { get; private set; } = Array.Empty<TestService.TildelingMedTestOgPasient>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
        VenterPaaGodkjenning = await _testService.HentUgodkjenteFullforteForBehandlerAsync(behandlerId, cancellationToken);
        IkkeFullfort = await _testService.HentIkkeFullforteForBehandlerAsync(behandlerId, cancellationToken);
    }
}
