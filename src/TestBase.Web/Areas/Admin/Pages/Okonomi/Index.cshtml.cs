using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;

namespace TestBase.Web.Areas.Admin.Pages.Okonomi;

public sealed record PeriodeRad(string Etikett, decimal InntektKr, decimal UtgiftKr)
{
    public decimal NettoKr => InntektKr - UtgiftKr;
}

/// <summary>
/// Superadmin-only (se SuperadminOmrade i Program.cs). Periodisert: måned for
/// måned for inneværende år (pluss en "hittil i år"-sum), og ett summert tall
/// per tidligere, avsluttede kalenderår — se docs/beslutningslogg.md "Partner
/// System + Test Monetization". Fortsatt ingen regnskapsstandard-formatering
/// (bruker vil selv gi eksakt tekst/format senere).
/// </summary>
public sealed class IndexModel : PageModel
{
    private static readonly CultureInfo Norsk = CultureInfo.GetCultureInfo("nb-NO");

    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    private sealed record Bevegelse(PengebevegelseType Type, decimal BelopKr, DateTimeOffset OpprettetUtc);

    public List<PeriodeRad> MånederIInneværendeÅr { get; private set; } = new();
    public PeriodeRad HittilIÅr { get; private set; } = new("Hittil i år", 0, 0);
    public List<PeriodeRad> TidligereÅr { get; private set; } = new();

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
    }

    private static PeriodeRad LagRad(string etikett, IEnumerable<Bevegelse> bevegelser)
    {
        var liste = bevegelser as IReadOnlyCollection<Bevegelse> ?? bevegelser.ToList();
        var inntekt = liste.Where(p => p.Type == PengebevegelseType.PlattformInntekt).Sum(p => p.BelopKr);
        var utgift = liste.Where(p => p.Type is PengebevegelseType.PartnerAndel or PengebevegelseType.BehandlerHonorar).Sum(p => p.BelopKr);
        return new PeriodeRad(etikett, inntekt, utgift);
    }
}
