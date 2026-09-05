# Prosjektstatus og beslutningslogg — Online Testesystem

*Sist oppdatert: 2026-09-02 (sky-deploy til Azure, resten av Del 1). Dette dokumentet lever gjennom hele prosjektet og skal til enhver tid kunne brukes til å regenerere løsningen med alle beslutninger tatt. Denne kopien ble tatt med inn i Git-repoet 2026-08-20 da prosjektet ble konvertert fra Claude (Cowork) til Claude Code — masterversjonen lå tidligere kun i et claude.ai-prosjekt ("Testdatabase"), som ikke er tilgjengelig fra Claude Code. Denne filen ER nå masterversjonen; oppdater den videre her.*

## Kilde

Basert på "Prosjektbeskrivelse Online Testesystem.docx", lastet opp 2026-08-18 — full tekst i `docs/prosjektbeskrivelse-original.md`. Original prosjektbeskrivelse forutsatte egen Windows Server 2016/IIS for alt. Dette er siden revidert av bruker for produksjonsmiljø — se "Hosting-pivot" under.

## Oppsummering av omfang

Et system for at en privatpraktiserende autorisert psykologspesialist og kontorfellesskapet hans kan gjennomføre psykologiske tester online med pasienter, med skåring, rapportgenerering, sikker lagring av helseopplysninger, og betaling. Fire delsystemer:

1. **Utviklingsmiljø** — automatisert deploy/versjonering, fjerntilgang.
2. **Administrasjon** — BankID-pålogging, behandler-/testadministrasjon, prising, økonomiske rapporter, brukerstyring.
3. **Behandlersystem** — pasientadministrasjon, tildeling av tester, rapporter, arkivering.
4. **Pasientsystem** — registrering, gjennomføring av tester, betaling (VIPPS), sletting av egne data.

Pluss et generelt **rammeverk for å definere psykologiske tester** (skåring, rapport, lokalisering), med WHO-5 som første eksempel-test.

Krav på tvers: HTML-grensesnitt (responsivt pc/mobil), kryptering iht. norske lovkrav for helseopplysninger, BankID + 2FA, VIPPS, SMS, e-post, fakturagenerering, automatisk backup/restore, versjonering av alle deler.

## Vurdering av gjennomførbarhet

Dette er ikke et ett-økt-prosjekt. Det er et flerspors utviklingsprogram som deles i faser over mange sesjoner, med denne loggen som lim mellom sesjonene. Kan ikke inngå avtaler med BankID-leverandør/Vipps/SMS-leverandør/skyleverandør, eller erstatte juridisk/DPO-vurdering av helsedatahåndtering — dette må bruker selv gjøre.

## Faseplan

Fase 0: Arkitektur- og compliance-grunnlag. **Ferdig.**
Fase 1: Del 1 — lokalt utviklingsmiljø. **Ferdig (lokal del) 2026-08-20.** Sky-deploy-delen (Azure) er **satt opp og verifisert 2026-09-02** — se "Sky-deploy til Azure (azd)" under. Test-miljøet kjører i Sweden Central, ikke Norway East/West som opprinnelig planlagt — se samme seksjon for hvorfor.
Fase 2: Del 2 — admin-skjelett + BankID/2FA-autentisering. **Første slice ferdig 2026-08-23** (se "Del 2 (slice 1)" under) — pris/rapporter/backup/org-støtte er bevisst utsatt, se "Åpne punkter".
Fase 3: Del 3 — behandlersystem. **Første slice ferdig 2026-08-24** (se "Del 3 (slice 1)" under) — rapporter/økonomi/automatiske utsendelser er bevisst utsatt, se "Åpne punkter".
Fase 4: Del 4 — pasientsystem + testmotor (generisk rammeverk). **Første slice ferdig 2026-08-24** (se "Del 4 (slice 1)" under) — lokalisering, Vipps-betalingssperre, påminnelser og skåring/rapporter er bevisst utsatt, se "Åpne punkter".
Fase 5: Første konkrete test (WHO-5) ende-til-ende som mal for fremtidige tester. **Første
slice ferdig 2026-08-24** (se "Del 5 (slice 1)" under) — lokalisering fortsatt utsatt, se
"Åpne punkter".
Fase 6: Betaling (VIPPS), fakturering, økonomiske rapporter.

Fulle krav for hver del ligger i `docs/prosjektbeskrivelse-original.md` — les den delen som er relevant før du designer videre, ikke stol på hukommelse/sammendrag alene.

## Beslutninger tatt

