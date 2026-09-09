using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

public sealed class RedigerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public RedigerModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public long Id { get; set; }

    [BindProperty]
    public string? Navn { get; set; }

    [BindProperty]
    public string? Gruppenavn { get; set; }

    [BindProperty]
    public string Personnummer { get; set; } = string.Empty;

    [BindProperty]
    public string MobilNr { get; set; } = string.Empty;

    [BindProperty]
    public string Epost { get; set; } = string.Empty;

    public string? Feilmelding { get; private set; }

    /// <summary>Kolleger i samme partnerskap som pasientens EIENDE behandler — tomt hvis den behandleren er uavhengig (ingen partnerskap å bytte innenfor), se OnPostByttBehandlerAsync.</summary>
    public IReadOnlyList<Behandler> BehandlereIPartnerskapet { get; private set; } = Array.Empty<Behandler>();
    public long EierBehandlerId { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pasient is null || !await HarTilgangAsync(pasient, cancellationToken))
        {
            return RedirectToPage("Index");
        }

        Id = pasient.Id;
        Navn = pasient.Navn;
        Gruppenavn = pasient.Gruppenavn;
        Personnummer = pasient.Personnummer;
        MobilNr = pasient.MobilNr;
        Epost = pasient.Email;
        EierBehandlerId = pasient.BehandlerId;
        await LastBehandlereIPartnerskapetAsync(pasient.BehandlerId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == Id, cancellationToken);
        if (pasient is null || !await HarTilgangAsync(pasient, cancellationToken))
        {
            return RedirectToPage("Index");
        }

        if (string.IsNullOrWhiteSpace(Personnummer) || string.IsNullOrWhiteSpace(MobilNr) || string.IsNullOrWhiteSpace(Epost))
        {
            Feilmelding = "Personnummer, mobilnummer og e-post er alle obligatoriske.";
            EierBehandlerId = pasient.BehandlerId;
            await LastBehandlereIPartnerskapetAsync(pasient.BehandlerId, cancellationToken);
            return Page();
        }

        // Personnummer er kryptert (se AppDbContext) og kan derfor ikke håndheves
        // unikt med en SQL-indeks — sammenlign i minnet, ekskluder denne pasienten selv.
        var andrePasienter = await _db.Pasienter
            .Where(p => p.Id != Id && p.Status != PasientStatus.Arkivert)
            .ToListAsync(cancellationToken);
        if (andrePasienter.Any(p => p.Personnummer == Personnummer))
        {
            Feilmelding = "Det finnes allerede en annen pasient med dette personnummeret.";
            EierBehandlerId = pasient.BehandlerId;
            await LastBehandlereIPartnerskapetAsync(pasient.BehandlerId, cancellationToken);
            return Page();
        }

        pasient.Navn = string.IsNullOrWhiteSpace(Navn) ? null : Navn;
        pasient.Gruppenavn = string.IsNullOrWhiteSpace(Gruppenavn) ? null : Gruppenavn;
        pasient.Personnummer = Personnummer;
        pasient.MobilNr = MobilNr;
        pasient.Email = Epost;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterPasient",
            nameof(Pasient), pasient.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }

    /// <summary>
    /// Popup-valget i viewet — bytter hvilken behandler som eier pasienten,
    /// begrenset til kolleger i SAMME partnerskap som pasientens nåværende
    /// behandler (se docs/beslutningslogg.md). Egen handler (ikke en del av
    /// hovedskjemaet) slik at et vanlig "Lagre" ikke ved et uhell bytter
    /// behandler samtidig som andre felt endres.
    /// </summary>
    public async Task<IActionResult> OnPostByttBehandlerAsync(long id, long nyBehandlerId, CancellationToken cancellationToken)
    {
        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pasient is null || !await HarTilgangAsync(pasient, cancellationToken))
        {
            return RedirectToPage("Index");
        }

        var eierPartnerId = await _db.Behandlere.Where(b => b.Id == pasient.BehandlerId).Select(b => b.PartnerId).FirstOrDefaultAsync(cancellationToken);
        var nyBehandler = eierPartnerId is null
            ? null
            : await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == nyBehandlerId && b.PartnerId == eierPartnerId, cancellationToken);
        if (nyBehandler is null)
        {
            Feilmelding = "Ugyldig valg av ny behandler.";
            Id = pasient.Id;
            Navn = pasient.Navn;
            Gruppenavn = pasient.Gruppenavn;
            Personnummer = pasient.Personnummer;
            MobilNr = pasient.MobilNr;
            Epost = pasient.Email;
            EierBehandlerId = pasient.BehandlerId;
            await LastBehandlereIPartnerskapetAsync(pasient.BehandlerId, cancellationToken);
            return Page();
        }

        var forrigeBehandlerId = pasient.BehandlerId;
        pasient.BehandlerId = nyBehandlerId;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "ByttPasientBehandler",
            nameof(Pasient), pasient.Id.ToString(), $"Fra {forrigeBehandlerId} til {nyBehandlerId}", cancellationToken);

        return RedirectToPage(new { id });
    }

    private async Task LastBehandlereIPartnerskapetAsync(long eierBehandlerId, CancellationToken cancellationToken)
    {
        var partnerId = await _db.Behandlere.Where(b => b.Id == eierBehandlerId).Select(b => b.PartnerId).FirstOrDefaultAsync(cancellationToken);
        if (partnerId is null)
        {
            BehandlereIPartnerskapet = Array.Empty<Behandler>();
            return;
        }

        BehandlereIPartnerskapet = await _db.Behandlere
            .Where(b => b.PartnerId == partnerId)
            .OrderBy(b => b.Etternavn)
            .ToListAsync(cancellationToken);
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

    /// <summary>Egen pasient ELLER (partner-admin OG pasientens behandler tilhører samme partner) — se Detaljer.cshtml.cs sitt motstykke.</summary>
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
