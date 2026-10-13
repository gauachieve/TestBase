using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages;

/// <summary>
/// Behandlers egen "Abonnement"-fane (se docs/beslutningslogg.md "Abonnement/
/// subscription-system") — viser test-modus/kort-status og lar behandleren
/// velge mellom de tre abonnementsmodiene NÅR et kort er lagret. Ekte
/// Stripe-kortopptak (SetupIntent) er IKKE bygget ennå, se Pages/BliBehandler —
/// denne siden kan derfor IKKE selv sette HarKortLagret=true, kun bytte modus
/// når det allerede er satt (manuelt, av Superadmin, se Admin/Behandlere/Rediger).
/// Egen topp-nivå-side, samme "for få sider til å rettferdiggjøre
/// AuthorizeAreaFolder"-mønster som Innstillinger.cshtml.
/// </summary>
[Authorize(Policy = "BehandlerOmrade")]
public sealed class AbonnementModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public AbonnementModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public AbonnementModus ValgtModus { get; set; }

    public bool ErKlinikktilknyttet { get; private set; }
    public bool HarKortLagret { get; private set; }
    public DateTimeOffset? ProveperiodeSluttUtc { get; private set; }
    public string? Melding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var behandler = await HentBehandlerAsync(cancellationToken);
        if (behandler is null)
        {
            return;
        }

        ErKlinikktilknyttet = behandler.PartnerId is not null;
        HarKortLagret = behandler.HarKortLagret;
        ProveperiodeSluttUtc = behandler.ProveperiodeSluttUtc;
        ValgtModus = behandler.AbonnementModus;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var behandler = await HentBehandlerAsync(cancellationToken);
        if (behandler is null)
        {
            return NotFound();
        }

        if (!behandler.HarKortLagret)
        {
            Melding = "Du må ha et kort lagret før du kan velge abonnementsmodus — se forklaringen under.";
            ErKlinikktilknyttet = behandler.PartnerId is not null;
            HarKortLagret = false;
            ProveperiodeSluttUtc = behandler.ProveperiodeSluttUtc;
            return Page();
        }

        behandler.AbonnementModus = ValgtModus;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "EndreAbonnementModus",
            nameof(Behandler), behandler.Id.ToString(), ValgtModus.ToString(), cancellationToken: cancellationToken);

        Melding = "Lagret.";
        ErKlinikktilknyttet = behandler.PartnerId is not null;
        HarKortLagret = true;
        ProveperiodeSluttUtc = behandler.ProveperiodeSluttUtc;
        return Page();
    }

    private async Task<Behandler?> HentBehandlerAsync(CancellationToken cancellationToken)
    {
        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
        return await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
    }
}
