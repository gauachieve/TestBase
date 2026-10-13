# TestBase — Online testesystem for psykologiske tester

Dette er et flerfase-prosjekt for en privatpraktiserende autorisert psykologspesialist i Norge og kontorfellesskapet hans: et system for å sende psykologiske tester til pasienter online, med skåring, rapporter, sikker lagring av helseopplysninger og betaling (Vipps).

**Les dette dokumentet i sin helhet før du gjør noe.** Prosjektet ble startet i Claude (Cowork) og er nettopp konvertert til Claude Code — alt av beslutninger, arkitektur og status fra det arbeidet er samlet her og i `docs/`, slik at ingenting går tapt i overgangen. Ikke anta noe om prosjektet som ikke står her eller i `docs/` — spesielt ikke om de delene som ennå ikke er bygget (fase 2–6), der er `docs/prosjektbeskrivelse-original.md` fasiten på krav, ikke hukommelse eller gjetting.

## Arbeidsøkonomi — bruk subagents/billigere modeller for mekanisk verifisering (brukerbeslutning 2026-10-11)

Prosjektet har mange regresjonstester og bruker mye Playwright-nettleserverifisering — begge
nødvendige (se gjentatte reelle 500-feil/sårbarheter i "Kjente fallgruver" under som kun ble
fanget ved faktisk nettleser-/testkjøring), men begge genererer mye rå verktøy-output som koster
dyrt å ha i hovedtråden. Fremover:

- Rutinemessig, mekanisk verifiseringsarbeid — kjøre regresjonstestsuiten og rapportere
  pass/fail, en Playwright-sjekk av at en side laster/et element vises korrekt etter en endring,
  loggsøk (`az webapp log tail`/`grep` over store filer) — skal kjøres via Agent-verktøyet
  (`fork`, eller en ny agent) i stedet for inline i hovedtråden, slik at kun et sammendrag/
  verdikt kommer tilbake, ikke hele transkriptet/alle skjermbildene.
- For denne typen mekanisk, lav-judgment subagent-arbeid: bruk et billigere modell-override
  (`model: "haiku"` på Agent-kallet) der oppgaven faktisk er mekanisk (kjør kommando X, les av
  resultat, rapporter kort). Behold hovedmodellen for selve kodeendringen, designvalg, og enhver
  verifisering som krever faktisk klinisk/domene-vurdering (skåringslogikk, personvern/sikkerhet,
  tolkning av et uventet resultat).
- Dette bytter IKKE bort selve verifiseringen — "verifisert i nettleser"/"alle tester grønne"
  skal fortsatt skje reelt før noe regnes som ferdig, kun HVOR i arkitekturen det kjører endres.
- **Commit+push uten å spørre først (brukerbeslutning 2026-10-11, se beslutningsloggen
  "Superadmin-driftsbrytere"):** når en endring er bygget OG begge verifiseringene over
  (regresjonstester grønne + browser-verifisering der relevant) faktisk har bestått, commit og
  push med en gang — ikke vent på et eksplisitt "commit"/"push" per endring lenger. Gjelder KUN
  når verifiseringen faktisk er gjort og bestått; en endring som ikke er verifisert, eller som
  feiler verifisering, skal IKKE committes — still spørsmål/rapporter feilen i stedet. Risikable
  handlinger (force-push, historie-omskriving, noe som påvirker ekte pasientdata/produksjon
  utover en vanlig `azd deploy`/CI-pipeline) er IKKE dekket av dette og krever fortsatt eksplisitt
  bekreftelse, se CLAUDE.md sin generelle "Executing actions with care"-instruks.

## Dokumenter i `docs/` — les i denne rekkefølgen ved behov

