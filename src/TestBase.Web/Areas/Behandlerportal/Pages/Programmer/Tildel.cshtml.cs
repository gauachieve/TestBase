using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Programmer;

/// <summary>
/// Tildel et program til enkeltpasient(er) eller en gruppe (fase 4) — "samme måte som du tildeler
/// en test, men du setter også en starttid" (brukerens krav): starttidspunktet (ukedag+klokkeslett)
/// er forhåndsutfylt fra programmets egen mal, men overstyrbart her per tildeling.
/// </summary>
public sealed class TildelModel : PageModel
{
    private readonly ProgramService _programService;
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public TildelModel(ProgramService programService, AppDbContext db, ICurrentUserContext currentUser)
    {
        _programService = programService;
        _db = db;
        _currentUser = currentUser;
    }

    public Behandlingsprogram? Program { get; private set; }
    public IReadOnlyList<Pasient> MinePasienter { get; private set; } = Array.Empty<Pasient>();
    public IReadOnlyList<Gruppe> MineGrupper { get; private set; } = Array.Empty<Gruppe>();
    public string? Feilmelding { get; private set; }
    public int? AntallTildelt { get; private set; }

    [BindProperty]
    public string Modus { get; set; } = "Pasienter";

    [BindProperty]
    public List<long> ValgtePasientIder { get; set; } = new();

    [BindProperty]
    public long? ValgtGruppeId { get; set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var personlige = await _programService.HentPersonligAsync(behandlerId, cancellationToken);
        Program = personlige.FirstOrDefault(p => p.Id == id);
        if (Program is null)
        {
            return NotFound();
        }

        MinePasienter = await _db.Pasienter.Where(p => p.BehandlerId == behandlerId && p.Status != PasientStatus.Arkivert).OrderBy(p => p.Navn).ToListAsync(cancellationToken);
        MineGrupper = await _db.Grupper.Where(g => g.BehandlerId == behandlerId && !g.ErArkivert).OrderBy(g => g.Navn).ToListAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = HentBehandlerId();
        var personlige = await _programService.HentPersonligAsync(behandlerId, cancellationToken);
        Program = personlige.FirstOrDefault(p => p.Id == id);
        if (Program is null)
        {
            return NotFound();
        }
        MinePasienter = await _db.Pasienter.Where(p => p.BehandlerId == behandlerId && p.Status != PasientStatus.Arkivert).OrderBy(p => p.Navn).ToListAsync(cancellationToken);
        MineGrupper = await _db.Grupper.Where(g => g.BehandlerId == behandlerId && !g.ErArkivert).OrderBy(g => g.Navn).ToListAsync(cancellationToken);

        long? gruppeId = null;
        IReadOnlyList<long> pasientIder;
        if (Modus == "Gruppe")
        {
            if (ValgtGruppeId is null)
            {
                Feilmelding = "Velg en gruppe.";
                return Page();
            }
            gruppeId = ValgtGruppeId;
            pasientIder = await _db.Pasienter.Where(p => p.GruppeId == ValgtGruppeId && p.Status != PasientStatus.Arkivert).Select(p => p.Id).ToListAsync(cancellationToken);
        }
        else
        {
            pasientIder = ValgtePasientIder;
        }

        if (pasientIder.Count == 0)
        {
            Feilmelding = "Velg minst én pasient (eller en gruppe med minst ett medlem).";
            return Page();
        }

        AntallTildelt = await _programService.TildelAsync(id, pasientIder, gruppeId, behandlerId: behandlerId, administratorId: null, cancellationToken);
        return Page();
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
