using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages;

/// <summary>
/// Behandlers personlige side: uleste meldinger (se BehandlerMelding — én per
/// pasient som nettopp har fullført en test), fullførte tester som venter på
/// godkjenning, tester tildelt egne pasienter som ennå ikke er besvart, og
/// (for partner-admin) kollegers utløpte HPR-frister — alt sammen tidligere
/// spredt over en egen "Oppgaver"-side, slått sammen hit (bugliste 2026-09-13
/// punkt 22) siden de i praksis alltid ble sjekket sammen.
/// </summary>
[Authorize(Policy = "BehandlerOmrade")]
public sealed class MinSideModel : PageModel
{
    private readonly BehandlerMeldingService _meldingService;
    private readonly TestService _testService;
    private readonly GruppeService _grupper;
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public MinSideModel(
        BehandlerMeldingService meldingService, TestService testService, GruppeService grupper,
        AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _meldingService = meldingService;
        _testService = testService;
        _grupper = grupper;
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public string? InvitasjonsLenke { get; private set; }

    public IReadOnlyList<MeldingMedDetaljer> UlesteMeldinger { get; private set; } = Array.Empty<MeldingMedDetaljer>();
    public IReadOnlyList<TestService.TildelingMedTestOgPasient> VenterPaaGodkjenning { get; private set; } = Array.Empty<TestService.TildelingMedTestOgPasient>();
    public IReadOnlyList<TestService.TildelingMedTestOgPasient> IkkeFullfort { get; private set; } = Array.Empty<TestService.TildelingMedTestOgPasient>();
    public IReadOnlyList<TestService.TildelingMedTestOgPasient> Godkjente { get; private set; } = Array.Empty<TestService.TildelingMedTestOgPasient>();
    public List<Behandler> UtlopteHprFrister { get; private set; } = new();

    public int AntallOppgaver => UlesteMeldinger.Count + VenterPaaGodkjenning.Count + UtlopteHprFrister.Count;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        UlesteMeldinger = await _meldingService.HentUlesteAsync(behandlerId, cancellationToken);
        VenterPaaGodkjenning = await _testService.HentUgodkjenteFullforteForBehandlerAsync(behandlerId, cancellationToken);
        IkkeFullfort = await _testService.HentIkkeFullforteForBehandlerAsync(behandlerId, cancellationToken);
        Godkjente = await _testService.HentGodkjenteFullforteForBehandlerAsync(behandlerId, cancellationToken);

        var qrToken = await _grupper.SikreBehandlerQrTokenAsync(behandlerId, cancellationToken);
        InvitasjonsLenke = $"{Request.Scheme}://{Request.Host}/BliPasient/b/{qrToken}";

        if (_currentUser.ErPartnerAdministrator && _currentUser.PartnerId is not null)
        {
            var kolleger = await _db.Behandlere
                .Where(b => b.PartnerId == _currentUser.PartnerId && !b.HprGodkjent && b.Status != BehandlerStatus.Arkivert && !b.ErSlettet && b.RegistrertUtc != null)
                .ToListAsync(cancellationToken);

            UtlopteHprFrister = kolleger
                .Where(b => HprPolicy.ErUtlopt(b, DateTimeOffset.UtcNow))
                .OrderBy(b => HprPolicy.BeregnFrist(b))
                .ToList();
        }
    }

    public async Task<IActionResult> OnPostGodkjennHprAsync(long id, CancellationToken cancellationToken)
    {
        if (!_currentUser.ErPartnerAdministrator || _currentUser.PartnerId is null)
        {
            return Forbid();
        }

        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id && b.PartnerId == _currentUser.PartnerId, cancellationToken);
        if (behandler is not null)
        {
            // HprGodkjentAvAdministratorId er reservert for en faktisk Administrator-Id —
            // en partner-admin ER en Behandler, ikke en Administrator, så feltet står
            // bevisst urørt her. Hvem som godkjente står uansett i audit-loggen under.
            behandler.HprGodkjent = true;
            behandler.HprGodkjentUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GodkjennHpr",
                nameof(Behandler), behandler.Id.ToString(), $"HPR-nr {behandler.HprNr}", cancellationToken);
        }

        return RedirectToPage();
    }

    /// <summary>Sletter en ikke-besvart tildeling permanent — se TestService.SlettIkkeFullfortTildelingAsync (kan ikke slette en fullført tildeling).</summary>
    public async Task<IActionResult> OnPostSlettIkkeBesvartAsync(long id, CancellationToken cancellationToken)
    {
        if (await _testService.SlettIkkeFullfortTildelingAsync(id, HentBehandlerId(), cancellationToken))
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "SlettIkkeBesvartTildeling",
                nameof(TestTildeling), id.ToString(), cancellationToken: cancellationToken);
        }

        // Bugliste 2026-10-06 punkt 12: behold brukeren på "ikke besvart"-fanen i stedet for å
        // hoppe tilbake til "venter på godkjenning" (faner.js sin default) — se faner.js.
        return Redirect(Url.Page("./MinSide")! + "#ikke-besvart");
    }

    /// <summary>
    /// Bugliste 2026-10-06 punkt 11: en samle-knapp som sletter ALLE behandlerens ikke-besvarte
    /// tildelinger i ett klikk — ikke sensitivt lagringsmessig siden tildelingen ikke inneholder
    /// noen pasientbesvarelse ennå (se TestService.SlettIkkeFullfortTildelingAsync sin egen
    /// sjekk mot at en FULLFØRT tildeling aldri kan slettes denne veien).
    /// </summary>
    public async Task<IActionResult> OnPostSlettAlleIkkeBesvarteAsync(CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var ikkeFullfort = await _testService.HentIkkeFullforteForBehandlerAsync(behandlerId, cancellationToken);
        var antallSlettet = 0;
        foreach (var rad in ikkeFullfort)
        {
            if (await _testService.SlettIkkeFullfortTildelingAsync(rad.Tildeling.Id, behandlerId, cancellationToken))
            {
                antallSlettet++;
            }
        }

        if (antallSlettet > 0)
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "SlettAlleIkkeBesvarteTildelinger",
                nameof(TestTildeling), AuditBatch.EntityId(ikkeFullfort.Select(r => r.Tildeling.Id).ToList()),
                $"{antallSlettet} tildeling(er) slettet", cancellationToken);
        }

        return Redirect(Url.Page("./MinSide")! + "#ikke-besvart");
    }

    /// <summary>Invaliderer gjeldende QR-kode/lenke umiddelbart — se Behandler.PasientInviteQrToken.</summary>
    public async Task<IActionResult> OnPostRegenererQrAsync(CancellationToken cancellationToken)
    {
        await _grupper.RegenererBehandlerQrTokenAsync(HentBehandlerId(), cancellationToken);
        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "RegenererBehandlerQr",
            nameof(Behandler), HentBehandlerId().ToString(), cancellationToken: cancellationToken);
        return RedirectToPage();
    }

    /// <summary>Selve QR-bildet — se QrBildeGenerator.</summary>
    public async Task<IActionResult> OnGetQrAsync(CancellationToken cancellationToken)
    {
        var qrToken = await _grupper.SikreBehandlerQrTokenAsync(HentBehandlerId(), cancellationToken);
        var lenke = $"{Request.Scheme}://{Request.Host}/BliPasient/b/{qrToken}";
        return File(QrBildeGenerator.GenererPng(lenke), "image/png");
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
