namespace TestBase.Shared.Domain.Hjelp;

/// <summary>
/// Alt hjelpeinnhold i systemet — statisk liste, samme "kode fremfor database"-mønster som
/// resten av den ikke-transaksjonelle innebygde testinnholdet. Oppdater denne filen når UI-et
/// endrer seg eller nye vanlige spørsmål dukker opp (f.eks. fra Admin/Tilbakemeldinger) — ingen
/// migrasjon nødvendig. Se HjelpService for filtrering på rolle/kontekst/søk.
/// </summary>
public static class HjelpInnhold
{
    private static readonly HjelpRolle[] Alle = { HjelpRolle.Anonym, HjelpRolle.Pasient, HjelpRolle.Behandler, HjelpRolle.Admin };
    private static readonly HjelpRolle[] KunAnonym = { HjelpRolle.Anonym };
    private static readonly HjelpRolle[] KunPasient = { HjelpRolle.Pasient };
    private static readonly HjelpRolle[] KunBehandler = { HjelpRolle.Behandler };
    private static readonly HjelpRolle[] KunAdmin = { HjelpRolle.Admin };
    private static readonly HjelpRolle[] BehandlerOgAdmin = { HjelpRolle.Behandler, HjelpRolle.Admin };

    public static readonly IReadOnlyList<HjelpArtikkel> Artikler = new List<HjelpArtikkel>
    {
        // ---------- Anonym (ikke innlogget) ----------
        new("anon-logge-inn-behandler",
            "Hvordan logger jeg inn som behandler eller administrator?",
            "<p>Trykk <strong>Logg inn</strong> øverst til høyre og velg <strong>Logg inn med BankID</strong>. Du bekrefter identiteten din i BankID-appen på telefonen — du trenger ikke taste personnummer på selve TestRom-siden, BankID-appen tar seg av det. Systemet finner kontoen din automatisk og logger deg inn som administrator eller behandler, avhengig av rollen din.</p>",
            KunAnonym, new[] { "/Konto/LoggInn" }, true, "Innlogging"),

        new("anon-logge-inn-pasient",
            "Hvordan logger jeg inn som pasient?",
            "<p>Pasienter logger inn med BankID via en egen side, atskilt fra behandler/administrator. Trykk <strong>Er du pasient?</strong> øverst, eller gå direkte til pasientsiden. Har du nettopp registrert deg via en SMS/e-post-lenke eller en QR-kode fra behandleren din, trenger du som regel ikke logge inn på nytt med det samme — du blir sendt rett videre til testen.</p>",
            KunAnonym, new[] { "/Pasientportal/Konto/LoggInn", "/Pasienter" }, true, "Innlogging"),

        new("anon-fatt-lenke",
            "Jeg har fått en SMS eller e-post med en lenke til en test — hva gjør jeg?",
            "<p>Trykk på lenken i meldingen. Den tar deg rett til testen, uten at du trenger å logge inn separat først. Har du ikke fylt ut hele profilen din ennå (navn, adresse osv.), kan du gjøre det senere — det er ikke nødvendig for å svare på selve testen.</p>",
            KunAnonym, Array.Empty<string>(), true, "Innlogging"),

        new("anon-bankid-feiler",
            "BankID-innloggingen virker ikke, eller jeg får en feilmelding",
            "<p>Sjekk først at du bruker riktig BankID-metode (f.eks. BankID-appen på telefonen, ikke en kodebrikke, hvis det er det du normalt bruker). Prøv gjerne på nytt — midlertidige nettverksfeil forekommer. Vedvarer problemet, ta kontakt med klinikken din, eller bruk tilbakemeldingsknappen nederst til høyre på siden så ser vi på saken.</p>",
            KunAnonym, Array.Empty<string>(), true, "Innlogging"),

        new("anon-hva-er-testrom",
            "Hva er TestRom?",
            "<p>TestRom er et system for å gjennomføre psykologiske tester digitalt. En behandler tildeler deg en eller flere tester, du svarer i eget tempo på telefon eller PC, og behandleren din får en ferdig skåret rapport. Alt behandles som helseopplysninger og lagres sikkert.</p>",
            KunAnonym, Array.Empty<string>(), true, "Generelt"),

        new("anon-personvern",
            "Er opplysningene mine trygge?",
            "<p>Ja. Personnummer og andre sensitive opplysninger lagres kryptert, og kun din behandler (og eventuelt administratoren på klinikken) har tilgang til svarene og rapportene dine. Se <a href=\"/personvern\">personvernsiden</a> vår for mer informasjon.</p>",
            KunAnonym, new[] { "/personvern" }, false, "Generelt"),

        // ---------- Pasient ----------
        new("pas-fylle-ut-test",
            "Hvordan fyller jeg ut en test?",
            "<p>Gå til <strong>Min side</strong> for å se hvilke tester du har fått tildelt. Trykk på en test for å starte. Du svarer side for side, og fremdriften lagres automatisk underveis — du kan trygt lukke nettleseren og fortsette senere. Når du er ferdig, trykker du <strong>Fullfør</strong>.</p>",
            KunPasient, new[] { "/Pasientportal/Tester/Fyll", "/Pasientportal/MinSide" }, true, "Fylle ut tester"),

        new("pas-mistet-svar",
            "Jeg kom bort fra testen midt i — mister jeg svarene mine?",
            "<p>Nei. Svarene dine lagres fortløpende mens du fyller ut, side for side. Gå tilbake til <strong>Min side</strong> og åpne testen på nytt — du fortsetter der du slapp.</p>",
            KunPasient, Array.Empty<string>(), true, "Fylle ut tester"),

        new("pas-hvorfor-ikke-se-rapport",
            "Hvorfor kan jeg ikke se resultatet mitt med én gang?",
            "<p>Behandleren din må gå gjennom og godkjenne rapporten før du får se den — dette sikrer at resultatet blir forklart og tolket riktig. Du får beskjed når rapporten er klar.</p>",
            KunPasient, new[] { "/Pasientportal/Tester/Rapport" }, true, "Rapporter"),

        new("pas-betaling",
            "Jeg skal betale for en test — hvordan gjør jeg det?",
            "<p>Noen tester koster noe å gjennomføre. Du blir i så fall sendt videre til en betalingsside (Vipps eller kort) før du kan starte testen. Beløpet vises tydelig før du betaler, og testen åpnes automatisk rett etter en vellykket betaling.</p>",
            KunPasient, new[] { "/Pasientportal/Tester/Betal" }, true, "Betaling"),

        new("pas-fullfor-profil",
            "Hva betyr \"Fullfør profilen din\"?",
            "<p>Hvis du meldte deg inn via en QR-kode eller en enkel lenke, har vi foreløpig bare registrert mobilnummer eller e-post. \"Fullfør profilen din\" lar deg legge til navn, kjønn og personnummer — dette trengs for enkelte tester (f.eks. de med kjønnsspesifikk normering), men er ikke påkrevd for å prøve systemet.</p>",
            KunPasient, new[] { "/PasientRegistrering/FullforProfil" }, true, "Min profil"),

        new("pas-varslingspreferanse",
            "Kan jeg velge om jeg får SMS eller e-post?",
            "<p>Ja — når du registrerer deg kan du velge om du vil varsles på SMS, e-post, eller begge deler. Mangler du kontaktinfo for det du valgte, bruker vi automatisk det du faktisk har oppgitt.</p>",
            KunPasient, Array.Empty<string>(), false, "Min profil"),

        new("pas-program-pause",
            "Jeg er med i et program og får stadig nye tester — kan jeg pause eller melde meg ut?",
            "<p>Ja. På utfyllingssiden for en test som er en del av et program, finner du en knapp nederst for å <strong>pause eller melde deg ut</strong>. <strong>Pause</strong> stopper fremtidige utsendinger midlertidig til du (eller behandleren din) starter det opp igjen. <strong>Meld meg ut</strong> stopper det for godt. Testen du eventuelt er midt i akkurat nå, blir ikke påvirket uansett hva du velger.</p>",
            KunPasient, new[] { "/Pasientportal/Tester/Fyll" }, false, "Fylle ut tester"),

        // ---------- Behandler ----------
        new("beh-tildele-test",
            "Hvordan tildeler jeg en test til en pasient?",
            "<p>Gå til <strong>Tildel tester</strong> i menyen. I steg 1 velger du én eller flere pasienter, i steg 2 velger du én eller flere tester fra kategori-treet (du kan søke i testnavnet). Noen tester har et honorar du kan justere i et eget steg før du sender. Du kan sende tildelingen med én gang, eller planlegge den til et senere tidspunkt.</p>",
            KunBehandler, new[] { "/Behandlerportal/Tildel" }, true, "Tildele tester"),

        new("beh-honorar",
            "Hva er \"Sett honorar\" for noe?",
            "<p>For tester som koster penger for pasienten, kan du som behandler sette ditt eget honorar innenfor et fastsatt intervall. Dette vises i en egen dialog rett før du bekrefter utsendingen, og gjelder kun de valgte testene som faktisk har prising satt opp.</p>",
            KunBehandler, new[] { "/Behandlerportal/Tildel" }, false, "Tildele tester"),

        new("beh-ikoner-tildel",
            "Hva betyr ikonene ved siden av testnavnene?",
            "<p><strong>+</strong> betyr at pasienten kan belastes for denne testen. <strong>$</strong> betyr at testen koster praksisen noe per gjennomføring (f.eks. en lisensavgift). Et stetoskop-ikon med understreket navn betyr at testen fylles ut av deg som behandler, ikke av pasienten.</p>",
            KunBehandler, new[] { "/Behandlerportal/Tildel" }, true, "Tildele tester"),

        new("beh-godkjenne-rapport",
            "Hvorfor må jeg godkjenne en rapport før pasienten ser den?",
            "<p>Dette sikrer at du har sett resultatet og kan følge opp riktig før pasienten får det presentert alene. Gå til rapporten, se gjennom resultatet, og trykk <strong>Godkjenn</strong>. Du kan også velge <strong>Forkast og send på nytt</strong> hvis noe gikk galt under utfyllingen.</p>",
            KunBehandler, new[] { "/Behandlerportal/Pasienter/Rapport" }, true, "Rapporter"),

        new("beh-grupper-qr",
            "Hvordan bruker jeg grupper og QR-registrering?",
            "<p>Opprett en gruppe under <strong>Grupper</strong> og velg hvilke tester som automatisk skal tildeles nye medlemmer. Hver gruppe får en egen QR-kode (finnes på gruppens redigeringsside) — pasienter som skanner den, registrerer seg selv og sendes rett til testen. Du har også din egen personlige QR-kode på Min side, for pasienter som ikke skal inn i noen bestemt gruppe.</p>",
            KunBehandler, new[] { "/Behandlerportal/Grupper" }, true, "Grupper"),

        new("beh-provedata",
            "Hva er \"prøvedata\" / \"prøv systemet\"-pasienter?",
            "<p>Pasienter som registrerer seg uten personnummer (f.eks. på et foredrag eller en stand) regnes som \"prøv systemet\". De får testen gratis uansett pris, og rapporten godkjennes automatisk. Du kan slette all prøvedata for en gruppe med ett klikk fra gruppens redigeringsside, uten at det påvirker ekte pasienter.</p>",
            KunBehandler, new[] { "/Behandlerportal/Grupper" }, true, "Grupper"),

        new("beh-aggregert-rapport",
            "Kan jeg se resultater for en hel gruppe samlet?",
            "<p>Ja — gå inn på gruppen og velg <strong>Aggregert rapport</strong> for en oversikt på tvers av alle besvarelser (gjennomsnitt, median, fordeling). Du kan også generere en navngitt rapport for én bestemt test i gruppen med et valgt datointervall.</p>",
            KunBehandler, new[] { "/Behandlerportal/Grupper" }, false, "Grupper"),

        new("beh-klinikertest",
            "Hvordan fyller jeg ut en test som behandler (ikke pasienten)?",
            "<p>Enkelte tester (merket med et stetoskop-ikon) er ment å fylles ut av deg basert på en klinisk samtale, ikke av pasienten selv. Disse dukker opp som en oppgave på Min side, og du fyller dem ut side for side — med en egen kommentarboks per spørsmål hvis du vil notere noe underveis.</p>",
            KunBehandler, new[] { "/Behandlerportal/Pasienter/FyllForPasient" }, true, "Fylle ut tester"),

        new("beh-hpr-frist",
            "Hva betyr HPR-fristen jeg ser på Min side?",
            "<p>Nye behandlere får en midlertidig prøveperiode før HPR-nummeret er godkjent av en administrator. Min side varsler deg (og administratoren) når fristen nærmer seg eller er utløpt, slik at godkjenningen ikke blir glemt.</p>",
            KunBehandler, new[] { "/Behandlerportal/MinSide" }, false, "Konto"),

        new("beh-invitere-kollega",
            "Hvordan inviterer jeg en kollega?",
            "<p>Trykk <strong>Inviter kollega</strong> i menyen, fyll inn kontaktinformasjonen, og en invitasjonslenke sendes automatisk. Kollegaen din fullfører selv resten av registreringen og logger deretter inn med BankID på vanlig måte.</p>",
            KunBehandler, new[] { "/Behandlerportal/Behandlere/Inviter" }, false, "Konto"),

        // ---------- Behandler: Hjemmeoppgaver og programmer (2026-10-05/06) ----------
        new("beh-hjemmeoppgave-lage",
            "Hvordan lager jeg min egen hjemmeoppgave?",
            "<p>Gå til <strong>Hjemmeoppgaver</strong> og trykk <strong>+ Opprett</strong>. Legg til ett eller flere ledd med <strong>+ Legg til ledd</strong> — hvert ledd har et spørsmål/tekst, en valgfri instruksjon, og en svartype. Dra i håndtaket (⠿) til venstre for et ledd for å endre rekkefølgen, og trykk <strong>Minimer</strong> for å få bedre oversikt når du har mange ledd. Marker et ledd som <strong>Påkrevd</strong> hvis pasienten må svare før de kan levere inn.</p>",
            KunBehandler, new[] { "/Behandlerportal/Hjemmeoppgaver/Rediger" }, true, "Hjemmeoppgaver"),

        new("beh-hjemmeoppgave-svartyper",
            "Hva betyr de ulike svartypene i hjemmeoppgave-editoren?",
            "<p><strong>Likert-skala:</strong> pasienten velger ett av flere faste alternativer du selv lister opp (f.eks. «Aldri» til «Alltid»). <strong>Visuell Analog Skala (VAS):</strong> en glidebryter mellom to ytterpunkter du navngir. <strong>Ja/Nei:</strong> to faste knapper, ingenting å sette opp. <strong>Fritekst:</strong> et fritt tekstsvar. <strong>Bilde:</strong> rent visningsinnhold du selv legger inn (f.eks. en illustrasjon) — pasienten svarer ikke på dette, og du kan legge til en valgfri lenke (f.eks. til en video) som vises under bildet. <strong>Lenke (URL):</strong> pasienten skriver selv inn en lenke som sitt svar.</p>",
            KunBehandler, new[] { "/Behandlerportal/Hjemmeoppgaver/Rediger" }, false, "Hjemmeoppgaver"),

        new("beh-hjemmeoppgave-dele",
            "Kan jeg dele hjemmeoppgaven min med andre behandlere?",
            "<p>Ja — fra <strong>Hjemmeoppgaver</strong>-listen kan du dele en egen hjemmeoppgave med alle behandlere i systemet, eller kun med kollegene i din egen klinikk (hvis du har en). Andre behandlere kan <strong>like</strong> den for å få en referanse i sin egen «Personlig»-fane, og lage sin egen redigerbare kopi derfra — originalen din påvirkes aldri av dette.</p>",
            KunBehandler, new[] { "/Behandlerportal/Hjemmeoppgaver" }, false, "Hjemmeoppgaver"),

        new("beh-program-lage",
            "Hvordan lager jeg et program?",
            "<p>Gå til <strong>Programmer</strong> og trykk <strong>+ Nytt program</strong>. Sett først ukedag og klokkeslett for når programmet skal starte — dette blir «Dag 0» i kalenderen under. Klikk deretter på en hvilken som helst dag i kalenderen for å legge til en «drop»: et tidsvindu (tester sendes ut på et tilfeldig tidspunkt innenfor vinduet) og hvilke tester/hjemmeoppgaver som skal inngå. En dag med en drop får en grønn ramme og viser antall tester. Du kan ha så mange drops du vil, spredt over flere uker.</p>",
            KunBehandler, new[] { "/Behandlerportal/Programmer/Rediger" }, true, "Hjemmeoppgaver og programmer"),

        new("beh-program-tildele",
            "Hvordan tildeler jeg et program til en pasient?",
            "<p>Du kan enten trykke <strong>Tildel</strong> direkte på programmet i <strong>Programmer</strong>-listen (for én pasient eller en hel gruppe, der alle medlemmene starter samme kalenderdag), eller velge programmet i «Programmer»-seksjonen i den vanlige <strong>Tildel tester</strong>-flyten sammen med vanlige tester — begge deler sendes da ut i én og samme handling. Kun den aller første testen i det aller første drop-et kan noensinne koste pasienten noe; resten av programmet er alltid gratis.</p>",
            KunBehandler, new[] { "/Behandlerportal/Programmer/Tildel", "/Behandlerportal/Tildel" }, true, "Hjemmeoppgaver og programmer"),

        new("beh-program-kjorende",
            "Hvordan følger jeg opp et program som allerede er i gang?",
            "<p>Fanen <strong>Kjørende</strong> på Programmer-siden viser alle aktive programdeltakelser du selv har tildelt, med antall gjenstående drops og antall deltakere. Du kan <strong>pause</strong> (stopper fremtidige drops midlertidig) eller <strong>fjerne</strong> (melder ut for godt) en hel gruppe- eller enkeltpasient-tildeling samlet. En pasient kan også pause eller melde seg selv ut fra utfyllingssiden — du får da en egen oppgave på Min side om det.</p>",
            KunBehandler, new[] { "/Behandlerportal/Programmer" }, false, "Hjemmeoppgaver og programmer"),

        // ---------- Behandler/Admin: "Tildelte tester"-fane + automatisk påminnelse (2026-10-11) ----------
        new("beh-tildelte-tester-fane",
            "Hvordan ser jeg hvilke tester som ikke er besvart ennå?",
            "<p>Fanen <strong>Tildelte tester — ikke fullført</strong> (på begge steg i Tildel-flyten) lister alle tildelinger som ikke er fullført, med pasient, test, tildelingsdato og status. Du kan trykke <strong>Påminn</strong> på en enkelt rad for å sende en ny SMS/e-post med en gang, eller <strong>Slett</strong> for å fjerne tildelingen helt. Velg flere rader med avkrysningsboksene for å påminne eller slette flere samtidig.</p>",
            BehandlerOgAdmin, new[] { "/Behandlerportal/Tildel", "/Admin/Tildel" }, true, "Tildele tester"),

        new("beh-automatisk-paaminnelse",
            "Hva betyr \"Automatisk påminnelse etter 3 dager\"?",
            "<p>Når du slår på denne avkrysningsboksen — i oppsummeringsdialogen når du tildeler tester, eller på et programs redigeringsside — sender systemet automatisk én påminnelse (SMS/e-post) hvis pasienten ikke har besvart testen innen 3 dager. Det skjer kun én gang per tildeling, og aldri for en test som skal fylles ut av deg som behandler (ikke pasienten).</p>",
            BehandlerOgAdmin, new[] { "/Behandlerportal/Tildel", "/Admin/Tildel", "/Behandlerportal/Programmer/Rediger" }, false, "Tildele tester"),

        // ---------- Behandler: Min klinikk (2026-10-11) ----------
        new("beh-min-klinikk",
            "Hva er \"Min klinikk\"?",
            "<p>Hvis du er del av en klinikk (flere behandlere under samme virksomhet), finner du <strong>Min klinikk</strong> i menyen. Alle i klinikken ser klinikkens navn/kontaktinfo og en liste over kollegene sine der. Er du satt som klinikk-administrator, ser du i tillegg flere knapper for å administrere klinikken — prising, utbetaling, automatisk utsendelse og redigering av klinikkens opplysninger.</p>",
            KunBehandler, new[] { "/Behandlerportal/MinPartner" }, true, "Min klinikk"),

        new("beh-klinikk-kolleger",
            "Hvordan ser jeg hvem andre jobber i klinikken min?",
            "<p>Trykk <strong>Behandlere i min klinikk</strong> under Min klinikk. Er du klinikk-administrator kan du også invitere en ny kollega til klinikken, eller fjerne en som ikke lenger skal være med, fra samme side.</p>",
            KunBehandler, new[] { "/Behandlerportal/MinPartner/Kolleger" }, false, "Min klinikk"),

        new("beh-klinikk-pasienter-knapp",
            "Hva er forskjellen på \"Pasienter\" og \"Pasienter i min klinikk\"?",
            "<p>Den vanlige <strong>Pasienter</strong>-lenken i menyen viser alle klinikkens pasienter hvis du er klinikk-administrator (ellers kun dine egne). Knappen <strong>Pasienter i min klinikk</strong> under Min klinikk viser derimot alltid KUN dine egne pasienter, uansett rolle — navnet er litt misvisende, men det er en bevisst snarvei til din egen pasientliste fra klinikk-siden.</p>",
            KunBehandler, new[] { "/Behandlerportal/Pasienter", "/Behandlerportal/MinPartner/Behandlere" }, false, "Min klinikk"),

        new("beh-klinikk-bytt-behandler",
            "Kan jeg flytte flere pasienter til en kollega samtidig?",
            "<p>Som klinikk-administrator kan du velge flere pasienter i listen (avkrysningsboksene) og trykke <strong>Bytt behandler for valgte</strong> for å flytte dem alle til en annen behandler i klinikken i én handling.</p>",
            KunBehandler, new[] { "/Behandlerportal/Pasienter" }, false, "Min klinikk"),

        new("beh-klinikk-automatisk-utsendelse",
            "Kan klinikken sende ut en fast test automatisk til alle nye pasienter?",
            "<p>Ja — under <strong>Automatisk utsendelse</strong> (klinikk-administrator) velger du hvilke tester/hjemmeoppgaver som skal tildeles automatisk så snart en ny pasient legges til i klinikken, og/eller når en pasient flyttes til \"Avsluttet\". Gjelder for alle behandlere i klinikken, ikke bare deg selv.</p>",
            KunBehandler, new[] { "/Behandlerportal/MinPartner/AutomatiskUtsendelse" }, false, "Min klinikk"),

        new("beh-klinikk-prising",
            "Hvordan setter klinikken sin egen pris på en test?",
            "<p>Under <strong>Tester tilgjengelig for oss / Klinikkprising</strong> (klinikk-administrator) setter du klinikkens egen andel per test — den klemmes alltid opp til minst det Superadmin har satt som gulv. Du kan også be om tilgang til flere tester, eller be om å fjerne en test klinikken ikke lenger ønsker tilgang til (<strong>ikonknappen</strong> ved siden av testen) — begge sendes til administrator for godkjenning.</p>",
            KunBehandler, new[] { "/Behandlerportal/MinPartner/Prising" }, false, "Min klinikk"),

        new("beh-klinikk-rediger",
            "Hvordan endrer jeg klinikkens navn eller kontaktinformasjon?",
            "<p>Trykk <strong>Editer felter</strong> på Min klinikk-siden (klinikk-administrator). Du kan endre navn, kontaktperson og kontakt-e-post/mobil selv — hvorvidt klinikken har et aktivt abonnement er fortsatt kun noe Superadmin kan endre.</p>",
            KunBehandler, new[] { "/Behandlerportal/MinPartner/Rediger" }, false, "Min klinikk"),

        // ---------- Behandler: Utbetaling (2026-10-10/11) ----------
        new("beh-min-utbetaling",
            "Hvordan får jeg utbetalt honoraret mitt?",
            "<p>Gå til <strong>Utbetaling</strong> og trykk <strong>Koble til utbetalingskonto</strong> under fanen \"Tilkobling\" — du fullfører registreringen av bankkonto hos Stripe (vi lagrer aldri selve kontonummeret). Når kontoen er tilkoblet, betales honoraret ditt automatisk ut én gang i måneden. Under fanen \"Honorarutskrift\" kan du se og skrive ut et eget inntektsbilag for en valgt måned.</p>",
            KunBehandler, new[] { "/Behandlerportal/Utbetaling" }, true, "Utbetaling"),

        new("beh-klinikk-utbetaling",
            "Hvordan får klinikken utbetalt sin andel?",
            "<p>Klinikk-administrator kobler til en egen bankkonto under <strong>Klinikkutbetaling</strong> → \"Tilkobling\", på samme måte som en behandler kobler til sin egen. Fanen \"Partneroppgjør\" viser klinikkens andel for en valgt måned, gruppert per behandler.</p>",
            KunBehandler, new[] { "/Behandlerportal/MinPartner/Utbetaling" }, false, "Utbetaling"),

        // ---------- Admin ----------
        new("adm-legge-til-behandler",
            "Hvordan legger jeg til en ny behandler?",
            "<p>Gå til <strong>Behandlere</strong> og trykk <strong>Inviter</strong>. Behandleren fullfører selv resten av registreringen (avtale, kontaktopplysninger) via lenken de mottar, og logger deretter inn med BankID.</p>",
            KunAdmin, new[] { "/Admin/Behandlere" }, true, "Administrasjon"),

        new("adm-hpr-godkjenning",
            "Hvordan godkjenner jeg HPR-nummeret til en behandler?",
            "<p>Behandlere med utløpt eller ventende HPR-godkjenning vises på Min side. Åpne behandlerens egen side for å godkjenne HPR-nummeret manuelt når du har verifisert det.</p>",
            KunAdmin, new[] { "/Admin/Behandlere", "/Admin/MinSide" }, true, "Administrasjon"),

        new("adm-partnere",
            "Hva er klinikksystemet?",
            "<p>En klinikk er en ekstern virksomhet med egen, kuratert tilgang til et utvalg tester. Som Superadmin oppretter du klinikken og bestemmer hvilke tester de får tilgang til. En klinikk-administrator (en behandler med en egen rettighet) kan deretter sette sin egen andel av prisen for hver test, innenfor en nedre grense du setter.</p>",
            KunAdmin, new[] { "/Admin/Partnere" }, false, "Klinikker og prising"),

        new("adm-prising",
            "Hvordan setter jeg pris på en test?",
            "<p>Gå til <strong>Prising</strong> under Tester. Der setter du en minste- og største pris pasienten kan betale, og et typisk behandlerhonorar som brukes som forslag når en behandler tildeler testen.</p>",
            KunAdmin, new[] { "/Admin/Tester/Prising" }, true, "Klinikker og prising"),

        new("adm-okonomi",
            "Hvor finner jeg økonomiske rapporter?",
            "<p>Under <strong>Økonomi</strong> finner du en oversikt over gjennomførte betalinger, inkludert plattform-, klinikk- og behandlerandeler. Fanen <strong>Periodisert oppgjør</strong> viser alle transaksjoner for en valgt måned, fordelt per tildeling (pasientpris/honorar/plattform-/klinikkandel) — nyttig for bokføring.</p>",
            KunAdmin, new[] { "/Admin/Okonomi" }, false, "Klinikker og prising"),

        new("adm-regenerer-tester",
            "Hva gjør \"Regenerer innebygde tester\"?",
            "<p>Denne knappen oppretter innebygde tester (som WHO-5, PHQ-9 osv.) som mangler i databasen — den rører ALDRI eksisterende tester eller tidligere svar. Nyttig etter at en ny innebygd test er lagt til i systemet.</p>",
            KunAdmin, new[] { "/Admin/Tester" }, false, "Administrasjon"),

        new("adm-tilbakemeldinger",
            "Hvor ser jeg tilbakemeldinger fra brukerne?",
            "<p>Under <strong>Tilbakemeldinger</strong> finner du alt som er sendt inn via tilbakemeldingsknappen nederst til høyre på hver side, inkludert automatisk vedlagte skjermbilder og tekniske feilmeldinger.</p>",
            KunAdmin, new[] { "/Admin/Tilbakemeldinger" }, false, "Administrasjon"),

        new("adm-kjorende-programmer",
            "Hva viser \"Kjørende programmer\"?",
            "<p>En samlet oversikt over ALLE aktive programdeltakelser i systemet, uansett hvilken behandler som tildelte dem — vist som «(behandlernavn) Programnavn» med antall gjenstående drops og antall deltakere. Du kan pause eller fjerne (melde ut for godt) en hel tildeling herfra, på samme måte som en behandler kan for sine egne.</p>",
            KunAdmin, new[] { "/Admin/Programmer/Kjorende" }, false, "Administrasjon"),

        // ---------- Admin: Driftsbrytere og utbetaling (2026-10-11) ----------
        new("adm-driftsbrytere",
            "Hva er \"Driftsbrytere\" på Min side?",
            "<p>Som Superadmin/Utvikler kan du slå SMS-utsending, pasientbetaling (Vipps/Stripe) og Stripe-utbetaling av/på for dette miljøet, med umiddelbar virkning og uten omstart. \"Mock\" gjør ingen ekte utsending/nettverkskall/pengebevegelse — nyttig når du vil teste trygt uten å påvirke ekte leverandører.</p>",
            KunAdmin, new[] { "/Admin/MinSide" }, false, "Administrasjon"),

        new("adm-utbetalinger",
            "Hvordan godkjenner jeg utbetaling av honorar/klinikkandel?",
            "<p>Under <strong>Utbetalinger</strong> genereres et samlet månedlig oppgjør automatisk tidlig i hver måned. Åpne <strong>Se detaljer</strong> på en periode og trykk <strong>Godkjenn og betal ut</strong> for å utløse de ekte Stripe-overføringene — dette flytter ekte penger og kan ikke angres. En enkelt mislykket linje blokkerer ikke resten av batchen, og kan forsøkes på nytt eller avvises for seg selv.</p>",
            KunAdmin, new[] { "/Admin/Utbetalinger" }, true, "Klinikker og prising"),

        // ---------- Admin: Driftsstatus-dashboard (2026-10-13) ----------
        new("adm-driftsstatus",
            "Hva viser \"Driftsstatus\"?",
            "<p>En rask oversikt over om database, Vipps, Stripe, SMS-kreditt (Vonage), e-post og BankID (Idura) faktisk fungerer akkurat nå — grønt/gult/rødt lys per funksjon, pluss om noen Stripe Connect-utbetalinger står som feilet. Grått betyr at funksjonen ikke er konfigurert i dette miljøet (f.eks. lokal dev uten ekte nøkler) — ikke nødvendigvis et problem. Dekker IKKE Azure-ressurshelse (CPU/minne/disk) eller om selve siden er nede. Siden for SMS-varsel ved kritisk feil (Superadmin/Utvikler) er foreløpig kun lagring av oppsett — ingen SMS sendes automatisk ennå.</p>",
            KunAdmin, new[] { "/Admin/Driftsstatus" }, false, "Administrasjon"),
    };
}
