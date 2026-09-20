using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.BankIdTest;

/// <summary>
/// Utløser den ekte BankID-OIDC-flyten via Idura — diagnostisk, se DevDemo.cshtml og
/// Program.cs. Gatet på Miljo:TillatUtviklingsSnarveier (se Security/Miljo.cs — usant på
/// live) OG på at "BankIdTest"-skjemaet faktisk er registrert (unngår en ubehandlet feil
/// hvis noen treffer denne URL-en direkte uten at Idura-konfigurasjon er satt).
/// </summary>
public sealed class StartModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly IAuthenticationSchemeProvider _schemes;

    public StartModel(IConfiguration configuration, IAuthenticationSchemeProvider schemes)
    {
        _configuration = configuration;
        _schemes = schemes;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!Miljo.TillatUtviklingsSnarveier(_configuration) || await _schemes.GetSchemeAsync("BankIdTest") is null)
        {
            return NotFound();
        }

        return Challenge(new AuthenticationProperties(), "BankIdTest");
    }
}
