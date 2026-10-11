# Notat: lagring av pasientdata i Norge + EU-regulering av digital helseteknologi

**Status: research-/planleggingsnotat, IKKE juridisk rådgivning.** Skrevet av Claude Code
2026-10-11 som svar på punkt 2 i en brukerlevert `bugs_features.txt`-runde, til forberedelse av
ekte brukertesting med reelle pasienter. Må kvalitetssikres av jurist/DPO (samme forbehold som
`docs/compliance-dpia-utkast.md`) før noe av dette legges til grunn for en faktisk beslutning,
og spesielt før ekte pasientdata går i produksjon.

## 1. Hvilket regelverk gjelder for selve lagringen

Tre lag, alle allerede delvis adressert i prosjektets arkitektur:

1. **Personopplysningsloven/GDPR** — helseopplysninger er en særlig kategori (GDPR artikkel 9),
   krever eksplisitt behandlingsgrunnlag + forsterkede sikkerhetstiltak. Allerede reflektert i
   kryptering-i-hvile (DataProtection) og audit-logging (`IAuditLogger`) fra dag én, se
   `docs/compliance-dpia-utkast.md`.
2. **Pasientjournalloven** (lov 2014-06-20-42) + **pasientjournalforskriften** (2019-03-01-168) —
   gjelder direkte siden dette er en privatpraktiserende psykologspesialists journalføring.
   § 22 krever tekniske/organisatoriske tiltak tilpasset risiko (tilgangsstyring, logging,
   etterfølgende kontroll — alt allerede bygget); § 22 a krever logging av utlevering.
3. **Normen** (Helsedirektoratets norm for informasjonssikkerhet og personvern i helse- og
   omsorgssektoren) — ikke en egen lov, men den praktiske standarden tilsynet/bransjen legger til
   grunn. Har et eget kapittel om skytjenester som er direkte relevant her.

