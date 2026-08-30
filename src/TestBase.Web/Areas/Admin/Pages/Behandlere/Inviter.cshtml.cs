using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Behandlere;

public sealed class InviterModel : PageModel
{
    private readonly BehandlerInvitasjonService _invitasjonService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public InviterModel(BehandlerInvitasjonService invitasjonService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _invitasjonService = invitasjonService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string? MobilNr { get; set; }

    [BindProperty]
    public string? Epost { get; set; }

    public string? Feilmelding { get; private set; }
    public bool Sendt { get; private set; }
    public string? Lenke { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(MobilNr) && string.IsNullOrWhiteSpace(Epost))
        {
            Feilmelding = "Fyll ut enten mobilnummer eller e-post.";
            return Page();
        }

        var administratorId = HentAdministratorId(_currentUser.UserId);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var resultat = await _invitasjonService.InviterAsync(
            MobilNr, Epost, administratorId: administratorId, behandlerId: null, baseUrl, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "InviterBehandler",
            nameof(Behandler), resultat.Behandler.Id.ToString(), cancellationToken: cancellationToken);

        Sendt = true;
        Lenke = resultat.Lenke;
        return Page();
    }

    // ICurrentUserContext.UserId settes til "administrator:{id}" ved innlogging
    // (se AuthSignIn) — begge innloggingsstier (passord og BankID) bruker samme
    // format, så denne parsingen fungerer uansett hvilken vei administratoren
    // logget inn.
    private static long HentAdministratorId(string userId) =>
        long.TryParse(userId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
