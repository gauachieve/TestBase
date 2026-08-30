using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.Skaaring;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

/// <summary>
/// Rapport per besvarelse + over tid (jf. "Definisjon av en test" punkt 8–9 i
/// kravdokumentet), bevist ut med WHO-5 i fase 5. Vises kun for tester med en
/// registrert ITestSkaaringsberegner — de fleste admin-forfattede tester har
/// ingen ennå.
/// </summary>
public sealed class RapportModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;

    public RapportModel(AppDbContext db, TestService testService, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _currentUser = currentUser;
    }

    public sealed record SvarRad(string Sporsmal, string SvarLabel);

    public Pasient? Pasient { get; private set; }
    public Test? Test { get; private set; }
    public TestTildeling? Tildeling { get; private set; }
    public TestSkaaring? Skaaring { get; private set; }
    public List<SvarRad> SvarRader { get; private set; } = new();
    public IReadOnlyList<SkaaringHistorikkPunkt> Historikk { get; private set; } = Array.Empty<SkaaringHistorikkPunkt>();

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();

        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null)
        {
            return NotFound();
        }

        Pasient = await _db.Pasienter.FirstOrDefaultAsync(
            p => p.Id == innhold.Tildeling.PasientId && p.BehandlerId == behandlerId, cancellationToken);
        if (Pasient is null)
        {
            return NotFound();
        }

        Tildeling = innhold.Tildeling;
        Test = innhold.Test;

        Skaaring = await _testService.BeregnSkaaringAsync(id, cancellationToken);
        if (Skaaring is null)
        {
            return NotFound();
        }

        SvarRader = innhold.AlleLedd.Select(ledd =>
        {
            var raaVerdi = innhold.EksisterendeSvar.GetValueOrDefault(ledd.Id, "-");
            var label = ledd.Svartype switch
            {
                TestSvartype.LikertSkala => TestLeddSvaralternativer.Parse(ledd.Svaralternativer)
                    .FirstOrDefault(p => p.Verdi.ToString() == raaVerdi)?.Tekst ?? raaVerdi,
                TestSvartype.VisuellAnalogSkala => $"{raaVerdi}/100",
                _ => raaVerdi
            };
            return new SvarRad(ledd.Sporsmalstekst, label);
        }).ToList();

        if (Test.Kode is not null)
        {
            Historikk = await _testService.HentSkaaringHistorikkAsync(Pasient.Id, Test.Kode, cancellationToken);
        }

        return Page();
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
