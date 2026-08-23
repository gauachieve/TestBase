# Prosjektbeskrivelse Online Testesystem (originaldokument)

*Dette er den originale prosjektbeskrivelsen bruker lastet opp som Word-dokument 2026-08-18. Tatt vare på ordrett som referanse — spesielt Del 2–4 og testdefinisjonen under er ikke designet i detalj ennå (kommer i fase 2–6) og må leses herfra, ikke fra hukommelse.*

## Bakgrunn for prosjektet

Evaluer om dette er et mulig prosjekt for deg, eller om man trenger å dele det opp for å lette gjennomføring. Foreslå metodikk og la oss følge den.

Jeg er privatpraktiserende autorisert psykologspesialist her i Norge. Jeg har en rekke pasienter privat. Jeg har også et kontorfelleskap med en rekke andre behandlere som alle skal ha tilgang til dette. Jeg ønsker å lage et system for online skal teste pasienter for en rekke egenskaper. Disse testene er ikke utviklet av meg, men reglene for skåring er unike per test. Det skal være høye krav til innlevering og oppbevaring av resultatene. Prosjektet inneholder 4 underprosjekter: 1)Utviklingssystem 2) Administrasjon av behandlere, generering av rapporter og tester. 3) Administrasjon fra hver behandler av pasienter og tillordning av tester, uthenting av rapporter osv. 4) Pålogging av pasienter og test-taking samt sletting av egne data. Det er høye krav til lagring. Det skal være støtte for betaling.

## Prosjektbeskrivelse i detalj

### Overordnet system

All brukergrenseflate skal være i HTML. Alle skal connecte mot https://www.ggpsykolog.com/testbase/[forskjellig]. Alt kjører på en windows server 2016. Det er IIS som er webserver. Alle sider skal virke på både pc og mobil. Mobil i stående format, pc i liggende. All data skal lagres i min MySQL server. Hvis det er enklere kan du legge opp til å bruke 3. parts web-side generering, det er din vurdering. All data skal backup'es til en skyløsning som er sikker utenfor min server. Det skal være automatisk backup og restore system. All data skal enkrypteres sikkert i henhold til lagring av helseopplysninger i Norge — sjekk lovkrav og praksis. Systemet må kunne støtte pålogging med BankID, pålogging med tofaktor løsning, Innehenting av betalingskrav med VIPPS, sending av SMS'er, generering av fakturarer, sending av emails.

Jeg innstallerer og kjøper løsninger som er nødvendig.

Spør spørsmål når det er noe du lurer på før du genrerer løsning.

Når du utfører oppgaven, generer et dokument som jeg kan bruke for å regenrere løsningen med alle beslutninger tatt.

Versjoner alle deler. Lag et automatisert utviklingsmiljø og system.

Tenk at dette systemet skal kunne utvikle seg over lang tid. Kanskje det er begrensninger i dine evner? Skal dette lages i mange biter for mange sesjoner, eller skal du gjøre alt i en sesjon?

**Merk (2026-08-19/20):** Kravet om egen Windows Server 2016/IIS for produksjon er siden revidert av bruker — produksjon flyttes til Azure. Se `docs/beslutningslogg.md`. Resten av dette dokumentet er ellers uendret som kravgrunnlag.

### Del 1 — Utviklingsmiljø

Jeg ønsker et system for utvikling av resten av løsningen som gjør at det er lett å oppgradere og teste. Vanligvis må jeg laste ned zip fil. Unzip for hånd. Logge på ekstern datamaskin med rdp. Kopiere filer til forskjellige mapper. Jeg ønsker et system hvor jeg kun trenger å laste ned zip-fil, så kjøre en bat-fil el så kobler skriptet seg til den andre maskinen, kopierer over, lagrer i riktig versjons-system, restarter evt servere og refresher browser. Så ikke så mye manuelle prosesser. Kan trykke f5 i browsern hvis det er vrient.

Jeg ønsker også at det virker selv om jeg sitter et annet sted enn hjemme, så ekstern pålogging og aktivering. Jeg har også versjonskontrollsystem. Instruer meg til hvordan det kan brukes til denne utviklingen. Hvordan gjøre det enklest for meg, raskest men allikevel sikkert ift versjonen.

*Status: Ferdig (lokal del) 2026-08-20 — se `docs/beslutningslogg.md` og `docs/del1-utviklingsmiljo-plan.md`. Original plan om VPN/RDP til egen server er skrotet etter hosting-pivot til Azure.*

### Del 2 — Administrativt system

- All administrasjonen foregår via websider.
- Administrator må logge på med norsk BankID. Når logget på, lagre i nettleser som lovlig pålogging med gitt lovlig varighet, med fokus på brukervennlighet og sikkerhet. Lag støtte for 2 faktor pålogging etter det, enkleste er sms støtte.
- En administrator er definert som: adminid*, mobilnr*, email*, fullt navn*, personnummer*, HPRnr* og passord. Hvis administrator har satt passord, er produktet i utviklingsmodus, og du logger på med adminid og passord, hvis ikke bankid og tofaktor.
  - Når produktet er i utviklingsmodus, kaller vi admin for utvikler.
  - Som utvikler skal man kunne enkelt bytte hva slags modus man er i mellom admin (uten/med utviklerstatus), behandler og pasient.
