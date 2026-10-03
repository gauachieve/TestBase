using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Areas.Admin.Pages.Konto;

/// <summary>
/// Lar DEN EKTE Superadmin-kontoen (BankID/2FA-verifisert, se AppClaimTypes.EktSuperadminId) logge
/// seg inn som en matchende Behandler-/Pasient-konto med SAMME personnummer, og tilbake igjen —
/// slik at systemets eneste Superadmin kan teste HELE produksjonsløpet (admin → behandler →
/// pasient) med sin ene ekte BankID-identitet, i stedet for å trenge tre separate fysiske personer.
/// Se docs/beslutningslogg.md "Superadmin-identitetsbytte" for bakgrunn — dette er BEVISST
/// annerledes enn ByttModus (som kun forfalsker rolle-claimen for Utvikler-kontoen i dev): denne
/// siden logger reelt inn som den matchende raden, med en EKTE NameIdentifier, slik at
/// behandler-/pasient-sider sine "mine data"-spørringer fungerer korrekt, ikke bare UI-skallet.
///
/// Gates INNENFOR handleren (samme mønster som ByttModusModel), ikke via en rolle-policy på
/// mappenivå — EktSuperadminId-claimen overlever bevisst et rollebytte, så siden må være
/// nåbar UANSETT hvilken rolle brukeren for øyeblikket "har på seg".
/// </summary>
[Authorize]
public sealed class ByttIdentitetModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly BehandlerAuthenticationService _behandlerAuth;
    private readonly PasientAuthenticationService _pasientAuth;
    private readonly IAuditLogger _auditLogger;

    public ByttIdentitetModel(
        AppDbContext db, BehandlerAuthenticationService behandlerAuth, PasientAuthenticationService pasientAuth, IAuditLogger auditLogger)
    {
        _db = db;
        _behandlerAuth = behandlerAuth;
        _pasientAuth = pasientAuth;
        _auditLogger = auditLogger;
    }

    public Administrator Superadministrator { get; private set; } = null!;
    public Behandler? MatchendeBehandler { get; private set; }
    public Pasient? MatchendePasient { get; private set; }
    public UserRole GjeldendeRolle { get; private set; }
    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var oppslag = await LastSuperadministratorAsync(cancellationToken);
        if (oppslag is null)
        {
            return Forbid();
        }

        GjeldendeRolle = Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var r)
            ? r
            : UserRole.Superadmin;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string nyRolle, CancellationToken cancellationToken)
    {
        var oppslag = await LastSuperadministratorAsync(cancellationToken);
        if (oppslag is null)
        {
            return Forbid();
        }

        switch (nyRolle)
        {
            case "Superadmin":
                await AuthSignIn.LoggInnAsync(
                    HttpContext, "administrator", Superadministrator.Id, Superadministrator.FulltNavn, UserRole.Superadmin,
                    huskMeg: true, ektSuperadminId: Superadministrator.Id);
                break;

            case "Behandler":
                if (MatchendeBehandler is null)
                {
                    Feilmelding = "Fant ingen behandlerkonto med ditt personnummer ennå. Opprett én under Behandlere først.";
                    GjeldendeRolle = UserRole.Superadmin;
                    return Page();
                }
                await AuthSignIn.LoggInnAsync(
                    HttpContext, "behandler", MatchendeBehandler.Id, MatchendeBehandler.Visningsnavn ?? "Behandler", UserRole.Behandler,
                    huskMeg: true, MatchendeBehandler.PartnerId, MatchendeBehandler.ErPartnerAdministrator,
                    ektSuperadminId: Superadministrator.Id);
                break;

            case "Pasient":
                if (MatchendePasient is null)
                {
                    Feilmelding = "Fant ingen pasientkonto med ditt personnummer ennå. Registrer deg som pasient først.";
                    GjeldendeRolle = UserRole.Superadmin;
                    return Page();
                }
                await AuthSignIn.LoggInnAsync(
                    HttpContext, "pasient", MatchendePasient.Id, MatchendePasient.Navn ?? "Pasient", UserRole.Pasient,
                    huskMeg: true, ektSuperadminId: Superadministrator.Id);
                break;

            default:
                return BadRequest();
        }

        await _auditLogger.LogAsync(
            Superadministrator.AdminId, nyRolle, "SuperadminByttIdentitet",
            nameof(Administrator), Superadministrator.Id.ToString(), $"Byttet til {nyRolle}-visning", cancellationToken);

        return RedirectToPage("/Index", new { area = "" });
    }

    /// <summary>
    /// Null betyr: ingen EktSuperadminId-claim (vanlig bruker — ikke en ekte Superadmin-innlogging
    /// noen gang), ELLER kontoen er siden arkivert/avdegradert. Begge tilfeller er Forbid, ikke en
    /// feilside — denne siden skal rett og slett ikke eksistere for noen andre enn deg.
    /// </summary>
    private async Task<Administrator?> LastSuperadministratorAsync(CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue(AppClaimTypes.EktSuperadminId), out var superadminId))
        {
            return null;
        }

        var administrator = await _db.Administratorer.FirstOrDefaultAsync(a => a.Id == superadminId && a.ErSuperadmin && !a.ErArkivert, cancellationToken);
        if (administrator is null)
        {
            return null;
        }

        Superadministrator = administrator;
        MatchendeBehandler = await _behandlerAuth.FinnVedPersonnummerAsync(administrator.Personnummer, cancellationToken);
        MatchendePasient = await _pasientAuth.FinnVedPersonnummerAsync(administrator.Personnummer, cancellationToken);
        return administrator;
    }
}
