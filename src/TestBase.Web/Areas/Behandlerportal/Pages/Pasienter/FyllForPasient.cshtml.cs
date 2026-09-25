using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

/// <summary>
/// Side-for-side utfylling AV BEHANDLER, om en pasient — for tester merket
/// Test.FyllesUtAvBehandler (YGTSS-R, MADRS klinikkversjon, SCID-5-PF), som
/// ALDRI sendes til pasienten (se TestTildelingsService.TildelOgVarsleAsync).
/// Samme grunnstruktur som Pasientportal/Tester/Fyll (fremdrift, Neste/
/// Forrige/Ferdig, ingen "Lagre"-knapp), MEN uten betalingsgate (behandler-
/// utfylte tester prises alltid 0/IkkePakrevd) og MED en fritekst-kommentarboks
/// per ledd (TestSvar.BehandlerKommentar) — tom og harmløs for alle andre
/// tester, men brukt tungt av SCID-5-PF sitt behov for en klinisk begrunnelse
/// per spørsmål. Rapporten genereres og godkjennes deretter på nøyaktig
/// samme måte som enhver annen test, se docs/beslutningslogg.md
/// "Behandler-utfylte tester".
/// </summary>
[Authorize(Policy = "BehandlerOmrade")]
public sealed class FyllForPasientModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public FyllForPasientModel(AppDbContext db, TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
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
    public string? PasientNavn { get; private set; }

    /// <summary>Behandlerens tidligere lagrede kommentarer for gjeldende sides ledd — vist i utfyllingsskjemaet.</summary>
    public IReadOnlyDictionary<long, string> KommentarPerLeddId { get; private set; } = new Dictionary<long, string>();

    public TestSide? GjeldendeSide => Innhold is null ? null : Innhold.Sider.ElementAtOrDefault(GjeldendeSideNummer - 1);

    public IEnumerable<TestLedd> LeddPaaGjeldendeSide =>
        GjeldendeSide is null ? Enumerable.Empty<TestLedd>() : Innhold!.AlleLedd.Where(l => l.TestSideId == GjeldendeSide.Id);

    public async Task<IActionResult> OnGetAsync(long id, int? side, CancellationToken cancellationToken)
    {
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null || !await HarTilgangAsync(innhold.Tildeling.PasientId, cancellationToken))
        {
            return NotFound();
        }

        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == innhold.Tildeling.TestId, cancellationToken);
        if (test is { FyllesUtAvBehandler: false })
        {
            // Denne siden er KUN for behandler-utfylte tester — en vanlig, pasient-utfylt
            // test skal fylles ut via Pasientportal/Tester/Fyll, ikke herfra.
            return NotFound();
        }

        Innhold = innhold;
        PasientNavn = (await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == innhold.Tildeling.PasientId, cancellationToken))?.Navn;

        if (innhold.Tildeling.Status == TestTildelingStatus.Fullfort)
        {
            ErFullfort = true;
            return Page();
        }

        GjeldendeSideNummer = innhold.Sider.Count == 0 ? 1 : Math.Clamp(side ?? 1, 1, innhold.Sider.Count);
        await LastKommentarerAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, int? side, CancellationToken cancellationToken)
    {
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null || !await HarTilgangAsync(innhold.Tildeling.PasientId, cancellationToken))
        {
            return NotFound();
        }

        if (innhold.Tildeling.Status == TestTildelingStatus.Fullfort)
        {
            return RedirectToPage(new { id });
        }

        Innhold = innhold;
        GjeldendeSideNummer = innhold.Sider.Count == 0 ? 1 : Math.Clamp(side ?? 1, 1, innhold.Sider.Count);

        var gjeldendeSide = GjeldendeSide;
        if (gjeldendeSide is null)
        {
            return Page();
        }

        var svar = new Dictionary<long, string>();
        var kommentarer = new Dictionary<long, string>();
        foreach (var ledd in LeddPaaGjeldendeSide)
        {
            var verdi = Request.Form[$"Svar_{ledd.Id}"].ToString();
            if (!string.IsNullOrWhiteSpace(verdi))
            {
                svar[ledd.Id] = verdi;
            }

            var kommentar = Request.Form[$"Kommentar_{ledd.Id}"].ToString();
            if (!string.IsNullOrWhiteSpace(kommentar))
            {
                kommentarer[ledd.Id] = kommentar;
            }
        }

        var erSisteSide = GjeldendeSideNummer == innhold.Sider.Count;
        var markerFullfort = Handling == "Ferdig" && erSisteSide;

        await _testService.LagreSvarAsync(id, svar, markerFullfort, kommentarer, cancellationToken);

        if (markerFullfort)
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "FullforBehandlerUtfyltTest",
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

    private async Task LastKommentarerAsync(long tildelingId, CancellationToken cancellationToken)
    {
        KommentarPerLeddId = await _db.TestSvar
            .Where(s => s.TestTildelingId == tildelingId && s.BehandlerKommentar != null)
            .ToDictionaryAsync(s => s.TestLeddId, s => s.BehandlerKommentar!, cancellationToken);
    }

    /// <summary>Samme mønster som Behandlerportal/Pasienter/Detaljer sin HarTilgangAsync — egen (ikke-partner-utvidet) pasient, eller partner-admin på tvers av partnerens behandlere.</summary>
    private async Task<bool> HarTilgangAsync(long pasientId, CancellationToken cancellationToken)
    {
        var behandlerId = await _db.Pasienter.Where(p => p.Id == pasientId).Select(p => p.BehandlerId).FirstOrDefaultAsync(cancellationToken);
        if (behandlerId == HentBehandlerId())
        {
            return true;
        }

        if (!_currentUser.ErPartnerAdministrator || _currentUser.PartnerId is null)
        {
            return false;
        }

        var eierPartnerId = await _db.Behandlere.Where(b => b.Id == behandlerId).Select(b => b.PartnerId).FirstOrDefaultAsync(cancellationToken);
        return eierPartnerId == _currentUser.PartnerId;
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
