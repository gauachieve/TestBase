using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Grupper;

public sealed class IndexModel : PageModel
{
    private readonly GruppeService _grupper;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public IndexModel(GruppeService grupper, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _grupper = grupper;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public sealed record GruppeRad(Gruppe Gruppe, int AntallPasienter);

    public IReadOnlyList<GruppeRad> Grupper { get; private set; } = Array.Empty<GruppeRad>();
    public IReadOnlyList<GruppeRad> ArkiverteGrupper { get; private set; } = Array.Empty<GruppeRad>();

    private long HentBehandlerId() => long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var grupper = await _grupper.HentForBehandlerAsync(behandlerId, cancellationToken);
        var arkiverte = await _grupper.HentArkiverteForBehandlerAsync(behandlerId, cancellationToken);
        var alle = grupper.Concat(arkiverte).Select(g => g.Id).ToList();
        var antallPerGruppe = await _grupper.HentPasientAntallPerGruppeAsync(alle, cancellationToken);
        Grupper = grupper.Select(g => new GruppeRad(g, antallPerGruppe.GetValueOrDefault(g.Id))).ToList();
        ArkiverteGrupper = arkiverte.Select(g => new GruppeRad(g, antallPerGruppe.GetValueOrDefault(g.Id))).ToList();
    }

    /// <summary>Arkiverer flere grupper i ett steg — eierskapssjekk per gruppe, samme prinsipp som enkelt-rad-handleren.</summary>
    public async Task<IActionResult> OnPostArkiverValgteAsync(long[] gruppeId, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var egneGruppeIder = (await _grupper.HentForBehandlerAsync(behandlerId, cancellationToken))
            .Select(g => g.Id).ToHashSet();

        var arkiverte = new List<long>();
        foreach (var id in gruppeId)
        {
            if (egneGruppeIder.Contains(id) && await _grupper.ArkiverAsync(id, cancellationToken))
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
        var behandlerId = HentBehandlerId();
        var gruppe = (await _grupper.HentForBehandlerAsync(behandlerId, cancellationToken)).FirstOrDefault(g => g.Id == id);
        if (gruppe is not null)
        {
            await _grupper.ArkiverAsync(id, cancellationToken);
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "ArkiverGruppe",
                nameof(Gruppe), id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGjenopprettAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var gruppe = (await _grupper.HentArkiverteForBehandlerAsync(behandlerId, cancellationToken)).FirstOrDefault(g => g.Id == id);
        if (gruppe is not null)
        {
            await _grupper.GjenopprettAsync(id, cancellationToken);
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GjenopprettGruppe",
                nameof(Gruppe), id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSlettAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var gruppe = (await _grupper.HentArkiverteForBehandlerAsync(behandlerId, cancellationToken)).FirstOrDefault(g => g.Id == id);
        if (gruppe is not null && await _grupper.SlettGruppeAsync(id, cancellationToken))
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "SlettGruppe",
                nameof(Gruppe), id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }
}
