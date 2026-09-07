using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;

namespace TestBase.Web.Areas.Admin.Pages.Okonomi;

/// <summary>
/// Superadmin-only (se SuperadminOmrade i Program.cs). Bevisst holdt
/// EKSTREMT enkel for v1 — kun to summerte tall, ingen regnskapsstandard-
/// formatering ennå (bruker vil selv gi eksakt tekst/format senere, se
/// docs/beslutningslogg.md "Partner System + Test Monetization").
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public decimal InntektFraSalgKr { get; private set; }
    public decimal UtgiftTilSalgKr { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        InntektFraSalgKr = await _db.Pengebevegelser
            .Where(p => p.Type == PengebevegelseType.PlattformInntekt)
            .SumAsync(p => p.BelopKr, cancellationToken);

        UtgiftTilSalgKr = await _db.Pengebevegelser
            .Where(p => p.Type == PengebevegelseType.PartnerAndel || p.Type == PengebevegelseType.BehandlerHonorar)
            .SumAsync(p => p.BelopKr, cancellationToken);
    }
}
