using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages.Tester;

/// <summary>
/// Side-for-side utfylling av en tildelt test, jf. "Definisjon av en test" i
/// kravdokumentet: fremdrift i %, instruksjon per test/side/ledd,
/// Neste/Forrige/Lagre/Ferdig, og en belønningsside ved fullføring.
/// </summary>
[Authorize(Policy = "PasientOmrade")]
public sealed class FyllModel : PageModel
{
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public FyllModel(TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string? Handling { get; set; }

    public TestMedInnhold? Innhold { get; private set; }
    public int GjeldendeSideNummer { get; private set; } = 1;
    public bool ErFullfort { get; private set; }
    public string? Feilmelding { get; private set; }

    public TestSide? GjeldendeSide => Innhold is null ? null : Innhold.Sider.ElementAtOrDefault(GjeldendeSideNummer - 1);

    public IEnumerable<TestLedd> LeddPaaGjeldendeSide =>
        GjeldendeSide is null ? Enumerable.Empty<TestLedd>() : Innhold!.AlleLedd.Where(l => l.TestSideId == GjeldendeSide.Id);

    public async Task<IActionResult> OnGetAsync(long id, int? side, CancellationToken cancellationToken)
    {
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null || innhold.Tildeling.PasientId != HentPasientId())
        {
            return NotFound();
        }

        Innhold = innhold;

        if (innhold.Tildeling.Status == TestTildelingStatus.Fullfort)
        {
            ErFullfort = true;
            return Page();
        }

        GjeldendeSideNummer = innhold.Sider.Count == 0 ? 1 : Math.Clamp(side ?? 1, 1, innhold.Sider.Count);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, int? side, CancellationToken cancellationToken)
    {
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null || innhold.Tildeling.PasientId != HentPasientId())
        {
            return NotFound();
        }

        Innhold = innhold;
        GjeldendeSideNummer = innhold.Sider.Count == 0 ? 1 : Math.Clamp(side ?? 1, 1, innhold.Sider.Count);

        var gjeldendeSide = GjeldendeSide;
        if (gjeldendeSide is null)
        {
            return Page();
        }

        var svar = new Dictionary<long, string>();
        foreach (var ledd in LeddPaaGjeldendeSide)
        {
            var verdi = Request.Form[$"Svar_{ledd.Id}"].ToString();
            if (!string.IsNullOrWhiteSpace(verdi))
            {
                svar[ledd.Id] = verdi;
            }
        }

        var erSisteSide = GjeldendeSideNummer == innhold.Sider.Count;
        var markerFullfort = Handling == "Ferdig" && erSisteSide;

        await _testService.LagreSvarAsync(id, svar, markerFullfort, cancellationToken);

        if (markerFullfort)
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "FullforTest",
                nameof(TestTildeling), id.ToString(), cancellationToken: cancellationToken);
            return RedirectToPage(new { id });
        }

        var nesteSideNummer = Handling switch
        {
            "Neste" => GjeldendeSideNummer + 1,
            "Forrige" => GjeldendeSideNummer - 1,
            _ => GjeldendeSideNummer
        };
        nesteSideNummer = Math.Clamp(nesteSideNummer, 1, innhold.Sider.Count);

        return RedirectToPage(new { id, side = nesteSideNummer });
    }

    private long HentPasientId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
