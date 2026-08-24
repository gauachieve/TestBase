using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages;

[Authorize(Policy = "PasientOmrade")]
public sealed class MinSideModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;

    public MinSideModel(AppDbContext db, TestService testService, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _currentUser = currentUser;
    }

    public sealed record TildeltTestRad(TestTildeling Tildeling, string TestNavn);

    public List<TildeltTestRad> Tildelinger { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var pasientId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
        var tildelinger = await _testService.HentTildelingerForPasientAsync(pasientId, cancellationToken);

        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var testNavn = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);

        Tildelinger = tildelinger.Select(t => new TildeltTestRad(t, testNavn.GetValueOrDefault(t.TestId, "(ukjent test)"))).ToList();
    }
}
