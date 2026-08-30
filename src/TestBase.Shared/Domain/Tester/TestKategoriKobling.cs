namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Kobler én test til én kategori — mange-til-mange, jf. samme mønster med
/// eksplisitt koblingsentitet (og rene long-FK-er, ingen navigasjonsegenskaper)
/// som resten av modellen. En test kan ha flere slike koblinger (én per
/// kategori den vises i).
/// </summary>
public sealed class TestKategoriKobling
{
    public long Id { get; set; }
    public long TestId { get; set; }
    public long TestKategoriId { get; set; }
}
