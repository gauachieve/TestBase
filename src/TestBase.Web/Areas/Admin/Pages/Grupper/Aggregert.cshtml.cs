using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Pasienter;

namespace TestBase.Web.Areas.Admin.Pages.Grupper;

/// <summary>Admin sin variant av Behandlerportal/Grupper/Aggregert — samme funksjonalitet, uten eierskapssjekk, se GruppeService.HentAggregatAsync.</summary>
public sealed class AggregertModel : PageModel
{
    private readonly GruppeService _grupper;

    public AggregertModel(GruppeService grupper)
    {
        _grupper = grupper;
    }

    public long Id { get; private set; }
    public string? GruppeNavn { get; private set; }
    public bool Provedata { get; private set; }
    public DateOnly Fra { get; private set; }
    public DateOnly Til { get; private set; }
    public IReadOnlyList<TestAggregatRad> Rader { get; private set; } = Array.Empty<TestAggregatRad>();
    public string? Feilmelding { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, bool provedata = true, DateOnly? fra = null, DateOnly? til = null, CancellationToken cancellationToken = default)
    {
        var innhold = await _grupper.HentMedTesterAsync(id, cancellationToken);
        if (innhold is null)
        {
            return RedirectToPage("Index");
        }

        Id = id;
        GruppeNavn = innhold.Gruppe.Navn;
        Provedata = provedata;
        Til = til ?? DateOnly.FromDateTime(DateTime.UtcNow);
        Fra = fra ?? Til.AddDays(-30);

        if (innhold.Tester.Count == 0)
        {
            Feilmelding = "Ingen tester er tilordnet denne gruppen ennå.";
            return Page();
        }

        var fraUtc = provedata ? null : (DateTimeOffset?)new DateTimeOffset(Fra.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var tilUtc = provedata ? null : (DateTimeOffset?)new DateTimeOffset(Til.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        Rader = await _grupper.HentAggregatAsync(id, provedata, fraUtc, tilUtc, cancellationToken);
        return Page();
    }
}