**Kilder:**
[Pasientjournalloven](https://lovdata.no/dokument/NL/lov/2014-06-20-42) ·
[Pasientjournalforskriften](https://lovdata.no/forskrift/2019-03-01-168) ·
[Dokumentasjonsplikt (Helsedirektoratet)](https://www.helsedirektoratet.no/rundskriv/helsepersonelloven-med-kommentarer/dokumentasjonsplikt) ·
[Normen: Skytjenester](https://www.helsedirektoratet.no/normen/medisinsk-utstyr-personvern-og-informasjonssikkerhet/sikkerhetstiltak/skytjenester) ·
[Normen: krav til overføringsgrunnlag utenfor EU/EØS](https://www.helsedirektoratet.no/normen/bruk-av-skytjenester-til-behandling-av-helse-og-personopplysninger/anskaffelser-ved-bruk-av-skytjenester/krav-til-overforingsgrunnlag-ved-bruk-av-databehandler-utenfor-eu-eos) ·
[Normen: databehandleravtale ved skytjenester](https://www.helsedirektoratet.no/normen/bruk-av-skytjenester-til-behandling-av-helse-og-personopplysninger/anskaffelser-ved-bruk-av-skytjenester/databehandleravtale-ved-bruk-av-skytjenester)

## 2. Må det stå fysisk i Norge? (Sweden Central-spørsmålet)

Dette er den direkte tekniske konsekvensen av testmiljøet som i dag kjører i **Sweden Central**
(se `docs/beslutningslogg.md` "Sky-deploy til Azure (azd)" — valgt fordi Norway East/West manglet
MySQL Flexible Server-kapasitet ved provisjonering).

**Funn:** Jeg fant INGEN eksplisitt lovkrav om at helseopplysninger må lagres fysisk i Norge.
Normen krever derimot:
- En **databehandleravtale** med skyleverandøren (Microsoft) som **eksplisitt navngir** landet/
  landene der lagringen faktisk skjer.
- En **konkret, dokumentert risikovurdering** dersom databehandleren/lagringsstedet er **utenfor
  EU/EØS** — man kan da IKKE automatisk legge til grunn at tredjelandet har et beskyttelsesnivå
  tilsvarende EU/EØS, og må vise til adekvansbeslutning/SCC/BCR eller tilsvarende.

**Sverige ER innenfor EU/EØS** — dette er den enkle, gode nyheten: Sweden Central trigger IKKE
den strenge tredjelands-vurderingen som f.eks. en amerikansk region ville gjort. Det gjenstående
kravet er administrativt, ikke et flytte-krav: få en **signert databehandleravtale med Microsoft
som eksplisitt navngir Sweden Central** som lagringssted — jeg har ikke funnet spor av at dette
faktisk er signert/dokumentert for dette prosjektet, kun antatt via standard Azure-vilkår. Dette
bør sjekkes/ordnes uavhengig av om man flytter region.

**Anbefaling:** IKKE en hastesak å flytte til Norway East/West. Det er en forbedring av
sikkerhets-/tillitsprofilen (Normen fremhever norsk lagring som et pluss for helseopplysninger),
men ikke, så vidt jeg kan dokumentere, et lovkrav. Revurder regional MySQL-kapasitet periodisk
(se eksisterende TODO i `CLAUDE.md`), men lar IKKE dette blokkere brukertesting i seg selv —
prioriter heller databehandleravtalen.

**Kilder:**
[Normen: krav til overføringsgrunnlag](https://www.helsedirektoratet.no/normen/bruk-av-skytjenester-til-behandling-av-helse-og-personopplysninger/anskaffelser-ved-bruk-av-skytjenester/krav-til-overforingsgrunnlag-ved-bruk-av-databehandler-utenfor-eu-eos) ·
[Helseopplysninger i skyen (Helsedirektoratet)](https://www.helsedirektoratet.no/rapporter/helseopplysninger-i-skyen-ny-01.10.2021)

## 3. Faller dette under EUs "digital helseteknologi"-regulering?

To separate regelverk dukket opp under research, og de har svært forskjellig alvorlighetsgrad:

### 3a. MDR (Medical Device Regulation) — potensielt det STØRSTE funnet i hele notatet

MDR klassifiserer programvare etter **Rule 11**, basert på hva programvaren FAKTISK brukes til,
ikke hva den heter. Kjernefunnet:

> Diagnostisk programvare ment for å skåre f.eks. depresjon basert på innrapporterte symptomer
> klassifiseres som **Klasse IIb**. Programvare som gir informasjon som brukes til å ta
> diagnostiske/terapeutiske BESLUTNINGER klassifiseres minimum som **Klasse IIa** — høyere
> (IIb/III) hvis en feil skåring kan føre til alvorlig/irreversibel helseskade.
> Rene selvhjelps-/øvingsapper UTEN at en behandler bruker resultatet til en beslutning kan være
> **Klasse I** eller utenfor MDR-definisjonen helt.

TestBase er IKKE en ren selvhjelpsapp — behandleren mottar en RAPPORT, godkjenner den, og bruker
den (implisitt eller eksplisitt) som grunnlag for videre behandling, inkl. uttrykkelige
risikoflagg (CORE-A, HCR-20 V3, selvmordsledd i MADRS). Dette ligner sterkt på eksemplet som
klassifiseres **Klasse IIa eller IIb**. En slik klassifisering ville normalt kreve CE-merking,
et kvalitetsstyringssystem (typisk ISO 13485), klinisk evaluering og teknisk dokumentasjon —
en vesentlig større forpliktelse enn det som er bygget så langt, og noe prosjektet IKKE har
planlagt eller priset inn ennå.

**Dette er usikkert nok, og konsekvensen stor nok, at det bør være punkt #1 man får en kvalifisert
vurdering av (regulatorisk rådgiver med MDR-erfaring, eller direkte henvendelse til Direktoratet
for medisinske produkter) FØR reell pasienttesting i produksjon** — ikke noe jeg kan avgjøre via
websøk. Jeg fant ingen norsk kilde som direkte vurderer psykologiske selvrapporteringsskjemaer
mot MDR-regelverket; vurderingen må gjøres konkret for DETTE systemets faktiske bruk.

**Kilder:**
[MDR Rule 11 — klassifiseringseksempler (OpenRegulatory)](https://openregulatory.com/articles/eu-mdr-classification-examples) ·
[MDCG 2019-11 guidance (offisiell EU-kommisjon PDF)](https://health.ec.europa.eu/system/files/2020-09/md_mdcg_2019_11_guidance_en_0.pdf) ·
[Software as a Medical Device i det europeiske markedet](https://www.eurodev.com/blog/medical-device-software-in-the-european-market)

### 3b. EHDS (European Health Data Space)

Nyere og mindre akutt: forordning (EU) 2025/327, trådte i kraft 26. mars 2025. Gjelder primært
journalsystem-produsenter og storskala sekundærbruk av helsedata (forskning/myndighetstilgang),
med konkrete kryssgrense-utvekslingsfrister i **2029 (pasientsammendrag/e-resept)** og
**2031 (bilder/labsvar/utskrivningsrapporter)**.

**For Norge spesifikt:** EHDS gjelder IKKE automatisk siden Norge ikke er EU-medlem — den må
først formelt innlemmes i EØS-avtalen. Vurdert som EØS-relevant, så det ventes å komme, men
Helsedirektoratet er fortsatt i en konsekvensutredningsfase, og pasientjournalloven må endres
før det er bindende i Norge.

**Vurdering:** sannsynligvis IKKE en umiddelbar blokkerende faktor for en liten enkeltpraksis
akkurat nå, men bør følges med på — spesielt om/når Norge innlemmer forordningen i EØS-avtalen.

**Kilder:**
[EHDS trer i kraft (DLA Piper Norge)](https://norway.dlapiper.com/no/node/6024) ·
[Felles europeisk helsedataområde EHDS (Helsedirektoratet)](https://www.helsedirektoratet.no/om-oss/internasjonalt-samarbeid/felles-europeisk-helsedataomrade-ehds) ·
[The European Health Data Space (Skadden)](https://www.skadden.com/insights/publications/2025/06/the-european-health-data-space)

## 4. Anbefalt plan / dokumentasjon til jurist

I prioritert rekkefølge:

1. **Få en kvalifisert MDR-vurdering** av om TestBase sin rapport-/skåringsfunksjon gjør det til
   medisinsk utstyr (se 3a) — dette er den ene vurderingen som kan endre hele produktets
   kompleksitet/kostnad, og bør derfor avklares FØRST, før noe annet planlegges i detalj.
2. **Få/dokumenter en signert databehandleravtale med Microsoft** som eksplisitt navngir
   lagringsregion (i dag Sweden Central), uavhengig av om regionen beholdes eller flyttes.
3. **Oppdater `docs/compliance-dpia-utkast.md`** med funnene i dette notatet (lagringssted-
   vurderingen og MDR-spørsmålet) — den er allerede skrevet med riktig forbehold og struktur for
   jurist-gjennomgang.
4. **Følg med på EHDS-innlemmelse i EØS-avtalen** og Helsedirektoratets konsekvensutredning — ikke
   en umiddelbar handling, men noter det som et åpent punkt.
5. Når 1-3 er gjort: send `docs/compliance-dpia-utkast.md` + dette notatet + en kort beskrivelse
   av systemets faktiske bruk (behandler godkjenner/bruker rapporten klinisk, hvilke tester som
   har risikoflagg) til jurist/DPO for en samlet vurdering før reelle pasienter tas inn.

## Forbehold

Dette notatet er bygget på websøk gjort 2026-10-11, ikke en jurists lesning av regelverket. Det
er IKKE juridisk rådgivning, og spesielt MDR-vurderingen i punkt 3a må behandles som en åpen,
ubesvart risiko — ikke en konklusjon — til en kvalifisert part har sett på den.