1. `docs/prosjektbeskrivelse-original.md` — det opprinnelige kravdokumentet (fra bruker, ordrett). Kilden til sannhet for ALLE funksjonskrav, spesielt Del 2–4 og testdefinisjonen, som ikke er designet i detalj ennå. Les den relevante delen herfra før du designer noe i fase 2 og utover.
2. `docs/beslutningslogg.md` — full historikk over beslutninger tatt, hva som er ferdig, kjente feilsøkingspunkter fra oppsett, og åpne punkter. Dette er masterdokumentet for prosjektstatus fra nå av — hold det oppdatert etter hvert som dere jobber videre. Filen er stor (8600+ linjer) og vokser — søk med Grep etter et stikkord/en dato/et kjent testnavn i stedet for å lese hele filen, med mindre oppgaven faktisk krever å forstå hele den kronologiske historikken.
3. `docs/compliance-dpia-utkast.md` — utkast til risikovurdering (DPIA) for helsedata, basert på Normen og GDPR. Ikke juridisk rådgivning; bør kvalitetssikres av jurist/DPO før ekte pasientdata går i produksjon.
4. `docs/del1-utviklingsmiljo-plan.md` — den opprinnelige planen for utviklingsmiljøet (delvis historisk — se beslutningsloggen for hva som faktisk endte opp implementert).

## Status akkurat nå (sist komprimert 2026-10-11 — se `docs/beslutningslogg.md` for hele historikken)

**Denne seksjonen er bevisst kort.** Den fulle, kronologiske historikken (hver dato, hvert
skjermbilde-funn, hver verifisering) er FLYTTET til `docs/beslutningslogg.md` og finnes IKKE
lenger duplikert her — den filen er stor (8600+ linjer), så bruk Grep/søk etter stikkord eller
dato i stedet for å lese den i sin helhet. Dette dokumentet lastes i sin helhet inn i hver
samtale, så alt som står her har en løpende kostnad — flytt ny historisk narrativ til
beslutningsloggen, ikke inn hit; hold denne seksjonen til en kort "hva finnes nå"-oversikt og
drifts-fakta som er direkte relevante for å ikke gjøre en feil akkurat nå.

**Bygget og verifisert (fase 0-5 ferdig, fase 6 og videre godt i gang):**
- Admin-, behandler- og pasientportal komplett end-til-ende: BankID/2FA-innlogging, pasient-/
  behandler-CRUD, invitasjon/HPR-godkjenning, gruppe- og QR-selvregistreringssystem.
- Generisk testmotor + skåringsmotor med et stort og voksende bibliotek innebygde tester (over
  100 regresjonstester per 2026-10-11) — admin forfatter tester, behandler/admin tildeler,
  pasient (eller behandler, for kliniker-administrerte tester) fyller ut, rapportgodkjenning,
  A4-rapportvisning, grupperapportering/histogram, utvikling-over-tid-grafer.
- Tildelingsflyt, meldinger/oppgaveliste (`MinSide` i alle tre Areas), automatiske/manuelle
  påminnelser.
- Partner/klinikk-system: allow-list per test, egne honorarandeler, betaling (Vipps/Stripe) som
  faktisk gater utfylling, periodiserte økonomirapporter, månedlig Stripe Connect-utbetaling
  (bygget og lokalt verifisert, IKKE deployet ennå).
- Hjemmeoppgaver (behandler-forfattet, delt/partner-nivå) og tidsbaserte behandlingsprogrammer.
- Tilbakemeldingswidget + hjelpemeny (rolle-/kontekstsensitivt) på alle sider.
- CI/CD: GitHub Actions bygger/tester/deployer automatisk til beta, og KUN ved grønn helsesjekk
  videre til live. En daglig Claude-rutine (05:00 UTC) leser tilbakemeldinger/krasjrapporter,
  sender en norsk e-postrapport, og åpner PR-er for krasj den er trygg på å fikse selv — ALDRI
  push rett til master.

**Drifts-fakta å kjenne til FØR du gjør noe (kan ha endret seg — bekreft i Admin/MinSide og
App Service-innstillinger før du antar, spesielt på live):**
- Tre miljøer: lokal dev, `testbase-beta` (beta.psytest.no), og `testbase-test` (azd-navn, men
  faktisk LIVE på `www.psytest.no`). `azd env select testbase-beta` er default aktivt miljø i
  repoet — velg `testbase-test` eksplisitt for å nå live.
