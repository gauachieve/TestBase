using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.Skaaring;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages.Tester;

/// <summary>
/// Pasientens egen lesetilgang til en rapport — KUN synlig når behandler
/// eksplisitt har godkjent OG gjort den synlig (se
/// TestTildeling.RapportGodkjentUtc/RapportSynligForPasient og
/// Behandlerportal/Pasienter/Rapport.cshtml.cs). Uten det: en vennlig melding,
/// ikke NotFound — tildelingen er legitimt pasientens egen, den er bare ikke
/// klar for visning ennå.
/// </summary>
[Authorize(Policy = "PasientOmrade")]
public sealed class RapportModel : PageModel
{
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;

    public RapportModel(TestService testService, ICurrentUserContext currentUser)
    {
        _testService = testService;
        _currentUser = currentUser;
    }

    public sealed record SvarRad(string Sporsmal, string SvarLabel);
    public sealed record SideMedSvar(TestSide Side, IReadOnlyList<SvarRad> Svar);

    public Test? Test { get; private set; }
    public TestTildeling? Tildeling { get; private set; }
    public TestSkaaring? Skaaring { get; private set; }
    public List<SideMedSvar> Sider { get; private set; } = new();
    public bool IkkeKlarEnna { get; private set; }

    /// <summary>Sammendrag = ett ark, svar (råskår) alltid til slutt — se Behandlerportal-motstykket for begrunnelse.</summary>
    public int TotalAntallArk => 1 + Sider.Count;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var pasientId = HentPasientId();
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null || innhold.Tildeling.PasientId != pasientId)
        {
            return NotFound();
        }

        Tildeling = innhold.Tildeling;
        Test = innhold.Test;

        if (Tildeling.RapportGodkjentUtc is null || !Tildeling.RapportSynligForPasient)
        {
            IkkeKlarEnna = true;
            return Page();
        }

        Skaaring = await _testService.BeregnSkaaringAsync(id, cancellationToken);

        Sider = innhold.Sider.Select(side =>
        {
            var svar = innhold.AlleLedd.Where(l => l.TestSideId == side.Id).Select(ledd =>
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
            return new SideMedSvar(side, svar);
        }).ToList();

        return Page();
    }

    private long HentPasientId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
