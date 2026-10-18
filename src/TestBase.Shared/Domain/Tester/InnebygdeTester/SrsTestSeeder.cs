namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// SRS (Session Rating Scale) — del av KOR/FIT (se OrsTestSeeder for ORS, som måler
/// tilstand/utfall). SRS måler ALLIANSEN i én konkret time, besvart AV PASIENTEN selv rett
/// ETTER timen (se KorFitOppsettService — sendes normalt automatisk en konfigurerbar tid etter
/// en definert "neste time"). I motsetning til ORS, der kun domenenavnet varierer og
/// VAS-endepunktene er generiske "Lav/Høy", er selve det MOTSATTE ANKERPARET poenget med hvert
/// SRS-ledd — pasienten tar stilling til et konkret utsagn i hver ende av linjen.
///
/// LISENS: se OrsTestSeeder sin klassekommentar — samme lisensstatus (PCOMS) og samme eksplisitte
/// brukerbeslutning om å bygge inn ekte, oversatt innhold nå (2026-10-18).
/// </summary>
public sealed class SrsTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "srs";

    private const string Instruksjon =
        "Tenk tilbake på timen i dag, og sett et merke på hver linje som passer med din erfaring " +
        "av denne timen.";

    private static readonly (string Domene, string Lav, string Hoy)[] Ledd =
    {
        ("Relasjon",
            "Jeg følte meg ikke hørt, forstått og respektert",
            "Jeg følte meg hørt, forstått og respektert"),
        ("Mål og temaer",
            "Vi arbeidet ikke med eller snakket om det jeg ønsket å arbeide med/snakke om",
            "Vi arbeidet med og snakket om det jeg ønsket å arbeide med/snakke om"),
        ("Tilnærming eller metode",
            "Behandlers tilnærming passer ikke for meg",
            "Behandlers tilnærming passer godt for meg"),
        ("Helhetsinntrykk",
            "Det var noe som manglet i timen i dag",
            "Totalt sett var timen i dag riktig for meg")
    };

    private const string Kategori = "Funksjon, livskvalitet og behandlingsutfall";

    private const string RapportIntroduksjonTekst =
        "SRS (Session Rating Scale) er del av KOR/FIT (Klient- og resultatstyrt terapi) og måler " +
        "pasientens opplevelse av alliansen i én konkret time. Besvares normalt rett etter timen, " +
        "gjentatt over tid, og vises sammen med ORS (tilstand/utfall) i en kombinert graf — se " +
        "\"KOR/FIT over tid\" i rapporten når begge er besvart minst én gang.";

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
            navn: "SRS (allianse i timen — KOR/FIT)",
            beskrivelse: "Dra glidebryteren til punktet som passer best for hvert av de fire utsagnsparene " +
                         "nedenfor, basert på din erfaring av timen i dag.",
            belonningstekst: "Takk for at du fylte ut SRS. Svarene dine inngår i oppfølgingen av behandlingen din.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Timen i dag", Instruksjon, cancellationToken);

        foreach (var (domene, lav, hoy) in Ledd)
        {
            await testService.LeggTilLeddAsync(
                side.Id, domene, instruksjon: null, TestSvartype.VisuellAnalogSkala, $"0:{lav},10:{hoy}", cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
