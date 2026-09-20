using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages.Tester;

/// <summary>
/// Samlet liste over pasientens godkjente OG delte rapporter (bugliste 2026-09-15),
/// atskilt fra "Min side" sin liste over tildelte/pågående tester. Lenket fra en
/// egen knapp på MinSide.cshtml.
/// </summary>
[Authorize(Policy = "PasientOmrade")]
public sealed class GodkjenteRapporterModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;

    public GodkjenteRapporterModel(AppDbContext db, TestService testService, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _currentUser = currentUser;
    }

    public sealed record GodkjentRapportRad(TestTildeling Tildeling, string TestNavn);

    public List<GodkjentRapportRad> Rapporter { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var tildelinger = await _testService.HentGodkjenteForPasientAsync(HentPasientId(), kunSynligForPasient: true, cancellationToken);

        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var testNavn = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);

        Rapporter = tildelinger.Select(t => new GodkjentRapportRad(t, testNavn.GetValueOrDefault(t.TestId, "(ukjent test)"))).ToList();
    }

    private long HentPasientId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
