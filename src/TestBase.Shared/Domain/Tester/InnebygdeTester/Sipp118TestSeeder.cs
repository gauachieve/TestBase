namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// SIPP-118 (Severity Indices of Personality Problems), opprinnelig Verheul,
/// Andrea, Berghout et al. (2008) — måler personlighetsfunksjon på tvers av
/// 5 overordnede domener (Selvkontroll, Identitetsintegrasjon, Relasjonell
/// kapasitet, Ansvarlighet, Sosial harmoni), som selve validerte instrumentet
/// deler videre inn i 16 spesifikke fasetter over 118 ledd.
///
/// VIKTIG BEGRENSNING: dette er en KRAFTIG REDUSERT, EGEN TILPASNING (30 ledd,
/// 6 per domene) BYGGET RUNDT DE 5 KJENTE OVERORDNEDE DOMENENE — IKKE en
/// gjengivelse av de 16 offisielle fasettene eller de 118 offisielle leddene,
/// som vi ikke har hatt tilstrekkelig sikker kildetilgang til å gjengi
/// korrekt under denne økten. Skal IKKE forveksles med det validerte,
/// lisensierte SIPP-118-instrumentet i klinisk bruk — egner seg kun til
/// uttesting av systemet, og bør enten erstattes med det faktiske 118-ledds
/// instrumentet (krever egen tilgang/lisensavklaring) eller tydelig
/// omdøpes/reforankres før noe forsøk på reell klinisk bruk. Se
/// docs/beslutningslogg.md.
/// </summary>
public sealed class Sipp118TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "sipp118";

    private const string Skala = "1:Sterkt uenig,2:Litt uenig,3:Litt enig,4:Sterkt enig";

    private static readonly string[] Selvkontroll =
    {
        "1. Jeg blir fort overveldet av sterke følelser.",
        "2. Jeg klarer å roe meg selv ned når jeg er opprørt.",
        "3. Jeg har vanskelig for å tåle frustrasjon.",
        "4. Jeg mister kontrollen når jeg blir sint.",
        "5. Jeg handler impulsivt når jeg er følelsesmessig presset.",
        "6. Jeg klarer å utsette en reaksjon til jeg har tenkt meg om."
    };

    private static readonly string[] Identitetsintegrasjon =
    {
        "7. Jeg vet stort sett hvem jeg er som person.",
        "8. Mitt syn på meg selv endrer seg mye fra situasjon til situasjon.",
        "9. Jeg har respekt for meg selv.",
        "10. Jeg forstår mine egne følelser og reaksjoner godt.",
        "11. Jeg føler meg som en helhetlig person, ikke oppstykket.",
        "12. Jeg klarer å reflektere over hvorfor jeg reagerer som jeg gjør."
    };

    private static readonly string[] RelasjonellKapasitet =
    {
        "13. Jeg klarer å være følelsesmessig nær andre mennesker.",
        "14. Jeg trekker meg unna når noen kommer meg nær.",
        "15. Jeg setter pris på tid sammen med andre.",
        "16. Jeg samarbeider godt med andre mennesker.",
        "17. Jeg har minst én person jeg kan stole fullt og helt på.",
        "18. Jeg klarer å sette meg inn i andres perspektiv."
    };

    private static readonly string[] Ansvarlighet =
    {
        "19. Jeg tar ansvar for mine egne handlinger.",
        "20. Jeg fullfører det jeg har satt meg fore.",
        "21. Jeg har klare mål for hva jeg vil med livet mitt.",
        "22. Jeg skylder ofte på andre når noe går galt.",
        "23. Jeg holder avtaler jeg har inngått.",
        "24. Jeg tenker gjennom konsekvensene før jeg handler."
    };

    private static readonly string[] SosialHarmoni =
    {
        "25. Jeg respekterer andre menneskers grenser.",
        "26. Jeg tilpasser meg sosiale normer og regler.",
        "27. Jeg har tillit til at andre mennesker vil meg vel.",
        "28. Jeg blir lett mistenksom overfor andres motiver.",
        "29. Jeg viser omtanke for andre menneskers behov.",
        "30. Jeg kommer ofte i konflikt med mennesker rundt meg."
    };

    private const string Kategori = "Personlighet, relasjoner og sosial fungering";

    private const string RapportIntroduksjonTekst =
        "Denne testen er en KRAFTIG REDUSERT, egen tilpasning inspirert av SIPP-118 sine 5 " +
        "overordnede domener for personlighetsfunksjon (Selvkontroll, Identitetsintegrasjon, " +
        "Relasjonell kapasitet, Ansvarlighet, Sosial harmoni) — IKKE en gjengivelse av det " +
        "validerte, 118-ledds/16-fasetts instrumentet. Egner seg til uttesting, ikke reell klinisk " +
        "diagnostikk. Høyere skår = bedre personlighetsfunksjon (motsatt av de fleste symptommål).";

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
            navn: "Personlighetsfunksjon (forenklet, inspirert av SIPP-118)",
            beskrivelse: "Kryss av i hvilken grad du er enig eller uenig i hvert utsagn om deg selv.",
            belonningstekst: "Takk for at du fylte ut denne testen. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var sideSelvkontroll = await testService.LeggTilSideAsync(test.Id, "Selvkontroll", null, cancellationToken);
        foreach (var s in Selvkontroll)
        {
            await testService.LeggTilLeddAsync(sideSelvkontroll.Id, s, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var sideIdentitet = await testService.LeggTilSideAsync(test.Id, "Identitetsintegrasjon", null, cancellationToken);
        foreach (var s in Identitetsintegrasjon)
        {
            await testService.LeggTilLeddAsync(sideIdentitet.Id, s, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var sideRelasjonell = await testService.LeggTilSideAsync(test.Id, "Relasjonell kapasitet", null, cancellationToken);
        foreach (var s in RelasjonellKapasitet)
        {
            await testService.LeggTilLeddAsync(sideRelasjonell.Id, s, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var sideAnsvar = await testService.LeggTilSideAsync(test.Id, "Ansvarlighet", null, cancellationToken);
        foreach (var s in Ansvarlighet)
        {
            await testService.LeggTilLeddAsync(sideAnsvar.Id, s, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var sideSosial = await testService.LeggTilSideAsync(test.Id, "Sosial harmoni", null, cancellationToken);
        foreach (var s in SosialHarmoni)
        {
            await testService.LeggTilLeddAsync(sideSosial.Id, s, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
