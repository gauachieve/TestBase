using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages;

/// <summary>
/// Behandlers varslingsinnstillinger — i dag kun den daglige påminnelsen om
/// ugodkjente fullførte rapporter (se PaaminnelseService og
/// DagligPaaminnelseBakgrunnstjeneste i TestBase.Web). "Send test-påminnelse
/// nå" lar behandler se meldingen med det samme i dev, samme prinsipp som
/// "Regenerer innebygde tester" i Admin/Tester/Index.
/// </summary>
[Authorize(Policy = "BehandlerOmrade")]
public sealed class InnstillingerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly PaaminnelseService _paaminnelseService;
    private readonly ICurrentUserContext _currentUser;

    public InnstillingerModel(AppDbContext db, PaaminnelseService paaminnelseService, ICurrentUserContext currentUser)
    {
        _db = db;
        _paaminnelseService = paaminnelseService;
        _currentUser = currentUser;
    }

    [BindProperty]
    public bool OnskerDagligPaaminnelse { get; set; }

    [BindProperty]
    public Varslingspreferanse PaaminnelseKanal { get; set; } = Varslingspreferanse.Begge;

    public DateTimeOffset? SistPaaminnetUtc { get; private set; }
    public string? Melding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var behandler = await HentBehandlerAsync(cancellationToken);
        if (behandler is null)
        {
            return;
        }

        OnskerDagligPaaminnelse = behandler.OnskerDagligPaaminnelse;
        PaaminnelseKanal = behandler.PaaminnelseKanal;
        SistPaaminnetUtc = behandler.SistPaaminnetUtc;
    }

    public async Task<IActionResult> OnPostLagreAsync(CancellationToken cancellationToken)
    {
        var behandler = await HentBehandlerAsync(cancellationToken);
        if (behandler is null)
        {
            return NotFound();
        }

        behandler.OnskerDagligPaaminnelse = OnskerDagligPaaminnelse;
        behandler.PaaminnelseKanal = PaaminnelseKanal;
        await _db.SaveChangesAsync(cancellationToken);

        Melding = "Lagret.";
        SistPaaminnetUtc = behandler.SistPaaminnetUtc;
        return Page();
    }

    public async Task<IActionResult> OnPostSendNaaAsync(CancellationToken cancellationToken)
    {
        var behandler = await HentBehandlerAsync(cancellationToken);
        if (behandler is null)
        {
            return NotFound();
        }

        OnskerDagligPaaminnelse = behandler.OnskerDagligPaaminnelse;
        PaaminnelseKanal = behandler.PaaminnelseKanal;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sendt = await _paaminnelseService.SendTilEnkeltBehandlerAsync(behandler.Id, baseUrl, cancellationToken);
        Melding = sendt
            ? "Påminnelse sendt (mock — se konsollen for SMS/e-post-innhold, eller sjekk kanalvalget ditt under)."
            : "Ingen fullførte tester venter på godkjenning akkurat nå — ingenting å sende.";

        SistPaaminnetUtc = behandler.SistPaaminnetUtc;
        return Page();
    }

    private async Task<Behandler?> HentBehandlerAsync(CancellationToken cancellationToken)
    {
        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
        return await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
    }
}
