using TestBase.Shared.Domain.Tester;

namespace TestBase.Web.Pages.Shared;

/// <summary>
/// Modellen bak _IkkeFullforteFane.cshtml — "Tildelte tester"-fanens innhold, delt mellom BÅDE
/// Tildel/Pasienter (steg 1) og Tildel/Tester (steg 2) i begge Areas. Lagt til på steg 1 i en
/// oppfølgingsrunde (2026-10-11) etter at brukeren rapporterte å ikke finne fanen i det hele tatt
/// — den fantes KUN på steg 2, usynlig for enhver som bruker navigasjonslenken "Tildel tester"
/// (som peker til steg 1) uten å først velge pasienter og trykke videre, se docs/beslutningslogg.md.
/// </summary>
public sealed class IkkeFullforteFaneModel
{
    public required IReadOnlyList<TestService.TildelingMedTestOgPasient> Rader { get; init; }
    public string? PaaminnelseMelding { get; init; }
    public required string Beskrivelse { get; init; }
}
