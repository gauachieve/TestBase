using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages;

/// <summary>
/// Pasientens oppgaveliste — alle tester som fortsatt venter på svar. Samme
/// grunnleggende idé som Behandlerportal/Oppgaver og Admin/Oppgaver: hver
/// rolle ser "det som gjenstår å gjøre" i sitt eget format, se beslutningsloggen
/// "Meldinger og oppgaveliste".
/// </summary>
[Authorize(Policy = "PasientOmrade")]
public sealed class OppgaverModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;

    public OppgaverModel(AppDbContext db, TestService testService, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _currentUser = currentUser;
    }

    public sealed record OppgaveRad(TestTildeling Tildeling, string TestNavn);

    public List<OppgaveRad> Oppgaver { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var pasientId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
        var tildelinger = (await _testService.HentTildelingerForPasientAsync(pasientId, cancellationToken))
            .Where(t => t.Status != TestTildelingStatus.Fullfort)
            .ToList();

        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var testNavn = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);

        Oppgaver = tildelinger.Select(t => new OppgaveRad(t, testNavn.GetValueOrDefault(t.TestId, "(ukjent test)"))).ToList();
    }
}
