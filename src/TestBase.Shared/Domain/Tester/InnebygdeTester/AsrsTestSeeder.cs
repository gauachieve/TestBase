namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// ASRS v1.1 Symptomsjekkliste (Adult ADHD Self-Report Scale), utviklet av
/// WHO/Kessler et al. (2005) i samarbeid med NIMH/NCS-R — offentlig
/// tilgjengelig screeningverktøy for voksen-ADHD, fritt til klinisk bruk.
/// 18 ledd totalt: Del A (ledd 1-6) er selve den validerte 6-ledds
/// screeneren, Del B (ledd 7-18) gir supplerende klinisk informasjon men
/// teller IKKE med i screener-skåren (samme mønster som PHQ-9s
/// funksjonsspørsmål, se Phq9TestSeeder). Norsk oversettelse her er egen,
/// IKKE hentet fra en spesifikk sitert norsk kilde ennå — bør kvalitetssikres
/// mot en offisiell norsk oversettelse (f.eks. Helsedirektoratet/ADHD Norge)
/// før reell klinisk bruk. Se docs/beslutningslogg.md.
/// </summary>
public sealed class AsrsTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "asrs";

    private const string Skala = "0:Aldri,1:Sjelden,2:Noen ganger,3:Ofte,4:Svært ofte";

    private static readonly string[] DelA =
    {
        "1. Hvor ofte har du problemer med å fullføre de siste detaljene i et prosjekt, når de vanskelige delene allerede er gjort?",
        "2. Hvor ofte har du problemer med å få ting i orden når du skal utføre en oppgave som krever organisering?",
        "3. Hvor ofte har du problemer med å huske avtaler eller forpliktelser?",
        "4. Når du har en oppgave som krever mye tankearbeid, hvor ofte unngår eller utsetter du å begynne på den?",
        "5. Hvor ofte sitter du og fikler med hendene eller føttene dine, eller vrir deg i stolen, når du må sitte lenge?",
        "6. Hvor ofte føler du deg overdrevent aktiv og tvunget til å gjøre ting, som om du ble drevet av en motor?"
    };

    private static readonly string[] DelB =
    {
        "7. Hvor ofte gjør du uforsiktige feil når du jobber med et kjedelig eller vanskelig prosjekt?",
        "8. Hvor ofte har du vansker med å holde oppmerksomheten når du gjør kjedelig eller repeterende arbeid?",
        "9. Hvor ofte har du vansker med å konsentrere deg om hva folk sier til deg, selv når de snakker direkte til deg?",
        "10. Hvor ofte forlegger eller har problemer med å finne ting hjemme eller på jobb?",
        "11. Hvor ofte blir du distrahert av aktivitet eller støy rundt deg?",
        "12. Hvor ofte forlater du setet ditt i møter eller andre situasjoner der du forventes å sitte i ro?",
        "13. Hvor ofte føler du deg rastløs eller urolig?",
        "14. Hvor ofte har du vansker med å slappe av og ta det med ro når du har tid for deg selv?",
        "15. Hvor ofte oppdager du at du snakker for mye i sosiale sammenhenger?",
        "16. Når du er i en samtale, hvor ofte avbryter du eller fullfører setninger for personer du snakker med, før de selv rekker det?",
        "17. Hvor ofte har du vansker med å vente på tur i situasjoner der det kreves?",
        "18. Hvor ofte avbryter du andre når de er opptatt med noe (f.eks. samtaler, spill)?"
    };

    private const string Kategori = "ADHD, autisme og nevroutvikling";

    private const string RapportIntroduksjonTekst =
        "ASRS v1.1 Symptomsjekkliste (WHO/Kessler et al.) er et screeningverktøy for voksen-ADHD. Del A " +
        "(spørsmål 1-6) er den validerte screeneren; Del B (spørsmål 7-18) gir supplerende klinisk " +
        "informasjon, men teller ikke med i screener-skåren.";

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
            navn: "ASRS Symptomsjekkliste (voksen-ADHD)",
            beskrivelse: "Kryss av for hvor ofte hver av de følgende situasjonene har vært aktuelle for deg de siste 6 månedene.",
            belonningstekst: "Takk for at du fylte ut ASRS-symptomsjekklisten. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var sideA = await testService.LeggTilSideAsync(test.Id, "Del A", "De 6 første spørsmålene er den validerte screeneren.", cancellationToken);
        foreach (var sporsmal in DelA)
        {
            await testService.LeggTilLeddAsync(sideA.Id, sporsmal, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var sideB = await testService.LeggTilSideAsync(test.Id, "Del B", "Disse spørsmålene teller ikke med i screener-skåren, men gir behandleren mer informasjon.", cancellationToken);
        foreach (var sporsmal in DelB)
        {
            await testService.LeggTilLeddAsync(sideB.Id, sporsmal, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
