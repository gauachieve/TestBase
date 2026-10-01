using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;

namespace TestBase.Shared.Domain.Tilbakemeldinger;

/// <summary>
/// Skriving/lesing av Tilbakemelding — se widgeten (wwwroot/js/tilbakemelding-widget.js),
/// Security/TilbakemeldingApi.cs (innsending + agent-API) og
/// Areas/Admin/Pages/Tilbakemeldinger (manuell gjennomgang).
/// </summary>
public sealed class TilbakemeldingService
{
    private const int MaksMeldingLengde = 4000;
    private const int MaksScreenshotLengde = 6_000_000; // ~6 MB data-URL (base64 er ~1,37x rå bildestørrelse)
    private const int MaksTekniskFeilLengde = 8000;

    private readonly AppDbContext _db;

    public TilbakemeldingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Tilbakemelding> RegistrerAsync(
        string melding,
        string? url,
        string? brukerAgent,
        int? skjermBredde,
        int? skjermHoyde,
        int? vindaugBredde,
        int? vindaugHoyde,
        string? innloggetRolle,
        long? innloggetBrukerId,
        string? tekniskFeilInfo,
        string? screenshotDataUrl,
        CancellationToken cancellationToken = default)
    {
        var tilbakemelding = new Tilbakemelding
        {
            OpprettetUtc = DateTimeOffset.UtcNow,
            Melding = melding.Length > MaksMeldingLengde ? melding[..MaksMeldingLengde] : melding,
            Url = Avkort(url, 1000),
            BrukerAgent = Avkort(brukerAgent, 500),
            SkjermBredde = skjermBredde,
            SkjermHoyde = skjermHoyde,
            VindaugBredde = vindaugBredde,
            VindaugHoyde = vindaugHoyde,
            InnloggetRolle = innloggetRolle,
            InnloggetBrukerId = innloggetBrukerId,
            TekniskFeilInfo = Avkort(tekniskFeilInfo, MaksTekniskFeilLengde),
            ErKrasjRapport = !string.IsNullOrWhiteSpace(tekniskFeilInfo),
            ScreenshotDataUrl = Avkort(screenshotDataUrl, MaksScreenshotLengde),
            Status = TilbakemeldingStatus.Ny
        };

        _db.Tilbakemeldinger.Add(tilbakemelding);
        await _db.SaveChangesAsync(cancellationToken);
        return tilbakemelding;
    }

    public Task<List<Tilbakemelding>> HentSidenAsync(DateTimeOffset siden, CancellationToken cancellationToken = default) =>
        _db.Tilbakemeldinger
            .Where(t => t.OpprettetUtc > siden)
            .OrderBy(t => t.OpprettetUtc)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// ALLE ubehandlede krasjrapporter, UANSETT alder — brukt av agenten (se TilbakemeldingApi.cs
    /// "/api/agent/krasjrapporter") til å plukke opp noe den ikke rakk forrige kjøring, i stedet
    /// for KUN siste 24t (HentSidenAsync). Status Lost/Avvist/Sett regnes som "ferdig vurdert" og
    /// ekskluderes — agenten markerer status selv via OppdaterStatusAsync etter å ha sett på en sak.
    /// </summary>
    public Task<List<Tilbakemelding>> HentApneKrasjrapporterAsync(CancellationToken cancellationToken = default) =>
        _db.Tilbakemeldinger
            .Where(t => t.ErKrasjRapport && (t.Status == TilbakemeldingStatus.Ny || t.Status == TilbakemeldingStatus.UnderArbeid))
            .OrderBy(t => t.OpprettetUtc)
            .ToListAsync(cancellationToken);

    public Task<List<Tilbakemelding>> HentAlleAsync(CancellationToken cancellationToken = default) =>
        _db.Tilbakemeldinger
            .OrderByDescending(t => t.OpprettetUtc)
            .ToListAsync(cancellationToken);

    public async Task OppdaterStatusAsync(long id, TilbakemeldingStatus status, string? notat, CancellationToken cancellationToken = default)
    {
        var tilbakemelding = await _db.Tilbakemeldinger.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tilbakemelding is null)
        {
            return;
        }

        tilbakemelding.Status = status;
        if (notat is not null)
        {
            tilbakemelding.Notat = notat;
        }
        tilbakemelding.SistOppdatertUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? Avkort(string? verdi, int maksLengde) =>
        verdi is null ? null : verdi.Length > maksLengde ? verdi[..maksLengde] : verdi;
}
