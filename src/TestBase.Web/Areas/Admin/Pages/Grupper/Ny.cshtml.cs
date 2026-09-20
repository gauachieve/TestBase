using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Grupper;

/// <summary>
/// Admin oppretter en gruppe for en VALGT behandler — i motsetning til
/// Behandlerportal/Grupper/Ny (som alltid bruker innlogget behandler sin
/// egen Id) må admin eksplisitt velge hvilken behandler som skal eie gruppen,
/// se prosjektbeskrivelsen: "Administrator skal kunne ... gjøre det samme som
/// behandlere kan ift pasienten."
/// </summary>
public sealed class NyModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly GruppeService _grupper;
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public NyModel(AppDbContext db, GruppeService grupper, TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _grupper = grupper;
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public long BehandlerId { get; set; }

    [BindProperty]
    public DateOnly? StartDato { get; set; }

    [BindProperty]
    public DateOnly? SluttDato { get; set; }

    [BindProperty]
    public List<long> TestIder { get; set; } = new();

    public IReadOnlyList<Behandler> Behandlere { get; private set; } = Array.Empty<Behandler>();
    public IReadOnlyList<TestService.KategoriMedTester> KategoriTre { get; private set; } = Array.Empty<TestService.KategoriMedTester>();
    public IReadOnlyDictionary<long, int> EstimertMinutterPerTestId { get; private set; } = new Dictionary<long, int>();
    public string? Feilmelding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LastValgAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Gruppenavn er obligatorisk.";
            await LastValgAsync(cancellationToken);
            return Page();
        }

        if (!await _db.Behandlere.AnyAsync(b => b.Id == BehandlerId && b.Status != BehandlerStatus.Arkivert, cancellationToken))
        {
            Feilmelding = "Velg en gyldig behandler.";
            await LastValgAsync(cancellationToken);
            return Page();
        }

        var gruppe = await _grupper.OpprettAsync(Navn, BehandlerId, TestIder, StartDato, SluttDato, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OpprettGruppe",
            nameof(Gruppe), gruppe.Id.ToString(), $"BehandlerId {BehandlerId}", cancellationToken);

        return RedirectToPage("Index");
    }

    private async Task LastValgAsync(CancellationToken cancellationToken)
    {
        Behandlere = (await _db.Behandlere
            .Where(b => b.Status != BehandlerStatus.Arkivert)
            .ToListAsync(cancellationToken))
            .OrderBy(b => b.Visningsnavn)
            .ToList();
        KategoriTre = await _testService.HentKategoriTreAsync(cancellationToken: cancellationToken);
        var testIder = KategoriTre.SelectMany(k => k.Tester).Select(t => t.Id).Distinct().ToList();
        var antallLedd = await _testService.HentAntallLeddPerTestAsync(testIder, cancellationToken);
        EstimertMinutterPerTestId = antallLedd.ToDictionary(kv => kv.Key, kv => Math.Max(1, (int)Math.Ceiling(kv.Value * 15.0 / 60)));
    }
}
