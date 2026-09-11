namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// WURS (Wender Utah Rating Scale for ADHD hos voksne) — norsk oversettelse
/// ved Knut Hallvard Bronder, Anne Lill Ørbeck og Gerd Strand, distribusjon
/// gitt av dr. Wender til Nasjonalt Kompetansesenter for AD/HD, Tourettes
/// syndrom og Narkolepsi (NK), Oslo universitetssykehus. Kilde:
/// https://www.helsebiblioteket.no/liste/_/attachment/download/524de0f1-5de2-42c7-a650-285d2c519d36:1553efd547e793e2c27cc83b27ae7dd99b48e0ce/wurs-skjema-2021.pdf
/// (hentet og lest i sin helhet 2026-09-12, se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\WURS.md). Alle 61
/// ledd fra det offisielle skjemaet digitaliseres — skåringen (se
/// WursSkaaringsberegner) bruker likevel kun de 25 leddene som utgjør den
/// klinisk validerte WURS-25-korttesten, siden selve manualen for hvilke 25
/// og cutoff kun deles ut av NK/OUS på forespørsel til fagfolk. IKKE
/// juridisk/klinisk kvalitetssikret av oss utover dette, se
/// docs/beslutningslogg.md.
/// </summary>
public sealed class WursTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "wurs";

    private const string Stem = "Som barn var jeg/hadde jeg:";

    private const string Skala =
        "0:Ikke i det hele tatt, eller bare litt,1:Av og til,2:En del,3:Nokså mye,4:Veldig mye";

    private static readonly string[] SomBarn =
    {
        "Aktiv, rastløs, alltid på farten",
        "Redd for spesielle ting",
        "Konsentrasjonsproblemer, lett å distrahere",
        "Engstelig, bekymret",
        "Nervøs, fiklete",
        "Uoppmerksom, dagdrømmende",
        "Hissig temperament, «kort lunte»",
        "Sjenert, sårbar",
        "Sinneutbrudd, raserianfall",
        "Problemer med å holde meg til en aktivitet, fullførte ikke det jeg hadde begynt på",
        "Sta, sterk vilje",
        "Trist, nedstemt, ulykkelig",
        "Uforsiktig, dumdristig, var ofte med på tull",
        "Sjelden begeistret, misfornøyd med livet",
        "Ulydig mot foreldrene, uforskammet, frekk",
        "Dårlig selvbilde",
        "Irritabel",
        "Utadvendt, vennlig, likte å være sammen med andre",
        "Slurvete, uorganisert",
        "Humørsyk, hadde humørsvingninger",
        "Sint",
        "Mange venner, var populær",
        "Velorganisert, ryddig, ordentlig",
        "Handlet uten å tenke, var impulsiv",
        "Tendens til å være umoden",
        "Plaget av skyldfølelse og anger",
        "Lett for å miste kontrollen over meg selv",
        "Tendens til å være eller oppføre meg irrasjonelt",
        "Upopulær blant andre barn, beholdt venner bare for en kort stund, kom dårlig overens med andre barn",
        "Dårlig koordinasjon, deltok ikke i sport",
        "Redd for å miste kontrollen over meg selv",
        "Bra koordinasjon, ble valgt ut først i leker",
        "Gutteaktig (gjelder bare jenter)",
        "Rømte hjemmefra",
        "Havnet lett i slåsskamp",
        "Lett for å erte andre barn",
        "Lederskikkelse, sjefete",
        "Vansker med å våkne om morgenen",
        "Medløper, lett å lede",
        "Vansker med å se ting fra andres synsvinkel",
        "Trøbbel med autoriteter, problemer på skolen, ble sendt til rektor",
        "Trøbbel med politiet, arrestert, straffet"
    };

    private static readonly string[] MedisinskeProblemer =
    {
        "Hodepine",
        "Mavesmerter",
        "Forstoppelse",
        "Diare",
        "Matallergier",
        "Andre former for allergier",
        "Sengevæting"
    };

    private static readonly string[] SomSkolebarn =
    {
        "Stort sett flink elev, lærte raskt",
        "Gjennomgående svak elev, lærte sent",
        "Sen til å lære å lese",
        "Langsom leser",
        "Skrev speilvendte bokstaver",
        "Problemer med å stave",
        "Problemer med matematikk eller tall",
        "Dårlig håndskrift",
        "I stand til å lese bra, men likte egentlig aldri å lese",
        "Ikke i stand til å utnytte evnene mine",
        "Gikk en klasse om igjen",
        "Utvist for kortere eller lengre tid"
    };

    private const string Kategori = "ADHD, autisme og nevroutvikling";

    private const string RapportIntroduksjonTekst =
        "WURS er et retrospektivt selvutfyllingsskjema hvor voksne vurderer egen barndomsatferd — 61 spørsmål " +
        "skåret 0–4. Skåringen her bruker den klinisk validerte WURS-25-korttesten (delmengde av de 61).";

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
            navn: "WURS (Wender Utah Rating Scale for ADHD hos voksne)",
            beskrivelse: Stem,
            belonningstekst: "Takk for at du fylte ut WURS. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side1 = await testService.LeggTilSideAsync(test.Id, "Som barn var jeg (eller hadde jeg)", Stem, cancellationToken);
        foreach (var tekst in SomBarn)
        {
            await testService.LeggTilLeddAsync(side1.Id, tekst, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var side2 = await testService.LeggTilSideAsync(test.Id, "Medisinske problemer som barn", instruksjon: null, cancellationToken);
        foreach (var tekst in MedisinskeProblemer)
        {
            await testService.LeggTilLeddAsync(side2.Id, tekst, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var side3 = await testService.LeggTilSideAsync(test.Id, "Som skolebarn var jeg (eller hadde jeg)", instruksjon: null, cancellationToken);
        foreach (var tekst in SomSkolebarn)
        {
            await testService.LeggTilLeddAsync(side3.Id, tekst, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }

    /// <summary>
    /// 0-baserte posisjoner (over alle 61 ledd, i rekkefølgen sidene legges til
    /// over) som utgjør WURS-25 — identifisert ved innholdsmatching mot
    /// Ward/Wender/Reimherr (1993) sin offisielle 25-item-liste, se
    /// WursSkaaringsberegner og forskningsfilen.
    /// </summary>
    public static readonly IReadOnlyList<int> Wurs25Posisjoner = new[]
    {
        2, 3, 4, 5, 6, 8, 9, 10, 11, 14, 15, 16, 19, 20, 23, 24, 25, 26, 27, 28, 39, 40, 50, 55, 58
    };
}
