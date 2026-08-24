using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Tester;

public sealed class NyModel : PageModel
{
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public NyModel(TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public string? Beskrivelse { get; set; }

    [BindProperty]
    public string? Belonningstekst { get; set; }

    public string? Feilmelding { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Navn er obligatorisk.";
            return Page();
        }

        var test = await _testService.OpprettTestAsync(Navn, Beskrivelse, Belonningstekst, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OpprettTest",
            nameof(Test), test.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Sider", new { id = test.Id });
    }
}
