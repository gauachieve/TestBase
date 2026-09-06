using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Providers;

namespace TestBase.Web.Pages.BetalingTest;

/// <summary>
/// Stripe sender brukeren tilbake hit med "payment_intent" i spørrestrengen.
/// Selve bekreftelsen skjer ved å SPØRRE Stripe sin status-API her på nytt —
/// ALDRI ved å stole på spørrestrengen alene, se IStripeClient.
/// </summary>
public sealed class StripeResultatModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    private readonly IStripeClient _stripe;

    public StripeResultatModel(IWebHostEnvironment env, IStripeClient stripe)
    {
        _env = env;
        _stripe = stripe;
    }

    public string? BetalingsId { get; private set; }
    public StripeStatusResultat? Status { get; private set; }

    public async Task<IActionResult> OnGetAsync([FromQuery(Name = "payment_intent")] string? paymentIntent, CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        BetalingsId = paymentIntent;
        if (!string.IsNullOrWhiteSpace(paymentIntent))
        {
            Status = await _stripe.HentStatusAsync(paymentIntent, cancellationToken);
        }

        return Page();
    }
}
