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

    public sealed record TildeltTestRad(TestTildeling Tildeling, string TestNavn, bool HarSkaaring);

    public Pasient? Pasient { get; private set; }
    public List<TildeltTestRad> Tildelinger { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        Pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (Pasient is null || !await HarTilgangAsync(Pasient, cancellationToken))
        {
            return NotFound();
        }

        await LastInnListerAsync(id, cancellationToken);
        return Page();
    }

    /// <summary>
    /// Sender behandler til den fulle tildelingswizarden (Tildel/Tester) med
    /// denne pasienten forhåndsvalgt, i stedet for å tildele direkte herfra —
    /// en direkte tildeling her hoppet forbi BÅDE prising
    /// (TestTildelingsService/TestPrisberegner) OG varsling (SMS/e-post), se
    /// docs/beslutningslogg.md. TempData-nøkkelen må matche
    /// Tildel/Pasienter.cshtml.cs sin, siden Tildel/Tester.cshtml.cs leser
    /// nøyaktig denne nøkkelen.
    /// </summary>
    public async Task<IActionResult> OnGetTildelAsync(long id, CancellationToken cancellationToken)
    {
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pasient is null || !await HarTilgangAsync(pasient, cancellationToken))
        {
            return NotFound();
        }

        TempData["TildelPasientIder"] = id.ToString();
        return RedirectToPage("/Tildel/Tester", new { area = "Behandlerportal" });
    }

    private async Task LastInnListerAsync(long pasientId, CancellationToken cancellationToken)
    {
        var tildelinger = await _testService.HentTildelingerForPasientAsync(pasientId, cancellationToken);
        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var testerById = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);
        Tildelinger = tildelinger.Select(t =>
        {
            var test = testerById.GetValueOrDefault(t.TestId);
            return new TildeltTestRad(
                t, test?.Navn ?? "(ukjent test)",
                t.Status == TestTildelingStatus.Fullfort && _testService.HarSkaaringsberegner(test?.Kode));
        }).ToList();
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

    /// <summary>
    /// Egen pasient ELLER (partner-admin OG pasientens behandler tilhører
    /// samme partner) — se docs/beslutningslogg.md om utvidet pasientvisning
    /// for partner-admin.
    /// </summary>
    private async Task<bool> HarTilgangAsync(Pasient pasient, CancellationToken cancellationToken)
    {
        if (pasient.BehandlerId == HentBehandlerId())
        {
            return true;
        }

        if (!_currentUser.ErPartnerAdministrator || _currentUser.PartnerId is null)
        {
            return false;
        }

        var eierPartnerId = await _db.Behandlere
            .Where(b => b.Id == pasient.BehandlerId)
            .Select(b => b.PartnerId)
            .FirstOrDefaultAsync(cancellationToken);
        return eierPartnerId == _currentUser.PartnerId;
    }
}
