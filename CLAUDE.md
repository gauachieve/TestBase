# TestBase — Online testesystem for psykologiske tester

Dette er et flerfase-prosjekt for en privatpraktiserende autorisert psykologspesialist i Norge og kontorfellesskapet hans: et system for å sende psykologiske tester til pasienter online, med skåring, rapporter, sikker lagring av helseopplysninger og betaling (Vipps).

**Les dette dokumentet i sin helhet før du gjør noe.** Prosjektet ble startet i Claude (Cowork) og er nettopp konvertert til Claude Code — alt av beslutninger, arkitektur og status fra det arbeidet er samlet her og i `docs/`, slik at ingenting går tapt i overgangen. Ikke anta noe om prosjektet som ikke står her eller i `docs/` — spesielt ikke om de delene som ennå ikke er bygget (fase 2–6), der er `docs/prosjektbeskrivelse-original.md` fasiten på krav, ikke hukommelse eller gjetting.

## Dokumenter i `docs/` — les i denne rekkefølgen ved behov

1. `docs/prosjektbeskrivelse-original.md` — det opprinnelige kravdokumentet (fra bruker, ordrett). Kilden til sannhet for ALLE funksjonskrav, spesielt Del 2–4 og testdefinisjonen, som ikke er designet i detalj ennå. Les den relevante delen herfra før du designer noe i fase 2 og utover.
2. `docs/beslutningslogg.md` — full historikk over beslutninger tatt, hva som er ferdig, kjente feilsøkingspunkter fra oppsett, og åpne punkter. Dette er masterdokumentet for prosjektstatus fra nå av — hold det oppdatert etter hvert som dere jobber videre.
3. `docs/compliance-dpia-utkast.md` — utkast til risikovurdering (DPIA) for helsedata, basert på Normen og GDPR. Ikke juridisk rådgivning; bør kvalitetssikres av jurist/DPO før ekte pasientdata går i produksjon.
4. `docs/del1-utviklingsmiljo-plan.md` — den opprinnelige planen for utviklingsmiljøet (delvis historisk — se beslutningsloggen for hva som faktisk endte opp implementert).

## Status akkurat nå (2026-08-31)

- **Fase 0** (arkitektur/compliance-grunnlag): ferdig.
- **Fase 1** (lokalt utviklingsmiljø): ferdig og verifisert lokalt. Sky-deploy til Azure er satt opp
  og verifisert (2026-09-02) via Azure Developer CLI (`azd`) — `azure.yaml` + `infra/` i repo-roten,
  test-miljøet `testbase-test` kjører i **Sweden Central** (ikke Norway East/West som opprinnelig
  planlagt — regional MySQL-kapasitet manglet der ved provisjonering, se
  `docs/beslutningslogg.md` under "Sky-deploy til Azure (azd)" før noen reell produksjonssetting).
- **Fase 2** (admin-skjelett + BankID/2FA-autentisering): **første slice ferdig og verifisert lokalt** — datamodell (Administrator/Behandler/invitasjon/2FA-kode), ekte cookie-basert innlogging (passord i utviklingsmodus, BankID+SMS-2FA-mock i produksjonsmodus), rollebytte for utvikler, og minimal admin-CRUD (opprett/arkiver administrator, inviter/frys/arkiver behandler).
- **Fase 3** (behandlersystem): **første slice ferdig og verifisert lokalt** — BankID+2FA-innlogging for behandler (intet passord-unntak), utvidet egenregistrering (flere felt, brukeravtale, e-post/mobil-verifisering), HPR-godkjenningsflyt (7-dagers prøveperiode), og grunnleggende pasient-CRUD (legg til enkeltvis/gruppeimport, arkiver/gjenopprett). Behandlere kan invitere kollegaer på samme måte som admin.
- **Fase 4** (pasientsystem + testmotor): **første slice ferdig og verifisert lokalt** — pasient-egenregistrering (uten kontaktverifisering, BankID etterpå er identitetsbekreftelsen), BankID-innlogging UTEN 2FA (bevisst, jf. kravet), pasientens egen side ("Min side"), og et generisk testmotor-skjelett: admin forfatter tester (sider/ledd/svartyper), behandler tildeler dem til pasienter, pasienten fyller ut side for side med fremdrift/lagre/belønningsside.
- **Fase 5** (WHO-5 ende-til-ende): **første slice ferdig og verifisert lokalt** — generalisert Likert-skala (`LikertSkala`, data-drevet N-punkts skala via `"verdi:tekst"`-par, avdekket av WHO-5s 6-punkts 0–5-skala), en pluggbar skåringsmotor (`ITestSkaaringsberegner`) og regenereringsmekanisme (`IInnebygdTestSeeder`, både dev-seed og admin-knapp) nøkkelen `Test.Kode`, og en behandler-rapportside (per besvarelse + "utvikling over tid" med 10%-signifikansmarkering). WHO-5 var første innebygde test. **2026-09-12**: åtte flere lagt til fra Helsebiblioteket
  (Cambridge atferdsskala/EQ40, RAADS-R, WURS, MADRS-S, Forenklet søvnutredningsskjema/SOVN, PHQ-9,
  IPDS/IOWA, TRAPS I), se `docs/beslutningslogg.md` "Åtte nye innebygde tester fra Helsebiblioteket".
  Samme tid: testkategoriene byttet fra de syv opprinnelige (Allianse/Angst/Depresjon/Funksjon/
  Kjerne/Nevropsykologiske/Utredning) til Helsebibliotekets 16 praktiske kategorier. **2026-09-20**:
  syv ICD-11-tester lagt til (ITQ, PDS-ICD-11 selvrapportering, PiCD, PAQ-11R, IDQ, IAQ, GADIT) —
  nytt felt `Test.IcdElleveKlar` merker disse i `Admin/Tester/Index`. Der offisiell norsk
  oversettelse IKKE finnes (PiCD, PAQ-11R, IDQ, IAQ) er dette merket via `Test.OversettelseNotat`
  ("Ikke offisielt oversatt – kun til uttesting") — se `docs/beslutningslogg.md` "ICD-11-tester" for
  kildehenvisninger, lisensvurderinger og hvilke høflighets-e-poster som gjenstår å sende. **Del 2
  (samme dag, etter tilbakemelding fra live-testing)**: alle 7 fikk korte, pasientvennlige navn
  (forkortelse til slutt) — `OversettelseNotat` vises KUN til behandler/admin, aldri til pasienten,
  se "ICD-11-tester, del 2" i beslutningsloggen. Samme runde: PDS-ICD-11 fikk vertikale
  fullbredde-svarbokser og tilfeldig visningsrekkefølge på ledd 1-10 (IKKE 11-14), PiCD fikk
  ordlydsrettelser ("Helt"/"Nøytralt"), og test-nivå intro-tekst (`Test.Beskrivelse`) fikk en egen,
  mer luftig CSS-stil (`.test-intro`).
- For fase 2–4: pris per test, økonomiske rapporter, backup/restore, organisasjonsstøtte, automatiske test-utsendelser/påminnelser, 10-års auto-sletting, Vipps-betalingssperre er bevisst IKKE gjort — se `docs/beslutningslogg.md` under "Del 2/3/4 (slice 1)" og "Åpne punkter til senere faser" for detaljer og resterende arbeid.
- Lokalisering av tester til flere språk er fortsatt bevisst utsatt (nå med et konkret
  andrespråksbehov å designe mot — WHO-5 finnes offisielt på engelsk — men ikke gjort ennå).
- Fase 6: ikke startet, men tre tverrgående forbedringer er gjort: en samlet tildelingsflyt der
  behandler/admin velger flere pasienter og flere tester (via et alfabetisk kategori-tre —
  Allianse/Angst/Depresjon/Funksjon/Kjerne/Nevropsykologiske/Utredning, foreløpig kun WHO-5 i
  Kjerne, resten tomme placeholdere) og sender i ett steg, en `Varslingspreferanse` pasienten
  velger ved registrering (SMS/e-post/Begge, standard Begge, med fallback til faktisk kontaktinfo),
  og et dev-only personnummer-overstyringsfelt på BankID-innloggingssidene for å kunne teste flere
  identiteter uten å arkivere forrige testkonto. Se `docs/beslutningslogg.md` under
  "Tildelingsflyt for tester + BankID personnummer-overstyring + varslingspreferanse".
- I tillegg: rapportgodkjenning (behandler MÅ godkjenne, og kan deretter valgfritt dele) + et enkelt
  meldings-/oppgavesystem + en daglig påminnelse-bakgrunnstjeneste til behandler om ugodkjente
  rapporter (aldri med pasientnavn i SMS/e-post, kun pasient-ID). **Rettet 2026-09-15:** en
  tidligere versjon av dette dokumentet påsto en egen `/Oppgaver`-side fantes i alle tre Areas —
  den ble faktisk bygget i fase 6, men SLÅTT SAMMEN inn i hver rolles `MinSide.cshtml` samme fase
  (bugliste 2026-09-13 punkt 22, se kommentaren øverst i `Admin/Pages/MinSide.cshtml.cs`), ikke en
  egen URL. `MinSide` ER oppgavelisten i alle tre Areas — ikke bygg en separat `/Oppgaver`-side,
  legg nye oppgavetyper til der i stedet (se f.eks. test-tilgangsforespørsler under). Se
  `docs/beslutningslogg.md` under "Meldinger og oppgaveliste" og "Test-tilgangsforespørsler".
- Rapportvisningen (behandler og pasient) er nå et paginert A4-"papir"-oppsett (ett ark per
  TestSide + forside + evt. historikk, bla med Forrige/Neste, ekte flersidig utskrift) med et fast
  handlingssett: Godkjenn/Forkast-og-send-på-nytt før godkjenning, Kopier-til-utklippstavle/
  Skriv-ut/Send-kopi-til-pasienten etter. Se `docs/beslutningslogg.md` under "Rapportvisning som
  A4-'papir'".
- **Partner System + Test Monetization** (2026-09-07, kraftig forenklet fra det opprinnelige,
  mye større forslaget i `finance_system.docx`): `Partner`-entitet (Superadmin-opprettet, egen
  `UserRole.Superadmin` — strengt supersett av Administrator), Superadmin-kuratert allow-list for
  hvilke tester en partner får (`PartnerTestTilgang`), partner-admin (BEVISST ikke egen rolle, kun
  en claim på Behandler — se `AppClaimTypes.ErPartnerAdministrator`/`PartnerId`) setter partnerens
  egen andel per test (`PartnerTestAndel`, klemt til et Superadmin-satt gulv). Hver test har nå
  Min/Maks-PASIENTPRIS + typisk behandler-honorar (`TestPrisberegner` beregner totalpris, se
  `docs/beslutningslogg.md`). Betaling gater nå `Pasientportal/Tester/Fyll` reelt (Vipps/Stripe via
  en ny `Betal`-side som gjenbruker `/BetalingTest`-mønsteret) med en finansiell snapshot
  (`TestTildelingBetaling`) og regnskapslogg (`Pengebevegelse`). Stripe Connect-utbetaling,
  abonnements-fakturering, partner-branding/embedding og multi-språk er ALLE eksplisitt utsatt —
  se `docs/beslutningslogg.md` under samme overskrift for full liste over hva som IKKE er bygget.
- **Invitasjons- og gruppesystem (2026-09-20, fase 1-2 av 5 — se docs/beslutningslogg.md):**
  `Gruppe` er nå en egen entitet (eier = behandler, tester tilordnet ved opprettelse via
  `GruppeTestTilordning`), IKKE lenger en fritekst-streng på `Pasient` (`Pasient.Gruppenavn` er
  FJERNET, erstattet av `Pasient.GruppeId`). Nye sider: `Behandlerportal/Grupper` og `Admin/Grupper`
  — BEGGE full CRUD (Admin fikk `Ny`/`Rediger` samme dag som Behandlerportal, se
  "Admin fikk full CRUD på Grupper" i beslutningsloggen — admin velger hvilken behandler som skal
  eie en ny gruppe, kan redigere/arkivere ENHVER gruppe, ingen eierskapssjekk). **Fase 2**:
  QR-basert selvregistrering — behandlerens EGEN QR
  (`Behandler.PasientInviteQrToken`) på `Behandlerportal/MinSide`, en gruppes QR (`Gruppe.QrToken`)
  på `Behandlerportal/Grupper/Rediger/{id}` (automatisk testutsending ved gruppeinnmelding). Ny
  offentlig side `Pages/BliPasient` (`/BliPasient/{b|g}/{token}`) — lagt til `StagingGate`-unntak
  for denne stien, ELLERS ville QR-funksjonen vært virkningsløs på live/beta (se beslutningsloggen
  for hvorfor, og et beslektet, IKKE ennå rettet funn om andre eksisterende offentlige sider).
  **Oppfølging samme dag:** en pasient som melder seg inn i en gruppe MED tilordnede tester logges
  nå rett inn og sendes DIREKTE til utfylling (`Pasientportal/Tester/Fyll`) etter registrering, i
  stedet for å måtte vente på SMS/e-post-lenken — se "Invitasjons- og gruppesystem, fase 2" i
  beslutningsloggen for detaljer og unntak (behandler-QR uten gruppe / gruppe uten tester viser
  fortsatt kun bekreftelsessiden). **Enda en oppfølging samme dag:** selve `BliPasient`-skjemaet
  forenklet til KUN mobilnr/e-post (minst én av dem) + valgfritt personnummer — navn/kjønn/adresse/
  Vipps-samtykke er flyttet til en ny, valgfri "fullfør profilen din"-side
  (`PasientRegistrering/FullforProfil/{token}`, IKKE et engangstoken), lenket fra en SMS/e-post sendt
  rett etter registrering. Personnummer værende blankt er BEVISST — det er skillet mellom "prøv
  systemet" og en fullverdig pasient. Se "Forenklet QR-registrering" i beslutningsloggen for
  detaljer og en reell modellbindings-bug som ble fanget og fikset underveis.
  **Fase 3 (2026-09-21):** en "prøv systemet"-pasient (intet personnummer) som fullfører en test får
  rapporten sin AUTOMATISK godkjent og vist umiddelbart — ALDRI en godkjenningsforespørsel til
  behandler (`TestService.LagreSvarAsync`). "Slett prøvedata"-knapp på BÅDE `Behandlerportal/
  Grupper/Rediger` og `Admin/Grupper/Rediger` sletter permanent alle slike pasienter (+ deres
  tildelinger/svar/betalinger) i en gruppe. **Fase 4 (2026-09-21):** aggregert rapport
  (`Grupper/Aggregert/{id}`, begge Areas) på tvers av en gruppes besvarelser — ÉN generisk visning
  for alle tester (N, gjennomsnitt/median prosentskår, kategorisk fordeling via
  `TestSkaaringIndikator`), med to modus (prøvedata = alt, ekte pasienter = Fra/Til-periode).
  **Fase 5 (2026-09-21):** verifisering av betalingsgaten for gruppetildelte tester avdekket et
  reelt hull — en prøvepasient (intet personnummer) med en PRISET test ville blitt sendt til Vipps/
  Stripe FØR de fikk prøve testen, i strid med selve "prøv før du betaler"-premisset. Fikset i
  `TestTildelingsService.TildelOgVarsleAsync`: prisen tvinges til 0/`IkkePakrevd` for enhver pasient
  uten personnummer, uansett testens faktiske pris — en ekte pasient er helt uendret. Se
  "Fase 4: aggregert rapportering + fase 5" i beslutningsloggen for full verifisering. Alle tre
  faser deployet til BÅDE beta og live samme dag som bygget, etter lokal Playwright-verifisering
  (brukerens eksplisitte instruks denne runden, i motsetning til forrige runde).
