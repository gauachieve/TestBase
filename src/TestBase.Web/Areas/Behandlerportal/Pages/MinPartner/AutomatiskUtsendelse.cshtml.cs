using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.MinPartner;

/// <summary>
/// Bugliste 2026-10-11 punkt 4 (del 2+3 av 3) — klinikk-admin velger to lister tester/
/// hjemmeoppgaver (fra klinikkens egen PartnerTestTilgang-allow-list, se
/// TestService.HentKategoriTreAsync) som automatisk tildeles+varsles enhver pasient som enten
/// legges til i klinikken, eller flyttes til "Avsluttet" — selve utsendelsen skjer i
/// PartnerAutomatiskUtsendelseService.UtforTriggerAsync, kalt fra Pasienter/Ny.cshtml.cs,
/// Pasienter/Gruppeimport.cshtml.cs og Pasienter/Index.cshtml.cs sine Arkiver-handlere.
/// </summary>
public sealed class AutomatiskUtsendelseModel : PageModel
{
    private readonly TestService _testService;
    private readonly PartnerAutomatiskUtsendelseService _utsendelseService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public AutomatiskUtsendelseModel(
        TestService testService, PartnerAutomatiskUtsendelseService utsendelseService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _testService = testService;
        _utsendelseService = utsendelseService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public IReadOnlyList<TestService.KategoriMedTester> KategoriTre { get; private set; } = Array.Empty<TestService.KategoriMedTester>();
    public IReadOnlySet<long> ForhaandsvalgteVedNyPasient { get; private set; } = new HashSet<long>();
    public IReadOnlySet<long> ForhaandsvalgteVedAvsluttet { get; private set; } = new HashSet<long>();
    public string? Melding { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null || !_currentUser.ErPartnerAdministrator)
        {
            return Forbid();
        }

        await LastAltAsync(_currentUser.PartnerId.Value, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostLagreVedNyPasientAsync(List<long> testIder, CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null || !_currentUser.ErPartnerAdministrator)
        {
            return Forbid();
        }

        await _utsendelseService.OppdaterAsync(
            _currentUser.PartnerId.Value, PartnerAutomatiskUtsendelseUtloser.VedNyPasient, testIder, HentBehandlerId(), cancellationToken);
        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterAutomatiskUtsendelseVedNyPasient",
            nameof(PartnerAutomatiskUtsendelse), _currentUser.PartnerId.Value.ToString(),
            details: $"TestIder {string.Join(",", testIder)}", cancellationToken: cancellationToken);

        Melding = "Lagret.";
        await LastAltAsync(_currentUser.PartnerId.Value, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostLagreVedAvsluttetAsync(List<long> testIder, CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null || !_currentUser.ErPartnerAdministrator)
        {
            return Forbid();
        }

        await _utsendelseService.OppdaterAsync(
            _currentUser.PartnerId.Value, PartnerAutomatiskUtsendelseUtloser.VedAvsluttet, testIder, HentBehandlerId(), cancellationToken);
        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterAutomatiskUtsendelseVedAvsluttet",
            nameof(PartnerAutomatiskUtsendelse), _currentUser.PartnerId.Value.ToString(),
            details: $"TestIder {string.Join(",", testIder)}", cancellationToken: cancellationToken);

        Melding = "Lagret.";
        await LastAltAsync(_currentUser.PartnerId.Value, cancellationToken);
        return Page();
    }

    private async Task LastAltAsync(long partnerId, CancellationToken cancellationToken)
    {
        KategoriTre = await _testService.HentKategoriTreAsync(partnerId, cancellationToken);

        var vedNyPasient = await _utsendelseService.HentForPartnerAsync(partnerId, PartnerAutomatiskUtsendelseUtloser.VedNyPasient, cancellationToken);
        var vedAvsluttet = await _utsendelseService.HentForPartnerAsync(partnerId, PartnerAutomatiskUtsendelseUtloser.VedAvsluttet, cancellationToken);
        ForhaandsvalgteVedNyPasient = _utsendelseService.ForhaandsvalgteTestIder(vedNyPasient);
        ForhaandsvalgteVedAvsluttet = _utsendelseService.ForhaandsvalgteTestIder(vedAvsluttet);
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
