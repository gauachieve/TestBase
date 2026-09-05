using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestBase.Web.Pages.BankIdTest;

/// <summary>Viser rå claims fra en ekte BankID-testinnlogging (via Idura) — se Start.cshtml.cs.</summary>
public sealed class ResultatModel : PageModel
{
    private readonly IWebHostEnvironment _env;

    public ResultatModel(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string? Claims { get; private set; }
    public string? Feil { get; private set; }

    public IActionResult OnGet(string? feil)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        Feil = feil;
        Claims = TempData["BankIdTestClaims"] as string;
        return Page();
    }
}
