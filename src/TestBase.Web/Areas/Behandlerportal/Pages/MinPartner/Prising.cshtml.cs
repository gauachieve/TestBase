using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.MinPartner;

public sealed record TestMedPartnerAndel(Test Test, decimal EffektivAndelKr);

/// <summary>
/// Partner-admin sin selvbetjente andel per test (se PartnerTestAndel) — kun
/// på tester Superadmin faktisk har gitt partneren tilgang til
/// (PartnerTestTilgang). Klemmes alltid til minst Test.MinstePartnerAndelKr
/// på skrivetidspunktet, se docs/beslutningslogg.md "Partner System + Test
/// Monetization".
/// </summary>
public sealed class PrisingModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public PrisingModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public List<TestMedPartnerAndel> Tester { get; private set; } = new();
    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        await LastTesterAsync(_currentUser.PartnerId.Value, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long testId, decimal andelKr, CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        var partnerId = _currentUser.PartnerId.Value;
        var harTilgang = await _db.PartnerTestTilganger.AnyAsync(t => t.PartnerId == partnerId && t.TestId == testId, cancellationToken);
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId, cancellationToken);
        if (!harTilgang || test is null)
        {
            return NotFound();
        }

        var klemtAndel = Math.Max(andelKr, test.MinstePartnerAndelKr);
        var eksisterende = await _db.PartnerTestAndeler.FirstOrDefaultAsync(a => a.PartnerId == partnerId && a.TestId == testId, cancellationToken);
        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var bid) ? bid : 0;

        if (eksisterende is not null)
        {
            eksisterende.AndelKr = klemtAndel;
            eksisterende.SistEndretAvBehandlerId = behandlerId;
            eksisterende.SistEndretUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            _db.PartnerTestAndeler.Add(new PartnerTestAndel
            {
                PartnerId = partnerId,
                TestId = testId,
                AndelKr = klemtAndel,
                SistEndretAvBehandlerId = behandlerId,
                SistEndretUtc = DateTimeOffset.UtcNow
            });
        }
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterPartnerTestAndel",
            nameof(PartnerTestAndel), $"{partnerId}/{testId}", klemtAndel.ToString("0.00"), cancellationToken);

        return RedirectToPage();
    }

    private async Task LastTesterAsync(long partnerId, CancellationToken cancellationToken)
    {
        var testIder = await _db.PartnerTestTilganger.Where(t => t.PartnerId == partnerId).Select(t => t.TestId).ToListAsync(cancellationToken);
        var tester = await _db.Tester.Where(t => testIder.Contains(t.Id)).OrderBy(t => t.Navn).ToListAsync(cancellationToken);
        var andeler = await _db.PartnerTestAndeler.Where(a => a.PartnerId == partnerId).ToDictionaryAsync(a => a.TestId, cancellationToken);

        Tester = tester.Select(t => new TestMedPartnerAndel(
            t, andeler.TryGetValue(t.Id, out var a) ? a.AndelKr : t.MinstePartnerAndelKr)).ToList();
    }
}
