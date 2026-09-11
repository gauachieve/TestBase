namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// RAADS-R (Ritvo Autisme Asperger Diagnoseskjema – Revidert), © Riva Ariella
/// Ritvo, Ph.D. og Edward Ritvo, M.D., 2007. Norsk oversettelse (v. 1.0,
/// 250112) ved Noricom Tolke og Translatørtjeneste, på oppdrag av Regionalt
/// fagmiljø for autisme, ADHD, Tourettes syndrom og narkolepsi Helse Sør-Øst,
/// Oslo universitetssykehus. Kilde:
/// https://www.oslo-universitetssykehus.no/493043/contentassets/56a56b24850043a2af1ef7f9297b83d4/1radds-r-norsk-versjon-1.0-kopi.pdf
/// (hentet og lest i sin helhet 2026-09-12, se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\RAADS_R.md). De
/// ~23 anamnestiske bakgrunnsspørsmålene (del A i originaldokumentet — navn,
/// alder, sivilstand, skolegang, diagnosehistorikk osv.) er IKKE digitalisert
/// som testledd — de er registreringsfelter uten skåringsverdi, ikke en del
/// av selve instrumentets 80 skårede utsagn. IKKE juridisk/klinisk
/// kvalitetssikret av oss utover dette, se docs/beslutningslogg.md.
/// </summary>
public sealed class RaadsRTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "raads_r";

    private const string Stem =
        "Nedenfor finner du en liste med utsagn om livserfaringer og personlighetstrekk som kanskje passer for " +
        "deg. For hvert utsagn, velg det svaret som best beskriver deg.";

    private const string Skala =
        "1:Stemmer nå og da jeg var ung,2:Stemmer bare nå,3:Stemte bare da jeg var under 16 år,4:Aldri stemt";

    /// <summary>(Spørsmålstekst, ErReversert) — reverserte (normative) items skåres motsatt vei, se Skaaringsberegner.</summary>
    private static readonly (string Tekst, bool ErReversert)[] Ledd =
    {
        ("Jeg er en medfølende person.", true),
        ("Jeg bruker ofte ord og uttrykk fra filmer og TV når jeg snakker med andre.", false),
        ("Jeg blir ofte overrasket når andre sier at jeg har vært uhøflig.", false),
        ("Noen ganger snakker jeg for høyt eller for lavt, uten at jeg er klar over det.", false),
        ("Jeg vet ofte ikke hvordan jeg skal oppføre meg sammen med andre.", false),
        ("Jeg kan sette meg inn i en annen persons situasjon.", true),
        ("Jeg har problemer med å forstå hva enkelte uttrykk betyr, som for eksempel \"du er lyset i mitt liv\".", false),
        ("Jeg liker bare å snakke med folk som deler mine interesser.", false),
        ("Jeg legger mer merke til detaljer enn helheten.", false),
        ("Jeg legger alltid merke til hvordan maten kjennes ut i munnen. Det er viktigere for meg enn hvordan den smaker.", false),
        ("Jeg savner familie og venner når vi er lenge borte fra hverandre.", true),
        ("Jeg fornærmer noen ganger andre når jeg sier hva jeg tenker, selv om jeg ikke mente å fornærme dem.", false),
        ("Jeg liker bare å tenke på og snakke om noen få ting som interesserer meg.", false),
        ("Jeg går heller alene på restaurant enn sammen med noen jeg kjenner.", false),
        ("Jeg kan ikke forestille meg hvordan det er å skulle være en annen person.", false),
        ("Jeg har blitt fortalt at jeg er klønete og klossete.", false),
        ("Andre synes jeg er rar eller annerledes.", false),
        ("Jeg forstår når venner trenger å bli trøstet.", true),
        ("Jeg er veldig følsom for hvordan klærne mine kjennes ut når jeg berører dem. Hvordan de føles er viktigere for meg enn hvordan de ser ut.", false),
        ("Jeg liker å etterligne hvordan noen mennesker snakker og oppfører seg på. Det hjelper meg til å virke mer normal.", false),
        ("Det kan være veldig ubehagelig for meg å snakke med flere personer samtidig.", false),
        ("Jeg må oppføre meg \"normalt\" for å glede andre slik at de skal like meg.", false),
        ("Å møte nye mennesker er vanligvis lett for meg.", true),
        ("Jeg blir veldig forvirret når noen avbryter meg mens jeg snakker om noe som jeg er veldig interessert i.", false),
        ("Det er vanskelig for meg å forstå hvordan andre har det når vi snakker sammen.", false),
        ("Jeg liker å snakke med flere mennesker, for eksempel når vi sitter ved middagsbordet, er på skolen eller på arbeidet.", true),
        ("Jeg oppfatter ting altfor bokstavelig, slik at jeg ofte går glipp av hva folk prøver å fortelle meg.", false),
        ("Jeg har vansker med å forstå når noen blir flaue eller sjalu.", false),
        ("Vanlige tekstiler som ikke er plagsomme for andre, føles veldig ubehagelig når de kommer i kontakt med huden min.", false),
        ("Jeg blir veldig opprørt når måten jeg liker å gjøre ting på plutselig blir endret.", false),
        ("Jeg har aldri ønsket eller hatt behov for det noen kaller et \"intimt forhold\".", false),
        ("Det er vanskelig for meg å begynne eller avslutte en samtale. Jeg må fortsette til jeg er ferdig.", false),
        ("Jeg snakker med normalt tempo (passe fort).", true),
        ("Opplevelsen av den samme lyden, fargen eller overflaten på et materiale kan skifte fra veldig sterk og intens til veldig svak og uinteressant.", false),
        ("Uttrykket \"sommerfugler i magen\" gir meg en ubehagelig følelse.", false),
        ("Jeg får av og til vondt i ørene av et bestemt ord eller høy lyd.", false),
        ("Jeg er en forståelsesfull person.", true),
        ("Jeg identifiserer meg ikke med rollefigurer i filmer og kan heller ikke føle det de føler.", false),
        ("Jeg forstår ikke når noen flørter med meg.", false),
        ("Jeg kan detaljert forestille meg ting som jeg er interessert i.", false),
        ("Jeg lager lister over ting som interesserer meg, selv om de ikke har noen praktisk betydning.", false),
        ("Når jeg blir overveldet av inntrykk, må jeg være for meg selv for å stenge inntrykkene ute.", false),
        ("Jeg liker å diskutere ting med mine venner.", true),
        ("Jeg aner ikke om noen er interessert i eller synes det jeg sier er kjedelig.", false),
        ("Det kan være vanskelig å forstå folks ansiktsuttrykk, håndbevegelser (gester) og kroppsspråk mens de snakker.", false),
        ("De samme tingene kan oppleves veldig forskjellig for meg på ulike tidspunkt.", false),
        ("Jeg føler meg komfortabel med å møte eller omgås andre.", true),
        ("Jeg prøver å være så hjelpsom som jeg klarer når andre mennesker forteller meg om sine personlige problemer.", true),
        ("Andre har sagt at jeg har en uvanlig stemme (for eksempel monoton, ensformig, barnslig eller skarp).", false),
        ("Noen ganger blir jeg opphengt i en tanke eller en sak som jeg må snakke om selv om ingen er interessert i dette.", false),
        ("Jeg gjør visse bevegelser med hendene mine om og om igjen (som for eksempel å vifte med hendene, snurre pinner eller snor).", false),
        ("Jeg har aldri vært interessert i det som de fleste jeg kjenner, er interessert i.", false),
        ("Jeg oppfattes som en person som har lett for å vise omtanke for andre.", true),
        ("Jeg kommer overens med andre mennesker ved å følge visse regler som får meg til å virke normal.", false),
        ("Jeg synes det er veldig vanskelig å arbeide og fungere i gruppe sammen med andre.", false),
        ("Når jeg snakker med noen, er det vanskelig å skifte samtaleemne. Hvis den andre gjør det, blir jeg opprørt og forvirret.", false),
        ("Noen ganger må jeg holde meg for ørene for å slippe å høre ubehagelige lyder.", false),
        ("Jeg kan småprate med andre folk.", true),
        ("Noen ganger føler jeg at ting som skulle være smertefulle, ikke er det.", false),
        ("Når jeg snakker med noen, har jeg problemer med å forstå når det er min tur til å snakke eller lytte.", false),
        ("De som kjenner meg best, oppfatter meg som en person som trives best med å være alene.", false),
        ("Jeg snakker vanligvis i normalt toneleie.", true),
        ("Jeg liker at ting skal være nøyaktig de samme dag etter dag, og selv små forandringer i mine rutiner gjør meg opprørt.", false),
        ("Det er et mysterium for meg hvordan man får venner og er sammen med dem.", false),
        ("Når jeg føler meg stresset, synes jeg det er beroligende å gå litt rundt eller gynge i en stol.", false),
        ("Uttrykket \"han er som en åpen bok\" er vanskelig å forstå.", false),
        ("Jeg blir engstelig og redd når jeg er på et sted med mange lukter, overflater å ta på, bråk eller skarpt lys.", false),
        ("Jeg forstår når noen sier en ting, men mener noe annet.", true),
        ("Jeg liker å være alene så mye som mulig.", false),
        ("Jeg har tankene mine lagret i hukommelsen som om de var arkivert.", false),
        ("De samme lydene høres noen ganger ut som om de er veldig høye eller veldig lave selv om jeg vet at de ikke har forandret seg.", false),
        ("Jeg liker å bruke tid på å spise og snakke med familien og vennene mine.", true),
        ("Jeg tåler ikke ting som jeg ikke liker (som lukt, stoffer, lyder og farger).", false),
        ("Jeg liker ikke å bli klemt eller holdt rundt.", false),
        ("Når jeg skal et sted, må jeg følge den veien jeg kjenner, ellers kan jeg bli forvirret og opprørt.", false),
        ("Det er vanskelig å forestille meg hva andre mennesker forventer av meg.", false),
        ("Jeg liker å ha gode venner.", true),
        ("Folk forteller meg at jeg er altfor opptatt av detaljer eller altfor omstendelig.", false),
        ("Jeg blir fortalt at jeg av og til stiller pinlige spørsmål.", false),
        ("Jeg har en tendens til å kommentere feil hos andre.", false)
    };

    private const string Kategori = "ADHD, autisme og nevroutvikling";

    private const string RapportIntroduksjonTekst =
        "RAADS-R er et selvutfyllingsinstrument for autismespekterforstyrrelse/Asperger syndrom hos voksne — " +
        "80 utsagn skåret 0–3, hvorav 17 normative (reverserte). Anbefales gjennomgått med behandler til stede.";

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
            navn: "RAADS-R (Ritvo Autisme Asperger Diagnoseskjema – Revidert)",
            beskrivelse: Stem,
            belonningstekst: "Takk for at du fylte ut RAADS-R. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Livserfaringer og personlighetstrekk", Stem, cancellationToken);

        foreach (var (tekst, _) in Ledd)
        {
            await testService.LeggTilLeddAsync(
                side.Id, tekst, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }

    /// <summary>Eksponert for RaadsRSkaaringsberegner (og enhetstester) slik at reversert-flagget og selve teksten aldri kan komme ut av synk.</summary>
    public static IReadOnlyList<bool> ErReversertPerLedd { get; } = Ledd.Select(l => l.ErReversert).ToArray();
}
