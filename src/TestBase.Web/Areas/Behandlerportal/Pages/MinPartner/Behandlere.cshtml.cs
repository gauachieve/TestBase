using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.MinPartner;

/// <summary>
/// "Min klinikk" — ren hub-side med knapper videre til underpagene, se "Klinikk-oppdatering" i
/// docs/beslutningslogg.md. Selve kollegalisten/inviter/fjern-logikken ligger nå i
/// MinPartner/Kolleger.cshtml(.cs) (flyttet ut). Åpen for ENHVER behandler i klinikken (se
/// PartnerMedlemOmrade i Program.cs) — kun de fire admin-knappene (Prising/Utbetaling/Automatisk
/// utsendelse/Pasienter) er skjult for en vanlig kollega, se ErPartnerAdministrator under.
/// </summary>
public sealed class BehandlereModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public BehandlereModel(AppDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public string? Feilmelding { get; private set; }

    /// <summary>Egen partner-oversikt (bugliste 2026-09-13 punkt 15) — navn/kontaktinfo/abonnement.</summary>
    public Partner? MinPartner { get; private set; }

    /// <summary>Testene partneren har fått tilgang til av Superadmin, se PartnerTestTilgang — kun brukt til antallet i Klinikkprising-knappens tekst.</summary>
    public List<Test> TilgjengeligeTester { get; private set; } = new();

    public bool ErPartnerAdministrator => _currentUser.ErPartnerAdministrator;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.PartnerId is null)
        {
            return Forbid();
        }

        var partnerId = _currentUser.PartnerId.Value;
        MinPartner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == partnerId, cancellationToken);

        var testIder = await _db.PartnerTestTilganger.Where(t => t.PartnerId == partnerId).Select(t => t.TestId).ToListAsync(cancellationToken);
        TilgjengeligeTester = await _db.Tester.Where(t => testIder.Contains(t.Id)).OrderBy(t => t.Navn).ToListAsync(cancellationToken);

        return Page();
    }
}
