using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Tildel;

/// <summary>
/// Steg 2 av tildelingsflyten: velg tester fra kategori-treet (jf.
/// TestService.HentKategoriTreAsync) for pasientene valgt i steg 1, bekreft i
/// oppsummerings-dialogen (se wwwroot/js/tildel.js), og send. Leser den
/// kommaseparerte pasient-id-listen fra TempData KUN via Peek — den skal
/// overleve et sideoppdatering før innsending, se Pasienter.cshtml.cs.
/// </summary>
public sealed class TesterModel : PageModel
{
    private readonly TestService _testService;
    private readonly TestTildelingsService _tildelingsService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _auditLogger;

    public TesterModel(TestService testService, TestTildelingsService tildelingsService, ICurrentUserContext currentUser, IAuditLogger auditLogger)
    {
        _testService = testService;
        _tildelingsService = tildelingsService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    [BindProperty]
    public string PasientIderCsv { get; set; } = string.Empty;

    [BindProperty]
    public List<long> TestIder { get; set; } = new();

    /// <summary>Valgt i oppsummerings-dialogen — overstyrer pasientens egen lagrede Varslingspreferanse for denne batchen, se TestTildelingsService.TildelOgVarsleAsync.</summary>
    [BindProperty]
    public Varslingspreferanse Varslingsmetode { get; set; } = Varslingspreferanse.Begge;

    /// <summary>
    /// Behandlerens eget honorar per test (kun relevant for tester med prising
    /// konfigurert, se docs/beslutningslogg.md "Partner System + Test
    /// Monetization") — tom/manglende verdi faller tilbake til
    /// Test.TypiskBehandlerHonorarKr i TestPrisberegner. IKKE [BindProperty]:
    /// et Dictionary&lt;long,...&gt; som MVC ikke finner noen "HonorarKr[...]"-
    /// felter for (f.eks. hvis ingen av testene i batchen har prising
    /// konfigurert) faller tilbake til å tolke ALLE andre skjemafelt-NAVN
    /// (som "PasientIderCsv") som dictionary-nøkler og kaster
    /// FormatException når disse ikke er tall — se
    /// docs/beslutningslogg.md. Leses derfor manuelt fra Request.Form i
    /// stedet, se LesHonorarFraSkjema().
    /// </summary>
    public Dictionary<long, decimal?> HonorarKr { get; set; } = new();

    public IReadOnlyList<TestService.KategoriMedTester> KategoriTre { get; private set; } = Array.Empty<TestService.KategoriMedTester>();
    public IReadOnlyDictionary<long, decimal> SistBrukteHonorarPerTestId { get; private set; } = new Dictionary<long, decimal>();
    public IReadOnlyList<PasientMedBehandlernavn> ValgtePasienter { get; private set; } = Array.Empty<PasientMedBehandlernavn>();
    public string? Feilmelding { get; private set; }
    public TildelingsBatchResultat? Resultat { get; private set; }

    /// <summary>Brukes av tildel.js til å speile TestPrisberegner.Beregn client-side for en levende pris-forhåndsvisning i oppsummerings-dialogen.</summary>
    public PrisingskontekstForBehandler Prisingskontekst { get; private set; } = new(false, new Dictionary<long, decimal>(), 0m);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (TempData.Peek("TildelPasientIder") is not string csv || string.IsNullOrWhiteSpace(csv))
        {
            Feilmelding = "Ingen pasienter valgt. Gå tilbake og velg pasienter først.";
            return;
        }

        PasientIderCsv = csv;
        await LastValgtePasienterAsync(csv, cancellationToken);
        KategoriTre = await _testService.HentKategoriTreAsync(_currentUser.PartnerId, cancellationToken);

        var behandlerId = HentBehandlerId();
        var alleTester = KategoriTre.SelectMany(k => k.Tester).GroupBy(t => t.Id).Select(g => g.First()).ToList();
        var sisteHonorar = new Dictionary<long, decimal>();
        foreach (var test in alleTester)
        {
            var sistBrukt = await _testService.HentSisteHonorarAsync(behandlerId, test.Id, cancellationToken);
            // Klemmes til testens NÅVÆRENDE StorstePrisKr — uten dette kan et tidligere
            // brukt honorar som senere ble for høyt etter en admin-prisjustering havne i
            // <input max="..."> med en verdi over maks. HTML5 sin range-validering gjør da
            // feltet stille "invalid" og BLOKKERER hele skjemainnsendingen uten synlig
            // feilmelding (feltet ligger bak den åpne oppsummerings-dialogen) — sett
            // fikk rapportert som "Bekreft og send-knappen gjør ingenting" (ny runde av
            // bugliste 2026-09-13, punkt "sendout"). Se docs/beslutningslogg.md.
            sisteHonorar[test.Id] = test.StorstePrisKr > 0 ? Math.Min(sistBrukt, test.StorstePrisKr) : sistBrukt;
        }
        SistBrukteHonorarPerTestId = sisteHonorar;
        Prisingskontekst = await _tildelingsService.HentPrisingskontekstAsync(behandlerId, alleTester.Select(t => t.Id).ToList(), cancellationToken);
    }