- **Ni brukerfeilrettinger (2026-09-21):** en "Fullfør profilen din"-knapp på `Pasientportal/
  MinSide` (viser lenken til `PasientRegistrering/FullforProfil/{token}` direkte i UI-et, ikke bare
  via utsendt SMS/e-post). `Behandlerportal/MinSide` sin testliste er nå tre faner ("Venter på
  godkjenning"/"Ikke besvart"/"Godkjente") over ÉN tabell med ett søkefelt som filtrerer alle tre
  samtidig (`wwwroot/js/faner.js`, generisk — samme skript brukt på Grupper sine Aktive/Arkivert-
  faner), pluss en slett-knapp på ikke-besvarte tildelinger (`TestService.
  SlettIkkeFullfortTildelingAsync`). `Gruppe` fikk rene informasjonsfelt `StartDato`/`SluttDato`;
  begge Grupper/Index-sidene fikk Opprettet/Start/Slutt-kolonner, ikonbaserte Rediger/Arkiver-
  knapper, og en egen Arkivert-fane med Gjenopprett/Slett (sistnevnte med `confirm()`-dialog —
  `GruppeService.SlettGruppeAsync` tillater KUN hard-sletting av allerede arkiverte grupper).
  `Behandlerportal/Pasienter/Rediger` fikk en opprettelsesdato og en "Påminn fullføring"-knapp
  (`PasientInvitasjonService.PaaminnFullforingAsync`) for pasienter med ufullstendig profil. En ny
  test-infoboks (`wwwroot/js/testinfo.js`) viser introduksjon/estimert tid (utledet automatisk fra
  faktisk `TestLedd`-antall, ikke et manuelt felt)/prising ved siden av kategori-treet på alle seks
  steder en test velges (Behandlerportal+Admin × Tildel/Tester, Grupper/Ny, Grupper/Rediger) — en
  reell CSS-flexbox-bug (feil flex-basis fikk boksen til alltid å bryte under treet på smalere
  sider) ble funnet og rettet under verifisering. Se "Ni brukerfeilrettinger" i beslutningsloggen.
- **Gruppe-rapportgenerator (2026-09-22):** "Generer Temp/Ekte Gruppe Rapport"-knapper på
  `Grupper/Rediger` (begge Areas) åpner en popup — radioknapp-velg ÉN av gruppens tilordnede tester
  + et datointervall (forhåndsutfylt fra tidligste tildelingsdato) — og genererer umiddelbart en
  navngitt enkelttest-rapport (N/gjennomsnitt/median/standardavvik + et SVG-spredningsplott) via en
  UTVIDELSE av den eksisterende `GruppeService.HentAggregatAsync`/`Grupper/Aggregert`-funksjonen fra
  fase 4 (nytt `testId`-query-parameter-modus), IKKE et nytt lagret rapport-subsystem — ingen ny
  migrasjon. Se "Gruppe-rapportgenerator" i beslutningsloggen for en reell kurskorrigering underveis
  (startet feilaktig med å bygge nye DB-tabeller før brukeren pekte på at aggregeringen allerede
  fantes) og en Razor `<text>`-tag-fallgruve som ble funnet og løst med `Html.Raw`.
- **Feiltolerant varsling ved QR-registrering (2026-09-22):** en kapasitetsgjennomgang FØR et
  planlagt foredrag (50–100 samtidige QR-registreringer) avdekket at hver registrering utløste
  opptil fire sekvensielle, UBESKYTTEDE eksterne API-kall (Vonage SMS + Azure Communication
  Services e-post, to ganger) — en feilende/strupet leverandør under en brå brukertopp kunne velte
  HELE registreringsforespørselen med en feilmelding til deltakeren, selv om pasienten og
  testtildelingen allerede var lagret. Fikset: `PasientInvitasjonService.
  SendFullforProfilLenkeAsync` og `TestTildelingsService.VarsleAsync` pakker nå hvert
  utsendingsforsøk i try/catch (logges, aldri kastet videre) — se "Feiltolerant varsling ved
  QR-registrering" i beslutningsloggen for full analyse og et forslag til en automatisert
  lasttest-plan (samme dato) før noe slikt kjøres skarpt igjen.
- **Lasttest mot beta + optimalisering (2026-09-22, samme dag):** en k6-lasttest med en REALISTISK
  front-tung ankomstkurve (ikke bare "alle samtidig") viste at ~1000 konferansedeltakere over
  2-5 minutter kollapser HELE App Service-en (Basic B1, én kjerne — 51-59 sekunders snitt-
  responstid, nesten null fullførte besvarelser), IKKE bare databasen. Fire kodeoptimaliseringer
  ble deretter implementert og retestet (kortlevd `IMemoryCache` for QR-token-oppslag og
  test-struktur, fjernet en duplikat `Behandler`-spørring, hevet MySQL sin `max_connections`
  171→250 + appens `Maximum Pool Size` 100→200, begge nå Bicep-forvaltet) — Azure-metrikker viste
  at MySQL ALDRI var nær en flaskehals i noen av kjøringene (CPU <11 %), og optimaliseringene
  gjorde derfor INGEN målbar forskjell på 1000-scenarioet (fortsatt tilnærmet null fullføringer).
  Beholdt likevel — reell nytte ved mer moderat, vedvarende last. Se "Lasttest mot beta, del 2" og
  "Optimalisering før skalering" i beslutningsloggen for full metodikk, tall og en ærlig
  forklaring på HVORFOR kode-optimalisering ikke kan løse en ren CPU-kjerne-flaskehals.
- **Lasttest mot beta, del 3 — App Service-SKU er nå parameterisert, "Standard S1" var feil
  neste steg (2026-09-22, samme dag):** `infra/resources.bicep`/`main.bicep` fikk fire nye
  azd-miljøvariabel-styrte parametere (`APP_SERVICE_SKU_NAME`/`_TIER`, `MYSQL_SKU_NAME`/`_TIER`,
  default dagens verdier) slik at fremtidig opp-/nedskalering for lasttesting er ren
  `azd env set` + `azd provision`, ikke en kodeendring. Beta ble skalert til Standard S1 +
  MySQL Burstable B2s og retestet med samme 1000-deltakere-scenario — resultatet ble MERKBART
  DÅRLIGERE enn før (27,9 % bestått, kun 1 fullført besvarelse), fordi **Basic B1 og Standard S1
  har IDENTISK maskinvare (1 vCPU/1,75 GB) — Standard gir kun ekstra funksjoner, aldri flere
  kjerner.** Azure-metrikker bekreftet CPU IKKE var mettet noe sted (App Service <16 %, MySQL
  <7 %), men tråd-/responstidsoppbygging (86 tråder, 48,8s snitt) pekte fortsatt mot
  trådpool-/tilkoblings-kø, ikke rå CPU. Live ble ALDRI rørt; beta satt tilbake til B1/Basic +
  Standard_B1ms/Burstable samme dag. Neste eksperiment-verdige tier er `S2` (2 vCPU/3,5 GB), ikke
  `S1` — se "Lasttest mot beta, del 3" i beslutningsloggen for full metodikk og Azure-metrikk-tall.
- **Lasttest mot beta, del 4 — Standard S2 er et reelt gjennombrudd (2026-09-22, samme dag):**
  App Service alene satt til `S2` (2 vCPU/3,5 GB, MySQL bevisst holdt på `Standard_B1ms` for å
  isolere variabelen) og retestet mot samme ~1000-deltakere-scenario: 473 fullførte besvarelser
  (mot 1 på S1, ~0 på opprinnelig B1), 82,4 % sjekker bestått (mot 27,9 %), snitt responstid 13,2s
  (mot 31,9s). App Service `CpuTime` viste nå faktisk 2-kjerners bruk (~50 % samlet utnyttelse) i
  stedet for kø-oppbygging. **Den NYE flaskehalsen: MySQL `active_connections` toppet på 222 av det
  hevede taket på 250** — App Service-siden er ikke lenger den begrensende faktoren, databasen
  (fortsatt B1ms) er det. Naturlig neste steg: `S2` + `Standard_B2s` SAMTIDIG. Beta satt tilbake
  til B1/Basic samme dag, live ALDRI rørt. Se "Lasttest mot beta, del 4" i beslutningsloggen for
  full sammenligningstabell og Azure-metrikk-tall.
- **Lasttest mot beta, del 5 — S2+B2s reproduserbart dårligere, LIVE skalert til S2 alene for
  ekte konferanse 2026-09-23 (2026-09-22, samme dag):** `S2`+`Standard_B2s` SAMMEN ble testet på
  beta TO ganger (rett etter provisjonering OG etter 4 min oppvarming) — BEGGE ganger
  reproduserbart DÅRLIGERE enn `S2` alene (3-15 fullførte besvarelser mot 473, 28-33 % sjekker
  bestått mot 82 %). MySQL var ikke flaskehalsen i noen av kjøringene; årsaken på App
  Service-siden er UAVKLART (Azure Monitor nektet å levere CpuTime/Threads-metrikker for én
  kjøring — mistenkelig, ikke fulgt opp pga. tidspress). **LIVE (`testbase-test`) er derfor
  skalert til `S2` ALENE (MySQL uendret `Standard_B1ms`) for den faktiske konferansen 23. sep
  2026 kl. 11:50-13:00** — den eneste konfigurasjonen med konsistent gode resultater. En
  session-only CronCreate-jobb (23. sep ~13:17 norsk tid, IKKE garantert å overleve til da) skal
  hente ekte konferanse-metrikker, dokumentere dem, og skalere live tilbake til B1/Basic
  etterpå — brukeren bedt om å uansett sjekke inn etter kl. 13:00. Se "Lasttest mot beta, del 5"
  i beslutningsloggen.
- **STØ avviste ekte BankID sin fødselsnummer-bestilling — løsning: dropp NNIN-scope
  (2026-09-22, samme dag):** GGPsykolog AS sin BankID-bestilling ble avvist av reselgeren Stø AS
  siden foretaket ikke er "helseforetak"/dokumentert databehandler. Undersøkt mot BankID sin
  offentlige OIDC-dokumentasjon: kun `nnin`/`nnin_altsub`-scopene (faktisk UTLEVERING av
  fødselsnummer) krever slik "legal basis" — `openid`+`profile` er ubetinget tilgjengelig for
  enhver klient. Besluttet retning (brukerens valg): fremtidig
  `Miljo:EktBankIdProfesjonell`-integrasjon skal KUN be om `openid`+`profile`, ALDRI
  `nnin`/`nnin_altsub` — BankID blir en ren autentiseringslager oppå det personnummeret brukeren
  ALLEREDE har oppgitt selv ved egenregistrering (som allerede lagres kryptert), ikke en
  oppslagskilde. Krever en arkitekturendring i `AdminAuthenticationService`/
  `BehandlerAuthenticationService` sin personnummer-basert matching — IKKE implementert ennå.
  Brukeren reapplyer PARALLELT for `nnin_altsub` hos Stø uansett. Gjelder KUN admin/behandler —
  pasient er alltid mock BankID, urelatert til konferansen. IKKE juridisk rådgivning, bør
  kvalitetssikres av DPO/jurist. Se "STØ avviste fødselsnummer-bestilling" i beslutningsloggen.
- **Etter konferansen (2026-09-23): konferansegruppen kunne IKKE identifiseres, live skalert ned
  uansett — VIKTIG SIKKERHETSFUNN:** den planlagte oppfølgingsjobben fant INGEN gruppe som matcher
  dagens WHO-5-konferanse i `Admin/Grupper` på live — kun én eksisterende gruppe ("Test Gruppe",
  opprettet 2026-09-20, tre dager FØR konferansen, med et pengespillavhengighet-screeningverktøy
  tilordnet, ikke WHO-5). MySQL-metrikker for konferansevinduet viser praktisk talt INGEN aktivitet
  utover idle-grunnlinje. **`Admin/Pasienter` avdekket at store deler av "Test Gruppe" sine 43
  pasienter har det som ser ut som EKTE navn/e-post/(i minst ett tilfelle) et gyldig-utseende
  personnummer — IKKE syntetisk testdata.** Disse radene er BEVISST IKKE rørt/slettet/undersøkt
  videre, og verken navn, e-post eller personnummer er gjengitt i noen kildekontrollert fil. Live
  ble likevel skalert trygt tilbake til `B1`/`Basic` (rent reversibelt, uavhengig av
  gruppe-usikkerheten). Avventer brukerens avklaring om hvilken gruppe/mekanisme konferansen
  faktisk brukte, og hva "Test Gruppe" faktisk er. Se "Etter konferansen" i beslutningsloggen.
  **Oppdatering samme dag: brukeren bekreftet "Test Gruppe" ER konferansegruppen** (GADIT-testen og
  09-20-datoen var altså bevisste, ikke tegn på feil gruppe) — og rapporterte deretter en reell
  500-feil ved rapportgenerering. Rotårsak: `GaditSkaaringsberegner` antok en FAST POSISJON i
  svar-listen (ledd 0-5 = frekvens, 6-7 = Ja/Nei), men `TestService.LagreSvarAsync` hopper stille
  over ubesvarte felt UTEN å hindre "Fullfort" — én pasient som droppet ett frekvensspørsmål fikk
  dermed et Ja/Nei-svar til å lande på en posisjon koden ventet tall, og `int.Parse` kastet.
  **Samme posisjonsbaserte mønster finnes i `Phq9Skaaringsberegner`** (`svar.Take(9)` for å
  ekskludere funksjonsspørsmålet) — der ville det IKKE krasje (alle 10 PHQ-9-ledd er numeriske) men
  STILLE gi feil depresjons-sumskår, ikke undersøkt/fikset ennå. Fikset kun for GADIT: klassifiserer
  nå hvert svar etter EGEN VERDI (tall vs. "Ja"/"Nei"), ikke posisjon — robust uavhengig av hvilke
  ledd som faktisk ble besvart. Ny regresjonstest lagt til, deployet med `azd deploy` (ren
  kodeendring), verifisert ende-til-ende i nettleser på selve den virkelige konferansedataen (34
  prøvedata-deltakere + 1 ekte pasient, ingen krasj). Se "Reell 500-feil i GADIT-skåring" i
  beslutningsloggen — inkl. en åpen oppfølgingsoppgave om å sjekke andre skåringsklasser for samme
  sårbarhetsmønster.
- **Tre konferanse-feedback-punkter (2026-09-23, samme dag):** (1) "Lagre"-knappen på
  `Pasientportal/Tester/Fyll` FJERNET — forvirret deltakere til å tro testen var levert; reelt
  overflødig siden Neste/Ferdig alltid lagrer gjeldende side uansett. (2) Personnummer FJERNET fra
  `BliPasient` (steg 1) — utelukkende samlet inn i `PasientRegistrering/FullforProfil` (steg 2,
  allerede eksisterende) nå. (3) NY generell mekanisme: `Test.MaksUbesvartProsent` (gyldighetsgrense)
  + `TestLedd.NormertGjennomsnitt` (imputering av ubesvarte ledd) — `TestSkaaring.GyldighetsAdvarsel`
  vises som advarsel i behandlers rapport (+ en mildere variant til pasienten på "Ferdig!"-siden),
  ALDRI en sperre for innsending. BEVISST SOVENDE for alle eksisterende tester (begge felt null) —
  ingen oppdiktede normeringstall lagt inn, kun ekte, sitert litteratur duger. Se "Tre
  brukerfeedback-punkter fra selve konferansen" i beslutningsloggen for full arkitektur og hvordan
  fylle inn ekte tall for én test når/hvis normeringsdata skaffes.
- **Grupperapportens "Spredning" byttet fra tidslinje til histogram (2026-09-23, samme dag):**
  `Grupper/Aggregert` (begge Areas) sin spredningsgraf viste tidligere prosentskår KRONOLOGISK —
  byttet til et ekte histogram: 10 %-brede bøtter over 0-100 %, søylehøyde = antall deltakere i
  bøtta. `ScatterPunkt`→`HistogramSoyle`, ny `BeregnHistogram` (bøtter via `verdi / 10`, klemt
  [0,9], siste bøtte dekker 90-100). Kun visningslaget endret — `GruppeService`/`ProsentDatapunkt`
  urørt. Se "Grupperapportens 'Spredning' byttet fra tidslinje til histogram" i beslutningsloggen.
- **Histogrammet byttet fra alltid-prosent til råskår-som-standard + cutoff-linjer (2026-09-23,
  samme dag):** de fleste standardiserte tester publiserer sin kliniske grenseverdi i RÅSKÅR (GADIT
  ≥5 av 8, PHQ-9 5/10/15/20 av 27), ikke prosent — å alltid konvertere skjulte de klinisk
  meningsfulle tallene, bevisst IKKE valgt selv om det hadde gjort koden enklere. Ny valgfri
  `ITestSkaaringsberegner`-konfigurasjon (default interface members, INGEN av de ~17 eksisterende
  beregnerne trengte kodeendring): `VisSomProsentIHistogram` (kun `true` for Who5/Who5Vas — deres
  EGEN offisielle rapporteringskonvensjon) og `Histogramgrenser` (populert fra allerede EKSISTERENDE
  cutoff-konstanter i Gadit/Ipds/TrapsI/Wurs/RaadsR/Phq9/MadrsS/Who5/Who5Vas — ingen ny klinisk data
  oppfunnet). `BeregnHistogram` generalisert til enhver skala (`bucketBredde = ⌈skalaMaks/10⌉`, så
  en liten råskala som GADITs 0-8 IKKE tvinges til 10 kunstig smale bøtter), cutoff-linjer tegnes
  presist (verdibasert X, ikke bøtte-indeksbasert). Verifisert i nettleser mot ekte lokal GADIT-data:
  oversikt viser "2,5 / 8" (råskår), enkelttest-histogram viser råskår-bøtter + stiplet
  "Grenseverdi"-linje presist ved verdi 5. Underveis funnet og fikset en NY, tidligere
  udokumentert Razor-fallgruve (bokstav-rett-før-`@variabel` renderer bokstavelig i stedet for å
  interpolere — se fallgruve-listen under). Se "Histogrammet byttet fra alltid-prosent til
  råskår-som-standard" i beslutningsloggen. Samme kjente PHQ-9-posisjonssårbarhet fortsatt IKKE
  fikset, kun re-flagget.
- **Reell 500-feil ved admin/behandler-innlogging: 2FA-SMS krasjet hele innloggingen (2026-09-24):**
  `ToFaktorService.StartAsync` kalte `ISmsSender.SendAsync` UBESKYTTET — Vonage-kontoen har for
  øyeblikket lav saldo (`402 Payment Required`), så ETHVERT innloggingsforsøk som når 2FA-steget
  (gyldig admin/behandler-personnummer, ingen betrodd enhet fra før) kastet en ufanget
  `HttpRequestException` helt til en `500`-side — reelt LIVE innloggingsstopp, reprodusert og
  bekreftet med `curl` + `az webapp log tail` (samme metode som GADIT-krasjen dagen før). Fikset:
  SMS-utsendingen er nå i try/catch (`ToFaktorStartResultat(Kode, SendtSms)`), 2FA-koden opprettes
  og er gyldig UANSETT, men `Pages/Konto/BekreftKode.cshtml` viser nå en ærlig advarselsboks når
  SMS-leveringen feilet i stedet for å late som den lyktes — 2FA er essensielt for innlogging (i
  motsetning til en fire-and-forget-varsling), så dette er IKKE samme "svelg stille"-mønster som
  "Feiltolerant varsling ved QR-registrering" (2026-09-22), som forøvrig aldri dekket denne
  kodestien. Selve Vonage-saldoen er et SEPARAT, ufikset driftsproblem (fakturering, ikke kode) —
  advarselsboksen vil fortsette å vises til kontoen fylles på. Deployet til BÅDE live og beta samme
  dag. Se "Reell 500-feil ved admin/behandler-innlogging" i beslutningsloggen.
- **Fire UI-forbedringer på tildelingsflyten + pasientlister (2026-09-24, samme dag):** gruppe­
  rapport-popupens "Generer"-knapp gråes ut + endrer tekst mens den genererer (kun ett
  `data-disable-on-submit`-attributt, gjenbrukt eksisterende mekanisme); ny "Tildel tester"-
  ikonknapp per rad på `Behandlerportal/Pasienter/Index` som hopper RETT til steg 2 av
  tildelingsflyten for akkurat den pasienten (`?forhaandsvalgtId=`); søkefilter på testvalget
  (`Tildel/Tester`, ny `testtre-filter.js`) og på pasientvalget (`Tildel/Pasienter`, gjenbrukt
  `tabellfilter.js`) i BEGGE Areas; Aktiv/Arkivert-faner (samme `faner.js`-mønster som
  `Grupper/Index`) på BÅDE `Behandlerportal/Pasienter/Index` og `Admin/Pasienter/Index`. Se
  "Fire UI-forbedringer på tildelingsflyten + pasientlister" i beslutningsloggen.
- **Natt-økt (2026-09-24/25, pågående over flere økter): stor batch nye innebygde tester,
  autonomt uten å vente på brukerens tilbakemelding** — se docs/beslutningslogg.md "Natt-økt:
  seks nye innebygde tester..." (og senere seksjoner samme natt/påfølgende dager) for full,
  løpende status. Ny, generell arkitekturfiks først: `ITestSkaaringsberegnerMedLedd` (valgfri
  utvidelse av `ITestSkaaringsberegner`, ingen eksisterende beregner endret) gir en
  skåringsberegner ekte `TestLedd.Id`-oppslag mot ALLE testens ledd — nødvendig for enhver ny test
  med FLERE delskalaer der alle ledd deler samme Likert-skala (samme rotårsak-klasse som
  GADIT-krasjen 2026-09-23, men verdibasert klassifisering virker ikke der). Samtidig fikset:
  `TestService.HentSkaaringHistorikkAsync` kalte skåringsberegneren direkte uten å sjekke dette
  nye grensesnittet (ville krasjet "utvikling over tid"-grafen for enhver MedLedd-basert test).
  Første seks tester ferdig: ASRS, AUDIT, DUDIT, SCL-25, BSQ-14, SDQ-20 — alle patient-
  selvutfylte, INGEN skjemaendring. Norsk oversettelse er EGENFORFATTET for samtlige (ikke hentet
  fra en sitert offisiell kilde) — eksplisitt flagget i hver seeder, bør kvalitetssikres før reell
  klinisk bruk, i tråd med brukerens eget "det blir feil i første forsøk uansett". Gjenstår:
  YGTSS-R, MADRS klinikkversjon, EDE-Q, CORE×3, SIPP-118, SCID-5-PF, TRAPS-II, Mini-Screen 6
  (usikker lisens), HCR-20 V3 (lisensiert forensisk risikovurderingsverktøy — bør IKKE bygges med
  oppdiktet reelt iteminnhold, se beslutningsloggen når/hvis den seksjonen er skrevet) — pluss en
  planlagt "test fylles ut av behandler, ikke pasient"-mekanisme som trengs for tre av disse. En
  session-only `CronCreate`-jobb (hver time, utløper etter 7 dager) satt opp for å fortsette
  arbeidet automatisk — INGEN garanti mot at en helt ny økt må startes manuelt hvis selve
  CLI-prosessen faktisk avsluttes, se beslutningsloggen. **Del 2 (samme natt):** EDE-Q og TRAPS II
  også ferdig — begge OMSKREVNE/PARAFRASERTE gjengivelser av opphavsrettslig beskyttede
  instrumenter (Fairburn & Beglins EDE-Q, og en uverifisert NKVTS "TRAPS II"-sammenstilling av
  SLESQ-R + samme innhold som den allerede innebygde ITQ-testen), IKKE verbatim kopier — flagget
  tydelig i hver seeder. En reell bug ble funnet OG FIKSET FØR commit (fanget av en ny enhetstest):
  `TrapsIiSkaaringsberegner` sitt første utkast parset ALLE svar som tall, men Del 1 sine JaNei-ledd
  er ikke tall — `FormatException` umiddelbart. Samtidig oppdaget, IKKE fikset: den allerede
  eksisterende, frittstående `ItqSkaaringsberegner` bruker fortsatt ren listeposisjon (`svar[index]`)
  for PTSD/DSO-klassifisering — SAMME sårbarhetsklasse som GADIT-krasjen — re-flagget sammen med
  PHQ-9s tilsvarende kjente sårbarhet for en fremtidig opprydningsrunde. **Del 3 (samme natt):**
  CORE-10 (kortversjon av CORE-OM, CORE System Trust) også ferdig — reverse-skårer ledd 1/5,
  flagger ledd 3/10 (selvskading/"livet ikke verdt å leve") separat, `ITestSkaaringsberegnerMedLedd`.
  **Del 4 (samme natt, cron-jobben fortsatte automatisk):** CORE-OM (full 34-ledds versjon, 4
  domener, 10 reverse-skårte ledd) og CORE-A (frittstående 8-ledds risikoscreening — brukerens
  forkortelse tolket som et supplement til CORE-OMs risikodomene, IKKE et navngitt offisielt
  dokument) også ferdig. CORE-A er BEVISST IKKE et sumskår-verktøy — risiko for seg selv/andre
  flagges som egne indikatorer uansett totalskår, verifisert ende-til-ende i nettleser (en
  besvarelse med 6 % totalskår viste likevel korrekt "Flagget" for risiko for andre). Gjenstår
  fortsatt: SIPP-118, YGTSS-R, MADRS klinikkversjon, SCID-5-PF (de tre siste krever "behandler
  fyller ut"-mekanismen), Mini-Screen 6 og HCR-20 V3 (lisensfølsomme). **Del 5 (samme natt):**
  "SIPP-118" (`sipp118`) er BEVISST en kraftig redusert egen tilpasning (30 ledd, kun de 5 kjente
  overordnede domenene), IKKE en gjengivelse av det ekte 118-ledds/16-fasetts instrumentet — vi
  hadde ikke sikker nok kildetilgang til å gjengi de 16 fasettene korrekt, og valgte å være
  eksplisitt om begrensningen (testnavnet sier selv "forenklet, inspirert av") fremfor å gjette med
  falsk selvsikkerhet. Høyere skår = bedre funksjon (motsatt konvensjon av symptommål), reverse-
  skårer maladaptive ledd men et hoppet-over maladaptivt ledd teller 0 (ikke falsk maks 5) —
  verifisert med regresjonstest. **Del 6 (samme natt):** "behandler fyller ut"-mekanismen bygget —
  `Test.FyllesUtAvBehandler` + `TestSvar.BehandlerKommentar` (ny migrasjon), en ny side
  `Behandlerportal/Pasienter/FyllForPasient/{id}/{side?}` (samme side-for-side-mønster som
  pasientens `Tester/Fyll`, men eierskapssjekk mot behandlers egne pasienter, ingen betalingsgate,
  én kommentarboks per ledd). `TestTildelingsService.TildelOgVarsleAsync` gir en slik test verken
  pris eller pasientvarsel — den havner i stedet i en ny `BehandlerOppgave`-liste vist i begge
  Tildel/Tester.cshtml-resultatsidene, og som en "Fyll ut"-lenke i `MinSide` sin "Ikke besvart"-fane.
  Kommentarer vises nå i BEGGE Areas' rapportvisning (inkl. Behandlerportals utklippstavle-mal)
  rett under spørsmålet. Første test på denne mekanismen: YGTSS-R (Leckman et al. 1989,
  tic-alvorlighet, `ITestSkaaringsberegnerMedLedd` siden sjekklisten kan ha ulikt antall avkryssede
  ledd) — verifisert FULLT ende-til-ende i nettleser (tildeling → ingen pasientvarsel vist → utfylt
  av behandler m/ kommentar → rapport viste korrekt 60/100 og kommentaren riktig plassert). **Del 7
  (samme natt):** MADRS klinikkversjon (`madrs_klinikk`) — den kliniker-administrerte originalen,
  til forskjell fra den innebygde selvutfyllingsversjonen MADRS-S. 10 ledd (mot MADRS-S sine 9,
  siden "tilsynelatende tungsinn" og "rapportert tungsinn" er separate ledd her), samme 0-6-skala
  (0-60 totalt). Andre test på "behandler fyller ut"-mekanismen — INGEN ny sideinfrastruktur
  trengtes, `FyllForPasient` er allerede fullt generisk. Selvmordsledd (ledd 10) flagges separat
  uavhengig av totalskår, identifisert via ekte TestLeddId (`ITestSkaaringsberegnerMedLedd`).
  Verifisert FULLT ende-til-ende i nettleser: 38/60 "alvorlig deprimert" + selvmordsflagg korrekt
  vist, kommentar korrekt plassert i rapporten. **Del 8 (samme natt):** SCID-5-PF (`scid5_pf`) — den
  mest komplekse testen bygget i natt: kliniker-administrert screening for alle 10 DSM-5
  personlighetsforstyrrelser (81 ledd, egne parafraserte kriterier per forstyrrelse — IKKE sitert
  fra DSM-5 eller det lisensierte SCID-5-PD-intervjuet, se seeder-XML-doc for fullt forbehold),
  bevisst et "første forsøk" brukeren selv ba om. Antisosial personlighetsforstyrrelse har to
  portvakt-Ja/Nei-ledd (atferdsforstyrrelse <15 år, alder ≥18) som MÅ begge være "Ja" uansett antall
  oppfylte kriterier. `Scid5PfSkaaringsberegner` grupperer ledd etter TestSideId, sortert etter
  LAVESTE TestLeddId per gruppe (en strengere robusthetsstandard enn tidligere i natt — stoler ikke
  på Rekkefolge-sortering på tvers av sider). Verifisert ende-til-ende med et scenario spesifikt
  designet for å bevise portvakt-logikken: Antisosial 7/7 kriterier oppfylt men portvakt "alder≥18"=
  Nei → korrekt IKKE diagnostisert, mens Borderline 5/9 (nøyaktig terskel) korrekt ble det. **Del 9
  (samme natt, avslutning):** Mini-Screen 6 og HCR-20 V3 opprinnelig IKKE bygget, kun dokumentert —
  **rettet 2026-09-26** etter at brukeren selv avklarte lisensspørsmålet for begge (se
  beslutningsloggen "HCR-20 V3 og M.I.N.I. bygget likevel"): HCR-20 V3 sitt faktiske arbeidsskjema
  (item-navn/struktur/vurderingsskala) er GRATIS fra SIFER (Helse Bergen) — kun brukermanualen
  (kr. 250,- per bruker) koster noe, og det er opp til hver kliniker selv. `hcr20_v3` bygget med de
  EKTE offisielle 20 faktornavnene/Tilstede-Relevans-skalaen fra SIFERs frie PDF, men IKKE de
  betalte kodingskriteriene. BEVISST IKKE et sumskår-verktøy (som CORE-A) — Trinn 7 sin
  Lav/Moderat/Høy-konklusjon er klinikerens EGEN vurdering, aldri utledet fra faktor-tellingen;
  verifisert med et scenario der 10/10 faktorer var Høy relevans men klinikeren likevel konkluderte
  Lav — rapporten viste korrekt klinikerens konklusjon, ikke en avledet "høy risiko". For M.I.N.I.:
  rettighetshaver ba om å SE systemet før lisensavtale, så `mini_strukturdemo` bygget som et rent
  UI/UX-eksempel — 10 offentlig kjente modulnavn, men HELT EGNE generiske spørsmål (IKKE M.I.N.I.
  sitt faktiske, lisensierte innhold eller gren-/hoppelogikk), testnavnet sier selv
  "(IKKE lisensiert innhold)". Begge på "behandler fyller ut"-mekanismen, verifisert ende-til-ende.
- **Reell 500-krasj rettet: EDE-Q (og potensielt ethvert fler-sides test) (2026-09-26/27):**
  `TestService.HentTestStrukturAsync` sorterte ledd KUN på `Rekkefolge` (nullstilles per side) —
  for en test med FLERE sider (EDE-Q: 3 sider) ga MySQL ingen garanti om at ledd fra ulike sider med
  SAMME Rekkefolge-verdi ble holdt sammen. EDE-Q sine 5 ikke-skårede fritekstledd havnet dermed
  innimellom de skårede leddene, fikk `EdeqSkaaringsberegner` sin posisjonsbaserte delskala-
  inndeling til å plukke opp et fritekst-svar som tallverdi → `FormatException` ved rapportvisning/
  godkjenning på LIVE (reprodusert og fikset direkte der). Fikset ved å eksplisitt sortere på
  `(side.Rekkefolge, ledd.Rekkefolge)` — samme prinsipp `BeregnSkaaringAsync` allerede fulgte riktig.
  Dekker `HentTildelingMedInnholdAsync` OG `HentSkaaringHistorikkAsync` samtidig — potensielt
  relevant for CORE-OM/SIPP-118-inspirert/TRAPS II/YGTSS-R også (alle fler-sides), som kun unngikk
  krasj ved tilfeldig MySQL-radrekkefølge, ikke ved design. CORE-A viste seg IKKE berørt (én side).
  Se docs/beslutningslogg.md "Natt-økt, del 10" for full analyse — starten på en stor
  brukerfeedback-runde med mange gjenstående UI/rapport-fikser.
- **Reelt sikkerhetshull rettet: pasient kunne fylle ut SCID-5-PF (2026-09-26/27):**
  `Pasientportal/Tester/Fyll.cshtml.cs` sjekket KUN eierskap, aldri `Test.FyllesUtAvBehandler` —
  en pasient med riktig tildeling-ID kunne fylle ut en klinikertest. Fikset med en eksplisitt
  `NotFound()`-sperre i BEGGE handlere (selve sikkerhetsgrensen), pluss en ny
  `TestService.HentPasientSynligeTildelingerAsync` (ekskluderer FyllesUtAvBehandler) brukt i
  `Pasientportal/MinSide`, "neste test"-navigasjonen, og `_Layout` sin badge-teller (UX-supplement,
  ikke selve grensen — `Behandlerportal/Pasienter/Detaljer` bruker fortsatt den ufiltrerte listen
  med vilje). Samtidig fikset "Ferdigstill og videre til {testnavn}"/"Ferdigstill og tilbake til
  Min Side"-knappene (proper `btn-accent`, samme rad, dynamisk testnavn, riktig "Min Side"-
  kapitalisering) — se docs/beslutningslogg.md "Natt-økt, del 11/12" for full verifisering.
- **Kommentarfelt-UX for alle klinikertester + SCID-5-PF strukturfikser (2026-09-27):**
  `FyllForPasient.cshtml` (delt av alle 5 behandler-utfylte tester) fikk auto-voksende
  kommentarfelt (`wwwroot/js/autogrow.js`, native `resize: vertical` for PC-drahåndtak) og en ny
  "Veiledning"-boks til høyre per ledd (viser `TestLedd.Instruksjon`, fremheves ved fokus på
  kommentarfeltet). Ny BEVISST destruktiv `TestService.SlettTestHeltForRegenereringAsync` — kun for
  tester under aktiv strukturell iterasjon, IKKE for tester med ekte pasientdata — brukt til å
  regenerere SCID-5-PF med: riktig SCID-5-PD-modulrekkefølge (Antisosial SIST, ikke fjerde som
  første versjon hadde), tallprefiks i skala-teksten ("0. Fraværende" osv.), og et eget
  eksempel/veiledning-notat per kriterium (alle 79). Se docs/beslutningslogg.md "Natt-økt, del 13".
- **M.I.N.I.-rapporten uten meningsløs prosent (2026-09-27, samme runde):** ny
  `TestSkaaring.SkjulProsent` (standard usann) lar en skåringsberegner be individrapporten (begge
  Areas + "Kopier alt"-malen) skjule prosent/råskår-linjen helt — satt sann for
  `MiniStrukturdemoSkaaringsberegner`, som nå kun bygger Indikatorer for FLAGGEDE moduler, formatert
  "{modulnavn} ({antall}/{totalt})" i selve Verdi-strengen. Se docs/beslutningslogg.md
  "Natt-økt, del 14".
- **Cutoff-linjer på individrapporten, ikke bare gruppehistogrammet (2026-09-27):** ny
  `RapportModel.Cutoffs` (begge Areas) tegner `ITestSkaaringsberegner.Histogramgrenser` som en
  vertikal strek + "▼ {navn}"-label over resultat-fremdriftsbaren i `Rapport.cshtml`, skalert til
  0-100% av baren for råskår-cutoffs. "Kopier alt"-malen får en tekstlig variant ("Grenseverdi:
  {navn} ved {verdi}") i stedet for en visuell strek. Ingen endring for tester uten
  `Histogramgrenser` (de fleste) eller med `SkjulProsent`. Se docs/beslutningslogg.md
  "Natt-økt, del 15".
- **Radar-graf for SIPP-118-inspirerte testens 5 domener (2026-09-27):** ny `Sipp118RadarBeregner`
  (ren C#, samme mønster som `UtviklingsGrafBeregner`) tegner et 5-akset pentagon av de 5
  domenepoengene DENNE besvarelsen fikk (leser `TestSkaaring.Indikatorer`), med en stiplet cutoff-
  ring ved den forenklede lavfunksjon-grensen. Kilde for domenestruktur/at radaren i litteraturen
  er PER RESPONDENT (ikke over tid): https://pmc.ncbi.nlm.nih.gov/articles/PMC12287623/ — bekreftet
  også at det ekte SIPP-118 har 16 fasetter under de 5 domenene, som vår test BEVISST ikke måler,
  så radaren viser kun de 5 domenene. `<text>`-elementer via `Html.Raw` (samme kjente Razor-
  fallgruve/løsning som `_UtviklingsGraf.cshtml`). Se docs/beslutningslogg.md "Natt-økt, del 16".
- **Stolpediagram per personlighetsforstyrrelse i SCID-5-PF-rapporten (2026-09-27, avslutning på
  denne rundens liste):** ny `Scid5PfBarBeregner` (egen selvstendig gruppering, samme prinsipp som
  `Scid5PfSkaaringsberegner`) bygger et 3-fargers horisontalt stolpediagram per forstyrrelse (mørkt
  = "2"-svar, lyst = "1"-svar, tom rest) + en ▼-cutoff-pil ved terskel/total, rendret som rene
  HTML/CSS-divs (ikke SVG, unngår `<text>`-fallgruven helt). PD-navn fet skrift når terskel nås.
  "Blandet personlighetsforstyrrelse"-heuristikken (minst 10 "2"-kriterier samlet, men ingen enkelt
  PD når egen terskel) er UTTALT IKKE en offisiell DSM-5/ICD-11-cutoff (undersøkt og bekreftet
  fraværende) — egen, tydelig merket tommelfingerregel. Se docs/beslutningslogg.md
  "Natt-økt, del 17".
- **SCID-5-PF-stolpediagrammet finpusset etter skjermbilde-tilbakemelding (2026-09-27):** fjernet
  duplikat Indikator-badges/Fortolkning fra "Resultat"-blokken for denne testen (samme info som
  stolpediagrammet under, bare dårligere format); stolpebredde nå `Total * 22px` PER forstyrrelse
  (kortere, proporsjonalt med faktisk antall kriterier, ikke en fast 300px for alle); cutoff-pilen
  har samme oransje farge som "Tydelig oppfylt"-segmentet (var(--accent-dark), ikke lenger rød);
  antall 2-ere/1-ere skrevet som synlig tekst INNI hvert fargede felt. Se docs/beslutningslogg.md
  "Natt-økt, del 18".
- **TRAPS II-rapporten utvidet med klyngeskår og bekreftede traumeeksponeringer (2026-09-27, siste
  utestående punkt fra denne rundens brukerfeedback):** `TrapsIiSkaaringsberegner` viste tidligere
  KUN den endelige PTSD/KPTSD-konklusjonen — Fortolkningsteksten og seks nye Indikatorer lister nå
  alle seks underliggende klyngeskår (Re/Av/Th for PTSD, Ad/Nsc/Dr for DSO, maks 8 hver), og hvert
  av de 14 Del 1-traumeeksponeringsspørsmålene besvart "Ja" (pluss frittekst-tillegget hvis besvart)
  vises som egen Indikator med selve spørsmålsteksten — "Nei"-svar vises ikke (samme "isoler det
  som faktisk er utløst"-prinsipp som M.I.N.I., del 14). 2 nye regresjonstester (74 totalt),
  verifisert ende-til-ende i nettleser (full utfylling + behandler-godkjenning). Se
  docs/beslutningslogg.md "Natt-økt, del 19".
- **Tilbakemeldingsverktøy — del 1 (2026-09-28):** en flytende, flyttbar, minimerbar
  tilbakemeldingsknapp (`_TilbakemeldingWidget.cshtml` + `wwwroot/js/tilbakemelding-widget.js`,
  vist på ALLE sider via den ene delte `_Layout.cshtml`) — rollup-meny med "Tilbakemelding"
  (aktiv)/"Hjelp" (grået ut, kommer senere), åpner et norsk skjema som alltid forsøker et
  best-effort skjermbilde (`html2canvas`, BEVISST vendoret lokalt i `wwwroot/js/vendor/`, ikke
  lastet fra CDN ved kjøretid) + auto-samlet teknisk kontekst + automatisk fanget siste JS-feil/
  500-side-markør (merket som "krasjrapport"). Lagres via nytt offentlig minimal-API-endepunkt
  (`POST /api/tilbakemelding`, `TestBase.Shared.Domain.Tilbakemeldinger.Tilbakemelding`-entitet, ny
  migrasjon) og gjennomgås av admin på `Admin/Tilbakemeldinger` (AdminOmrade — kan inneholde
  pasientdata i et skjermbilde, samme tilgangsnivå som ellers). Et nytt delt-nøkkel-beskyttet
  `/api/agent/*`-API (samme `StagingGate:AccessKey`-mønster, ny Bicep-parameter
  `tilbakemeldingAgentNokkel`) er bygget for en PLANLAGT daglig rapport-agent, men selve den
  planlagte jobben (og den separat avtalte, brukervalgte "fullt autonome krasj-fiks-og-deploy-til-
  live"-pipelinen, som krever en egen GitHub Actions-arbeidsflyt + Azure-tjenesteprinsipal) er IKKE
  bygget ennå — se docs/beslutningslogg.md "Tilbakemeldingsverktøy" for full status og hva som
  gjenstår. Verifisert lokalt i nettleser FØR deploy (inkl. en reell CSS-spesifisitets-fallgruve
  funnet og fikset, se fallgruve-listen), deployet til begge miljøer (traff underveis den kjente
  `azd deploy`-stale-kode-fallgruven på LIVE, løst med en ny `azd deploy`).
- **Tilbakemeldingsverktøy — del 3: daglig rutine + fullt autonom CI/CD-pipeline FERDIG
  (2026-10-01/02):** en ekte, varig Claude Code "routine" (`trig_01WnY5ug8qJegu3DbTub5hC4`, 05:00
  UTC daglig) henter tilbakemeldinger+åpne krasjrapporter, e-poster en norsk rapport, og åpner en
  PR (ALDRI push til master) for krasjrapporter den er trygg nok på å fikse selv. En ny GitHub
  Actions-arbeidsflyt (`.github/workflows/deploy.yml`, Azure-tjenesteprinsipal med OIDC-føderasjon,
  Contributor KUN på de to resource groupene) bygger+tester+deployer til beta, helsesjekker, og KUN
  hvis den består, deployer til live — dette er den faktiske "fullt autonomt til live"-mekanismen
  brukeren valgte, implementert som en deterministisk CI/CD-pipeline (ikke ved å gi selve LLM-
  agenten stående Azure-legitimasjon). Fem reelle, tidligere usette feil funnet og rettet under
  verifisering i en ekte kjøring (ikke bare lest i kildekoden) — se docs/beslutningslogg.md
  "Tilbakemeldingsverktøy, del 3" for alle fem, inkl. en NY fallgruve (MSYS-sti-konvertering
  rammer også `az`-CLI-en, ikke bare `curl`) lagt til i fallgruve-lista under, og at `azd auth
  login` IKKE dekker en separat `az`-CLI-innlogging i samme jobb. Verifisert med en fullstendig
  grønn CI-kjøring PLUSS en uavhengig sjekk utenfor selve pipelinen etterpå.
- **Ekte BankID for admin/behandler, del 5 — koblingsbasert innlogging bygget, IKKE aktivert
  (2026-10-01/02):** brukeren ble godkjent av Idura for produksjon, men EKSPLISITT kun som "ren
  klient, ingen personnummer-scope" — nøyaktig scenarioet "STØ avviste fødselsnummer-bestilling"
  forutså. Bygget: ny `BankIdSubjekt`-kolonne (ukryptert, unik) på Administrator/Behandler, nye
  `FinnVedBankIdSubjektAsync`/`KoblBankIdSubjektAsync`, `ProfesjonellInnloggingService` refaktorert
  med tre inngangspunkter (personnummer/mock, sub-oppslag, kobl-og-fullfør), ny side
  `Pages/Konto/BankIdKobleKonto` (vises automatisk FØRSTE gang en ukjent BankID-sub dukker opp, ber
  om personnummeret brukeren allerede er registrert med, kobler PERMANENT). `BankIdInnlogging`-
  schemaet ber nå kun om `openid+profile` (ikke `ssn`). 5 nye tester (79 totalt). **IKKE aktivert**
  (`Miljo:EktBankIdProfesjonell` fortsatt `"false"` på live) — et REELT, uavklart funn under
  verifisering mot Idura sin TEST-sandkasse viste at den faktiske utgående scope-forespørselen til
  BankID inkluderte `sub_nnin`/`sub_bankid` UANSETT hva koden ber om, trolig konfigurert på Idura-
  klient-nivå, ukjent om samme gjelder produksjonsklienten — MÅ avklares (se docs/beslutningslogg.md
  "Ekte BankID for admin/behandler, del 5" for full analyse) før bryteren skrus på. To nye
  fallgruver lagt til under (JWT "sub"-claim-remapping, og at en brokers scope-forespørsel ikke
  nødvendigvis er klientens egen).

Prosjektet er et Git-repo i `C:\code\TestBase`.

## Arkitektur — kort versjon (full begrunnelse i beslutningsloggen)

- **Backend:** ASP.NET Core (C#), .NET 8. Razor Pages.
- **Database:** MySQL via Entity Framework Core + Pomelo-provider, EF Core migrations for skjemaversjonering.
- **Produksjon:** Azure App Service + Azure Database for MySQL – Flexible Server. Test-miljøet er satt opp via `azd` (se `azure.yaml`/`infra/`) og kjører i Sweden Central, ikke det opprinnelig planlagte Norway East/West — regional MySQL-kapasitet manglet der (se `docs/beslutningslogg.md`, "Sky-deploy til Azure (azd)"); må revurderes før reell produksjonssetting med ekte pasientdata. IKKE egen Windows Server/IIS — det opprinnelige kravet om dette er revidert bort.
- **Tre miljøer, ikke to (2026-09-16):** `testbase-test` (azd-miljønavn, men tross navnet den
  faktiske LIVE-siden, `www.psytest.no`) + et nytt `testbase-beta` (`beta.psytest.no`, DNS ikke
  fullført ennå) mellom lokal dev og live — full klone av samme Bicep-mal/App
  Service-/MySQL-SKU-er, egen ressursgruppe (`rg-testbase-beta`), SAMME `StagingGate`-nøkkel som
  live. Beta har en runtime-bryter (`Miljo:ErBeta`, Admin/MinSide, kun Superadmin/Utvikler) som
  bytter Vipps mellom Mock/Test(Vipps sin egen sandkasse)/Produksjon(SAMME ekte konto som live) og
  Stripe mellom Mock/Test, uten omstart — se `BetaSwitchingVippsClient`/`BetaSwitchingStripeClient`
  og `docs/beslutningslogg.md` "Beta-miljø". `azd env select testbase-beta` er nå DEFAULT aktivt
  miljø i dette repoet — bruk eksplisitt `azd env select testbase-test` for å nå live. Nattlig
  database-synk produksjon→beta er avtalt, men IKKE bygget ennå (bevart UID, syntetisk
  personnummer generert med BETAS EGEN krypteringsnøkkel — aldri delt fra live — se
  "Beta-miljø" i beslutningsloggen for designet). **Ekte BankID for admin/behandler sin FELLES
  innloggingsside ER bygget OG pushet til BEGGE miljøer** (2026-09-18/19, pasient forblir alltid
  mock) — se `"BankIdInnlogging"`-schemaet i `Program.cs` og
  `TestBase.Web/Security/ProfesjonellInnloggingService.cs`. Styrt av et EGET flagg,
  `Miljo:EktBankIdProfesjonell` — BEVISST IKKE samme flagg som `Miljo:ErBeta` (de to er urelaterte;
  å gjenbruke ErBeta ville aktivert betalingsbryteren — som defaulter til Mock — samtidig på live,
  se `docs/beslutningslogg.md` "Ekte BankID for admin/behandler, del 2"). Verifisert opp til en
  fullstendig, korrekt OIDC-forespørsel til Idura på BÅDE `www.psytest.no` og beta — selve den
  første ekte interaktive innloggingen i nettleser (for å bekrefte at personnummer-claimen faktisk
  heter "ssn") gjenstår, må gjøres av bruker selv. **StagingGate FJERNET fra live 2026-09-20**
  (bevisst brukerbeslutning — "that is the point of live"), BEHOLDT UENDRET på beta. Live kjører
  fortsatt `ASPNETCORE_ENVIRONMENT=Development` (kun for auto-migrering), men et nytt, EGET flagg
  `Miljo:TillatUtviklingsSnarveier` (`TestBase.Web/Security/Miljo.cs`) — sant lokalt og på beta,
  usant på live — gater nå ALLE auth-relaterte utviklingssnarveier som før hang på
  `IsDevelopment()` alene: `PersonnummerOverride` (begge innloggingssider), 2FA-kode vist i
  klartekst, de seks diagnostiske BankID-/betalingstestsidene, og `UseHsts`/`UseExceptionHandler`
  (nå faktisk aktive på live for første gang). Underveis oppdaget og lukket: `Pages/Konto/
  LoggInn.cshtml.cs` sin AdminId+passord-innlogging hadde ALDRI vært gatet i selve handleren (kun
  skjult i viewet) — en `dev-admin`/`utvikler123`-konto (hardkodet passord, synlig i denne
  offentlige repoen) kunne dermed ha gitt full utvikler-tilgang til hvem som helst på internett i
  det StagingGate falt bort. Se `docs/beslutningslogg.md` "StagingGate fjernet fra live, nytt flagg
  for utviklingssnarveier" for full liste over hva som ble funnet og fikset, og et gjenstående
  oppfølgingspunkt (sjekk `/Administratorer` manuelt for en gjenværende "dev-admin"-rad i live sin
  database). **Rettet samme dag:** `Miljo:TillatUtviklingsSnarveier=false` gatet også
  `PersonnummerOverride`, men `MockBankIdProvider` er den ENESTE `IBankIdProvider` som noensinne
  registreres — uten override kan INGEN ekte administrator/behandler/pasient logge inn før
  `Miljo:EktBankIdProfesjonell` er skrudd på (ekte BankID-avtale ~13 dager unna). Nytt, snevrere
  flagg `Miljo:TillatPersonnummerOverride` (KUN dette feltet, MIDLERTIDIG `"true"` på live) gjeninnfører
  personnummer-feltet uten å røre `TillatUtviklingsSnarveier` — AdminId+passord-bypasset og de
  diagnostiske sidene forblir stengt på live. **MÅ settes tilbake til `"false"`
  (`MILJO_TILLAT_PERSONNUMMER_OVERRIDE`) den dagen ekte BankID er verifisert på live** — se
  `docs/beslutningslogg.md` "PersonnummerOverride midlertidig gjeninnført på live".
- **Lokal utvikling:** Docker Compose (lokal MySQL-container) + `dotnet watch run`. Bevisst holdt enkelt og sky-fritt for rask iterasjon.
- **Sikkerhetsprinsipp — arkitektur nå, infrastruktur senere:** Tilgangsstyring og audit-logging er bygget inn i kodearkitekturen fra dag én (`TestBase.Shared/Security/`: `ICurrentUserContext`, `IAuditLogger`) og er aktiv i ALLE miljøer, også lokalt — men peker på enkle lokale dummy-nøkler i dev og ekte Azure Key Vault/IAM i prod. Følg dette mønsteret videre: ny sikkerhetsrelatert kode skal alltid være aktiv i dev også, bare med enklere infrastruktur bak.
- **Eksterne leverandører (BankID, Vipps, SMS, e-post, betaling):** BankID og Vipps har fortsatt ingen PRODUKSJONSAVTALER (Vipps sin ekte ePayment API-integrasjon, se under, er kodeklar men uverifisert live siden Vipps sitt sandkassemiljø krever et godkjent kunde-/partnerforhold, ikke selvbetjent som Idura). Stripe (kort/Apple Pay/Google Pay) har en ekte, selvbetjent test-integrasjon — se `IStripeClient`/`StripePaymentClient` og beslutningsloggen "Vipps + Stripe (Apple Pay/Google Pay)". E-post (Azure Communication Services) og SMS (Vonage) har begge en ekte, fungerende integrasjon i Azure test-App Service nå — se beslutningsloggen under "Ekte e-postutsending via Azure Communication Services" og "SMS-integrasjon: byttet fra Azure til Vonage". All kode mot disse går bak grensesnitt (`IBankIdProvider`, `IVippsClient`, `ISmsSender`, `IEmailSender`) med mock-implementasjoner i `TestBase.Shared/Providers/Mock/` som brukes lokalt uansett, slik at utvikling ikke er avhengig av ekte avtaler/kontoer. Se `/DevDemo`-siden for eksempel på bruk. I tillegg finnes det siden 2026-09-05 en ekte, gratis Idura BankID-TEST-integrasjon (`/DevDemo` → "Test ekte BankID (Idura)") — kun et diagnostisk sideverktøy, IKKE koblet til `IBankIdProvider`/den faktiske innloggingsflyten, se beslutningsloggen "BankID-testintegrasjon via Idura".
- **Ingen ekte pasientdata i dev/test noensinne** — kun syntetiske testdata.

## Prosjektstruktur

```
TestBase.sln
src/
  TestBase.Web/          ASP.NET Core Razor Pages-app (inngangspunkt)
    Program.cs             Wiring: DB, DataProtection, cookie-auth (delt mellom begge portaler,
                           ruter til riktig LoginPath basert på sti), autorisasjon, dev-seed
    Pages/Konto/           Samlet innlogging for administrator OG behandler (LoggInn/BekreftKode/
                           LoggUt) — fase 6-designomgang, se beslutningsloggen "Offentlig design +
                           samlet profesjonell innlogging". ÉN BankID-knapp, ingen rollevalg:
                           finner personen via personnummer og logger inn på høyeste rolle
                           (administrator før behandler). AdminId+passord (kun utviklingsmiljø)
                           er et sekundært ett-stegs alternativ på samme side. Pasient er bevisst
                           IKKE med her — egen inngang, se Areas/Pasientportal og Pages/Pasienter.cshtml.
    Pages/Pasienter.cshtml Offentlig landingsside for pasienter, separat fra forsiden ("/") som nå
                           er admin/behandler sin inngang — lenker til Areas/Pasientportal/Konto/LoggInn.
    Areas/Admin/Pages/     Admin-portalen: Konto/ByttModus (rollebytte, dev-only),
                           Administratorer (Index/Ny/Rediger), Behandlere (Index m/ HPR-godkjenning/Inviter)
                           — beskyttet av "AdminOmrade"-policyen, se Program.cs. Innlogging skjer nå
                           via Pages/Konto (se over), ikke en egen side i dette Area-et.
    Areas/Behandlerportal/Pages/  Behandler-portalen (fase 3) — MERK: heter "Behandlerportal",
                           ikke "Behandler", for å unngå at Area-navnerommet skygger for
                           domenetypen Behandler (se beslutningsloggen "Del 3 (slice 1)").
                           Konto/GodkjennAvtale (BankID+2FA kun, intet passord — selve
                           innloggingen skjer via Pages/Konto, se over), Behandlere/Inviter
                           (kollega), Pasienter (Index/Ny/Rediger/Gruppeimport/Detaljer m/ testtildeling)
                           — "BehandlerOmrade"-policyen
    Areas/Pasientportal/Pages/  Pasientportalen (fase 4) — samme navngivningsprinsipp som
                           Behandlerportal. Konto (LoggInn/LoggUt — BankID KUN, ingen 2FA, EGEN
                           inngang atskilt fra admin/behandler sin samlede Pages/Konto),
                           MinSide (tildelte tester), Tester/Fyll (side-for-side utfylling) —
                           `[Authorize(Policy = "PasientOmrade")]` direkte på de to sidene
                           (for få sider til å rettferdiggjøre AuthorizeAreaFolder)
    Areas/Admin/Pages/Tester/  Admin forfatter tester (fase 4): Index/Ny/Rediger/Sider/Ledd — Rediger
                           dekker kun testens egne felt (navn/beskrivelse/belønningstekst/aktiv),
                           ingen rediger/slett av sider/ledd ennå. Index har en "Regenerer innebygde
                           tester"-knapp (fase 5) som kjører alle IInnebygdTestSeeder på nytt
    Areas/Behandlerportal/Pages/Pasienter/Rapport.cshtml  Skårings-/rapportside (fase 5): per
                           besvarelse (råskår/prosentskår/fortolkning/svartabell) + "utvikling
                           over tid" ved flere fullførte besvarelser av samme test. Fase 6: behandler
                           må godkjenne (RapportGodkjentUtc) før valgfri deling til pasient
                           (RapportSynligForPasient) — se Pasientportal/Pages/Tester/Rapport.cshtml
                           for pasientens lesetilgang
    Areas/*/Pages/MinSide.cshtml  DETTE er oppgavelisten (fase 6) i alle tre Areas — helt ulikt
                           innhold per rolle, ikke en separat "/Oppgaver"-side (se rettelse i
                           statusseksjonen over). Admin: HPR-frist utløpt + ventende
                           test-tilgangsforespørsler (fase 6, 2026-09-15, bulk godkjenn/avvis).
                           Behandler: HPR-frist for kolleger, ugodkjente fullførte rapporter (se
                           TestService.HentUgodkjenteFullforteForBehandlerAsync), meldingsinnboks
                           (BehandlerMelding/BehandlerMeldingService), ikke-fullførte tildelinger.
                           Behandlerportal fikk også egen Innstillinger.cshtml (daglig
                           påminnelse-preferanser, se PaaminnelseService)
    Areas/Admin/Pages/Tildel/ og Areas/Behandlerportal/Pages/Tildel/  Tildelingsflyt (fase 6):
                           Pasienter.cshtml (steg 1, velg pasienter — admin ser alle, behandler
                           kun egne) → Tester.cshtml (steg 2, kategori-tre + dialog-oppsummering +
                           send). Nesten identiske sidepar per Area (samme mønster som de separate
                           LoggInn-sidene per portal) som begge kaller inn i den delte
                           TestTildelingsService i TestBase.Shared
    Pages/                 Forside (admin/behandler-rettet), Pasienter.cshtml (pasient-forside),
                           Personvern.cshtml (cookies), /DevDemo, /health, Inviter/Fullfor+Verifiser
                           (behandler), PasientRegistrering/Fullfor (pasient — én side, ingen
                           kontaktverifisering) — alle offentlige, med enkelt bot-vern
    Security/AuthSignIn.cs   Utsteder innloggingscookien (delt mellom admin passord/BankID+2FA,
                           behandler BankID+2FA, og pasient BankID)
    Security/BotVern.cs      Honeypot + minimumstid-vern for offentlige skjemaer (registrering/
                           invitasjon — se også ICaptchaProvider for innloggingssidenes CAPTCHA)
    Security/StagingGate.cs  Sperre foran hele test-appen (se egen fallgruve under) — unntar
                           BankID-OIDC-callbacken og Vipps/Stripe-webhook-stiene eksplisitt
    Security/PaymentWebhooks.cs  Minimal-API-endepunkter (IKKE Razor Pages, se beslutningsloggen)
                           for Vipps-/Stripe-webhooks, med signaturverifisering
    Pages/BetalingTest/      Diagnostisk test av ekte Vipps-/Stripe-betaling (samme mønster som
                           Pages/BankIdTest/) — IKKE koblet til noen reell betalingsflyt i
                           pasientsystemet ennå, se beslutningsloggen
    Properties/launchSettings.json   (setter ASPNETCORE_ENVIRONMENT=Development)
    appsettings.json / appsettings.Development.json
  TestBase.Shared/       Klassebibliotek (har FrameworkReference til Microsoft.AspNetCore.App
                         for Identity/DataProtection/HttpContextAccessor uten å være Sdk.Web)
    Security/             ICurrentUserContext (+ AuthenticatedCurrentUserContext),
                           IAuditLogger/EfAuditLogger, AuditLogEntry, AppClaimTypes,
                           AdminAuthenticationService, BehandlerAuthenticationService,
                           PasientAuthenticationService (BankID uten 2FA),
                           ToFaktorService (delt 2FA-logikk for admin/behandler)
    Domain/Administrasjon/  Administrator, Behandler (utvidet i fase 3), BehandlerInvitasjon,
                             BehandlerKontaktVerifisering, ToFaktorKode, Brukeravtale (versjonert
                             lisensavtale-tekst), BehandlerInvitasjonService
    Domain/Pasienter/       Pasient (utvidet i fase 4, fikk `Varslingspreferanse` i fase 6),
                             PasientStatus, Varslingspreferanse (Sms/Epost/Begge), PasientInvitasjon,
                             PasientBrukeravtale, BiologiskKjonn, Kjonnsidentitet,
                             PasientInvitasjonService
    Domain/Tester/          Testmotor (fase 4 skjelett, fase 5 skåring, fase 6 kategorier+tildelingsflyt):
                             Test (fikk `Kode`, fase 5), TestSide, TestLedd, TestSvartype
                             (`Likert5`→`LikertSkala` i fase 5 — data-drevet N-punkts skala),
                             TestLeddSvaralternativer (parser "verdi:tekst"-par), TestTildeling
                             (fikk nullable `TildeltAvAdministratorId` ved siden av det nå nullable
                             `TildeltAvBehandlerId` i fase 6), TestTildelingStatus, TestSvar,
                             TestKategori/TestKategoriKobling (fase 6, mange-til-mange), TestService
                             (forfatning + tildeling + utfylling + skåring + regenerering +
                             kategorier), TestTildelingsService (fase 6: bulk tildeling på tvers
                             av valgte pasienter × tester + varsling)
    Domain/Tester/Skaaring/  Skåringsmotor (fase 5): TestSkaaring (record),
                             ITestSkaaringsberegner, Who5Skaaringsberegner
    Domain/Tester/InnebygdeTester/  Regenereringsmekanisme (fase 5): IInnebygdTestSeeder,
                             Who5TestSeeder — idempotent, kalt fra dev-seed OG admin-knapp
    Providers/             IBankIdProvider, IVippsClient, IStripeClient, ISmsSender,
                           IEmailSender, AzureEmailSender (ekte e-post via Azure
                           Communication Services, se beslutningsloggen "Ekte
                           e-postutsending via Azure Communication Services"),
                           VonageSmsSender (ekte SMS via Vonage sitt Messages API, IKKE
                           Azure — Norge manglet i praksis en fungerende selvbetjent
                           alfanumerisk avsender-ID-flyt i Azure Portal, se
                           beslutningsloggen "SMS-integrasjon: byttet fra Azure til
                           Vonage"), VippsPaymentClient (ekte Vipps ePayment API,
                           asynkron opprett+status, IKKE en synkron "charge"),
                           StripePaymentClient (ekte Stripe via Stripe.net — kort/Apple
                           Pay/Google Pay, se beslutningsloggen "Vipps + Stripe (Apple
                           Pay/Google Pay)")
    Providers/Mock/        Mock-implementasjoner av alle fem grensesnitt — fortsatt
                           brukt lokalt for alle, og i Azure for det som ikke er
                           konfigurert med ekte legitimasjon (App Service-innstillinger,
                           se Program.cs)
    Data/AppDbContext.cs   EF Core-kontekst — ALL databasetilgang skal gå gjennom denne.
                           Personnummer krypteres i hvile via DataProtection (se beslutningsloggen)
    Migrations/            EF Core migrations (generert med dotnet ef)
docker-compose.yml        Lokal MySQL
docs/                      Se over
```

Admin-, behandler- og pasientflatene endte alle opp som Razor Pages Areas (`Areas/Admin`,
`Areas/Behandlerportal`, `Areas/Pasientportal`) inni `TestBase.Web`, ikke egne prosjekter.
**Viktig:** ikke gi et fremtidig Area samme navn som en domeneentitet (f.eks. ikke `Areas/Test`
hvis `Test`-klassen brukes ukvalifisert i kode nestet under `Areas/*`) — se fallgruven under.
`TestBase.TestEngine` som eget prosjekt ble aldri opprettet — testmotoren ble en mappe
(`Domain/Tester/`) i `TestBase.Shared` i stedet, samme mønster som resten av domenet.

## Kjøre lokalt

```
docker compose up -d
cd src\TestBase.Web
dotnet ef migrations add <Navn> --project ..\TestBase.Shared --startup-project .   # kun ved skjemaendringer
dotnet ef database update --project ..\TestBase.Shared --startup-project .
dotnet watch run
```

`launchSettings.json` er på plass, så `dotnet watch run` skal åpne nettleseren automatisk. Prøv `/DevDemo` og `/health` for å bekrefte at alt fungerer.

## Kjente fallgruver (alle støtt på og løst under Del 1 — se beslutningsloggen for detaljer)

- Docker Desktop kan feile med "Virtualization support not detected" selv om BIOS-virtualisering er på — da mangler Windows-funksjonene `VirtualMachinePlatform`/`Microsoft-Windows-Subsystem-Linux`.
- `dotnet ef`-kommandoer feiler med tilkoblingsfeil hvis Docker/MySQL-containeren ikke er startet først (`ServerVersion.AutoDetect` i `Program.cs` krever en faktisk tilkobling).
- Kjør aldri `dotnet ef database update` i et annet vindu mens `dotnet watch run` kjører samtidig — build-output-filene er låst. Stopp `dotnet watch run` midlertidig først.
- Razor Pages' standard `TempData`-serialisering støtter ikke `long` (kaster `InvalidOperationException` ved lagring) — lagre som `string` og parse tilbake med `long.TryParse`, se `Pages/Konto/LoggInn.cshtml.cs`/`BekreftKode.cshtml.cs`.
- Personnummer og andre DataProtection-krypterte kolonner kan IKKE slås opp med SQL `WHERE` eller håndheves unikt med en databaseindeks (krypteringen er ikke deterministisk) — sammenlign i minnet i stedet, se `AdminAuthenticationService.FinnVedPersonnummerAsync`/`BehandlerAuthenticationService.FinnVedPersonnummerAsync`.
- `MockBankIdProvider` returnerer alltid SAMME faste personnummer — siden `Pages/Konto/LoggInnModel` nå slår opp administrator FØR behandler ("høyeste rolle", se beslutningsloggen), vil en administrator og en behandler med dette faste personnummeret kollidere: BankID-innlogging finner alltid administratoren. Ved manuell/automatisert testing av behandler-BankID-innlogging må en eventuell administrator-testkonto med samme personnummer arkiveres/fjernes først (se `HeleFlytenTests.cs` for mønsteret).
- Å navngi et Razor Pages Area likt en domeneentitet (f.eks. `Areas/Behandler` når klassen `Behandler` finnes) gjør at C#s navneromsoppslag lar Area-navnerommet skygge for typen i ALL kode nestet under `Areas/*` — kompilatorfeil `CS0118 '<Navn>' is a namespace but is used like a type`. Løst ved å kalle arealet `Behandlerportal` i stedet. Ikke gjenta mønsteret for fremtidige Areas.
- `dotnet ef migrations add` kan feiltolke en kolonne-fjerning + en urelatert ny kolonne som en **rename** når flere kolonner endres samtidig på samme tabell (så skjedde med `FulltNavn`→`Arbeidsadresse` på `behandlere` i fase 3-migrasjonen — ville ha flyttet data feil vei). Les alltid gjennom en generert migrasjon med flere samtidige kolonneendringer før den kjøres; fiks manuelt til drop+add hvis feltene ikke faktisk er samme data.
- Razor Pages' automatiske antiforgery-token vises IDENTISK i flere `<form>`-elementer på samme side (f.eks. én per rad i en tabell) — ved skripting/testing med curl: bruk `grep -o ... | head -1` for å hente kun ÉN forekomst før bruk. Fanger man opp alle forekomster i én shell-variabel, får man et flerlinjers, korrupt token og et 400-svar som ser ut som en ekte antiforgery-feil, men ikke er det.
- Et Area kan trygt hete "Tester" (flertall) selv om domenetypen heter "Test" (entall) og bor i navnerommet `TestBase.Shared.Domain.Tester` — kollisjonsregelen over krever et EKSAKT navnematch mellom navnerom-segment og typenavn, og "Test" ≠ "Tester". Bekreftet trygt i fase 4 (`Areas/Admin/Pages/Tester/`, `Areas/Pasientportal/Pages/Tester/`).
- Bash-tool-kall deler IKKE shell-variabler mellom separate kall (kun working directory bevares) — hvis du henter en CSRF-token/tidsstempel i ett `Bash`-kall og prøver å bruke variabelen i et senere kall, er den tom. Gjør GET+utvinning+POST i SAMME kall (eller samme shell-script) når du tester skjemaer med curl.
- `curl` følger IKKE redirects som standard — en `302`-respons uten `-L` gir en TOM body i `-o`-filen din. Bruk `-D -` for å se `Location`-headeren, og gjør en eksplisitt oppfølgende GET selv (eller legg til `-L`) hvis du trenger innholdet på redirect-målet.
- Å legge til en NY valgfri parameter et sted MIDT i en eksisterende metodesignatur (før eksisterende parametre, selv med default-verdi) knekker eksisterende POSISJONELLE kall på det stedet — C# binder positional args til ny rekkefølge, ikke navn, så et 4. positional argument som før traff `cancellationToken` kan plutselig treffe den nye parameteren i stedet (`CS1503`). Bruk et navngitt argument (`cancellationToken: ct`) på eksisterende kallsteder i stedet for å regne med at posisjon fortsatt stemmer, eller legg den nye parameteren sist.
- Razor-filer: skriv IKKE `@{ ... }` rundt en enkelt C#-setning når du allerede ER i en ren C#-kodeblokk (f.eks. rett etter en `</tag>` inni en `@foreach { }`) — gir `RZ1010 Unexpected "{" after "@"`. `@{` trengs kun for å SWITCHE fra markup til kode, ikke inni kode som allerede er kode.
- Razors "betinget attributt"-oppførsel (et `bool`-typet `@(...)`-uttrykk som HELE verdien av et rent HTML-attributt render en MINIMERT boolsk form — `attributtnavn="attributtnavn"` når true, attributtet utelates helt når false) gjelder for ALLE slik bundne attributter, ikke bare ekte boolske HTML-attributter (`disabled`/`checked`). Et skjult felt som `value="@(!Model.X.Bool)"` render bokstavelig `value="value"` i stedet for `value="True"` — ser riktig ut ved rask titt på Razor-kilden, men knekker server-side bool-modellbinding fullstendig. Bruk eksplisitt `.ToString()` på slike uttrykk for et skjult felt/en ikke-boolsk attributt. Sjekk generert HTML, ikke bare kildekoden.
- Etter en Area-omdøping (f.eks. fase 3s `Behandler`→`Behandlerportal`): kjør `grep -r 'href="/GamleNavn/'` over HELE `src/`, ikke stol på å ha funnet alle harde lenker manuelt. Fire slike lenker (`/Behandler/Pasienter/...` i stedet for `/Behandlerportal/Pasienter/...`) overlevde fra fase 3 til fase 5 og ga 404 på "Legg til pasient" — fase 4s opprydding fanget kun ett av flere tilsvarende tilfeller.
- Mock-leverandørene (`MockSmsSender`/`MockEmailSender`) logger KUN via `ILogger` — usynlig i selve nettleser-UI-et, kun synlig i konsollen der `dotnet watch run` kjører. En invitasjonslenke som kun finnes der er i praksis ubrukelig for reell manuell testing i nettleser. Slike tjenester bør returnere lenken/meldingen til kalleren (se `BehandlerInvitasjonResultat`/`PasientInvitasjonResultat` i fase 5s feilrettinger) slik at UI-et kan vise den direkte, i tillegg til mock-loggingen.
- Hvis nettleser-testing ikke reflekterer nylige kodeendringer selv om `dotnet watch run` "kjører": sjekk (1) at nettleseren faktisk peker på porten fra `Properties/launchSettings.json` (`https://localhost:7257`/`http://localhost:5257`) og ikke en gammel manuelt overstyrt port fra en tidligere økt, og (2) om flere/hengende `TestBase.Web.exe`-prosesser (`tasklist`, `netstat -ano | grep <port>`) låser build-outputen uten selv å svare på riktig port — drep de gamle prosessene og start `dotnet watch run` på nytt uten portoverstyring.
- Git Bash (MSYS) konverterer automatisk et kommandolinje-argument som begynner med `/` (f.eks. `curl --data-urlencode "ReturnUrl=/Pasientportal/..."`) til en Windows-sti FØR curl noensinne ser det — verdien som faktisk sendes blir korrupt (`C:/Program Files/Git/Pasientportal/...`), noe som ser ut som en server-side bug (feltet "bindes ikke") men egentlig er testverktøyet som lyver om hva som ble sendt. Sett `MSYS_NO_PATHCONV=1` foran curl-kommandoer som poster verdier med innledende skråstrek.
- Enhver App Service-innstilling satt kun via `az webapp config appsettings set` (utenfor
  `infra/resources.bicep`) forsvinner SPORLØST ved neste `azd provision` — `siteConfig.appSettings`
  på en `Microsoft.Web/sites`-ressurs er en FULL erstatning av hele innstillingssamlingen, ikke en
  sammenslåing. Skjedde reelt 2026-09-03: `StagingGate__AccessKey` ble borte og test-appen sto åpen
  for internett i noen minutter etter en `azd provision` for å legge til nye ressurser (se
  beslutningsloggen). Enhver innstilling som må overleve, MÅ inn i Bicep sin `appSettings`-liste —
  bruk en `@secure()`-parameter koblet til en azd-miljøvariabel (`azd env set NAVN verdi`,
  `main.parameters.json` sin `"${NAVN}"`-syntaks) for hemmeligheter som ikke skal ligge som literal
  i kildekontroll, ALDRI en hardkodet verdi i selve Bicep-filen.
- Å skjule et skjemafelt i en Razor-view med `@if (Env.IsDevelopment())` gater KUN visningen — det gater IKKE selve POST-handleren. `Pages/Konto/LoggInn.cshtml.cs` sin `OnPostAsync` kaller `StartBankIdAsync(personnummerOverride: PersonnummerOverride, ...)` ubetinget, og `MockBankIdProvider` honorerer en hvilken som helst oppgitt streng — så en rå POST med `PersonnummerOverride=<kjent-testpersonnummer>` er et fullverdig auth-bypass uansett `ASPNETCORE_ENVIRONMENT`, oppdaget da test-App Service-en sto offentlig tilgjengelig (se beslutningsloggen "Google Chrome/Safe Browsing flagget test-appen"). Ethvert fremtidig dev-only felt av denne typen må gates i selve handleren (`if (!_env.IsDevelopment()) { ignorer verdien }`), ikke bare i viewet — spesielt for alt som kan nå et miljø som er reachable utenfor localhost.
- Manuell `dotnet run --no-launch-profile --urls "http://localhost:5257"` kan likevel plutselig begynne å 307-redirecte til `https://localhost:7257` (via `app.UseHttpsRedirection()`, som er ubetinget i `Program.cs` — kun `UseHsts`/`UseExceptionHandler` er bak `!IsDevelopment()`) selv når ingen launch-profil brukes. Sett `ASPNETCORE_HTTPS_PORT=` (tom) i tillegg til `--urls` for å hindre at middlewaren likevel klarer å gjette et https-mål å omdirigere til.
- Et OIDC-endepunkt (BankID via Idura, se beslutningsloggen "BankID-testintegrasjon via Idura") som bruker `response_mode=form_post` mottar en cross-site POST fra identity-providerens domene på sin `CallbackPath` — en `SameSite=Lax`-cookie (StagingGate sin gate-cookie, MEN også `CorrelationCookie`/`NonceCookie` som ASP.NET Core selv setter opp default på Lax) blir IKKE sendt av nettleseren på en slik cross-site POST. `CorrelationCookie`/`NonceCookie` må settes eksplisitt til `SameSite=None`+`Secure=Always` i OIDC-options, og enhver egen app-gate (som `StagingGate`) trenger et snevert, hardkodet unntak for nøyaktig denne callback-stien — se `StagingGate.cs`. Gjelder enhver fremtidig OIDC-basert integrasjon med `form_post`, ikke bare BankID-testen.
- `Microsoft.AspNetCore.Authentication.OpenIdConnect` er IKKE inkludert i `Microsoft.AspNetCore.App`-shared-framework-referansen (i motsetning til Cookie-autentisering, som er det) — gir `CS0234` med mindre pakken legges til eksplisitt (`dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect`).
- `azd deploy` kan rapportere `SUCCESS` uten at koden faktisk endret seg på kjørende App Service — se etter advarselen `"Deployment completed, but azd observed no App Service deployment status change for ...m"` i loggen (lett å overse når man kun sjekker exit code/siste linje). Løsning var ganske enkelt å kjøre `azd deploy` på nytt; lærdommen er å lese hele deploy-loggen ved uventet oppførsel rett etter en "vellykket" deploy, ikke bare stole på siste linje.
- En `.cshtml`-fil med en tilhørende code-behind-`PageModel` (`X.cshtml` + `X.cshtml.cs`) kobles IKKE automatisk sammen basert på filnavn/mappeplassering alene — `.cshtml`-filen MÅ ha en eksplisitt `@model Fullt.Kvalifisert.NavnPaaModel`-direktiv. Uten den faller Razor Pages tilbake til en implisitt, tom standard-side: siden returnerer stille `200 OK` med TOM body, og `PageModel`-klassens `OnGet(Async)`/`OnPost(Async)` kjører ALDRI — ingen kompilatorfeil, ingen runtime-feil, bare et stille feil resultat som lett tolkes som "gaten fungerte" eller "alt er OK" når det faktisk betyr "koden din kjørte aldri". Skjedde med `Pages/BetalingTest/Vipps.cshtml` (glemte `@model` ved kopiering av `BankIdTest/Start.cshtml`-mønsteret for en side uten synlig markup) — testet ALLTID med et faktisk forventet avvik (f.eks. en gated 404) på en side uten markup, ikke bare en 200/close-enough-status.
- Mange samtidige `dotnet build`/`dotnet run`/`dotnet watch run`-kall i én lang økt etterlater seg lett flere hengende `TestBase.Web.exe`/`dotnet.exe`-prosesser som fortsatt lytter på 5257/7257 fra TIDLIGERE kodeversjoner — påfølgende `curl`-tester mot "localhost" kan da stille treffe en gammel prosess i stedet for den nye, og gi resultater som ser ut som en reell bug i ny kode. Sjekk alltid `tasklist`/`netstat -ano | grep <port>` og drep alle gamle `TestBase.Web.exe`-prosesser (ikke bare anta at forrige `dotnet run`-kommando i samme Bash-kall faktisk avsluttet) før man stoler på et overraskende testresultat — vurder `dotnet clean` også hvis mistanke om stale `obj`/`bin`-artefakter.
- Et `[BindProperty] Dictionary<TKey,TValue>` (eller annen collection-type) PÅ TOPPNIVÅ (en PageModel-egenskap, ikke nøstet i et annet objekt) som IKKE finner noen felt med sitt eget prefiks i det posted skjemaet (f.eks. `HonorarKr[...]` når ingen av radene i skjemaet faktisk har det feltet med i denne innsendingen) faller tilbake til å tolke ALLE ANDRE topnivå-skjemafelt-NAVN på siden som om de var dictionary-nøkler, og kaster `FormatException` når disse navnene ikke kan konverteres til `TKey` (skjedde reelt: `HonorarKr` (Dictionary<long,...>) prøvde å parse skjemafeltet `PasientIderCsv` som en `long`). Dette er en reell ASP.NET Core-modellbindingsfallgruve, IKKE noe som fanges av en test som kaller `OnPost...Async` direkte (slik `BetalingPipelineTests.cs` gjør) — det hopper forbi hele modellbinding-pipelinen. Løsning: ikke bruk `[BindProperty]` på et Dictionary som kan komme inn tomt; les det manuelt fra `Request.Form` i selve handleren i stedet (se `Behandlerportal/Tildel/Tester.cshtml.cs` sin `LesHonorarFraSkjema()`).
- Razors standard `@decimalVerdi.ToString()` (eller rett og slett `@decimalVerdi` uten eksplisitt kultur) inni `value=`/`min=`/`max=` på et `<input type="number">` bruker SERVERENS gjeldende kultur — på en norsk Windows-maskin blir det komma som desimalskilletegn (`"50,00"`). HTML5 `type="number"` krever ALLTID punktum uansett sidespråk, og forkaster en verdi med komma HELT STILLE: feltet vises tomt, ingen konsoll-feil, ingen server-feil. Skjedde reelt på `Admin/Tester/Prising` og `Behandlerportal/MinPartner/Prising` — tidligere lagrede priser (f.eks. 50 kr) forsvant fra visningen, og siden feltet så ut som "aldri satt", var det én "Lagre"-klikk unna å STILLE nullstille en ekte, fungerende pris til 0. Bruk alltid `.ToString(System.Globalization.CultureInfo.InvariantCulture)` for disse tre attributtene på ethvert `type="number"`-felt bundet til en `decimal`/`double`/`float`. Ren visningstekst utenfor et faktisk skjemafelt (f.eks. "maks 50,00 kr" i en `<label>`) skal IKKE endres — komma er riktig der, det er kun de maskinlesbare HTML5-attributtene som må være invariant-formatert.
- Et `submit`-event-lyttere som SYNKRONT setter `disabled = true` på DEN KLIKKEDE innsendingsknappen (f.eks. for å hindre dobbeltklikk, se `wwwroot/js/validering.js` sin `data-disable-on-submit`-håndtering) gjør at nettleseren stille UTELATER akkurat den knappens eget navn/verdi-par fra selve POST-en — nettleseren bygger skjemaets entry-list ut fra knappenes tilstand PÅ INNSENDINGSTIDSPUNKTET, ikke slik den var da submit-eventet ble trigget. Rammer ethvert flerknapps-skjema der server-siden grener på hvilken knapp som ble trykket (f.eks. `Handling="Ferdig"` vs. `"Neste"` på `Pasientportal/Tester/Fyll`) — serveren mottar `Handling=null` og faller til en default-gren i stedet. Oppdaget 2026-09-20: "Ferdig" markerte ALDRI en test som fullført i en ekte nettleser, kun maskert fordi `HeleFlytenTests.cs` poster skjemadata direkte og aldri kjører klientsidens JS. Fiks: utsett selve `disabled = true` til `setTimeout(fn, 0)` (neste task) — nettleseren rekker da å lese knappens navn/verdi FØRST, mens lastetekst-visningen fortsatt skjer umiddelbart. Enhver test av et slikt flerknapps-skjema MÅ verifiseres i en ekte nettleser (Playwright), ikke bare via en test som poster skjemafelter direkte.
- En ny mappe under `Areas/Admin/Pages/` eller `Areas/Behandlerportal/Pages/` er IKKE automatisk beskyttet av noen policy — Razor Pages har INGEN autorisasjon som standard, og denne kodebasen bruker `options.Conventions.AuthorizeAreaFolder(...)`-lister i `Program.cs` (ikke per-side `[Authorize]`) for å beskytte hver mappe. Glemmer man å legge til en ny mappe i denne listen, er den 100% offentlig tilgjengelig for en HELT uautentisert besøker — ingen kompilatorfeil, ingen runtime-advarsel. Skjedde reelt med `Grupper`-mappen i BEGGE Areas gjennom hele fase 1+2 av gruppesystemet (oppdaget 2026-09-20, etter at StagingGate var fjernet fra live og dermed ikke lenger maskerte det) — bekreftet med en rå `curl` (uten cookie) mot `/Behandlerportal/Grupper/Ny` som ga `200 OK` i stedet for en redirect til innlogging. Sjekk ALLTID at en ny sidemappe er lagt til i riktig `AuthorizeAreaFolder`-liste FØR den regnes som ferdig, og verifiser med en uautentisert `curl`-forespørsel (forvent `302`, ikke `200`), ikke bare ved å teste som innlogget bruker i nettleseren.
- EF Core kan IKKE oversette en `OrderBy`/`Where` på en BEREGNET C#-property (en `=>`-uttrykksbundet getter som kombinerer flere kolonner, f.eks. `Visningsnavn => $"{Fornavn} {Etternavn}"`) til SQL — kaster `InvalidOperationException` ved spørringsoversettelse (500-feil ved sidevisning), selv om akkurat samme property brukes helt trygt EFTER `.ToListAsync()` (LINQ-to-Objects, ikke LINQ-to-Entities). Skjedde reelt i `Admin/Grupper/Ny.cshtml.cs` (`_db.Behandlere.OrderBy(b => b.Visningsnavn)` — fanget lokalt via Playwright før deploy, se beslutningsloggen "Admin fikk full CRUD på Grupper"). Hent listen FØRST via `ToListAsync()`, sorter/filtrer på beregnede propertyer i minnet ETTERPÅ.
- ASP.NET Cores modellbinding konverterer et INNSENDT MEN TOMT skjemafelt til `null` for en `[BindProperty] string`-property — IKKE til `""`, UANSETT hvilken C#-defaultverdi (`= string.Empty`) propertyen har. Rammer ethvert valgfritt tekstfelt der property-typen er ikke-nullbar `string`: en `required string`/`NOT NULL`-kolonne nedstrøms (f.eks. `Pasient.Email`) får da `DbUpdateException`/500 ("Column 'X' cannot be null") så snart feltet faktisk står tomt — selv om ALDRI EN ENESTE linje kode eksplisitt satte noe til `null`. Skjedde reelt i `BliPasient/Index.cshtml.cs` og `PasientRegistrering/FullforProfil.cshtml.cs` (2026-09-20, se beslutningsloggen "Forenklet QR-registrering") da e-post ble gjort valgfritt — fanget ved å faktisk teste "kun telefon, ikke e-post" i nettleser, ikke bare "alle felt utfylt". Ethvert valgfritt tekstfelt MÅ deklareres `string?` på PageModel-en (ikke `string` med default `""`), med en eksplisitt `?? string.Empty`/tilsvarende konvertering ved kallet til laget under som fortsatt krever en ikke-nullbar streng.
- Razors spesial-håndterte `<text>`-pseudo-tag (kun ment for å bryte ut av markup til ren kode inni en `@foreach`/`@if`-blokk) kan IKKE bære attributter i det hele tatt — heller ikke med en annen store/små bokstaver-variant (`<TEXT>` gir samme `RZ1023`-feil). Rammer et hvilket som helst forsøk på å bygge et ekte SVG `<text>`-element (akseetiketter i et graf/plott) direkte i Razor-markup. Løsning: bygg elementet som en plain C#-streng og skriv den ut via `@Html.Raw(...)` i stedet, se `Grupper/Aggregert.cshtml` sitt spredningsplott (docs/beslutningslogg.md "Gruppe-rapportgenerator"). Samme sted ble en beslektet fallgruve funnet: `@(uttrykk).Metode(...)` (eksplisitt parentes rundt `@`) avslutter selve Razor-uttrykket ved den lukkende parentesen — alt etter, inkludert `.ToString(...)`, blir literal HTML-tekst i stedet for en del av C#-uttrykket. Kun IMPLISITTE uttrykk (`@verdi.Metode(...)`, uten omsluttende parentes) lar et kjede av medlemstilgang/metodekall henge med; et eksplisitt uttrykk må ha hele kjeden inni parentesen: `@((uttrykk).Metode(...))`.
- Et bokstav-tegn UMIDDELBART etterfulgt av `@variabel` uten mellomrom, inni HTML-elementinnhold (f.eks. `<div>Gjennomsnitt@maksSuffiks</div>`), tolkes IKKE pålitelig som en Razor-kodeovergang — renderer bokstavelig `"Gjennomsnitt@maksSuffiks"`, INKLUDERT selve `@`-tegnet, i stedet for å interpolere variabelens verdi. Ingen kompilatorfeil, ingen runtime-feil, bare feil tekst på skjermen (bekreftet via skjermbilde i `Grupper/Aggregert.cshtml`, 2026-09-23, se "Histogrammet byttet fra alltid-prosent til råskår-som-standard" i beslutningsloggen). Et EKSPLISITT uttrykk rett før `@` (`@(uttrykk)@variabel`, parentes-tegn) er IKKE rammet — kun bokstav-rett-før-`@` er det. Løsning: bygg hele strengen som en frittstående C#-variabel FØRST (`var etikett = "Gjennomsnitt" + maksSuffiks;`), og referer den som et frittstående `@etikett`-uttrykk med ingen tilstøtende bokstavtekst.
- En unqualified CSS-regel som `.mittElement { display: flex }` har SAMME spesifisitet (0,1,0) som nettleserens innebygde `[hidden] { display: none }`-regel — siden forfatter-CSS alltid kommer etter UA-stilarket i kaskaden, VINNER `display:flex` over `[hidden]` uansett rekkefølge i egen fil, så et element med `hidden`-attributtet er likevel SYNLIG hvis noen egen klasse på det unconditionally setter `display`. Ingen konsollfeil, bare et element som vises når det ikke skal (skjedde reelt med `.tbm-meny`/`.tbm-mini` i tilbakemeldingswidgeten, 2026-09-28 — begge synlige samtidig med hovedknappen ved ren sidelasting, før noe klikk). Løsning: en eksplisitt `.mittElement[hidden] { display: none; }`-regel (høyere spesifisitet, 0,2,0) — samme mønster som allerede fantes for `.cookie-banner[hidden]` men ikke fulgt konsekvent for den nye widgeten. Sjekk ALLTID i nettleser (skjermbilde), ikke bare ved å lese CSS-kilden — bugen er usynlig fra koden alene.
- `azd deploy` sin kjente "rapporterer SUCCESS uten at koden faktisk endret seg"-fallgruve (se lenger opp i denne lista) viser IKKE alltid den forventede "azd observed no App Service deployment status change"-advarselen i loggen — en kjøring uten den advarselen kan likevel ha latt den GAMLE koden bli stående (skjedde reelt på LIVE 2026-09-28, mens BETA sin kjøring SAMME dag viste advarselen men faktisk hadde lykkes). Advarselen er altså ikke en pålitelig indikator i seg selv. Stol i stedet på en FUNKSJONELL sjekk av noe som kun finnes i den nye koden (f.eks. et nytt API-endepunkt som skal returnere 200, ikke 404) — ikke bare fravær/nærvær av advarselen, og ikke bare en generisk helse-sjekk som fortsatt ville returnert 200 fra den gamle koden.
- Git Bash (MSYS) sin kjente automatisk-konverter-en-innledende-skråstrek-til-en-Windows-sti-fallgruve (dokumentert lenger opp for `curl`) rammer OGSÅ `az`-CLI-en — `az role assignment create --scope "/subscriptions/..."` ga en kryptisk `MissingSubscription`-feil fra Azure sin REST-API i stedet for noe som pekte mot MSYS. `--debug` avslørte at den faktiske forespørselen gikk til `https://management.azure.com/C:/Program Files/Git/subscriptions/...` — altså at `/subscriptions/...`-argumentet ble konvertert til en Windows-sti FØR `az` i det hele tatt så det (2026-10-01/02, se docs/beslutningslogg.md "Tilbakemeldingsverktøy, del 3"). Samme løsning som for `curl`: prefiks med `MSYS_NO_PATHCONV=1`. Gjelder trolig ethvert kommandolinjeverktøy som mottar et argument med innledende `/`, ikke bare disse to.
- `azd auth login` autentiserer KUN `azd` selv — en separat `az`-CLI-kommando i SAMME jobb/skript (f.eks. i en GitHub Actions-steg) har en HELT ANNEN credential-store og er fortsatt helt uinnlogget, selv rett etter en vellykket `azd auth login`. Et `az`-kall feiler da stille med en autentiseringsfeil som lett tolkes som noe annet hvis stderr undertrykkes (skjedde reelt i `.github/workflows/deploy.yml` sin helsesjekk, som brukte `az webapp show` etter kun `azd auth login` — løst med en egen `azure/login@v2`-innlogging for `az`-CLI-en ved siden av). Trenger man BEGGE verktøyene i samme jobb, må begge logges inn eksplisitt og separat, selv med samme OIDC-legitimasjon.
- `Microsoft.AspNetCore.Authentication.OpenIdConnect` sin `JwtSecurityTokenHandler`-baserte token-validering har DEFAULT inbound-claim-mapping (`MapInboundClaims=true`) som omdøper enkelte STANDARD OIDC-claims (bl.a. "sub" → `ClaimTypes.NameIdentifier`) FØR koden i `OnTokenValidated` i det hele tatt ser `ClaimsPrincipal`-en — men et IKKE-standard claim-navn (som "ssn", brukt tidligere i dette prosjektet) rammes IKKE av denne mappingen og forblir bokstavelig. Kode som leser et standard claim-navn direkte via `FindFirst("sub")` kan derfor stille få `null` selv om claimet faktisk kom tilbake i tokenet (oppdaget 2026-10-01/02 ved ekte BankID-aktivering, se docs/beslutningslogg.md "Ekte BankID for admin/behandler, del 5"). Prøv ALLTID begge navn (det bokstavelige claim-navnet OG dets `ClaimTypes.*`-ekvivalent) for ethvert STANDARD OIDC/JWT-claim lest i en `OnTokenValidated`-handler, ikke bare det ene — et egendefinert/ikke-standard claim-navn trenger ikke denne defensive sjekken.
- En OIDC-klients `scope`-parameter i selve autorisasjonsforespørselen er IKKE nødvendigvis det den faktiske oppstrøms-identitetsleverandøren mottar — en BROKER (som Idura foran ekte BankID) kan være konfigurert PER KLIENT-ID (i brokerens eget dashbord/klientoppsett) til å alltid legge til egne scopes oppå det klienten ber om. Bekreftet reelt 2026-10-01/02: koden ber kun om `openid profile`, men den faktiske utgående autorisasjons-URL-en til BankID viste `scope=openid+profile+sub_nnin+sub_bankid` — `options.Scope.Add(...)` i vår egen kode styrer altså IKKE nødvendigvis hva som faktisk forhandles med den underliggende identitetsleverandøren. Anta ALDRI at en klients scope-forespørsel er den fulle sannheten for en brokered OIDC-integrasjon — observer den FAKTISKE utgående URL-en (eller spør brokerens støtteapparat) for å vite hva som egentlig blir bedt om, spesielt når en spesifikk scope bevisst UNNGÅS av en etterlevelsesgrunn (se "Ekte BankID for admin/behandler, del 5").

## Hvordan jobbe videre

1. Fase 6 (Vipps/fakturering/økonomiske rapporter) eller resten av Del 2/3/4
   (pris/rapporter/økonomi/Vipps-sperre/påminnelser/backup/organisasjonsstøtte) er de naturlige
   neste store stegene — disse henger tett sammen (samme betalings-/faktureringsgrunnlag), så
   det er en rimelig avveining hvilken som tas først. Les kravene i
   `docs/prosjektbeskrivelse-original.md` nøye — det er fortsatt fasiten. Se
   `docs/beslutningslogg.md` under "Åpne punkter til senere faser" for full liste, inkl. mindre
   ting som ble bevisst utsatt (enhetstester, ekte BankID/SMS/e-post-leverandør, ekte CAPTCHA,
   lokalisering, flere innebygde tester utover WHO-5).
2. Oppdater `docs/beslutningslogg.md` etter hvert som beslutninger tas — det er masterdokumentet for prosjektstatus fra nå av, siden Claude Code ikke har tilgang til det opprinnelige claude.ai-prosjektet ("Testdatabase") arbeidet startet i.
3. Bruk samme mønster som i Del 1–5: mock-implementasjoner bak grensesnitt for alt som krever ekte tredjepartsavtaler, ekte pasientdata aldri i dev/test, sikkerhetskode (inkl. kryptering) aktiv i alle miljøer fra starten, IKKE gi et nytt Razor Pages Area samme navn som en domeneentitet med mindre navnet er en annen bøyningsform som ikke matcher eksakt (se fallgruven over — "Tester" vs "Test" var trygt), og ny innebygd test = ny `Test.Kode` + `IInnebygdTestSeeder` + evt. `ITestSkaaringsberegner` (samme mønster som WHO-5).