- Brukerstyring
  - Det skal være brukerstyring for de forskjellige adminstrative systemene. Brukerstyringen kan gjøres tilsvarende tilgangen til MySQL, men sett opp et system som passer med resten.
- Administrator skal kunne gjøre alle tingene en pasient kan gjøre, alle tingene en tester kan gjøre.
- Administrator skal kunne legge til nye behandlere, endre data til dem, slette og arkivere andre behandlere, administratorer og gjøre det samme som behandlere kan ift pasienten.
  - En behandler er definert som en bruker hvor følgende data må være lagt til av administrator, eller selv-innlagt av tester selv. Administrator kan sende ut en invitasjon til en behandler for å gjøre det må vedkommende legge inn mobilnummer eller email som skal generere en email eller sms link.
- Administrator skal kunne legge til ny psykologiske tester.
  - En test er en egen webside, under samme domene, som bruker over nevnte system. Alle testene er ikke definert nå. Jeg skal senere lage en prosjekt-beskrivelse for en test som du skal lage støtte for.
    - Alle tester må kunne registrere seg selv i en liste etter at hovedsystemet er laget.
    - Alle tester må kunne generere sin egen rapport (mer om dette senere).
    - Alle tester må kunne legges til en gitt og godt gjennomtenkt flate systemisk.
- Administrator skal kunne starte en backup, og rulle tilbake en tidligere backup.
- Etter hvert vil vi må kunne støtte organisasjoner, det vil si en samling av behandlere.
- Administrator skal kunne sette prisen på en test. Den velges å sendes ut av behandler. Det er pasienten som betaler for å gjennomføre testen. Prisen kan være 0 kr.
- Administrator skal kunne sette, per test, hva den koster for bruker å gjennomføre, hvor mye skal gå til test-systemet og hvor mye som skal gå til behandler. Her må man etter hvert støtte (men ikke nå) organisasjoner.
- Administrator kan lage organisasjoner, men det skal ikke støttes pt. En org adminstrator skal kunne ha mulighet for å gjøre noe av adm. tjenester (etter hvert).
- For hver uke, hver måned, hvert kvartal og hvert år må systemet kunne genrere økonomiske rapporter. Den skal vise hvor mye som er genert til vipps for tester, hvilke behandlere som har hvilke utestående krav. Det skal kunne brukes for enkel månedlig økonomiføring i regnskapsprogram.
- Hadde vært fint med støtte for halv-automatisk generering av å legge inn i økonomiføring, hvis det er greit.
- Hvis det er mulig å få til kjøring av halv-automatisk kjøring av utbetaling fra bank er det også fint.
- Administrator skal kunne velge hvilke behandlere skal ha tilgang til hvilke tester.
- Administrator skal kunne fryse, slette og editere behandlere og pasienter.

*Status: Ikke startet — dette er fase 2, neste steg.*

### Del 3 — Behandlersystem

- En behandler er en godkjent helsearbeider. De må logge på med norsk bankID. Samme tofaktor etterpå. (Fornyes en gang i måneden? Kvartalet? Vet ikke)
- Når de mottar invitasjon, må de registrere følgende selv: Fornavn*, etternavn*, personnummer*, mobilnummer*, HPRNummer*, kontonummer for innbetaling av godtgjørelse*, arbeidsaddresse, tittel. De uten * er frivillig.
- En behandler må godkjenne en bruker og lisens avtale for å kunne registrere seg. Du må genrere den ut i fra vanlige regler og hva de må godta normalt. Det må være et system for at de må regodkjenne den hver gang den endrer seg.
- Det må være et system for automatisk godkjenne email addressen og mobilnummer som relle.
- Det må være et system for å stoppe botter og spammere.
- HPR nummer må generere en automatisk mail til alle administratorer om å sjekke HPR nummer til behandler i oppslag. Hvis behandlers HPR nummer ikke er godkjent, vil å legge til pasienter ikke gå. Første gangen en behandler registrerer seg får de en uke hvor alt virker før administrasjons godkjenning av bruker er på plass.
- En behandlers viktigste funksjon er legge til pasienter og legge til tester.
- Når du legger til en pasient, trenger du personnr, mobil og mail. Send invitasjon til en av dem. Pasienten må registrere seg før de kan svare på tester.
- En behandler kan genrere en «pasient invitasjon» som sendes til sms eller email. Derfor må de ha riktig telefonnummer eller email for generere en.
- Når en pasient er registret får de sin egen side.
- Når en pasient har registrert seg (se under) vil de kunne sende ut tester. Det generer en sms/mail som har en link til pasientens-test-side.
- En test kan ha en frist for å starte å fylle inn og varighet for å fylle inn. Behandler kan bestemme å sende ut sms/email påminning for oppstart, varighet og be om kvittering for gjennomført og om det ikke ble gjennomført på tiden.
- De skal se på pasientens resultater og generere rapporter. Rapportene kan enten kopieres inn i journal, eller sendes i kopi til pasientens email. Å se på resultater er det samme som å generere rapporter.
- Noen tester skal sendes ut automatisk. Behandler skal sette hvordan (hyppighet, hvor, og generering av rapporter automatisk som sendes til behandler på sms/email).
- Behandlere må kunne flytte avsluttede pasienter til et arkiv. All data fra pasienten slettes automatisk etter 10 år.
- Behandlere må kunne flytte egne arkiverte pasienter tilbake til aktiv bruk.
- Behandlere kan også invitere andre behandler på samme måte som admin.
- Behandler skal kunne ha en oversikt over egne pasienter. Gå inn på hver av dem og se på hva de besvart, hva de har blitt invitert til. Når besvarelsen startet, når den ble avsluttet. Genere en rapport, en rekke rapporter eller alle i en stor.
- Behandler skal kunne generere rapport om hvor mye deres pasienter har generert tester, generert økonomi og forventet utbetaling.
- En behandler skal kunne genere en haug med pasienter i en «gruppe». De skal kunne generes med en komma-seperart liste med: gruppenavn, Navn, email, sms, pnr [return].