- Ekte BankID for admin/behandler (Idura/Stø) FUNGERER, styrt av en runtime-bryter på
  `Admin/MinSide` (Superadmin/Utvikler-only, `EktBankIdInnstilling`) — skrus av/på UTEN redeploy.
  Pasient-BankID og ekte Vipps/kortbetaling er IKKE bygget/aktivert.
- `Miljo:TillatPersonnummerOverride` har stått midlertidig `"true"` på live som en nødløsning før
  ekte BankID var verifisert — skal settes tilbake til `"false"` nå som ekte BankID fungerer; sjekk
  faktisk App Service-innstilling, ikke bare denne teksten.
- Kjent, IKKE fikset sårbarhet: `ItqSkaaringsberegner`/`Phq9Skaaringsberegner` bruker fortsatt
  posisjonsbasert svarindeksering (samme klasse som den GADIT/IAQ-krasjen som ER fikset,
  se "Kjente fallgruver" under) — en pasient som hopper over ett ledd kan gi stille feil skår
  eller en krasj. Fiks med `ITestSkaaringsberegnerMedLedd` hvis du er i nærheten av disse testene.
- App Service/MySQL-SKU står normalt på billigste tier (Basic B1/Burstable B1ms) — kun skalert opp
  midlertidig og reversibelt ved kjente lasttopper (se beslutningsloggen for hvilken SKU som faktisk
  fungerer under last, `S2` alene var gjennombruddet, `S1`/`S2+B2s` var IKKE det).
- **Før ekte pasienttesting:** et research-notat (`docs/juridisk-notat-helsedata-og-digital-
  helseteknologi.md`, 2026-10-11, IKKE juridisk rådgivning) flagget at TestBase sin rapport-/
  skåringsfunksjon trolig faller under MDR (medisinsk utstyr) Klasse IIa eller høyere — en
  UBESVART, potensielt stor forpliktelse (CE-merking/kvalitetsstyringssystem) som IKKE er avklart
  eller priset inn. Ikke anta dette er løst uten å sjekke om en kvalifisert vurdering har skjedd.

