using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

/// <summary>
/// Samlet liste over ALLE godkjente rapporter for én pasient (bugliste 2026-09-15),
/// uavhengig av om behandler i tillegg har delt dem med pasienten (i motsetning til
/// Pasientportal-motstykket, som kun viser de delte). Lenket fra en egen knapp på
/// Detaljer.cshtml. Samme tilgangsregel som Detaljer/Rapport: egen pasient, eller
/// partner-admin for en pasient hos en behandler i samme partnerskap.
/// </summary>
public sealed class GodkjenteRapporterModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;

    public GodkjenteRapporterModel(AppDbContext db, TestService testService, ICurrentUserContext currentUser)
    {
        _db = db;
        _testService = testService;
        _currentUser = currentUser;
    }

    public sealed record GodkjentRapportRad(TestTildeling Tildeling, string TestNavn);

    public Pasient? Pasient { get; private set; }
    public List<GodkjentRapportRad> Rapporter { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        Pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (Pasient is null || !await HarTilgangAsync(Pasient, cancellationToken))
        {
            return NotFound();
        }

        var tildelinger = await _testService.HentGodkjenteForPasientAsync(id, kunSynligForPasient: false, cancellationToken);
        var testIder = tildelinger.Select(t => t.TestId).Distinct().ToList();
        var testNavn = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);

        Rapporter = tildelinger.Select(t => new GodkjentRapportRad(t, testNavn.GetValueOrDefault(t.TestId, "(ukjent test)"))).ToList();
        return Page();
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

    /// <summary>Samme regel som Detaljer.cshtml.cs — hold i sync hvis den endres.</summary>
    private async Task<bool> HarTilgangAsync(Pasient pasient, CancellationToken cancellationToken)
    {
        if (pasient.BehandlerId == HentBehandlerId())
        {
            return true;
        }

        if (!_currentUser.ErPartnerAdministrator || _currentUser.PartnerId is null)
        {
            return false;
        }

        var eierPartnerId = await _db.Behandlere
            .Where(b => b.Id == pasient.BehandlerId)
            .Select(b => b.PartnerId)
            .FirstOrDefaultAsync(cancellationToken);
        return eierPartnerId == _currentUser.PartnerId;
    }
}
