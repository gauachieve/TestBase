using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Okonomi;

public sealed record PeriodeRad(string Etikett, decimal InntektKr, decimal UtgiftKr)
{
    public decimal NettoKr => InntektKr - UtgiftKr;
}

/// <summary>
/// Superadmin-only (se SuperadminOmrade i Program.cs). Tre faner: "Oversikt"
/// (opprinnelig måned/år-oppsummering), "Periodisert oppgjør" (det "fine"
/// designforslaget fra 2026-10-10 — Regnskapsoppgjør, nå faktisk koblet til
/// Pengebevegelse via OkonomiRapportService) og "Oppsett" (firmaets egen
/// kontoplan, AdminOkonomiOppsett). Se docs/beslutningslogg.md "Periodiserte
/// oppgjør, del 2".
/// </summary>
public sealed class IndexModel : PageModel
{
    private static readonly CultureInfo Norsk = CultureInfo.GetCultureInfo("nb-NO");

    private readonly AppDbContext _db;
    private readonly OkonomiRapportService _rapportService;
    private readonly AdminOkonomiOppsettService _oppsettService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuditLogger _auditLogger;

    public IndexModel(
        AppDbContext db, OkonomiRapportService rapportService, AdminOkonomiOppsettService oppsettService,
        ICurrentUserContext currentUser, IAuditLogger auditLogger)
    {
        _db = db;
        _rapportService = rapportService;
        _oppsettService = oppsettService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    private sealed record Bevegelse(PengebevegelseType Type, decimal BelopKr, DateTimeOffset OpprettetUtc);

    public List<PeriodeRad> MånederIInneværendeÅr { get; private set; } = new();
    public PeriodeRad HittilIÅr { get; private set; } = new("Hittil i år", 0, 0);
    public List<PeriodeRad> TidligereÅr { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? Aar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Maaned { get; set; }

    public int ValgtAar { get; private set; }
    public int ValgtManed { get; private set; }
    public AdminOkonomiRapport PeriodisertRapport { get; private set; } = null!;

    public AdminOkonomiOppsett Oppsett { get; private set; } = null!;

    public bool OppsettLagret { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var rader = await _db.Pengebevegelser
            .Select(p => new Bevegelse(p.Type, p.BelopKr, p.OpprettetUtc))
            .ToListAsync(cancellationToken);

        var iAar = DateTimeOffset.UtcNow.Year;
        var raderIÅr = rader.Where(p => p.OpprettetUtc.Year == iAar).ToList();

        MånederIInneværendeÅr = raderIÅr
            .GroupBy(p => p.OpprettetUtc.Month)
            .OrderBy(g => g.Key)
            .Select(g => LagRad($"{Norsk.DateTimeFormat.GetMonthName(g.Key)} {iAar}", g))
            .ToList();

        HittilIÅr = LagRad($"Hittil i {iAar}", raderIÅr);

        TidligereÅr = rader
            .Where(p => p.OpprettetUtc.Year < iAar)
            .GroupBy(p => p.OpprettetUtc.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => LagRad(g.Key.ToString(), g))
            .ToList();

        var naa = DateTimeOffset.UtcNow;
        ValgtAar = Aar ?? naa.Year;
        ValgtManed = Maaned ?? naa.Month;
        PeriodisertRapport = await _rapportService.HentAdminRapportAsync(ValgtAar, ValgtManed, cancellationToken);

        Oppsett = await _oppsettService.HentAsync(cancellationToken);
        OppsettLagret = TempData["OppsettLagret"] as bool? ?? false;
    }

    public async Task<IActionResult> OnPostLagreOppsettAsync(AdminOkonomiOppsett innsendt, CancellationToken cancellationToken)
    {
        var brukerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
        await _oppsettService.LagreAsync(innsendt, brukerId, cancellationToken);
        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "LagreAdminOkonomiOppsett",
            nameof(AdminOkonomiOppsett), "1", cancellationToken: cancellationToken);

        TempData["OppsettLagret"] = true;
        return Redirect(Url.Page("./Index")! + "#oppsett");
    }

    private static PeriodeRad LagRad(string etikett, IEnumerable<Bevegelse> bevegelser)
    {
        var liste = bevegelser as IReadOnlyCollection<Bevegelse> ?? bevegelser.ToList();
        var inntekt = liste.Where(p => p.Type == PengebevegelseType.PlattformInntekt).Sum(p => p.BelopKr);
        var utgift = liste.Where(p => p.Type is PengebevegelseType.PartnerAndel or PengebevegelseType.BehandlerHonorar).Sum(p => p.BelopKr);
        return new PeriodeRad(etikett, inntekt, utgift);
    }
}
