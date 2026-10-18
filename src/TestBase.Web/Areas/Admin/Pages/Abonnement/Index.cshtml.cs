using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Abonnement;

/// <summary>
/// Superadmin-only (se SuperadminOmrade i Program.cs) — redigering av
/// AbonnementOppsett (kostnads-/prosentkonstantene i de tre behandler-
/// abonnementsmodellene, se TestPrisberegner) og KlinikkSeterTier (klinikkenes
/// sete-baserte prising, se KlinikkPrisService). Se docs/beslutningslogg.md
/// "Abonnement/subscription-system".
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly AbonnementOppsettService _oppsettService;
    private readonly KorFitOppsettService _korFitOppsettService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _auditLogger;

    public IndexModel(
        AppDbContext db, AbonnementOppsettService oppsettService, KorFitOppsettService korFitOppsettService,
        ICurrentUserContext currentUser, IAuditLogger auditLogger)
    {
        _db = db;
        _oppsettService = oppsettService;
        _korFitOppsettService = korFitOppsettService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public AbonnementOppsett Oppsett { get; private set; } = null!;
    public List<KlinikkSeterTier> SeterTiers { get; private set; } = new();
    public bool OppsettLagret { get; private set; }
    public bool TierLagret { get; private set; }

    /// <summary>KOR/FIT-tidsforskyvningene (se KorFitOppsettService) — egen, liten seksjon på denne siden i stedet for et eget Area-underområde (unngår å måtte legge en ny mappe til AuthorizeAreaFolder-listen, se CLAUDE.md sin fallgruve om nye, ubeskyttede sidermapper).</summary>
    public KorFitOppsett KorFitOppsett { get; private set; } = null!;
    public bool KorFitOppsettLagret { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Oppsett = await _oppsettService.HentAsync(cancellationToken);
        KorFitOppsett = await _korFitOppsettService.HentAsync(cancellationToken);
        SeterTiers = await _db.KlinikkSeterTiers.OrderBy(t => t.FraAntallSeter).ToListAsync(cancellationToken);
        if (SeterTiers.Count == 0)
        {
            SeterTiers = await SeedStandardTiersAsync(cancellationToken);
        }

        OppsettLagret = TempData["OppsettLagret"] as bool? ?? false;
        TierLagret = TempData["TierLagret"] as bool? ?? false;
        KorFitOppsettLagret = TempData["KorFitOppsettLagret"] as bool? ?? false;
    }

    public async Task<IActionResult> OnPostLagreKorFitOppsettAsync(KorFitOppsett innsendt, CancellationToken cancellationToken)
    {
        await _korFitOppsettService.LagreAsync(innsendt, BrukerId(), cancellationToken);
        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "LagreKorFitOppsett",
            nameof(KorFitOppsett), "1", cancellationToken: cancellationToken);

        TempData["KorFitOppsettLagret"] = true;
        return Redirect(Url.Page("./Index")! + "#korfit");
    }

    /// <summary>
    /// Foreslåtte startsjikt (ingen tidligere brukerspesifiserte tall fantes —
    /// se docs/beslutningslogg.md "Abonnement/subscription-system"), kun satt
    /// dersom tabellen er helt tom — superadmin kan redigere/slette umiddelbart.
    /// </summary>
    private async Task<List<KlinikkSeterTier>> SeedStandardTiersAsync(CancellationToken cancellationToken)
    {
        var standard = new List<KlinikkSeterTier>
        {
            new() { FraAntallSeter = 1, TilAntallSeter = 5, KrPerSeteManed = 199m },
            new() { FraAntallSeter = 6, TilAntallSeter = 15, KrPerSeteManed = 169m },
            new() { FraAntallSeter = 16, TilAntallSeter = 30, KrPerSeteManed = 139m },
            new() { FraAntallSeter = 31, TilAntallSeter = null, KrPerSeteManed = 109m }
        };
        _db.KlinikkSeterTiers.AddRange(standard);
        await _db.SaveChangesAsync(cancellationToken);
        return standard;
    }

    private long BrukerId() => long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

    public async Task<IActionResult> OnPostLagreOppsettAsync(AbonnementOppsett innsendt, CancellationToken cancellationToken)
    {
        await _oppsettService.LagreAsync(innsendt, BrukerId(), cancellationToken);
        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "LagreAbonnementOppsett",
            nameof(AbonnementOppsett), "1", cancellationToken: cancellationToken);

        TempData["OppsettLagret"] = true;
        return Redirect(Url.Page("./Index")! + "#oppsett");
    }

    public async Task<IActionResult> OnPostLeggTilTierAsync(int fraAntallSeter, int? tilAntallSeter, decimal krPerSeteManed, CancellationToken cancellationToken)
    {
        var tier = new KlinikkSeterTier
        {
            FraAntallSeter = fraAntallSeter,
            TilAntallSeter = tilAntallSeter,
            KrPerSeteManed = krPerSeteManed
        };
        _db.KlinikkSeterTiers.Add(tier);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "LeggTilKlinikkSeterTier",
            nameof(KlinikkSeterTier), tier.Id.ToString(), cancellationToken: cancellationToken);

        TempData["TierLagret"] = true;
        return Redirect(Url.Page("./Index")! + "#seter");
    }

    public async Task<IActionResult> OnPostSlettTierAsync(long id, CancellationToken cancellationToken)
    {
        var tier = await _db.KlinikkSeterTiers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tier is not null)
        {
            _db.KlinikkSeterTiers.Remove(tier);
            await _db.SaveChangesAsync(cancellationToken);
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "SlettKlinikkSeterTier",
                nameof(KlinikkSeterTier), id.ToString(), cancellationToken: cancellationToken);
        }

        return Redirect(Url.Page("./Index")! + "#seter");
    }
}
