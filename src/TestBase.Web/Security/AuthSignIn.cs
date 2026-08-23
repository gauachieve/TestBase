using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TestBase.Shared.Security;

namespace TestBase.Web.Security;

/// <summary>
/// Utsteder innloggingscookien — delt mellom administrator- (passord- og
/// BankID+2FA-stien) og behandler-pålogging (BankID+2FA), se
/// Areas/Admin/Pages/Konto og Areas/Behandlerportal/Pages/Konto, slik at
/// claim-oppsettet kun finnes ett sted.
/// </summary>
public static class AuthSignIn
{
    public static Task LoggInnAsync(
        HttpContext httpContext,
        string brukerIdPrefix,
        long brukerId,
        string displayName,
        UserRole rolle,
        bool huskMeg)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, $"{brukerIdPrefix}:{brukerId}"),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Role, rolle.ToString()),
            new(AppClaimTypes.BaseRolle, rolle.ToString())
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        return httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = huskMeg });
    }
}
