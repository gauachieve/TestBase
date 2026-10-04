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

    public sealed record SvarRad(string Sporsmal, string SvarLabel, string? BehandlerKommentar = null);
    public sealed record SideMedSvar(TestSide Side, IReadOnlyList<SvarRad> Svar);

    /// <summary>Én cutoff-linje til å tegne over resultat-fremdriftsbaren — Posisjon er allerede
    /// omregnet til 0-100% av baren (råskår-cutoffs skaleres mot RaaSkaarMaks, se LastInnAsync).</summary>
    public sealed record CutoffMarkering(string Navn, decimal PosisjonProsent);

    public Pasient? Pasient { get; private set; }
    public Test? Test { get; private set; }
    public TestTildeling? Tildeling { get; private set; }
    public TestSkaaring? Skaaring { get; private set; }

    /// <summary>Sann for en Test.ErHjemmeoppgave-test — rapporten viser rå svar + start-/sluttidspunkt (Tildeling.StartetUtc/FullfortUtc) i stedet for en TestSkaaring, se LastInnAsync.</summary>
    public bool ErHjemmeoppgaveRapport { get; private set; }
    public IReadOnlyList<CutoffMarkering> Cutoffs { get; private set; } = Array.Empty<CutoffMarkering>();

    /// <summary>Se ITestSkaaringsberegner.SignifikantEndringProsentpoeng — NULL skjuler hele
    /// "signifikant endring"-markeringen/forklaringslinjen i "utvikling over tid" (bugliste
    /// 2026-10-04: en tidligere hardkodet 10%-regel, kun gyldig for WHO-5, ble feilaktig vist
    /// for ALLE tester).</summary>
    public double? SignifikantEndringProsentpoeng { get; private set; }

    /// <summary>Radar-graf av de 5 SIPP-118-inspirerte domenene — kun satt for Test.Kode == "sipp118" (se Sipp118RadarBeregner).</summary>
    public Sipp118RadarData? Sipp118Radar { get; private set; }

    /// <summary>To hexagon-radarer (fleksibilitet/rigiditet) — kun satt for Test.Kode == "mpfi_24" (se MpfiRadarBeregner).</summary>
    public MpfiRadarData? MpfiFleksibilitetRadar { get; private set; }
    public MpfiRadarData? MpfiRigiditetRadar { get; private set; }

    /// <summary>Stolpediagram per personlighetsforstyrrelse — kun satt for Test.Kode == "scid5_pf" (se Scid5PfBarBeregner).</summary>
    public Scid5PfBarData? Scid5PfBar { get; private set; }

    /// <summary>Samme grenser som <see cref="Cutoffs"/>, men i RÅ enhet (ikke prosent-skalert) —
    /// brukt i "Kopier alt"-malen der en tekstlig "grenseverdi X" er mer nyttig enn en visuell
    /// strek på en fremdriftsbar som ikke overlever inn i et journalsystem.</summary>
    public IReadOnlyList<TestSkaaringGrenseverdi> RaaCutoffs { get; private set; } = Array.Empty<TestSkaaringGrenseverdi>();
    public List<SideMedSvar> Sider { get; private set; } = new();
    public IReadOnlyList<SkaaringHistorikkPunkt> Historikk { get; private set; } = Array.Empty<SkaaringHistorikkPunkt>();

    /// <summary>Ferdigberegnet SVG-geometri for "utvikling over tid"-grafen — null når det er &lt;2 besvarelser å vise (se UtviklingsGrafBeregner).</summary>
    public UtviklingsGrafData? UtviklingsGraf { get; private set; }

    public string? Melding { get; private set; }

    /// <summary>Neste ugodkjente, fullførte rapport i køen — se bugliste 2026-09-13 punkt 26 ("Neste oppgave").</summary>
    public long? NesteVentendeTildelingId { get; private set; }

    /// <summary>Testens kategori, til rapportens fargekoding — se bugliste 2026-09-13 punkt 27.</summary>
    public string? KategoriNavn { get; private set; }

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
            // Beholder samme honorar som forrige tildeling av denne testen — dette er en
            // "send på nytt", ikke en ny prisingsbeslutning.
            var forrigeHonorar = await _testService.HentSisteHonorarAsync(behandlerId, Test.Id, cancellationToken);
            await _tildelingsService.TildelOgVarsleAsync(
                new[] { Pasient.Id }, new[] { Test.Id }, behandlerId: behandlerId, administratorId: null,
                new Dictionary<long, decimal?> { [Test.Id] = forrigeHonorar },
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

        // Bugliste punkt 13: TestTildeling.AnsvarligBehandlerId (satt kun når noen eksplisitt
        // valgte en ANNEN behandler enn pasientens egen for en behandler-utfylt test, se feltets
        // XML-doc) vinner over Pasient.BehandlerId — ellers havner rapporten alltid hos pasientens
        // vanlige behandler uansett hvem som faktisk ble bedt om å gjennomføre/rapportere på den
        // kliniske vurderingen.
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == innhold.Tildeling.PasientId, cancellationToken);
        if (pasient is null || (innhold.Tildeling.AnsvarligBehandlerId ?? pasient.BehandlerId) != behandlerId)
        {
            return false;
        }
        Pasient = pasient;

        Tildeling = innhold.Tildeling;
        Test = innhold.Test;
        KategoriNavn = await _testService.HentPrimaerKategoriNavnAsync(Test.Id, cancellationToken);
        SignifikantEndringProsentpoeng = _testService.HentSignifikantEndringProsentpoeng(Test.Kode);

        // Hjemmeoppgaver (2026-10-04): ALDRI en TestSkaaring (Test.Kode er null, se
        // TestService.BeregnSkaaringAsync — FinnBeregner(null) returnerer null uendret, ingen
        // kodeendring trengtes der). I MOTSETNING TIL enhver annen score-løs test (som i dag
        // fortsatt 404-er her — bevisst IKKE endret for dem, utenfor scope) hopper vi for EN
        // HJEMMEOPPGAVE eksplisitt over Skaaring-kravet og viser rå svar + start-/sluttidspunkt
        // i stedet, se ErHjemmeoppgaveRapport i viewet.
        ErHjemmeoppgaveRapport = Test.ErHjemmeoppgave;
        if (!ErHjemmeoppgaveRapport)
        {
            Skaaring = await _testService.BeregnSkaaringAsync(id, cancellationToken);
            if (Skaaring is null)
            {
                return false;
            }

            // Cutoff-linjer på resultat-fremdriftsbaren (2026-09-27, samme prinsipp som
            // Grupper/Aggregert sitt histogram) — Histogramgrenser er enten allerede i prosent
            // (VisSomProsentIHistogram, kun WHO-5/WHO-5 VAS) eller i råskår og må skaleres mot
            // RaaSkaarMaks for å plasseres riktig på en 0-100%-bar. Ikke vist når SkjulProsent er
            // satt (se TestSkaaring), siden det ikke finnes noen fremdriftsbar å tegne linjer over da.
            if (!Skaaring.SkjulProsent && Skaaring.RaaSkaarMaks > 0)
            {
                RaaCutoffs = _testService.HentHistogramgrenser(Test.Kode);
                var visSomProsent = _testService.VisSomProsentIHistogram(Test.Kode);
                Cutoffs = RaaCutoffs
                    .Select(g => new CutoffMarkering(g.Navn, visSomProsent ? g.Verdi : g.Verdi * 100m / Skaaring.RaaSkaarMaks))
                    .ToList();
            }

            if (Test.Kode == "sipp118")
            {
                Sipp118Radar = Sipp118RadarBeregner.Beregn(Skaaring.Indikatorer);
            }
            else if (Test.Kode == "scid5_pf")
            {
                Scid5PfBar = Scid5PfBarBeregner.Beregn(innhold.AlleLedd, innhold.EksisterendeSvar);
            }
            else if (Test.Kode == "mpfi_24")
            {
                MpfiFleksibilitetRadar = MpfiRadarBeregner.Beregn(Skaaring.Indikatorer, "Fleksibilitet — ");
                MpfiRigiditetRadar = MpfiRadarBeregner.Beregn(Skaaring.Indikatorer, "Rigiditet — ");
            }
        }

        // Kun behandler-utfylte tester (se Test.FyllesUtAvBehandler) har noensinne en
        // BehandlerKommentar — tomt oppslag/harmløst for enhver annen test.
        var kommentarPerLeddId = await _db.TestSvar
            .Where(s => s.TestTildelingId == id && s.BehandlerKommentar != null)
            .ToDictionaryAsync(s => s.TestLeddId, s => s.BehandlerKommentar!, cancellationToken);

        Sider = innhold.Sider.Select(side =>
        {
            // Bilde-ledd (hjemmeoppgaver) er rent visningsinnhold forfatteren la inn, ALDRI en
            // besvart "spørsmål/svar"-rad — tas derfor ikke med i selve svar-tabellen.
            var svar = innhold.AlleLedd.Where(l => l.TestSideId == side.Id && l.Svartype != TestSvartype.Bilde).Select(ledd =>
            {
                var raaVerdi = innhold.EksisterendeSvar.GetValueOrDefault(ledd.Id, "-");
                var label = ledd.Svartype switch
                {
                    TestSvartype.LikertSkala => TestLeddSvaralternativer.Parse(ledd.Svaralternativer)
                        .FirstOrDefault(p => p.Verdi.ToString() == raaVerdi)?.Tekst ?? raaVerdi,
                    TestSvartype.VisuellAnalogSkala => $"{raaVerdi}/100",
                    _ => raaVerdi
                };
                return new SvarRad(ledd.Sporsmalstekst, label, kommentarPerLeddId.GetValueOrDefault(ledd.Id));
            }).ToList();
            return new SideMedSvar(side, svar);
        }).ToList();

        if (Test.Kode is not null)
        {
            Historikk = await _testService.HentSkaaringHistorikkAsync(Pasient.Id, Test.Kode, cancellationToken);
            if (Historikk.Count > 1)
            {
                var referanselinjer = _testService.HentReferanselinjer(Test.Kode);
                UtviklingsGraf = UtviklingsGrafBeregner.Beregn(Historikk, referanselinjer);
            }
        }

        if (Tildeling.RapportGodkjentUtc is not null)
        {
            var venterPaaGodkjenning = await _testService.HentUgodkjenteFullforteForBehandlerAsync(behandlerId, cancellationToken);
            NesteVentendeTildelingId = venterPaaGodkjenning.Select(t => (long?)t.Tildeling.Id).FirstOrDefault();
        }

        return true;
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
