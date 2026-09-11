namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// MADRS-S (Montgomery Åsberg Depression Rating Scale — selvutfylling), norsk
/// oversettelse fra svensk ved professor dr. med. Ulrik Fredrik Malt. Kilde:
/// https://www.helsebiblioteket.no/liste/_/attachment/download/cd2c8aaa-ec16-46ab-a175-59e40dd25b02:fb287f393595427f2bd18804afc0566c3223bde2/madrs-selvutfylling-pasient.pdf
/// + brukerveiledningen (samme forfatter) for skåringsbakgrunn (hentet og lest
/// i sin helhet 2026-09-12, se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\MADRS_S.md). De to
/// ekstraspørsmålene (X1 døgnvariasjon, X2 blandingssymptomer) er IKKE tatt
/// med denne runden — de teller ikke i totalskåren, og kildedokumentet
/// anbefaler dem kun når hovedsummen er over 15, en betinget visningsregel
/// testmotoren ikke støtter i dag. IKKE juridisk/klinisk kvalitetssikret av
/// oss utover dette, se docs/beslutningslogg.md.
/// </summary>
public sealed class MadrsSTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "madrs_s";

    private const string Stem =
        "Hensikten med dette spørreskjemaet er å gi et detaljert bilde av ditt nåværende stemningsleie. Vi vil " +
        "derfor be om at du på skjemaet under besvarer noen spørsmål omkring stemningsleiet de siste tre døgn. " +
        "Sett et kryss ved den påstanden som stemmer best med hvordan du har hatt det de siste 3 dagene. Er du " +
        "i tvil om hvilken påstand som stemmer best, kan du velge et av mellomtrinnene.";

    private const string Mellomtrinn = "(mellomtrinn — velg hvis du er i tvil mellom nabotrinnene)";

    private sealed record MadrsLedd(string Etikett, string Intro, string Verdi0, string Verdi2, string Verdi4, string Verdi6)
    {
        public string Skala =>
            $"0:{Verdi0},1:{Mellomtrinn},2:{Verdi2},3:{Mellomtrinn},4:{Verdi4},5:{Mellomtrinn},6:{Verdi6}";
    }

    private static readonly MadrsLedd[] Ledd =
    {
        new("1. Stemningsleie",
            "Her skal du krysse av for hvordan ditt stemningsleie har vært: Om du har kjent deg trist, tungsindig eller dyster.",
            "Jeg kan kjenne meg trist eller glad, alt etter omstendighetene.",
            "Jeg kjenner meg for det meste nedstemt, men iblant kjennes det lettere.",
            "Jeg kjenner meg gjennomgående nedstemt og dyster. Jeg kan ikke glede meg over slikt som vanligvis gjør meg glad.",
            "Jeg er så totalt nedstemt og ulykkelig at jeg ikke kan tenke meg at det kan bli verre."),
        new("2. Følelse av indre uro / spenning",
            "Her ber vi deg krysse av for i hvilken grad du har hatt følelser av indre spenning, uro, angst, eller udefinerbar redsel.",
            "Jeg kjenner meg for det meste rolig.",
            "Iblant har jeg ubehagelig følelser av indre uro.",
            "Jeg har ofte en følelse av indre uro som iblant kan bli meget sterk og som jeg må anstrenge meg for å holde i sjakk.",
            "Jeg har fryktelige, langvarige eller uutholdelige følelser av angst."),
        new("3. Søvn",
            "Her ber vi deg anføre hvor godt du sover, uansett om du har brukt sovemedisin eller ikke.",
            "Jeg sover rolig og godt, og lenge nok til å dekke mitt behov. Jeg har ingen særlige vanskeligheter med innsovning.",
            "Jeg har visse søvnvanskeligheter. Iblant har jeg vanskelig for å sovne eller jeg sover mer urolig eller overfladisk enn jeg pleier.",
            "Jeg sover minst 2 timer mindre per natt enn normalt. Jeg våkner ofte i løpet av natten selv om jeg ikke forstyrres av noen.",
            "Jeg sover svært dårlig, ikke mer enn 2-3 timer per natt."),
        new("4. Matlyst",
            "Her ber vi deg anføre hvordan appetitten din er, sammenlignet med det som er normalt for deg.",
            "Min appetitt er slik den pleier å være.",
            "Min appetitt er dårligere enn vanlig.",
            "Jeg har nesten ikke appetitt. Maten smaker ikke, og jeg må tvinge meg til å spise.",
            "Jeg orker ikke mat i det hele tatt. Hvis jeg skal få noe i meg må jeg overtales til å spise."),
        new("5. Konsentrasjonsevne",
            "Her ber vi deg vurdere din evne til å holde tankene samlet og konsentrere deg om ulike aktiviteter.",
            "Jeg har ingen konsentrasjonsvanskeligheter.",
            "Jeg har for tiden problemer med å holde tankene mine samlet om slikt som normalt burde fange oppmerksomheten.",
            "Jeg har betydelige problemer med å konsentrere meg om slikt som normalt ikke krever noen anstrengelse fra min side.",
            "Jeg kan overhodet ikke konsentrere meg om noen ting."),
        new("6. Initiativ",
            "Her ber vi deg vurdere din handlekraft de tre siste dagene — om du har lett eller vanskelig for å komme i gang med saker.",
            "Jeg har ingen vanskeligheter med å påbegynne nye oppgaver.",
            "Når jeg skal påbegynne noe, byr det meg imot på en måte som ikke er normal for meg.",
            "Det kreves en stor anstrengelse av meg bare å komme i gang med enkle oppgaver som jeg vanligvis utfører mer eller mindre rutinemessig.",
            "Jeg klarer ikke å påbegynne selv de enkleste hverdagssysler."),
        new("7. Følelsesmessig engasjement",
            "Her ber vi deg ta stilling til din interesse for omverdenen og andre mennesker, og for aktiviteter som pleier å gi deg glede.",
            "Jeg er interessert i omverdenen og engasjerer meg i den, og det gir meg både moro og glede.",
            "Jeg føler mindre sterkt for det som pleier å engasjere meg. Jeg har vanskeligere enn vanlig for å bli glad eller sint når det er nødvendig.",
            "Jeg har ingen interesse eller følelser for omverdenen, ikke engang for venner og bekjente.",
            "Jeg har sluttet å oppleve følelser. Jeg føler meg smertefullt likegyldig til og med overfor mine nærmeste."),
        new("8. Pessimisme",
            "Dette omhandler hvordan du ser på din egen fremtid og din egen verdi — om du bebreider deg selv eller plages av skyldfølelse.",
            "Jeg ser lyst på fremtiden. Jeg er stort sett ganske fornøyd med meg selv.",
            "Iblant anklager jeg meg selv og synes jeg er mindre verd enn andre.",
            "Jeg grubler ofte over mine tabber og føler meg mindreverdig eller verdiløs, selv om andre synes noe annet.",
            "Jeg ser svart på alt og kan ikke se noe håp. Det føles som jeg er et tvers gjennom verdiløst menneske."),
        new("9. Livslyst",
            "Spørsmålet gjelder lysten til å leve, om livet har føltes meningsløst, og om du har hatt tanker om selvmord.",
            "Jeg har normal lyst til å leve.",
            "Livet føles ikke særlig meningsfylt, men jeg ønsker likevel ikke at jeg var død.",
            "Jeg synes ofte det ville vært bedre å være død, og selv om jeg egentlig ikke ønsker det kan selvmord av og til oppleves som en mulig løsning.",
            "Jeg har egentlig innsett at min eneste utvei er å dø, og jeg tenker mye på hvordan jeg skal ta livet av meg.")
    };

    private const string Kategori = "Depresjon og bipolaritet";

    private const string RapportIntroduksjonTekst =
        "MADRS-S er selvutfyllingsversjonen av Montgomery Åsberg Depression Rating Scale — 9 spørsmål om " +
        "stemningsleiet de siste 3 dagene, hvert skåret 0–6. Krever klinisk skjønn ved tolkning; ingen absolutt " +
        "cutoff-«sannhet» finnes ifølge brukerveiledningen.";

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
            navn: "MADRS-S (Montgomery Åsberg Depression Rating Scale – selvutfylling)",
            beskrivelse: Stem,
            belonningstekst: "Takk for at du fylte ut MADRS-S. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Egenvurdering av stemningsleie", Stem, cancellationToken);

        foreach (var ledd in Ledd)
        {
            await testService.LeggTilLeddAsync(
                side.Id, ledd.Etikett, ledd.Intro, TestSvartype.LikertSkala, ledd.Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