**Teknologistack:** ASP.NET Core (C#) som backend-rammeverk. Begrunnelse: sterk typing og modenhet for et system som skal driftes i mange år av én person, godt bibliotekstøtte for kryptering (`Microsoft.AspNetCore.DataProtection`, `System.Security.Cryptography`), BankID-integrasjonsbiblioteker finnes for .NET, god MySQL-støtte via **Pomelo.EntityFrameworkCore.MySql**. Razor Pages/MVC for admin- og behandlerflater; ren HTML/JS (evt. lettvekts frontend som Alpine.js/HTMX) for pasient- og testsider for å holde det enkelt og raskt på mobil.

**Database:** MySQL, tilgang via Entity Framework Core + Pomelo-provider, med migrations for versjonering av skjema.

**Compliance-tilnærming:** Bruker har ikke jurist/DPO-vurdering på plass ennå. Et utkast til risikovurdering (DPIA) og Normen-tilpasning ligger i `docs/compliance-dpia-utkast.md`. Dette er et startpunkt bruker bør la en jurist/DPO kvalitetssikre før pasientdata går i reell produksjon — det er ikke i seg selv juridisk rådgivning. **Ingen ekte pasientdata i dev/test noensinne.**

**Leverandørstatus:** Ingen avtaler på plass ennå for BankID-integrasjon, Vipps-forhandler, eller SMS/e-post-utsending. Anskaffelse tas inn som egne deloppgaver, tidligst relevant i fase 2 (BankID/2FA) og fase 6 (Vipps/fakturering). Kandidater å vurdere da: BankID via Signicat eller Criipto, SMS via Link Mobility eller Twilio.

**Versjonskontroll:** Git. Prosjektet ligger i `C:\code\TestBase` på brukers maskin (`gaute-pc`), under versjonskontroll med en fungerende første commit fra 2026-08-20.

**Skifte til Claude Code (2026-08-20):** Prosjektet ble startet i Claude (Cowork), der en sky-til-PC-"bro" for direkte filtilgang aldri fikk kontakt gjennom hele Del 1-arbeidet (til tross for flere forsøk, inkl. reinstallasjon av Claude-appen). Kode ble derfor levert som zip-filer, og bruker kjørte kommandoer selv i egen terminal med veiledning. Bruker konverterte deretter til Claude Code, som kjører direkte lokalt uten noen bro-mekanisme. Denne filen og resten av `docs/`-mappen ble skrevet for at ingenting av konteksten skulle gå tapt i overgangen.

### Sky-deploy til Azure (azd)

Del 1s gjenstående sky-deploy-punkt er nå satt opp med **Azure Developer CLI (`azd`)**:
`azure.yaml` i repo-roten + `infra/main.bicep`/`infra/main.parameters.json`/`infra/resources.bicep`
definerer og provisjonerer alle ressurser (`azd provision`/`azd up`):

- App Service (Linux, `.NET 8`, Basic B1) for `TestBase.Web`
- Azure Database for MySQL – Flexible Server (Burstable `Standard_B1ms`, ingen HA, 7 dagers backup — testmiljø, ikke produksjonsdimensjonert)
- Key Vault (RBAC-autorisert, App Service sin system-assignerte identitet får `Key Vault Secrets User`) som holder tilkoblingsstrengen; App Service leser den via `@Microsoft.KeyVault(...)`-referanse i `ConnectionStrings__DefaultConnection`
- MySQL-administratorpassordet genereres deterministisk i Bicep (`uniqueString(...)`) og lagres kun i Key Vault — aldri i kildekode eller `.env`

Miljøet heter `testbase-test` (azd-environment, lokal `.azure/`-mappe, gitignored — inneholder ressurs-IDer/abonnements-ID). Verifisert 2026-09-02: alle fire ressurser `Succeeded`, `/health`-endepunktet på den utrullede App Service-URL-en svarer `200 Healthy`.

**Avvik fra planlagt region (viktig):** `docs/beslutningslogg.md` sin opprinnelige "Hosting-pivot"-beslutning under sier Norway East/West for datalagringssted. Et tidligere forsøk på å provisjonere til Norway East/West feilet fordi det ikke var regional kapasitet for den valgte MySQL-SKU-en (`Standard_B1ms`) — ikke en konfigurasjonsfeil. Testmiljøet ble derfor lagt i **Sweden Central** i stedet. Bekreftet på nytt 2026-09-02 med en engangs disposable-probe (opprettet og slettet en egen ressursgruppe i Norway East): Norway West er ikke engang et tillatt region for dette Azure-abonnementet, og et faktisk forsøk på å opprette MySQL Flexible Server i Norway East feiler fortsatt umiddelbart med `InternalServerError` (samme feil som `az mysql flexible-server list-skus --location norwayeast` gir). Dette er et test-miljø uten ekte pasientdata, så regionvalget er ikke kritisk ennå — men **før reell produksjonssetting med ekte pasientdata må regionkapasiteten sjekkes på nytt** (Azure-regional kapasitet for burstable PaaS-SKU-er endrer seg over tid og kan ikke sjekkes på forhånd via en quota-API, kun ved et faktisk forsøk), og dersom Norway fortsatt ikke er mulig må databeslutningen revurderes eksplisitt med bruker/DPO (jf. `docs/compliance-dpia-utkast.md`) — ikke anta at Sweden Central er godkjent for ekte helsedata uten den vurderingen.

Kjent begrensning: `ASPNETCORE_ENVIRONMENT` er satt til `Development` i App Service-konfigurasjonen (`infra/resources.bicep`) — bevisst, siden mock-leverandørene og enkle dev-nøkler fortsatt brukes og det ikke finnes ekte pasientdata i dette miljøet. Må endres til en ekte produksjonskonfigurasjon (og reelle leverandøravtaler/nøkler) før noe reelt driftsettes.

**Viktig sikkerhetsfunn (2026-09-02): Google Chrome/Safe Browsing flagget test-appen som "Dangerous site" (phishing).** Årsak: innloggingssiden (`Pages/Konto/LoggInn.cshtml`) har en knapp merket "Logg inn med BankID" og et utviklingsmiljø-felt merket "Personnummer" som overstyrer BankID-mocken — dette er strukturelt identisk med et ekte BankID-phishing-forsøk (nasjonalt varemerke for identitetsverifisering + innsamling av fødselsnummer, driftet på en generisk, ubrandet `azurewebsites.net`-adresse uten noen reell BankID-integrasjon). Google sin phishing-klassifisering fanget dette mønsteret, mest sannsynlig korrekt ut fra mønstergjenkjenning, ikke en feil. App Service-en sto samtidig helt åpen for hele internett (`ipSecurityRestrictions: Allow Any`), inkludert SCM/Kudu-endepunktet — hvem som helst (inkl. automatiske skannere) kunne nå siden.

**Umiddelbar tiltak:** La til IP-baserte access restrictions på App Service (både hovedsiden og `--scm-site`) som kun tillater brukerens IP (`51.175.216.201/32`, prioritet 100) — alt annet nektes automatisk (`Deny all` la seg på som default så snart en eksplisitt Allow-regel ble lagt til). Verifisert: appen svarer fortsatt fra brukerens IP, avvises for alle andre. Dette bør holde til flagget forsvinner (Safe Browsing revurderer over tid når siden ikke lenger er skannbar) og til videre testing skjer bak samme restriksjon.

**Oppdatering samme dag — IP-restriksjon erstattet med en app-nivå tilgangssperre (`StagingGate`).**
Bruker trengte å teste fra mobil/nettbrett/flere PC-er med skiftende IP-er, noe en IP-allowliste
ikke egner seg til. Samtidig ble et uavhengig, mer alvorlig funn gjort: `PersonnummerOverride`-feltet
på innloggingssiden er et UBETINGET auth-bypass — `LoggInn.cshtml.cs` sin `OnPostAsync` kaller
`StartBankIdAsync(personnummerOverride: PersonnummerOverride, ...)` uten noen `IsDevelopment()`-sjekk
i selve POST-handleren (kun VISNINGEN av feltet i Razor-viewet er gatet på dev), og
`MockBankIdProvider` honorerer en hvilken som helst oppgitt streng som "verifisert" personnummer
uten videre kontroll. Siden `dev-admin` sitt personnummer er en fast, kildekode-synlig konstant
(`"01010000000"` i `Program.cs`), kunne HVEM SOM HELST med nettverkstilgang til siden logge inn
som administrator ved å løse det trivielle regnestykke-CAPTCHA-et og oppgi denne kjente verdien —
helt uavhengig av passord, BankID eller 2FA. Å bare gjenåpne brannmuren (selv med omdøpt
BankID/personnummer-tekst) ville ha eksponert dette bypasset for hele internett igjen.

Løsning: en enkel tilgangssperre FORAN HELE appen (`Security/StagingGate.cs` i `TestBase.Web`,
registrert som `app.UseStagingGate()` helt først i pipelinen i `Program.cs`, før alt annet
inkludert `/health`). Aktiveres kun når App Service-innstillingen `StagingGate__AccessKey` er satt
(aldri lokalt) — uten en gyldig, DataProtection-signert cookie (satt etter riktig nøkkel postet i et
enkelt skjema) får ALLE forespørsler et generisk 401-svar uten noe BankID/personnummer-relatert
innhold i det hele tatt. Dette løser begge problemene samtidig: Google/andre krypere kan aldri se
det phishing-lignende innholdet (siden de aldri kommer forbi sperren), OG auth-bypasset er
utilgjengelig uten nøkkelen. IP-restriksjonen på selve nettsiden ble deretter fjernet igjen (tilbake
til "Allow Any" på nettverksnivå) siden appen nå beskytter seg selv — SCM/Kudu-endepunktet
(deployment) beholder fortsatt IP-restriksjon til brukerens IP, uendret. Verifisert: feil nøkkel gir
401, riktig nøkkel gir 302 + 90-dagers cookie, påfølgende forespørsler med cookien går rett gjennom.

**Prinsipp å ta med videre — også relevant for reell produksjon:**
- Et BankID-lignende innloggingsgrensesnitt bør ALDRI være offentlig tilgjengelig på en generisk, ubrandet sky-adresse uten nettverksrestriksjon, uansett om det er mock eller ekte bak. Dette gjelder ikke bare "ingen ekte pasientdata i dev/test" (som allerede var prinsippet) — selve SIDENS UTSEENDE/tekst kan trigge phishing-klassifisering og i verste fall skade brukerens/virksomhetens domene-omdømme, helt uavhengig av hva som faktisk skjer bak kulissene.
- Før reell produksjonssetting: egen, brandet custom-domene (ikke rå `*.azurewebsites.net`), ekte BankID-leverandøravtale (Signicat/Criipto), og en vurdering av om noe UI-tekst kan mistolkes som identitetstyveri-forsøk. Vurder også å sende inn en "false positive"-rapport til Google (https://safebrowsing.google.com/safebrowsing/report_error/) når/hvis en fremtidig offentlig test-URL trengs, i tillegg til IP-restriksjon.
- Standard for FREMTIDIGE Azure-testmiljøer: sett IP-restriksjon (`az webapp config access-restriction add`) som en del av `infra/`-oppsettet fra dag én, ikke som en etterhåndsrettelse — vurder å legge dette inn i `infra/resources.bicep` selv (`ipSecurityRestrictions` på `Microsoft.Web/sites`-ressursen) fremfor kun manuelt via CLI, siden CLI-endringer ikke overlever en `azd provision` på nytt (Bicep er kilden til sannhet og vil overskrive/fjerne manuelle CLI-endringer ved neste provision).

### Hosting-pivot

Bruker har revidert det opprinnelige kravet om egen Windows Server 2016/IIS for produksjon. Ny beslutning:

- **Produksjon:** Flyttes til en administrert skyløsning — **Azure** (App Service for applikasjonen, Azure Database for MySQL – Flexible Server for databasen), i **Norway East/West**-regionen for datalagringssted. Begrunnelse: kryptering i hvile, geo-redundant backup, tilgangsstyring (IAM) og sikkerhetsoppdateringer følger med som administrerte tjenester, i stedet for at bruker må bygge og drifte dette selv på en Windows-boks. Egen Windows Server droppes helt for produksjon — dermed bortfaller også behovet for VPN/RDP-tilgang til en hjemme-/kontorserver som var planlagt i første utkast av Del 1.
- **Utvikling:** Skjer fortsatt **lokalt**, ikke i skyen — bruker foretrekker det fordi sky-basert utvikling/debugging er tregt for den daglige kodesyklusen. Lokalt utviklingsmiljø: Docker Compose med MySQL i container, `dotnet watch` for rask iterasjon, og mock-implementasjoner av BankID/Vipps/SMS/e-post bak samme grensesnitt som brukes i prod — appen "vet ikke" om den snakker med ekte eller falske tjenester.
- **Prinsipp for sikkerhet — arkitektur nå, infrastruktur senere:** Tilgangsstyrings- og auditlogg-kode bygges inn fra dag én (sentral data-tilgangs-lag, autorisasjonstjeneste, append-only audit-logg), men kjører i dev mot enkle lokale dummy-nøkler/ukryptert lokal database. Samme kode kjører i prod, bare koblet til ekte nøkler (Azure Key Vault) og ekte tilgangsstyring (Azure IAM). Det er kun *infrastrukturen bak* koden som trappes opp fra dev til prod, ikke selve kodestien. Implementert i `TestBase.Shared/Security/` (`ICurrentUserContext`, `IAuditLogger`/`EfAuditLogger`) og verifisert i praksis: `/DevDemo` skriver til `audit_log_entries`-tabellen i den lokale MySQL-databasen.
- **Om sky-debugging:** Mye av smerten ved å feilsøke sky-hostede systemer kommer av manglende observability, ikke av skyen i seg selv. Sett opp strukturert logging + Application Insights fra dag én i prod/staging.

## Del 1 — sluttstatus

Del 1s lokale del er **ferdig og verifisert**: `dotnet build` går gjennom, `docker compose up -d` starter lokal MySQL, `dotnet ef`-migrasjoner oppretter skjemaet, `dotnet watch run` starter appen og åpner nettleseren automatisk (`launchSettings.json` er nå lagt til), `/DevDemo` og `/health` svarer begge riktig, og prosjektet er under versjonskontroll med Git. Gjenstående del av Del 1 — selve sky-deploy-pipelinen til Azure — er bevisst utsatt til vi faktisk trenger å driftsette noe.

## Del 2 (slice 1) — admin-skjelett + BankID/2FA-autentisering

**Status: ferdig og verifisert lokalt 2026-08-23.** Omfang for denne slicen (bevisst avgrenset,
bekreftet av bruker): datamodell + ekte autentisering + minimal admin-CRUD. Pris per test,
økonomiske rapporter, backup/restore og organisasjonsstøtte er IKKE del av denne slicen — se
"Åpne punkter til senere faser".

**Datamodell** (`TestBase.Shared/Domain/Administrasjon/`): `Administrator`, `Behandler`,
`BehandlerInvitasjon`, `ToFaktorKode`. Migrasjon `Fase2AdminSkjelett` oppretter
`administratorer`, `behandlere`, `behandler_invitasjoner`, `to_faktor_koder`.

**Autentisering — cookie-basert, ikke full ASP.NET Core Identity:** prosjektet har allerede en
hånd-rullet entitetsmodell og et eget `ICurrentUserContext`-abstraksjonslag; full
Identity/EF Identity-tabeller ville duplisert dette. Passord hashes likevel med den innebygde
`Microsoft.AspNetCore.Identity.PasswordHasher<Administrator>` (krever ingen Identity-tabeller).
`TestBase.Shared` fikk `<FrameworkReference Include="Microsoft.AspNetCore.App" />` for å få
tilgang til denne, samt DataProtection og `IHttpContextAccessor`, uten å bli et Sdk.Web-prosjekt.

**Passord-tilstedeværelse = utviklingsmodus:** jf. kravdokumentet ordrett — det er PER KONTO
(`Administrator.PasswordHash` satt eller ikke), ikke et miljøvalg, som avgjør om kontoen logger
inn med AdminId+passord (rolle `Utvikler`) eller BankID+SMS-2FA (rolle `Administrator`).
Dev-seed i `Program.cs` oppretter én slik konto (`dev-admin` / `utvikler123`) ved oppstart i
Development hvis databasen er tom.

**Rollebytte for utvikler:** en egen claim (`AppClaimTypes.BaseRolle` — omdøpt fra `AdminClaimTypes` i fase 3 da den også ble tatt i bruk for behandler-pålogging) lagrer rollen kontoen
faktisk logget inn med og endres ALDRI av rollebytte — det er det som avgjør om
`Bytt-modus`-siden er tilgjengelig. `ClaimTypes.Role` (det autorisasjon faktisk sjekkes mot) kan
byttes fritt mellom Administrator/Behandler/Pasient/Utvikler for å teste andre roller uten å
logge ut. Konsekvens, bevisst: bytter en utvikler bort fra Utvikler, mister de umiddelbart
tilgang til admin-sidene (siden AdminOmrade-policyen krever Administrator/Utvikler) — det er
poenget (simulerer faktisk hva den rollen kan se), og `Bytt-modus` er nådd via `[Authorize]` +
manuell claim-sjekk, ikke AdminOmrade-policyen, så veien tilbake er alltid åpen.

**Personnummer krypteres i hvile fra dag én** (også i dev — jf. "arkitektur nå, infrastruktur
senere"), via `Microsoft.AspNetCore.DataProtection` og en EF Core `ValueConverter` i
`AppDbContext`. Konsekvens: siden DataProtection ikke er deterministisk, kan personnummer IKKE
slås opp med SQL `WHERE` eller håndheves unikt med en databaseindeks — oppslag
(`AdminAuthenticationService.FinnVedPersonnummerAsync`) og unikhetssjekk (ved opprettelse i
`Administratorer/Ny`) skjer i minnet i stedet. Uproblematisk i praksis: administrator-tabellen
er svært liten (én psykolog + kontorfellesskap). I prod pekes samme kode senere mot Azure Key
Vault ved konfigurasjon alene.

**BankID+2FA-flyt (mock):** `Logg-inn` → (ingen passord) → `MockBankIdProvider` (alltid
vellykket, fast fiktiv testperson) → match mot administratorens (dekrypterte) personnummer →
`ToFaktorKode` genereres, hashes (SHA-256) og "sendes" via `MockSmsSender` (logger til
konsollen) → `Bekreft-kode` verifiserer (maks 5 forsøk, 10 min levetid) → innlogging. Alt
verifisert manuelt ende-til-ende under utvikling, inkl. et konkret funn: to administratorer med
samme personnummer gjør BankID-oppslaget tvetydig (`FirstOrDefault` treffer "feil" konto) — pass
på at kun ÉN konto i systemet noensinne har det faste testpersonnummeret
(`01019012345`) `MockBankIdProvider` returnerer.

**Behandler-invitasjon:** admin taster mobil ELLER e-post → `BehandlerInvitasjonService`
oppretter `Behandler` (status `Invitert`) + en tidsbegrenset (7 dager), engangs
`BehandlerInvitasjon`-token → lenke sendes via mock SMS/e-post → behandler åpner
`/Inviter/Fullfor/{token}` (offentlig, uautentisert side i `Pages/Inviter/`, IKKE i Admin-arealet)
og fyller inn fullt navn + HPR-nr → status blir `Aktiv`. Selve behandler-innlogging/-portal er
Del 3 og finnes ikke ennå — denne siden fullfører kun stamdata.
*(Utvidet betydelig i fase 3 — se "Del 3 (slice 1)" under; `FulltNavn` finnes ikke lenger som eget felt.)*

**Funnet og fikset underveis:** ASP.NET Core sin standard TempData-serialisering
(`DefaultTempDataSerializer`) støtter IKKE `long` — måtte lagres som `string` og parses tilbake
(brukt til å bære administrator-id mellom `Logg-inn` og `Bekreft-kode`-stegene).

**Ikke gjort i denne slicen (se "Åpne punkter"):** ekte BankID-/SMS-/e-post-leverandør (fortsatt
mock), pris per test, økonomiske rapporter, backup/restore, organisasjonsstøtte, enhetstester
for `AdminAuthenticationService`/`BehandlerInvitasjonService`, og polert admin-UI (dagens sider
er funksjonelle, ikke visuelt ferdige).

## Del 3 (slice 1) — behandler-innlogging + registrering + HPR-godkjenning + pasient-CRUD

**Status: ferdig og verifisert lokalt 2026-08-24.** Omfang (bekreftet av bruker): BankID+2FA-
innlogging for behandler, utvidet egenregistrering (flere felt, brukeravtale, e-post/mobil-
verifisering), HPR-godkjenningsflyt, og grunnleggende pasient-CRUD. Rapporter, økonomi-oversikt
og automatiske påminnelser/utsendelser er IKKE del av denne slicen — de avhenger reelt sett av
testrammeverket i fase 4–5 uansett. Se "Åpne punkter til senere faser".

**Refaktorering før utvidelse (ingen atferdsendring for admin):** 2FA-logikken ble løftet ut av
`AdminAuthenticationService` til en delt `ToFaktorService`, med `ToFaktorKode` endret fra
`AdministratorId` til `PrincipalType` (ny enum: `Administrator`/`Behandler`) + `PrincipalId` —
unngår å duplisere sikkerhetskritisk kode (hash/utløp/forsøksbrems) for hver ny prinsipaltype som
trenger 2FA. Samtidig ble `AdminSignIn` generalisert til `AuthSignIn` (tar primitiver i stedet
for et `Administrator`-objekt) og `AdminClaimTypes` omdøpt til `AppClaimTypes`, siden begge nå
brukes av både administrator- og behandler-pålogging.

**Funnet under implementasjon — Area-navn kolliderte med domenetypenavn:** Razor Pages-arealet
ble først kalt "Behandler", men det skaper en C#-navnerom `TestBase.Web.Areas.Behandler` som
skygger for domenetypen `Behandler` (fra `TestBase.Shared.Domain.Administrasjon`) i ALL kode
nestet under `Areas.Admin.*` og `Areas.Behandler.*` — kompilatoren tolker det ubekvalifiserte
navnet `Behandler` som navnerommet, ikke typen. Løsning: arealet heter `Behandlerportal`
(URL-prefiks `/Behandlerportal/...`) i stedet. Generell lærdom for senere Areas i dette
prosjektet: ikke naveme et Area likt en domeneentitet.

**Datamodell** (`TestBase.Shared/Domain/Administrasjon/` og ny `Domain/Pasienter/`): `Behandler`
utvidet kraftig (Fornavn/Etternavn i stedet for FulltNavn, Personnummer — kryptert, samme mønster
som Administrator —, Kontonummer, Arbeidsadresse, Tittel, HprGodkjent/-Utc/-AvAdministratorId,
RegistrertUtc, Epost-/MobilVerifisertUtc, BrukeravtaleGodkjentVersjon/-Utc,
InvitertAvAdministratorId/InvitertAvBehandlerId — begge nullable, nøyaktig én satt). Nye
entiteter: `BehandlerKontaktVerifisering`, `Pasient`, `PasientStatus`, `PasientInvitasjon`.
Migrasjon `Fase3BehandlerOgPasienter`.

**EF-migrasjons-fallgruve — feiltolket kolonne-rename:** `dotnet ef migrations add` genererte
automatisk `RenameColumn(FulltNavn → Arbeidsadresse)` på `behandlere`-tabellen i stedet for
drop+add, fordi EFs heuristikk for å oppdage rename forvekslet det fjernede `FulltNavn`-feltet
med det nye, semantisk helt urelaterte `Arbeidsadresse`-feltet. Ubehandlet ville dette ha flyttet
eksisterende navn-data inn i adressefeltet ved oppgradering. Rettet manuelt i migrasjonsfilen
(drop `FulltNavn` + add `Arbeidsadresse`, og tilsvarende i `Down()`) før den ble kjørt. **Les
alltid gjennom en generert migrasjon når flere kolonner endres samtidig på samme tabell** —
stol ikke blindt på EFs rename-deteksjon.

**Behandler-pålogging er BankID+2FA KUN** — intet passord-unntak (i motsetning til administrator),
jf. kravet ordrett. Admin- og Behandlerportal-området deler samme cookie-scheme; siden hvert
område har sin egen innloggingsside, omdirigerer `OnRedirectToLogin`/`OnRedirectToAccessDenied` i
`Program.cs` til riktig portal basert på forespørselens sti, i stedet for én global `LoginPath`.

**Personnummer-kollisjon på tvers av kontotyper er OK, men ikke innad i samme tabell:** samme
fallgruve som i fase 2 gjelder også her — kun ÉN behandler bør ha det faste
mock-personnummeret (`01019012345`) om gangen, ellers blir BankID-oppslaget tvetydig. En
administrator og en behandler KAN derimot dele personnummer uten konflikt (forskjellige
tabeller/oppslag, realistisk for en behandler som også er administrator).

**Brukeravtale:** versjonert utkast i `Brukeravtale.cs` (IKKE juridisk rådgivning, jf. samme
forbehold som DPIA-utkastet). Godtas som del av `Fullfor`-skjemaet ved førstegangsregistrering;
`GodkjennAvtale`-siden brukes kun senere, hvis `GjeldendeVersjon` økes og en allerede aktiv
behandler logger inn med en utdatert aksept.

**HPR-godkjenning:** ved fullført registrering (begge kontaktkoder bekreftet) sendes en
mock-e-post til ALLE administratorer om å sjekke behandlerens HPR-nummer. 7 dagers prøveperiode
fra `RegistrertUtc` hvor alt virker; etter det blokkeres kun "legg til pasient"-handlingen
(`Pasienter/Ny`, `Gruppeimport`) hvis `HprGodkjent` fortsatt er `false` — IKKE innlogging, jf.
kravet ordrett. Admin godkjenner/tilbakekaller via en toggle på `Administratorer > Behandlere`.

**Bot-/spam-vern:** enkel honeypot + minimumstid-fra-visning (`BotVern.cs`) på den offentlige
`Fullfor`-siden — ikke en ekte CAPTCHA-tjeneste. Reell CAPTCHA (hCaptcha/Turnstile) er en
fremtidig leverandørbeslutning på linje med BankID/Vipps, se "Åpne punkter".

**Pasient-invitasjon sender bevisst ingen lenke ennå:** `PasientInvitasjonService` lagrer et
invitasjonstoken (samme mønster som behandler-invitasjon) for at Del 4 kan bygge videre på det
direkte, men mock-meldingen som sendes nå er ren tekst uten URL, siden pasientens egen
fullføringsside ikke finnes før Del 4. Unngår en død lenke i mock-loggen.
*(Fikset i fase 4 — se "Del 4 (slice 1)" under: `LeggTilAsync` sender nå en ekte lenke.)*

**Verifisert manuelt ende-til-ende:** admin inviterer behandler → fullfør skjema (alle felt +
brukeravtale) → bekreft mobil+e-post-kode → status `Aktiv`, HPR-varsling sendt til alle
administratorer → behandler logger inn med BankID+2FA → legg til pasient enkeltvis → gruppeimport
(inkl. én linje med for få felt, korrekt rapportert som hoppet over, ikke stille forkastet) →
behandler inviterer en kollega-behandler (samme tjeneste som admin bruker) → admin godkjenner
HPR → audit-logg viser alle handlingene. Regresjonstestet: admin-innlogging (passord og
BankID+2FA) fungerer uendret etter 2FA-/claims-refaktoreringen.

**Ikke gjort i denne slicen (se "Åpne punkter"):** pris/rapporter/økonomi, automatiske
test-utsendelser/påminnelser, 10-års auto-sletting av pasientdata, ekte CAPTCHA, enhetstester,
og pasientens egen fullføringsside/portal (Del 4).

## Del 4 (slice 1) — pasientsystem + testmotor-skjelett

**Status: ferdig og verifisert lokalt 2026-08-24.** Omfang (bekreftet av bruker):
pasient-egenregistrering, BankID-innlogging (uten 2FA), pasientens egen side, og et generisk
testmotor-skjelett (definere tester, tildele til pasient, fylle ut side for side). INGEN
Vipps-betalingssperre, INGEN påminnelser, INGEN skåring/rapporter — alt dette hører naturlig
sammen med fase 5 (WHO-5 beviser skåring/rapporter ut med et konkret eksempel) og fase 6
(Vipps/økonomi). Se "Åpne punkter".

**Pasient-registrering — INGEN kontaktverifisering (i motsetning til behandler i fase 3):**
kravdokumentet nevner ikke SMS/e-post-verifiseringskoder for pasient, kun BankID-innlogging
etterpå — det er identitetsbekreftelsen. `PasientInvitasjonService.FullforRegistreringAsync`
setter derfor `Status = Aktiv` direkte ved fullført skjema, ett steg kortere enn behandler-flyten.

**BankID-innlogging uten 2FA for pasient:** kravdokumentet sier eksplisitt "Samme tofaktor
etterpå" for behandler (Del 3), men gjentar det IKKE for pasient (Del 4) — lest bokstavelig som
et bevisst skille, ikke en forglemmelse. `PasientAuthenticationService` har derfor ingen
avhengighet til `ToFaktorService`. Samme kjente personnummer-fallgruve som admin/behandler
gjelder fortsatt: kun ÉN konto (i hvilken som helst av de tre tabellene) bør ha det faste
mock-personnummeret om gangen for at BankID-oppslaget skal være entydig — men en administrator,
en behandler OG en pasient kan trygt dele personnummer samtidig seg imellom, siden hvert
BankID-oppslag kun søker i sin egen tabell (realistisk for én person med flere roller i systemet).

**Testmotor-skjelett** (`Domain/Tester/`): `Test` → `TestSide` → `TestLedd` (definisjon),
`TestTildeling` (tildeling/forsøk) → `TestSvar` (per-ledd-svar). `TestService` samler
forfatning, tildeling og utfylling. Fire svartyper dekket (`Likert5`, `VisuellAnalogSkala`,
`JaNei`, `Fritekst`), rendret som radioknapper/range-input/tekstfelt i
`Areas/Pasientportal/Pages/Tester/Fyll.cshtml`. Fremdrift, side-instruksjoner,
Neste/Forrige/Lagre/Ferdig og en belønningsside er alle implementert. `LagreSvarAsync` tar en
eksplisitt `markerFullfort`-parameter (ikke utledet fra "er dette siste side") — Ferdig-knappen
uttrykker intensjon, posisjon er bare hvor knappen tilfeldigvis vises.

**Admin-side for å forfatte tester** (`Areas/Admin/Pages/Tester/`): kun opprett (test → side →
ledd), ingen rediger/slett i denne slicen — jf. kravets "det er ikke nødvendig å lage et system
for å generere tester uten å gå gjennom hovedsystemet", altså er det nok at forfatning skjer
gjennom hovedsystemets admin-UI, selv i sin enkleste form.

**Lokalisering bevisst IKKE bygget:** kravet krever at "hver test skal kunne støtte
lokalisering til mange språk", men å designe et språk-skjema uten et konkret andrespråk å teste
det mot ville sannsynligvis blitt feil og måtte gjøres om — utsatt til fase 5 (WHO-5 kan trenge
norsk+engelsk), se "Åpne punkter".

**Fallgruve fra fase 3 sjekket, men IKKE inntruffet denne gangen:** siden `Pasient` kun fikk nye
kolonner (ingen fjernet/omdøpt), genererte `dotnet ef migrations add` en ren migrasjon uten
feiltolkede rename-er denne gangen — bekrefter at risikoen faktisk er knyttet til
fjern+legg-til-samtidig-mønsteret, ikke til antall nye kolonner alene.

**Area-navngivning — samme lærdom som fase 3, fulgt riktig fra start:** pasientportalen heter
`Pasientportal`, ikke `Pasient`, nettopp for å unngå gjentakelse av
navnerom-skygger-domenetype-kollisjonen fra fase 3. `Areas/Admin/Pages/Tester/` og
`Areas/Pasientportal/Pages/Tester/` (begge navngitt "Tester", entall "Test" som type) kolliderer
IKKE — kollisjonsregelen krever eksakt navnematch mellom navnerom-segment og typenavn, og
"Tester" ≠ "Test".

**Funnet og fikset i eksisterende kode:** `Behandlerportal/Pasienter/Detaljer.cshtml` hadde en
gjenglemt lenke til den gamle `/Behandler/Pasienter`-stien fra før fase 3s area-omdøping (fanget
ikke opp av søk-og-erstatt-et den gang siden mønsteret var litt annerledes). Rettet til
`/Behandlerportal/Pasienter` i samme slag som denne slicen uansett rørte filen.

**Verifisert manuelt ende-til-ende:** admin oppretter en test (2 sider, 4 ledd — én av hver
svartype) → behandler tildeler den til en pasient → pasient fullfører egenregistrering via ekte
invitasjonslenke (ingen lenke ble sendt i fase 3 — det er fikset nå) → logger inn med BankID
(uten 2FA) → "Min side" viser tildelingen → fyller ut side 1 (Lagre+Neste, bekreftet lagret i
databasen og status ble `Startet`) → side 2 (Ferdig) → belønningsside vises → behandlerens
pasientdetaljside viser `Fullfort` med start-/sluttidspunkt → `audit_log_entries` har rader for
alle stegene. Regresjonstestet: admin- og behandler-innlogging uendret.

**Ikke gjort i denne slicen (se "Åpne punkter"):** lokalisering, Vipps-betalingssperre,
påminnelser (frist/varighet lagres men håndheves ikke), skåring, rapporter (per besvarelse og
over tid), rediger/slett av tester/sider/ledd, enhetstester.

## Del 5 (slice 1) — WHO-5 ende-til-ende (skåring, rapport, regenerering)

**Status: ferdig og verifisert lokalt 2026-08-24.** Omfang (bekreftet av bruker): full WHO-5
ende-til-ende, kun norsk — lokalisering fortsatt bevisst utsatt, se "Åpne punkter". Formålet var
å bevise testrammeverket ut med et konkret, virkelig validert instrument, ikke bare
skjelett-data.

**Kilder brukt (offisiell norsk oversettelse, hentet 2026-08-24):** WHO-hostet norsk PDF
(oversatt av overlege Olaf Bakke, Arendal 2004, versjon 1.1),
`https://cdn.who.int/media/docs/default-source/mental-health/five-well-being-index-(who-5)/who-5_norwegian.pdf`,
samt Psyktestbarn (r-bup.no) som bekreftelse på instruksjonstekst og skåringsformel. Et første
PDF-søketreff viste seg å faktisk være dansk til tross for å være merket norsk i søkeresultatet —
oppdaget ved lesing, korrigert ved å finne det faktiske WHO-hostede norske dokumentet via et
gjettet CDN-URL-mønster. **Forbehold arvet fra WHOs eget dokument** (samme som DPIA-/
brukeravtale-utkastene): oversettelsen er ikke WHOs ansvar for nøyaktighet, engelsk versjon er
bindende ved uoverensstemmelse — bør kvalitetssikres før reell klinisk bruk.

**Designhull WHO-5 avdekket i testmotoren fra fase 4:** `TestSvartype.Likert5` var hardkodet til
nøyaktig 5 punkter (verdi 1–5) — WHO-5 er en 6-PUNKTS skala (verdi 0–5). Løst ved å generalisere
til en data-drevet N-punkts Likert-skala (`Likert5` → `LikertSkala`) i stedet for én ny
enum-verdi per punktantall, gjenbrukbart for fremtidige tester med andre skalastørrelser.
`Svaralternativer` gikk fra implisitt CSV-liste av labels til eksplisitte `"verdi:tekst"`-par
(`TestLeddSvaralternativer.Parse`), kommaseparert, i den REKKEFØLGEN de skal vises — WHO-5 viser
"Hele tiden" (5) først, "Aldri" (0) sist, samme rekkefølge som originaldokumentet, IKKE sortert
på verdi. Konsekvens for eksisterende dev-data: den ene demo-testen fra fase 4-verifiseringen
brukte gammelt `Likert5`-CSV-format som ikke lenger var gyldig — slettet manuelt via SQL som del
av verifiseringen (ren syntetisk dev-testdata, ikke en produksjonsmigrasjon).

**`Test.Kode`** (ny, nullable, unik indeks når satt): dobbel rolle — identifiserer testen for
idempotent regenerering, OG nøkkel for å slå opp riktig skåringsberegner. WHO-5 har
`Kode="who5"`. Migrasjon `Fase5Who5OgSkaaring` (kun denne ene kolonnen + indeks).

**Regenereringsmekanisme** (`Domain/Tester/InnebygdeTester/`): `IInnebygdTestSeeder`
(`Kode` + `SeedAsync`) + `Who5TestSeeder`, idempotent (sjekker `FinnesTestMedKodeAsync` før
oppretting). Kalles fra to steder via samme `IEnumerable<IInnebygdTestSeeder>`-registrering: (1)
`Program.cs`s dev-seed-blokk ved oppstart i Development, og (2) en "Regenerer innebygde tester"-
knapp på `Admin/Tester/Index` som virker i ALLE miljøer — jf. kravets "husk alltid å lage
regenerering av tester" lest som et generelt krav, ikke bare en dev-bekvemmelighet. Verifisert
manuelt: WHO-5 slettet via SQL (test+side+ledd), knapp trykket, testen gjenskapt identisk
(samme navn, kode, 1 side, 5 ledd) uten omstart av appen.

**Skåringsmotor** (`Domain/Tester/Skaaring/`): `TestSkaaring`-record
(`RaaSkaar`/`RaaSkaarMaks`/`ProsentSkaar`/`Fortolkning`), `ITestSkaaringsberegner`-grensesnitt
(`TestKode` + `BeregnSkaaring(svar)`), `Who5Skaaringsberegner` — råskår = sum (0–25),
prosentskår = råskår × 4, flagget for nærmere undersøkelse hvis råskår < 13 ELLER noe
enkeltsvar er 0/1. `TestService` fikk `BeregnSkaaringAsync(tildelingId)` (null hvis testen ikke
har noen registrert beregner) og `HentSkaaringHistorikkAsync(pasientId, testKode)` for
"over tid"-rapporten (kronologisk liste over alle fullførte tildelinger med samme `Kode`).

**Rapport-side** (`Behandlerportal/Pasienter/Rapport.cshtml`, eierskapssjekk mot innlogget
behandler samme mønster som `Detaljer.cshtml`): råskår/maks, prosentskår (enkel CSS-stolpe, ingen
eksternt graf-bibliotek), fortolkningstekst, full svartabell (spørsmål + gitt svar-label), og
en "Utvikling over tid"-tabell (vises kun ved >1 fullført besvarelse av samme test) som markerer
"(signifikant endring)" når endring i prosentskår ≥ 10 % — ordrett fra WHO-5-veiledningen.
`Detaljer.cshtml` fikk en "Se rapport"-lenke per fullført tildeling, KUN der testen faktisk har
en registrert skåringsberegner (`TestService.HarSkaaringsberegner`).

**Verifisert manuelt ende-til-ende, begge fasit-tilfeller:**
- Alle svar = 5: råskår 25/25, prosentskår 100, "Over grenseverdien — indikerer ikke i seg selv
  behov for videre undersøkelse."
- Alle svar = 0 (ny tildeling, samme pasient): råskår 0/25, prosentskår 0, "Under grenseverdien
  (13) ... WHO-5-veiledningen anbefaler å gå videre med nærmere undersøkelse."
- "Utvikling over tid" viste begge besvarelsene kronologisk med endring −100 markert
  "(signifikant endring)" — over/under-terskel-logikken fungerer i begge retninger.

Regresjonstestet: `dotnet build` rent (0 advarsler/feil), integrasjonstestsuiten
(`HeleFlytenTests`, oppdatert for `LikertSkala`) grønn, admin-innlogging (passord-modus) og
BankID+2FA-flyten uendret.

**Ikke gjort i denne slicen (se "Åpne punkter"):** lokalisering, enhetstester for
`Who5Skaaringsberegner`/`Who5TestSeeder`, WHO-5-spesifikke assertions i integrasjonstestsuiten
(kjøres i dag kun mot en generisk test), rediger/slett av `Test.Kode` i admin-UI.

## Feilrettinger funnet ved reell bruk (2026-08-25)

Bruker rapporterte to ting som ikke virket ved manuell testing i nettleser (ikke fanget opp av
integrasjonstestsuiten, som bruker `RequestUri`-substring-sjekker og leser mock-meldinger
direkte fra `ISmsSender`/`IEmailSender` i stedet for å klikke lenker i UI):

**"Legg til pasient" ga 404:** `Behandlerportal/Pasienter/Index.cshtml` og `Gruppeimport.cshtml`
hadde fortsatt harde lenker til `/Behandler/Pasienter/...` (uten "portal") — et gjenglemt
levning fra area-omdøpingen i fase 3 (`Behandler` → `Behandlerportal`, se "Del 3 (slice 1)").
Fase 4s opprydding fanget kun opp ett tilsvarende tilfelle i `Detaljer.cshtml`; disse fire
(`Index.cshtml` × 3, `Gruppeimport.cshtml` × 1) ble oversett. Rettet til `/Behandlerportal/...`.
**Lærdom:** et `grep` etter `href="/Behandler/` på tvers av HELE `src/` bør kjøres som en siste
sjekk hver gang et Area omdøpes, ikke stole på å ha fanget opp alle stedene manuelt.

**Behandler-/pasient-invitasjon "virket ikke":** koden fungerte teknisk (invitasjon ble
opprettet, lenke generert riktig), men lenken ble KUN logget via `ILogger` inni
`MockSmsSender`/`MockEmailSender` — synlig bare i konsollen der `dotnet watch run` kjører, ikke
noe sted i selve nettleser-UI-et. Uten tilgang til den konsollen (eller uten å vite man skulle
lete der) var det umulig å faktisk fullføre en invitasjon. Fikset ved å la
`BehandlerInvitasjonService.InviterAsync` og `PasientInvitasjonService.LeggTilAsync` returnere
lenken direkte (nye records `BehandlerInvitasjonResultat`/`PasientInvitasjonResultat`), og vise
den som en klikkbar lenke rett i bekreftelsen på alle fire berørte sider (`Admin/Behandlere/Inviter`,
`Behandlerportal/Behandlere/Inviter`, `Behandlerportal/Pasienter/Ny`, `Behandlerportal/Pasienter/Gruppeimport`
— sistnevnte fikk én lenke per opprettet pasient). `Pasienter/Ny` gikk samtidig fra å redirecte
rett til pasientlisten (ingen bekreftelse vist) til å vise samme "opprettet + lenke"-mønster som
gruppeimport allerede hadde. Dette er fortsatt mock (ingen ekte SMS/e-post sendes), men admin/
behandler kan nå selv kopiere lenken videre til personen de inviterer, eller klikke seg gjennom
den under testing, uten terminaltilgang.

**Driftsfunn under feilsøkingen — port-mismatch + hengende prosesser:** appen som faktisk kjørte
og ble testet mot var startet på port 5299 en gang tidligere i prosjektet, men
`launchSettings.json` (eneste profil, `"https"`) har alltid vært `https://localhost:7257;http://localhost:5257`
— 5299 var aldri den konfigurerte porten, bare en avvikende manuell overstyring fra en tidligere
øving som aldri ble skrevet tilbake til `launchSettings.json`. Kombinert med to `TestBase.Web.exe`-
prosesser som satt og låste build-outputen (klassisk "kjør aldri `dotnet ef`/`dotnet build` mens
`dotnet watch run` kjører samtidig i et annet vindu"-fallgruve, se under, men her var det TO
gamle prosesser, ikke én aktiv), gjorde det at Razor-endringer ikke ble hot-reloadet inn i den
kjørende appen bruker testet mot. Løst ved å drepe de gamle prosessene og starte
`dotnet watch run` på nytt uten portoverstyring — appen kjører nå på standardporten fra
`launchSettings.json`. **Lærdom:** hvis nettleser-testing ikke reflekterer nylige kodeendringer
til tross for at `dotnet watch run` "kjører", mistenk (1) feil port (sjekk `launchSettings.json`
i stedet for å anta), og (2) flere/hengende `TestBase.Web.exe`-prosesser (`tasklist`/`netstat -ano`)
som låser build-outputen uten selv å svare på requests på riktig port.

## Offentlig design + samlet profesjonell innlogging (2026-08-30)

Bruker ba om et visuelt design (referansebilde av en profesjonell konsulent-nettside) for
forsiden, og deretter om at all funksjonalitet skulle bringes inn i samme design, samt en rekke
innloggings-/personvernendringer. Gjort i to omganger:

**Design:** Ny `wwwroot/css/site.css` (oransje/mørk fargepalett, vinklede figurer i hero,
responsivt fra mobil til desktop) + generiske komponentstiler (kort, tabeller, skjemaer, knapper)
som treffer ALLE eksisterende sider via attributt-/strukturselektorer (`table[border]`,
`form:not([style*="display:inline"])`, `p[style*="color: darkred"]` osv.) — bevisst valgt
FREMFOR å redigere alle ~30 `.cshtml`-filene enkeltvis, siden skjemamønsteret var 100 % identisk
på tvers av admin/behandler/pasient-sidene. `wwwroot/img/hero-placeholder.svg` er en tydelig
merket dummy — bytt ut når ekte bilder finnes.

**Samlet innlogging for administrator og behandler:** Ny `Pages/Konto/{LoggInn,BekreftKode,LoggUt}`
(utenfor Areas) erstatter de tidligere separate `Areas/Admin/Pages/Konto/*` og
`Areas/Behandlerportal/Pages/Konto/*`-sidene. Ett skjema, ingen rollevalg — BankID-knappen finner
personen via personnummer og logger inn på HØYESTE tilgjengelige rolle (administrator sjekkes før
behandler i `LoggInnModel.OnPostAsync`), i stedet for at brukeren velger portal selv. AdminId+
passord (kun utviklingsmiljø) er nå et sekundært ETT-STEGS alternativ i en `<details>`-boks på
samme side (tidligere et to-stegs skjema på samme URL) — se `Pages/Konto/LoggInn.cshtml(.cs)`.
Pasient beholder egen separat innlogging (`Areas/Pasientportal`), siden pasienter er en egen
gruppe uten rolleoverlapp, med egen offentlig landingsside `/Pasienter` (separat fra `/`, som nå
er admin/behandler sin inngang). `Program.cs` sin `InnloggingsstiFor` og cookie-`LoginPath` er
oppdatert tilsvarende.

**Viktig konsekvens for testing/dev-seed:** siden `MockBankIdProvider` alltid returnerer samme
faste personnummer, vil en administrator OG en behandler med dette personnummeret nå kollidere —
den samlede innloggingen velger alltid administrator. `HeleFlytenTests.cs` måtte oppdateres til å
arkivere test-BankID-administratoren før behandler-BankID-steget testes (se kommentar i testen).
Dette er tilsiktet oppførsel (jf. brukerens ønske om "høyeste rolle"), ikke en bug.

**Innlogget-som-indikator:** Lagt til `Innlogget som: @CurrentUser.DisplayName` i header
(`_Layout.cshtml`) ved siden av "Logg ut" — fantes tidligere kun som tekst på den gamle
dev-status-forsiden, som ble fjernet i designomgangen. Uten denne var det ingen sidenøytral måte
å bekrefte hvem som er innlogget (både i appen og i integrasjonstesten).

**Etter-innlogging-mål endret:** admin havner nå på `/Admin/Administratorer` (var `/Index`),
behandler på `/Behandlerportal/Pasienter` (var `/Index`, med `GodkjennAvtale` fortsatt i mellom
ved behov) — landing rett i arbeidsflaten i stedet for på markedsføringsforsiden.

**CAPTCHA:** Nytt grensesnitt `ICaptchaProvider` (`TestBase.Shared/Providers/`) +
`MockCaptchaProvider` (`Providers/Mock/`) — samme mønster som BankID/Vipps/SMS/e-post. Mock-
implementasjonen er et enkelt regnestykke ("hva er X + Y?") signert med DataProtection (samme
mekanisme som krypterer personnummer) i et skjult felt, uten server-side sesjon. Lagt til på alle
tre innloggingssider (samlet admin/behandler + pasient). Dette er FORTSATT ikke en ekte
tredjeparts-CAPTCHA (hCaptcha/Turnstile) — se oppdatert punkt under "Åpne punkter".

**Dev-bar skjult etter innlogging:** `Env.IsDevelopment()`-varselet (lenker til `/DevDemo`,
`/health`) vises nå kun for ikke-innloggede besøkende, ikke for noen innlogget rolle — unngår at
det ligger og forstyrrer i alle tre portalene etter innlogging.

**EU-cookie-varsel:** Ny `Pages/Shared/_CookieSamtykke.cshtml`-partial (ren HTML/CSS/inline JS,
ingen ekstern leverandør) + `/personvern`-side. Rent informativt — appen bruker kun strengt
nødvendige cookies (innlogging, antiforgery, selve samtykkevarselet), som juridisk sett ikke
krever aktivt samtykke, men varselet gir åpenhet uten å gate noen funksjonalitet bak et
samtykkevalg (unngikk bevisst `CookiePolicyMiddleware`/`ITrackingConsentFeature` for å ikke
risikere å blokkere innloggingscookien).

**"Husk meg" er allerede cookie-basert:** ingen endring nødvendig — `AuthSignIn.LoggInnAsync`
har alltid satt `IsPersistent = huskMeg` på innloggingscookien.

## Rediger-funksjon for administrator/test/pasient (2026-08-30)

Lagt til en grønn "Rediger"-knapp ved siden av "Arkiver" (og "Rediger sider" for tester) på de tre
oversiktssidene: `Admin/Administratorer`, `Admin/Tester`, `Behandlerportal/Pasienter`. Hver har nå
en `Rediger/{id}`-side (GET forhåndsutfyller, POST lagrer) som følger nøyaktig samme
skjemamønster som de eksisterende "Ny"-sidene, og arver dermed kort-designet fra CSS-en uten
noen egen styling. `TestService` fikk `HentTestAsync`/`OppdaterTestAsync`. Personnummer er bevisst
redigerbart for administrator/pasient (samme i-minnet-unikhetssjekk som ved opprettelse,
ekskludert entiteten selv) — nyttig for å rette skrivefeil, men endrer BankID-identitetsmatching
hvis det gjøres etter at personen har logget inn.

Ny testklasse `RedigerTests.cs` (samme collection/database som `HeleFlytenTests`) dekker alle tre.
**Lærdom:** siden alle tester i `TestBaseCollection` deler én database, må enhver test som logger
inn en behandler via BankID (fast mock-personnummer) enten bruke unike identifikatorer ELLER
selv arkivere det den oppretter etterpå — `RedigerPasient`-testen lot først en aktiv
testbehandler stå igjen med det delte personnummeret, som gjorde `HeleFlytenTests` sitt eget
behandler-BankID-steg tvetydig (og dermed feilslått) når det kjørte etterpå i samme test-run.
Fikset ved å arkivere behandleren igjen på slutten av testen (samme prinsipp som HeleFlytenTests
allerede bruker for administrator-kollisjonen).

## Tildelingsflyt for tester + BankID personnummer-overstyring + varslingspreferanse (2026-08-30)

Tre relaterte tilføyelser etter brukertilbakemelding om at (1) man ikke kunne bytte mellom flere
BankID-mock-personer for å teste ulike roller, (2) testmotoren manglet reelt innhold utover
skjelettet/WHO-5, og (3) det ikke fantes noen samlet måte å tildele tester til flere pasienter på
én gang.

**BankID personnummer-overstyring (dev-only):** `IBankIdProvider.AuthenticateAsync` fikk en ny
`string? personnummerOverride`-parameter (lagt FØRST, med `CancellationToken` fortsatt sist —
alle kallsteder oppdatert til navngitte argumenter, jf. fallgruven om posisjonelle kall lenger ned
i dette dokumentet). `MockBankIdProvider` returnerer det angitte personnummeret hvis satt, ellers
samme faste testperson som før. Et nytt tekstfelt "Personnummer (kun utviklingsmiljø)" er lagt til
på `/Konto/LoggInn` og `/Pasientportal/Konto/LoggInn`, kun synlig i Development — lar en tester
logge inn/registrere flere ulike personer uten å måtte arkivere den forrige testkontoen først
(løser fallgruven om at `MockBankIdProvider` alltid ga samme personnummer).

**Testkategorier (kun struktur, ikke nytt testinnhold ennå):** Ny `TestKategori` +
`TestKategoriKobling` (mange-til-mange, samme mønster med rene long-FK-er og eksplisitt
koblingsentitet som resten av modellen — ingen EF-navigasjonsegenskaper noe sted). Faste
kategorier seedes idempotent av `TestService.SikreStandardkategorierAsync` (kalt fra
`Who5TestSeeder`, som også kobler WHO-5 til "Kjerne"): Allianse, Angst, Depresjon, Funksjon,
Kjerne, Nevropsykologiske, Utredning — alfabetisk. **Bevisst IKKE fylt med nytt testinnhold i
denne omgangen** (brukeren ba eksplisitt om kun strukturen nå, instrumenter kommer senere) — de
fleste kategoriene er derfor tomme placeholdere inntil videre. Ingen admin-UI for å
opprette/redigere/slette kategorier ennå.

**Tildelingsflyt (`/Behandlerportal/Tildel` og `/Admin/Tildel`):** Ny `TestTildelingsService`
(steg 1: velg pasienter — admin ser alle ikke-arkiverte på tvers av behandlere, behandler ser kun
sine egne; steg 2: tre-visning av kategori→tester, alle utvidet, checkbox synkronisert på tvers av
kategorier via `wwwroot/js/tildel.js` siden en test kan ligge i flere kategorier; native
`<dialog>`-oppsummering client-side før innsending). `TestTildeling` fikk et nullable
`TildeltAvAdministratorId` ved siden av det nå nullable `TildeltAvBehandlerId` — samme
dobbelt-aktør-mønster som `BehandlerInvitasjon` — siden både behandler og admin nå kan tildele.
Pasienten varsles på kanalen(e) hen valgte ved registrering (ny `Varslingspreferanse`-enum på
`Pasient`, standard Begge, valgt via radioknapper på `PasientRegistrering/Fullfor`), med fallback
til hva pasienten faktisk har av kontaktinfo hvis den foretrukne kanalen mangler. Lenkene til hver
tildelte test vises direkte på resultatsiden (samme "vis lenken i UI, ikke bare i mock-loggen"-
prinsipp som `BehandlerInvitasjonResultat`/`PasientInvitasjonResultat`) — nyttig siden en pasient
uten fullført kontaktverifisering ellers ikke kan finne lenken sin. En pasient uten verken
mobilnummer eller e-post vises grået ut og ikke-valgbar i steg 1 (ingen vits i å tildele en test
ingen kan varsles om via denne flyten — vanlig enkelttildeling på `Behandlerportal/Pasienter/Detaljer`
finnes fortsatt for det tilfellet).

Verifisert med full ende-til-ende curl-basert manuell test (admin-innlogging → tildel WHO-5 til to
pasienter → resultatside med SMS/e-post-status og fungerende lenke) og eksisterende
integrasjonstester (`HeleFlytenTests`, 4/4 grønne etter endringen).

## BankID personnr-forhåndsutfylling fra testlenke + 2FA-kode-synlighet + betrodd enhet (2026-08-30)

Tre oppfølgingsfikser etter reell bruk av tildelingsflyten over: (1) pasienten fikk "du har ingen
tildelte tester" på Min side, (2) admin/behandler fikk aldri se 2FA-SMS-koden i det hele tatt, og
(3) ønske om å slippe SMS hver gang på en kjent nettleser.

**Rotårsak til "ingen tildelte tester":** IKKE en databasefeil — testene lå riktig i databasen.
Pasienten logget bare inn som EN ANNEN pasient enn den testen faktisk var tildelt, fordi
lenken pekte rett på `/Pasientportal/Tester/Fyll/{id}` uten noen kobling til hvilket
(mock-)personnummer akkurat DEN pasienten har. Uten å vite riktig personnummer endte man opp med
enten feil test-pasient eller den faste mock-personen — begge med tom tildelingsliste. Løst
generelt (ikke bare for nylig genererte lenker) ved å utvide `Program.cs`' `OnRedirectToLogin`
(`InnloggingsstiForAsync`): når en ubeskyttet forespørsel til nøyaktig
`/Pasientportal/Tester/Fyll/{tildelingId}` blir omdirigert til innlogging, slår vi opp
tildelingens pasient og legger personnummeret ved som `?personnummer=`-parameter — KUN i
Development (aldri i produksjon, siden ekte BankID uansett ignorerer det og vi ikke vil ha
personnummer i URL-er unødvendig). Samtidig la vi til en generell `?returnUrl=`-parameter på ALLE
login-omdirigeringer (validert med `Url.IsLocalUrl` før bruk, jf. open-redirect), slik at man også
havner rett tilbake på siden man egentlig prøvde å besøke — ikke bare på Min side/forsiden.
`Pages/Konto/LoggInn`/`BekreftKode` og `Areas/Pasientportal/Pages/Konto/LoggInn` leser og bærer
`ReturnUrl` videre (skjult felt i skjemaet, siden query string ikke overlever et POST av seg selv).

**2FA-kode usynlig i UI:** Samme fallgruve som invitasjonslenkene i fase 3/5 (mock-tjenester logger
KUN til `ILogger`) hadde IKKE blitt fikset for selve 2FA-SMS-koden. `ToFaktorService.StartAsync`
returnerer nå den genererte koden; `AdminAuthenticationService`/`BehandlerAuthenticationService.
StartToFaktorAsync` propagerer den videre til `Pages/Konto/LoggInn.cshtml.cs`, som (kun i
Development) legger den i TempData for `BekreftKode.cshtml` å vise direkte i en dev-hint-boks.

**Betrodd enhet (hopp over 2FA en stund):** Ny `TestBase.Web/Security/BetroddEnhet.cs` — etter en
vellykket BankID+SMS-2FA settes en egen, tidsbegrenset (DataProtection
`ToTimeLimitedDataProtector`, IKKE en vanlig ukryptert cookie-verdi) cookie
`testbase_betrodd_administrator`/`_behandler` som binder nettleseren til AKKURAT den kontoen i
`Auth:BetroddEnhetDager` dager (config, standard 30 — samme mønster som eksisterende
`Auth:RememberMeDays`). En påfølgende BankID-innlogging fra samme nettleser for samme konto
hopper da over SMS-steget helt (`Pages/Konto/LoggInn.cshtml.cs` sjekker `BetroddEnhet.ErBetrodd`
rett før den ellers ville sendt SMS-koden) — etter utløp kreves SMS igjen. Uavhengig av og i
tillegg til den eksisterende "Husk meg"-cookien (som styrer selve øktens levetid, en annen ting).
Denne cookien lever i `TestBase.Web`, ikke `TestBase.Shared`, jf. det eksisterende prinsippet om at
autentiseringstjenestene i Shared bevisst ikke har noen HttpContext/cookie-avhengighet.

**Fallgruve oppdaget under verifisering:** Git Bash (MSYS) konverterer automatisk et
kommandolinje-argument som begynner med `/` (f.eks. `--data-urlencode "ReturnUrl=/Pasientportal/..."`)
til en Windows-sti (`C:/Program Files/Git/Pasientportal/...`) FØR curl noensinne ser det — ga et
falskt "bug" som så ut som at ReturnUrl ikke ble bundet server-side, mens det i virkeligheten var
verdien som ble sendt som var korrupt. Sett `MSYS_NO_PATHCONV=1` foran slike curl-kommandoer ved
manuell/scriptet testing av skjemafelt som starter med skråstrek.

## Meldinger og oppgaveliste — rapportgodkjenning, betrodd deling, daglig påminnelse (2026-08-30)

**Rapportgodkjenning + delingsbryter:** `TestTildeling` fikk `RapportGodkjentUtc` (behandler MÅ
eksplisitt godkjenne en fullført rapport — `TestService.GodkjennRapportAsync`) og
`RapportSynligForPasient` (egen, valgfri bryter — kun betydningsfull/tilgjengelig ETTER
godkjenning, standard false — `TestService.SettRapportSynlighetAsync`). Pasienten ser ALDRI en
rapport med mindre BEGGE er satt — godkjenning alene deler ikke automatisk. Ny lesetilgang for
pasienten på `Pasientportal/Tester/Rapport/{id}` (viser en vennlig "ikke klar ennå"-melding, ikke
NotFound, hvis ikke delt — tildelingen er tross alt legitimt pasientens egen). `MinSide` lenker til
den når den er klar.

**Meldinger (BehandlerMelding):** Et enkelt lest/ulest-innbokssystem — `TestService.LagreSvarAsync`
oppretter automatisk en melding til pasientens FAKTISKE behandler (`Pasient.BehandlerId`, ikke
nødvendigvis den som tildelte testen — en admin kan ha gjort det) hver gang en tildeling markeres
fullført. Uleste meldinger vises som en tallboble (`.varsel-badge`) ved siden av "Oppgaver" i
navigasjonen (`_Layout.cshtml`, injiserer `BehandlerMeldingService`/`TestService` direkte for å
telle — samme pragmatiske mønster som resten av appen, ingen ViewComponent-lag innført). Å åpne
rapporten for den aktuelle tildelingen markerer meldingen lest.

**Oppgaveliste (`/Oppgaver` i alle tre Areas):** Samme URL-mønster og
`[Authorize(Policy = "...")]` direkte på PageModel (for få sider til å rettferdiggjøre
AuthorizeAreaFolder, som `Pasientportal/MinSide`/`Tester/Fyll` allerede gjør) — men helt ulikt
innhold per rolle: pasient ser egne ubesvarte tester, behandler ser to lister (fullførte tester som
venter på godkjenning + ikke-besvarte tester tildelt egne pasienter, kun sistnevnte til oversikt —
ingen handling kreves der), admin ser en placeholder inntil feedback-systemet bygges. Behandler fikk
også en egen `Behandlerportal/MinSide` (fantes ikke fra før — kun Pasientportal hadde det) med
meldingsinnboksen, og `Behandlerportal/Innstillinger` for varslingspreferanser.

**Daglig påminnelse:** `Behandler` fikk `OnskerDagligPaaminnelse` (av/på), `PaaminnelseKanal`
(gjenbruker `Varslingspreferanse`-enumen fra Pasient — samme "hvordan vil du varsles"-konsept,
kryssreferert fra `Domain/Administrasjon` selv om enumen bor i `Domain/Pasienter`, bevisst ikke
duplisert) og `SistPaaminnetUtc` (hindrer dobbeltsending samme UTC-dag). `PaaminnelseService`
(Shared, testbar uten HttpContext) bygger meldingen og sender via SMS/e-post etter samme
fallback-til-faktisk-kontaktinfo-logikk som `TestTildelingsService`. **VIKTIG personvernvalg**:
meldingsteksten bruker ALDRI pasientnavn, kun pasient-ID (f.eks. "Pasient 7") — SMS/e-post er ikke
sikre kanaler; fullt navn vises først etter innlogging via lenken til oppgavelisten. Selve
"hver dag"-logikken er en enkel, selvhelbredende `DagligPaaminnelseBakgrunnstjeneste`
(`BackgroundService` i `TestBase.Web`, sjekker hvert 15. minutt om konfigurert klokkeslett
— `Varsling:PaaminnelseKlokkeslettUtc`, standard 07 UTC — er passert OG at noen faktisk venter,
i stedet for en presis engangs-timer som ikke tåler nedetid rundt selve klokkeslettet). En
"Send test-påminnelse nå"-knapp i `Innstillinger` lar behandler teste umiddelbart i dev, samme
prinsipp som "Regenerer innebygde tester" i Admin/Tester.

**Kjent gap:** `Varsling:BaseUrl` (lenken i påminnelsen) MÅ settes eksplisitt i konfigurasjon ved
reell drift — en bakgrunnstjeneste har ingen HTTP-forespørsel å lese `Request.Scheme`/`Host` fra
slik `TestTildelingsService`/`Program.cs`s `InnloggingsstiForAsync` har. Faller tilbake til
`https://localhost:7257` i dev.

**Ny fallgruve funnet ved verifisering:** Razors "betinget attributt"-oppførsel (der en
`bool`-typet `@(...)`-uttrykk som HELE verdien av et rent HTML-attributt gjør at Razor render en
MINIMERT boolsk attributt-form — `attributtnavn="attributtnavn"` når true, attributtet utelates
helt når false) gjelder for ALLE attributter bundet på denne måten, ikke bare ekte boolske
HTML-attributter som `disabled`/`checked`. Et `<input type="hidden" name="synlig"
value="@(!Model.X.Bool)" />` rendret bokstavelig `value="value"` i stedet for `value="True"` —
usynlig i vanlig bruk (ser riktig ut i markup ved rask sjekk) men brøt server-side
`bool`-modellbinding fullstendig (knappen gjorde alltid det motsatte av det den skulle). Fikset ved
eksplisitt `.ToString()` på uttrykket, som tvinger `string`-typen og dermed unngår den boolske
spesialbehandlingen. Sjekk ALLTID generert HTML (ikke bare Razor-kildekoden) for et skjult felt
bundet til et negert/beregnet bool-uttrykk.

## Rapportvisning som A4-"papir" + godkjenn/forkast/kopier/skriv ut/send (2026-08-31)

Behandlers rapportside (og pasientens lesetilgang) fikk et fullstendig visuelt og funksjonelt
grunnsystem etter bruker-tilbakemelding ("skal se ut som et papirark").

**Visning:** Rapporten er nå delt opp i "ark" (`.rapport-ark`) — én forside (tittel/pasient/skåring/
fortolkning), én PER TestSide i testen (samme sidestruktur som forfatning/utfylling — se
`TestMedInnhold.Sider`/`AlleLedd`), og — hvis pasienten har flere fullførte besvarelser av samme
test — en historikk-side til slutt. Hvert ark er stylet som et A4-ark (`width`/`min-height` i `cm`,
`box-shadow: var(--shadow)`, hvit bakgrunn) med sidetall i bunnen. Sideflipping
(`wwwroot/js/rapport.js`) skjuler alle unntatt gjeldende ark via `hidden`-attributtet og viser
Forrige/Neste + "Side X av Y" — rent DOM, ingen server-tur. `@@media print`-regler tvinger ALLE ark
synlige igjen (`[hidden] { display: block !important; }`) med `page-break-after: always`, skjuler
verktøylinje/handlinger/site-header/footer, og setter `@@page { size: A4; }` — så utskrift blir
faktisk flersidig, ikke bare det synlige arket.

**Handlinger (nøyaktig som spesifisert — ikke mer):** På en fullført, ikke-behandlet besvarelse:
KUN Godkjenn og Forkast-og-send-på-nytt (behandler må ta ett av de to valgene — se
`TestService.GodkjennRapportAsync`/`ForkastRapportAsync`, gjensidig utelukkende). Etter godkjenning:
Kopier til utklippstavle, Skriv ut, Send kopi til pasienten — presentert som HELT separate
knapperader (ikke samtidig), jf. eksplisitt krav om at c–e KUN skal være mulig med godkjent rapport.
Den tidligere frittstående synlighets-bryteren er fjernet — "Send kopi til pasienten" dekker samme
behov (gjør synlig OG varsler, i motsetning til den stille bryteren, som bare gjorde synlig).

**Forkast, resend:** Ny `TestTildeling.RapportForkastetUtc` — BEVISST IKKE en Status-verdi (Status
forblir `Fullfort`, et historisk faktum: testen BLE besvart; forkastelse er en egen beslutning lagt
oppå, som `RapportGodkjentUtc` — samme mønster, ingen av de eksisterende Status-filtrene andre
steder i appen (Oppgaver, MinSide, Fyll) trengte noen endring). Svarene slettes IKKE — kun
tilgjengelighets-statusen endres, for sporbarhet. `Rapport.cshtml.cs` sin `OnPostForkastAsync`
kaller så det eksisterende `TestTildelingsService.TildelOgVarsleAsync` for å opprette OG varsle om
en helt ny tildeling av samme test til samme pasient — gjenbruker hele
tildelings-/varslingsmotoren fra tildelingsflyten i stedet for å duplisere den.

**Kopier til utklippstavle:** `navigator.clipboard.write` med BÅDE `text/plain` og `text/html` (via
`ClipboardItem`) fra hele `#rapportDokument`-elementet, slik at et rikteksteditor-journalsystem
beholder litt struktur ved innliming, med automatisk fallback til ren `writeText` i eldre nettlesere
og en tydelig manuell instruks (merk + Ctrl+C) hvis Utklippstavle-API-et mangler helt.

**Send kopi til pasienten:** Ny `TestTildelingsService.SendRapportKopiAsync` — samme
kanal-fallback-logikk som den opprinnelige tildelingsvarslingen (`VarsleAsync`, nå refaktorert til å
ta meldingstekst som parameter i stedet for å bygge den selv, slik at begge bruks-tilfellene kan
dele den uten å duplisere kanallogikken), men med en annen meldingstekst/e-post-emne tilpasset "din
rapport er klar" fremfor "du har fått en ny test".

Verifisert med et fullt manuelt scenario over curl (godkjenn → send kopi → varsel med korrekt
lenke og synlighet slått på i databasen; forkast → ny tildeling opprettet og varslet → forsvinner
fra "venter på godkjenning" → dukker opp under "ikke besvart ennå") og 4/4 grønne automatiserte
tester.

## Rapport-visuell — mal fra bruker (report.png), ett-arks WHO-5 (2026-08-31)

Bruker ga et konkret referansebilde (`report.png` i repo-roten, IKKE committet — kun brukt som
visuell mal, se `.gitignore`-vurdering) av en moderne rapportmal: stor to-linjers tittel, et
dekorativt avrundet fargeblokk-hjørne øverst til høyre, seksjoner som fylte "pille"-overskrifter,
og et fargebånd nederst. Gjenskapt i appens oransje aksentfarge (`--accent`/`--accent-dark`) i
stedet for malens grønnfarge, som ny CSS i `site.css` (`.rapport-hjornedekor`, `.rapport-tittel-
stor/-liten`, `.rapport-seksjon-tittel`, `.rapport-sidefot` nå et fylt fargebånd i stedet for en
tynn strek).

**Ett ark for enkle tester:** `RapportModel.SlaaSammenTilEttArk` (`Sider.Count <= 1`) slår forside
(tittel/skåring) og selve svartabellen sammen til ETT `.rapport-ark` når testen kun har én TestSide
— WHO-5 sitt tilfelle. Tester med faktisk flere TestSider beholder fortsatt ett ark per side (samme
struktur som forfatning/utfylling). `TotalAntallArk` regner riktig sidetall i foten uansett hvilken
gren som brukes. Omrisset (`.rapport-ark`) beholder likevel ALLTID full A4-`min-height` uansett
innholdsmengde, jf. eksplisitt krav — et kort WHO-5-ark er fortsatt et fullt, hvitt A4-ark med
skygge, ikke en krympet boks.

**Alle handlingsknapper i samme stil:** Byttet fra blandet `.btn-accent`/`.btn-outline` til
utelukkende `.btn-accent` (oransje, fylt) på Godkjenn/Forkast/Kopier/Skriv ut/Send kopi — presentert
i én horisontal `flex`-rad (`.rapport-handlinger`, wrap kun på smale skjermer).

**Fallgruve truffet under verifisering:** Å legge til nye C#-egenskaper på en PageModel
(`SlaaSammenTilEttArk`/`TotalAntallArk`) mens `dotnet watch run` kjørte i brukerens eget vindu,
utløste en `dotnet`-hot-reload-feil (`ArgumentOutOfRangeException: Token ... is not valid in the
scope of module`) i `RazorPagePropertyActivator` — hot reload klarer ikke alltid nye/endrede
public-egenskaper på en allerede lastet PageModel-type, i motsetning til rene metode-/markup-
endringer. Krever en FULL omstart av `dotnet watch run` (ikke bare en ny fil-lagring) for å komme
seg videre — samme grunnleggende "restart, ikke stol blindt på hot reload ved strukturelle
typeendringer"-lærdom som allerede gjelder for `dotnet build`-fillåsing.

Verifisert med curl mot en helt fersk (ikke-watch) serverinstans: godkjent WHO-5-rapport blir
"side 1 av 2" (skåring+svar slått sammen, historikk som eget ark) for behandler, "side 1 av 1" for
pasienten (som ikke ser historikk), og en forkastet rapport viser korrekt banner uten handlingsrad.
4/4 grønne automatiserte tester uendret.

## Rapport: introduksjon, ekte kopierbar boks, råskår flyttet til slutt (2026-08-31)

Tre oppfølgingsjusteringer etter bruker-tilbakemelding om reell bruk av kopier-til-utklippstavle-
knappen:

**Introduksjon:** Gjenbruker `Test.Beskrivelse` (samme felt som vises til pasienten før utfylling)
i en liten sitatboks-stil seksjon (`.rapport-introduksjon`) på sammendragsarket — ingen nytt
datafelt, bevisst minimal endring siden brukeren ba om "ikke mye, bare en liten introduksjon".

**Ny sidestruktur — råskår til slutt:** `RapportModel.TotalAntallArk` forenklet til `1 + Sider.Count`
— sammendraget (tittel/intro/skåring/utvikling over tid) er NÅ ALLTID ett samlet ark, og selve
svartabellen (råskårene) kommer alltid ETTER, som egne, avsluttende ark (ett per TestSide) — ikke
rett etter skåringen som i forrige versjon. For WHO-5 gir dette nøyaktig "side 2 av 2", som bedt om.
Fjernet `SlaaSammenTilEttArk` (ikke lenger treffende — sammendraget slås alltid sammen nå, det er
ikke lenger betinget av antall TestSider).

**Ekte kopierbar boks:** Den forrige "Kopier til utklippstavle" kopierte rå `innerHTML` fra selve
sidevisningen (`#rapportDokument`) — som er stylet via EKSTERNE CSS-klasser i `site.css`. Limt inn i
et journalsystem (som ikke har den stilarket) forsvant all formatering, akkurat som rapportert. Løst
med en helt separat, SKJULT (`hidden`) mal (`#rapportKopierMal`) bygget med KUN inline
`style="..."`-attributter og harde fargeverdier (ikke `var(--x)`, som ikke betyr noe utenfor vår
egen stilark) — en synlig, oransje-kantet boks som skiller seg fra vanlig journaltekst ved
innliming, uansett mottakerens redigeringsverktøy. Samme rekkefølge som den nye sidestrukturen
(sammendrag/intro/skåring/utvikling → svar til slutt), uavhengig av hvilken "side" brukeren står på
i sideflipperen når de trykker Kopier (viktig — siden elementet er `hidden`, ville `.innerText` gitt
tom streng; `.textContent` brukes for tekst-fallbacken i stedet, se `wwwroot/js/rapport.js`).

Verifisert på fersk serverinstans: introduksjon vises, rekkefølge Skåring→Utvikling over tid→Svar
bekreftet i generert HTML, ingen `var(...)`-referanser lekket inn i `#rapportKopierMal`s
inline-stiler. 4/4 grønne automatiserte tester.

## Rapport: seksjonspiller til venstre kant + egen WHO-5-introduksjonstekst (2026-08-31)

To små justeringer etter at bruker sammenlignet med malen (`report.png`) på nytt:

**Seksjonspillene bleeder til arkkanten:** `.rapport-seksjon-tittel` hadde `margin: 0 0 1rem` (vanlig
innrykk, samme som brødteksten) — malen viser dem flush mot SIDENS venstre kant, ikke innrykket.
Løst med negativ venstremargin lik `.rapport-ark`s venstre padding (`-2cm`, `-1.5rem` på mobil,
speilet i `@@media (max-width: 900px)`), kompensert med tilsvarende `padding-left` slik at selve
teksten fortsatt har luft. `border-radius` endret til kun høyre side (`0 999px 999px 0`) siden
pillen nå faktisk treffer kanten — en avrundet venstrekant ville sett feil ut der.

**Egen `Test.RapportIntroduksjon`:** Rapportens "introduksjon" brukte inntil nå
`Test.Beskrivelse` — som EGENTLIG er pasientvendte utfyllingsinstruksjoner ("sett en sirkel
rundt..."), ikke en klinisk beskrivelse av hva testen måler. Bruker ga riktig WHO-5-tekst
(oversatt fra engelsk WHO-materiell). Løst med et helt nytt, eget felt på `Test` fremfor å fortsette
å overbelaste `Beskrivelse` — riktigere datamodell, og lar de to tekstene utvikle seg uavhengig
senere. Satt via en ny, minimal `TestService.SettRapportIntroduksjonAsync` (IKKE et nytt parameter
på `OpprettTestAsync`/`OppdaterTestAsync` — unngår enhver risiko for den kjente
positional-argument-fallgruven, og feltet trengs uansett ikke i noe kallsted utenfor
`Who5TestSeeder` ennå). **Bevisst utsatt:** ingen admin-UI for dette feltet på
admin-forfattede tester ennå — kun tilgjengelig via kode-seedere (som WHO-5) foreløpig.

Verifisert på fersk serverinstans: ny tekst vises både i selve rapportvisningen og i
kopier-til-utklippstavle-malen. 4/4 grønne automatiserte tester.

## Pasientliste-søk, tildelt/besvart-kolonner, PNR i liste+rapport, ny Admin/Pasienter (2026-08-31)

`TestService.HentTildelingTellingerAsync` — én ny, gjenbrukbar metode som gir antall tildelt og
antall besvart (Fullfort) per pasient i én spørring (gruppert i minnet, ikke N+1), brukt av begge
listene under.

**Klientside tabellfilter (`wwwroot/js/tabellfilter.js`):** Generisk — et `<input
data-tabellfilter="#tabellId">` skjuler/viser `<tr data-sok="...">` live mens man skriver, ingen
server-tur. `data-sok` bygges server-side per rad av ALLE relevante felt (navn, gruppe, mobil,
e-post, personnummer, status — og behandlernavn for admin-visningen) slått sammen og små bokstaver.
Gjenbrukt uendret på begge listene under — ett skript, to bruksområder.

**Behandlerportal/Pasienter/Index:** Fikk søkefeltet + tre nye kolonner (Personnummer, Tildelt,
Besvart) foran den eksisterende Status/handlings-kolonnen.

**Ny Admin/Pasienter/Index:** Fantes ikke fra før — admin hadde ingen enkel "se alle pasienter"-side,
kun det smalere pasient-VALGET i tildelingsflyten (Admin/Tildel/Pasienter). Rent lesetilgang (ingen
Rediger/Arkiver — pasient-CRUD hører fortsatt til behandler), med samme Behandler-kolonne-mønster
som Admin/Tildel/Pasienter allerede har, pluss søk + de samme tellekolonnene. Lagt til i
`AuthorizeAreaFolder`-listen i `Program.cs` og i navigasjonen.

**PNR i rapporten:** `Behandlerportal/Pasienter/Rapport.cshtml` viser nå personnummer rett under
pasientnavnet i sammendrags-arket (`.rapport-pnr`, dempet/mindre skrift) OG i den skjulte
kopier-til-utklippstavle-malen (samme inline-stil-prinsipp som resten av den malen). KUN
behandlerens rapportvisning — pasientens egen rapportside trenger ikke vise dem sitt eget
personnummer tilbake.

Verifisert på fersk serverinstans: tellinger stemte mot faktisk databasetilstand (4 tildelt/3
besvart for en testpasient med én forkastet+re-sendt besvarelse), Admin/Pasienter viser alle
pasienter på tvers av behandlere med korrekt behandlernavn, PNR vises begge steder i rapporten.
4/4 grønne automatiserte tester (ingen skjemaendring i denne runden — ingen ny migrasjon nødvendig).

## "Resultat"-seksjon med WHO-5-indikatorer + kopier resultat separat (2026-08-31)

**Generisk `TestSkaaringIndikator`:** `TestSkaaring` fikk et valgfritt `Indikatorer`-felt (default
null — bakoverkompatibelt, ingen eksisterende kallsted trengte endring) av navngitte, kategoriske
konklusjoner (`Navn`, `Verdi`, `Positiv`) UTOVER selve tallskåren — bevisst generisk (ikke
WHO-5-spesifikt på `TestSkaaring`-nivå) slik at fremtidige skåringsberegnere for andre tester kan
levere sine egne uten endring i selve rapport-rammeverket.

**WHO-5s to indikatorer:** "Velvære"/"Ikke velvære" og "Indikerer depresjon"/"Indikerer ikke
depresjon" — BEGGE avledet av den samme, allerede siterte grenseverdien (råskår 13, jf. "VEILEDNING
I BRUK AV WHO 5-WBQ", Bakke 2004) som lå i koden fra før, ikke en ny/usikker terskel. Siden
WHO-5s prosentskår kun kan ta verdiene 0/4/8/…/100 (råskår×4), finnes det ingen skår som ville gitt
de to indikatorene ulikt utfall — de er likevel bevisst to separate, navngitte verdier (ikke duplisert
tekst) fordi de svarer på to ulike kliniske spørsmål. `Fortolkning`-teksten justert til eksplisitt å
si "WHO-5-veiledningen anbefaler [ikke] å gå videre med nærmere undersøkelse" i BEGGE retninger (før
kun eksplisitt i den ene retningen).

**Visning:** Indikatorene rendres som fremhevede "badges" — fylt bakgrunn, farge, understrek
(`.rapport-indikator-positiv`/`-negativ`, grønn/rød) — under "Resultat" (omdøpt fra "Skåring", jf.
bruker-ønske) i BEGGE rapportvisningene (behandler og pasient, siden `Fortolkning`-teksten allerede
ble delt med pasienten før dette).

**To kopier-knapper:** "Kopier alt til utklippstavlen" (eksisterende, kun omdøpt) og en ny "Kopier
resultat til utklippstavlen" som KUN henter en egen, adresserbar underboks (`#rapportKopierResultat`)
inni den skjulte kopimalen — ikke hele malen. Samme inline-stil-prinsipp som resten av kopimalen
(harde fargeverdier, ikke `var(--x)`), egen synlig ramme rundt akkurat denne boksen slik at den
skiller seg ut når den limes inn separat i et journalsystem. `wwwroot/js/rapport.js` sin
kopier-logikk er refaktorert til én delt `kopierTilUtklippstavle(elementId)`-funksjon kalt fra begge
knappene, i stedet for duplisert kode.

Verifisert med to fullførte WHO-5-besvarelser på samme server (høy skår → grønne "Velvære"/"Indikerer
ikke depresjon"; råskår 5/25 → røde "Ikke velvære"/"Indikerer depresjon" + eksplisitt
anbefalings-tekst). 4/4 grønne automatiserte tester, ingen migrasjon nødvendig.

## Navigasjon: egen "funksjonsnav"-rad med store oransje knapper (2026-08-31)

Toppnavigasjonen (`_Layout.cshtml`) blandet kommersielle lenker (Hjem/Tjenester/Om/Kontakt) med
rollespesifikke app-funksjoner (Pasienter/Tildel tester/Oppgaver/…) i samme rad, som vanlige
tekstlenker. Delt i to:

- **`.site-nav`** (uendret) — kun de kommersielle/markedsførings-lenkene.
- **Ny `.funksjons-nav`** — egen, full-bredde fargelagt rad RETT UNDER, med rollens
  hovedfunksjoner som store, oransje "app-knapper" (`.funksjonsknapp`, ikon + tekst, samme
  full-bleed-`width:100vw`-triks som `.hero`/`.features` allerede bruker andre steder i denne
  filen). "Min side" står alltid FØRST for behandler/pasient (admin har ingen egen Min side ennå,
  se "Bevisst utsatt" under). 8 nye ikoner lagt til i `_Ikon.cshtml` (hus, pasienter, behandlere,
  administratorer, tester, tildel, inviter, oppgaver) — enkle Feather-ish strek-SVG-er i samme stil
  som de eksisterende.
- **"Innlogget som"/"Logg ut"** flyttet inn i en egen `.bruker-omrade`-boks (avrundet, lys
  bakgrunnsfarge) i toppraden — samme idé som footerens fargede bånd nederst på siden, bare mer
  kompakt siden den sitter inni headeren. Fortsatt i toppraden, ikke flyttet ned til funksjonsraden.
- "Bytt modus" (kun dev) ble IKKE en stor knapp — det er et utviklerverktøy, ikke en hovedfunksjon
  i appen — beholdt som en liten tekstlenke i toppraden ved siden av bruker-boksen.

**Bevisst utsatt:** Admin har ingen "Min side" (siden fantes ikke fra før, og ble ikke bedt om her)
— admin-knapperaden starter derfor rett på Administratorer.

Verifisert på fersk serverinstans for alle tre roller (behandler: Min side først, deretter
Pasienter/Tildel tester/Inviter kollega/Oppgaver; admin: eget sett; ekte pasient-konto: kun Min
side + Oppgaver — Utvikler-rollen ser fortsatt alle tre sett samtidig, som før, nå bare som
knapper). 4/4 grønne automatiserte tester, ingen migrasjon nødvendig.

## Kjente feilsøkingspunkter fra oppsett (til referanse)

- **Docker Desktop "Virtualization support not detected":** Løst ved å aktivere Windows-funksjonene `VirtualMachinePlatform` og `Microsoft-Windows-Subsystem-Linux` via PowerShell (admin) + omstart, selv om Intel VMX/VT-x allerede var aktivert i BIOS.
- **`dotnet ef` "Unable to connect to any of the specified MySQL hosts"**: Oppstår hvis migrasjons-kommandoene kjøres før `docker compose up -d` har startet MySQL-containeren — `ServerVersion.AutoDetect(...)` i `Program.cs` krever en faktisk databasetilkobling selv for `migrations add`.
- **Manglende `launchSettings.json`**: Uten den defaulter appen til Production-miljø (ingen tilkoblingsstreng der) i stedet for Development. Nå lagt til i repoet.
- **"Table ... doesn't exist"**: Skjer hvis migrasjonene ikke er kjørt etter at Docker/MySQL-containeren startet. Kjør `dotnet ef database update` på nytt.
- **`dotnet ef database update` → "Build failed" uten detaljer**: Skjer hvis `dotnet watch run` kjører i et annet vindu og låser build-output-filene (vanlig på Windows). Stopp `dotnet watch run` midlertidig (Ctrl+C), kjør migrasjonen, start appen igjen.

### Ekte e-postutsending via Azure Communication Services (2026-09-03)

Første ekte (ikke-mock) leverandørintegrasjon: `IEmailSender` har nå en reell implementasjon,
`AzureEmailSender` (`TestBase.Shared/Providers/AzureEmailSender.cs`), som sender via **Azure
Communication Services (ACS) Email** — valgt fordi det er Azure-native (samme abonnement som
resten av infrastrukturen, ingen egen leverandøravtale å fremforhandle, i motsetning til
SendGrid/Mailgun-sporet som ellers ville vært naturlig). Ny infrastruktur i
`infra/resources.bicep`: `Microsoft.Communication/emailServices` + et **Azure-administrert domene**
(`domainManagement: 'AzureManaged'`, ressursnavn `AzureManagedDomain`) — ingen DNS-verifisering
nødvendig, Azure genererer selv et `*.azurecomm.net`-domene og DKIM/DMARC/SPF er automatisk
"Verified" fra dag én — pluss `Microsoft.Communication/communicationServices` (linket til
domenet) og en `senderUsernames`-ressurs (`noreply`). Databeliggenhet satt til **Norway** for disse
to ressursene (gyldig `dataLocation`-verdi for ACS, i motsetning til MySQL Flexible Server-en som
måtte til Sweden Central pga. kapasitet — se "Sky-deploy til Azure (azd)") — det første stedet i
denne infrastrukturen som faktisk lander i den opprinnelig planlagte regionen.

ACS-tilkoblingsstrengen lagres i Key Vault (samme mønster som MySQL-tilkoblingsstrengen) og
eksponeres til App Service som `Acs__ConnectionString`; avsenderadressen
(`noreply@<generert>.azurecomm.net`, lest ut fra domenets faktiske `mailFromSenderDomain`-egenskap
etter provisjonering) som `Email__SenderAddress`. `Program.cs` velger `AzureEmailSender` når
`Acs:ConnectionString` er satt, ellers `MockEmailSender` (lokal utvikling har den aldri satt, så
lokal oppførsel er uendret). Verifisert 2026-09-03: sendte en ekte behandler-invitasjon fra
test-appen til brukerens egen e-postadresse via `Areas/Admin/Pages/Behandlere/Inviter` — SDK-kallet
(`EmailClient.SendAsync` med `WaitUntil.Completed`) fullførte uten feil. Oppdaterte samtidig UI-teksten
i de tre "invitasjon sendt"-sidene (Admin- og Behandlerportal-Inviter, Behandlerportal Pasienter/Ny)
som fortsatt påsto "mock — ingen ekte SMS/e-post" — SMS er fortsatt mock, e-post er det ikke lenger
her. To mindre steder (`Pages/Inviter/Verifiser.cshtml`, Behandlerportal `Innstillinger.cshtml.cs`
sin påminnelsestekst) ble bevisst IKKE oppdatert — sekundære flyter, se "Åpne punkter" hvis de skal rettes.

**Nesten-hendelse under samme arbeid — appSettings på `Microsoft.Web/sites` er en FULL erstatning,
ikke en sammenslåing, ved hver `azd provision`.** `StagingGate__AccessKey` (satt manuelt via
`az webapp config appsettings set` i forrige økt, aldri lagt inn i `infra/resources.bicep`) forsvant
sporløst da denne økten kjørte `azd provision` for å legge til ACS-ressursene — App Service-ens
`siteConfig.appSettings`-liste i Bicep ble deployet på nytt med KUN de fem opprinnelige innstillingene,
og Azure erstattet HELE appSettings-samlingen med akkurat den listen, uten å bevare
`StagingGate__AccessKey` som var satt utenfor malen. Test-appen sto dermed helt åpen for internett
igjen i noen minutter (oppdaget og lukket samme økt, ved rutinemessig statussjekk før neste
funksjonstest — ingen kjent ekstern tilgang i vinduet). Rettet permanent: `stagingGateAccessKey` er
nå en egen `@secure()`-parameter i `main.bicep`/`resources.bicep`, verdien kommer fra
azd-miljøvariabelen `STAGING_GATE_ACCESS_KEY` (satt via `azd env set`, lagret kun i den allerede
gitignorede `.azure/testbase-test/.env` — ALDRI en literal verdi i selve Bicep-filen, samme prinsipp
som MySQL-passordet). Satt til samme verdi som før, så ingen enheter trengte å taste inn en ny nøkkel.
**Generell lærdom: ENHVER App Service-innstilling som skal overleve fremtidige `azd provision`-kall
MÅ inn i `infra/resources.bicep` sin `appSettings`-liste — en "sett den bare via CLI for nå"-løsning
blir stille borte ved neste provisjonering, ikke bare "ikke reprodusert et annet sted" som tidligere
antatt.** Dette gjelder trolig `Acs__ConnectionString`/`Email__SenderAddress` også — de ble lagt inn i
Bicep fra START i denne økten (lærdommen ble anvendt med en gang for de nye innstillingene), så de er
ikke utsatt for samme risiko.

### Eget domene for test-miljøet: psytest.no (2026-09-03)

Bruker kjøpte `psytest.no` hos domene.no (bundlet med et "web 5"-hostingpakke — cPanel bak
kulissene, DNS redigeres via cPanel sin Zone Editor, IKKE domene.no sitt eget "Subdomener"-panel
som kun gjelder deres egen webhosting). Satt opp til å peke på test-App Service-en:

- **DNS** (cPanel Zone Editor, `psytest.no` sin sone): CNAME `www.psytest.no` →
  `app-testbase-tk46vyxboocho.azurewebsites.net` (redigerte en eksisterende selvrefererende
  CNAME, IKKE en ny post — DNS tillater kun én CNAME per navn), pluss TXT `asuid.www.psytest.no`
  → App Service sin `customDomainVerificationId` (Azure sitt obligatoriske eierskapsbevis for
  alle custom domains, hindrer at noen andre kan kapre et forlatt CNAME-mål).
- **Apex-domenet** (`psytest.no` uten www) bruker domene.no sin egen HTTP-omdirigeringstjeneste
  ("Omdiriger domene", et eget hostingprodukt-nivå-feature, IKKE en DNS-post — A-recorden for
  `psytest.no` peker fortsatt på domene.no sin egen hosting-IP `185.126.36.19` og har ikke
  endret seg) — satt til 301 permanent redirect til `https://www.psytest.no`. Ble ved en feil
  først satt til den rå Azure-URL-en (fra tidlig testing før DNS var på plass), rettet i etterkant.
- **Azure-siden**: `az webapp config hostname add` (custom domain binding, `hostNameType: Verified`)
  + `az webapp config ssl create`/`ssl bind` (gratis App Service Managed Certificate, SNI, utsteder
  GeoTrust TLS RSA CA G1, fornyes automatisk før 2027-03-03). Verifisert med ekte DNS-oppslag
  (også mot 8.8.8.8) og faktisk HTTPS-kall: `https://www.psytest.no` → 401 fra `StagingGate`
  (helt korrekt og forventet — samme beskyttelse som `azurewebsites.net`-adressen), `psytest.no`
  (http og https) → 301 til `https://www.psytest.no`.
- **Ikke gjort ennå, bevisst utsatt**: hostnavn-bindingen og sertifikatet er satt opp via CLI, ikke
  lagt inn i `infra/resources.bicep` — `Microsoft.Web/sites/hostNameBindings` og
  `Microsoft.Web/certificates` er egne ressurstyper (IKKE en del av `Microsoft.Web/sites` sin
  `appSettings`-liste), så dette er ikke utsatt for samme "full erstatning ved neste provision"-
  problem som rammet `StagingGate__AccessKey` (se "Ekte e-postutsending via Azure Communication
  Services") — men det er heller ikke reprodusert automatisk ved en fersk `azd provision` et annet
  sted, siden DNS-eierskap (TXT-verifisering) må finnes FØR Azure godtar bindingen. Se "Åpne
  punkter" for om/når dette bør kodifiseres.
- ACS-avsenderadressen (`noreply@<generert>.azurecomm.net`) er IKKE endret til å bruke
  `psytest.no` ennå — det er en egen, separat oppgave (krever egne DNS-verifiseringsposter for
  ACS sitt e-postdomene, ikke bare for selve nettstedet).

### Rebranding til "PsyTest" (2026-09-04)

All synlig branding/tekst byttet fra "TestBase" til "PsyTest" for å matche det innkjøpte domenet
`psytest.no` — sidetitler, header-/footer-logo (`Psy<span>Test</span>`, samme farge-stil/CSS som
før, kun teksten endret), forsideteksten, personvernsiden, rapport-vannmerket, og ALLE
e-post/SMS-meldingstekster appen sender (invitasjonar, bekreftelseskoder, 2FA-koder,
rapportvarsler, påminnelser). Bevisst IKKE endret: C#-navnerom (`TestBase.Web`/`TestBase.Shared`),
Azure-ressursnavn (`rg-testbase-test`, `app-testbase-tk46vyxboocho` osv.), databasenavn, og alle
DataProtection-formålsstrenger (`"TestBase.Personnummer.v1"`, `"TestBase.BetroddEnhet.v1"`,
`"TestBase.StagingGate.v1"`, `"TestBase.Captcha.v1"`) samt `StagingGate`-cookien sitt navn
(`.TestBase.StagingGate`) — disse er interne tekniske identifikatorer, ikke synlig branding, og å
endre dem ville ha gjort eksisterende krypterte personnummer uleselige og ugyldiggjort aktive
StagingGate-/BetroddEnhet-cookies. Samme prinsipp som at et produkts interne kodenavn ikke trenger
matche det offentlige produktnavnet.

Verifisert grundig før commit: bygg OK, alle 4 integrasjonstester (`tests/TestBase.IntegrationTests`)
grønne (ingen av dem asserter på den gamle "TestBase"-teksten, så omdøpingen påvirket dem ikke),
lokalt miljø startet på nytt (`dotnet run` — MERK: `--no-launch-profile` MÅ ikke brukes, se
"Kjente fallgruver" i CLAUDE.md) og verifisert manuelt at forsiden viser "PsyTest" og at en ekte
behandler-invitasjon (mock e-post lokalt) logger riktig "Invitasjon til PsyTest"-tekst. Deployet
til Azure (`azd deploy`) og verifisert der også: `StagingGate` fortsatt aktiv (401 uten nøkkel),
`/health` OK, forsiden viser "PsyTest".

**Lokal e-post fortsatt mock inntil videre** — brukeren fikk en engangskommando for å hente
`Acs:ConnectionString` fra Key Vault og lagre den i `dotnet user-secrets` (kjørt i brukerens EGEN
terminal, ikke via Claude Code — å lese en Key Vault-hemmelighet direkte ble riktig nok blokkert av
sikkerhetsklassifisereren, se `docs/beslutningslogg.md` sin generelle sikkerhetsprofil). Når den er
satt, plukker `Program.cs` automatisk opp `AzureEmailSender` lokalt også, uten kodeendring —
samme valgmekanisme som allerede styrer dette i Azure.

### SMS-integrasjon: valgt Azure Communication Services (2026-09-04)

Vurderte tre spor for ekte SMS med navngitt avsender ("PsyTest" i stedet for et telefonnummer):
Azure Communication Services (samme ressurs/faktura som e-post), Link Mobility (norsk aktør,
direkte operatørforbindelser, ~380 EUR + mva engangsavgift for avsendernavn, men mer
salgsdrevet oppstart), og globale utviklervennlige plattformer (Twilio ~$0.065–0.07/SMS til
Norge — dyrere enn nødvendig; 46elks/Messente billigere men mindre dokumentert for Norge
spesifikt). Valgte Azure Communication Services — samme mønster som e-post, minst ny
infrastruktur å forholde seg til.

**Viktig, uavhengig av leverandørvalg:** Norge krever **forhåndsregistrert** alfanumerisk
avsender-ID (i motsetning til Sverige/Danmark som tillater dynamisk/øyeblikkelig avsender-ID) —
dette er et krav fra de norske mobiloperatørene, ikke en Azure-spesifikk begrensning. Forventet
behandlingstid **6–8 uker** ifølge Microsofts egen dokumentasjon. Avsender-ID er kun
énveis-utgående (kan ikke motta svar/STOP-meldinger) — uproblematisk her, appen har ingen
innkommende SMS-flyt noe sted.

**Kodesiden er klar:** `AzureSmsSender` (`TestBase.Shared/Providers/AzureSmsSender.cs`), samme
mønster som `AzureEmailSender` — bruker SAMME `Acs:ConnectionString` som e-post (SMS er en
frittstående kapabilitet på samme Communication Services-ressurs, ikke en egen underressurs slik
e-postdomenet er). `Program.cs` velger `AzureSmsSender` kun når BÅDE `Acs:ConnectionString` OG
`Sms:SenderId` er satt, ellers `MockSmsSender` som før — lokalt miljø upåvirket. Ingen
ARM/Bicep-ressurstype finnes for selve avsender-ID-søknaden (bekreftet via `az provider show` —
kun `EmailServices/Domains/SenderUsernames` finnes, intet SMS-ekvivalent), så dette kan IKKE
automatiseres via `infra/resources.bicep` slik e-postdomenet ble.

**OPPDATERT samme dag, se neste seksjon: dette Azure-sporet ble forlatt** — den planlagte
"Submit an application"-knappen viste seg ikke å eksistere i praksis for Norge i portalen.
`AzureSmsSender` er fjernet fra kodebasen igjen.

### SMS-integrasjon: byttet fra Azure til Vonage (2026-09-04, samme dag som forrige notat)

Forrige notat konkluderte med Azure Communication Services for SMS — det viste seg feil i praksis.
Brukeren fant selv, ved å faktisk sjekke Azure Portal, at kun USA/Canada/Puerto Rico har en
fungerende selvbetjent flyt for alfanumerisk avsender-ID; øvrige "Preregistered"-land (Norge
inkludert) mangler i praksis den dokumenterte knappen/skjemaet i portalen (bekreftet av flere
uavhengige rapporter i Microsofts egne Q&A-fora — et kjent, udokumentert gap mellom
funksjonstabellen og faktisk portalstøtte), og ville i beste fall krevd en supportsak med usikker
utfall/tidsbruk, ikke de dokumenterte "6–8 ukene".

**Testet empirisk i stedet:** Opprettet en Vonage-konto, hentet API-nøkkel/-hemmelighet fra
dashbordet, og sendte en ekte SMS til et norsk nummer via Vonage sitt Messages API
(`https://api.nexmo.com/v1/messages`) med `"from": "PsyTest"` — **fungerte umiddelbart, ingen
forhåndsregistrering, meldingen viste riktig "PsyTest" som avsender på mottakers telefon.** Dette
stemmer med (usikre, delvis 403-blokkerte) søk som antydet at Vonage ikke krever forhåndsregistrering
for Norge i det hele tatt, i motsetning til Azures offisielle klassifisering — den faktiske,
utprøvde APIen er den eneste kilden vi til slutt stolte på.

**Byttet fullstendig fra Azure- til Vonage-sporet:** Fjernet `AzureSmsSender.cs` og
`Azure.Communication.Sms`-pakken (ubrukt/blokkert av Norge-begrensningen uansett — ingen vits i å
beholde to alternative SMS-implementasjoner når den ene reelt sett ikke fungerer for dette
markedet). Ny `VonageSmsSender` (`TestBase.Shared/Providers/VonageSmsSender.cs`) bruker en enkel
`HttpClient` + Basic Auth mot Vonage sitt Messages API — bevisst IKKE Vonages offisielle .NET-SDK,
siden den rå HTTP-forespørselen allerede var empirisk verifisert å fungere og et SDK ville lagt til
en ny, uverifisert abstraksjon oppå noe som allerede var bekreftet riktig. Inkluderer normalisering
av `MobilNr` (fritekstfelt uten formatvalidering i dag) til Vonages forventede format (kun siffer,
norsk landkode, ingen "+").

`Program.cs` velger `VonageSmsSender` kun når `Vonage:ApiKey`/`Vonage:ApiSecret`/`Sms:SenderId`
alle er satt, ellers `MockSmsSender` som før. Alle tre er Bicep-parametere fra start (azd-
miljøvariablene `VONAGE_API_KEY`/`VONAGE_API_SECRET`/`SMS_SENDER_ID`, aldri literale verdier i
Bicep) — samme "aldri kun CLI"-prinsipp som `StagingGate__AccessKey` måtte læres på den harde
måten. API-nøkkel og -hemmelighet lagres som Key Vault-hemmeligheter (`VonageApiKey`/
`VonageApiSecret`), samme mønster som `AcsConnectionString`.

Verifisert ende-til-ende 2026-09-04: `azd provision` + `azd deploy`, inviterte en behandler via
mobilnummer (ikke e-post) fra `Areas/Admin/Pages/Behandlere/Inviter` på `www.psytest.no` — ekte SMS
mottatt med riktig "PsyTest"-avsender. Lokalt miljø verifisert oppstartsklart (ingen DI-/
konfigurasjonsfeil), men ingen ekte SMS sendt derfra i denne økten (unødvendig å bruke enda en
sending når selve API-kontrakten allerede er bekreftet på samme kodesti).

Rettet i samme slag: `emailSenderUsername.displayName` i `infra/resources.bicep` sa fortsatt
"TestBase (testmiljø)" — overlevd fra rebrandingen 2026-09-04 tidligere samme dag fordi den
gjennomgangen kun søkte i `.cshtml`/`.cs`-filer, ikke Bicep. Rettet til "PsyTest (testmiljø)".

**Lærdom:** Azures egen dokumentasjon av landstøtte for en funksjon kan ikke tas for gitt å matche
hva som faktisk er tilgjengelig i portalen/APIen — når noe virker "off" (som brukerens observasjon
om at kun tre land vises), er en rask, billig empirisk test mot en konkurrents faktiske API en mer
pålitelig kilde enn å fortsette å lete i dokumentasjon som kan være foreldet eller aspirasjonell.

### BankID-testintegrasjon via Idura (2026-09-05)

Etter at ekte SMS (Vonage) og e-post (Azure Communication Services) var på plass, var neste
naturlige spørsmål om noe tilsvarende kunne gjøres for BankID uten å vente på en reell
produksjonsavtale (som fortsatt ikke finnes, se "Leverandørstatus" øverst i dette dokumentet).
**Idura** (tidligere Criipto, nylig kjøpt av BankID BankAxept) tilbyr en gratis test-tenant
(`psytest.test.idura.broker`) med et fullverdig BankID OIDC-testmiljø — ingen registrerings-
ventetid, i motsetning til Azure Communication Services SMS som strandet på nettopp dette for
Norge (se "SMS-integrasjon: byttet fra Azure til Vonage").

**Viktig avgrensning:** dette er BEVISST holdt som en diagnostisk sideintegrasjon, IKKE en
erstatning for `IBankIdProvider`/`MockBankIdProvider` i den faktiske innloggingsflyten
(`Pages/Konto/LoggInn`, Behandlerportal/Pasientportal). Grunnen er den samme som gjelder for
BankID/Vipps generelt (se "Leverandørstatus"): en gratis Idura-testkonto er ikke en signert
BankID-produksjonsavtale, og selve identitetsverifiseringen (hvilket personnummer som faktisk
skal logges inn som hva i domenemodellen) er en betydelig større beslutning enn det som var
til vurdering her. Integrasjonen finnes derfor kun som et eget, isolert testverktøy:
`/DevDemo` → "Test ekte BankID (Idura)" → `/BankIdTest/Start` → ekte BankID-innlogging → 
`/BankIdTest/Resultat` (viser ALLE claims BankID faktisk returnerer, rått, uten å gjette navn
på personnummer-claimet på forhånd).

**Teknisk:** `Microsoft.AspNetCore.Authentication.OpenIdConnect` (NuGet — IKKE inkludert i
`Microsoft.AspNetCore.App`-shared-framework-referansen slik Cookie-autentisering er, må legges
til eksplisitt) registrert som en named scheme `"BankIdTest"` i `Program.cs`, kun når
`BankId:Idura:Authority`/`ClientId`/`ClientSecret` faktisk er satt (samme "fravær av
konfigurasjon = av"-mønster som Vonage/ACS). `response_mode=form_post` + `ResponseType=code`
(Authorization Code med PKCE). `acr_values` styrer hvilket BankID-sikkerhetsnivå som kreves:
`urn:grn:authn:no:bankid:substantial` feilet med "You must activate the BankID app" — krever en
reell, aktivert BankID-app-installasjon, umulig i et rent testoppsett. `urn:grn:authn:no:bankid:high`
derimot matcher Iduras dokumenterte testbrukerflyt (engangskode `otp` + passord `qwer1234`, ingen
app nødvendig) og er derfor valgt som standardverdi. `OnTokenValidated` fanger opp responsen selv
(`ctx.HandleResponse()`) i stedet for å la standard-cookie-signeringen kjøre, lagrer alle claims
som tekst i `TempData`, og redirecter til `/BankIdTest/Resultat` — bevisst valgt fremfor å skrive
en ekte auth-cookie, siden dette ikke skal kunne forveksles med en reell innlogging noe sted i
systemet. Test-personnummer/synteiske identiteter opprettes via BankID sitt eget
`ra-preprod.bankidnorge.no`-testverktøy (Test Number Generator + End User-søk), ikke noe vi bygde
selv.

To reelle feil ble avdekket og rettet underveis i verifiseringen, begge verdt å huske for
fremtidige OIDC-baserte integrasjoner i dette prosjektet:

1. **`DevDemo` krasjet (500) etter at ekte SMS/e-post var konfigurert i Azure.**
   `DevDemoModel.OnGetAsync` kalte ubetinget `_sms.SendAsync("+4700000000", ...)` og
   `_email.SendAsync("dev@example.test", ...)` ved hver sidevisning — helt ufarlig med mock, men
   ekte Vonage/ACS avviser åpenbart oppdiktede mottakeradresser (`Azure.RequestFailedException:
   EmailDroppedAllRecipientsSuppressed`). Rettet ved å pakke begge kallene i try/catch og vise
   feilmeldingen i UI i stedet for å la siden krasje — `/DevDemo` er en diagnostisk side, den skal
   tåle at en avhengighet feiler uten å ta med seg resten av siden.
2. **`StagingGate` (se samme seksjon lenger opp) blokkerte selve BankID-callbacken med 401** etter
   en ellers vellykket BankID-innlogging. Årsak: `response_mode=form_post` gjør at Idura POSTer
   cross-site tilbake til vår `CallbackPath` (`/signin-bankid-test`) — StagingGate-cookien er
   `SameSite=Lax`, og nettlesere sender IKKE en Lax-cookie på en cross-site POST. Rettet med et
   snevert, hardkodet unntak for nøyaktig denne ene stien i `StagingGate.cs` — trygt fordi stien
   er fast (ingen wildcard) og selve OIDC-håndteringen uansett validerer state/nonce/PKCE, så en
   vilkårlig POST mot denne stien uten en ekte Idura-autorisasjonskode oppnår ingenting. Generell
   lærdom: enhver fremtidig funksjon som mottar en cross-site `form_post`-callback (flere OIDC-
   identity-providere følger samme mønster) vil støte på nøyaktig denne SameSite-kollisjonen mot
   `StagingGate` og trenger samme type unntak.

Verifisert ende-til-ende 2026-09-05 med ekte nettleserautomatisering (Playwright MCP, se
"Playwright MCP for nettleserautomatisering" under) direkte mot `www.psytest.no` (ikke bare det
frittstående `oidcdebugger.com`-verktøyet som ble brukt til å diagnostisere `acr_values`-valget
først): `/BankIdTest/Start` → Idura → BankID-testinnlogging (personnummer, engangskode `otp`,
passord `qwer1234`) → `/signin-bankid-test`-callback → `/BankIdTest/Resultat` viser reelle claims,
inkl. `socialno`, `name`, `authenticationtype: urn:grn:authn:no:bankid:high`.

Infrastruktur: samme mønster som Vonage/ACS — tre azd-miljøvariabler
(`BANKID_IDURA_AUTHORITY`/`BANKID_IDURA_CLIENT_ID`/`BANKID_IDURA_CLIENT_SECRET`) →
`infra/main.parameters.json` → `infra/main.bicep`/`infra/resources.bicep`, client secret som
`@secure()`-parameter lagret i Key Vault (`BankIdIduraClientSecret`), ALDRI literal i Bicep.

**Lærdom (driftsmessig, ikke kode):** et `azd deploy web`-kjøring rapporterte `SUCCESS` men endret
faktisk aldri kjørende kode — loggen inneholdt en lett-å-overse advarsel
(`"Deployment completed, but azd observed no App Service deployment status change for 5m0s"`)
som var eneste signal om at noe var galt (observert som et 404 på en helt ny endepunkt-sti rett
etter en "vellykket" deploy). Løsningen var ganske enkelt å kjøre `azd deploy web` på nytt — men
lærdommen er å faktisk lese hele deploy-loggen for advarsler, ikke bare stole på
`SUCCESS`-linjen, når noe nylig deployet ikke oppfører seg som forventet.

### Playwright MCP for nettleserautomatisering (2026-09-05)

Lagt til som en MCP-server (`claude mcp add -s user playwright -- npx @playwright/mcp@latest`)
etter gjentatte økter der skjermbilde-basert veiledning av bruker gjennom eksterne
dashbord (domene.no, Idura, BankID RA-verktøy) var tregt og feilutsatt sammenlignet med å kunne
navigere/klikke/lese selv. Krevde Node.js installert (`winget install --id OpenJS.NodeJS.LTS -e`)
— `npx.cmd` trenger `node` på PATH, og en allerede åpen terminal-økt fanger ikke opp en PATH-
endring gjort av en installer som kjørte i mellomtiden; løsningen var å starte en helt ny
terminal og gjenoppta økten (`claude --continue`), ikke noe som kan fikses i den samme prosessen.
Brukt til å kjøre selve sluttverifiseringen av BankID-integrasjonen over, direkte mot den
deployede appen.

### Seed av brukerens egen admin-konto (2026-09-05)

For å kunne teste hele admin/behandler/pasient-flyten selv (ikke bare med fiktive
test-personnumre via `PersonnummerOverride`, se "Tildelingsflyt for tester + BankID
personnummer-overstyring") ble brukerens egen, ekte administrator-konto (navn +
personnummer) lagt til — men BEVISST aldri som en literal i kildekode. Dette repoet er
offentlig på GitHub, og et ekte norsk personnummer i en committed fil ville vært
permanent eksponert (git-historikk beholder det selv etter en senere "fjerning").

Løsning: fire nye konfigurasjonsnøkler (`Seed:AdminPersonnummer`/`AdminNavn`/
`AdminMobilNr`/`AdminEpost`), lest av en idempotent seed-blokk i `Program.cs` (samme
mønster som `IInnebygdTestSeeder`) som kjører ved hver oppstart inni den eksisterende
`IsDevelopment()`-seed-blokken — dekker BÅDE lokalt OG Azure test-App Service, siden
sistnevnte fortsatt kjører i Development-modus. Oppslag mot eksisterende administratorer
skjer via `AdminAuthenticationService.FinnVedPersonnummerAsync` (i minnet, siden
personnummer er kryptert i databasen) før noe opprettes, så kontoen aldri dupliseres —
og den gjenopprettes automatisk etter enhver database-gjenoppretting, uten manuelt
gjentatt arbeid.

Konfigurasjonen settes UTELUKKENDE via `dotnet user-secrets` lokalt og
`azd env set SEED_ADMIN_*` → Key Vault-hemmeligheter i Azure (samme
`@secure()`-parameter-mønster som Idura/Vonage-hemmelighetene) — aldri literal i
`appsettings.*.json` (som selv er committed) eller i Bicep. Fraværende konfigurasjon =
ingen seeding, samme "av som standard"-prinsipp som resten av leverandørintegrasjonene.

Kontoen logger inn med ekte BankID+2FA-flyt (ikke passord-unntaket), og siden ekte SMS
(Vonage)/e-post (ACS) allerede er konfigurert i Azure test-App Service, mottar den
faktiske 2FA-koder på ekte mobil/e-post der (i tillegg til dev-miljøets kodevisning i
UI). Verifisert ende-til-ende 2026-09-05 med Playwright mot `www.psytest.no`: personnummer-
oppslag fant riktig administrator, 2FA-bekreftelse fungerte, og kontoen vises korrekt i
`/Admin/Administratorer` uten duplikater.

**Lærdom, ikke rettet i denne økten:** `/Konto/BekreftKode` sin tekst ("mock — ingen
ekte SMS sendes i dev") er nå misvisende i Azure test-App Service, som faktisk sender en
ekte SMS via Vonage i tillegg til å vise koden i UI — teksten ble skrevet før ekte
SMS-integrasjon fantes og er ikke oppdatert siden. Kun kosmetisk (koden vises uansett),
men bør rettes til å skille "kun i dev vises koden her" fra "SMS er ekte når konfigurert".

## Åpne punkter til senere faser

- CI/CD-pipeline for `azd deploy` (i dag kjøres `azd up`/`azd deploy` manuelt fra lokal maskin) —
  naturlig neste steg for sky-deploy-delen av Del 1, se "Sky-deploy til Azure (azd)".
- Regionvalg for reell produksjon: bekreft Norway East/West-kapasitet på nytt (eller revurder
  regionbeslutningen med bruker/DPO) før ekte pasientdata — se "Sky-deploy til Azure (azd)".
- Bytte App Service sin `ASPNETCORE_ENVIRONMENT` fra `Development` til en reell produksjonsprofil
  når ekte leverandøravtaler/nøkler er på plass — se "Sky-deploy til Azure (azd)".
- IP-restriksjonen som nå beskytter test-App Service-en (se "Google Chrome/Safe Browsing
  flagget test-appen") er satt manuelt via `az webapp config access-restriction` — bør kodifiseres
  i `infra/resources.bicep` (`ipSecurityRestrictions`) fremfor å leve som en CLI-engangsendring,
  helst parameterisert via en azd-miljøvariabel siden brukerens IP kan endre seg over tid.
- Custom, brandet domene (ikke rå `*.azurewebsites.net`) for reell produksjon, samt vurdering av
  om noe UI-tekst/knappetekst kan minne om identitetstyveri-forsøk før noe eksponeres offentlig
  igjen — se "Google Chrome/Safe Browsing flagget test-appen".
- `StagingGate` (se samme seksjon) gater ALT, inkludert `/health` — helt greit så lenge ingen ekte
  overvåkning/health-probe er koblet til test-App Service-en ennå, men må huskes på hvis/når det
  legges til (Azure sin egen App Service health check-funksjon ville også blitt blokkert av gaten).
- To sekundære steder påstår fortsatt at e-post er mock (`Pages/Inviter/Verifiser.cshtml`,
  Behandlerportal `Innstillinger.cshtml.cs` sin påminnelsestekst) — se "Ekte e-postutsending via
  Azure Communication Services". Rett når disse flytene faktisk testes/brukes.
- SMS er nå ekte via Vonage (se "SMS-integrasjon: byttet fra Azure til Vonage") — verifisert kun fra
  Azure så langt, ikke fra lokalt dev-miljø ennå (samme status som e-post hadde en periode).
- Lenger opp i dette dokumentet nevnes fortsatt Link Mobility/Twilio som SMS-kandidater fra en
  tidligere vurdering — utdatert, se "SMS-integrasjon"-seksjonene for hva som faktisk ble valgt og hvorfor.
- `psytest.no` sin hostnavn-binding + SSL-sertifikat (se "Eget domene for test-miljøet") er satt
  opp via CLI, ikke kodifisert i `infra/resources.bicep` — vurder å legge dette til som
  `Microsoft.Web/sites/hostNameBindings` + `Microsoft.Web/certificates`-ressurser hvis miljøet
  noen gang må reprodusveres fra bunnen (krever da at DNS/TXT-verifisering allerede peker riktig
  FØR den delen av en `azd provision` kan lykkes, i motsetning til resten av infrastrukturen).
- Bekreft ekte e-postutsending FRA LOKALT dev-miljø når brukeren har kjørt
  `dotnet user-secrets set "Acs:ConnectionString" ...` (se "Rebranding til PsyTest") — kun mock
  var verifisert lokalt i denne økten, ekte ACS-sending er kun bekreftet fra Azure så langt.
- ACS-avsenderdomenet er fortsatt Azure sitt genererte `*.azurecomm.net`, ikke `psytest.no` — bytt
  til et ekte domenebasert avsenderdomene (`noreply@psytest.no` e.l.) når/hvis ønskelig, egen
  DNS-verifisering kreves for ACS sitt e-postdomene (SPF/DKIM/DMARC-poster i cPanel Zone Editor).
- Bekreft ekte SMS-utsending FRA LOKALT dev-miljø når `dotnet user-secrets` for Vonage er satt (se
  "SMS-integrasjon: byttet fra Azure til Vonage") — kun konfigurasjonsoppstart uten feil ble
  verifisert lokalt, ingen ekte SMS sendt derfra ennå.
- Vonage-forbruket er foreløpig på gratis prøvekreditt — vurder fakturering/betalingsmetode og et
  reelt kostnadsbilde (pris per SMS til Norge var ikke bekreftet i selve kontoen, kun anslått fra
  offentlige prislister under research-fasen) før volumet økes forbi manuell testing.
- Resten av Del 2: pris per test (fordeling test-system/behandler), økonomiske rapporter
  (uke/måned/kvartal/år), (halv-)automatisk bokføring/utbetaling, backup/restore av
  administrator, organisasjonsstøtte (eksplisitt "skal ikke støttes pt." i kravdokumentet) — alt
  naturlig hjemmehørende sammen med fase 6 (Vipps/fakturering) eller egne deloppgaver.
- Konkret databasedesign (skjema) for pasient/test/rapport — tas i fase 3–4.
- Detaljert BankID- og Vipps-leverandørvalg (Signicat/Criipto, Link Mobility/Twilio) — ekte
  implementasjoner bak `IBankIdProvider`/`ISmsSender`/`IEmailSender`/`IVippsClient` byttes inn
  når avtale er signert; mock brukes fortsatt i dev/test uansett (se prinsippet i toppen av
  dette dokumentet).
- Enhetstester for alle tjenestene i `Security`/`Domain` (ren logikk, ingen `HttpContext`-
  avhengighet — bevisst designet for å være lett å teste, men ikke gjort ennå):
  `AdminAuthenticationService`, `BehandlerAuthenticationService`, `PasientAuthenticationService`,
  `ToFaktorService`, `BehandlerInvitasjonService`, `PasientInvitasjonService`, `TestService`.
- Resten av Del 3: rapporter (per pasient/samlet), økonomi-oversikt (genererte tester/forventet
  utbetaling), automatiske test-utsendelser med påminnelser, 10-års auto-sletting av arkiverte
  pasienter (driftsjobb, hører sammen med fase 6).
- Resten av Del 4: Vipps-betalingssperre før utfylling (gjenbruk `IVippsClient`/`MockVippsClient`),
  påminnelser (frist/varighet lagres på `TestTildeling` men håndheves/varsles ikke ennå).
- Lokalisering av tester til flere språk — nå har vi et konkret andrespråksbehov å designe mot
  (WHO-5 finnes offisielt på engelsk), men fortsatt bevisst utsatt til det faktisk trengs.
- Ekte CAPTCHA-leverandør (hCaptcha/Turnstile) bak `ICaptchaProvider` — leverandørbeslutning på
  linje med BankID/Vipps/SMS (se "Offentlig design + samlet profesjonell innlogging"). I dag:
  `MockCaptchaProvider` (lokalt regnestykke) på innloggingssidene, og fortsatt kun
  honeypot+tidssjekk (`BotVern.cs`) på de offentlige registrerings-/invitasjonsskjemaene — vurder
  å legge CAPTCHA til også der når leverandør er valgt.
- Enhetstester for `Who5Skaaringsberegner`/`Who5TestSeeder`, og WHO-5-spesifikke assertions i
  integrasjonstestsuiten (se "Del 5 (slice 1)").
- Flere innebygde tester utover WHO-5 — samme mønster (`ITestSkaaringsberegner` +
  `IInnebygdTestSeeder`) er nå på plass og klart til gjenbruk, og kategoristrukturen
  (Allianse/Angst/Depresjon/Funksjon/Kjerne/Nevropsykologiske/Utredning, se "Tildelingsflyt for
  tester...") venter på faktisk innhold — seederen for en ny test kobler seg til én eller flere av
  disse via `TestService.KoblTestTilKategoriAsync`. Bevisst utsatt: hvilke konkrete
  instrumenter/spørsmål som skal fylle Allianse/Angst/Depresjon/Funksjon/Nevropsykologiske/
  Utredning er ikke besluttet — vurder lisensiering/copyright nøye per instrument (jf. WHO-5s
  kildehenvisning) før noe legges inn, ikke bare gjenbruk kjente skalanavn uten å sjekke.
- Admin-UI for å opprette/redigere/slette testkategorier — i dag kun en fast, kodet liste
  (`TestService.StandardKategorier`), seedet idempotent ved oppstart.
- Rediger/slett av SIDER/LEDD i admin-forfatterverktøyet (kun opprett i dag) — selve testens egne
  felt (navn/beskrivelse/belønningstekst/aktiv) kan nå redigeres, se "Rediger-funksjon for
  administrator/test/pasient".
- BankID-testintegrasjonen via Idura (se samme seksjon) er kun et diagnostisk sideverktøy —
  koble ekte BankID inn i selve innloggingsflyten (`IBankIdProvider`) er en egen, mye større
  beslutning (identitetsmodell, hvilket personnummer-claim som faktisk skal brukes, produksjons-
  avtale) som ikke er tatt ennå.
- Polering av admin-/behandlerportal-/pasientportal-UI (dagens sider er funksjonelle, ikke visuelt ferdige).
