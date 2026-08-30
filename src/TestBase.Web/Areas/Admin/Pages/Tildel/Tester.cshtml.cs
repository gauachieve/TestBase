using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Tildel;

/// <summary>Steg 2 av tildelingsflyten for admin — se Behandlerportal-motstykket for detaljer om flyten.</summary>
public sealed class TesterModel : PageModel
{
    private readonly TestService _testService;
    private readonly TestTildelingsService _tildelingsService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _auditLogger;

    public TesterModel(TestService testService, TestTildelingsService tildelingsService, ICurrentUserContext currentUser, IAuditLogger auditLogger)
    {
        _testService = testService;
        _tildelingsService = tildelingsService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    [BindProperty]
    public string PasientIderCsv { get; set; } = string.Empty;

    [BindProperty]
    public List<long> TestIder { get; set; } = new();

    public IReadOnlyList<TestService.KategoriMedTester> KategoriTre { get; private set; } = Array.Empty<TestService.KategoriMedTester>();
    public IReadOnlyList<PasientMedBehandlernavn> ValgtePasienter { get; private set; } = Array.Empty<PasientMedBehandlernavn>();
    public string? Feilmelding { get; private set; }
    public TildelingsBatchResultat? Resultat { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (TempData.Peek("TildelPasientIder") is not string csv || string.IsNullOrWhiteSpace(csv))
        {
            Feilmelding = "Ingen pasienter valgt. Gå tilbake og velg pasienter først.";
            return;
        }

        PasientIderCsv = csv;
        await LastValgtePasienterAsync(csv, cancellationToken);
        KategoriTre = await _testService.HentKategoriTreAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostSendAsync(CancellationToken cancellationToken)
    {
        await LastValgtePasienterAsync(PasientIderCsv, cancellationToken);
        KategoriTre = await _testService.HentKategoriTreAsync(cancellationToken);

        if (!ValgtePasienter.Any())
        {
            Feilmelding = "Ingen gyldige pasienter valgt. Gå tilbake til steg 1.";
            return Page();
        }

        var testIder = TestIder.Distinct().ToList();
        if (testIder.Count == 0)
        {
            Feilmelding = "Velg minst én test.";
            return Page();
        }

        var pasientIder = ValgtePasienter.Select(p => p.Pasient.Id).ToList();
        Resultat = await _tildelingsService.TildelOgVarsleAsync(
            pasientIder, testIder, behandlerId: null, administratorId: HentAdministratorId(),
            baseUrl: $"{Request.Scheme}://{Request.Host}", cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "TildelTesterBatch",
            nameof(TestTildeling), string.Join(",", testIder), $"PasientIder {string.Join(",", pasientIder)}", cancellationToken);

        return Page();
    }

    private async Task LastValgtePasienterAsync(string csv, CancellationToken cancellationToken)
    {
        var onskedeIder = ParseIder(csv).ToHashSet();
        var tilgjengelige = await _tildelingsService.HentTilgjengeligePasienterAsync(null, cancellationToken);
        ValgtePasienter = tilgjengelige.Where(p => onskedeIder.Contains(p.Pasient.Id)).ToList();
    }

    private static IReadOnlyList<long> ParseIder(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => long.TryParse(s, out var id) ? (long?)id : null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToList();

    private long HentAdministratorId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
