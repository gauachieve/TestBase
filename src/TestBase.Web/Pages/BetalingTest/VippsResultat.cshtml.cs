using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Providers;

namespace TestBase.Web.Pages.BetalingTest;

/// <summary>
/// Vipps sender brukeren tilbake hit etter et betalingsforsøk (returUrl). Selve
/// bekreftelsen skjer ved å SPØRRE Vipps sin status-API her på nytt — ALDRI ved
/// å stole på at brukeren i det hele tatt kom tilbake (kan lukke nettleseren,
/// miste nett osv.), se IVippsClient.
/// </summary>
public sealed class VippsResultatModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    private readonly IVippsClient _vipps;

    public VippsResultatModel(IWebHostEnvironment env, IVippsClient vipps)
    {
        _env = env;
        _vipps = vipps;
    }

    public string? Referanse { get; private set; }
    public VippsStatusResultat? Status { get; private set; }
    public string? Feil { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? referanse, string? feil, CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        Referanse = referanse;
        Feil = feil;

        if (!string.IsNullOrWhiteSpace(referanse) && feil is null)
        {
            Status = await _vipps.HentStatusAsync(referanse, cancellationToken);
        }

        return Page();
    }
}
