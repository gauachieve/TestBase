using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace TestBase.Web.Areas.Admin.Pages.Grupper;

/// <summary>
/// Admin sitt oversiktsbilde over ALLE behandleres grupper, MED full
/// CRUD (Ny/Rediger/Arkiver) — admin skal kunne gjøre det samme som
/// behandlere kan, se prosjektbeskrivelsen. Se Admin/Grupper/Ny og
/// Admin/Grupper/Rediger for selve redigeringen.
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly GruppeService _grupper;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public IndexModel(AppDbContext db, GruppeService grupper, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _grupper = grupper;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public sealed record GruppeRad(Gruppe Gruppe, string? BehandlerNavn, int AntallPasienter, int AntallTester);

    public IReadOnlyList<GruppeRad> Grupper { get; private set; } = Array.Empty<GruppeRad>();
    public IReadOnlyList<GruppeRad> ArkiverteGrupper { get; private set; } = Array.Empty<GruppeRad>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var grupper = await _grupper.HentAlleAsync(cancellationToken);
        var arkiverte = await _grupper.HentAlleArkiverteAsync(cancellationToken);
        var behandlerNavnById = (await _db.Behandlere.ToListAsync(cancellationToken)).ToDictionary(b => b.Id, b => b.Visningsnavn);
        var alleGrupper = grupper.Concat(arkiverte).ToList();
        var gruppeIder = alleGrupper.Select(g => g.Id).ToList();
        var antallPasienterPerGruppe = await _grupper.HentPasientAntallPerGruppeAsync(gruppeIder, cancellationToken);
        var antallTesterPerGruppe = (await _db.GruppeTestTilordninger
                .Where(t => gruppeIder.Contains(t.GruppeId))
                .GroupBy(t => t.GruppeId)
                .Select(g => new { GruppeId = g.Key, Antall = g.Count() })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.GruppeId, x => x.Antall);

        GruppeRad TilRad(Gruppe g) => new(
            g,
            behandlerNavnById.GetValueOrDefault(g.BehandlerId),
            antallPasienterPerGruppe.GetValueOrDefault(g.Id),
            antallTesterPerGruppe.GetValueOrDefault(g.Id));

        Grupper = grupper.Select(TilRad).ToList();
        ArkiverteGrupper = arkiverte.Select(TilRad).ToList();
    }

    /// <summary>Arkiverer flere grupper i ett steg — samme bulk-mønster som Pasienter/Administratorer.</summary>
    public async Task<IActionResult> OnPostArkiverValgteAsync(long[] gruppeId, CancellationToken cancellationToken)
    {
        var arkiverte = new List<long>();
        foreach (var id in gruppeId)
        {
            if (await _grupper.ArkiverAsync(id, cancellationToken))
            {
                arkiverte.Add(id);
            }
        }

        if (arkiverte.Count > 0)
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "ArkiverFlereGrupper",
                nameof(Gruppe), AuditBatch.EntityId(arkiverte), details: $"GruppeIder {string.Join(",", arkiverte)}", cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostArkiverAsync(long id, CancellationToken cancellationToken)
    {
        if (await _grupper.ArkiverAsync(id, cancellationToken))
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "ArkiverGruppe",
                nameof(Gruppe), id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGjenopprettAsync(long id, CancellationToken cancellationToken)
    {
        if (await _grupper.GjenopprettAsync(id, cancellationToken))
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GjenopprettGruppe",
                nameof(Gruppe), id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSlettAsync(long id, CancellationToken cancellationToken)
    {
        if (await _grupper.SlettGruppeAsync(id, cancellationToken))
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "SlettGruppe",
                nameof(Gruppe), id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }
}
