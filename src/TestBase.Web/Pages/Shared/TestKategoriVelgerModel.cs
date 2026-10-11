using TestBase.Shared.Domain.Tester;

namespace TestBase.Web.Pages.Shared;

/// <summary>Modellen bak _TestKategoriVelger.cshtml — se partial-filens egen kommentar.</summary>
public sealed class TestKategoriVelgerModel
{
    public required IReadOnlyList<TestService.KategoriMedTester> KategoriTre { get; init; }

    /// <summary>Unik per instans på samme side (f.eks. "drop") — forhindrer DOM-id-kollisjon hvis komponenten noensinne brukes flere steder på én side.</summary>
    public string IdPrefix { get; init; } = string.Empty;

    /// <summary>Null = ingen name-attributt (kalleren leser avkrysningene selv via JS, se Programmer/Rediger). Satt = native skjemainnsending (se MinPartner/AutomatiskUtsendelse, FØRSTE faktiske bruker av denne verdien — avdekket en reell Razor-fallgruve der attributtet manglet Html.Raw og derfor ble HTML-enkodet bort, se CLAUDE.md).</summary>
    public string? CheckboxNavn { get; init; }

    public IReadOnlySet<long> ForhaandsvalgteTestIder { get; init; } = new HashSet<long>();
}
