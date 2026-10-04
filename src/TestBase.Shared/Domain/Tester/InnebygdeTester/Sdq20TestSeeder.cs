namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// SDQ-20 (Somatoform Dissociation Questionnaire-20), utviklet av Nijenhuis,
/// Spinhoven, Van Dyck, Van der Hart &amp; Vanderlinden (1996) — kartlegger
/// somatoforme (kroppslige) dissosiative symptomer. Norsk oversettelse her er
/// egen, IKKE hentet fra en spesifikk sitert norsk kilde ennå — bør
/// kvalitetssikres før reell klinisk bruk. Se docs/beslutningslogg.md.
/// </summary>
public sealed class Sdq20TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "sdq20";

    private const string Skala = "1:Gjelder ikke for meg,2:I liten grad,3:I noen grad,4:I stor grad,5:Gjelder helt for meg";

    private static readonly string[] Sporsmal =
    {
        "1. Det hender at jeg ikke kjenner smerte, selv ved en skade som normalt ville gjort vondt",
        "2. Det hender at kroppen min, eller en del av den, føles nummen",
        "3. Det hender at jeg ikke merker temperaturforskjeller (varmt/kaldt) som jeg egentlig burde kjenne",
        "4. Det hender at jeg (delvis) ikke kan se, eller at synsfeltet mitt er innsnevret",
        "5. Det hender at jeg (delvis) ikke kan høre",
        "6. Det hender at ting rundt meg plutselig ser mindre eller lenger unna ut enn de faktisk er",
        "7. Det hender at jeg reagerer på smertefulle situasjoner uten selv å kjenne smerte",
        "8. Det hender at jeg mister stemmen, eller at stemmen min endrer seg helt",
        "9. Det hender at jeg ikke kan snakke, eller bare med stort besvær",
        "10. Det hender at kroppen min, eller en del av den, føles stiv eller anspent",
        "11. Det hender at kroppen min, eller en del av den, føles svak",
        "12. Det hender at kroppen min beveger seg på en måte jeg ikke selv kontrollerer",
        "13. Det hender at jeg opplever smerte som er sterkere enn den vanligvis ville vært",
        "14. Det hender at kroppslig ubehag hos meg forsvinner uvanlig raskt",
        "15. Det hender jeg kjenner en sterk lukt uten at andre kjenner den",
        "16. Det hender at jeg (delvis) ikke kan lukte",
        "17. Det hender at jeg (delvis) ikke kan smake",
        "18. Det hender at kroppen min, eller en del av den, plutselig oppleves som annerledes (f.eks. større eller mindre enn vanlig)",
        "19. Det hender at vannlating eller avføring skjer uten at jeg merker det",
        "20. Det hender at det gjør vondt når jeg later vannet, uten at det finnes noen kjent medisinsk årsak"
    };

    private const string Kategori = "Traumer, dissosiasjon og belastninger";

    private const string RapportIntroduksjonTekst =
        "SDQ-20 (Somatoform Dissociation Questionnaire, Nijenhuis et al. 1996) kartlegger kroppslige " +
        "(somatoforme) dissosiative symptomer. 20 spørsmål, sumskår 20-100. SDQ-20 er et " +
        "screeningverktøy, ikke tilstrekkelig alene for å stille diagnose.";

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
            navn: "SDQ-20 (Kroppslig dissosiasjon)",
            beskrivelse: "Kryss av i hvilken grad de følgende utsagnene gjelder for deg.",
            belonningstekst: "Takk for at du fylte ut SDQ-20. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Kroppslige opplevelser", null, cancellationToken);
        foreach (var sporsmal in Sporsmal)
        {
            await testService.LeggTilLeddAsync(side.Id, sporsmal, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