For alt annet — hvorfor en beslutning ble tatt, bevisste scope-kutt, nøyaktig hva som ble
verifisert og hvordan, lisens-/kildevurderinger for enkelttester osv. — søk i
`docs/beslutningslogg.md` på stikkord/dato. Anta ALDRI at noe er "sikkert ferdig" for en
spesifikk, detaljert påstand uten å bekrefte i koden eller loggen — denne oversikten er bevisst
grov.

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
- **Eksterne leverandører (BankID, Vipps, SMS, e-post, betaling) — RETTET 2026-10-11, denne linjen var stale:**
  BankID for admin/behandler har en EKTE produksjonsavtale (Idura/Stø) som FUNGERER på live, styrt
  av en runtime-bryter (`EktBankIdInnstilling`, default på) — se "Ekte BankID for admin/behandler,
  del 1-9" i beslutningsloggen. Pasient-BankID er fortsatt IKKE bygget (kun mock). Vipps har en
  EKTE produksjonsavtale og ekte produksjonsnøkler konfigurert på live (`Vipps:Miljo=Produksjon`,
  bekreftet direkte i App Service-innstillingene 2026-10-11) — brukeren gjennomførte selv en reell
  betaling 2026-09-15 som avdekket og fikk fikset en kritisk "capture"-bug (penger ble reservert,
  aldri trukket, se "Kritisk bugfiks: Vipps-betaling ble aldri fanget"); IKKE re-verifisert siden,
  men bør fungere. Stripe (kort/Apple Pay/Google Pay) sin nøkkel på live er BEKREFTET en
  TEST-nøkkel (`sk_test_...`, sjekket direkte i Key Vault 2026-10-12) — ekte kortbetaling kan
  altså IKKE trekke ekte penger akkurat nå, uansett hva betalings-driftsbryteren står på, før
  denne byttes til en `sk_live_...`-nøkkel i Stripe-dashbordet/Key Vault. Den NYE Stripe
  Connect-UTBETALINGEN (til behandler/klinikk, bygget 2026-10-10) er
  KUN testet mot Stripes eget test-API — ingen reell mottaker har fullført ekte konto-onboarding
  hos Stripe ennå, se "Driftsbrytere"-spørsmålet i beslutningsloggen for full sjekkliste før dette
  kan fungere med ekte penger. E-post (Azure Communication Services) og SMS (Vonage) har begge en
  ekte, fungerende integrasjon i Azure test-App Service — se beslutningsloggen under "Ekte
  e-postutsending via Azure Communication Services" og "SMS-integrasjon: byttet fra Azure til
  Vonage". All kode mot disse går bak grensesnitt (`IBankIdProvider`, `IVippsClient`, `ISmsSender`,
  `IEmailSender`) med mock-implementasjoner i `TestBase.Shared/Providers/Mock/` som brukes lokalt
  uansett, slik at utvikling ikke er avhengig av ekte avtaler/kontoer. Se `/DevDemo`-siden for
  eksempel på bruk. I tillegg finnes det siden 2026-09-05 en ekte, gratis Idura BankID-TEST-
  integrasjon (`/DevDemo` → "Test ekte BankID (Idura)") — kun et diagnostisk sideverktøy, IKKE
  koblet til `IBankIdProvider`/den faktiske innloggingsflyten, se beslutningsloggen
  "BankID-testintegrasjon via Idura".
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
- En ny `IInnebygdTestSeeder`- og/eller `ITestSkaaringsberegner`-IMPLEMENTASJON (ny klasse) blir IKKE automatisk tatt i bruk — BEGGE grensesnittene krever et EKSPLISITT `builder.Services.AddScoped<...>(...)`-kall i Program.cs per klasse (ingen reflection/assembly-scan). Glemmer man dette, kjører "Regenerer innebygde tester" (Admin/Tester) stille uten feil og uten å opprette testen i det hele tatt — ingen kompilatorfeil, ingen runtime-advarsel, testen ser bare ut til å "ikke eksistere". Skjedde reelt ved MPFI-24 (2026-10-03, se docs/beslutningslogg.md). Sjekk ALLTID at BEGGE linjene (seeder OG skåringsberegner, hvis testen har en) er lagt til i Program.cs før en ny innebygd test regnes som ferdig, og bekreft ved å faktisk se testen dukke opp i Admin/Tester-listen etter et regenerer-klikk.
- En `_auditLogger.LogAsync(...)`-kall med `entityId: string.Join(",", ider)` (en rå kommaseparert liste av FLERE entitet-IDer) kaster `DbUpdateException`/`MySqlException: Data too long for column 'EntityId'` så snart listen blir lang nok — `AuditLogEntry.EntityId` har `HasMaxLength(64)` i AppDbContext, indeksert for oppslag på ÉN entitet, ikke designet for en voksende batch-liste. Skjedde reelt på LIVE ved "velg alle tester"-tildeling (2026-10-03, se docs/beslutningslogg.md "Reell 500-feil ved bulk-tildeling") — og samme mønster fantes i FEM andre kallsteder, inkludert ett (`Admin/Tester/Prising` sin "lagre alle") som trigges av en VANLIG lagring, ikke et spesielt brukervalg. Bruk ALLTID `AuditBatch.EntityId(ider)` (`TestBase.Shared/Security/AuditBatch.cs`) for et audit-kall som dekker flere entiteter — den returnerer enten selve ID-en (ett element) eller en kort `"batch:{antall}"`-streng, og legg heller den fulle listen i `details` (2000 tegns grense, ikke indeksert). `EfAuditLogger.LogAsync` trunkerer nå også defensivt ALLE felt som et sikkerhetsnett, men det er ingen unnskyldning for å ikke bruke AuditBatch ved nye bulk-kallsteder — en silent trunkering gir et ufullstendig/meningsløst audit-spor, selv om den ikke lenger krasjer.
- html2canvas (vendoret lokalt, se tilbakemeldingswidgeten) kaster `"Unable to find element in cloned iframe"` når den bes om å rendre et `<svg>`-ROT-element DIREKTE — en kjent begrensning i biblioteket, ikke noe spesifikt ved vårt markup. Skjedde reelt ved bugliste punkt 8 (2026-10-04, "Kopier alt" skulle inkludere graf-bitmaps) — løst ved å plassere capture-målet (`data-rapport-graf="..."`-attributtet) på en vanlig omsluttende `<div>` rundt SVG-en i stedet for på selve `<svg>`-taggen. Gjelder ethvert fremtidig forsøk på å skjermbilde-fange et SVG-element med html2canvas — pakk det ALLTID i en HTML-wrapper først.
- Den tidligere dokumenterte "ny valgfri parameter midt i signaturen knekker positional calls"-fallgruven gjentok seg reelt ved bugliste punkt 13 (2026-10-04): å legge `long? ansvarligBehandlerId = null` midt i `TestTildelingsService.TildelOgVarsleAsync` sin signatur (før den eksisterende `cancellationToken`) knekte ET kallsted i `PlanlagtTildelingService.cs` som fortsatt brukte positional argumenter helt til slutten (`CS1503`, kompilatoren fanget det — denne gangen IKKE en stille runtime-bug, men prinsippet/risikoen er identisk). Minner om: `grep -rn "\.MetodeNavn(" src/` for ALLE kallsteder FØR man endrer en eksisterende offentlig metodesignatur, ikke bare de kallstedene man selv nettopp jobbet i.
- `style="display: inline;"` (MED et mellomrom etter kolon) matcher ALDRI en CSS-attributt-selektor skrevet som `[style*="display:inline"]` (UTEN mellomrom) — en substring-match er bokstavelig, så selv om begge "betyr det samme" for en nettleser som CSS-egenskap, er de to ulike TEKSTSTRENGER for en `[attr*="..."]`-selektor. Et skjema-element med denne stavemåten faller dermed stille tilbake til en helt ANNEN, bredere regel ment for FRITTSTÅENDE forms (`main.page form:not([style*="display:inline"])` — kortstyling med bakgrunn/padding/border), i stedet for den smale "stå flatt på linje"-regelen. Skjedde reelt med to sider bygget i en tidligere runde (Hjemmeoppgaver/Programmer sine Index-sider, bugliste 2026-10-06 punkt 1) — ingen kompilatorfeil, ingen runtime-feil, bare synlig som uønskede "bokser" rundt knapper ved skjermbilde-inspeksjon. Skriv ALLTID `style="display:inline;"` (ingen mellomrom) for å matche konvensjonen resten av kodebasen allerede bruker, og mistenk denne fallgruven FØRST hvis et nytt sett med inline-knapp-forms plutselig ser feilstylet ut sammenlignet med eldre, tilsynelatende identisk markup.
- En CSS-regel med flere `:not([attributt="verdi"])`-ledd på samme selektor (f.eks. `input:not([type="a"]):not([type="b"]):not([type="c"])...`) får spesifisitet fra HVERT `:not()`-ledds argument — seks slike ledd teller som SEKS klasser i spesifisitetsberegningen, ikke null. En nyere, tilsynelatende mer "spesifikk" override-regel med bare ÉN ekstra klasse (f.eks. `main.page form input.min-klasse`) kan dermed TAPE mot en slik bred `:not()`-kjede selv om den ser mer target-ert ut ved rask lesning — spesifisitet telles på ANTALL seleksjonsledd, ikke på "hvor spesifikk den føles". Skjedde reelt med `.program-klokkeslett-input` (bugliste 2026-10-06 punkt 2 — en tidligere "fiks" fra en forrige runde hadde ALDRI faktisk virket, feltet var fortsatt 100% bredt i praksis). Løsning: `!important` på den smalere, bevisst snevre override-regelen (samme presedens som `.rapport-handlinger form` allerede bruker et annet sted i filen) — eller tell spesifisitetsleddene eksplisitt (inkl. hvert `:not()`-arguments bidrag) før man stoler på at en ny regel vinner.
- Et `flex`-element sin `flex-basis` satt i prosent (f.eks. to kolonner `60%`/`38%` for en ca. 2-kolonners splitt) PLUSS en separat `gap`-verdi på selve flex-containeren summeres OPPÅ hverandre — en 60%+38%+`gap: 1.5rem` kan fint overskride containerens 100% bredde, og `flex-wrap: wrap` tvinger da kolonnene til å pakke om på hver sin linje i stedet for å stå side om side, selv om prosentene isolert sett ser ut som de skal summere til under 100%. CSS Grid (`display: grid; grid-template-columns: 60% 1fr; gap: 1.5rem;`) har IKKE dette problemet — `gap` i grid trekkes aldri fra kolonnesporenes egen bredde. Skjedde reelt i `_TestKategoriVelger.cshtml` (bugliste 2026-10-06 punkt 5/6) — løst ved å bytte fra flex til grid for akkurat denne to-kolonners splitten, ikke ved å redusere prosentene for å "gi plass til" gapet.
- Et betinget `@(uttrykk)`-token skrevet BART inni et HTML-tag for å injisere en HEL attributt-streng (f.eks. `@(cond ? "" : "name=\"X\"")` direkte mellom andre attributter, i stedet for som verdien TIL et navngitt attributt) blir HTML-ENKODET av Razor som vanlig tekst-output, ikke parset som ekte markup — `name="X"` ender opp som `name=&quot;X&quot;` i selve HTML-kilden, et INGEN nettleser tolker som et attributt. Et skjemafelt (f.eks. en checkbox) skrevet slik mister dermed sitt `name`-attributt HELT STILLE: ingen kompilatorfeil, ingen runtime-feil, siden laster og submitter helt normalt — feltet er bare usynlig for server-side modellbinding (en handler som leser `List<long> testIder` får alltid en tom liste). Skjedde reelt i `_TestKategoriVelger.cshtml` sin `CheckboxNavn`-parameter (bugliste 2026-10-11 punkt 4) — ALDRI faktisk brukt med en ikke-null verdi før den runden, så bugen hadde ligget latent i komponenten siden den ble skrevet. Løsning: pakk HELE det betingede uttrykket i `@Html.Raw(...)` — samme "verktøy", men en ANNEN årsaksklasse enn den allerede dokumenterte SVG `<text>`-fallgruven (der roten er en pseudo-tag-begrensning, ikke HTML-enkoding av en attributt-streng). Bekreft ALLTID en ny skjemahandler sin faktiske databaseeffekt direkte (f.eks. en rå SQL-spørring), ikke bare at siden viser en "Lagret."-melding — handleren kan kjøre helt uten feil og likevel ha mottatt tomme data.

