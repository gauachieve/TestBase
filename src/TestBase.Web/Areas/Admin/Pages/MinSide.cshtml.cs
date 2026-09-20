using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages;

/// <summary>
/// Admins "Min side" — tidligere en egen "Oppgaver"-side, slått sammen hit
/// (bugliste 2026-09-13 punkt 22, samme prinsipp som Behandlerportal/MinSide)
/// siden Admin ikke har noen annen personlig side (meldingsinnboks finnes
/// kun for Behandler). Oppgavetyper: behandlere med utløpt, ugodkjent
/// HPR-frist (punkt 11) — delt datakilde med partner-admin sin filtrerte
/// visning på Behandlerportal/MinSide — og, fra 2026-09-15, ventende
/// TestTilgangForespoersel-er fra partner-admins (bulk godkjenn/avvis), se
/// docs/beslutningslogg.md "Test-tilgangsforespørsler".
/// </summary>
[Authorize(Policy = "AdminOmrade")]
public sealed class MinSideModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly BetaInnstillingService _betaInnstillinger;
    private readonly IConfiguration _configuration;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public MinSideModel(
        AppDbContext db, TestService testService, BetaInnstillingService betaInnstillinger, IConfiguration configuration,
        IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _betaInnstillinger = betaInnstillinger;
        _configuration = configuration;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public List<Behandler> UtlopteHprFrister { get; private set; } = new();
    public IReadOnlyList<TestService.VentendeTestTilgangForespoerselRad> VentendeTestTilgangForesporsler { get; private set; } = Array.Empty<TestService.VentendeTestTilgangForespoerselRad>();

    /// <summary>Betalingsmodus-bryteren vises KUN på beta (Miljo:ErBeta) og KUN for Superadmin/dev-rollebytte — se docs/beslutningslogg.md "Beta-miljø".</summary>
    public bool VisBetalingsmodusBryter => _configuration.GetValue("Miljo:ErBeta", false) && _currentUser.Role is UserRole.Superadmin or UserRole.Utvikler;
    public VippsDriftsmodus VippsModus { get; private set; }
    public StripeDriftsmodus StripeModus { get; private set; }
    public bool VippsTestKonfigurert => !string.IsNullOrWhiteSpace(_configuration["Vipps:BetaTest:ClientId"]);
    public bool VippsProduksjonKonfigurert => !string.IsNullOrWhiteSpace(_configuration["Vipps:ClientId"]);
    public bool StripeTestKonfigurert => !string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LastOppgaverAsync(cancellationToken);
        if (VisBetalingsmodusBryter)
        {
            var innstilling = await _betaInnstillinger.HentAsync(cancellationToken);
            VippsModus = innstilling.VippsModus;
            StripeModus = innstilling.StripeModus;
        }
    }

    /// <summary>
    /// Endrer betalingsleverandør-modus for HELE beta-miljøet — påvirker EKTE
    /// betalinger (inkl. mot ekte produksjonskonto for Vipps, se
    /// BetaSwitchingVippsClient) med umiddelbar virkning, uten omstart.
    /// Auditlogget eksplisitt utover det vanlige mønsteret siden dette er en
    /// finansielt følsom bryter, ikke en vanlig CRUD-handling.
    /// </summary>
    public async Task<IActionResult> OnPostSettBetalingsmodusAsync(VippsDriftsmodus vippsModus, StripeDriftsmodus stripeModus, CancellationToken cancellationToken)
    {
        if (!VisBetalingsmodusBryter)
        {
            return Forbid();
        }

        var userId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var uid) ? uid : 0;
        await _betaInnstillinger.SettModusAsync(vippsModus, stripeModus, userId, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "EndreBetaBetalingsmodus",
            nameof(BetaBetalingsinnstilling), "1", $"Vipps={vippsModus}, Stripe={stripeModus}", cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGodkjennValgteTestTilgangAsync(long[] foresporselId, CancellationToken cancellationToken)
    {
        await BehandleValgteTestTilgangAsync(foresporselId, godkjenn: true, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAvvisValgteTestTilgangAsync(long[] foresporselId, CancellationToken cancellationToken)
    {
        await BehandleValgteTestTilgangAsync(foresporselId, godkjenn: false, cancellationToken);
        return RedirectToPage();
    }

    private async Task BehandleValgteTestTilgangAsync(long[] foresporselId, bool godkjenn, CancellationToken cancellationToken)
    {
        if (foresporselId.Length == 0)
        {
            return;
        }

        var administratorId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var aid) ? aid : 0;
        await _testService.BehandleTestTilgangForesporslerAsync(foresporselId, godkjenn, administratorId, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(),
            godkjenn ? "GodkjennTestTilgangForesporsler" : "AvvisTestTilgangForesporsler",
            nameof(TestTilgangForespoersel), string.Join(",", foresporselId), cancellationToken: cancellationToken);
    }

    public async Task<IActionResult> OnPostGodkjennHprAsync(long id, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.HprGodkjent = true;
            var administratorId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var aid) ? aid : (long?)null;
            behandler.HprGodkjentAvAdministratorId = administratorId;
            behandler.HprGodkjentUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GodkjennHpr",
                nameof(Behandler), behandler.Id.ToString(), $"HPR-nr {behandler.HprNr}", cancellationToken);
        }

        return RedirectToPage();
    }

    private async Task LastOppgaverAsync(CancellationToken cancellationToken)
    {
        var behandlere = await _db.Behandlere
            .Where(b => !b.HprGodkjent && b.Status != BehandlerStatus.Arkivert && !b.ErSlettet && b.RegistrertUtc != null)
            .ToListAsync(cancellationToken);

        UtlopteHprFrister = behandlere
            .Where(b => HprPolicy.ErUtlopt(b, DateTimeOffset.UtcNow))
            .OrderBy(b => HprPolicy.BeregnFrist(b))
            .ToList();

        VentendeTestTilgangForesporsler = await _testService.HentVentendeTestTilgangForesporslerAsync(cancellationToken);
    }
}
