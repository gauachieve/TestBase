using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;

namespace TestBase.Web.Areas.Admin.Pages.Pasienter;

/// <summary>
/// Admins oversikt over ALLE pasienter på tvers av behandlere — reint
/// lesetilgang (patient-CRUD hører til behandler, se Behandlerportal/Pasienter),
/// samme grunnleggende idé som Behandlerportal/Pasienter/Index men med en
/// Behandler-kolonne siden admin ser flere behandleres pasienter samtidig
/// (samme mønster som Admin/Tildel/Pasienter).
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;

    public IndexModel(AppDbContext db, TestService testService)
    {
        _db = db;
        _testService = testService;
    }

    public sealed record PasientRad(Pasient Pasient, string? BehandlerNavn, int Tildelt, int Besvart);

    public List<PasientRad> Rader { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var pasienter = await _db.Pasienter.OrderByDescending(p => p.OpprettetUtc).ToListAsync(cancellationToken);

        var behandlere = await _db.Behandlere.ToListAsync(cancellationToken);
        var behandlerNavnById = behandlere.ToDictionary(b => b.Id, b => b.Visningsnavn);

        var tellinger = await _testService.HentTildelingTellingerAsync(pasienter.Select(p => p.Id).ToList(), cancellationToken);

        Rader = pasienter.Select(p =>
        {
            var telling = tellinger.GetValueOrDefault(p.Id, new TestService.TildelingTelling(0, 0));
            return new PasientRad(p, behandlerNavnById.GetValueOrDefault(p.BehandlerId), telling.Tildelt, telling.Besvart);
        }).ToList();
    }
}