- Et `<form>` FYSISK nøstet inni et annet `<form>` (f.eks. en per-rad "slett/be om fjerning"-form
  i en tabellcelle, der HELE tabellen igjen er omsluttet av en ytre bulk-lagre-form) er ugyldig
  HTML — nettleseren "reparerer" det ved parsing (den indre form-taggen ignoreres/smeltes inn i
  den ytre), som i praksis DUPLISERER skjulte felt (inkl. antiforgery-tokenet) inn i én overlevende
  form og gir et ekte, reprodusérbart `400 Bad Request` ved innsending — IKKE en antiforgery-
  feilmelding i loggen man lett kan spore tilbake til nøstingen, bare en generisk 400. Skjedde
  reelt på `MinPartner/Prising.cshtml` (2026-10-11) der per-rad "Be om fjerning"-knappen lå inni
  samme tabell som "Lagre alle endringer" sin omsluttende form. Løsning: ALDRI nøst — bruk i
  stedet samme `form="elementId"`-kryssreferanse-mønster som `data-flervalg-beholder` allerede
  bruker andre steder (en TOM `<form id="x" method="post"></form>` plassert FØR/ETTER tabellen,
  ikke rundt den, med `form="x"` på hvert felt/knapp som faktisk skal poste dit). Se samtidig
  neste punkt — en slik tom form trenger ALLTID en eksplisitt `asp-page`/`asp-page-handler`.
