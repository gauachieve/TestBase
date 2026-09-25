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
/// kjente diagnostiske trekket (selve trekkene — f.eks. "frykt for
/// forlatelse" ved emosjonelt ustabil personlighetsforstyrrelse — er
/// velkjent fagkunnskap), IKKE et sitat fra DSM-5-manualen. Antall kriterier
/// og terskelverdi per forstyrrelse (f.eks. "minst 5 av 9" for emosjonelt
/// ustabil) er hentet fra offentlig kjent diagnostisk struktur.
///
/// Brukeren selv har eksplisitt bedt om at dette bygges som et "første
/// forsøk" ("det blir feil uansett, men lettere å kommentere enn å beskrive
/// for hånd") — forventet å kvalitetssikres og korrigeres av bruker (se
/// docs/beslutningslogg.md "SCID-5-PF"). IKKE ment som et validert
/// diagnostisk verktøy før det er gjennomgått.
///
/// Struktur: 10 TestSider, én per forstyrrelse, med kriterier skåret
/// 0=Fraværende/1=Delvis (subklinisk)/2=Oppfylt. Antisosial personlighets-
/// forstyrrelse har i tillegg to portvakt-ledd (mistanke om atferdsforstyrrelse
/// før 15 år, alder ≥18 år) FØR sine 7 kriterier — identifisert i skårings-
/// motoren via Svartype (JaNei), ikke via listeposisjon.
/// </summary>
public sealed class Scid5PfTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "scid5_pf";

    private const string KriterieSkala = "0:Fraværende,1:Delvis til stede (subklinisk),2:Tydelig oppfylt";

    private sealed record Forstyrrelse(string Navn, int Terskel, string[] Kriterier);

    private static readonly Forstyrrelse[] Forstyrrelser =
    {
        new("Paranoid personlighetsforstyrrelse", 4, new[]
        {
            "Mistenker, uten tilstrekkelig grunnlag, at andre utnytter, skader eller lurer vedkommende",
            "Er opptatt av uberettiget tvil om lojaliteten eller troverdigheten til venner eller kolleger",
            "Er tilbakeholden med å betro seg til andre, av frykt for at informasjonen brukes mot en",
            "Leser skjulte nedsettende eller truende betydninger inn i uskyldige bemerkninger eller hendelser",
            "Bærer nag vedvarende — tilgir ikke fornærmelser, krenkelser eller forbigåelser",
            "Oppfatter angrep på egen karakter eller rykte som andre ikke ser, og reagerer raskt med sinne eller motangrep",
            "Har gjentatte, uberettigede mistanker om en partners troskap"
        }),
        new("Schizoid personlighetsforstyrrelse", 4, new[]
        {
            "Ønsker verken å ha eller å nyte nære relasjoner, inkludert med familie",
            "Velger nesten alltid solitære aktiviteter",
            "Har liten eller ingen interesse for seksuelle opplevelser med en annen person",
            "Finner glede i få, om noen, aktiviteter",
            "Mangler nære venner eller fortrolige utover nær familie",
            "Virker likegyldig til både ros og kritikk fra andre",
            "Viser emosjonell kulde, distanse eller flat affekt"
        }),
        new("Schizotyp personlighetsforstyrrelse", 5, new[]
        {
            "Har referanseideer (uten at det er egentlige vrangforestillinger)",
            "Har rare overbevisninger eller magisk tenkning som påvirker atferd, i strid med subkulturelle normer",
            "Har uvanlige persepsjonsopplevelser, inkludert kroppslige illusjoner",
            "Har rar tenkning og tale (vag, omstendelig, metaforisk, overelaborert eller stereotyp)",
            "Er mistenksom eller har paranoide tanker",
            "Har upassende eller innsnevret affekt",
            "Fremstår rar, eksentrisk eller sær i atferd eller fremtoning",
            "Mangler nære venner eller fortrolige utover nær familie",
            "Har overdreven sosial angst som ikke avtar med fortrolighet, og som henger sammen med paranoide frykter snarere enn negativ selvvurdering"
        }),
        new("Antisosial personlighetsforstyrrelse", 3, new[]
        {
            "Manglende evne til å overholde sosiale normer/lover — gjentatte handlinger som er grunnlag for pågripelse",
            "Bedragersk atferd — gjentatt løgn, bruk av dekknavn, eller lureri av andre for personlig vinning",
            "Impulsivitet eller manglende evne til å planlegge fremover",
            "Irritabilitet og aggressivitet — gjentatte slagsmål eller overfall",
            "Hensynsløs likegyldighet for egen eller andres sikkerhet",
            "Konsekvent uansvarlighet — gjentatt unnlatelse av å opprettholde arbeid eller økonomiske forpliktelser",
            "Mangel på anger — er likegyldig til eller rasjonaliserer å ha skadet, mishandlet eller stjålet fra andre"
        }),
        new("Emosjonelt ustabil personlighetsforstyrrelse (Borderline)", 5, new[]
        {
            "Gjør desperate forsøk på å unngå reell eller innbilt forlatelse",
            "Har et mønster av ustabile og intense mellommenneskelige relasjoner (idealisering/devaluering)",
            "Har en identitetsforstyrrelse — markert og vedvarende ustabilt selvbilde",
            "Er impulsiv på minst to potensielt selvskadelige områder (f.eks. pengebruk, sex, rus, kjøring, overspising)",
            "Har gjentatt selvmordsatferd, -gestikulering, -trusler eller selvskading",
            "Har affektiv ustabilitet grunnet markert reaktivitet i stemningsleiet",
            "Har en kronisk følelse av tomhet",
            "Har uttalt sinne eller vansker med å kontrollere sinne",
            "Har forbigående, stressrelaterte paranoide tanker eller alvorlige dissosiative symptomer"
        }),
        new("Histrionisk personlighetsforstyrrelse", 5, new[]
        {
            "Er ukomfortabel når ikke i sentrum av oppmerksomheten",
            "Viser upassende seksuelt forførende eller provoserende atferd",
            "Viser raskt skiftende og grunne følelsesuttrykk",
            "Bruker konsekvent fysisk fremtoning for å trekke oppmerksomhet til seg selv",
            "Har en impresjonistisk taleform med mangel på detaljer",
            "Viser selvdramatisering, teatralitet og overdreven følelsesuttrykk",
            "Er lettpåvirkelig av andre eller av omstendigheter",
            "Oppfatter relasjoner som mer intime enn de faktisk er"
        }),
        new("Narsissistisk personlighetsforstyrrelse", 5, new[]
        {
            "Har et grandiost selvbilde av egen betydning",
            "Er opptatt av fantasier om ubegrenset suksess, makt, briljans, skjønnhet eller ideell kjærlighet",
            "Tror man er \"spesiell\" og unik, og bare kan forstås av/bør omgås andre spesielle eller høytstående personer",
            "Krever overdreven beundring",
            "Forventer spesialbehandling eller automatisk etterkommelse av egne forventninger",
            "Utnytter andre mellommenneskelig for egen vinning",
            "Mangler empati",
            "Er ofte misunnelig på andre, eller tror andre er misunnelige på vedkommende",
            "Viser arrogant, hovmodig atferd eller holdninger"
        }),
        new("Unnvikende personlighetsforstyrrelse", 4, new[]
        {
            "Unngår yrkesmessige aktiviteter med mye mellommenneskelig kontakt, av frykt for kritikk eller avvisning",
            "Vil ikke involvere seg med andre uten sikkerhet for å bli likt",
            "Viser tilbakeholdenhet i nære relasjoner av frykt for å bli gjort til skamme eller ledd av",
            "Er opptatt av å bli kritisert eller avvist i sosiale situasjoner",
            "Er hemmet i nye mellommenneskelige situasjoner grunnet følelse av utilstrekkelighet",
            "Ser på seg selv som sosialt inkompetent, lite tiltrekkende eller mindreverdig",
            "Er uvanlig motvillig til å ta personlige risikoer eller involvere seg i nye aktiviteter, av frykt for at det skal vise seg pinlig"
        }),
        new("Avhengig personlighetsforstyrrelse", 5, new[]
        {
            "Har vansker med å ta hverdagslige beslutninger uten overdrevne råd og forsikring fra andre",
            "Trenger andre til å ta ansvar for de fleste viktige livsområder",
            "Har vansker med å uttrykke uenighet, av frykt for å miste støtte eller godkjenning",
            "Har vansker med å igangsette prosjekter eller gjøre ting alene (mangel på selvtillit, ikke motivasjon)",
            "Går uforholdsmessig langt for å oppnå omsorg og støtte fra andre",
            "Føler seg ukomfortabel eller hjelpeløs alene, av overdreven frykt for ikke å klare seg selv",
            "Søker straks en ny relasjon som omsorgs-/støttekilde når en nær relasjon tar slutt",
            "Er urealistisk opptatt av frykten for å bli overlatt til å ta vare på seg selv"
        }),
        new("Tvangspreget personlighetsforstyrrelse", 4, new[]
        {
            "Er opptatt av detaljer, regler, lister, orden, organisering eller skjemaer i en slik grad at hovedpoenget med aktiviteten går tapt",
            "Har en perfeksjonisme som forstyrrer fullføring av oppgaver",
            "Er overdrevent dedikert til arbeid og produktivitet, på bekostning av fritid og vennskap",
            "Er overdrevent samvittighetsfull, skrupuløs og lite fleksibel med hensyn til moral, etikk eller verdier",
            "Klarer ikke å kaste utslitte eller verdiløse gjenstander, selv uten sentimental verdi",
            "Er motvillig til å delegere oppgaver eller samarbeide med andre, med mindre de underkaster seg egen måte å gjøre ting på",
            "Har en gjerrig pengebruksstil mot både seg selv og andre; ser penger som noe som skal spares til fremtidige katastrofer",
            "Viser stivhet og stahet"
        })
    };

    private const string Kategori = "Personlighet, relasjoner og sosial fungering";

    private const string RapportIntroduksjonTekst =
        "SCID-5-PF er en kliniker-administrert screening av de 10 DSM-5 personlighetsforstyrrelsene, " +
        "inspirert av strukturen i SCID-5-PD-intervjuet — fylt ut av behandler etter klinisk intervju, IKKE " +
        "av pasienten selv. Dette er en EGEN, forenklet tilpasning (ikke en gjengivelse av det lisensierte " +
        "originalintervjuet eller av DSM-5s ordrette kriterier) — et bevisst \"første forsøk\" som bør " +
        "kvalitetssikres og korrigeres av behandler før klinisk bruk. Hver forstyrrelse har sitt eget antall " +
        "kriterier og sin egen terskelverdi for om diagnostisk terskel er nådd.";

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
            beskrivelse: "Basert på et klinisk intervju med pasienten, vurder hvert kriterium for hver av de 10 personlighetsforstyrrelsene. Bruk kommentarfeltet til å begrunne vurderingen — dette er spesielt viktig her siden terskelverdiene er omtrentlige.",
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
                    "DSM-5 krever dette som et tilleggskriterium for antisosial personlighetsforstyrrelse.",
                    TestSvartype.JaNei, null, cancellationToken);
                await testService.LeggTilLeddAsync(
                    side.Id, "Er pasienten 18 år eller eldre?",
                    "Diagnosen kan ikke settes før fylte 18 år.",
                    TestSvartype.JaNei, null, cancellationToken);
            }

            foreach (var kriterium in f.Kriterier)
            {
                await testService.LeggTilLeddAsync(side.Id, kriterium, null, TestSvartype.LikertSkala, KriterieSkala, cancellationToken);
            }
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
