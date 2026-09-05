using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestBase.Web.Pages.BankIdTest;

/// <summary>
/// Utløser den ekte BankID-OIDC-flyten via Idura — diagnostisk, se DevDemo.cshtml og
/// Program.cs. Gatet på IsDevelopment() OG på at "BankIdTest"-skjemaet faktisk er
/// registrert (unngår en ubehandlet feil hvis noen treffer denne URL-en direkte uten at
/// Idura-konfigurasjon er satt).
/// </summary>
public sealed class StartModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    private readonly IAuthenticationSchemeProvider _schemes;

    public StartModel(IWebHostEnvironment env, IAuthenticationSchemeProvider schemes)
    {
        _env = env;
        _schemes = schemes;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_env.IsDevelopment() || await _schemes.GetSchemeAsync("BankIdTest") is null)
        {
            return NotFound();
        }

        return Challenge(new AuthenticationProperties(), "BankIdTest");
    }
}
