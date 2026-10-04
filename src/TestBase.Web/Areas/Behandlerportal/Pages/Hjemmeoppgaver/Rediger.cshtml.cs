using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Hjemmeoppgaver;

/// <summary>
/// Opprett/rediger en hjemmeoppgave (fase 1, se docs/beslutningslogg.md "Hjemmeoppgaver og
/// programmer") — ÉN side for begge, id null = ny. Flat ledd-liste (brukerens eget svar: "for
/// nå") bundet via indekserte skjemafelt (Ledd[0].Sporsmalstekst osv.), rader lagt til/fjernet
/// klient-side (se wwwroot/js/hjemmeoppgave-editor.js) — ASP.NET Cores standard modellbinding
/// håndterer en List&lt;T&gt; fra sammenhengende indekser uten noe eget parse-arbeid.
/// </summary>
public sealed class RedigerModel : PageModel
{
    private readonly HjemmeoppgaveService _hjemmeoppgaveService;
    private readonly ICurrentUserContext _currentUser;

    public RedigerModel(HjemmeoppgaveService hjemmeoppgaveService, ICurrentUserContext currentUser)
    {
        _hjemmeoppgaveService = hjemmeoppgaveService;
        _currentUser = currentUser;
    }

    public sealed class LeddFormRad
    {
        public string Sporsmalstekst { get; set; } = string.Empty;
        public string? Instruksjon { get; set; }
        public TestSvartype Svartype { get; set; } = TestSvartype.Fritekst;
        public string? Svaralternativer { get; set; }
        public bool ErPaakrevd { get; set; }
        public string? BildeData { get; set; }
        public string? BildeContentType { get; set; }
    }

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public string? Beskrivelse { get; set; }

    [BindProperty]
    public string? BelonningsTittel { get; set; }

    [BindProperty]
    public string? Belonningstekst { get; set; }

    [BindProperty]
    public List<LeddFormRad> Ledd { get; set; } = new();

    public long? TestId { get; private set; }
    public bool LeddLaast { get; private set; }
    public string? Feilmelding { get; private set; }
    public bool Lagret { get; private set; }

    public async Task<IActionResult> OnGetAsync(long? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            Ledd.Add(new LeddFormRad());
            return Page();
        }

        var personlige = await _hjemmeoppgaveService.HentPersonligAsync(HentBehandlerId(), cancellationToken);
        var test = personlige.FirstOrDefault(t => t.Id == id);
        if (test is null || test.OpprettetAvBehandlerId != HentBehandlerId())
        {
            return NotFound();
        }

        TestId = test.Id;
        Navn = test.Navn;
        Beskrivelse = test.Beskrivelse;
        BelonningsTittel = test.BelonningsTittel;
        Belonningstekst = test.Belonningstekst;

        // Flat liste — nøyaktig én TestSide, se HjemmeoppgaveService.OpprettAsync.
        var innhold = await _hjemmeoppgaveService.HentTestLeddForRedigeringAsync(test.Id, cancellationToken);
        Ledd = innhold.Count == 0
            ? new List<LeddFormRad> { new() }
            : innhold.Select(l => new LeddFormRad
            {
                Sporsmalstekst = l.Sporsmalstekst,
                Instruksjon = l.Instruksjon,
                Svartype = l.Svartype,
                Svaralternativer = l.Svaralternativer,
                ErPaakrevd = l.ErPaakrevd,
                BildeData = l.BildeData,
                BildeContentType = l.BildeContentType
            }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long? id, CancellationToken cancellationToken)
    {
        TestId = id;
        var gyldigeLedd = Ledd.Where(l => !string.IsNullOrWhiteSpace(l.Sporsmalstekst) || l.Svartype == TestSvartype.Bilde).ToList();
        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Navn er påkrevd.";
            return Page();
        }
        if (gyldigeLedd.Count == 0)
        {
            Feilmelding = "Legg til minst ett ledd.";
            return Page();
        }

        var leddInput = gyldigeLedd.Select(l => new HjemmeoppgaveLeddInput(
            l.Sporsmalstekst, l.Instruksjon, l.Svartype, l.Svaralternativer, l.ErPaakrevd, l.BildeData, l.BildeContentType)).ToList();

        if (id is null)
        {
            var nyTest = await _hjemmeoppgaveService.OpprettAsync(HentBehandlerId(), Navn, Beskrivelse, BelonningsTittel, Belonningstekst, leddInput, cancellationToken);
            return RedirectToPage("Index", new { opprettet = nyTest.Id });
        }

        var (lykkes, leddLaast, feilmelding) = await _hjemmeoppgaveService.OppdaterAsync(
            id.Value, HentBehandlerId(), Navn, Beskrivelse, BelonningsTittel, Belonningstekst, leddInput, cancellationToken);
        if (!lykkes)
        {
            Feilmelding = feilmelding;
            return Page();
        }

        LeddLaast = leddLaast;
        Feilmelding = feilmelding; // informasjonstekst om låsingen, ikke en feil i streng forstand
        Lagret = true;
        return Page();
    }

    private long HentBehandlerId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
