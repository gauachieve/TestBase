using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Tildel;

/// <summary>Steg 2 av tildelingsflyten for admin — se Behandlerportal-motstykket for detaljer om flyten.</summary>
public sealed class TesterModel : PageModel
{
    private readonly TestService _testService;
    private readonly TestTildelingsService _tildelingsService;
    private readonly PlanlagtTildelingService _planlagtTildelingService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly AppDbContext _db;

    public TesterModel(
        TestService testService, TestTildelingsService tildelingsService, PlanlagtTildelingService planlagtTildelingService,
        ICurrentUserContext currentUser, IAuditLogger auditLogger, AppDbContext db)
    {
        _testService = testService;
        _tildelingsService = tildelingsService;
        _planlagtTildelingService = planlagtTildelingService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _db = db;
    }

    /// <summary>Se Behandlerportal-motstykket for full begrunnelse av alle Plan*-feltene.</summary>
    [BindProperty]
    public string PlanTidspunktValg { get; set; } = "Na";

    [BindProperty]
    public string? PlanEgendefinertLokal { get; set; }

    [BindProperty]
    public bool PlanGjentaAktiv { get; set; }

    [BindProperty]
    public DayOfWeek? PlanGjentaUkedag { get; set; }

    [BindProperty]
    public string? PlanGjentaKlokkeslett { get; set; }

    [BindProperty]
    public int? PlanGjentaAntall { get; set; }

    public bool PlanlagtOpprettet { get; private set; }
    public DateTimeOffset? PlanlagtTidspunktVisning { get; private set; }

    [BindProperty]
    public string PasientIderCsv { get; set; } = string.Empty;

    [BindProperty]
    public List<long> TestIder { get; set; } = new();

    /// <summary>Valgt i oppsummerings-dialogen — se Behandlerportal-motstykket for full begrunnelse.</summary>
    [BindProperty]
    public Varslingspreferanse Varslingsmetode { get; set; } = Varslingspreferanse.Begge;

    /// <summary>
    /// Bugliste punkt 13: KUN relevant når minst én valgt test er Test.FyllesUtAvBehandler.
    /// Admin kan tildele til EN HVILKEN SOM HELST pasient (ikke begrenset til egne, i motsetning
    /// til en behandler) — null betyr "bruk pasientens egen behandler" (uendret oppførsel fra før
    /// dette feltet fantes), en eksplisitt verdi overstyrer hvem som får oppgaven OG rapporten.
    /// Se TestTildeling.AnsvarligBehandlerId sin XML-doc for hele bakgrunnen.
    /// </summary>
    [BindProperty]
    public long? AnsvarligBehandlerId { get; set; }

    public IReadOnlyList<Behandler> AktiveBehandlere { get; private set; } = Array.Empty<Behandler>();

    public IReadOnlyList<TestService.KategoriMedTester> KategoriTre { get; private set; } = Array.Empty<TestService.KategoriMedTester>();
    public IReadOnlyList<PasientMedBehandlernavn> ValgtePasienter { get; private set; } = Array.Empty<PasientMedBehandlernavn>();
    public IReadOnlyDictionary<long, int> EstimertMinutterPerTestId { get; private set; } = new Dictionary<long, int>();
    public string? Feilmelding { get; private set; }
    public TildelingsBatchResultat? Resultat { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (TempData.Peek("TildelPasientIder") is not string csv || string.IsNullOrWhiteSpace(csv))
        {
            Feilmelding = "Ingen pasienter valgt. Gå tilbake og velg pasienter først.";
            return;
        }

        PasientIderCsv = csv;
        await LastValgtePasienterAsync(csv, cancellationToken);
        KategoriTre = await _testService.HentKategoriTreAsync(cancellationToken: cancellationToken);
        await LastAktiveBehandlereAsync(cancellationToken);

        var alleTestIder = KategoriTre.SelectMany(k => k.Tester).Select(t => t.Id).Distinct().ToList();
        var antallLedd = await _testService.HentAntallLeddPerTestAsync(alleTestIder, cancellationToken);
        EstimertMinutterPerTestId = antallLedd.ToDictionary(kv => kv.Key, kv => Math.Max(1, (int)Math.Ceiling(kv.Value * 15.0 / 60)));
    }

