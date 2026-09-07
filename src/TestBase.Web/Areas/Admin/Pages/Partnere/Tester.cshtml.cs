using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Partnere;

/// <summary>
/// Superadmin-kuratert allow-list — hvilke tester partnerens behandlere får
/// lov til å bruke, se PartnerTestTilgang og docs/beslutningslogg.md "Partner
/// System + Test Monetization".
/// </summary>
public sealed class TesterModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public TesterModel(AppDbContext db, TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public long PartnerId { get; private set; }
    public string PartnerNavn { get; private set; } = string.Empty;
    public IReadOnlyList<TestService.KategoriMedTester> KategoriTre { get; private set; } = Array.Empty<TestService.KategoriMedTester>();
    public HashSet<long> TilgjengeligeTestIder { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is null)
        {
            return NotFound();
        }

        PartnerId = partner.Id;
        PartnerNavn = partner.Navn;
        KategoriTre = await _testService.HentKategoriTreAsync(cancellationToken);
        TilgjengeligeTestIder = (await _db.PartnerTestTilganger.Where(t => t.PartnerId == id).Select(t => t.TestId).ToListAsync(cancellationToken)).ToHashSet();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(long id, long testId, CancellationToken cancellationToken)
    {
        var eksisterende = await _db.PartnerTestTilganger.FirstOrDefaultAsync(t => t.PartnerId == id && t.TestId == testId, cancellationToken);
        if (eksisterende is not null)
        {
            _db.PartnerTestTilganger.Remove(eksisterende);
        }
        else
        {
            var administratorId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var aid) ? aid : 0;
            _db.PartnerTestTilganger.Add(new PartnerTestTilgang
            {
                PartnerId = id,
                TestId = testId,
                GittAvAdministratorId = administratorId,
                OpprettetUtc = DateTimeOffset.UtcNow
            });
        }
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "EndrePartnerTestTilgang",
            nameof(PartnerTestTilgang), $"{id}/{testId}", cancellationToken: cancellationToken);

        return RedirectToPage(new { id });
    }
}