*Status: Ikke startet — fase 3.*

### Del 4 — Pasient

- Når en pasient har fått en invitasjon, står fritt til å registrere seg. I invitasjonen står hvem som sender den ut, hvem som er ansvarlig behandler (tittel, navn, og email til feedback).
  - I registrering må de fylle inn navn*, personnr*, mobil*, email*, biologisk kjønn ved fødsel*, kjønn (støtte også «annet» og «spesifiser»), addresse. (Nødvendig med *).
  - For å registrere seg må de godkjenne en bruker avtale. Den må du skrive. Sjekke lovlighet. Krysse av for å godt lagring av data. Forklare lovgivning og deres beskyttelse. Krysse av for å godta å kunne bli nødt til å akseptere å betale en mindre sum via vipps for å kunne fylle ut tester.
- Når en pasient får en test tilegnet seg, får de påminning hvis behandler har bestemt det. Dersom det er frist kan de få ny sms før fristen går ut som påminning.
- Før pasienten kan fylle inn en test, kan det hende de må betale for den. Da kan de velge betalingsløsning — for nå støtte VIPPS.
- På en eller annen måte må systemet få bekreftelse for betalt (vipps har garantert system) for å fortsette med innfylling.

*Status: Ikke startet — fase 4.*

### Definisjon av en test

En test består av følgende ting:

1. (Husk før utførelse av test — en innlogging med BankID på samme måte som registrering. Bruk samme innlogging.)
2. En test gis som et oppdrag til deg i form av eksempler vi har under.
3. En rekke ledd delt inn i sider.
   a. Det skal være rom for instruksjon per test, per side og per ledd.
   b. Hver side skal inneholde navn på test, %vis progresjon, Neste/forrige/ferdig/lagre av innfylt.
   c. En inndeling av hvilke ledd som tilhører en side.
   d. En belønningsside for gjennomføring.
4. En presentasjon av hvert ledd og metodikk for besvarelse (likert, visuell analog skala, ja/nei, fyll inn tekst etc).
5. Det er ikke nødvendig å lage et system for å generere tester uten å gå [gjennom hovedsystemet — setningen er ufullstendig i originaldokumentet].
6. Hver test skal kunne støtte lokalisering til mange språk.
7. En skåringsmetodikk som genererer noen variabler med en spesiell formel/metodikk.
8. Et rapportoppsett per besvarelse. Det kan være grafer, tekst eller noe. Viktig her er at det lages en metodikk for å definere rapporter og tester.
9. Et rapportoppsett sett over tid. Noen ganger skal den samme pasienten sjekkes mange ganger med forskjellig mellomrom, og tidsdefinerte grafer skal genereres.
10. Regler for automatisk rapportutsending.
11. Lag system for automatisk generering av lagring og oppsett av nye tester.

*Status: Ikke startet — testrammeverket er fase 4–5, sammen med det konkrete WHO-5-eksempelet under.*

### Test eksempel 1: WHO-5

Den første skal være WHO-5. Sjekk på nettet hvordan den ser ut. Sjekk på nettet hvordan den skåres. Sjekk på nettet hvordan den instrueres. Lag et forslag til rapport. Lag forslag til lagring, enkryptering, sql oppsett osv. Husk alltid å lage regenerering av tester.

Send automatisk rapport til behandler.

*Status: Ikke startet — fase 5. Ingen research på WHO-5 (utseende/skåring/instruksjon) er gjort ennå; dette må gjøres med websøk når fase 5 starter.*
