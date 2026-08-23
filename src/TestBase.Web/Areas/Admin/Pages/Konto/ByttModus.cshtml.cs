using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Konto;

/// <summary>
/// Lar en bruker som logget inn med AdminId+passord (dvs. i utviklingsmodus)
/// teste systemet som Administrator/Behandler/Pasient uten å logge ut, jf.
/// kravdokumentets Del 2: "Som utvikler skal man kunne enkelt bytte hva slags
/// modus man er i". Sjekker <see cref="AppClaimTypes.BaseRolle"/> (rollen man
/// faktisk logget inn med), ikke den ev. allerede byttede rollen — ellers ville
/// man låst seg selv ute etter første bytte bort fra Utvikler.
/// </summary>
[Authorize]
public sealed class ByttModusModel : PageModel
{
    public IReadOnlyList<UserRole> Roller { get; } = Enum.GetValues<UserRole>();
    public UserRole GjeldendeRolle { get; private set; }

    public IActionResult OnGet()
    {
        if (!ErUtvikler())
        {
            return Forbid();
        }

        GjeldendeRolle = Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var rolle)
            ? rolle
            : UserRole.Utvikler;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(UserRole nyRolle)
    {
        if (!ErUtvikler())
        {
            return Forbid();
        }

        var identity = (ClaimsIdentity)User.Identity!;
        var eksisterendeRolleClaim = identity.FindFirst(ClaimTypes.Role);
        if (eksisterendeRolleClaim is not null)
        {
            identity.RemoveClaim(eksisterendeRolleClaim);
        }

        identity.AddClaim(new Claim(ClaimTypes.Role, nyRolle.ToString()));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, User);

        return RedirectToPage("/Index", new { area = "" });
    }

    private bool ErUtvikler() => User.FindFirstValue(AppClaimTypes.BaseRolle) == nameof(UserRole.Utvikler);
}
