using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Programmer;

/// <summary>
/// Opprett/rediger et Behandlingsprogram (fase 3/4) — ÉN side for begge, id null = nytt. Hver
/// drop har sin egen rad med et flerutvalg av tester (rekkefølge = listerekkefølge i
/// flerutvalget, IKKE en egen drag-sorter — se docs/beslutningslogg.md for denne bevisste
/// forenklingen).
/// </summary>
public sealed class RedigerModel : PageModel
{
    private readonly ProgramService _programService;
    private readonly TestService _testService;
    private readonly ICurrentUserContext _currentUser;

    public RedigerModel(ProgramService programService, TestService testService, ICurrentUserContext currentUser)
    {
        _programService = programService;
        _testService = testService;
        _currentUser = currentUser;
    }

    public sealed class DropFormRad
    {
        public int DagerEtterForrige { get; set; }
        public string FraKlokkeslett { get; set; } = "09:00";
        public string TilKlokkeslett { get; set; } = "18:00";
        public bool UnngaaNatt { get; set; } = true;
        public List<long> TestIder { get; set; } = new();
    }

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public string? Forklaring { get; set; }

    [BindProperty]
    public DayOfWeek StartUkedag { get; set; } = DayOfWeek.Monday;

    [BindProperty]
    public string StartKlokkeslett { get; set; } = "09:00";

    [BindProperty]
    public List<DropFormRad> Drops { get; set; } = new();

    public long? ProgramId { get; private set; }
    public bool DropsLaast { get; private set; }
    public string? Feilmelding { get; private set; }
    public bool Lagret { get; private set; }
    public IReadOnlyList<Test> TilgjengeligeTester { get; private set; } = Array.Empty<Test>();

    public async Task<IActionResult> OnGetAsync(long? id, CancellationToken cancellationToken)
    {
        TilgjengeligeTester = await _testService.HentAktiveTesterAsync(cancellationToken);

        if (id is null)
        {
            Drops.Add(new DropFormRad());
            return Page();
        }

        var behandlerId = HentBehandlerId();
        var personlige = await _programService.HentPersonligAsync(behandlerId, cancellationToken);
        var program = personlige.FirstOrDefault(p => p.Id == id);
        if (program is null || program.OpprettetAvBehandlerId != behandlerId)
        {
            return NotFound();
        }

        var medDrops = await _programService.HentMedDropsAsync(program.Id, cancellationToken);
        if (medDrops is null)
        {
            return NotFound();
        }

        ProgramId = program.Id;
        Navn = program.Navn;
        Forklaring = program.Forklaring;
        StartUkedag = program.StartUkedag;
        StartKlokkeslett = program.StartKlokkeslett.ToString(@"hh\:mm");
        Drops = medDrops.Drops.Count == 0
            ? new List<DropFormRad> { new() }
            : medDrops.Drops.Select(d => new DropFormRad
            {
                DagerEtterForrige = d.DagerEtterForrige,
                FraKlokkeslett = d.FraKlokkeslett.ToString(@"hh\:mm"),
                TilKlokkeslett = d.TilKlokkeslett.ToString(@"hh\:mm"),
                UnngaaNatt = d.UnngaaNatt,
                TestIder = medDrops.TestIderPerDropId.GetValueOrDefault(d.Id, new List<long>())
            }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long? id, CancellationToken cancellationToken)
    {
        ProgramId = id;
        TilgjengeligeTester = await _testService.HentAktiveTesterAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Navn er påkrevd.";
            return Page();
        }
        var gyldigeDrops = Drops.Where(d => d.TestIder.Count > 0).ToList();
        if (gyldigeDrops.Count == 0)
        {
            Feilmelding = "Legg til minst én drop med minst én test.";
            return Page();
        }
        if (!TimeSpan.TryParse(StartKlokkeslett, out var startKl))
        {
            Feilmelding = "Ugyldig starttidspunkt.";
            return Page();
        }

        var dropInput = new List<ProgramDropInput>();
        foreach (var rad in gyldigeDrops)
        {
            if (!TimeSpan.TryParse(rad.FraKlokkeslett, out var fra) || !TimeSpan.TryParse(rad.TilKlokkeslett, out var til))
            {
                Feilmelding = "Ugyldig klokkeslett i en av dropsene.";
                return Page();
            }
            dropInput.Add(new ProgramDropInput(Math.Max(0, rad.DagerEtterForrige), fra, til, rad.UnngaaNatt, rad.TestIder));
        }

        var behandlerId = HentBehandlerId();
        if (id is null)
        {
            var nytt = await _programService.OpprettAsync(behandlerId, Navn, Forklaring, StartUkedag, startKl, dropInput, cancellationToken);
            return RedirectToPage("Index", new { opprettet = nytt.Id });
        }

        var (lykkes, dropsLaast, feilmelding) = await _programService.OppdaterAsync(id.Value, behandlerId, Navn, Forklaring, StartUkedag, startKl, dropInput, cancellationToken);
        if (!lykkes)
        {
            Feilmelding = feilmelding;
            return Page();
        }
        DropsLaast = dropsLaast;
        Feilmelding = feilmelding;
        Lagret = true;
        return Page();
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
