using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Providers;
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
    private readonly EktBankIdInnstillingService _ektBankIdInnstillinger;
    private readonly DriftsbryterInnstillingService _driftsbrytere;
    private readonly IVippsClient _vipps;
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly IConfiguration _configuration;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public MinSideModel(
        AppDbContext db, TestService testService, BetaInnstillingService betaInnstillinger,
        EktBankIdInnstillingService ektBankIdInnstillinger, DriftsbryterInnstillingService driftsbrytere,
        IVippsClient vipps, IAuthenticationSchemeProvider schemes, IConfiguration configuration,
        IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _betaInnstillinger = betaInnstillinger;
        _ektBankIdInnstillinger = ektBankIdInnstillinger;
        _driftsbrytere = driftsbrytere;
        _vipps = vipps;
        _schemes = schemes;
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

    /// <summary>
    /// I MOTSETNING TIL betalingsmodus-bryteren over, IKKE begrenset til beta — ekte
    /// BankID gjelder for live akkurat som beta. Vist KUN når "BankIdInnlogging"-
    /// schemaet faktisk er registrert (dvs. Idura-nøkler er satt i dette miljøet,
    /// se Program.cs) — ellers ville bryteren vært virkningsløs uansett stilling.
    /// Se EktBankIdInnstilling/EktBankIdInnstillingService og
    /// docs/beslutningslogg.md "Ekte BankID — driftsbryter uten redeploy".
    /// </summary>
    public bool VisEktBankIdBryter { get; private set; }
    public bool EktBankIdAktiv { get; private set; }

    /// <summary>SMS-driftsbryteren vises for Superadmin/Utvikler på ALLE miljøer — se DriftsbryterSmsSender, som gjelder uansett Miljo:ErBeta.</summary>
    public bool VisDriftsbrytere => _currentUser.Role is UserRole.Superadmin or UserRole.Utvikler;

    /// <summary>
    /// Betaling/utbetaling-driftsbryterne er KUN virkningsfulle utenfor beta — beta har allerede
    /// egen Mock/Test-styring for begge via betalingsmodus-bryteren over (BetaSwitchingVippsClient/
    /// BetaSwitchingStripeClient dekker alltid alt, uansett DriftsbryterInnstilling). Å vise dem på
    /// beta også ville vært nøyaktig samme "virkningsløs bryter"-fallgruve som BankID-bryteren
    /// eksplisitt unngår, se CLAUDE.md "Kjente fallgruver".
    /// </summary>
    public bool VisBetalingUtbetalingDriftsbrytere => VisDriftsbrytere && !VisBetalingsmodusBryter;

    public bool SmsErAktivNaa { get; private set; }
    public bool BetalingErAktivNaa { get; private set; }
    public bool UtbetalingErAktivNaa { get; private set; }

    public bool SmsKonfigurert => !string.IsNullOrWhiteSpace(_configuration["Vonage:ApiKey"]);
    public bool BetalingKonfigurert => !string.IsNullOrWhiteSpace(_configuration["Vipps:ClientId"]) || !string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]);
    public bool UtbetalingKonfigurert => !string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]);

    /// <summary>
    /// Vipps kaller ALDRI vår webhook-sti bare fordi Vipps:WebhookSecret er satt i
    /// konfigurasjonen — en webhook-mottaker må eksplisitt REGISTRERES hos Vipps
    /// (Webhooks API) først. Dette verktøyet gjør akkurat det, og viser hemmeligheten
    /// Vipps returnerer KUN denne ene gangen (må lagres i Key Vault manuelt med en gang)
    /// — se docs/beslutningslogg.md "Vipps-webhook registrert aldri hos Vipps".
    /// </summary>
    public bool VisVippsWebhookVerktoy => VisDriftsbrytere && VippsProduksjonKonfigurert;
    public IReadOnlyList<VippsWebhookInfo> VippsWebhooker { get; private set; } = Array.Empty<VippsWebhookInfo>();
    public string? VippsWebhookListeFeil { get; private set; }
    public string? VippsWebhookNyttSecret { get; private set; }
    public string? VippsWebhookFeilmelding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LastOppgaverAsync(cancellationToken);
        if (VisBetalingsmodusBryter)
        {
            var innstilling = await _betaInnstillinger.HentAsync(cancellationToken);
            VippsModus = innstilling.VippsModus;
            StripeModus = innstilling.StripeModus;
        }

        VisEktBankIdBryter = _currentUser.Role is UserRole.Superadmin or UserRole.Utvikler
            && await _schemes.GetSchemeAsync("BankIdInnlogging") is not null;
        if (VisEktBankIdBryter)
        {
            EktBankIdAktiv = (await _ektBankIdInnstillinger.HentAsync(cancellationToken)).ErAktiv;
        }

        if (VisDriftsbrytere)
        {
            var driftsbrytere = await _driftsbrytere.HentAsync(cancellationToken);
            SmsErAktivNaa = driftsbrytere.SmsErAktiv;
            BetalingErAktivNaa = driftsbrytere.BetalingErAktiv;
            UtbetalingErAktivNaa = driftsbrytere.UtbetalingErAktiv;
        }

        if (VisVippsWebhookVerktoy)
        {
            var liste = await _vipps.HentWebhookerAsync(cancellationToken);
            VippsWebhooker = liste.Webhooker;
            VippsWebhookListeFeil = liste.Success ? null : liste.ErrorMessage;
        }

        VippsWebhookNyttSecret = TempData["VippsWebhookNyttSecret"] as string;
        VippsWebhookFeilmelding = TempData["VippsWebhookFeilmelding"] as string;
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

    /// <summary>
    /// Slår ekte BankID for admin/behandler-innlogging av/på med UMIDDELBAR virkning,
    /// uten redeploy/omstart — se EktBankIdInnstilling. Auditlogget eksplisitt, samme
    /// begrunnelse som betalingsmodus-bryteren over: en sikkerhetssensitiv bryter, ikke
    /// en vanlig CRUD-handling. Sjekker autorisasjon FERSKT her (ikke bare via
    /// OnGetAsync sin cachede VisEktBankIdBryter, som aldri kjører på en ren POST).
    /// </summary>
    public async Task<IActionResult> OnPostSettEktBankIdAsync(bool erAktiv, CancellationToken cancellationToken)
    {
        if (_currentUser.Role is not (UserRole.Superadmin or UserRole.Utvikler) ||
            await _schemes.GetSchemeAsync("BankIdInnlogging") is null)
        {
            return Forbid();
        }

        var userId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var uid) ? uid : 0;
        await _ektBankIdInnstillinger.SettErAktivAsync(erAktiv, userId, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "EndreEktBankIdAktiv",
            nameof(EktBankIdInnstilling), "1", $"ErAktiv={erAktiv}", cancellationToken);

        return RedirectToPage();
    }

    /// <summary>Slår SMS-driftsbryteren av/på med umiddelbar virkning — se DriftsbryterSmsSender. Sjekker autorisasjon ferskt (POST kjører aldri OnGetAsync sin cachede VisDriftsbrytere).</summary>
    public async Task<IActionResult> OnPostSettSmsAsync(bool erAktiv, CancellationToken cancellationToken)
    {
        if (_currentUser.Role is not (UserRole.Superadmin or UserRole.Utvikler))
        {
            return Forbid();
        }

        var userId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var uid) ? uid : 0;
        await _driftsbrytere.SettSmsErAktivAsync(erAktiv, userId, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "EndreSmsDriftsbryter",
            nameof(DriftsbryterInnstilling), "1", $"ErAktiv={erAktiv}", cancellationToken);

        return RedirectToPage();
    }

    /// <summary>
    /// Slår betaling (Vipps+Stripe patientbetaling)/utbetaling (Stripe Connect) av/på med
    /// umiddelbar virkning — se DriftsbryterVippsClient/DriftsbryterStripeClient. KUN virkningsfullt
    /// utenfor beta, se VisBetalingUtbetalingDriftsbrytere; sjekker likevel ikke erBeta her, siden
    /// et POST-forsøk på beta (f.eks. rett URL) bare lagrer en verdi som aldri leses der — ufarlig.
    /// </summary>
    public async Task<IActionResult> OnPostSettBetalingUtbetalingAsync(bool betalingErAktiv, bool utbetalingErAktiv, CancellationToken cancellationToken)
    {
        if (_currentUser.Role is not (UserRole.Superadmin or UserRole.Utvikler))
        {
            return Forbid();
        }

        var userId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var uid) ? uid : 0;
        await _driftsbrytere.SettBetalingOgUtbetalingAsync(betalingErAktiv, utbetalingErAktiv, userId, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "EndreBetalingUtbetalingDriftsbryter",
            nameof(DriftsbryterInnstilling), "1", $"Betaling={betalingErAktiv}, Utbetaling={utbetalingErAktiv}", cancellationToken);

        return RedirectToPage();
    }

    /// <summary>Registrerer en NY webhook-mottaker hos Vipps for dette miljøets egen URL — se VisVippsWebhookVerktoy.</summary>
    public async Task<IActionResult> OnPostRegistrerVippsWebhookAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.Role is not (UserRole.Superadmin or UserRole.Utvikler))
        {
            return Forbid();
        }

        var url = $"{Request.Scheme}://{Request.Host}{TestBase.Web.Security.PaymentWebhooks.VippsWebhookSti}";
        var resultat = await _vipps.RegistrerWebhookAsync(url, cancellationToken);

        if (resultat.Success)
        {
            TempData["VippsWebhookNyttSecret"] = resultat.Secret;
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "RegistrerVippsWebhook",
                "VippsWebhook", resultat.WebhookId ?? "ukjent", $"Url {url}", cancellationToken);
        }
        else
        {
            TempData["VippsWebhookFeilmelding"] = resultat.ErrorMessage;
        }

        return RedirectToPage();
    }

    /// <summary>Fjerner en feilregistrert/utdatert webhook-mottaker hos Vipps — se VisVippsWebhookVerktoy.</summary>
    public async Task<IActionResult> OnPostSlettVippsWebhookAsync(string webhookId, CancellationToken cancellationToken)
    {
        if (_currentUser.Role is not (UserRole.Superadmin or UserRole.Utvikler))
        {
            return Forbid();
        }

        var slettet = await _vipps.SlettWebhookAsync(webhookId, cancellationToken);
        if (!slettet)
        {
            TempData["VippsWebhookFeilmelding"] = $"Kunne ikke slette webhook {webhookId} hos Vipps.";
        }
        else
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "SlettVippsWebhook",
                "VippsWebhook", webhookId, cancellationToken: cancellationToken);
        }

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
            nameof(TestTilgangForespoersel), AuditBatch.EntityId(foresporselId),
            details: $"ForesporselIder {string.Join(",", foresporselId)}", cancellationToken: cancellationToken);
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
