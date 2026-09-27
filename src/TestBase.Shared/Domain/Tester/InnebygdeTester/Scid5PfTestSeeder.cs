namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// SCID-5-PF — en KLINIKER-ADMINISTRERT screening for de 10 DSM-5
/// personlighetsforstyrrelsene (Section II), inspirert av strukturen i det
/// virkelige, kommersielt lisensierte SCID-5-PD-intervjuet (First, Williams,
/// Karg &amp; Spitzer). Bruker samme "behandler fyller ut"-mekanisme som
/// YGTSS-R og MADRS klinikkversjon — sendes ALDRI til pasienten.
///
/// VIKTIG, LES FØR KLINISK BRUK: dette er BEVISST en FORENKLET, EGEN
/// tilpasning, ikke en gjengivelse av det ekte SCID-5-PD-intervjuet eller av
/// DSM-5s ordrette diagnosekriterier (begge er opphavsrettslig beskyttet av
/// hhv. American Psychiatric Association Publishing og APA). Hvert
/// kriterium under er en EGEN, klinisk informert PARAFRASE av det offentlig
/// kjente diagnostiske trekket, IKKE et sitat fra DSM-5-manualen — samme
/// gjelder de korte "veiledning/eksempel"-tekstene per kriterium (vist i en
/// egen forklaringsboks ved siden av kommentarfeltet på utfyllingssiden,
/// se FyllForPasient.cshtml) — EGNE illustrasjonseksempler, ikke sitert fra
/// noe kildedokument. Antall kriterier og terskelverdi per forstyrrelse er
/// hentet fra offentlig kjent diagnostisk struktur.
///
/// REKKEFØLGE (rettet 2026-09-27, se docs/beslutningslogg.md "Natt-økt, del
/// 13"): følger nå SCID-5-PD sin faktiske modulrekkefølge — Unnvikende,
/// Avhengig, Tvangspreget, Paranoid, Schizotyp, Schizoid, Histrionisk,
/// Narsissistisk, Emosjonelt ustabil, og Antisosial SIST (krever dokumentert
/// barndomsdebut, undersøkes derfor til slutt i det ekte intervjuet også).
/// Første versjon (2026-09-25) hadde Antisosial fjerde — brukeren påpekte at
/// dette ikke stemte med reell klinisk praksis.
///
/// Brukeren selv har eksplisitt bedt om at dette bygges som et "første
/// forsøk" ("det blir feil uansett, men lettere å kommentere enn å beskrive
/// for hånd") — forventet å kvalitetssikres og korrigeres av bruker (se
/// docs/beslutningslogg.md "SCID-5-PF"). IKKE ment som et validert
/// diagnostisk verktøy før det er gjennomgått.
///
/// Struktur: 10 TestSider, én per forstyrrelse, med kriterier skåret
/// 0=Fraværende/1=Delvis (subklinisk)/2=Oppfylt — selve skala-TEKSTEN har nå
/// et tallprefiks ("0. Fraværende" osv., ikke bare tallverdien bak
/// radioknappen) etter brukerens ønske. Antisosial personlighets-
/// forstyrrelse har i tillegg to portvakt-ledd (mistanke om atferds-
/// forstyrrelse før 15 år, alder ≥18 år) FØR sine 7 kriterier — identifisert
/// i skåringsmotoren via Svartype (JaNei), ikke via listeposisjon.
/// </summary>
public sealed class Scid5PfTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "scid5_pf";

    private const string KriterieSkala = "0:0. Fraværende,1:1. Delvis til stede (subklinisk),2:2. Tydelig oppfylt";

    private sealed record Kriterium(string Tekst, string Veiledning);
    private sealed record Forstyrrelse(string Navn, int Terskel, Kriterium[] Kriterier);

    private static readonly Forstyrrelse[] Forstyrrelser =
    {
        new("Unnvikende personlighetsforstyrrelse", 4, new[]
        {
            new Kriterium(
                "Unngår yrkesmessige aktiviteter med mye mellommenneskelig kontakt, av frykt for kritikk eller avvisning",
                "Eksempel: takker nei til stillinger med mye kunde-/møtekontakt til tross for at kompetansen er der."),
            new Kriterium(
                "Vil ikke involvere seg med andre uten sikkerhet for å bli likt",
                "Eksempel: unngår nye vennskap med mindre den andre først uttrykkelig viser interesse."),
            new Kriterium(
                "Viser tilbakeholdenhet i nære relasjoner av frykt for å bli gjort til skamme eller ledd av",
                "Eksempel: holder tilbake følelser overfor partner av frykt for å bli latterliggjort."),
            new Kriterium(
                "Er opptatt av å bli kritisert eller avvist i sosiale situasjoner",
                "Eksempel: grubler lenge på hvordan man ble oppfattet etter et møte."),
            new Kriterium(
                "Er hemmet i nye mellommenneskelige situasjoner grunnet følelse av utilstrekkelighet",
                "Eksempel: sier lite i nye grupper av frykt for å virke dum."),
            new Kriterium(
                "Ser på seg selv som sosialt inkompetent, lite tiltrekkende eller mindreverdig",
                "Eksempel: sammenligner seg konsekvent negativt med andre sosialt."),
            new Kriterium(
                "Er uvanlig motvillig til å ta personlige risikoer eller involvere seg i nye aktiviteter av frykt for at det skal vise seg pinlig",
                "Eksempel: takker nei til et kurs/en klubb av frykt for å dumme seg ut.")
        }),
        new("Avhengig personlighetsforstyrrelse", 5, new[]
        {
            new Kriterium(
                "Har vansker med å ta hverdagslige beslutninger uten overdrevne råd og forsikring fra andre",
                "Eksempel: ringer noen for råd før selv en enkel klesinnkjøp."),
            new Kriterium(
                "Trenger andre til å ta ansvar for de fleste viktige livsområder",
                "Eksempel: lar partner styre økonomi/bosted uten egen involvering."),
            new Kriterium(
                "Har vansker med å uttrykke uenighet, av frykt for å miste støtte eller godkjenning",
                "Eksempel: sier seg enig selv når man er uenig, for å unngå konflikt."),
            new Kriterium(
                "Har vansker med å igangsette prosjekter eller gjøre ting alene (mangel på selvtillit, ikke motivasjon)",
                "Eksempel: utsetter et prosjekt til noen andre tar initiativ."),
            new Kriterium(
                "Går uforholdsmessig langt for å oppnå omsorg og støtte fra andre",
                "Eksempel: påtar seg ubehagelige oppgaver kun for å beholde en relasjon."),
            new Kriterium(
                "Føler seg ukomfortabel eller hjelpeløs alene, av overdreven frykt for ikke å klare seg selv",
                "Eksempel: unngår å bo alene selv når det er praktisk fullt mulig."),
            new Kriterium(
                "Søker straks en ny relasjon som omsorgs-/støttekilde når en nær relasjon tar slutt",
                "Eksempel: inngår en ny relasjon svært kort tid etter et samlivsbrudd, primært for støtte."),
            new Kriterium(
                "Er urealistisk opptatt av frykten for å bli overlatt til å ta vare på seg selv",
                "Eksempel: bekymrer seg overdrevent for hvordan man skal klare seg om man blir alene.")
        }),
        new("Tvangspreget personlighetsforstyrrelse", 4, new[]
        {
            new Kriterium(
                "Er opptatt av detaljer, regler, lister, orden, organisering eller skjemaer i en slik grad at hovedpoenget med aktiviteten går tapt",
                "Eksempel: bruker timevis på et perfekt regneark mens selve fristen glipper."),
            new Kriterium(
                "Har en perfeksjonisme som forstyrrer fullføring av oppgaver",
                "Eksempel: leverer aldri et prosjekt fordi det aldri blir \"godt nok\"."),
            new Kriterium(
                "Er overdrevent dedikert til arbeid og produktivitet, på bekostning av fritid og vennskap",
                "Eksempel: dropper gjentatte ganger sosiale planer/ferier for å jobbe."),
            new Kriterium(
                "Er overdrevent samvittighetsfull, skrupuløs og lite fleksibel med hensyn til moral, etikk eller verdier",
                "Eksempel: insisterer rigid på en regel selv når et unntak er åpenbart rimelig."),
            new Kriterium(
                "Klarer ikke å kaste utslitte eller verdiløse gjenstander, selv uten sentimental verdi",
                "Eksempel: samler gamle aviser/gjenstander \"man kan få bruk for en dag\"."),
            new Kriterium(
                "Er motvillig til å delegere oppgaver eller samarbeide med andre, med mindre de underkaster seg egen måte å gjøre ting på",
                "Eksempel: gjør alt selv fordi andre \"ikke gjør det riktig\"."),
            new Kriterium(
                "Har en gjerrig pengebruksstil mot både seg selv og andre; ser penger som noe som skal spares til fremtidige katastrofer",
                "Eksempel: nekter seg selv/familien nødvendige utgifter av frykt for en fremtidig krise."),
            new Kriterium(
                "Viser stivhet og stahet",
                "Eksempel: vanskelig å endre en plan selv når omstendighetene klart tilsier det.")
        }),
        new("Paranoid personlighetsforstyrrelse", 4, new[]
        {
            new Kriterium(
                "Mistenker, uten tilstrekkelig grunnlag, at andre utnytter, skader eller lurer vedkommende",
                "Eksempel: antar uten holdepunkter at kollegaer bevisst holder informasjon tilbake."),
            new Kriterium(
                "Er opptatt av uberettiget tvil om lojaliteten eller troverdigheten til venner eller kolleger",
                "Eksempel: mistenker venner for baksnakking uten konkrete holdepunkter."),
            new Kriterium(
                "Er tilbakeholden med å betro seg til andre, av frykt for at informasjonen brukes mot en",
                "Eksempel: unngår å dele personlig informasjon selv med nære relasjoner."),
            new Kriterium(
                "Leser skjulte nedsettende eller truende betydninger inn i uskyldige bemerkninger eller hendelser",
                "Eksempel: tolker en nøytral kommentar som en skjult fornærmelse."),
            new Kriterium(
                "Bærer nag vedvarende — tilgir ikke fornærmelser, krenkelser eller forbigåelser",
                "Eksempel: husker og gjengjelder en krenkelse år etter at den skjedde."),
            new Kriterium(
                "Oppfatter angrep på egen karakter eller rykte som andre ikke ser, og reagerer raskt med sinne eller motangrep",
                "Eksempel: konfronterer noen aggressivt for en antatt fornærmelse ingen andre la merke til."),
            new Kriterium(
                "Har gjentatte, uberettigede mistanker om en partners troskap",
                "Eksempel: sjekker partners telefon gjentatte ganger uten konkret grunn.")
        }),
        new("Schizotyp personlighetsforstyrrelse", 5, new[]
        {
            new Kriterium(
                "Har referanseideer (uten at det er egentlige vrangforestillinger)",
                "Eksempel: tror tilfeldige hendelser har en spesiell personlig betydning rettet mot en selv."),
            new Kriterium(
                "Har rare overbevisninger eller magisk tenkning som påvirker atferd, i strid med subkulturelle normer",
                "Eksempel: en sterk overbevisning om at egne tanker kan påvirke ytre hendelser direkte."),
            new Kriterium(
                "Har uvanlige persepsjonsopplevelser, inkludert kroppslige illusjoner",
                "Eksempel: føler en usynlig tilstedeværelse i rommet uten en åpenbar psykotisk forklaring."),
            new Kriterium(
                "Har rar tenkning og tale (vag, omstendelig, metaforisk, overelaborert eller stereotyp)",
                "Eksempel: svarer vagt og omstendelig, vanskelig for lytteren å følge tankerekken."),
            new Kriterium(
                "Er mistenksom eller har paranoide tanker",
                "Eksempel: antar skjulte motiver hos fremmede uten konkret grunn."),
            new Kriterium(
                "Har upassende eller innsnevret affekt",
                "Eksempel: smiler eller virker uberørt under alvorlige samtaletemaer."),
            new Kriterium(
                "Fremstår rar, eksentrisk eller sær i atferd eller fremtoning",
                "Eksempel: uvanlig klesstil eller talemåte som tydelig skiller seg fra omgivelsene."),
            new Kriterium(
                "Mangler nære venner eller fortrolige utover nær familie",
                "Eksempel: ingen fortrolige utenom eventuelt nærmeste familie."),
            new Kriterium(
                "Har overdreven sosial angst som ikke avtar med fortrolighet, og som henger sammen med paranoide frykter snarere enn negativ selvvurdering",
                "Eksempel: unngår grupper fordi man mistenker de snakker negativt om en, ikke fordi man frykter å dumme seg ut.")
        }),
        new("Schizoid personlighetsforstyrrelse", 4, new[]
        {
            new Kriterium(
                "Ønsker verken å ha eller å nyte nære relasjoner, inkludert med familie",
                "Eksempel: uttrykker ingen lengsel etter partner eller nære venner."),
            new Kriterium(
                "Velger nesten alltid solitære aktiviteter",
                "Eksempel: foretrekker konsekvent å tilbringe fritiden helt alene."),
            new Kriterium(
                "Har liten eller ingen interesse for seksuelle opplevelser med en annen person",
                "Eksempel: viser ingen interesse for seksuelle relasjoner over tid."),
            new Kriterium(
                "Finner glede i få, om noen, aktiviteter",
                "Eksempel: rapporterer sjelden eller aldri entusiasme for noe."),
            new Kriterium(
                "Mangler nære venner eller fortrolige utover nær familie",
                "Eksempel: ingen fortrolige utenom eventuell nær familie."),
            new Kriterium(
                "Virker likegyldig til både ros og kritikk fra andre",
                "Eksempel: reagerer ikke merkbart verken på skryt eller kritikk fra overordnet."),
            new Kriterium(
                "Viser emosjonell kulde, distanse eller flat affekt",
                "Eksempel: viser lite mimikk eller følelsesuttrykk i samtale.")
        }),
        new("Histrionisk personlighetsforstyrrelse", 5, new[]
        {
            new Kriterium(
                "Er ukomfortabel når ikke i sentrum av oppmerksomheten",
                "Eksempel: leder samtalen konsekvent tilbake til seg selv."),
            new Kriterium(
                "Viser upassende seksuelt forførende eller provoserende atferd",
                "Eksempel: flørter påfallende i klart upassende sosiale sammenhenger."),
            new Kriterium(
                "Viser raskt skiftende og grunne følelsesuttrykk",
                "Eksempel: går fra begeistring til fortvilelse i løpet av minutter."),
            new Kriterium(
                "Bruker konsekvent fysisk fremtoning for å trekke oppmerksomhet til seg selv",
                "Eksempel: kler seg påfallende for å bli lagt merke til."),
            new Kriterium(
                "Har en impresjonistisk taleform med mangel på detaljer",
                "Eksempel: beskriver noe som \"helt fantastisk\" uten konkrete detaljer."),
            new Kriterium(
                "Viser selvdramatisering, teatralitet og overdreven følelsesuttrykk",
                "Eksempel: overdriver reaksjoner på hverdagslige hendelser."),
            new Kriterium(
                "Er lettpåvirkelig av andre eller av omstendigheter",
                "Eksempel: skifter mening raskt etter siste person man snakket med."),
            new Kriterium(
                "Oppfatter relasjoner som mer intime enn de faktisk er",
                "Eksempel: omtaler en bekjent som \"bestevenn\" etter svært kort tids kontakt.")
        }),
        new("Narsissistisk personlighetsforstyrrelse", 5, new[]
        {
            new Kriterium(
                "Har et grandiost selvbilde av egen betydning",
                "Eksempel: overvurderer klart egne prestasjoner/talenter uten faktisk grunnlag."),
            new Kriterium(
                "Er opptatt av fantasier om ubegrenset suksess, makt, briljans, skjønnhet eller ideell kjærlighet",
                "Eksempel: bruker mye tid på å fantasere om fremtidig storhet."),
            new Kriterium(
                "Tror man er \"spesiell\" og unik, og bare kan forstås av/bør omgås andre spesielle eller høytstående personer",
                "Eksempel: mener kun visse høytstående personer kan forstå en."),
            new Kriterium(
                "Krever overdreven beundring",
                "Eksempel: blir tydelig fornærmet uten konstant anerkjennelse."),
            new Kriterium(
                "Forventer spesialbehandling eller automatisk etterkommelse av egne forventninger",
                "Eksempel: blir sint når vanlige regler/køer gjelder for en selv."),
            new Kriterium(
                "Utnytter andre mellommenneskelig for egen vinning",
                "Eksempel: bruker relasjoner strategisk for å oppnå egne fordeler."),
            new Kriterium(
                "Mangler empati",
                "Eksempel: viser lite forståelse for andres følelser eller behov."),
            new Kriterium(
                "Er ofte misunnelig på andre, eller tror andre er misunnelige på vedkommende",
                "Eksempel: nedvurderer andres suksess, eller antar andre er sjalu på en selv."),
            new Kriterium(
                "Viser arrogant, hovmodig atferd eller holdninger",
                "Eksempel: snakker nedlatende til andre.")
        }),
        new("Emosjonelt ustabil personlighetsforstyrrelse (Borderline)", 5, new[]
        {
            new Kriterium(
                "Gjør desperate forsøk på å unngå reell eller innbilt forlatelse",
                "Eksempel: panikkartet reaksjon på en avlyst avtale med en nær person."),
            new Kriterium(
                "Har et mønster av ustabile og intense mellommenneskelige relasjoner (idealisering/devaluering)",
                "Eksempel: går fra å idolisere til fullstendig å avvise samme person."),
            new Kriterium(
                "Har en identitetsforstyrrelse — markert og vedvarende ustabilt selvbilde",
                "Eksempel: skifter drastisk mellom ulike selvbilder/verdier over kort tid."),
            new Kriterium(
                "Er impulsiv på minst to potensielt selvskadelige områder (f.eks. pengebruk, sex, rus, kjøring, overspising)",
                "Eksempel: impulsiv pengebruk OG impulsiv rus-/seksuell atferd i samme periode."),
            new Kriterium(
                "Har gjentatt selvmordsatferd, -gestikulering, -trusler eller selvskading",
                "Eksempel: kutter seg selv ved sterk emosjonell overveldelse."),
            new Kriterium(
                "Har affektiv ustabilitet grunnet markert reaktivitet i stemningsleiet",
                "Eksempel: intense stemningsskift innen samme dag, tydelig utløst av ytre hendelser."),
            new Kriterium(
                "Har en kronisk følelse av tomhet",
                "Eksempel: beskriver en vedvarende følelse av indre tomhet uavhengig av situasjon."),
            new Kriterium(
                "Har uttalt sinne eller vansker med å kontrollere sinne",
                "Eksempel: hyppige sinneutbrudd som er uforholdsmessige til situasjonen."),
            new Kriterium(
                "Har forbigående, stressrelaterte paranoide tanker eller alvorlige dissosiative symptomer",
                "Eksempel: føler seg \"utenfor kroppen\" under sterkt stress.")
        }),
        new("Antisosial personlighetsforstyrrelse", 3, new[]
        {
            new Kriterium(
                "Manglende evne til å overholde sosiale normer/lover — gjentatte handlinger som er grunnlag for pågripelse",
                "Eksempel: gjentatte pågripelser for ulike typer lovbrudd."),
            new Kriterium(
                "Bedragersk atferd — gjentatt løgn, bruk av dekknavn, eller lureri av andre for personlig vinning",
                "Eksempel: bruker falske identiteter eller lyver systematisk for egen vinning."),
            new Kriterium(
                "Impulsivitet eller manglende evne til å planlegge fremover",
                "Eksempel: flytter eller bytter jobb brått uten noen forutgående plan."),
            new Kriterium(
                "Irritabilitet og aggressivitet — gjentatte slagsmål eller overfall",
                "Eksempel: gjentatte fysiske konfrontasjoner utløst av bagateller."),
            new Kriterium(
                "Hensynsløs likegyldighet for egen eller andres sikkerhet",
                "Eksempel: kjører uforsvarlig eller utsetter andre for fare uten bekymring."),
            new Kriterium(
                "Konsekvent uansvarlighet — gjentatt unnlatelse av å opprettholde arbeid eller økonomiske forpliktelser",
                "Eksempel: gjentatte jobber mistet pga. fravær, eller vedvarende ubetalt gjeld."),
            new Kriterium(
                "Mangel på anger — er likegyldig til eller rasjonaliserer å ha skadet, mishandlet eller stjålet fra andre",
                "Eksempel: rasjonaliserer å ha skadet andre som \"deres egen skyld\".")
        })
    };

    private const string Kategori = "Personlighet, relasjoner og sosial fungering";

    private const string RapportIntroduksjonTekst =
        "SCID-5-PF er en kliniker-administrert screening av de 10 DSM-5 personlighetsforstyrrelsene, " +
        "inspirert av strukturen i SCID-5-PD-intervjuet — fylt ut av behandler etter klinisk intervju, IKKE " +
        "av pasienten selv. Dette er en EGEN, forenklet tilpasning (ikke en gjengivelse av det lisensierte " +
        "originalintervjuet eller av DSM-5s ordrette kriterier) — et bevisst \"første forsøk\" som bør " +
        "kvalitetssikres og korrigeres av behandler før klinisk bruk. Hver forstyrrelse har sitt eget antall " +
        "kriterier og sin egen terskelverdi for om diagnostisk terskel er nådd. Rekkefølgen følger SCID-5-PD " +
        "sin faktiske modulrekkefølge (Antisosial sist, siden den krever dokumentert barndomsdebut).";

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
            navn: "SCID-5-PF (personlighetsforstyrrelser, screening — kliniker)",
            beskrivelse: "Basert på et klinisk intervju med pasienten, vurder hvert kriterium for hver av de 10 personlighetsforstyrrelsene. Bruk kommentarfeltet til å begrunne vurderingen — se veiledningsboksen til høyre for et eksempel per kriterium.",
            belonningstekst: "Vurderingen er lagret.",
            kode: Kode,
            cancellationToken: cancellationToken);
        await testService.SettFyllesUtAvBehandlerAsync(test.Id, true, cancellationToken);

        foreach (var f in Forstyrrelser)
        {
            var side = await testService.LeggTilSideAsync(
                test.Id, f.Navn,
                $"Vurder hvert av de {f.Kriterier.Length} kriteriene under. Diagnostisk terskel for {f.Navn.ToLowerInvariant()} er satt til minst {f.Terskel} tydelig oppfylte kriterier.",
                cancellationToken);

            if (f.Navn.StartsWith("Antisosial", StringComparison.Ordinal))
            {
                await testService.LeggTilLeddAsync(
                    side.Id, "Tegn til atferdsforstyrrelse (conduct disorder) før fylte 15 år?",
                    "DSM-5 krever dette som et tilleggskriterium for antisosial personlighetsforstyrrelse. Eksempel: dokumentert skulking, tyveri eller vold i tenårene.",
                    TestSvartype.JaNei, null, cancellationToken);
                await testService.LeggTilLeddAsync(
                    side.Id, "Er pasienten 18 år eller eldre?",
                    "Diagnosen kan ikke settes før fylte 18 år.",
                    TestSvartype.JaNei, null, cancellationToken);
            }

            foreach (var kriterium in f.Kriterier)
            {
                await testService.LeggTilLeddAsync(side.Id, kriterium.Tekst, kriterium.Veiledning, TestSvartype.LikertSkala, KriterieSkala, cancellationToken);
            }
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
