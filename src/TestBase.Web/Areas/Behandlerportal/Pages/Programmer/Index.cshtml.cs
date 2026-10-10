using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Programmer;

/// <summary>
/// Fase 5 (se docs/beslutningslogg.md "Hjemmeoppgaver og programmer") — samme fane-mønster
/// (Personlig/Delt/Partner/Opprett) som Hjemmeoppgaver/Index, pluss en "Kjørende"-fane (fase 4)
/// over behandlerens EGNE kjørende programdeltakelser.
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly ProgramService _programService;
    private readonly ICurrentUserContext _currentUser;

    public IndexModel(ProgramService programService, ICurrentUserContext currentUser)
    {
        _programService = programService;
        _currentUser = currentUser;
    }

    public IReadOnlyList<Behandlingsprogram> Personlige { get; private set; } = Array.Empty<Behandlingsprogram>();
    public IReadOnlyList<Behandlingsprogram> DeltMedAlle { get; private set; } = Array.Empty<Behandlingsprogram>();
    public IReadOnlyList<Behandlingsprogram> DeltMedPartner { get; private set; } = Array.Empty<Behandlingsprogram>();
    public IReadOnlyList<ProgramService.KjorendeRad> Kjorende { get; private set; } = Array.Empty<ProgramService.KjorendeRad>();
    public HashSet<long> LikteProgramIder { get; private set; } = new();
    public bool HarPartner { get; private set; }
    public string? Melding { get; private set; }
    public long EgenBehandlerId { get; private set; }

    public async Task OnGetAsync(long? opprettet, CancellationToken cancellationToken)
    {
        await LastAltAsync(cancellationToken);
        if (opprettet is not null)
        {
            Melding = "Programmet er opprettet.";
        }
    }

    public async Task<IActionResult> OnPostSlettAsync(long programId, CancellationToken cancellationToken)
    {
        var lykkes = await _programService.SlettAsync(programId, HentBehandlerId(), cancellationToken);
        Melding = lykkes ? "Programmet er fjernet/arkivert." : "Fant ikke programmet, eller du eier det ikke.";
        await LastAltAsync(cancellationToken);
        return Page();
    }

    /// <summary>Fjerner/arkiverer flere EGNE programmer i ett steg.</summary>
    public async Task<IActionResult> OnPostSlettValgteAsync(long[] programId, CancellationToken cancellationToken)
    {
        var antall = 0;
        foreach (var id in programId)
        {
            if (await _programService.SlettAsync(id, HentBehandlerId(), cancellationToken))
            {
                antall++;
            }
        }

        Melding = antall > 0 ? $"{antall} program(mer) fjernet/arkivert." : "Fant ingen av de valgte programmene.";
        await LastAltAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostKopierAsync(long programId, CancellationToken cancellationToken)
    {
        var kopi = await _programService.KopierAsync(programId, HentBehandlerId(), cancellationToken);
        if (kopi is null)
        {
            Melding = "Fant ikke programmet.";
            await LastAltAsync(cancellationToken);
            return Page();
        }
        return RedirectToPage("Rediger", new { id = kopi.Id });
    }

    public async Task<IActionResult> OnPostLikAsync(long programId, CancellationToken cancellationToken)
    {
        await _programService.LikAsync(HentBehandlerId(), programId, cancellationToken);
        await LastAltAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostFjernLikingAsync(long programId, CancellationToken cancellationToken)
    {
        await _programService.FjernLikingAsync(HentBehandlerId(), programId, cancellationToken);
        await LastAltAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDelMedAlleAsync(long programId, bool verdi, CancellationToken cancellationToken)
    {
        await _programService.SettDeltMedAlleAsync(programId, HentBehandlerId(), verdi, cancellationToken);
        await LastAltAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDelMedPartnerAsync(long programId, bool verdi, CancellationToken cancellationToken)
    {
        await _programService.SettDeltMedPartnerAsync(programId, HentBehandlerId(), verdi, cancellationToken);
        await LastAltAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostPauseKjorendeAsync(long[] deltakelseIder, CancellationToken cancellationToken)
    {
        await _programService.PauseFlereAsync(deltakelseIder, cancellationToken);
        Melding = "Pauset.";
        await LastAltAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostFjernKjorendeAsync(long[] deltakelseIder, CancellationToken cancellationToken)
    {
        await _programService.FjernFlereAsync(deltakelseIder, cancellationToken);
        Melding = "Fjernet.";
        await LastAltAsync(cancellationToken);
        return Page();
    }

    private async Task LastAltAsync(CancellationToken cancellationToken)
    {
        EgenBehandlerId = HentBehandlerId();
        Personlige = await _programService.HentPersonligAsync(EgenBehandlerId, cancellationToken);
        DeltMedAlle = await _programService.HentDeltMedAlleAsync(EgenBehandlerId, cancellationToken);
        DeltMedPartner = await _programService.HentDeltMedPartnerAsync(EgenBehandlerId, cancellationToken);
        LikteProgramIder = await _programService.HentLikteProgramIderAsync(EgenBehandlerId, cancellationToken);
        Kjorende = await _programService.HentKjorendeForBehandlerAsync(EgenBehandlerId, cancellationToken);
        HarPartner = _currentUser.PartnerId is not null;
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
