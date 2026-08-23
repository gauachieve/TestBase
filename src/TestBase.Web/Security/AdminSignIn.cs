using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Security;

/// <summary>
/// Utsteder innloggingscookien for en administrator. Delt mellom
/// passord-innloggingsstien (LoggInn) og BankID+2FA-stien (BekreftKode) — se
/// Areas/Admin/Pages/Konto — slik at claim-oppsettet kun finnes ett sted.
/// </summary>
public static class AdminSignIn
{
    public static Task LoggInnAsync(HttpContext httpContext, Administrator administrator, UserRole rolle, bool huskMeg)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, $"administrator:{administrator.Id}"),
            new(ClaimTypes.Name, administrator.FulltNavn),
            new(ClaimTypes.Role, rolle.ToString()),
            new(AdminClaimTypes.BaseRolle, rolle.ToString())
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        return httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = huskMeg });
    }
}
