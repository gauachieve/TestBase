using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Providers;

namespace TestBase.Web.Pages.BetalingTest;

/// <summary>
/// Diagnostisk trigger for en ekte Stripe-testbetaling — kort, Apple Pay og
/// Google Pay dukker automatisk opp i Stripes "Payment Element" på klientsiden
/// (se .cshtml) uten egen serverside-kode per betalingsmiddel.
/// </summary>
public sealed class StripeModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    private readonly IStripeClient _stripe;
    private readonly IConfiguration _configuration;

    public StripeModel(IWebHostEnvironment env, IStripeClient stripe, IConfiguration configuration)
    {
        _env = env;
        _stripe = stripe;
        _configuration = configuration;
    }

    public string? ClientSecret { get; private set; }
    public string? PublishableKey { get; private set; }
    public string? Feil { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        // Gates ved bruk, ikke bare via lenkens synlighet i DevDemo.cshtml.
        PublishableKey = _configuration["Stripe:PublishableKey"];
        if (!_env.IsDevelopment() || string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]) || string.IsNullOrWhiteSpace(PublishableKey))
        {
            return NotFound();
        }

        // Stripe har en minimumsgrense på 3 kr for NOK-transaksjoner.
        var resultat = await _stripe.OpprettBetalingsintensjonAsync(5m, "Diagnostisk testbetaling (PsyTest)", cancellationToken);
        if (!resultat.Success || resultat.ClientSecret is null)
        {
            Feil = resultat.ErrorMessage ?? "Ukjent feil";
        }
        else
        {
            ClientSecret = resultat.ClientSecret;
        }

        return Page();
    }
}
