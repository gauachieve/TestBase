namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// PHQ-9 (Patient Health Questionnaire-9) — norsk oversettelse ved Sverre Urnes
/// Johnson, Asle Hoffart, Pål Ulvenes, Harold Sexton og Bruce E. Wampold,
/// distribuert av Norsk Forening for Kognitiv Terapi. Kilde:
/// https://www.kognitiv.no/wp-content/uploads/2025/05/PHQ-9_elektronisk.pdf
/// (hentet og lest i sin helhet 2026-09-12, se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\PHQ9.md).
/// IKKE juridisk/klinisk kvalitetssikret av oss utover dette — samme forbehold
/// som WHO-5, se docs/beslutningslogg.md.
/// </summary>
public sealed class Phq9TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "phq9";

    private const string Stem = "Hvor ofte har du vært plaget av ett eller flere av de følgende problemene i løpet av de siste to ukene.";

    private const string Skala = "0:Ikke i det hele tatt,1:Noen dager,2:Mer enn halvparten av dagene,3:Nesten hver dag";

    private static readonly string[] Sporsmal =
    {
        "1. Liten interesse for eller glede av å gjøre ting",
        "2. Følt deg nedfor, deprimert eller fylt av håpløshet",
        "3. Vansker med å sovne, sove uten avbrudd eller sovet for mye",
        "4. Følt deg trett eller energiløs",
        "5. Dårlig matlyst eller å spise for mye",
        "6. Vært misfornøyd med deg selv eller følt deg mislykket, eller følt at du har sviktet deg selv eller familien din",
        "7. Vansker med å konsentrere deg om ting, slik som å lese avisen eller se på tv",
        "8. Beveget deg eller snakket så langsomt at andre kan ha merket det? Eller motsatt – følt deg så urolig eller rastløs at du har beveget deg mye mer enn vanlig",
        "9. Tanker om at det ville vært bedre om du var død eller om å skade deg selv"
    };

    private const string FunksjonsSporsmal =
        "Hvis du har opplevd ett eller flere av de problemene som nevnes, i hvor stor grad har problemene gjort det " +
        "vanskelig for deg å utføre arbeidet ditt, ordne med ting hjemme eller å komme overens med andre? (teller ikke med i skåren)";

    private const string FunksjonsSkala =
        "0:Ikke vanskelig i det hele tatt,1:Litt vanskelig,2:Svært vanskelig,3:Ekstremt vanskelig";

    private const string Kategori = "Depresjon og bipolaritet";

    private const string RapportIntroduksjonTekst =
        "PHQ-9 (Patient Health Questionnaire-9) er et kartleggingsverktøy for depresjon — ikke tilstrekkelig " +
        "alene for å stille diagnose. Ni spørsmål om de siste to ukene, hvert skåret 0–3, samt ett " +
        "funksjonsspørsmål som ikke inngår i sumskåren.";

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
            navn: "PHQ-9 (Patient Health Questionnaire-9)",
            beskrivelse: Stem,
            belonningstekst: "Takk for at du fylte ut PHQ-9. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Depresjonskartlegging", Stem, cancellationToken);

        foreach (var sporsmalstekst in Sporsmal)
        {
            await testService.LeggTilLeddAsync(
                side.Id, sporsmalstekst, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.LeggTilLeddAsync(
            side.Id, FunksjonsSporsmal, instruksjon: null, TestSvartype.LikertSkala, FunksjonsSkala, cancellationToken);

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
