using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Tester;

public sealed class RedigerModel : PageModel
{
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public RedigerModel(TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public long Id { get; set; }

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public string? Beskrivelse { get; set; }

    [BindProperty]
    public string? Belonningstekst { get; set; }

    [BindProperty]
    public bool ErAktiv { get; set; }

    public string? TestKode { get; private set; }
    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var test = await _testService.HentTestAsync(id, cancellationToken);
        if (test is null)
        {
            return RedirectToPage("Index");
        }

        Id = test.Id;
        Navn = test.Navn;
        Beskrivelse = test.Beskrivelse;
        Belonningstekst = test.Belonningstekst;
        ErAktiv = test.ErAktiv;
        TestKode = test.Kode;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Navn er obligatorisk.";
            return Page();
        }

        var oppdatert = await _testService.OppdaterTestAsync(Id, Navn, Beskrivelse, Belonningstekst, ErAktiv, cancellationToken);
        if (!oppdatert)
        {
            return RedirectToPage("Index");
        }

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterTest",
            nameof(Test), Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }
}
