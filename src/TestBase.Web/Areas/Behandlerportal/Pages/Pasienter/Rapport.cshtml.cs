using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.Skaaring;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

/// <summary>
/// Rapport per besvarelse + over tid (jf. "Definisjon av en test" punkt 8–9 i
/// kravdokumentet), bevist ut med WHO-5 i fase 5. Fase 6: behandler må ta ett
/// av to valg på en fullført, ikke-behandlet besvarelse — Godkjenn (åpner for
/// valgfri deling med pasienten) eller Forkast (oppretter og varsler om en
/// NY tildeling av samme test, se TestTildelingsService.TildelOgVarsleAsync).
/// Etter godkjenning: Kopier (utklippstavle), Skriv ut, og Send kopi til
/// pasienten (varsler i tillegg til å dele, i motsetning til den stille
/// synlighetsbryteren). Å åpne siden markerer en eventuell tilhørende
/// BehandlerMelding som lest. Sidene i rapporten (forside + én per
/// TestSide + evt. historikk) vises som separate "ark" med neste/forrige i
/// visningen, se wwwroot/js/rapport.js.
/// </summary>
public sealed class RapportModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly TestTildelingsService _tildelingsService;
    private readonly BehandlerMeldingService _meldingService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public RapportModel(
        AppDbContext db, TestService testService, TestTildelingsService tildelingsService,
        BehandlerMeldingService meldingService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _tildelingsService = tildelingsService;
        _meldingService = meldingService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public sealed record SvarRad(string Sporsmal, string SvarLabel);
    public sealed record SideMedSvar(TestSide Side, IReadOnlyList<SvarRad> Svar);

    public Pasient? Pasient { get; private set; }
    public Test? Test { get; private set; }
    public TestTildeling? Tildeling { get; private set; }
    public TestSkaaring? Skaaring { get; private set; }
    public List<SideMedSvar> Sider { get; private set; } = new();
    public IReadOnlyList<SkaaringHistorikkPunkt> Historikk { get; private set; } = Array.Empty<SkaaringHistorikkPunkt>();
    public string? Melding { get; private set; }

    /// <summary>
    /// Sammendraget (tittel/intro/skåring/utvikling over tid) er ALLTID ett
    /// ark, uansett testens størrelse. Råskårene (de faktiske svarene) ligger
    /// alltid til slutt — ett ark per TestSide — ETTER sammendraget, ikke rett
    /// etter skåringen som før. For WHO-5 (én TestSide) gir dette nøyaktig to
    /// ark: side 1 = sammendrag, side 2 = svar.
    /// </summary>
    public int TotalAntallArk => 1 + Sider.Count;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();

        var funnet = await LastInnAsync(id, behandlerId, cancellationToken);
        if (!funnet)
        {
            return NotFound();
        }

        await _meldingService.MarkerLestForTildelingAsync(behandlerId, id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostGodkjennAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        if (!await LastInnAsync(id, behandlerId, cancellationToken))
        {
            return NotFound();
        }

        await _testService.GodkjennRapportAsync(id, cancellationToken);
        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "GodkjennRapport",
            nameof(TestTildeling), id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage(new { id });
    }

    /// <summary>Forkaster besvarelsen og sender testen på nytt til samme pasient — se TestService.ForkastRapportAsync.</summary>
    public async Task<IActionResult> OnPostForkastAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        if (!await LastInnAsync(id, behandlerId, cancellationToken))
        {
            return NotFound();
        }

        var forkastet = await _testService.ForkastRapportAsync(id, cancellationToken);
        if (forkastet && Pasient is not null && Test is not null)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            await _tildelingsService.TildelOgVarsleAsync(
                new[] { Pasient.Id }, new[] { Test.Id }, behandlerId: behandlerId, administratorId: null,
                baseUrl: baseUrl, cancellationToken: cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "ForkastRapportOgSendPaaNytt",
                nameof(TestTildeling), id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage("/Pasienter/Detaljer", new { id = Pasient?.Id });
    }

    /// <summary>Gjør rapporten synlig (hvis ikke alt) OG varsler pasienten med en lenke — se TestTildelingsService.SendRapportKopiAsync.</summary>
    public async Task<IActionResult> OnPostSendKopiAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        if (!await LastInnAsync(id, behandlerId, cancellationToken))
        {
            return NotFound();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sendt = await _tildelingsService.SendRapportKopiAsync(id, baseUrl, cancellationToken);
        Melding = sendt
            ? "Kopi sendt til pasienten."
            : "Fant ingen kontaktinfo å varsle pasienten på — rapporten er likevel gjort synlig for pasienten når hen logger inn.";

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "SendRapportkopiTilPasient",
            nameof(TestTildeling), id.ToString(), cancellationToken: cancellationToken);

        await LastInnAsync(id, behandlerId, cancellationToken);
        return Page();
    }

    private async Task<bool> LastInnAsync(long id, long behandlerId, CancellationToken cancellationToken)
    {
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null)
        {
            return false;
        }

        Pasient = await _db.Pasienter.FirstOrDefaultAsync(
            p => p.Id == innhold.Tildeling.PasientId && p.BehandlerId == behandlerId, cancellationToken);
        if (Pasient is null)
        {
            return false;
        }

        Tildeling = innhold.Tildeling;
        Test = innhold.Test;

        Skaaring = await _testService.BeregnSkaaringAsync(id, cancellationToken);
        if (Skaaring is null)
        {
            return false;
        }

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

        if (Test.Kode is not null)
        {
            Historikk = await _testService.HentSkaaringHistorikkAsync(Pasient.Id, Test.Kode, cancellationToken);
        }

        return true;
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
