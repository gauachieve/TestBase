using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Pasientportal.Pages.Tester;

/// <summary>
/// Side-for-side utfylling av en tildelt test, jf. "Definisjon av en test" i
/// kravdokumentet: fremdrift i %, instruksjon per test/side/ledd,
/// Neste/Forrige/Ferdig, og en belønningsside ved fullføring. Hadde tidligere
/// en egen "Lagre"-knapp (lagre-og-bli-på-samme-side) — FJERNET 2026-09-23:
/// den var reelt overflødig (Neste/Ferdig lagrer alltid gjeldende sides svar
/// FØR de flytter videre, se OnPostAsync under) og forvirret pasienter under
/// en reell konferanse til å tro at "Lagre" betydde at testen var levert til
/// behandler. Se docs/beslutningslogg.md.
/// </summary>
[Authorize(Policy = "PasientOmrade")]
public sealed class FyllModel : PageModel
{
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public FyllModel(TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public string? Handling { get; set; }

    public TestMedInnhold? Innhold { get; private set; }
    public int GjeldendeSideNummer { get; private set; } = 1;
    public bool ErFullfort { get; private set; }
    public string? Feilmelding { get; private set; }

    /// <summary>Neste ikke-fullførte tildeling for samme pasient — se bugliste 2026-09-13 punkt 8 ("Ferdigstill og videre til neste test").</summary>
    public long? NesteIkkeFullforteTildelingId { get; private set; }

    /// <summary>Navn på testen i <see cref="NesteIkkeFullforteTildelingId"/> — vist i knappeteksten i stedet for et generisk "neste test".</summary>
    public string? NesteIkkeFullforteTestNavn { get; private set; }

    /// <summary>
    /// Satt (2026-09-23) når for mange ledd sto ubesvart, se
    /// Test.MaksUbesvartProsent/TestService.BeregnSkaaringAsync — vist som en
    /// informasjonsboks på "Ferdig!"-siden, ALDRI en sperre for selve
    /// innsendingen (den har allerede skjedd på dette tidspunktet).
    /// </summary>
    public string? GyldighetsAdvarsel { get; private set; }

    public TestSide? GjeldendeSide => Innhold is null ? null : Innhold.Sider.ElementAtOrDefault(GjeldendeSideNummer - 1);

    public IEnumerable<TestLedd> LeddPaaGjeldendeSide =>
        GjeldendeSide is null ? Enumerable.Empty<TestLedd>() : Innhold!.AlleLedd.Where(l => l.TestSideId == GjeldendeSide.Id);

    public async Task<IActionResult> OnGetAsync(long id, int? side, CancellationToken cancellationToken)
    {
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null || innhold.Tildeling.PasientId != HentPasientId() || innhold.Test.FyllesUtAvBehandler)
        {
            return NotFound();
        }

        Innhold = innhold;

        // En allerede fullført tildeling skal ALDRI sendes til betaling — sjekkes FØR
        // betalingsgaten under, uansett hva betalingsraden måtte si (2026-09-15: en
        // gammel lenke til en allerede besvart/betalt test kunne i teorien sende
        // pasienten til Vipps på nytt hvis betalingsraden av en eller annen grunn ikke
        // reflekterte fullføringen — denne rekkefølgen gjør det umulig uansett årsak).
        if (innhold.Tildeling.Status == TestTildelingStatus.Fullfort)
        {
            ErFullfort = true;
            await LastNesteIkkeFullforteAsync(id, cancellationToken);
            var skaaring = await _testService.BeregnSkaaringAsync(id, cancellationToken);
            GyldighetsAdvarsel = skaaring?.GyldighetsAdvarsel;
            return Page();
        }

        // Betaling må være bekreftet (eller ikke påkrevd) før utfylling kan starte, jf.
        // kravdokumentet — se docs/beslutningslogg.md "Partner System + Test Monetization".
        var betaling = await _testService.HentBetalingAsync(id, cancellationToken);
        if (betaling is { Status: BetalingStatus.Venter })
        {
            return RedirectToPage("Betal", new { id });
        }

        GjeldendeSideNummer = innhold.Sider.Count == 0 ? 1 : Math.Clamp(side ?? 1, 1, innhold.Sider.Count);
        if (GjeldendeSideNummer == innhold.Sider.Count)
        {
            // På siste side, FØR innsending: hent hva "neste steg" faktisk blir, slik at
            // selve fullfør-knappen kan navngi det direkte i stedet for et generisk "Ferdig".
            await LastNesteIkkeFullforteAsync(id, cancellationToken);
        }
        return Page();
    }