    public async Task<IActionResult> OnPostSendAsync(CancellationToken cancellationToken)
    {
        await LastValgtePasienterAsync(PasientIderCsv, cancellationToken);
        KategoriTre = await _testService.HentKategoriTreAsync(cancellationToken: cancellationToken);
        await LastAktiveBehandlereAsync(cancellationToken);

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
        Resultat = await _tildelingsService.TildelOgVarsleAsync(
            pasientIder, testIder, behandlerId: null, administratorId: HentAdministratorId(),
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(),
            baseUrl: $"{Request.Scheme}://{Request.Host}",
            varslingsmetode: Varslingsmetode, ansvarligBehandlerId: AnsvarligBehandlerId, cancellationToken: cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "TildelTesterBatch",
            nameof(TestTildeling), AuditBatch.EntityId(testIder),
            $"TestIder {string.Join(",", testIder)}; PasientIder {string.Join(",", pasientIder)}", cancellationToken);

        return Page();
    }

    /// <summary>Se Behandlerportal-motstykket for full begrunnelse.</summary>
    public async Task<IActionResult> OnPostPlanleggAsync(CancellationToken cancellationToken)
    {
        await LastValgtePasienterAsync(PasientIderCsv, cancellationToken);
        KategoriTre = await _testService.HentKategoriTreAsync(cancellationToken: cancellationToken);

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

        DateTimeOffset planlagtUtc;
        if (PlanTidspunktValg == "ImorgenArbeidstid")
        {
            planlagtUtc = PlanlagtTildelingService.BeregnImorgenArbeidstidUtc(DateTimeOffset.UtcNow);
        }
        else if (PlanTidspunktValg == "Egendefinert")
        {
            if (!DateTime.TryParse(PlanEgendefinertLokal, out var lokalDt))
            {
                Feilmelding = "Ugyldig tidspunkt valgt.";
                return Page();
            }
            var osloTid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
            planlagtUtc = new DateTimeOffset(DateTime.SpecifyKind(lokalDt, DateTimeKind.Unspecified), osloTid.GetUtcOffset(lokalDt));
        }
        else
        {
            planlagtUtc = DateTimeOffset.UtcNow;
        }

        DayOfWeek? gjentaUkedag = null;
        TimeSpan? gjentaKlokkeslett = null;
        int? gjentaAntall = null;
        if (PlanGjentaAktiv && PlanGjentaUkedag is not null && PlanGjentaAntall is > 0
            && TimeSpan.TryParse(PlanGjentaKlokkeslett, out var parsetKlokkeslett))
        {
            gjentaUkedag = PlanGjentaUkedag;
            gjentaKlokkeslett = parsetKlokkeslett;
            gjentaAntall = PlanGjentaAntall;
            planlagtUtc = PlanlagtTildelingService.BeregnNesteForekomstUtc(planlagtUtc.AddSeconds(-1), gjentaUkedag.Value, gjentaKlokkeslett.Value);
        }

        var pasientIder = ValgtePasienter.Select(p => p.Pasient.Id).ToList();
        var administratorId = HentAdministratorId();
        await _planlagtTildelingService.OpprettAsync(
            pasientIder, testIder, behandlerId: null, administratorId: administratorId,
            new Dictionary<long, decimal?>(), Varslingsmetode, planlagtUtc,
            gjentaUkedag, gjentaKlokkeslett, gjentaAntall, opprettetAvUserId: administratorId, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "PlanleggTildelingBatch",
            nameof(TestTildeling), AuditBatch.EntityId(testIder),
            $"TestIder {string.Join(",", testIder)}; PasientIder {string.Join(",", pasientIder)}, planlagt {planlagtUtc:O}", cancellationToken);

        PlanlagtOpprettet = true;
        PlanlagtTidspunktVisning = planlagtUtc;
        return Page();
    }

    private async Task LastValgtePasienterAsync(string csv, CancellationToken cancellationToken)
    {
        var onskedeIder = ParseIder(csv).ToHashSet();
        var tilgjengelige = await _tildelingsService.HentTilgjengeligePasienterAsync(null, cancellationToken);
        ValgtePasienter = tilgjengelige.Where(p => onskedeIder.Contains(p.Pasient.Id)).ToList();
    }

    /// <summary>Bugliste punkt 13: kandidatlisten for "ansvarlig behandler"-dropdownen. Sortering
    /// skjer i MINNET, ikke via EF OrderBy — Visningsnavn er en beregnet C#-property EF Core ikke
    /// kan oversette til SQL (kjent fallgruve, se CLAUDE.md).</summary>
    private async Task LastAktiveBehandlereAsync(CancellationToken cancellationToken)
    {
        var behandlere = await _db.Behandlere.Where(b => b.Status == BehandlerStatus.Aktiv).ToListAsync(cancellationToken);
        AktiveBehandlere = behandlere.OrderBy(b => b.Visningsnavn).ToList();
    }

    private static IReadOnlyList<long> ParseIder(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => long.TryParse(s, out var id) ? (long?)id : null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToList();

    private long HentAdministratorId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
