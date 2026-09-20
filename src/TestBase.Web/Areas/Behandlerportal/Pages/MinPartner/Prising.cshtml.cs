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
///
/// Utvidet 2026-09-15 (bugliste punkt 3/4/5): partner-admin kan nå be om å
/// legge til/fjerne tester fra denne listen selv, i stedet for å måtte be
/// Superadmin gjøre det direkte på Admin/Partnere/Tester — forespørselen trer
/// ikke i kraft før en administrator godkjenner den på Admin/MinSide, se
/// TestTilgangForespoersel.
/// </summary>
public sealed class PrisingModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public PrisingModel(AppDbContext db, TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public List<TestMedPartnerAndel> Tester { get; private set; } = new();
    public List<Test> TesterUtenTilgang { get; private set; } = new();
    public List<TestTilgangForespoersel> VentendeForesporsler { get; private set; } = new();
    public Dictionary<long, string> TestNavnById { get; private set; } = new();
    public string? Feilmelding { get; private set; }
    public string? Melding { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        await LastAltAsync(_currentUser.PartnerId.Value, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        var partnerId = _currentUser.PartnerId.Value;
        await LastAltAsync(partnerId, cancellationToken);
        var behandlerId = HentBehandlerId();

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

    public async Task<IActionResult> OnPostForesporTilgangAsync(long testId, TestTilgangHandling handling, CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        var partnerId = _currentUser.PartnerId.Value;
        var resultat = await _testService.OpprettTestTilgangForespoerselAsync(partnerId, testId, handling, HentBehandlerId(), cancellationToken);
        if (resultat.Opprettet)
        {
            Melding = "Forespørsel sendt til administrator for godkjenning.";
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "OpprettTestTilgangForespoersel",
                nameof(TestTilgangForespoersel), $"{partnerId}/{testId}/{handling}", cancellationToken: cancellationToken);
        }
        else
        {
            Feilmelding = resultat.Feilmelding;
        }

        await LastAltAsync(partnerId, cancellationToken);
        return Page();
    }

    private async Task LastAltAsync(long partnerId, CancellationToken cancellationToken)
    {
        var testIder = await _db.PartnerTestTilganger.Where(t => t.PartnerId == partnerId).Select(t => t.TestId).ToListAsync(cancellationToken);
        var tester = await _db.Tester.Where(t => testIder.Contains(t.Id)).OrderBy(t => t.Navn).ToListAsync(cancellationToken);
        var andeler = await _db.PartnerTestAndeler.Where(a => a.PartnerId == partnerId).ToDictionaryAsync(a => a.TestId, cancellationToken);

        Tester = tester.Select(t => new TestMedPartnerAndel(
            t, andeler.TryGetValue(t.Id, out var a) ? a.AndelKr : t.MinstePartnerAndelKr)).ToList();

        TesterUtenTilgang = await _db.Tester
            .Where(t => t.ErAktiv && !testIder.Contains(t.Id))
            .OrderBy(t => t.Navn)
            .ToListAsync(cancellationToken);

        VentendeForesporsler = await _testService.HentVentendeTestTilgangForesporslerForPartnerAsync(partnerId, cancellationToken);
        var alleTestIder = VentendeForesporsler.Select(f => f.TestId).ToList();
        TestNavnById = await _db.Tester.Where(t => alleTestIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
