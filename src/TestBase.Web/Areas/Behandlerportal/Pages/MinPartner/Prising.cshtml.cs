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
/// Monetization". Ett samlet skjema/én Lagre-knapp for alle rader — se
/// Admin/Tester/Prising/Index.cshtml.cs sitt motstykke for full begrunnelse
/// (bugliste 2026-09-13 punkt 12).
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

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        var partnerId = _currentUser.PartnerId.Value;
        await LastTesterAsync(partnerId, cancellationToken);
        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var bid) ? bid : 0;

        foreach (var rad in Tester)
        {
            var raw = Request.Form[$"AndelKr[{rad.Test.Id}]"].ToString();
            if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var andelKr))
            {
                continue;
            }

            var klemtAndel = Math.Max(andelKr, rad.Test.MinstePartnerAndelKr);
            var eksisterende = await _db.PartnerTestAndeler.FirstOrDefaultAsync(a => a.PartnerId == partnerId && a.TestId == rad.Test.Id, cancellationToken);

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
                    TestId = rad.Test.Id,
                    AndelKr = klemtAndel,
                    SistEndretAvBehandlerId = behandlerId,
                    SistEndretUtc = DateTimeOffset.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterPartnerTestAndel",
            nameof(PartnerTestAndel), partnerId.ToString(), cancellationToken: cancellationToken);

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