- En TOM `<form method="post"></form>` (brukt kun som `form="..."`-kryssreferansemål, se punktet
  over) UTEN noen `asp-page`/`asp-page-handler`-attributt mangler en eksplisitt `action` — HTML-
  spesifikasjonen lar da nettleseren falle tilbake til å poste til GJELDENDE DOKUMENT-URL,
  INKLUDERT en hvilken som helst `?handler=...`-spørrestreng som måtte stå i adresselinjen fra en
  TIDLIGERE handler på SAMME side (f.eks. en handler som returnerer `Page()` uten å redirecte).
  Resultatet: brukeren klikker en helt urelatert knapp (f.eks. "Lagre alle endringer") og
  POST-en treffer stille feil handler. Skjedde reelt rett etter nøstet-form-fiksen over, fanget
  ved å reprodusere nøyaktig rekkefølgen "klikk en per-rad-handling (etterlater `?handler=X` i
  adresselinjen) → klikk bulk-lagre uten mellomliggende reload". Løsning: gi ENHVER slik tom
  kryssreferanse-form en eksplisitt `asp-page="./SidensEgetNavn"` (ingen handler = standard
  `OnPostAsync`), slik at action-URL-en ALLTID genereres rent, uavhengig av hva som tilfeldigvis
  står i adresselinjen idet brukeren klikker.

