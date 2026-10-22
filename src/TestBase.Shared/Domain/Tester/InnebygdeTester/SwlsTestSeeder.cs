namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// SWLS (Satisfaction With Life Scale, Diener, Emmons, Larsen &amp; Griffin, 1985) — måler GENERELL
/// livstilfredshet (et kognitivt velvære-mål), ikke symptomer på noen tilstand. Valgt 2026-10-22
/// som erstatning for ORS (se tidligere KOR/FIT-innføring) nettopp fordi SWLS aldri er validert
/// eller publisert som et diagnostisk/screening-instrument for noen sykdom — se
/// docs/beslutningslogg.md "Fjerning av diagnostiske tester / MDR-opprydding" for hele
/// resonnementet. Forfatterens offisielle side (labs.psychology.illinois.edu/~ediener/SWLS.html)
/// erklærer skalaen fri til bruk av klinikere/praktikere mot kildehenvisning — INGEN kommersiell
/// lisens kreves, i motsetning til ORS/SRS (PCOMS). Ordlyden under er en egen, norsk oversettelse
/// av den kjent publiserte 5-leddsstrukturen, ikke en ordrett kopi av en bestemt oversatt utgave.
///
/// Hører sammen med SAI (se SaiTestSeeder) i "Tilbakemelding til neste time" — SWLS besvares FØR
/// timen (generell livstilfredshet), SAI ETTER (alliansen i den konkrete timen), begge vist i én
/// kombinert graf (se Skaaring/KorFitGrafBeregner).
/// </summary>
public sealed class SwlsTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "swls";

    private const string Instruksjon =
        "Nedenfor finner du fem utsagn som du kan være enig eller uenig i. Bruk 1–7-skalaen " +
        "nedenfor til å vise hvor enig eller uenig du er i hvert utsagn, slik det passer akkurat " +
        "nå for deg — ikke tenk på noen enkelt time eller hendelse.";

    private const string Skala =
        "1:Sterkt uenig,2:Uenig,3:Litt uenig,4:Verken enig eller uenig,5:Litt enig,6:Enig,7:Sterkt enig";

    private static readonly string[] Utsagn =
    {
        "På de fleste måter er livet mitt nær mitt ideal",
        "Livsforholdene mine er utmerkede",
        "Jeg er fornøyd med livet mitt",
        "Så langt har jeg fått de viktige tingene jeg ønsker meg i livet",
        "Hvis jeg kunne leve livet om igjen, ville jeg nesten ikke endret noe"
    };

    private const string Kategori = "Funksjon, livskvalitet og behandlingsutfall";

    private const string RapportIntroduksjonTekst =
        "SWLS (Satisfaction With Life Scale, Diener m.fl. 1985) måler generell livstilfredshet — " +
        "IKKE symptomer på noen tilstand, og er ikke ment som et diagnostisk verktøy. Besvares " +
        "normalt rett før en time, gjentatt over tid, og vises sammen med SAI (alliansemål) i en " +
        "kombinert graf — se \"Tilbakemelding over tid\" i rapporten når begge er besvart minst én gang.";

    public async Task SeedAsync(TestService testService, CancellationToken cancellationToken = default)
    {
        await testService.SikreStandardkategorierAsync(cancellationToken);

        var eksisterende = await testService.HentTestVedKodeAsync(Kode, cancellationToken);
        if (eksisterende is not null)
        {
            await testService.KoblTestTilKategoriAsync(eksisterende.Id, Kategori, cancellationToken);
            await testService.SettRapportIntroduksjonAsync(eksisterende.Id, RapportIntroduksjonTekst, cancellationToken);
            return;
        }

        var test = await testService.OpprettTestAsync(
            navn: "SWLS (livstilfredshet)",
            beskrivelse: "Les hvert utsagn og velg tallet som best beskriver hvor enig eller uenig du er.",
            belonningstekst: "Takk for at du fylte ut SWLS. Svarene dine inngår i oppfølgingen av behandlingen din.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Livstilfredshet", Instruksjon, cancellationToken);

        foreach (var utsagn in Utsagn)
        {
            await testService.LeggTilLeddAsync(
                side.Id, utsagn, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
