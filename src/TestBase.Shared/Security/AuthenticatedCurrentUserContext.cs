using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace TestBase.Shared.Security;

/// <summary>
/// Ekte implementasjon av ICurrentUserContext (fase 2), til erstatning for
/// DevCurrentUserContext i DI (se Program.cs). Leser claims satt av
/// cookie-autentiseringen ved innlogging — se AdminAuthenticationService og
/// Areas/Admin/Pages/Konto for hvor claimene settes.
/// </summary>
public sealed class AuthenticatedCurrentUserContext : ICurrentUserContext
{
    private readonly ClaimsPrincipal? _principal;

    public AuthenticatedCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _principal = httpContextAccessor.HttpContext?.User;
    }

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated == true;

    public string UserId => IsAuthenticated
        ? _principal!.FindFirstValue(ClaimTypes.NameIdentifier) ?? "ukjent"
        : "anonym";

    public string DisplayName => IsAuthenticated
        ? _principal!.FindFirstValue(ClaimTypes.Name) ?? UserId
        : "Ikke innlogget";

    // Faller tilbake til Pasient (minst-privilegerte rolle) hvis ikke
    // innlogget eller rolle-claimen av en eller annen grunn mangler.
    public UserRole Role => IsAuthenticated
        && Enum.TryParse<UserRole>(_principal!.FindFirstValue(ClaimTypes.Role), out var rolle)
        ? rolle
        : UserRole.Pasient;
}
