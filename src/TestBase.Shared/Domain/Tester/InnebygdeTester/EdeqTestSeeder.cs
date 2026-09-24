namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// EDE-Q (Eating Disorder Examination Questionnaire), opprinnelig Fairburn &amp;
/// Beglin (1994). Selve originalspørsmålene og -rekkefølgen er OPPHAVSRETTSLIG
/// beskyttet (Fairburn) — denne versjonen er en OMSKREVET/TILPASSET gjengivelse
/// (parafrasert innhold, delskalaene GRUPPERT sammen fremfor den offisielle
/// sammenflettede rekkefølgen) for intern uttesting, IKKE en verbatim
/// gjengivelse av det lisensierte skjemaet. Må erstattes med/kvalitetssikres
/// mot en offisiell, lisensiert norsk oversettelse før reell klinisk bruk. 4
/// delskalaer (Restriksjon, Spisebekymring, Figurbekymring, Vektbekymring,
/// 0-6 hver) + 5 ikke-skårede atferdsspørsmål (fritekst, samme prinsipp som
/// PHQ-9s ekskluderte funksjonsspørsmål). Se docs/beslutningslogg.md.
/// </summary>
public sealed class EdeqTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "edeq";

    private const string FrekvensSkala = "0:Ingen dager,1:1-5 dager,2:6-12 dager,3:13-15 dager,4:16-22 dager,5:23-27 dager,6:Hver dag";
    private const string AlvorlighetsSkala = "0:Ikke i det hele tatt,1:Litt,2:Noe,3:Middels,4:Ganske mye,5:Mye,6:I ekstrem grad";

    private static readonly string[] Restriksjon =
    {
        "1. Har du bevisst prøvd å begrense hvor mye du spiser, for å påvirke figur eller vekt?",
        "2. Har du gått lange perioder (8 timer eller mer i våken tilstand) uten å spise noe, for å påvirke figur eller vekt?",
        "3. Har du prøvd å utelate mat du liker fra kostholdet ditt, for å påvirke figur eller vekt?",
        "4. Har du prøvd å følge klare regler for spisingen din (f.eks. et kalorimål), for å påvirke figur eller vekt?",
        "5. Har du hatt et sterkt ønske om tom mage, for å påvirke figur eller vekt?"
    };

    private static readonly string[] Spisebekymring =
    {
        "6. Har tanker om mat, spising eller kalorier gjort det svært vanskelig å konsentrere deg om ting du er interessert i?",
        "7. Har du hatt en klar frykt for å miste kontrollen over spisingen?",
        "8. Har du spist i hemmelighet (i skjul for andre)?",
        "9. Har du hatt en klar frykt for å gå opp i vekt?",
        "10. Har du følt deg skyldig etter å ha spist, i den forstand at du følte du hadde gjort noe galt?"
    };

    private static readonly string[] Figurbekymring =
    {
        "11. Har flat mage vært viktig for hvordan du tenker om deg selv som person?",
        "12. Har du vært redd for å gå opp i vekt?",
        "13. Har du følt deg fet?",
        "14. Har du hatt et sterkt ønske om å gå ned i vekt?",
        "15. Hvor misfornøyd har du vært med figuren din?",
        "16. Hvor bekymret har du vært for at andre skal se figuren din og oppfatte den negativt?",
        "17. Hvor mye har vekten din påvirket hvordan du vurderer deg selv som person?",
        "18. Hvor mye har figuren din påvirket hvordan du vurderer deg selv som person?"
    };

    private static readonly string[] Vektbekymring =
    {
        "19. Hvor misfornøyd har du vært med vekten din?",
        "20. Har du unngått situasjoner der andre kunne se kroppen eller figuren din (f.eks. svømmehall, garderobe)?",
        "21. Har du unngått å ha på deg stram-sittende klær som viser kroppsformen din?",
        "22. Har opplevelsen av å veie deg påvirket humøret ditt?",
        "23. Har du følt skam over kroppen din?"
    };

    private static readonly string[] AtferdssporsmalIkkeSkaaret =
    {
        "24. I løpet av de siste 28 dagene: hvor mange ganger har du spist det du selv vil kalle en uvanlig stor mengde mat, samtidig som du følte at du mistet kontrollen? (skriv et tall, teller ikke med i skåren)",
        "25. Hvor mange ganger har du fremkalt oppkast for å kontrollere figur eller vekt? (skriv et tall, teller ikke med i skåren)",
        "26. Hvor mange ganger har du brukt avføringsmidler for å kontrollere figur eller vekt? (skriv et tall, teller ikke med i skåren)",
        "27. Hvor mange ganger har du trent hardt eller overdrevent for å kontrollere figur eller vekt? (skriv et tall, teller ikke med i skåren)",
        "28. Har du opplevd fravær av menstruasjon de siste 3-4 månedene, hvis relevant? (fritekst, teller ikke med i skåren)"
    };

    private const string Kategori = "Spiseforstyrrelser og kroppsbilde";

    private const string RapportIntroduksjonTekst =
        "EDE-Q (Eating Disorder Examination Questionnaire, tilpasset/omskrevet versjon — se " +
        "seeder-kommentar for opphavsrettslig forbehold) kartlegger spiseforstyrrelsessymptomer " +
        "siste 28 dager, med 4 delskalaer (Restriksjon, Spisebekymring, Figurbekymring, " +
        "Vektbekymring) og et globalskår som gjennomsnittet av disse.";

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
            navn: "EDE-Q (Spisevaner)",
            beskrivelse: "Følgende spørsmål gjelder de siste 28 dagene. Les hvert spørsmål nøye og kryss av det svaret som passer best.",
            belonningstekst: "Takk for at du fylte ut EDE-Q. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side1 = await testService.LeggTilSideAsync(test.Id, "Restriksjon og spisebekymring", null, cancellationToken);
        foreach (var s in Restriksjon)
        {
            await testService.LeggTilLeddAsync(side1.Id, s, null, TestSvartype.LikertSkala, FrekvensSkala, cancellationToken);
        }
        foreach (var s in Spisebekymring)
        {
            await testService.LeggTilLeddAsync(side1.Id, s, null, TestSvartype.LikertSkala, FrekvensSkala, cancellationToken);
        }

        var side2 = await testService.LeggTilSideAsync(test.Id, "Figur- og vektbekymring", null, cancellationToken);
        foreach (var s in Figurbekymring)
        {
            await testService.LeggTilLeddAsync(side2.Id, s, null, TestSvartype.LikertSkala, AlvorlighetsSkala, cancellationToken);
        }
        foreach (var s in Vektbekymring)
        {
            await testService.LeggTilLeddAsync(side2.Id, s, null, TestSvartype.LikertSkala, AlvorlighetsSkala, cancellationToken);
        }

        var side3 = await testService.LeggTilSideAsync(test.Id, "Atferd (teller ikke med i skåren)", null, cancellationToken);
        foreach (var s in AtferdssporsmalIkkeSkaaret)
        {
            await testService.LeggTilLeddAsync(side3.Id, s, null, TestSvartype.Fritekst, null, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
