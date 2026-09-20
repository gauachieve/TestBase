using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.BankIdTest;

/// <summary>Viser rå claims fra en ekte BankID-testinnlogging (via Idura) — se Start.cshtml.cs.</summary>
public sealed class ResultatModel : PageModel
{
    private readonly IConfiguration _configuration;

    public ResultatModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? Claims { get; private set; }
    public string? Feil { get; private set; }

    public IActionResult OnGet(string? feil)
    {
        if (!Miljo.TillatUtviklingsSnarveier(_configuration))
        {
            return NotFound();
        }

        Feil = feil;
        Claims = TempData["BankIdTestClaims"] as string;
        return Page();
    }
}
