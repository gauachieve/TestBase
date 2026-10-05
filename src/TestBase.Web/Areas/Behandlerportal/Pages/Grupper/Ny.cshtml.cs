using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Grupper;

public sealed class NyModel : PageModel
{
    private readonly GruppeService _grupper;
    private readonly TestService _testService;
    private readonly HjemmeoppgaveService _hjemmeoppgaveService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public NyModel(GruppeService grupper, TestService testService, HjemmeoppgaveService hjemmeoppgaveService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _grupper = grupper;
        _testService = testService;
        _hjemmeoppgaveService = hjemmeoppgaveService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public DateOnly? StartDato { get; set; }

    [BindProperty]
    public DateOnly? SluttDato { get; set; }

    [BindProperty]
    public List<long> TestIder { get; set; } = new();

    public IReadOnlyList<TestService.KategoriMedTester> KategoriTre { get; private set; } = Array.Empty<TestService.KategoriMedTester>();
    public IReadOnlyDictionary<long, int> EstimertMinutterPerTestId { get; private set; } = new Dictionary<long, int>();
    public string? Feilmelding { get; private set; }

    private long HentBehandlerId() => long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LastKategoriTreAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Gruppenavn er obligatorisk.";
            await LastKategoriTreAsync(cancellationToken);
            return Page();
        }

        var behandlerId = HentBehandlerId();
        var gruppe = await _grupper.OpprettAsync(Navn, behandlerId, TestIder, StartDato, SluttDato, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OpprettGruppe",
            nameof(Gruppe), gruppe.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }

    private async Task LastKategoriTreAsync(CancellationToken cancellationToken)
    {
        var tre = await _testService.HentKategoriTreAsync(cancellationToken: cancellationToken);
        // Bugliste 2026-10-05 punkt 36: samme "Egenproduserte"-pinning som Tildel/Tester — en
        // gruppe skal kunne tilordnes en hjemmeoppgave akkurat som en vanlig test.
        var hjemmeoppgaver = await _hjemmeoppgaveService.HentTilgjengeligeForTildelingAsync(HentBehandlerId(), cancellationToken);
        if (hjemmeoppgaver.Count > 0)
        {
            var egenprodusert = new TestKategori { Id = -1, Navn = "Egenproduserte", OpprettetUtc = DateTimeOffset.UtcNow };
            tre = new[] { new TestService.KategoriMedTester(egenprodusert, hjemmeoppgaver) }.Concat(tre).ToList();
        }
        KategoriTre = tre;
        var testIder = KategoriTre.SelectMany(k => k.Tester).Select(t => t.Id).Distinct().ToList();
        var antallLedd = await _testService.HentAntallLeddPerTestAsync(testIder, cancellationToken);
        EstimertMinutterPerTestId = antallLedd.ToDictionary(kv => kv.Key, kv => Math.Max(1, (int)Math.Ceiling(kv.Value * 15.0 / 60)));
    }
}
