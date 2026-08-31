using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages;

/// <summary>
/// Behandlers personlige side: uleste meldinger (se BehandlerMelding — én per
/// pasient som nettopp har fullført en test) med lenke rett til rapporten
/// (åpning markerer meldingen lest, se Pasienter/Rapport.cshtml.cs), samt
/// snarveier til oppgavelisten og varslingsinnstillingene.
/// </summary>
[Authorize(Policy = "BehandlerOmrade")]
public sealed class MinSideModel : PageModel
{
    private readonly BehandlerMeldingService _meldingService;
    private readonly ICurrentUserContext _currentUser;

    public MinSideModel(BehandlerMeldingService meldingService, ICurrentUserContext currentUser)
    {
        _meldingService = meldingService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<MeldingMedDetaljer> UlesteMeldinger { get; private set; } = Array.Empty<MeldingMedDetaljer>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
        UlesteMeldinger = await _meldingService.HentUlesteAsync(behandlerId, cancellationToken);
    }
}