## Hvordan jobbe videre

1. Fase 0-5 er ferdig, og fase 6 (tildeling/meldinger/rapportgodkjenning/partner-/klinikksystem/
   hjemmeoppgaver/programmer/økonomirapporter) er langt forbi "neste steg" — det meste er bygget
   og verifisert, se "Status akkurat nå" over for en kort oversikt. De reelt gjenstående punktene
   per 2026-10-11: deploye det lokalt bygde Stripe Connect-utbetalingssystemet, bekrefte at
   `Miljo:TillatPersonnummerOverride` er satt tilbake til `"false"` på live, pasient-BankID og ekte
   Vipps/kortbetaling (ikke bygget), og den kjente posisjonsbaserte skåringssårbarheten i
   `ItqSkaaringsberegner`/`Phq9Skaaringsberegner`. Søk `docs/beslutningslogg.md` under "Åpne
   punkter til senere faser" for full liste, og `docs/prosjektbeskrivelse-original.md` for
   fasiten på krav som ennå ikke er designet i detalj.
   **Nytt 2026-10-13:** et komplett abonnement/subscription-system (tre behandler-
   abonnementsmodeller, klinikk-sete-prising, superadmin-kostnadsoppsett, selvregistrering,
   test-modus-sperre) er bygget og fullt test-/Playwright-verifisert — se
   `docs/beslutningslogg.md` "Abonnement/subscription-system" (to oppføringer, fase 1 og
   fase 2-5) for full detalj. Ekte Stripe-kortopptak/Subscriptions-API/faktisk månedlig
   fakturering er BEVISST IKKE bygget ennå (kun en manuell superadmin-override finnes,
   `Admin/Behandlere/Rediger`) — krever ekte Stripe-testnøkler og brukerens egen interaktive
   verifisering før det kan fullføres, samme mønster som BankID/Vipps alltid har fulgt.
2. Oppdater `docs/beslutningslogg.md` etter hvert som beslutninger tas — det er masterdokumentet for prosjektstatus fra nå av, siden Claude Code ikke har tilgang til det opprinnelige claude.ai-prosjektet ("Testdatabase") arbeidet startet i.
3. Bruk samme mønster som i Del 1–5: mock-implementasjoner bak grensesnitt for alt som krever ekte tredjepartsavtaler, ekte pasientdata aldri i dev/test, sikkerhetskode (inkl. kryptering) aktiv i alle miljøer fra starten, IKKE gi et nytt Razor Pages Area samme navn som en domeneentitet med mindre navnet er en annen bøyningsform som ikke matcher eksakt (se fallgruven over — "Tester" vs "Test" var trygt), og ny innebygd test = ny `Test.Kode` + `IInnebygdTestSeeder` + evt. `ITestSkaaringsberegner` (samme mønster som WHO-5).