    private async Task LastNesteIkkeFullforteAsync(long gjeldendeTildelingId, CancellationToken cancellationToken)
    {
        var alle = await _testService.HentPasientSynligeTildelingerAsync(HentPasientId(), cancellationToken);
        NesteIkkeFullforteTildelingId = alle
            .Where(t => t.Id != gjeldendeTildelingId && t.Status != TestTildelingStatus.Fullfort)
            .OrderBy(t => t.TildeltUtc)
            .Select(t => (long?)t.Id)
            .FirstOrDefault();

        if (NesteIkkeFullforteTildelingId is not null)
        {
            NesteIkkeFullforteTestNavn = await _testService.HentTestNavnForTildelingAsync(NesteIkkeFullforteTildelingId.Value, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostAsync(long id, int? side, CancellationToken cancellationToken)
    {
        var innhold = await _testService.HentTildelingMedInnholdAsync(id, cancellationToken);
        if (innhold is null || innhold.Tildeling.PasientId != HentPasientId() || innhold.Test.FyllesUtAvBehandler)
        {
            return NotFound();
        }

        // Samme rekkefølge som OnGetAsync: en allerede fullført tildeling skal aldri
        // kunne sendes til betaling eller få flere svar lagret via en gjenbrukt/stale POST.
        if (innhold.Tildeling.Status == TestTildelingStatus.Fullfort)
        {
            return RedirectToPage(new { id });
        }

        // Gates ved bruk, ikke bare på OnGetAsync — en rå POST kan ellers hoppe over
        // betalingssjekken, se CLAUDE.md sine kjente fallgruver.
        var betaling = await _testService.HentBetalingAsync(id, cancellationToken);
        if (betaling is { Status: BetalingStatus.Venter })
        {
            return RedirectToPage("Betal", new { id });
        }

        Innhold = innhold;
        GjeldendeSideNummer = innhold.Sider.Count == 0 ? 1 : Math.Clamp(side ?? 1, 1, innhold.Sider.Count);

        var gjeldendeSide = GjeldendeSide;
        if (gjeldendeSide is null)
        {
            return Page();
        }

        var svar = new Dictionary<long, string>();
        foreach (var ledd in LeddPaaGjeldendeSide)
        {
            var verdi = Request.Form[$"Svar_{ledd.Id}"].ToString();
            if (!string.IsNullOrWhiteSpace(verdi))
            {
                svar[ledd.Id] = verdi;
            }
        }

        var erSisteSide = GjeldendeSideNummer == innhold.Sider.Count;
        var markerFullfort = Handling is "Ferdig" or "FerdigNeste" or "FerdigHjem" && erSisteSide;

        await _testService.LagreSvarAsync(id, svar, markerFullfort, cancellationToken);

        if (markerFullfort)
        {
            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "FullforTest",
                nameof(TestTildeling), id.ToString(), cancellationToken: cancellationToken);

            // "FerdigNeste"/"FerdigHjem" går RETT til neste steg uten den tidligere
            // "Ferdig!"-mellomsiden (bugliste punkt 1, se docs/beslutningslogg.md) — en
            // eventuell GyldighetsAdvarsel bæres videre via TempData og vises som en
            // engangsbanner på målsiden i stedet. "Ferdig" (ingen neste test å velge mellom)
            // beholder den opprinnelige "Ferdig!"-siden UENDRET — der er den ikke redundant.
            if (Handling is "FerdigNeste" or "FerdigHjem")
            {
                var skaaring = await _testService.BeregnSkaaringAsync(id, cancellationToken);
                if (skaaring?.GyldighetsAdvarsel is not null)
                {
                    TempData["GyldighetsAdvarselForrigeTest"] = skaaring.GyldighetsAdvarsel;
                }

                if (Handling == "FerdigNeste")
                {
                    await LastNesteIkkeFullforteAsync(id, cancellationToken);
                    if (NesteIkkeFullforteTildelingId is not null)
                    {
                        return RedirectToPage(new { id = NesteIkkeFullforteTildelingId.Value });
                    }
                }

                return RedirectToPage("/MinSide", new { area = "Pasientportal" });
            }

            return RedirectToPage(new { id });
        }

        var nesteSideNummer = Handling switch
        {
            "Neste" => GjeldendeSideNummer + 1,
            "Forrige" => GjeldendeSideNummer - 1,
            _ => GjeldendeSideNummer
        };
        nesteSideNummer = Math.Clamp(nesteSideNummer, 1, innhold.Sider.Count);

        return RedirectToPage(new { id, side = nesteSideNummer });
    }

    private long HentPasientId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var id) ? id : 0;
}
