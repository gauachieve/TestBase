using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

public sealed class DetaljerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public DetaljerModel(AppDbContext db, TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public sealed record TildeltTestRad(TestTildeling Tildeling, string TestNavn);

    public Pasient? Pasient { get; private set; }
    public List<TildeltTestRad> Tildelinger { get; private set; } = new();
    public List<Test> AktiveTester { get; private set; } = new();

    [BindProperty]
    public long TestId { get; set; }

    [BindProperty]
    public DateTime? Frist { get; set; }

    [BindProperty]
    public int? VarighetMinutter { get; set; }

    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();

        Pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id && p.BehandlerId == behandlerId, cancellationToken);
        if (Pasient is null)
        {
            return NotFound();
        }

        await LastInnListerAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();

        Pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id && p.BehandlerId == behandlerId, cancellationToken);
        if (Pasient is null)
        {
            return NotFound();
        }

        var tildeling = await _testService.TildelAsync(
            TestId, id, behandlerId,
            Frist is null ? null : new DateTimeOffset(Frist.Value, TimeSpan.Zero),
            VarighetMinutter, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "TildelTest",
            nameof(TestTildeling), tildeling.Id.ToString(), $"PasientId {id}", cancellationToken);

        return RedirectToPage(new { id });
    }

    private async Task LastInnListerAsync(long pasientId, CancellationToken cancellationToken)
    {
        var tildelinger = await _testService.HentTildelingerForPasientAsync(pasientId, cancellationToken);
        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var testNavn = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);
        Tildelinger = tildelinger.Select(t => new TildeltTestRad(t, testNavn.GetValueOrDefault(t.TestId, "(ukjent test)"))).ToList();

        AktiveTester = await _testService.HentAktiveTesterAsync(cancellationToken);
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
