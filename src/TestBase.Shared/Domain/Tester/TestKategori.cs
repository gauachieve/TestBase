namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Kategori for gruppering av tester i tildelingsflytens tre-visning (jf.
/// beslutningsloggen "Tildelingsflyt for tester"). Faste kategorier seedes av
/// <see cref="TestTildelingsService.SikreStandardkategorierAsync"/> — ingen
/// admin-UI for å opprette/slette kategorier ennå (bevisst utsatt, se
/// beslutningsloggen). En test kan tilhøre flere kategorier via
/// <see cref="TestKategoriKobling"/>.
/// </summary>
public sealed class TestKategori
{
    public long Id { get; set; }
    public required string Navn { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
}
