using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Providers;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.BetalingTest;

/// <summary>Diagnostisk trigger for en ekte Vipps-testbetaling — se DevDemo.cshtml.</summary>
public sealed class VippsModel : PageModel
{
    private readonly IVippsClient _vipps;
    private readonly IConfiguration _configuration;

    public VippsModel(IVippsClient vipps, IConfiguration configuration)
    {
        _vipps = vipps;
        _configuration = configuration;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        // Gates ved bruk, ikke bare via lenkens synlighet i DevDemo.cshtml — se
        // CLAUDE.md sine kjente fallgruver om dev-only-felt som kun er gatet i viewet.
        if (!Miljo.TillatUtviklingsSnarveier(_configuration) || string.IsNullOrWhiteSpace(_configuration["Vipps:ClientId"]))
        {
            return NotFound();
        }

        var referanse = $"betalingtest-{Guid.NewGuid():N}";
        var returUrl = Url.Page("/BetalingTest/VippsResultat", pageHandler: null, values: new { referanse }, protocol: Request.Scheme)
            ?? throw new InvalidOperationException("Klarte ikke å generere returUrl for VippsResultat.");

        var resultat = await _vipps.OpprettBetalingAsync(referanse, 1m, "Diagnostisk testbetaling (PsyTest)", returUrl, cancellationToken);
        if (!resultat.Success || resultat.RedirectUrl is null)
        {
            return RedirectToPage("VippsResultat", new { referanse, feil = resultat.ErrorMessage ?? "Ukjent feil" });
        }

        return Redirect(resultat.RedirectUrl);
    }
}
