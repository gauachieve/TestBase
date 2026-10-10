using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Utbetaling;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Utbetalinger;

public sealed record UtbetalingsLinjeRad(UtbetalingsLinje Linje, string MottakerNavn, bool KontoKlarForUtbetaling);

/// <summary>
/// Godkjenn/avvis/gjenta-flyten for én måneds utbetalingsbatch — se
/// docs/beslutningslogg.md "Monthly Stripe Connect payout system".
/// OnPostGodkjennBatchAsync er det ENESTE stedet i hele systemet som faktisk
/// flytter penger (kaller IStripeClient.OpprettOverforingAsync) — ALDRI noe
/// automatisk, kun denne eksplisitte, SuperadminOmrade-beskyttede handlingen.
/// Ett mislykket overføringsforsøk blokkerer ALDRI resten av batchen.
/// </summary>
public sealed class DetaljerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IStripeClient _stripeClient;
    private readonly UtbetalingsOnboardingService _onboarding;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public DetaljerModel(
        AppDbContext db, IStripeClient stripeClient, UtbetalingsOnboardingService onboarding,
        IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _stripeClient = stripeClient;
        _onboarding = onboarding;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public UtbetalingsBatch? Batch { get; private set; }
    public List<UtbetalingsLinjeRad> Linjer { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        Batch = await _db.UtbetalingsBatcher.FindAsync([id], cancellationToken);
        if (Batch is null)
        {
            return NotFound();
        }

        await LastLinjerAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostGodkjennBatchAsync(long id, CancellationToken cancellationToken)
    {
        var batch = await _db.UtbetalingsBatcher.FindAsync([id], cancellationToken);
        if (batch is null)
        {
            return NotFound();
        }

        var linjer = await _db.UtbetalingsLinjer
            .Where(l => l.UtbetalingsBatchId == id && l.Status == UtbetalingsLinjeStatus.Utkast)
            .ToListAsync(cancellationToken);

        foreach (var linje in linjer)
        {
            await ForsokOverforingAsync(linje, cancellationToken);
        }

        batch.GodkjentAvAdministratorId = HentAdministratorId();
        batch.GodkjentUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await OppdaterBatchStatusAsync(id, cancellationToken);

        var linjeIder = linjer.Select(l => l.Id).ToList();
        if (linjeIder.Count > 0)
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GodkjennUtbetalingsBatch",
                nameof(UtbetalingsBatch), AuditBatch.EntityId(linjeIder),
                details: $"Linjer {string.Join(",", linjeIder)}", cancellationToken: cancellationToken);
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAvvisLinjeAsync(long linjeId, CancellationToken cancellationToken)
    {
        var linje = await _db.UtbetalingsLinjer.FindAsync([linjeId], cancellationToken);
        if (linje is null)
        {
            return NotFound();
        }

        linje.Status = UtbetalingsLinjeStatus.Avvist;
        await _db.SaveChangesAsync(cancellationToken);
        await OppdaterBatchStatusAsync(linje.UtbetalingsBatchId, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "AvvisUtbetalingsLinje",
            nameof(UtbetalingsLinje), linje.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage(new { id = linje.UtbetalingsBatchId });
    }

    public async Task<IActionResult> OnPostGjentaLinjeAsync(long linjeId, CancellationToken cancellationToken)
    {
        var linje = await _db.UtbetalingsLinjer.FindAsync([linjeId], cancellationToken);
        if (linje is null || linje.Status != UtbetalingsLinjeStatus.Feilet)
        {
            return NotFound();
        }

        await ForsokOverforingAsync(linje, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await OppdaterBatchStatusAsync(linje.UtbetalingsBatchId, cancellationToken);

        return RedirectToPage(new { id = linje.UtbetalingsBatchId });
    }

    /// <summary>
    /// Setter linjens status ut fra ett overføringsforsøk — kaster ALDRI, en
    /// mislykket linje blokkerer aldri resten av batchen. Audit-logger HVERT
    /// forsøk (initialt eller gjentatt, se AntallForsok i detaljene) — dekker
    /// BÅDE OnPostGodkjennBatchAsync og OnPostGjentaLinjeAsync siden begge
    /// kaller denne.
    /// </summary>
    private async Task ForsokOverforingAsync(UtbetalingsLinje linje, CancellationToken cancellationToken)
    {
        linje.AntallForsok++;

        var konto = linje.MottakerType == MottakerType.Behandler
            ? await _onboarding.HentForBehandlerAsync(linje.BehandlerId!.Value, cancellationToken)
            : await _onboarding.HentForPartnerAsync(linje.PartnerId!.Value, cancellationToken);

        if (konto is null || !konto.PayoutsEnabled)
        {
            linje.Status = UtbetalingsLinjeStatus.Feilet;
            linje.SisteFeilmelding = "Mottaker har ikke fullført Stripe-onboarding ennå.";
        }
        else
        {
            var resultat = await _stripeClient.OpprettOverforingAsync(
                konto.StripeAccountId, linje.BelopKr, $"utbetaling-{linje.Id}", cancellationToken);

            if (resultat.Success)
            {
                linje.Status = UtbetalingsLinjeStatus.Overfort;
                linje.StripeTransferId = resultat.StripeTransferId;
                linje.OverfortUtc = DateTimeOffset.UtcNow;
                linje.SisteFeilmelding = null;
            }
            else
            {
                linje.Status = UtbetalingsLinjeStatus.Feilet;
                linje.SisteFeilmelding = resultat.ErrorMessage;
            }
        }

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(),
            linje.Status == UtbetalingsLinjeStatus.Overfort ? "UtbetalingsLinjeOverfort" : "UtbetalingsLinjeFeilet",
            nameof(UtbetalingsLinje), linje.Id.ToString(),
            details: $"Belop={linje.BelopKr}, Mottaker={linje.MottakerType}:{linje.BehandlerId ?? linje.PartnerId}, Forsok={linje.AntallForsok}",
            cancellationToken: cancellationToken);
    }

    private async Task OppdaterBatchStatusAsync(long batchId, CancellationToken cancellationToken)
    {
        var batch = await _db.UtbetalingsBatcher.FindAsync([batchId], cancellationToken);
        if (batch is null)
        {
            return;
        }

        var linjer = await _db.UtbetalingsLinjer.Where(l => l.UtbetalingsBatchId == batchId).ToListAsync(cancellationToken);
        var relevante = linjer.Where(l => l.Status != UtbetalingsLinjeStatus.Avvist).ToList();

        if (relevante.Count == 0)
        {
            batch.Status = UtbetalingsBatchStatus.Avvist;
        }
        else if (relevante.All(l => l.Status == UtbetalingsLinjeStatus.Overfort))
        {
            batch.Status = UtbetalingsBatchStatus.Godkjent;
        }
        else if (relevante.Any(l => l.Status is UtbetalingsLinjeStatus.Overfort or UtbetalingsLinjeStatus.Feilet or UtbetalingsLinjeStatus.Godkjent))
        {
            batch.Status = UtbetalingsBatchStatus.DelvisGodkjent;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task LastLinjerAsync(long batchId, CancellationToken cancellationToken)
    {
        var linjer = await _db.UtbetalingsLinjer.Where(l => l.UtbetalingsBatchId == batchId).ToListAsync(cancellationToken);

        var behandlerIder = linjer.Where(l => l.BehandlerId is not null).Select(l => l.BehandlerId!.Value).Distinct().ToList();
        var partnerIder = linjer.Where(l => l.PartnerId is not null).Select(l => l.PartnerId!.Value).Distinct().ToList();

        var behandlere = await _db.Behandlere.Where(b => behandlerIder.Contains(b.Id)).ToListAsync(cancellationToken);
        var partnere = await _db.Partnere.Where(p => partnerIder.Contains(p.Id)).ToListAsync(cancellationToken);
        var kontoer = await _db.UtbetalingsMottakerKontoer
            .Where(k =>
                (k.MottakerType == MottakerType.Behandler && k.BehandlerId != null && behandlerIder.Contains(k.BehandlerId.Value)) ||
                (k.MottakerType == MottakerType.Partner && k.PartnerId != null && partnerIder.Contains(k.PartnerId.Value)))
            .ToListAsync(cancellationToken);

        Linjer = linjer.Select(l =>
        {
            var navn = l.MottakerType == MottakerType.Behandler
                ? behandlere.FirstOrDefault(b => b.Id == l.BehandlerId)?.Visningsnavn ?? $"Behandler #{l.BehandlerId}"
                : partnere.FirstOrDefault(p => p.Id == l.PartnerId)?.Navn ?? $"Partner #{l.PartnerId}";

            var kontoKlar = kontoer.Any(k =>
                k.MottakerType == l.MottakerType &&
                (l.MottakerType == MottakerType.Behandler ? k.BehandlerId == l.BehandlerId : k.PartnerId == l.PartnerId) &&
                k.PayoutsEnabled);

            return new UtbetalingsLinjeRad(l, navn, kontoKlar);
        }).ToList();
    }

    private long HentAdministratorId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