    public async Task<IActionResult> OnPostSendAsync(CancellationToken cancellationToken)
    {
        HonorarKr = LesHonorarFraSkjema();
        await LastValgtePasienterAsync(PasientIderCsv, cancellationToken);
        KategoriTre = await _testService.HentKategoriTreAsync(_currentUser.PartnerId, cancellationToken);

        if (!ValgtePasienter.Any())
        {
            Feilmelding = "Ingen gyldige pasienter valgt. Gå tilbake til steg 1.";
            return Page();
        }

        var testIder = TestIder.Distinct().ToList();
        if (testIder.Count == 0)
        {
            Feilmelding = "Velg minst én test.";
            return Page();
        }

        var pasientIder = ValgtePasienter.Select(p => p.Pasient.Id).ToList();
        var onsketHonorarKrPerTestId = testIder.ToDictionary(id => id, id => HonorarKr.GetValueOrDefault(id));
        Resultat = await _tildelingsService.TildelOgVarsleAsync(
            pasientIder, testIder, behandlerId: HentBehandlerId(), administratorId: null,
            onsketHonorarKrPerTestId, baseUrl: $"{Request.Scheme}://{Request.Host}",
            varslingsmetode: Varslingsmetode, cancellationToken: cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "TildelTesterBatch",
            nameof(TestTildeling), string.Join(",", testIder), $"PasientIder {string.Join(",", pasientIder)}", cancellationToken);

        return Page();
    }

    private async Task LastValgtePasienterAsync(string csv, CancellationToken cancellationToken)
    {
        var onskedeIder = ParseIder(csv).ToHashSet();
        var tilgjengelige = await _tildelingsService.HentTilgjengeligePasienterAsync(HentBehandlerId(), cancellationToken);
        ValgtePasienter = tilgjengelige.Where(p => onskedeIder.Contains(p.Pasient.Id)).ToList();
    }

    private static IReadOnlyList<long> ParseIder(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => long.TryParse(s, out var id) ? (long?)id : null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToList();

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

    private Dictionary<long, decimal?> LesHonorarFraSkjema()
    {
        var resultat = new Dictionary<long, decimal?>();
        foreach (var key in Request.Form.Keys)
        {
            if (!key.StartsWith("HonorarKr[", StringComparison.Ordinal) || !key.EndsWith(']'))
            {
                continue;
            }

            var idDel = key[10..^1];
            if (!long.TryParse(idDel, out var testId))
            {
                continue;
            }

            var raw = Request.Form[key].ToString();
            resultat[testId] = decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var verdi)
                ? verdi
                : null;
        }

        return resultat;
    }
}
