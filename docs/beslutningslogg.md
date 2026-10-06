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

### Vipps + Stripe (Apple Pay/Google Pay) (2026-09-06)

Kravdokumentet spesifiserte opprinnelig kun Vipps som betalingsløsning (Del 4: "kan
det hende de må betale for den... for nå støtte VIPPS"). Bruker ønsket i tillegg Apple
Pay/Google Pay-støtte. Research avdekket at Vipps sin egen ePayment API IKKE støtter
Apple Pay/Google Pay som betalingsmiddel — de er nettleser-/enhets-baserte lommebok-
teknologier som kun dukker opp via en betalingsprosessor som eksplisitt støtter dem
(f.eks. Stripe/Adyen/Nexi), ikke noe Vipps selv formidler. Valgte **Stripe** som andre
leverandør (i tillegg til Vipps, ikke i stedet for) — kostnadsanalyse viste at
initialkostnaden ligger på Vipps uansett (som brukeren ville støttet uavhengig av
Apple/Google Pay), mens Stripe i seg selv ikke har noen fast kostnad (se under).

**Kostnadsbilde (research, ikke forhandlet avtale):** Vipps sin reelle utviklerAPI
(ikke "Vipps Go", som kun er betalingslenker uten API) ligger under "Vipps Business" —
rundt 1,25–1,75 % per transaksjon, "0 til ~200 kr/mnd avhengig av modul" (ikke fullt
bekreftet om grunn-nettbutikk-modulen faktisk er kr 0/mnd — bør avklares direkte med
Vipps salg før en reell avtale inngås). Stripe: kr 0 i oppstarts-/månedsavgift, ca.
1,5 % + 1,80 kr per kort-/Apple Pay/Google Pay-transaksjon i Norge — Apple/Google tar
ingen egen avgift for å aktivere lommeboken, den følger samme sats som et vanlig
kortkjøp. Se kildehenvisninger i selve samtaleloggen (ikke gjentatt her, kan endre seg).

**Teknisk, begge asynkrone (IKKE en synkron "charge"-samtale, i motsetning til den
opprinnelige `IVippsClient.ChargeAsync`-stubben fra fase 0):**

- **Vipps** (`VippsPaymentClient`, ekte ePayment API): `IVippsClient` redesignet til
  `OpprettBetalingAsync` (oppretter en betaling, gir en `RedirectUrl` brukeren sendes
  til) + `HentStatusAsync` (spør Vipps om faktisk utfall — ALDRI stol på at brukeren
  kommer tilbake på returUrl som bevis alene). Tilgangstoken hentes via
  `/accesstoken/get` og caches i minnet (~1 time), derfor registrert som **Singleton**
  i DI (ikke Scoped som resten av leverandørene) — se kommentarer i selve klassen.
- **Stripe** (`StripePaymentClient`, offisiell Stripe.net-SDK v52.4.1): `IStripeClient`
  med `OpprettBetalingsintensjonAsync` (oppretter en `PaymentIntent` med
  `automatic_payment_methods` — dette er det som gjør at Apple Pay/Google Pay dukker
  opp automatisk i Stripes "Payment Element" på klientsiden når enheten støtter det,
  ingen egen kode per betalingsmiddel nødvendig) + `HentStatusAsync`.
- **Webhooks** (`Security/PaymentWebhooks.cs`, `/webhooks/vipps` og `/webhooks/stripe`):
  bevisst minimal-API-endepunkter, IKKE Razor Pages — Razor Pages sin automatiske
  antiforgery-validering på POST-handlere ville avvist disse, siden Vipps/Stripe sine
  servere naturligvis ikke har vår antiforgery-cookie. Stripe-verifisering bruker
  Stripe sin egen SDK-metode (`EventUtility.ConstructEvent`) og er umiddelbart
  verifiserbar siden Stripe sitt testmiljø er selvbetjent. Vipps-verifisering er
  implementert etter Vipps sin offentlige dokumentasjon (kanonisk streng
  `{METHOD}\n{PATH}\n{DATE};{HOST};{CONTENT_HASH}`, HMAC-SHA256 med webhook-
  hemmeligheten, signatur i `Authorization`-headeren) — men er **IKKE bekreftet mot et
  ekte, levende Vipps-webhook-kall ennå**, se avsnittet under om sandkasse-tilgang.
  `HentStatusAsync`/polling er derfor den reelle fallback-mekanismen inntil dette er
  verifisert live.
- **StagingGate**: begge webhook-stiene er unntatt sperren (`StagingGate.cs`), samme
  resonnement som BankID-callbacken — et server-til-server-kall fra Vipps/Stripe har
  ingen mulighet til å sende vår cookie, og den reelle sikkerheten er HMAC-
  signaturverifiseringen inni selve handleren.
- **Diagnostisk sideverktøy** (`/BetalingTest/Vipps` og `/BetalingTest/Stripe`, lenket
  fra `/DevDemo` når konfigurert): samme mønster som `/BankIdTest` — verifiserer selve
  den tekniske integrasjonen (en ekte 1-krone testbetaling) UTEN å være koblet til noen
  reell "betal for test"-flyt i pasientsystemet. Den faktiske forretningsflyten (når i
  pasientreisen betaling skjer, prising, hvilken leverandør pasienten velger) er
  BEVISST IKKE designet her — bruker skal designe denne selv.

**Sandkasse-tilgang, ulik friksjon (viktig for videre arbeid):**
- **Stripe**: selvbetjent, umiddelbar — akkurat som Idura for BankID. Ingen ventetid.
  Apple Pay på nett krever i tillegg domeneverifisering i Stripe Dashboard (last opp en
  fil, eller registrer domenet via API) — en engangs, manuell dashueby-oppgave, ikke
  noe som kan kodifiseres i Bicep.
- **Vipps**: IKKE selvbetjent — sandkasse-/testmiljøet krever enten et allerede
  godkjent kunde-/merchant-forhold, ELLER en "partner-søknad" (raskere, men fortsatt
  en godkjenningsrunde) — mer likt ACS SMS sin Norge-friksjon enn Idura sin frie
  BankID-testkonto. Ende-til-ende-verifisering av den ekte Vipps-integrasjonen
  (inkludert webhook-signaturen) må derfor vente til brukeren har fått testtilgang.

**Reell feil funnet og rettet underveis:** `Pages/BetalingTest/Vipps.cshtml` manglet
først `@model`-direktivet (kopiert fra `BankIdTest/Start.cshtml`-mønsteret, men
`@model`-linjen ble glemt). Konsekvens: siden returnerte stille `200 OK` med TOM body
i stedet for enten å kjøre `VippsModel.OnGetAsync` sin logikk eller feile synlig —
Razor Pages faller tilbake til en tom standard-side når en `.cshtml`-fil mangler
`@model`, den kobler IKKE automatisk til en likt-navngitt PageModel-klasse i samme
mappe basert på filnavn alene. Rettet ved å legge til
`@model TestBase.Web.Pages.BetalingTest.VippsModel`. Se også ny fallgruve i CLAUDE.md.

**Stripe verifisert ende-til-ende 2026-09-06** med brukerens egen, gratis, selvbetjente
Stripe-sandkasse: `/BetalingTest/Stripe` opprettet en ekte `PaymentIntent` (avdekket
underveis at Stripe har en minimumsgrense på kr 3,00 for NOK — den opprinnelige
diagnostiske summen på 1 kr ble avvist med en ekte, presis feilmelding fra Stripe sin
API, rettet til 5 kr), viste Payment Element med ekte testkort (`4242 4242 4242 4242`),
og fullførte betalingen — landet på `/BetalingTest/StripeResultat` med status "Betalt".
Automatisert gjennomklikking via Playwright ble stoppet av Stripe sin egen
bot-/svindeldeteksjon (en hCaptcha-utfordring, sannsynligvis fordi Playwright-trafikk
fremstår som automatisert) — det siste "bekreft betaling"-steget ble derfor fullført
manuelt av bruker i egen nettleser i stedet, noe som er forventet og riktig oppførsel
fra Stripe sin side, ikke en feil i integrasjonen.

### Midlertidig HTTP Basic Auth i StagingGate (2026-09-06)

Vipps sin merchant-registrering (som bruker gikk for i stedet for partner-programmet,
se "Vipps + Stripe" — riktig valg for en enkeltpraksis som skal ta betalt for egne
tjenester, ikke bygge integrasjoner for andre) har et "Verifiser nettstedet"-steg som
krever standard HTTP Basic Auth (eget brukernavn+passord-skjema i søknaden) for
passordbeskyttede nettsteder. `StagingGate` sitt egendefinerte
ett-felts-nøkkelskjema er ikke noe en automatisert tredjeparts nettsted-verifiserer
kan fylle ut.

Løsning: en midlertidig, EKSTRA aksepteringsvei i `StagingGate.cs` — ekte HTTP Basic
Auth, aktivert KUN når `StagingGate:BasicAuthUsername`/`BasicAuthPassword` begge er
satt (samme "fravær = av"-mønster som resten av StagingGate/leverandørene). Når
aktiv, sendes `WWW-Authenticate: Basic`-header på 401-responsen — dette gjør at ALLE
besøkende uten gyldig cookie ser nettleserens NATIVE Basic Auth-dialog i stedet for
vår egen HTML-nøkkelside, siden nettlesere reagerer på selve headeren uansett
responsinnhold. Bevisst akseptert som en midlertidig kosmetisk endring, ikke noe å
la stå permanent — fjern `StagingGate:BasicAuthUsername`/`BasicAuthPassword`
(`azd env set` til tomme verdier + `azd provision`) så snart Vipps sin
nettsted-verifisering er bestått, for å gå tilbake til kun nøkkelskjemaet.

**Reell feil unngått under implementasjon:** skrev først `IsNullOrEmpty` for å sjekke
om Basic Auth-konfigurasjonen var satt — ville vært feil, siden Key Vault sin
plassholderverdi for "ikke satt" er ETT MELLOMROM (`' '`), ikke tom streng (se
`empty(x) ? ' ' : x`-mønsteret i `infra/resources.bicep`, brukt for ALLE hemmeligheter
her). `IsNullOrEmpty(" ")` er `false` — ville gjort Basic Auth "aktiv" i Azure selv
når ingen verdi faktisk var satt, og dermed slått av hele nøkkelskjema-sperren
utilsiktet. Rettet til `IsNullOrWhiteSpace` (samme fallgruve som alle andre
`empty(x) ? ' ' : x`-konfigurerte verdier i dette prosjektet må sjekkes med).

### Salgsvilkår-side for Vipps sin nettsted-verifisering (2026-09-06)

Vipps sin merchant-registrering krever en salgsvilkår-side som minst dekker Parter/
Betaling/Levering/Angrerett/Retur/Reklamasjonshåndtering/Konfliktløsning, pluss synlig
firmanavn/organisasjonsnummer/adresse/telefon/e-post (se
https://vippsmobilepay.com/nb-NO/legal/krav-til-nettside). Ny side `/Salgsvilkar`
(`Pages/Salgsvilkar.cshtml(.cs)`, lenket fra footer i `_Layout.cshtml`) dekker alle
disse seksjonene.

**Bevisst forskjellig fra en vanlig nettbutikk:** kjøpet involverer TRE parter, ikke to
— Behandler (den autoriserte psykologspesialisten som har det faglige
behandlingsansvaret og BESTILLER/TILDELER testen som en del av behandlingen — pasienten
velger ikke selv hvilken test som "kjøpes"), Pasient (mottar behandlingen, gjennomfører
og betaler for testen), og PsyTest (plattformen som formidler selve betalingen). "Parter"-
seksjonen er skrevet for å gjøre dette tydelig — det faglige behandlingsforholdet
(utredning/diagnostisering) er eksplisitt IKKE en del av selve salgsavtalen, kun
betalingen for tilgang til å gjennomføre testen er det.

**Eksplisitt UTKAST, samme status som `docs/compliance-dpia-utkast.md`** — bruker skal
la en jurist kvalitetssikre/erstatte teksten før reelle pasienter bruker tjenesten.
Spesielt angrerett-seksjonen (§ 22-unntak for digitalt innhold/fullførte tjenester) er
markert som juridisk ikke-vurdert i selve sideteksten — riktig klassifisering av en
psykologisk test opp mot disse unntakene er en reell juridisk vurdering, ikke gjort her.

**Reell, ennå ikke lukket blokkerende gap:** footer sin kontaktinfo (`_Layout.cshtml`)
hadde fra før kun plassholder e-post/telefon, og manglet organisasjonsnummer/adresse
HELT — begge nå lagt til som plassholdere i footer OG på selve salgsvilkår-siden, men
Vipps sin nettsted-verifisering krever ekte informasjon her. Dette må fylles inn av
bruker (og trolig samkjøres med den faktiske organisasjonsformen som til slutt brukes
for Vipps-merchant-avtalen) før verifiseringen faktisk kan bestås — se "Åpne punkter".

### Partner System + Test Monetization (2026-09-07)

Stor ny funksjon bygget etter en lang avklaringsrunde med bruker (opprinnelig
skissert, mye større, i `finance_system.docx`) — se hele planen i
`C:\Users\gaute\.claude\plans\fizzy-churning-dusk.md` for full kontekst og
resonnement. Bevisst kraftig forenklet fra det opprinnelige dokumentet:
partner-egen database/pålogging/embedding, ekte Stripe Connect-utbetaling,
multi-språk testversjoner og formell regnskapsstandard er ALLE eksplisitt
utsatt til senere — se "Åpne punkter" under.

**Ny datamodell:** `Partner` (Superadmin oppretter), `PartnerTestTilgang`
(Superadmin-kuratert allow-list — partneren velger IKKE selv hvilke tester de
får), `PartnerTestAndel` (partnerens EGEN andel per test, satt av deres egen
partner-admin, alltid klemt til minst Superadmin sitt gulv), `Test` fikk
prisingsfelt (`MinstePrisKr`/`StorstePrisKr`/`TypiskBehandlerHonorarKr`/
`MinstePartnerAndelKr` — Min/Maks er grenser på PASIENTENS TOTALPRIS, ikke
bare behandlerens honorar, bekreftet eksplisitt med bruker), `Behandler` fikk
`PartnerId`/`ErPartnerAdministrator`/`HarEgetAbonnement`, `Administrator` fikk
`ErSuperadmin`. Finansiell snapshot (`TestTildelingBetaling`, 1:1 mot
`TestTildeling`, bevisst en EGEN tabell fremfor flere nye kolonner direkte på
`TestTildeling` — se kjent EF-fallgruve om feiltolkede kolonneendringer i
CLAUDE.md) og en ren regnskapslogg (`Pengebevegelse`).

**Roller:** Ny `UserRole.Superadmin` — strengt supersett av `Administrator`
(inkludert i `AdminOmrade`-policyen), egen `SuperadminOmrade`-policy for
partner-/prisingssider. Kun ÉN reell konto får dette (den config-seedede
admin-kontoen, se "Seed av brukerens egen admin-konto"). Partner-admin er
BEVISST IKKE en ny rolle — kun en claim (`ErPartnerAdministrator`/`PartnerId`)
satt ved innlogging, sjekket via en `RequireAssertion`-policy
(`PartnerAdminOmrade`). Rad-nivå-filtrering (kun samme partner) håndheves
eksplisitt i hver spørring, ikke av policyen alene.

**Prisingsformel** (`TestPrisberegner`, ren/tilstandsløs, 8 enhetstester i
`TestPrisberegnerTests.cs`): `plattform` (0 hvis dekket av abonnement) +
`partner` (0 hvis ingen partner) + `behandlerHonorar` (behandlerens ønskede
beløp, standard = testens typiske honorar) klemmes samlet til testens
Min/Maks — overskrides maks, er det ALLTID behandlerens egen andel som
reduseres, plattform-/partnerandelen er garanterte gulv. Beregnes ÉN gang per
test per tildelingsbatch (identisk for alle pasienter fra samme behandler i
samme batch) i `TestTildelingsService.TildelOgVarsleAsync`, som nå også
skriver én `TestTildelingBetaling`-rad per tildeling.

**Betalingsflyt:** `Pasientportal/Tester/Fyll` sjekker nå
`TestTildelingBetaling.Status` (gatet i BÅDE `OnGetAsync` og `OnPostAsync`,
jf. kjent fallgruve om å kun gate i viewet) og sender til en ny
`Pasientportal/Tester/Betal`-side (gjenbruker EKSAKT samme mønster som det
allerede verifiserte `/BetalingTest` — Vipps-redirect eller Stripe Payment
Element) når betaling er `Venter`. `Security/PaymentWebhooks.cs` sin
tidligere TODO er nå fullført: Vipps-webhooken leser `reference` fra
JSON-payloaden (format `tildeling-{id}`, MERK: eksakt JSON-form fortsatt ikke
bekreftet mot et ekte Vipps-kall, se "Vipps + Stripe"), Stripe-webhooken leser
en `referanse`-metadata-verdi satt på `PaymentIntent` ved opprettelse
(`IStripeClient.OpprettBetalingsintensjonAsync` fikk en ny påkrevd
`referanse`-parameter). Begge kaller samme
`TestService.MarkerBetalingBetaltAsync` (idempotent — trygt om webhook og
brukerens retur-side (`BetalResultat`, synkron `HentStatusAsync`-fallback,
viktig for Vipps siden webhooken ikke er live-verifisert) begge prøver).

**Verifisert:** 8 enhetstester (`TestPrisberegnerTests`) + 2 nye
ende-til-ende-tester (`BetalingPipelineTests`, mot en ekte migrert
testdatabase — dekker både en full betaling-til-ledger-runde MED
partner/honorar, idempotens ved dobbel bekreftelse, og at en uprissatt test
fortsatt går rett gjennom uten noen betalingssperre eller ledger-rader) — 14/14
grønt. Manuelt via Playwright: opprettet en ekte partner og satt WHO-5-prising
gjennom Superadmin-kontoen, bekreftet i databasen.

### Håndhevet partnerens test-allow-list ved tildeling (2026-09-07)

Full manuell ende-til-ende-verifisering av hele "Partner System + Test
Monetization" (Superadmin setter prising → oppretter partner → inviterer
behandler → kobler behandler til partner + gjør partner-admin → partner-admin
setter egen andel → behandler legger til pasient → tildeler med honorar →
pasient betaler ekte Stripe-testkort → fullfører testen → Superadmin sin
`/Admin/Okonomi` viser riktige tall) fungerte perfekt og bekreftet hele
regnestykket (50 kr plattform + 30 kr partner + 100 kr honorar = 180 kr totalt,
"Inntekt fra salg" 50 kr, "Utgift til salg" 130 kr — alt stemte eksakt).

Underveis ble ett reelt hull avdekket: `PartnerTestTilgang` (allow-listen,
se forrige seksjon) var kun håndhevet i Superadmin/partner-admin sin
KONFIGURASJON av allow-listen, ikke faktisk i selve tildelingsflyten — en
partner-tilknyttet behandler så og kunne tildele ALLE aktive tester, ikke
bare de på partnerens liste. Usynlig i den manuelle testen siden WHO-5 var
eneste test i systemet. Rettet i to lag (samme "gate ved bruk, ikke bare i
viewet"-prinsipp som CLAUDE.md sine kjente fallgruver allerede advarer om):

1. `TestService.HentKategoriTreAsync` fikk en ny valgfri `partnerId`-parameter
   — når satt, filtreres tre-visningen (steg 2 i tildelingsflyten) til KUN
   partnerens tillatte tester. `null` (admin, uavhengig behandler, ELLER
   Superadmin sin egen allow-list-konfigurasjonsside som nettopp trenger ALLE
   tester å velge blant) → ingen filtrering, uendret oppførsel.
2. `TestTildelingsService.TildelOgVarsleAsync` filtrerer nå SELV `testIder`
   ned til partnerens allow-list FØR noe opprettes, uavhengig av hva som ble
   sendt inn — en rå POST med en test utenfor allow-listen oppretter rett og
   slett ingen tildeling for den testen, i stedet for å stole på at UI-listen
   alene hindrer det.

Ny enhetstest (`PartnerbehandlerKanIkkeTildeleTestUtenforAllowList` i
`BetalingPipelineTests.cs`) dekker begge lagene mot en ekte database — 15/15
grønt totalt.

### Automatisk EF-migrasjon ved oppstart — eneste vei til psytest.no sin database (2026-09-07)

Etter forrige rettings-commit skulle den nye migrasjonen
(`20260906235354_PartnerSystemOgTestPrising`) også ut til test-App Service.
Da ble et reelt, hittil udokumentert hull avdekket: Azure MySQL Flexible
Server sin brannmur har KUN regelen `AllowAzureServices` (0.0.0.0–0.0.0.0) —
ingen regel slipper til utviklerens lokale maskin, så `dotnet ef database
update` kan ikke kjøres direkte mot den. `Program.cs` hadde heller ingen
`Database.MigrateAsync()`-vei, og ingenting i denne loggen forklarer hvordan
tidligere migrasjoner faktisk kom seg til Azure — antagelig manuelt/ad-hoc.

Løst permanent: `Database.MigrateAsync()` kjøres nå automatisk helt først i
dev-seed-blokken i `Program.cs` (`if (app.Environment.IsDevelopment())`).
Dette dekker Azure test-App Service også, siden den (se samme blokks
eksisterende kommentar om superadmin-seeden) bevisst fortsatt kjører i
Development-modus. Trygt fordi: (1) miljøet har uansett kun syntetisk
testdata, jf. "ingen ekte pasientdata i dev/test noensinne", (2)
`MigrateAsync` er idempotent — ingenting skjer på oppstarter uten ventende
migrasjoner. Dette må revurderes den dagen appen kjører i ekte
Production-modus mot en database med reelle pasientdata — automatisk migrasjon
ved hver oppstart er en rimelig avveining for et testmiljø, ikke noe som bør
videreføres ukritisk til en reell driftssetting.

### Manglende navigasjon til Superadmin-sidene (2026-09-07)

Da brukeren logget inn som sin egen Superadmin-konto på psytest.no fant hen
umiddelbart et reelt UX-hull: `_Layout.cshtml` sin `erAdmin`-sjekk (styrer
hele funksjons-nav-raden) var aldri oppdatert til å inkludere
`UserRole.Superadmin` da den rollen ble lagt til — en ren Superadmin (uten
også å være Utvikler) fikk altså INGEN admin-knapper i det hele tatt, ikke
bare manglende lenke til Partnere. Usynlig under selve
Partner System-utviklingen fordi all lokal/manuell testing skjedde som
Utvikler (som alltid har inkludert Superadmin-sidene i policyene, se
`Program.cs`), aldri som en ren `Administrator.ErSuperadmin=true`-konto.

Rettet:
- Ny `erSuperadmin`-variabel (`Role == Superadmin || Role == Utvikler`) i
  `_Layout.cshtml`, og `erAdmin` utvidet til å inkludere `Superadmin` slik at
  en ren Superadmin i det minste ser alt en Administrator ser.
- Tre nye funksjonsknapper ("Partnere", "Prising", "Økonomi") lagt til i
  admin-funksjonsnavet, synlige kun når `erSuperadmin`.
- `Admin/Partnere/Rediger.cshtml` (partnerens redigeringsside) manglet en vei
  videre til allow-list/prisingssiden (`Admin/Partnere/Tester/{id}`) — den
  fantes kun som en lenke tilbake på `Admin/Partnere/Index.cshtml`, ikke inne
  på selve redigeringssiden. Lagt til en direkte lenke øverst på
  redigeringssiden.

Verifisert lokalt: bygget grønt, 15/15 tester grønt, og manuelt i nettleser
(Playwright) — nav-knappene og lenken vises og fungerer som forventet.

### Bug-runde etter brukerens andre manuelle gjennomgang (2026-09-07)

Brukeren logget ut som Superadmin og inn igjen som partner-admin, og fant en
rekke reelle feil. Rettet i denne runden:

1. **Krasj ved tildeling som behandler**: `System.FormatException: The input
   string 'PasientIderCsv' was not in a correct format` i
   `DictionaryModelBinder`. Rotårsak: `Behandlerportal/Tildel/Tester.cshtml.cs`
   sin `[BindProperty] Dictionary<long, decimal?> HonorarKr` — når INGEN test
   i batchen har prising konfigurert (så ingen `HonorarKr[...]`-felt postes i
   det hele tatt), faller ASP.NET Cores modellbinder for et TOPPNIVÅ
   Dictionary/collection-`[BindProperty]` tilbake til å tolke ALLE andre
   skjemafelt-NAVN på siden (som `PasientIderCsv`, `TestIder`) som om de var
   dictionary-nøkler, og kaster når disse ikke er tall. Dette er en reell,
   dokumentert ASP.NET Core-fallgruve for Dictionary/collection-typede
   `[BindProperty]`-egenskaper uten treff på sitt eget prefiks — IKKE noe som
   fanges av å kalle `OnPostSendAsync` direkte i en test (slik
   `BetalingPipelineTests.cs` gjør), siden det hopper forbi selve
   modellbinding-pipelinen. Rettet ved å fjerne `[BindProperty]` fra
   `HonorarKr` helt og lese den manuelt fra `Request.Form` i stedet
   (`LesHonorarFraSkjema()`) — strukturelt umulig for MVC å røre egenskapen nå,
   ikke bare en symptomlapp. Ny kjent fallgruve, bør inn i CLAUDE.md.
2. **Partner-admin hadde ingen navigasjon i det hele tatt**: samme klasse feil
   som Superadmin-navigasjonshullet over, men for `ErPartnerAdministrator` —
   `_Layout.cshtml` sin `erBehandler`-blokk manglet helt lenker til
   `Behandlerportal/MinPartner/Behandlere` og `.../Prising`. Funksjonaliteten
   fantes allerede og virket (bygget tidligere), den var bare uoppdagelig.
   Lagt til to nye knapper ("Min partner", "Partnerprising"), synlige kun når
   `CurrentUser.ErPartnerAdministrator`.
3. **Manglende honorar-mulighet ved tildeling** var i praksis samme
   grunnårsak som (1)+(2) kombinert: testen som ble tildelt manglet
   `StorstePrisKr > 0` (aldri konfigurert, siden Prising-siden var
   unåbar), så `@if (test.StorstePrisKr > 0)` i tildelingssiden aldri viste
   honorar-feltet — og selve forsøket på å sende krasjet i tillegg pga (1).
4. **`/Behandlerportal/Oppgaver`-lenken i daglige påminnelser pekte på
   `localhost:7257`** på selve psytest.no: `DagligPaaminnelseBakgrunnstjeneste`
   bygger lenker fra `Varsling:BaseUrl`-konfigurasjon, som aldri var satt i
   Azure (fallback var den hardkodede lokale utviklings-URL-en). Lagt til
   `Varsling__BaseUrl = https://www.psytest.no` i `infra/resources.bicep` sin
   `appSettings`-liste (literal, ikke hemmelig — offentlig domenenavn).
   Krever `azd provision` for å ta effekt, ikke bare `azd deploy`.
5. **`Admin/Partnere/Rediger.cshtml` sin nye lenke ("administrer tester og
   prising") gikk faktisk BARE til allow-list-siden** (avkrysningsbokser for
   hvilke tester partneren får), ikke noe reelt prisingsgrensesnitt — siden
   selve pris-grensene er GLOBALE per test (`Admin/Tester/Prising`), ikke
   partner-spesifikke. Delt opp i to tydelige lenker: én til allow-listen, én
   direkte til den globale prissiden.
6. **Kunne kun legge til eksisterende, uavhengige behandlere i en partner via
   en nedtrekksliste** — ingen måte å invitere en helt ny behandler (eller
   gjøre noen til partner-admin) ved å skrive inn e-post. Lagt til på
   `Admin/Partnere/Rediger.cshtml`:
   - Et enkelt e-postfelt: finnes en uavhengig behandler med den e-posten,
     vises en eksplisitt bekreftelsesprompt ("Fant eksisterende behandler X —
     koble til?") FØR noe faktisk kobles — ingen stille kobling. Finnes ingen
     match, sendes en vanlig invitasjon (`BehandlerInvitasjonService`, som
     allerede støttet `partnerId`) med e-post som eneste kontaktmetode.
     E-post som matcher en administrator-konto avvises med feilmelding.
   - Et bulk-felt (tekstområde, én e-post per linje/komma-separert) som gjør
     det samme for flere e-poster samtidig, UTEN enkeltvis bekreftelse (lista
     er allerede eksplisitt skrevet inn av Superadmin) — rapporterer
     "N koblet, M invitert, hoppet over: ..." i én oppsummering.
   - (Partner-admin sin EGEN invitasjon av kolleger — SMS ELLER e-post, se
     `Behandlerportal/MinPartner/Behandlere.cshtml` — fantes faktisk allerede
     fra før og virket; den var bare uoppdagelig pga. punkt 2 over.)
7. **`/Admin/Okonomi` periodisert**: viste tidligere kun to alltid-summerte
   tall (siden systemets begynnelse). Bygget om til måned-for-måned-rader for
   inneværende år (med en "Hittil i år"-fotrad), og ett summert tall per
   TIDLIGERE, avsluttede kalenderår (kollapset til én rad når året er omme,
   slik brukeren spesifiserte) — se `Areas/Admin/Pages/Okonomi/Index.cshtml.cs`.
8. **Manglende felt fikk ingen visuell rød-innramming ved innsendingsforsøk**
   på noen skjema (pasient-/behandler-/administrator-/partner-registrering).
   Lagt til en global, sidebred mekanisme: `wwwroot/js/validering.js`
   (fanger `submit` på ALLE `<form>`, kjører `checkValidity()`, legger til
   klassen `skjema-forsokt-sendt` på selve FORM-elementet KUN ved feilet
   forsøk — ikke ved sidelasting, for å unngå rødt før brukeren har rukket å
   gjøre noe) + en CSS-regel i `site.css`
   (`form.skjema-forsokt-sendt :invalid { border: 2px solid ...; }`). Lagt til
   `required`/`type="email"` på de faktisk obligatoriske feltene i de mest
   sentrale registreringsskjemaene (pasient-egenregistrering,
   behandler-invitasjon-fullføring, ny administrator, ny partner) slik at
   native HTML5-validering faktisk har noe å style. IKKE gjort uttømmende for
   ALLE skjema i systemet i denne runden — mekanismen virker automatisk for
   ethvert fremtidig skjema med `required`-attributter, men eldre skjema uten
   `required` i det hele tatt (f.eks. behandler/admin sine invitér-skjema, som
   bevisst tillater "enten mobil ELLER e-post" og derfor ikke passer en enkel
   `required`) er ikke gjennomgått ett for ett.

Verifisert: bygget grønt, 15/15 tester grønt (uendret — ingen av disse
rettingene hadde eksisterende testdekning, se punkt 1 sin forklaring på
HVORFOR krasjen ikke ble fanget av eksisterende tester). Punkt 4 krever
`azd provision` (infra-endring), ikke bare `azd deploy`.

### Tredje bug-runde: tildelingsdialogen, prising-input, pasientvisning på tvers av partnerskapet (2026-09-09/10)

Brukerens tredje manuelle gjennomgang denne uken. Rettet:

1. **Knappestørrelse/-farge i tildelingsdialogen**: "Gå til oppsummering",
   "Avbryt" og "Bekreft og send" i `Behandlerportal/Tildel/Tester.cshtml` og
   `Admin/Tildel/Tester.cshtml` hadde ingen `.btn`-klasse i det hele tatt
   (nettleserens standard, uformaterte knapp). Lagt til `.btn .btn-accent`
   (oransje) for de bekreftende, og en NY klasse `.btn-muted` (nøytral grå
   outline, bevisst forskjellig fra både `.btn-accent` og `.btn-outline` — jf.
   brukerens "kanskje avbryt-knapper bør være en annen fargekombo") for Avbryt.
2. **Reell prisoppsummering i dialogen**: dialogen viste bare testnavn, ingen
   pris, og ingen mulighet til å velge varslingsmetode — begge deler manglet
   helt (IKKE en visningsfeil, funksjonaliteten fantes ikke). Lagt til:
   - Tre valgknapper (SMS/E-post/Begge) i dialogen. `TestTildelingsService.TildelOgVarsleAsync`
     fikk en ny `varslingsmetode`-parameter (default `Begge`) som nå ALLTID er et
     eksplisitt valg fra avsenderen for selve tildelingen — pasientens egen
     lagrede `Varslingspreferanse` brukes fortsatt uendret for
     `SendRapportKopiAsync` (rapport-kopier), det er en annen flyt.
   - En JavaScript-speiling av `TestPrisberegner.Beregn` i `wwwroot/js/tildel.js`
     som viser pris PER TEST og summert i bunn for HELE batchen (inkl. hvem som
     får hvor mye — plattform/partner/behandler) FØR innsending, uten
     server-tur. Prisdataene (Min/Maks/typisk honorar/partnerandel/dekket-av-
     abonnement) leses fra nye `data-*`-attributter på hver test-checkbox,
     hentet via en ny `TestTildelingsService.HentPrisingskontekstAsync`.
     Admin-siden har ingen slike attributter (admin-tildeling er alltid
     "IkkePåkrevd") — JS-en degraderer stille til bare testnavn der.
   - **SMS koster penger å sende (Vonage) — nytt `Priser:SmsGebyrKr`-
     konfigurasjonsnøkkel** (default 0 kr, samme mønster som `Varsling:BaseUrl`
     — settes uten redeploy). Når varslingsmetoden inkluderer SMS, legges
     gebyret OVENPÅ pasientens klemte totalpris (ikke inni Min/Maks-grensene —
     det er en kanalkostnad, ikke en del av selve testens pris) og går i sin
     helhet til plattformandelen. `TestPrisberegner.Beregn` fikk en ny
     `smsGebyrKr`-parameter (default 0, lagt til SIST i signaturen — ingen
     eksisterende kallsteder påvirket). **Reell konsekvens å være obs på**: en
     helt gratis/uprist test (Min=Maks=0) kan bli betalingspliktig UTELUKKENDE
     fordi SMS ble valgt som varslingsmetode for den utsendingen — bevisst
     akseptert som riktig (plattformen må dekke den reelle SMS-kostnaden fra
     NOEN), men verdt å huske hvis det oppleves som overraskende senere.
     `Priser:SmsGebyrKr` MÅ settes til et reelt tall før reell bruk — 0 kr nå.
3. **`DictionaryModelBinder`-krasjen fra forrige runde re-dukket delvis opp
   som en NY, mer alvorlig oppdagelse**: da prisfeltene på
   `Admin/Tester/Prising` og `Behandlerportal/MinPartner/Prising` ble undersøkt
   for høyrejustering, viste det seg at de faktisk konfigurerte prisene
   (f.eks. 50 kr) IKKE vises i feltene i det hele tatt — feltene fremstår tomme.
   Årsak: `value="@test.MinstePrisKr"` bruker Razors STANDARD `ToString()`, som
   følger serverens gjeldende KULTUR (norsk Windows → komma som
   desimalskilletegn, f.eks. "50,00") — men HTML5 `<input type="number">`
   krever ALLTID punktum, uansett sidespråk, og forkaster stille en verdi med
   komma (feltet vises tomt, ingen feilmelding). **Reell fare**: en admin som
   åpner siden, ser et tomt felt (uvitende om at det faktisk er en tidligere
   satt pris under overflaten) og trykker "Lagre" uten å skrive inn noe på
   nytt, ville stille NULLSTILT en fungerende pris til 0 kr. Rettet ved å
   bruke `.ToString(System.Globalization.CultureInfo.InvariantCulture)` på
   ALLE `value=`/`min=`/`max=`-attributter på `type="number"`-felt i hele
   appen (6 forekomster funnet og rettet: 4 på Admin/Tester/Prising, 1 på
   MinPartner/Prising, 1 honorar-feltet på Behandlerportal/Tildel/Tester) — ny
   kjent fallgruve, lagt til CLAUDE.md. Rene visningstekster (som "maks X kr"
   utenfor et faktisk skjemafelt) er IKKE endret — komma er korrekt der, det
   er kun maskinlesbare HTML5-attributter som må være invariant-formatert.
4. **NOK i stedet for kr + høyrejustering + mystisk hvit boks** på samme to
   prisingssider: kolonneoverskriftene sa "(kr)", ikke per rad; tallene var
   venstrejustert; og en synlig tom hvit boks dukket opp etter tallet. Sistnevnte
   var det tomme `<form>`-elementet som holder den skjulte `testId`-en (HTML5
   "form=id"-trikset fra CLAUDE.md sine kjente fallgruver) — et `<form>` er et
   blokkelement som tar synlig plass selv når det er tomt. Rettet: "(NOK)" i
   overskriften OG "NOK" etter hvert felt, en ny `.tall-input`-CSS-klasse
   (`text-align: right`), og `hidden`-attributt på de tomme skjemaene (påvirker
   ikke selve "form=id"-koblingen — kun skjulte elementer kan fortsatt være
   gyldige skjema-mål).
5. **Partner-admin sett ALLE pasienter i partnerskapet, admin fikk en
   Partner-kolonne**: `Behandlerportal/Pasienter/Index.cshtml.cs` viste
   tidligere KUN innloggede behandler sine egne pasienter, uansett rolle. Nå:
   når `CurrentUser.ErPartnerAdministrator`, vises ALLE pasienter for ALLE
   behandlere i partnerskapet, med en ny Behandler-kolonne (samme prinsipp som
   `Admin/Pasienter` alt viste på tvers av behandlere). `Admin/Pasienter`
   fikk i tillegg en ny Partner-kolonne. Siden `Detaljer.cshtml.cs` og
   `Rediger.cshtml.cs` fortsatt håndhevet "kun egen pasient", ville en
   partner-admin ha sett raden i listen men fått 404/redirect ved klikk — ny
   delt `HarTilgangAsync`-sjekk (egen pasient ELLER partner-admin i samme
   partnerskap som pasientens behandler) lagt til begge steder.
6. **"Bytt behandler"-popup på Rediger pasient**: ny handler
   `OnPostByttBehandlerAsync` + en `<dialog>` med alle behandlere i SAMME
   partnerskap som pasientens nåværende behandler (tom liste — og popupen
   vises ikke i det hele tatt — hvis behandleren er uavhengig, siden det da
   ikke finnes noe partnerskap å velge innenfor). Tilgjengelig for BÅDE
   pasientens egen behandler (vanlig "overlever saken til en kollega") og en
   partner-admin (via den utvidede tilgangen i punkt 5).

Verifisert: bygget grønt, 16/16 tester grønt (inkl. en ny
`TestPrisberegnerTests`-test for SMS-gebyr-formelen), samt manuell
Playwright-gjennomgang lokalt av tildelingsdialogen (knappefarger, testnavn i
oppsummeringen, ingen konsoll-feil) og prisingssidene (NOK, høyrejustering,
faktiske lagrede tall vises nå korrekt). IKKE live-testet med ekte
partner+behandler+pris-oppsett: den fulle pris-forhåndsvisningen i
Behandlerportal-dialogen (med partnerandel og SMS-gebyr faktisk beregnet) og
"Bytt behandler"-popupen med flere reelle behandlere i samme partnerskap er
verifisert ved kodegjennomgang og at formelen matcher `TestPrisberegner`
(samme enhetstestede formel), men ikke klikket gjennom med ekte data denne
runden.

### Ekte betalingsside for pasienten — for Vipps sin nettsted-verifisering (2026-09-11)

Vipps sin KYC-saksbehandler godtok "kun engangsbetaling, ikke faste
betalinger" (se forrige runde), men kunne ikke godkjenne bestillingen uten å
se selve produktet/tjenesten og prisen slik en ekte kunde ser det — noe som
ikke var mulig siden alt ligger bak BankID-innlogging. Ba om enten
testmiljø-tilgang eller et skjermbilde.

`Pasientportal/Tester/Betal.cshtml` var fortsatt den rå diagnostiske
utgaven fra da betalingsflyten først ble koblet til (ustylet `<button>`,
ingen visuell polish) — bygget om til en ekte, presentabel betalingsside:

- Én samlet totalpris i stor skrift (`TotalprisKr` — aldri en oppdelt
  visning til pasienten, i tråd med det opprinnelige designprinsippet "Patient
  never sees a breakdown").
- En Vipps-knapp i Vipps sin offisielle merkevarefarge (`#ff5b24`) — ren
  tekst/farge, ikke selve det varemerkebeskyttede logo-bildet, som er
  standard og fullt ut akseptert praksis for norske nettbutikker.
- Kortbetalingen (Stripe Payment Element) fikk en tydelig "Betal med
  kort"-boks med samme visuelle vekt som Vipps-knappen, pluss en liten
  "Kort · Apple Pay · Google Pay"-tekstlinje som stemmer med det
  salgsvilkårene allerede lovet.
- En eksplisitt "Dette er en engangsbetaling — ikke et abonnement"-setning
  rett ved siden av betalingsknappene (samme sted en ekte kunde/Vipps-
  saksbehandler faktisk ser den), pluss en direkte lenke til
  `/salgsvilkar`.
- Ny CSS-seksjon i `site.css` (`.checkout-card`, `.btn-vipps`, `.btn-kort`
  m.fl.) — et avgrenset "kort" midt på siden, ikke løse skjemaelementer
  strødd utover slik det var før.

Verifisert med en full, reell ende-til-ende-manuell test lokalt: ny
pasient opprettet med kjent personnummer → fullført egenregistrering →
tildelt WHO-5 med honorar via den ordinære Behandlerportal-tildelingsflyten
(som samtidig bekreftet at forrige rundes pris-forhåndsvisning i dialogen
OG `DictionaryModelBinder`-fiksen begge fungerer korrekt med ekte data,
ikke bare i teorien) → logget inn som pasienten via BankID-mock →
betalingssiden viste riktig produktnavn og samlet pris (200,00 NOK), med
fungerende salgsvilkår-lenke. Vipps-knappen vises ikke lokalt siden
dev-miljøet ikke har ekte Vipps-legitimasjon konfigurert (kun Stripe-
testnøkler) — det er forventet, ikke en feil; Azure test-miljøet har ekte
Vipps-legitimasjon i Key Vault og viser begge betalingsknappene.

### Testkategorier byttet ut med Helsebibliotekets 16 praktiske kategorier (2026-09-11)

Brukeren ga oss `Helsebiblioteket_psykologiske_og_nevropsykologiske_tester.xlsx`
— en kuratert oversikt over 206 skåringsverktøy fra helsebiblioteket.no, med
egne faner "Alle verktøy" (lenker, målgruppe, tilgang/lisensmerknad per
verktøy), "Direkte filer", og "Kategorier" (16 praktiske hovedkategorier
definert for akkurat denne arbeidsboken). Dette skal være kildegrunnlaget
for nye innebygde tester fremover — se også egen seksjon under for de første
åtte testene som bygges fra denne kilden.

`TestService.StandardKategorier` (de syv opprinnelige: Allianse/Angst/
Depresjon/Funksjon/Kjerne/Nevropsykologiske/Utredning — satt sammen ad hoc
under fase 6, ikke fra noen ekstern kilde) er byttet HELT UT med
Helsebibliotekets 16: Kognisjon/demens/nevropsykologisk screening; ADHD,
autisme og nevroutvikling; Søvn og døgnrytme; Rus og avhengighet;
Spiseforstyrrelser og kroppsbilde; Traumer, dissosiasjon og belastninger;
Angst, tvang og relaterte plager; Depresjon og bipolaritet; Psykose og
alvorlige psykiske lidelser; Personlighet, relasjoner og sosial fungering;
Vold, selvmord og risikovurdering; Seksuell helse og kjønn; Barn og unges
psykiske helse – generelt; Funksjon, livskvalitet og behandlingsutfall;
Somatiske symptomer, smerte og utmattelse; Diagnostikk, tverrgående og
øvrige verktøy.

Dette er en REELL bytt-ut, ikke bare et tillegg: `SikreStandardkategorierAsync`
fjernet tidligere kun manglende kategorier, aldri foreldede — utvidet til nå
også å slette enhver `TestKategori` (og dens `TestKategoriKobling`-rader) som
ikke lenger er i listen, kjørt idempotent ved hver oppstart (som før). Verifisert
lokalt: startet appen mot en database med de syv gamle kategoriene fra
tidligere testing, bekreftet via direkte DB-spørring at alle syv ble fjernet
og erstattet med nøyaktig de 16 nye, uten manuell opprydding.

WHO-5 (tidligere i "Kjerne", som ikke lenger finnes) er flyttet til
"Funksjon, livskvalitet og behandlingsutfall" — nærmeste semantiske treff
for en generell trivselsindeks. `BetalingPipelineTests.cs` sin
`PartnerbehandlerKanIkkeTildeleTestUtenforAllowList`-test brukte "Kjerne" som
en vilkårlig testkategori (ikke knyttet til WHO-5 spesifikt) — byttet til
"Diagnostikk, tverrgående og øvrige verktøy". 16/16 tester grønt.

### Åtte nye innebygde tester fra Helsebiblioteket (2026-09-12)

Bygget de første åtte testene fra `Helsebiblioteket_psykologiske_og_nevropsykologiske_tester.xlsx`
etter nøyaktig samme mønster som WHO-5 (egen `IInnebygdTestSeeder` + `ITestSkaaringsberegner` per
test, se `docs/beslutningslogg.md` fase 5): **Cambridge atferdsskala (EQ40), RAADS-R, WURS,
MADRS-S, Forenklet søvnutredningsskjema (SOVN), PHQ-9, IPDS (IOWA), TRAPS I**. Fullstendig
implementasjonsplan (per-test kilde, skala, skåringsformel) ligger i
`C:\Users\gaute\.claude\plans\fizzy-churning-dusk.md`; rå kildeforskning (fullstendig
spørsmålstekst + skåringsgrunnlag hentet direkte fra hver PDF/nettside) i
`C:\Users\gaute\AppData\Local\Temp\claude\test-research\*.md` — begge referert her siden de er
FASIT for hvorfor tekstene/formlene er som de er, ikke gjentatt i sin helhet i denne loggen.

**Reelt, tidligere ukjent hull avdekket og rettet**: `Behandlerportal/Pasienter/Rapport.cshtml.cs`
sin `TestService.BeregnSkaaringAsync` returnerer `null` (→ 404 for behandler) for enhver test UTEN
en registrert `ITestSkaaringsberegner` — dette gjaldt umerket alle åtte, inkludert
søvnskjemaet som i kildedokumentet er et rent kartleggingsskjema UTEN offisiell sumskår. Løst ved
at også søvnskjemaet fikk en egen skåringsklasse (`SovnSkaaringsberegner`) som produserer en enkel,
ikke-klinisk sum av de 14 hovedsymptomene pluss strukturerte indikator-flagg (mulig søvnapné/
narkolepsi-mistanke), tydelig merket som IKKE en validert skår.

**To reelle, tidligere ukjente motor-begrensninger avdekket og rettet i samme runde** (begge
bakoverkompatible, ingen eksisterende tester påvirket):

1. `TestLeddSvaralternativer.Parse` splittet på ALLE komma — brøt sammen for MADRS-S, hvis
   offisielle svartekster selv inneholder komma ("Jeg kjenner meg for det meste nedstemt, men
   iblant kjennes det lettere."). Rettet til å splitte KUN på komma etterfulgt av "tall:" (neste
   par) via en enkel regex — WHO-5 sin skala (ingen interne komma) parses identisk som før.
2. `TestService.BeregnSkaaringAsync` leverte svar til skåringsklassen i databasens tilfeldige
   rekkefølge, ikke garantert lik spørsmålenes faktiske rekkefølge — usynlig for WHO-5 (alle 5 ledd
   teller likt, rekkefølge er irrelevant for en ren sum), men kritisk for PHQ-9 (må ekskludere det
   10. funksjonsspørsmålet) og TRAPS I (må hoppe over del 1 sine 16 ledd og kun skåre del 2 sine
   20). Rettet: svar sorteres nå eksplisitt etter (side.Rekkefolge, ledd.Rekkefolge) før de sendes
   til `ITestSkaaringsberegner.BeregnSkaaring` — nødvendig fordi `TestLedd.Rekkefolge` telles PER
   SIDE, ikke globalt (side 2 sitt ledd 1 ville ellers sortert før side 1 sitt ledd 5 uten å også
   sortere på siden selv).

**To av de åtte (EQ40, WURS) manglet en offisiell skåringsnøkkel i selve det norske
kildedokumentet** — brukeren godkjente eksplisitt (via spørsmål under planlegging) å bruke kjente
internasjonale nøkler i stedet, tydelig merket i kode/rapport som ikke bekreftet av
rettighetshaver for akkurat denne norske oversettelsen:
- **EQ40**: hentet og lest hele Baron-Cohen & Wheelwright (2004) sin originalartikkel (inkl.
  appendix med alle 60 originalitems + offisiell skåringsnøkkel). Verifiserte deretter, ledd for
  ledd, at den norske PDF-ens 40 items er EKSAKT de 40 scorede originalitemene (de 20
  fyll-spørsmålene allerede fjernet), i samme rekkefølge — dvs. retningen (enig-/uenig-keyed) er
  ikke gjettet, men 1:1 innholdsmatchet.
- **WURS**: hentet Ward, Wender & Reimherr (1993) sin offisielle WURS-25-item-liste (25 av de 61)
  og cutoff (46) fra to uavhengige kilder, matchet hvert av de 25 engelske items mot et entydig
  norsk motstykke i den norske 61-item-listen. Alle 25 fant nøyaktig ett treff.

**Ny testfil** `tests/TestBase.IntegrationTests/SkaaringsberegnereTests.cs` (9 nye enhetstester,
25/25 totalt grønt) — dekker spesielt de mest feilutsatte formlene (RAADS-R og EQ40 sin reverserte
skåring, WURS sin 25-av-61-delmengde, PHQ-9 sin ekskluderte 10. spørsmål, TRAPS I sin
del-1-hopping). Dette var FØRSTE gang noen skåringsklasse i prosjektet fikk egen enhetstestdekning
(WHO-5 har aldri hatt det).

**Verifisert**: bygget grønt, 25/25 tester grønt. Full manuell ende-til-ende-gjennomgang av PHQ-9
lokalt (valgt som enkleste struktur uten reversering): dukket opp korrekt under "Depresjon og
bipolaritet" i tildelingstreet → tildelt uten prising (ingen 404/krasj) → fylt ut som pasient med
alle 9 hovedspørsmål satt til "Mer enn halvparten av dagene" (verdi 2) og funksjonsspørsmålet til
en vilkårlig verdi → rapportside viste korrekt Råskår 18/27 (67 %), fortolkning "moderat til
alvorlig" — bekrefter at BÅDE ordrekkefølge-fiksen OG ekskluderingslogikken fungerer korrekt med
ekte data, ikke bare i enhetstestene. De øvrige syv er verifisert via kodegjennomgang +
enhetstestene over, IKKE klikket gjennom fullt ut hver, gitt omfanget (åtte fulle instrumenter i
én runde).

### Bugliste 2026-09-13, gruppe A — sikkerhet/dataintegritet (2026-09-13)

Bruker leverte en 28-punkts bugliste (`bugs20260913.txt`) samlet fra faktisk bruk av test-miljøet.
Planen delte den i fire grupper (A: sikkerhet/dataintegritet, B: slett/arkiver-modell, C: HPR-flyt,
D: resten av UI-punktene), committet/deployet hver for seg — se `C:\Users\gaute\.claude\plans\
fizzy-churning-dusk.md` for den fulle planen. Denne seksjonen dekker gruppe A.

**Punkt 7 (tom personnummer-override) og punkt 1 (logo → localhost) reproduserte IKKE** —
`MockBankIdProvider.AuthenticateAsync` bruker allerede `IsNullOrWhiteSpace` og faller korrekt
tilbake til det faste mock-personnummeret (verifisert live med Playwright: tomt felt → går videre
til 2FA som normalt), og `_Layout.cshtml` sin logo-lenke er allerede en relativ `"/"`-lenke (ingen
localhost-forekomst funnet noe sted i kodebasen). Ingen kodeendring gjort for disse to — trolig
en forveksling med en ELDRE, allerede rettet feil (jf. `docs/beslutningslogg.md` sine tidligere
StagingGate/PersonnummerOverride-notater), eller et miljøspesifikt avvik som ikke reproduserte her.

**Punkt 21 — reell sikkerhets-/dataintegritetsfeil, rettet**: `Behandlerportal/Pasienter/
Detaljer.cshtml` hadde en "Tildel ny test"-miniform som postet rett til `TestService.TildelAsync`
— denne veien hoppet forbi BÅDE prising (`TestTildelingsService`/`TestPrisberegner`) OG varsling
(SMS/e-post er kablet inn i `TestTildelingsService`, ikke `TestService`). Fjernet miniformen helt;
erstattet med en lenke (`OnGetTildelAsync`) som setter samme `TempData["TildelPasientIder"]`-nøkkel
som steg 1 i den vanlige tildelingswizarden bruker, og sender behandler rett til
`Tildel/Tester`-siden med pasienten forhåndsvalgt — all tildeling går nå uunngåelig gjennom den
ekte wizarden. Verifisert live med Playwright: lenken fra pasientdetaljsiden lander på "Tildeler
til: Vipps Demo" i wizarden. `HeleFlytenTests.cs` måtte oppdateres til å drive denne nye flyten
(poste til `Tildel/Tester?handler=Send` i stedet for det gamle skjemaet) — avdekket samtidig en
ekte forbedring: fordi wizarden nå faktisk sender en varsel-SMS til pasienten, måtte testen hente
registreringsinvitasjonens SMS-token RETT ETTER pasientopprettelsen i stedet for etter tildelingen
(ellers overskrev den nye varsel-SMS-en "siste melding" og skjulte inviterings-tokenet) — og
audit-logg-handlingen endret navn fra `TildelTest` til `TildelTesterBatch` (batch-tildeling er nå
eneste vei). 25/25 tester grønt etter oppdateringen.

**Punkt 23 (ingen e-post mottatt) — rotårsak diagnostisert**: fryktet først manglende
`Acs:ConnectionString`/`Email:SenderAddress`-konfigurasjon i Azure (siden `azd env get-values`
ikke lister noen ACS-relatert variabel) — men `infra/resources.bicep` viser at
`Microsoft.Communication/communicationServices` og tilhørende e-postdomene er EKTE, alltid-
provisjonerte ressurser der tilkoblingsstrengen hentes direkte fra `communicationService.
listKeys()` og lagres i Key Vault (`Acs__ConnectionString`/`Email__SenderAddress` i `appSettings`),
IKKE fra en azd-miljøvariabel — konfigurasjonen er altså allerede korrekt og krever ingen fiks. Én
reell, funnet forsendelse (en "Invitasjon til PsyTest"-e-post i en test-postkasse) bekrefter at
ACS-utsendelse faktisk virker. Mest sannsynlige rotårsak for brukerens opplevde "aldri mottatt
mail" er dermed punkt 21 sin bug alene (tildeling via snarveien sendte ALDRI noe varsel, uansett
Varslingspreferanse) — løst av samme fiks som over.

**Punkt 19 — PNR sanity-sjekk**: ny `PersonnummerValidator.ErGyldigFormat` (`TestBase.Shared/
Domain/PersonnummerValidator.cs`) — ren formatsjekk (nøyaktig 11 sifre, kun tall), IKKE en MOD11-
kontrollsifferalgoritme (ikke bedt om). Lagt til i alle skjemahandlere som tar imot et
personnummer direkte fra bruker: `Administratorer/Ny`+`Rediger`, `Admin/Behandlere/Rediger`,
`Behandlerportal/Pasienter/Rediger`, `Inviter/Fullfor`, `PasientRegistrering/Fullfor`. Bevisst
UTELATT fra `Behandlerportal/Pasienter/Ny` — det skjemaet fjernes helt i gruppe D (behandler skal
ikke lenger fylle inn PNR ved oppretting av pasient, kun ved BankID-innlogging). Verifisert live
med Playwright: `Personnummer=123` → "Personnummer må bestå av nøyaktig 11 siffer.", gyldig verdi
→ lagres som normalt.

### Produksjonsutfall: krasj ved oppstart pga. arkivert seed-admin-konto (2026-09-13)

Oppdaget ved rutinemessig `/health`-sjekk etter gruppe A-deployen over: `www.psytest.no` svarte
503 etter to påfølgende `azd deploy web`-kjøringer (begge med den kjente "ingen App Service
deployment status change"-advarselen, se CLAUDE.md — denne gangen var det IKKE bare kosmetisk).
`az webapp log download` + gjennomsøk av `StartupLogs/*_failure.log` viste roten:
`MySqlConnector.MySqlException: Duplicate entry 'gaute-godager' for key
'administratorer.IX_administratorer_AdminId'` — et `Unhandled exception` ved HVER oppstart, altså
en total, vedvarende nedetid, ikke en forbigående feil.

**Rotårsak**: seed-av-brukerens-egen-admin-blokken i `Program.cs` (se "Seed av brukerens egen
admin-konto") slo kun opp eksisterende konto via `AdminAuthenticationService.
FinnVedPersonnummerAsync`, som eksplisitt EKSKLUDERER arkiverte administratorer
(`Where(a => !a.ErArkivert)`). Kontoen "gaute-godager" var på et tidspunkt blitt arkivert (hvordan/
når er ikke sporet — ingen kodeendring denne økten rørte administrator-arkivering) — oppslaget
fant derfor ingenting, og seed-logikken prøvde å OPPRETTE en ny rad med samme utledede `AdminId`
("gaute-godager", fra `Seed:AdminNavn`), som krasjet på den unike indeksen. Dette var en LATENT
bug uavhengig av denne øktens bugliste-arbeid — den ville krasjet ved enhver fremtidig omstart
fra det øyeblikket kontoen ble arkivert, og tilfeldigvis først synlig nå fordi App Service ikke
hadde restartet siden den datoen.

**Fiks**: seed-blokken faller nå tilbake til å slå opp kontoen via `AdminId` (uavhengig av
arkiveringsstatus) hvis personnummer-oppslaget ikke finner noe, og selvhelbreder den (nullstiller
`ErArkivert`/`ArkivertUtc`, retter opp `Personnummer` til konfigurert verdi) i stedet for å prøve å
opprette en duplikat — samme "selvhelbredende"-prinsipp koden allerede brukte for
`ErSuperadmin`-flagget rett under. Verifisert: 25/25 tester fortsatt grønt, `dotnet build` rent.
**Lærdom for videre arbeid**: enhver fremtidig admin-arkiveringsfunksjon (bl.a. gruppe B/D i denne
buglisten — ekte slett-knapp for administratorer) MÅ eksplisitt hindre at seed-admin-kontoen
(identifisert ved `Seed:AdminPersonnummer`) noensinne kan arkiveres/slettes via UI, ikke bare stole
på at oppstarts-seedingen henter den tilbake — en admin som arkiverer/sletter denne kontoen ved et
uhell bør få en tydelig feilmelding, ikke en stille handling som først krasjer appen ved neste omstart.

### Bugliste 2026-09-13, gruppe B — slett/arkiver-modell + selvsletting (2026-09-13)

Gruppe B av `bugs20260913.txt` (se gruppe A over for full kontekst/plan-referanse) — punktene 3, 4,
6, 17 og 18: ekte sletting (ikke bare arkivering) for Administrator/Partner/Behandler/Pasient,
bekreftelse+captcha på sletting, og selvbetjent kontosletting.

**Datamodell**: ny migrasjon `LeggTilSlettetPaaFireEntiteter` legger til `ErSlettet`
(bool)/`SlettetUtc` (nullable) på alle fire entiteter — et ANDRE nivå under eksisterende
arkivering (`ErArkivert`/`Status==Arkivert`), ikke en erstatning. Slett-knappen er kun
aktiv/synlig når raden allerede er arkivert (håndhevet BÅDE i UI — grået ut via
`disabled="@(!x.ErArkivert)"` — OG server-side i selve handleren, uavhengig av UI-tilstand).
Sletting skjuler raden fra alle andre enn Superadmin (`WHERE !ErSlettet` i standard-spørringen);
Superadmin får en `?visSlettede=true`-visning (Administratorer, Behandlere, Admin/Pasienter — den
sistnevnte siden Pasient-sletting selv skjer på den behandler-eide `Behandlerportal/Pasienter`,
mens "vis alt + gjenopprett" hører hjemme på admin-oversikten Superadmin allerede bruker) med en
grønn "Gjenopprett"-knapp (`_Ikon.cshtml` fikk et nytt `"slett"`-ikon også) som nullstiller BÅDE
slettet- og arkivert-status i ett steg.

**Sikkerhetssperre direkte utløst av forrige seksjons produksjonsutfall**: en administrator med
`ErSuperadmin==true` kan verken arkiveres ELLER slettes via `Administratorer/Index.cshtml.cs` —
lagt til som en eksplisitt kodesperre (ikke bare et håp om at oppstartsseedingen henter kontoen
tilbake), nøyaktig lærdommen fra "Produksjonsutfall"-saken over.

**Bekreftelse + captcha (punkt 17)**: gjenbruker det eksisterende `ICaptchaProvider`/
`MockCaptchaProvider` fra innloggingssidene (samme enkle regnestykke-mønster, DataProtection-
signert fasit tur-retur i et skjult felt) i stedet for et nytt tredjeparts-bibliotek. Ny delt
JS-funksjon `bekreftSletting(form, hvaSlettes)` i `wwwroot/js/validering.js`: `confirm()` først,
så `prompt()` for regnestykke-svaret, satt inn i et skjult `CaptchaSvar`-felt før faktisk innsending.

**Reell bug funnet og rettet UNDERVEIS, ikke bare i teorien**: første Playwright-verifisering av
selve slette-knappen feilet gjentatte ganger med "Feil svar på sikkerhetsspørsmålet" SELV MED
korrekt uthentet og riktig utregnet svar. Rotårsak: i feilhåndteringsgrenen ble en FERSK
`_captcha.LagUtfordring()` generert via `OnGetAsync(...)` og tilordnet
`CaptchaSporsmal`/`CaptchaSignertFasit` — men Razors `asp-for`-taghjelper gjengir som kjent en
POSTET verdi fra `ModelState` FREMFOR den gjeldende C#-modellverdien når ModelState allerede har en
oppføring for feltnavnet (her: `CaptchaSignertFasit`, bundet fra forrige innsending). Resultatet:
det synlige spørsmålet oppdaterte seg (fra modellverdien via `data-captcha-sporsmal`), mens det
skjulte, faktiske signerte svaret som ble sendt inn IKKE gjorde det — et korrekt svar på det NYE
spørsmålet ble dermed alltid verifisert mot det GAMLE signerte svaret, og feilet alltid. Fikset med
`ModelState.Clear()` rett før `OnGetAsync(...)`-kallet i alle sju berørte feilhåndteringsgrener
(Administratorer/Partnere/Behandlere-listene, Behandlerportal/Pasienter-listen, samt de tre
selvslett-sidene under). Dette er en generell ASP.NET Core Razor Pages-fallgruve — enhver fremtidig
side som gjenoppfrisker en `[BindProperty]`-verdi inne i SAMME POST-handler (ikke via redirect) må
huske `ModelState.Clear()` først, ellers vinner alltid den opprinnelig posted verdien i viewet.

**Selvsletting (punkt 6)**: ny handling "Slett min konto" for behandler
(`Behandlerportal/Innstillinger.cshtml`, siden som fantes fra før), pasient (`Pasientportal/
MinSide.cshtml`) og en helt ny, minimal admin-side (`Admin/MinKonto.cshtml` — admin hadde ingen
selvbetjeningsside fra før). Alle tre går RETT til fullt slettet (arkivert+slettet i ett steg,
ikke topartssteget admin ellers bruker) siden brukeren selv ber om å forsvinne umiddelbart, logger
ut med `SignOutAsync` etterpå, og forblir synlig for Superadmin med samme gjenopprettingsvei.
Admin-varianten nekter eksplisitt Superadmin-kontoen å slette seg selv (samme sperre som over).

**Verifisert**: 25/25 tester grønt, `dotnet build` rent. Full Playwright-gjennomgang av hele
slett/arkiver/gjenopprett-syklusen for Administrator (inkl. selve bug-jakten over — captcha-feilen
ble funnet og bekreftet rettet live, ikke bare i kode), og full ende-til-ende selvsletting av en
ekte (dev-only) administratorkonto (logget ut, kontoen borte fra standardvisningen etterpå). De
øvrige tre entitetenes slett/gjenopprett-handlere (Partner, Behandler, Pasient) og selvsletting for
behandler/pasient bruker identisk, allerede verifisert kode-mønster og er verifisert ved
kodegjennomgang + enhetstestsuiten, ikke hver for seg klikket gjennom fullt ut — samme åpenhet om
omfang som tidligere runder.

### Bugliste 2026-09-13, gruppe C — HPR-flyt (2026-09-13)

Gruppe C av `bugs20260913.txt` — punktene 9, 10 og 11: standard HPR-prøveperiode til 21 dager, en
"utvid fristen"-knapp, og HPR-godkjenning som en ekte oppgave.

**Samlet i én kilde**: ny `HprPolicy`-klasse (`TestBase.Shared/Domain/Administrasjon/HprPolicy.cs`)
med `ProveperiodeDager = 21` og `ForlengelseDager = 21`, og `BeregnFrist`/`ErUtlopt` som eneste sted
frist-logikken skjer. Erstatter tre tidligere uavhengige hardkodinger av "7 dager"
(`Pasienter/Ny.cshtml.cs`, `Gruppeimport.cshtml.cs`, en visningstekst i `Admin/Behandlere/Index.cshtml`).

**Forlengelse**: nytt felt `Behandler.HprForlengetTilUtc` (migrasjon `LeggTilHprForlengetTilUtc`) —
`HprPolicy.BeregnFrist` bruker den faktiske fristen ELLER forlengelsen, whichever er senere. Ny
admin-handler `OnPostForlengHprAsync` (`Admin/Behandlere/Index.cshtml.cs`) kan kun kjøres når
`HprForlengetTilUtc is null`, altså kun én gang per behandler — akkurat som bug-teksten spesifiserte
("resetter fristen en gang, typisk behov i ferier").

**HPR-godkjenning som oppgave**: `Admin/Oppgaver.cshtml(.cs)`, tidligere en tom placeholder, fylles
nå med behandlere der `HprPolicy.ErUtlopt(...)` er sann — samme "Godkjenn HPR"-handler som allerede
fantes på `Admin/Behandlere/Index`. Siden `ErPartnerAdministrator` er en claim på `Behandler`, ikke
en egen rolle/side (jf. CLAUDE.md), fikk partner-admin i stedet en filtrert utvidelse av SIN
eksisterende `Behandlerportal/Oppgaver`-side — samme spørring, men begrenset til behandlere i eget
partnerskap (`PartnerId`-match), med sin egen `OnPostGodkjennHprAsync` (bevisst IKKE satt
`HprGodkjentAvAdministratorId`, siden en partner-admin er en `Behandler` og ikke en `Administrator`
— hvem som godkjente står uansett i audit-loggen). "Godkjenning fjerner oppgaven for alle" virker
automatisk uten noe eget arbeid, siden oppgavelisten er en ren spørring mot delt DB-tilstand
(`HprGodkjent`), ikke noe lokalt per admin-økt.

**Verifisert live, ende-til-ende**: backdatert en test-behandlers `RegistrertUtc` lokalt til å være
utløpt under det nye 21-dagersvinduet → bekreftet "Venter — frist [dato]" og en aktiv "Utvid
fristen med 21 dager"-knapp dukket opp på `Admin/Behandlere` → bekreftet SAMME behandler dukket opp
som en rad under "HPR-godkjenning utløpt" på `Admin/Oppgaver` → klikket "Godkjenn HPR" der →
bekreftet oppgaven forsvant helt ("Ingen oppgaver akkurat nå."). 25/25 tester grønt, `dotnet build`
rent. Partner-admin sin filtrerte visning på `Behandlerportal/Oppgaver` er identisk kode-mønster,
verifisert ved kodegjennomgang (krever et ekte partnerskap med en behandler med utløpt frist for en
fullt egen klikk-gjennomgang, vurdert som lav ekstra risiko gitt at spørringen er nesten identisk
til den allerede klikk-verifiserte admin-siden).

### Bugliste 2026-09-13, gruppe D del 1 — fargeskjema, favicon, prising, PNR-frie invitasjoner, partner-selvbetjening (2026-09-13)

Første del av gruppe D (resten av `bugs20260913.txt`) — punktene 2, 8, 12, 13, 14, 15, 16, 20, 24.

**Fargeskjema per rolle (punkt 2)**: `<body>` får nå en `rolle-*`-klasse fra `_Layout.cshtml`
basert på `ICurrentUserContext.Role` (Superadmin/Administrator/Behandler/Pasient — `Utvikler` og
uinnlogget faller tilbake til standard oransje). `site.css` sine `--rolle-accent`/`-dark`/`-tekst`-
variabler styrer `.btn-accent` og `.funksjonsknapp` (nav-knappene) — Superadmin lilla (#8b5cf6),
Administrator blå (#3b82f6), Behandler teal (#14b8a6), Pasient beholder dagens varme oransje som
allerede etablert merkevarefarge. Verifisert live for alle fire via "Bytt modus" (dev-only rollebytte).

**Ekte bug funnet og rettet mens dette ble verifisert**: `site.css` hadde ALDRI hatt noen
cache-busting/versjonering — `<link href="~/css/site.css">` uten `asp-append-version`. Under
verifisering serverte den samme, lenge kjørende nettleserøkten en TIMEVIS gammel, cachet kopi av
filen (bekreftet med `document.styleSheets[0].cssRules` — mangler helt de nye reglene — mens en
direkte `curl`/`fetch(..., {cache:'no-store'})` samtidig hentet korrekt, fersk fil fra samme
server). Dette er ikke bare et testartefakt: uten cache-busting kan ECHTE brukeres nettlesere på
samme vis holde på en utdatert `site.css` en god stund etter enhver fremtidig deploy som endrer
CSS, uavhengig av denne økten. Fikset ved å legge `asp-append-version="true"` på `site.css` og
`validering.js` i `_Layout.cshtml`, samt de fem andre side-spesifikke skriptene
(`tabellfilter.js`, `tildel.js`, `rapport.js`) — ASP.NET Cores innebygde taghjelper legger
automatisk til en innholds-hash (`?v=...`) som endrer seg hver gang filen endres.

**Favicon (punkt 20)**: `wwwroot/favicon.svg` — sort bakgrunn, "PT" i samme oransje som
`--accent`, referert med `<link rel="icon" type="image/svg+xml">`.

**MADRS-S stablet svarlayout (punkt 24)**: ny modifier-klasse `.svar-rad--stablet` (vertikal,
innrykket) i `site.css`, satt betinget i `Fyll.cshtml` kun når `Test.Kode == "madrs_s"` — andre
testers kortere Likert-alternativer beholder radlayouten uendret.

**"Ferdigstill og videre til neste test" (punkt 8)**: `Fyll.cshtml.cs` sin ferdig-visning
(`ErFullfort`) slår nå opp pasientens neste ikke-fullførte tildeling
(`HentTildelingerForPasientAsync`, samme kilde som MinSide) og viser en lenke rett dit når det
finnes en.

**Prising — én Lagre-knapp for alle rader (punkt 12)**: både `Admin/Tester/Prising/Index` og
`Behandlerportal/MinPartner/Prising` bygget om fra N uavhengige per-rad-skjemaer (som gjorde at et
"Lagre"-klikk på én rad forkastet ulagrede tall i alle andre) til ETT skjema med navngitte felt
(`MinstePrisKr[testId]` osv.), lest manuelt fra `Request.Form` — samme "Dictionary-felt kan komme
tomt"-fallgruve som `LesHonorarFraSkjema()` i Tildel-wizarden allerede løste, gjenbrukt her.

**Pasient-PNR ikke lenger påkrevd ved "Legg til" (punkt 13)**: `Pasient.Personnummer` er nå
`string?` (migrasjon `GjorPasientPersonnummerValgfritt`, samme nullable-mønster som allerede brukt
for `Behandler.Personnummer`) — `Behandlerportal/Pasienter/Ny` krever bare mobil+e-post; pasienten
oppgir selv personnummer via den EKSISTERENDE invitasjonslenken (`FullforRegistreringAsync`, uendret).
Undersøkte "alle andre invitasjoner" også — `BehandlerInvitasjonService.InviterAsync` (både
Admin- og Behandlerportal sin "Inviter kollega") krevde ALDRI personnummer ved selve invitasjonen
i utgangspunktet (kun mobil/e-post, personnummer samles inn av behandleren selv via
`FullforProfilAsync`), så ingen endring trengtes der. `Gruppeimport` sitt CSV-format
("gruppenavn,navn,epost,mobil,pnr") er bevisst UENDRET — et etablert bulkformat, ikke eksplisitt
del av denne bug-meldingen.

**Partner-selvbetjening (punkt 14, 15)**: `MinPartner/Behandlere.cshtml(.cs)` fikk (a) en
partner-oversikt øverst (navn/kontaktperson/kontakt-info/abonnement + liste over tester partneren
har fått tilgang til, `PartnerTestTilgang`), og (b) fjernet selvsperren i `OnPostFjernAsync` — en
partner-admin kan nå fjerne seg selv fra partneren (logges automatisk ut etterpå, siden
`PartnerId`/`ErPartnerAdministrator` er innloggingscookie-claims som ellers ville vært utdaterte
resten av økten).

**Partnere-knapper (punkt 16)**: Rediger/Tester-knappene på `Admin/Partnere` (allerede gjort om til
ikon-knapper i gruppe B) er nå bevisst ORANSJE (`.btn-icon--partner`, ny CSS-klasse) i stedet for
den innloggede Superadmin-ens egen lilla rollefarge — disse handlingene gjelder PARTNEREN på
raden, ikke brukeren selv, se design-begrunnelsen i planen.

**Verifisert**: 25/25 tester grønt (to eksisterende tester måtte oppdateres for den nye PNR-frie
flyten — se `HeleFlytenTests.cs`/`RedigerTests.cs`), `dotnet build` rent. Fargeskjema klikk-testet
for alle fire roller. Favicon, prising-enkeltknapp og MADRS-S-layout verifisert ved kodegjennomgang
+ testsuiten; partner-selvbetjening verifisert ved kodegjennomgang (krever et ekte partnerskap med
minst to behandlere for en full klikk-gjennomgang).

### Bugliste 2026-09-13, gruppe D del 2 — dobbelklikk-vern, oppgave/min-side-sammenslåing, kategori-fargekoding, IPDS-klyngeindikatorer (2026-09-13)

Siste del av bug-listen — punktene 22, 25, 26, 27, 28 (punkt 5 er en ren praksis-bekreftelse,
ingen kodeendring, se planen).

**Dobbeltklikk-vern (punkt 25)**: nytt `data-disable-on-submit="<tekst>"`-attributt + en
capture-phase `submit`-lytter i `wwwroot/js/validering.js` (allerede lastet globalt) — disabler
submit-knappen og bytter teksten til den oppgitte "…"-teksten straks et gyldig skjema sendes inn,
for å hindre dobbel innsending av trege/ikke-idempotente handlinger (SMS/e-post-utsendelse,
kontoopprettelse, godkjenning). Lagt på: Godkjenn/Send-kopi i `Rapport.cshtml`, "Legg til
pasient", "Inviter kollega" (begge Areas), "Legg til partner", Tildel-wizardens "Bekreft og send".
Bevisst IKKE lagt på "Forkast"-knappen i `Rapport.cshtml` — den har allerede en konkurrerende
inline `onsubmit="return confirm(...)"`; de to ville kollidert (capture-phase-lytteren ville
disablet knappen før `confirm()` i det hele tatt rakk å svare, og latt den forbli disablet for
godt hvis brukeren trykket Avbryt). Verifisert LIVE: Godkjenn-knappen ble bekreftet `disabled`
med teksten "Godkjenner …" umiddelbart ved klikk.

**"Neste oppgave"-knapp (punkt 26)**: `Rapport.cshtml.cs` slår etter en vellykket godkjenning opp
neste ugodkjente/fullførte rapport i behandlerens kø (`HentUgodkjenteFullforteForBehandlerAsync`)
og viser en lenke direkte dit. Verifisert LIVE (korrekt fravær av knappen når køen var tom etter
den ene godkjenningen som ble gjort i denne økten).

**Rapport-fargekoding per kategori (punkt 27)**: ny `TestKategoriFarge`-klasse i
`TestBase.Shared/Domain/Tester/` — en FAST, forhåndsvalgt (ikke kjøretids-hashet) tabell fra alle
16 Helsebiblioteket-kategorinavn til en CSS-slug/farge (`kategori-depresjon` osv.), for å
garantere stabile farger som ikke endrer seg ved en ombygging. `Rapport.cshtml.cs` slår opp
testens primærkategori (`TestService.HentPrimaerKategoriNavnAsync`, ny metode) og
`Rapport.cshtml`/`site.css` viser en farget stripe + kategorinavn på rapport-headeren. Verifisert
LIVE på en ekte PHQ-9-rapport: `rapport-tittelblokk` fikk klassen `kategori-depresjon`, blå farge
(`#3b82f6`) og teksten "Depresjon og bipolaritet".

**IPDS personlighetsforstyrrelse-klynger per ledd (punkt 28)**: `IpdsSkaaringsberegner.cs`
utvidet med en ledd→klynge-nøkkel og en indikator-generator som grupperer alle "Ja"-besvarte ledd
per klynge og legger til `TestSkaaringIndikator("Mulig personlighetsklynge (uverifisert)",
"Indikerer: <klynge> – Ledd <n,m>", Positiv: false)` for enhver klynge med minst ett Ja-svar.
**Kilde, verifisert (samme rigor som EQ40/WURS-fotnotene tidligere denne uken)**: Pfohl/Langbehns
originalvalideringsartikkel "A cross-sectional testing of The Iowa Personality Disorder Screen in
a psychiatric outpatient setting" (PMC3151206) sin Tabell 1 gir original engelsk itemtekst +
tilhørende DSM-IV-personlighetsforstyrrelse for alle 11 ledd. Hvert av de 11 norske leddene i
`IpdsTestSeeder.cs` ble innholdssammenlignet mot denne tabellen i SAMME rekkefølge — perfekt 1:1-
match, ikke en gjetning: ledd 1+7 (humørsvingninger/ustabilt selvbilde) → Emosjonelt ustabil PF
(borderline), 2+3 (oppmerksomhet/impulsivitet) → Histrionisk PF, 4+9+10 (mistillit/mistenksomhet/
nag) → Paranoid PF, 5+6 (sosial angst/unngåelse) → Unnvikende PF, 8+11 (grandiositet/mangel på
empati) → Narsissistisk PF. Klart merket "uverifisert" i selve indikatorteksten siden koblingen er
litteraturbasert sekundærkilde, ikke en offisiell skåringsnøkkel fra rettighetshaver. Verifisert
LIVE ende-til-ende: fylte ut en ekte IPDS-besvarelse som pasient (Ja på ledd 1 og 7, Nei på resten)
og bekreftet at rapporten som behandler viste nøyaktig "Indikerer: Emosjonelt ustabil PF
(borderline type) – Ledd 1,7" — i tillegg til den eksisterende enhetstesten
`Ipds_Grupperer_JaBesvarteLedd_PerPersonlighetsklynge`.

**Oppgaver → Min side, én samlet personlig side per rolle (punkt 22)**: de tre separate
`Oppgaver.cshtml`-sidene (Admin/Behandlerportal/Pasientportal) er SLETTET. Innholdet er flyttet inn
i hver rolles "Min side":
- **Pasientportal**: `MinSide.cshtml` fikk et oppgave-antall som overskrifts-badge + en
  oppsummeringslinje ("Du har N test(er) som venter på svar") — praktisk talt en sammenslåing av
  to sider som allerede overlappet nesten helt.
- **Behandlerportal**: `MinSide.cshtml(.cs)` (tidligere kun meldingsinnboks) er utvidet med
  seksjonene fra den gamle Oppgaver-siden — utløpte HPR-frister for kolleger (kun synlig for
  partner-admin, filtrert til egne behandlere), "Venter på godkjenning", og "Ikke besvart ennå" —
  i tillegg til den eksisterende meldingsinnboksen. Ny `AntallOppgaver`-egenskap driver
  overskrifts-badgen.
- **Admin**: fikk sin FØRSTE "Min side" (`Areas/Admin/Pages/MinSide.cshtml(.cs)`, ny fil, ikke å
  forveksle med den allerede eksisterende selvsletting-siden `MinKonto.cshtml` fra gruppe B) — viser
  utløpte HPR-godkjenningsoppgaver, samme innhold som den slettede `Admin/Oppgaver`.
- `_Layout.cshtml` mistet alle tre separate "Oppgaver"-navigasjonslenker; varselbadgene
  (uleste meldinger, ubesvarte tester) flyttet over til de respektive "Min side"-lenkene.
- `PaaminnelseService`s daglige påminnelse-e-post/SMS til behandler peker nå til
  `/Behandlerportal/MinSide` i stedet for den slettede `/Behandlerportal/Oppgaver`.
- Verifisert LIVE: både `/Admin/MinSide` og `/Behandlerportal/MinSide` bekreftet å rendre riktig
  sammenslått innhold og korrekt navigasjon via full tilgjengelighetstre-snapshot.

**Verifisert**: 26/26 tester grønt (25 eksisterende + ny
`Ipds_Grupperer_JaBesvarteLedd_PerPersonlighetsklynge`), `dotnet build` rent. Dobbeltklikk-vern,
"Neste oppgave", kategori-fargekoding, IPDS-klyngeindikator og oppgave/min-side-sammenslåingen for
alle tre roller er alle klikk-testet live i tillegg til automatiserte tester — ingen gjenstående
kun-kodeverifiserte punkter i denne gruppen.

Med dette er alle 28 punktene i `bugs20260913.txt` gjennomført (gruppe A/B/C/D), committet, pushet
og deployet til psytest.no.

### Bugliste 2026-09-13, andre runde — fargekoding for Utvikler-dobbeltnav, prisingstabell-overflow, per-test-honorar, SMS-gebyr-transparens, stille skjema-blokkering, lovtekst ved pasient-selvsletting (2026-09-13)

Brukeren oppdaterte `bugs20260913.txt` med seks nye punkter funnet under reell bruk på psytest.no
(vedlagt skjermbilder `buttonrow.png`/`prising.png`/`treatmentprice.png`/`sendout.png`).

**Funksjonsnav farget etter EGEN seksjon, ikke innlogget brukers ene rolle (punkt 1+2)**: en
Utvikler-konto (som `dev-admin`) ser BÅDE admin- og behandler-seksjonen av funksjonsnavet samtidig
(`erAdmin`/`erBehandler` er OR'et med Utvikler, se `_Layout.cshtml`), men `CurrentUser.Role` for en
slik konto er `Utvikler` — som ikke matcher noen `rolle-*`-klasse og dermed falt tilbake til
standard-oransje for ALLE 13 knapper uansett hvilken seksjon de tilhørte. Dette så ut som
"samme knapp om og om igjen i feil farge" og et rotete, ujevnt linjeskift. Løst med en ny
`.funksjonsnav-seksjon--{admin,behandler,pasient}`-klasse (CSS custom property-scoping via
`display:contents`, se `site.css`) som farger HVER seksjon etter sin EGEN rolle uavhengig av
hvem som er innlogget — pluss et betinget suffiks ("Min side (admin)"/"(behandler)"/"(pasient)")
når mer enn én seksjon vises samtidig, slik at duplikatene også blir SKILT tekstlig. Verifisert
live: admin-seksjonen blå, behandler-seksjonen teal, pasient-seksjonen oransje, alle tre distinkt
merket, ingen endring for en normal ett-rolle-bruker (ingen suffiks, kun én farge).

**Prisingstabellen tvang frem en unødvendig horisontal scrollbar (punkt 3)**: rotårsak var IKKE
tabellen selv, men en generisk kort-styling-regel (`main.page form:not([style*="display:inline"])
{ max-width: 640px; ... }`) ment for vanlige ett-kolonnes skjemaer (f.eks. "Ny administrator") som
også trykket prisingsskjemaets BREDE 5-kolonners tabell ned i 640px — tabellens egen
`overflow-x: auto` fikk dermed langt mindre plass enn nødvendig. Løst med en ny opt-in
`.skjema-bred`-klasse (`max-width: 100%`) lagt på nettopp disse to skjemaene
(`Admin/Tester/Prising`, `Behandlerportal/MinPartner/Prising`) — ingen andre skjemaer i appen
endret. I tillegg: `.tall-input` smalnet fra 6rem til 4.5rem, redundant " NOK"-tekst fjernet fra
hver celle (står allerede i kolonneoverskriften), og testnavn-kolonnen fikk lov til å BRYTE linje
(`.prising-tabell td:first-child { white-space: normal }`) i stedet for tvunget on-linje som
resten av appens tabeller. Verifisert live: alle 4 tallkolonner synlige uten scrollbar på en
1280px bredde.

**"Fikk bare sette honorar én gang per kategori" (punkt 4) — ikke en kodefeil**: kildekoden
(`Tildel/Tester.cshtml`) rendrer allerede ETT eget honorarfelt PER TEST (`id="honorar-@test.Id"`),
ikke ett per kategori — bekreftet ved kodelesning OG live (krysset av RAADS-R+WURS samtidig i
samme kategori, fikk to uavhengige honorarfelt). Det brukeren faktisk observerte var at de 8 nye
testene fra Helsebiblioteket (RAADS-R, WURS, IPDS, MADRS-S, PHQ-9, TRAPS I, Cambridge, søvn) alle
sto med `StorstePrisKr = 0` i databasen (kun WHO-5 hadde reell prising) — koden viser bevisst
INGEN honorarfelt når maksprisen er 0 (`@if (test.StorstePrisKr > 0)`, en gratis test kan ikke gi
honorar). Med kun COIN test priset, så det ut som "bare én test i kategorien fikk et felt". Dette
løser seg av seg selv når punkt 3 sin prisingsside faktisk brukes til å prise de resterende 7
testene — ingen egen kodeendring for punkt 4 utover prisingsside-fiksen.

**SMS-gebyr gjort transparent i tildelingsdialogen (punkt 5, tolket som usikkerhet snarere enn en
konkret feil — "isnt it? ... is this too confusing?")**: `Priser:SmsGebyrKr` er ikke konfigurert i
noe miljø ennå (faller til 0 via `TestTildelingsService.HentSmsGebyrKr()`), så total pris endret
seg reelt IKKE ved bytte mellom SMS/E-post/Begge — koden regner riktig, men det var usynlig FOR
HVORFOR ingenting skjedde. Løst uten å dikte opp en forretningspris: SMS/Begge-radioknappene i
`Tildel/Tester.cshtml` viser nå det FAKTISK konfigurerte gebyret direkte i etiketten
("SMS (+X NOK gebyr)") når `SmsGebyrKr > 0`, og hint-teksten under fieldsettet vises kun når det
faktisk finnes et gebyr å forklare. Med dagens `SmsGebyrKr = 0` er oppførselen uendret (ingen
gebyr å vise), men blir selvforklarende automatisk den dagen en reell SMS-pris konfigureres — det
er en forretningsbeslutning utenfor denne rettingens scope, IKKE satt av meg.

**Reell funnet bug: "Bekreft og send"-knappen gjorde ingenting (punkt "sendout") — stille HTML5-
valideringsblokkering bak den åpne dialogen**: `TesterModel.OnGetAsync` (Behandlerportal) fylte
honorarfeltet med behandlerens SIST BRUKTE honorar for denne testen
(`TestService.HentSisteHonorarAsync`) UTEN å klemme det til testens NÅVÆRENDE `StorstePrisKr`. Når
en admin senere SENKET maksprisen (f.eks. fra 5 til 2, se `treatmentprice.png`/`prising.png`), ble
et gammelt, nå for høyt honorar forhåndsutfylt i et `<input max="2">`-felt — HTML5 sin innebygde
range-validering gjør feltet stille "invalid" og BLOKKERER hele skjemainnsendingen uten synlig
feilmelding, fordi feltet selv ligger bak den åpne, modale oppsummerings-dialogen (brukeren ser
aldri hvorfor). Dette er nøyaktig symptomet i `sendout.png` (verdi "5" i et felt merket
"maks 2.00"). Fikset i `Tester.cshtml.cs` sin `OnGetAsync`: `sisteHonorar[test.Id] =
Math.Min(sistBrukt, test.StorstePrisKr)` når `StorstePrisKr > 0`. **Reprodusert og verifisert
live, end-to-end**: tildelte WURS med honorar 140 (da maks var 150) → senket WURS sin maks til 50
via Prising-siden → lastet tildelingssiden på nytt og bekreftet at det forhåndsutfylte
honorarfeltet nå viste 50 (klemt) med `validity.rangeOverflow === false` (ville vært `true` med
140 uten fiksen) → sendte inn skjemaet på nytt og fikk en vellykket "Tildeling fullført" i stedet
for en stille, uforklarlig ikke-respons.

**Ny lovpålagt tekst ved pasient-selvsletting (siste punkt)**: `Pasientportal/MinSide.cshtml` sin
"Slett min konto"-seksjon bytter ut den generiske arkiverings-teksten fra gruppe B (2026-09-13,
tidligere runde) med brukerens eksakte ordlyd om journalloven, 10-års oppbevaringsplikt,
behandlers fortsatte tilgang og Superadmin sin gjenåpningsmulighet — kun for PASIENT-selvsletting
(admin/behandler sine tilsvarende sider er urørt, siden "journal" kun gjelder pasientdata). Lagt
til en ny `#journalforing`-seksjon på den offentlige `/personvern`-siden som "[link]"-plassholderen
peker til, med en kort forklaring av regelverket og en eksplisitt merknad om at mer juridisk
innhold kommer senere (bekreftet levende link, ikke en død anker).

**Verifisert**: 26/26 tester grønt, `dotnet build` rent. Alle seks punkter er klikk-/live-testet i
nettleser (inkl. den reelle stille-blokkering-bugen reprodusert BÅDE med og uten fiksen for å
bekrefte årsakssammenhengen), ikke bare kodelest.

### Bugliste 2026-09-13, mobilrunde — sticky funksjonsnav fylte hele mobilskjermen, BankID-innlogging avviste korrekt sikkerhetssvar (2026-09-13)

Brukeren testet siden på faktisk mobil og fant to nye problemer, pluss et gjentatt ønske om
mindre knapper.

**Sticky funksjonsnav fylte hele mobilskjermen (kunne ikke skrolles forbi)**: `.site-header`
(brand-rad + hele funksjonsnavet under) er `position: sticky; top: 0`. Dette var uproblematisk da
funksjonsnavet var kort, men har vokst gjennom flere runder (Min side, Min konto, Partnere,
Prising, Økonomi, Min partner, Partnerprising m.m.) — for en konto som ser flere seksjoner
samtidig (Utvikler) blir det 13 knapper som med `flex-wrap` bryter om til mange linjer. På en smal
mobilskjerm ble denne stick-ede, flerlinjede headeren nesten like høy som selve skjermen, og siden
den er sticky "hang den fast" i toppen i stedet for å oppføre seg som et vanlig element man
scroller forbi — brukerens ord: "gjort knappene på toppen til et ikke-skroll-område". Løst i en ny
regel i det eksisterende `@media (max-width: 960px)`-bruddpunktet: `.site-header { position:
static; }` — på mobil ruller nå hele headeren/funksjonsnavet bort som normalt innhold. Samtidig
gjort knappene mindre på mobil (både `.funksjonsknapp` og radhandlings-`.btn-icon`, sistnevnte fra
30px til 26px) med mindre skrift/ikonstørrelse, jf. brukerens eget forslag. Verifisert live i en
390×844 (mobilstørrelse) Playwright-økt: headeren er nå borte fra skjermen etter et vanlig
scroll ned, og tabellinnholdet under blir tilgjengelig — bekreftet både i utgangsposisjon og etter
scroll.

**Reell funnet bug: BankID-innlogging avviste et korrekt sikkerhetssvar ("feil sikkerhetskode")**:
nøyaktig samme root cause som `ModelState`/`asp-for`-fallgruven funnet og fikset i slette-
bekreftelsesflytene tidligere denne dagen (bugliste 2026-09-13 gruppe B) — men DENNE gangen i selve
INNLOGGINGS-sidene, som aldri ble dekket av den rettingen siden de bruker `ICaptchaProvider`
uavhengig av slette-flyten. `Pages/Konto/LoggInn.cshtml.cs` og
`Areas/Pasientportal/Pages/Konto/LoggInn.cshtml.cs` sin `NyCaptcha()` genererer et FERSKT
spørsmål+signert fasit og kaller `return Page()` (ikke `RedirectToPage`) ved ethvert mislykket
forsøk — men `LoggInn.cshtml` sin `<input type="hidden" asp-for="CaptchaSignertFasit" />` rendrer
fortsatt den GAMLE POSTEDE ModelState-verdien, ikke den nye. Konsekvens: EN gang man svarer feil
på sikkerhetsspørsmålet (uansett årsak — tastefeil, chippet av på mobil, doble klikk), blir ALLE
senere forsøk på samme sidevisning avvist som "feil sikkerhetskode" uansett hva man svarer,
siden det skjulte feltet aldri oppdaterer seg — kun en full sideoppdatering (ny GET) løser det.
Dette rammer administrator-, behandler- OG pasient-innlogging likt. Fikset med `ModelState.Clear()`
som første linje i `NyCaptcha()` i begge filer (samme presedens som gruppe B, nå anvendt
universelt inni selve NyCaptcha()-metoden i stedet for ved hvert enkelt kallsted — renere og
umulig å glemme ved et fremtidig nytt kallsted). **Reprodusert og verifisert live**: sendte inn et
bevisst FEIL svar først (fikk "Feil svar på sikkerhetsspørsmålet" som forventet, med et NYTT
spørsmål vist) → leste det NYE spørsmålet og svarte korrekt → kom denne gangen forbi
captcha-sjekken (endte på "Fant ingen administrator/behandlerkonto", som er korrekt og forventet
siden BankID-mocken uansett bruker et fast testpersonnummer i dette lokale miljøet — det viktige
er at feilmeldingen ikke lenger var "feil sikkerhetskode").

**Verifisert**: 26/26 tester grønt, `dotnet build` rent. Begge funn reprodusert og bekreftet
rettet live (ikke bare kodelest) — sticky-fjerningen i en ekte mobil viewport-størrelse (390×844),
og captcha-fiksen med en ekte to-forsøks-sekvens (feil så riktig svar) som tidligere ville feilet
på steg to.

### Bugliste 2026-09-13, "BIGBUTTONS"-runden — radhandlingsknapper med feil form/farge/senter, for smal handlingskolonne, bygg-versjonsnummer i footer (2026-09-13)

Brukeren sendte et nærbilde (`BIGBUTTONS.png.jpg`) av radhandlingsknappene på Admin/Behandlere:
den grønne Rediger-knappen var et rent kvadrat, mens de oransje Frys/Arkiver/Tilbakekall-knappene
var brede rektangler med ikonet dyttet ned i hjørnet i stedet for sentrert — og med 4 knapper i
raden brøt de om til to linjer.

**Reell funnet bug: `.btn-icon`-knapper tapte en CSS-spesifisitetskonflikt mot to generiske
knappe-regler**: `main.page form button[type="submit"]` (ment for VANLIGE innsendingsknapper som
"Lagre alle endringer") har HØYERE CSS-spesifisitet enn `main.page .btn-icon` alene
(klasse+type+attributt-selektor slår ren klasse-selektor) — og siden radhandlingsknappene
(Frys/Arkiver/Godkjenn/Slett m.m.) OGSÅ har `type="submit"`, vant denne generiske regelen over
`.btn-icon` sin egen `display`/`padding`/`background` for akkurat disse knappene. Dette forklarte
alle tre symptomer samtidig: feil form (padding 0.75rem 1.5rem i stedet for fast 30×30px), feil
farge (den generiske regelens `var(--accent)` oransje i stedet for `.btn-icon` sin egen
bakgrunnsfarge — kun den grønne Rediger, en `<a>` og ikke en `<button>`, unngikk kollisjonen) og
feil sentrering (`display:inline-block` i stedet for `.btn-icon` sin `inline-flex`-sentrering, som
ga et synlig, men feilplassert ikon). Fikset ved å legge `:not(.btn-icon)` til akkurat DEN
regelen. Da dette ble fikset, dukket en NESTE, identisk kollisjon opp: den eldre, "nøytrale"
reset-regelen for radhandlings-skjemaer (`main.page form[style*="display:inline"] button`, fra
lenge før denne bugliste-rundene) har OGSÅ høyere spesifisitet enn `.btn-icon` alene og satte
`background: none` — knappene ble usynlige (transparent bakgrunn) helt til samme `:not(.btn-icon)`-
unntak ble lagt til der også. Begge steder er nå kommentert med hvorfor unntaket er nødvendig, for
å unngå at noen fjerner det ved en misforståelse senere.

**Handlingskolonnen gjort bredere, ikke bare knappene riktige (brukerens eksplisitte ønske
"make the button-area wider")**: `main.page table[border] td:last-child` sin `min-width` økt fra
et første forsøk på 150px (viste seg utilstrekkelig — testet direkte mot Admin/Behandlere sin
mest ekstreme rad, som kan vise HELE FEM knapper samtidig: Rediger+Frys+Arkiver+GodkjennHpr+
ForlengHpr) til 260px, verifisert empirisk (ikke bare beregnet i hodet — knapper-i-skjema har
DOBBEL marginering, både fra `.btn-icon` selv OG fra den omsluttende `<form style="display:inline">`,
som gjør en ren "N×knappbredde + mellomrom"-utregning for lav). Dette gjør at handlingskolonnen nå
kan tvinge selve TABELLEN bredere enn synlig skjermbredde i noen tilfeller — akseptert bevisst
etter brukerens eksplisitte instruks, og tabellen har allerede en etablert, testet
horisontal-scroll-mekanisme (`overflow-x: auto` på selve `<table>`, ikke hele siden) for nettopp
dette tilfellet.

**Bygg-versjonsnummer i footeren**: ny `TestBase.Shared.AppVersjon.Nummer`-konstant (starter på 1,
økes manuelt med én for hver commit som skal deployes), vist som "v{nummer}" rett ved siden av
copyright-linjen i `_Layout.cshtml` sin footer — slik at brukeren visuelt kan bekrefte at
nettleseren faktisk viser nyeste deploy og ikke en cachet, gammel side. **Reell Razor-fallgruve
oppdaget underveis**: den ferskt tilføyde `@AppVersjon.Nummer` (implisitt Razor-uttrykk, uten
parentes) rendret bokstavelig som TEKSTEN "@AppVersjon.Nummer" i produsert HTML i stedet for å bli
evaluert — bekreftet direkte fra server-responsen (`curl`), ikke bare i nettleseren, så det var
ingen cache-effekt. Løst ved å bruke et eksplisitt Razor-uttrykk med parentes, `@(AppVersjon.Nummer)`,
som fungerte umiddelbart. Årsaken til at nettopp DENNE implisitte `@`-plasseringen (rett etter en
bokstav "v", uten mellomrom, rett etter en HTML-entitet `&mdash;`) feilet er ikke fullt forklart,
men presedensen er klar: bruk ALLTID parentes-formen `@(uttrykk)` for nye interpolasjoner i denne
kodebasen fremover — det er alltid entydig riktig, mens den implisitte formen har minst ett kjent
tilfelle der den IKKE er det.

**Verifisert**: 26/26 tester grønt, `dotnet build` rent. Alle tre funn er live-testet (radhandlings-
knappenes eksakte pikselmål/farge/posisjon lest ut direkte fra DOM-en, ikke bare visuelt fra et
skjermbilde; kolonnebredden verifisert empirisk mot den faktiske 5-knappersraden; versjonsnummeret
bekreftet fra rå server-HTML via curl før og etter fiksen).

### WHO-5 VAS + generisk "utvikling over tid"-graf (2026-09-14)

Brukerens oppdrag: en repeterbar variant av WHO-5 for gjentatt måling, og en visuell graf for
utvikling over tid i rapporten — bygget som INFRASTRUKTUR for gjenbruk i flere fremtidige tester,
ikke en engangsløsning for kun denne ene testen.

**WHO-5 VAS (`who5_vas`)** — `Who5VasTestSeeder.cs`: samme fem utsagn og samme tidsramme ("de siste
to ukene") som originalen (Who5TestSeeder), men `TestSvartype.VisuellAnalogSkala` (glidebryter)
i stedet for den 6-punkts Likert-skalaen. Endepunktene på glidebryteren er ORDRETT Likert-skalaens
ytterpunkter ("Aldri"/"Hele tiden"), lagret i samme "verdi:tekst"-format
(`TestLeddSvaralternativer`) som allerede brukes for Likert, bare med kun to nøkler (0 og 100) —
ingen ny lagringsmekanisme trengtes. **Hvorfor VAS gjør gjentatt måling bedre**: en Likert-basert
WHO-5 kan kun endre prosentskår i sprang på 4 (0/4/8/…/100, siden råskår×4 med råskår 0–25), mens
en kontinuerlig 0–100-glidebryter fanger opp reelle, mindre endringer mellom to målinger — verifisert
konkret i en egen test
(`Who5Vas_KontinuerligSkaarKanSkilleVelvaereOgDepresjonsgrenseneNoeLikertIkkeKan`) som viser et
utfall (skår mellom de to grenseverdiene) som er umulig for Likert-versjonen.

**Skåring (`Who5VasSkaaringsberegner.cs`)**: råskår = sum av de fem 0–100-svarene (0–500),
prosentskår = råskår/5 (matematisk gjennomsnittet). Grenseverdiene er hentet DIREKTE fra
normeringslitteraturen på selve prosentskalaen — Topp CW, Østergaard SD, Søndergaard S, Bech P
(2015). "The WHO-5 Well-Being Index: A Systematic Review of the Literature." Psychotherapy and
Psychosomatics, 84(3), 167–176: prosentskår ≤ 50 anbefales som terskel for å screene videre for
depresjon (sensitivitet 79-88 %, spesifisitet 76-88 %), og ≤ 28 er en strengere, mye brukt terskel
for uttalt lav velvære. Kilden ble slått opp og verifisert (WebSearch), ikke husket/gjettet — samme
standard som EQ40/WURS/IPDS tidligere.

**VAS-glidebryteren i utfyllingssiden** (`Fyll.cshtml`): forbedret fra den allerede eksisterende,
men helt nakne `<input type="range">`-fallbacken (fantes i kodebasen fra før uten noen reell test
å bruke den, kun en generisk smoke-test) — la til synlige endepunkt-etiketter og egen styling
(`.vas-slider`/`.vas-endepunkter` i site.css, farget med `--rolle-accent`). Bevisst IKKE lagt til
et live tallvisning ved siden av glidebryteren: research (WebSearch) viste at å vise tallverdien
mens brukeren drar gjør den om til en numerisk skala (NRS) i stedet for en ekte VAS, som måler en
POSISJON, ikke et VALGT TALL — se "Visual analogue scale", Wikipedia, og CASRAI sin VAS-guide.
Bevisst IKKE løst: en glidebryter uten forhåndsutfylt verdi ville i teorien unngått at en helt
uberørt glidebryter (fortsatt midt på) lagres som et ekte "50"-svar (samme forskning nevner at en
forhåndsplassert glidebryter "ankrer" svar mot midtpunktet) — dette krever en egen
"rørt/ikke rørt"-sporingsmekanisme i JS og ble bevisst utsatt som en for stor kompleksitetsøkning
for denne runden; notert her som en kjent, akseptert forenkling.

**Generisk "utvikling over tid"-graf (NY, gjenbrukbar infrastruktur)**:
- `ITestSkaaringsberegner.Referanselinjer` — en NY, valgfri default-egenskap (tom liste som
  standard, C# 8 default interface-medlem) som lar en skåringsberegner navngi normerte horisontale
  grenselinjer (f.eks. WHO-5 VAS sine "Velvære"/"Depresjon"). Alle 9 eksisterende
  skåringsberegnere (WHO-5, PHQ-9, IPDS, MADRS-S, TRAPS I, RAADS-R, WURS, EQ40, søvn) krevde
  IKKE en eneste kodeendring for å få dette — bekreftet med en egen test
  (`SkaaringsberegnereUtenReferanselinjer_ReturnererTomListeSomStandard`).
- `UtviklingsGrafBeregner.cs` — ren, testbar C# (ingen ASP.NET Core-avhengighet) som regner om
  en `SkaaringHistorikkPunkt`-liste (finnes fra før, generisk for ALLE tester) + valgfrie
  referanselinjer til ferdige SVG-koordinater: Y-akse fast 0–100, X-akse datopunkter jevnt fordelt,
  med dato-ETIKETTER (ikke selve punktene) tynnet ut til høyst 8 synlige når det er flere målinger
  enn det — første og siste dato vises ALLTID. Dekket av 5 egne enhetstester
  (`UtviklingsGrafBeregnerTests.cs`), inkl. eksplisitt verifisering av at alle datapunkter
  fortsatt tegnes selv når etikettene tynnes ut.
- `Pages/Shared/_UtviklingsGraf.cshtml` — delt Razor-partial som rendrer et rent SVG-linjediagram
  fra denne ferdigberegnede geometrien: rutenett + Y-akse-tall, dato-etiketter på X-aksen,
  stiplede referanselinjer med tekstetikett, selve trendlinjen og sirkler per målepunkt (med
  `<title>`-hover-tekst). **Kjent Razor-fallgruve underveis**: `<text>` er et reservert Razor-
  "råtekst"-hjelpeelement som IKKE tillater attributter — kolliderer direkte med SVGs eget
  `<text>`-element (som trenger x/y/class). Løst ved å bygge disse elementene som rå
  strenger + `Html.Raw()` i stedet for vanlige Razor-tagger — verdt å huske for enhver fremtidig
  SVG-i-Razor-kode i denne kodebasen.
- Lagt inn i `Behandlerportal/Pasienter/Rapport.cshtml` sin eksisterende "Utvikling over tid"-
  seksjon (over den allerede eksisterende tabellen, ikke i stedet for den — tabellen viser fortsatt
  eksakte tall og "signifikant endring"-merking). **Bevisst IKKE lagt til** i (a) den inline-
  stylede "Kopier til utklippstavle"-versjonen av rapporten (SVG limes upålitelig inn i eksterne
  journalsystemers rikteksteditorer, samme forbehold som allerede gjelder CSS-klasser der), eller
  (b) pasientens egen rapportside (`Pasientportal/Tester/Rapport.cshtml`), som i dag ikke viser
  noen historikk i det hele tatt (kun behandlersiden gjorde det fra før) — å utvide pasientens
  rapport til også å vise historikk er en egen, ikke forespurt beslutning, ikke tatt her.

**Verifisert LIVE, ende-til-ende**: seedet testen, tildelte den til en pasient tre ganger via den
ekte tildelingswizarden, fylte ut glidebryteren med tre ulike skårsett (72, 42, 18 — bevisst valgt
for å krysse BEGGE referanselinjene), tidsforskjøv fullføringstidspunktene i lokal dev-database
(01.08/15.08/01.09.2026) for en realistisk datospredning, og bekreftet i behandlerens rapport at
grafen viser riktig fallende kurve, korrekte 0/25/50/75/100-Y-akselinjer, korrekte datoer på
X-aksen, og begge referanselinjene ("Velvære (50 %)"/"Depresjon (28 %)") plassert nøyaktig der de
skal være relativt til datapunktene. Bekreftet i tillegg at grafen skalerer korrekt på en ekte
mobil viewport-størrelse (390×844) uten overflow. 36/36 automatiserte tester grønt (10 nye:
5 for Who5VasSkaaringsberegner, 5 for UtviklingsGrafBeregner), `dotnet build` rent.

### Bugfiks: nye tester usynlige for partner-tilknyttede behandlere (2026-09-15)

Brukeren meldte: "Det virker som om nye tester må tildeles minst en partner før de blir
tilgjengelige for ikke partner behandlere." Stemmer delvis — koden viste at UAVHENGIGE (ikke
partnertilknyttede) behandlere ALLTID har sett alle aktive tester (`TestService.HentKategoriTreAsync`
filtrerer kun når en `partnerId` er gitt), men enhver partner-tilknyttet behandler var reelt
avhengig av at Superadmin manuelt krysset av testen for partneren på `Admin/Partnere/Tester`
(`PartnerTestTilgang`-allow-listen, se "Partner System + Test Monetization") — en ny test var i
praksis usynlig for ALLE partneres behandlere helt til noen husket å gjøre dette N ganger (én gang
per partner). Med kun én partner i systemet i forrige testrunde var dette usynlig.

Løsning, eksplisitt begrunnet med at vi fortsatt er i TEST PERIODEN (kan strammes inn igjen den
dagen partner-kurering faktisk skal brukes til å begrense hvilke tester en partner får):
`TestService.OpprettTestAsync` gir nå automatisk ALLE ikke-arkiverte/ikke-slettede partnere tilgang
til en ny test i det den opprettes (`GiAllePartnereTilgangTilTestAsync`, privat), og en ny
`GiAllePartnereTilgangTilAlleTesterAsync` kjøres idempotent ved hver applikasjonsoppstart
(`Program.cs`, rett etter `IInnebygdTestSeeder`-løkken) for å rette opp allerede eksisterende hull —
begge er ren "legg til manglende par"-logikk, ingen duplikater, ingen fjerning av allerede satte
rader (så en Superadmin som har fjernet en partners tilgang til en SPESIFIKK test manuelt beholder
den fjerningen — kun helt NYE tester/partnere fylles inn). `GittAvAdministratorId=0` markerer disse
radene som systemgenererte (samme sentinel-mønster som allerede brukt i
`Areas/Admin/Pages/Partnere/Tester.cshtml.cs` sin fallback).

To eksisterende `BetalingPipelineTests`-tester bygde bevisst en scenario der en partner KUN fikk
tilgang til én av to tester — måtte oppdateres til å eksplisitt FJERNE tilgangen til den ene testen
etter opprettelse (i stedet for å legge til tilgang til den andre), siden begge nå får tilgang
automatisk. Selve allow-list-HÅNDHEVELSEN i `TestTildelingsService.TildelOgVarsleAsync` og
tre-visningens filtrering er urørt og fortsatt reell — en Superadmin kan fortsatt kuratere ned
igjen, det er bare startpunktet som endret seg fra "ingenting" til "alt". 36/36 tester grønt.

### Bugfiks: betalingssteget blokkerte test-utfylling uten ekte Vipps/Stripe-avtale (2026-09-15)

Brukeren meldte at hun ikke kom gjennom til å fylle ut tester hun hadde satt en pris på — siden
viste "Ingen betalingsmetode er konfigurert", og spurte hva som trengs for å legge inn en
"jukse-betaling" for testing (hun har nå selv blitt godkjent for Vipps web, men det er en egen,
senere oppgave å koble til EKTE Vipps-legitimasjon).

Rotårsak: `Pasientportal/Tester/Betal.cshtml.cs` viste kun Vipps-/Stripe-betalingsknappene når de
FIRE ekte Vipps-nøklene (`Vipps:ClientId`/`ClientSecret`/`SubscriptionKey`/`MerchantSerialNumber`)
eller Stripe-nøklene faktisk var satt i konfigurasjon — selv om `IVippsClient`/`IStripeClient`
ALLTID er registrert i DI (ekte klient når nøklene finnes, ellers `MockVippsClient`/
`MockStripeClient`, se `Program.cs`). `azd env get-values` bekreftet at INGEN av disse er satt for
`testbase-test` ennå, så begge knappene var alltid skjult i det faktiske pasientflyten — selv om
mock-implementasjonene allerede fungerer fullt ut ende-til-ende (bevist tidligere via de
diagnostiske `Pages/BetalingTest`-sidene): `MockVippsClient.OpprettBetalingAsync` simulerer en
vellykket Vipps-redirect uten noe klientsidejavaskript i det hele tatt, og
`MockVippsClient.HentStatusAsync` rapporterer alltid "Fanget" — nøyaktig det
`BetalResultat.cshtml.cs` sin (leverandør-uavhengige) statusoppslags-bekreftelse trenger.

Løsning: `VippsTilgjengelig` er nå ALLTID `true` (en fungerende klient — ekte eller mock — finnes
alltid), med en ny `VippsErMock`-flagg som viser en tydelig "Testmodus: ingen ekte Vipps-avtale er
koblet til ennå, så dette simulerer en vellykket betaling uten at penger flyttes"-tekst under
knappen når det faktisk er `MockVippsClient` bak. Stripe er BEVISST IKKE endret på samme måte —
`MockStripeClient` returnerer aldri en ekte `ClientSecret`, så Stripe sitt front-end-JS
(Stripe Elements) kan ikke monteres uten en ekte (om enn test-modus) Stripe-konto; Stripe-knappen
vises fortsatt kun når ekte Stripe-nøkler er satt. Den nå alltid-usanne
"ingen betalingsmetode er konfigurert"-meldingen er fjernet fra viewet. Verifisert LIVE lokalt: satt
en midlertidig pris på WHO-5 VAS, tildelt til en testpasient, bekreftet at Vipps-knappen med
testmodus-teksten vises, klikket den, og fulgte hele redirect-runden til `BetalResultat` som viste
"Betaling bekreftet! Du kan nå fylle ut testen." — testdata og testpris i lokal dev-database
reversert etterpå. Neste steg når ekte Vipps-legitimasjon er klar: sett
`Vipps:ClientId`/`ClientSecret`/`SubscriptionKey`/`MerchantSerialNumber` (aldri som literal i
kildekoden — via `azd env set` → Key Vault, se `infra/resources.bicep`), så bytter appen automatisk
til ekte `VippsPaymentClient` og `VippsErMock` blir `false` av seg selv.

### Forsidebilde erstattet (2026-09-15)

`hero-placeholder.svg` på forsidens hero-seksjon (`Pages/Index.cshtml`) — eksplisitt merket
"byttes ut senere" i alt-teksten helt siden fase 6-designomgangen — er erstattet med et ekte bilde
brukeren la i repo-roten (`intro_image.jpg`, penn på notatblokk). Originalen var 4592×3448/2,1 MB,
langt større enn nødvendig for en hero-seksjon som vises i maks ~640px bredde på skjerm — skalert
ned til 1600px bredde (~124 KB, kvalitet 82) og lagt inn som `wwwroot/img/hero-intro.jpg`.
`hero-placeholder.svg` er BEVISST beholdt (fortsatt i bruk på `Pages/Pasienter.cshtml`, den
separate pasient-landingssiden — kun forsiden for behandler/admin fikk nytt bilde denne runden).

### Ekte Vipps satt i produksjon + bugfiks: manglende Idempotency-Key (2026-09-15)

Brukeren ble godkjent av Vipps og satte `VIPPS_CLIENT_ID`/`VIPPS_CLIENT_SECRET`/
`VIPPS_SUBSCRIPTION_KEY`/`VIPPS_MERCHANT_SERIAL_NUMBER`/`VIPPS_MILJO=Produksjon` selv via
`azd env set` (verdiene aldri sett/sett av meg — kun nøkkelnavn bekreftet via
`azd env get-values`). Bekreftet eksplisitt med brukeren at dette betyr EKTE Vipps
(api.vipps.no, ikke apitest.vipps.no) før `azd provision` + `azd deploy web` ble kjørt — se
"Vipps + Stripe (Apple Pay/Google Pay)" og "Vipps:Miljo som konfigurerbar azd-innstilling".

Første reelle betalingsforsøk feilet med `400 Bad Request` fra Vipps:
`"idempotencyKey": "The idempotencyKey field is required."`. Rotårsak:
`VippsPaymentClient.OpprettBetalingAsync` (`epayment/v1/payments`) satte aldri
`Idempotency-Key`-headeren Vipps ePayment API krever på betalingsopprettelse — usynlig i tidligere
testing siden `MockVippsClient` (brukt inntil nå) ikke kaller noe ekte API i det hele tatt. Fikset
ved å sende betalingsreferansen selv (`referanse`, allerede en stabil, unik streng per tildeling —
se `PaymentWebhookReferanse`) som `Idempotency-Key`: en eventuell retry med samme referanse
dedupliseres da trygt av Vipps i stedet for å opprette en ny betaling, i tråd med Vipps sin egen
anbefaling om å bruke en nøkkel knyttet til selve forretningstransaksjonen, ikke en tilfeldig GUID
per HTTP-kall. 36/36 tester grønt (ingen dedikerte enhetstester for selve HTTP-klienten — samme
bevisste unntak som for øvrige ekte leverandørintegrasjoner, se "Åpne punkter til senere faser").
Ikke selv verifisert med en fullført ekte betaling (krever godkjenning fra en ekte Vipps-app på en
telefon) — brukeren gjør selv første reelle ende-til-ende-test.

### Kritisk bugfiks: fullført test kunne (i teorien) sendes til betaling på nytt (2026-09-15)

Brukeren meldte, rett etter at ekte Vipps ble satt i produksjon: å klikke en GAMMEL e-post-/
SMS-lenke til en test hun allerede hadde besvart og betalt for, sendte henne til Vipps-betaling PÅ
NYTT — flagget eksplisitt som en potensiell forretningskatastrofe (dobbeltbelastning av pasienter).

Kodegjennomgang av hele betalingskjeden (`TestTildelingsService`, `TestService`,
`Fyll.cshtml.cs`, `Betal.cshtml.cs`) fant INGEN kodesti som eksplisitt SETTER en
`TestTildelingBetaling.Status` tilbake til `Venter` etter at den er `Betalt` — kun
`MarkerBetalingBetaltAsync` endrer status, og kun én vei (→ `Betalt`). Rotårsaken er derfor ikke
100 % bekreftet fra kildekoden alene (kan være en annen, distinkt tildeling av samme test, en
race/timing-detalj rundt Vipps-returflyten, eller noe utenfor det jeg klarte å reprodusere i
denne økten) — men uansett faktisk årsak var selve KODEREKKEFØLGEN i `Fyll.cshtml.cs`
(`OnGetAsync`/`OnPostAsync`) og `Betal.cshtml.cs` (`LastBetalingAsync`) usikker: betalings-gaten
(`betaling.Status == Venter` → send til Vipps/Betal) ble sjekket FØR sjekken på om tildelingen
allerede var `Fullfort` — så HVIS betalingsraden noensinne befinner seg i denne uventede
tilstanden (uansett hvordan), ville koden aktivt sende pasienten til en ekte betaling i stedet for
å vise "allerede besvart".

Fikset som en RENDYRKET forsvars-i-dybden-endring, uavhengig av om rotårsaken noensinne fullt ut
identifiseres: alle tre stedene sjekker nå `TestTildelingStatus.Fullfort` FØRST og returnerer
umiddelbart (ingen betalingssjekk, ingen Vipps-/Stripe-kall i det hele tatt) hvis testen allerede
er fullført — uansett hva betalingsraden måtte si. Spesielt `Betal.cshtml.cs` sin
`LastBetalingAsync` er den siste linjen mot et EKTE eksternt betalingskall (Vipps/Stripe), og har
nå denne sjekken rett før den henter betalingsraden i det hele tatt.

Ny ende-til-ende-regresjonstest lagt til i `HeleFlytenTests.cs` (etter den eksisterende
fullføringsflyten): tvinger betalingsraden for en ALLEREDE fullført tildeling tilbake til `Venter`
direkte i databasen (simulerer den mistenkte tilstanden uavhengig av årsak), gjør et nytt GET mot
`/Pasientportal/Tester/Fyll/{id}`, og bekrefter at responsen IKKE omdirigerer til `/Betal/` og
fortsatt viser "Ferdig!"-siden. 36/36 tester grønt. Ikke selv bekreftet mot ekte produksjonsdata
om noen faktisk dobbel Vipps-betaling ble gjennomført — brukeren bør sjekke Vipps-portalen sin
transaksjonshistorikk for denne konkrete testen/pasienten og eventuelt refundere derfra, dette er
UTENFOR hva applikasjonskoden kan rette opp i etterkant.

### Kritisk bugfiks: Vipps-betaling ble aldri fanget (capture) — pengene nådde aldri selger (2026-09-15)

Rett etter at ekte Vipps ble satt i produksjon, gjennomførte brukeren selv en ekte betaling for å
teste hele kjeden. Appen viste "Betaling bekreftet!", men ~20 timer senere sto beløpet fortsatt
som "Reservert" i selgerens egen Vipps-app — ikke trukket, ikke oppgjort. Bekreftet mot Vipps sin
offisielle dokumentasjon (developer.vippsmobilepay.com): ePayment API bruker "reserver så fang"
(reserve-and-capture) som STANDARD — en betaling som brukeren har godkjent går til status
`AUTHORIZED` (kun en RESERVASJON hos betaleren), og blir ALDRI trukket for godt eller utbetalt til
selgeren før selgeren selv gjør et eksplisitt `POST /epayment/v1/payments/{reference}/capture`-kall
(kan skje sekunder eller opptil ~180 dager senere, avhengig av betalingsmetode). Uten dette
kanselleres reservasjonen til slutt automatisk, og betaleren får pengene sine tilbake — selgeren
sitter da igjen med NULL kroner til tross for at appen viste "bekreftet".

Rotårsak: `VippsPaymentClient`/`BetalResultat.cshtml.cs` kalte ALDRI capture-endepunktet —
`HentStatusAsync` sitt svar `AUTHORIZED` ble behandlet som ferdig betalt (`erBetaltHosLeverandor`
inkluderte `VippsBetalingsstatus.Autorisert` som suksess) og markerte umiddelbart
`TestTildelingBetaling.Status = Betalt` i egen database — helt usynlig i all tidligere testing
siden `MockVippsClient` simulerer en allerede "Fanget" (ikke bare autorisert) betaling og aldri
snakker med noe ekte API.

Fikset ved å legge til et nytt `IVippsClient.FangBetalingAsync`-medlem (ekte implementasjon:
`POST .../capture` med `modificationAmount` + egen `Idempotency-Key` forskjellig fra selve
betalingsopprettelsens; mock: simulerer alltid vellykket fanging umiddelbart) og en ny privat
`ErVippsBetalingBekreftetOgFangetAsync`-hjelper i `BetalResultat.cshtml.cs`: når Vipps rapporterer
`Autorisert` (ikke allerede `Fanget`), fanges FULLT beløp med det samme — riktig valg siden testen
leveres digitalt og umiddelbart ved bekreftet betaling, ingen grunn til å utsette slik man ville
gjort for fysiske varer som først skal sendes. Siden testen ALLEREDE hadde markert brukerens
konkrete transaksjon som `Betalt` i databasen (fra FØR denne fiksen), la jeg samme fangst-sjekk inn
i den eksisterende "allerede betalt"-early-return-grenen også — selvhelbredende, slik at et nytt
besøk på `/Pasientportal/Tester/BetalResultat/{tildelingId}` for DENNE spesifikke, allerede
"betalte"-men-ikke-fangede transaksjonen faktisk trigger den manglende fangingen, uten å måtte
gjøre noen direkte databaseendring i produksjon. 36/36 tester grønt.

Vipps-webhooken (`Security/PaymentWebhooks.cs`) fikk IKKE samme fangst-logikk denne runden —
`Vipps:WebhookSecret` er ikke konfigurert ennå, så webhook-mottakeren avviser (401) alt uansett og
er for øyeblikket død kode i praksis; den synkrone `BetalResultat`-siden er derfor den ENESTE reelle
bekreftelsesveien akkurat nå. Må huskes som en oppfølging DEN DAGEN webhooken faktisk kobles til
(se "Åpne punkter til senere faser") — den vil trenge samme reserver-så-fang-behandling.

**Om oppgjørstid** (fra Vipps sin offisielle dokumentasjon, se kilder): etter et vellykket capture
tar det normalt to virkedager før beløpet faktisk overføres til selgerens bankkonto — dag 1 er
selve fangingen, dag 2 genereres utbetalingstransaksjonen på kvelden/natten, dag 3 er selve
bankoverføringen fra Vipps sin konto til selgerens. Beregnes hver dag (også helg/helligdag), men
selve overføringsfrekvensen (daglig/ukentlig/månedlig) avhenger av den enkelte selgeravtalen.

Kilder: [Settlements](https://developer.vippsmobilepay.com/docs/knowledge-base/settlements/),
[Capture](https://developer.vippsmobilepay.com/docs/knowledge-base/reserve-and-capture/),
[Capture the payment with the ePayment API](https://developer.vippsmobilepay.com/docs/APIs/epayment-api/api-guide/operations/capture/).

## Åpne punkter til senere faser

- **[HANDLING PÅKREVD AV BRUKER]** Bekreft at 2026-09-14-transaksjonen (den første ekte
  Vipps-betalingen, oppdaget "Reservert" i Vipps-appen ~20 timer etter godkjenning — se "Kritisk
  bugfiks: Vipps-betaling ble aldri fanget") faktisk ble FANGET (capture) etter fiksen: besøk
  `https://www.psytest.no/Pasientportal/Tester/BetalResultat/{tildelingId}` (id fra den opprinnelige
  SMS-/e-post-lenken, `/Fyll/{id}`) innlogget som den pasienten — siden er selvhelbredende og
  fanger reservasjonen ved besøk. Fjern denne linjen når bekreftet fanget OG at Vipps-appen viser
  trukket (ikke lenger "Reservert") og etter hvert oppgjort til bankkontoen (se samme seksjon for
  ~2 virkedagers oppgjørstid etter fanging).
- **[HANDLING PÅKREVD AV BRUKER, LAVERE PRIORITET]** Vipps-webhooken (`Security/PaymentWebhooks.cs`)
  er reell dødkode akkurat nå — `Vipps:WebhookSecret` er ikke konfigurert, så den avviser (401) alt
  den mottar. Den synkrone `BetalResultat`-siden er eneste reelle bekreftelsesvei inntil videre
  (fungerer helt fint alene), men BØR kobles til etter hvert for redundans (pasienten som aldri
  kommer tilbake til returUrl, f.eks. ved nettverksfeil midt i Vipps-appen). Krever: registrere et
  webhook-endepunkt hos Vipps (`https://www.psytest.no/webhooks/vipps`) og sette
  `VIPPS_WEBHOOK_SECRET` via `azd env set` (samme mønster som de fire andre Vipps-nøklene). Husk òg
  å legge til SAMME "fang før merk betalt"-logikk i webhook-handleren når den kobles til — den
  mangler denne behandlingen ennå, se samme seksjon.
- Stripe Connect-basert automatisk utbetaling til partnere/behandlere — helt
  utsatt i denne fasen, se "Partner System + Test Monetization". Databasen
  (Pengebevegelse) er bevisst formet slik at dette er tilføybart senere uten
  ombygging, men selve integrasjonen er ikke startet.
- Den faktiske tilbakevendende abonnements-FAKTURERINGEN (å faktisk trekke
  behandler/partner sitt kort månedlig) er ikke bygget — kun tilstanden
  (`HarEgetAbonnement`/`HarAktivtAbonnement`) finnes, satt manuelt inntil videre.
- Partner-egen database/pålogging/embedding i partnerens eget nettsted — helt
  utsatt, ingen konkret partner-spesifikasjon finnes ennå til å bygge mot.
- Multi-språk testversjoner (sende engelsk versjon av en test fra norsk side)
  og partner-theming (farger/logo) — begge eksplisitt utsatt fra det
  opprinnelige `finance_system.docx`-forslaget.
- Formell regnskapsstandard-eksport (SAF-T e.l.) for `/Admin/Okonomi` —
  siden er bevisst en placeholder (to tall) inntil bruker gir eksakt
  tekst/format, og bør uansett kvalitetssikres av regnskapsfører.
- Vipps-webhookens JSON-parsing (`reference`-feltet) er ikke bekreftet mot et
  ekte, levende kall ennå — se "Vipps + Stripe (Apple Pay/Google Pay)" og
  "Partner System + Test Monetization". `HentStatusAsync`-fallbacken på
  `BetalResultat`-siden er det som faktisk holder Vipps-betalinger fungerende
  inntil dette er live-verifisert.
- Ingen UI for å vise pasienten kvittering/betalingshistorikk ennå — kun selve
  betalings-gaten er bygget.


- Fyll inn ekte firmanavn/organisasjonsnummer/adresse/telefon/e-post i footer
  (`_Layout.cshtml`) og på `/Salgsvilkar` — i dag rene plassholdere, se "Salgsvilkår-side
  for Vipps sin nettsted-verifisering". Sannsynlig blokkerende for at Vipps sin
  nettsted-verifisering faktisk skal bestås.
- La en jurist kvalitetssikre/erstatte `/Salgsvilkar` (samme status som
  `docs/compliance-dpia-utkast.md`) før reelle pasienter bruker tjenesten — spesielt
  angrerett-vurderingen (angrerettloven § 22) er eksplisitt ikke gjort her.
- Fjern `StagingGate:BasicAuthUsername`/`BasicAuthPassword` igjen (se "Midlertidig
  HTTP Basic Auth i StagingGate") så snart Vipps sin merchant-registrering har bestått
  "Verifiser nettstedet" — ikke la den midlertidige native Basic Auth-dialogen stå
  permanent i stedet for nøkkelskjemaet.
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

## Runde 2026-09-15: pasienttabell-bredde + "Lagrer …" over hele appen

Fra `bugs_features.txt` — bruker rapporterte at pasientlisten (Admin/Behandlerportal) var for
bred for `main.page`-kortet, med skjulte handlingsknapper bak en lite synlig horisontal
scrollbar, og ba om at den "Lagrer …"-grånings-oppførselen som allerede fantes på
tildelingsflyten (`data-disable-on-submit`, se `wwwroot/js/validering.js`) skulle brukes
konsekvent på ALLE skjemaer som faktisk endrer databasen.

**Pasienttabell-bredde:** ny `ViewData["BredSide"]` → `<main class="page page--bred">`
(`Pages/Shared/_Layout.cshtml`), med `main.page.page--bred { max-width: min(1600px, 96vw); }`
i `site.css`. Satt på `Areas/Admin/Pages/Pasienter/Index.cshtml` og
`Areas/Behandlerportal/Pages/Pasienter/Index.cshtml`. I tillegg fikk `#pasientTabell
td:last-child` en egen, lavere `min-width` (150px, ned fra den generiske 260px dimensjonert for
Behandlere-tabellens opptil 5 knapper) siden denne tabellen kun har 2-3 handlingsknapper — de to
endringene sammen fjerner behovet for tabellens egen `overflow-x:auto` helt på vanlige
bærbar-bredder (verifisert på 1440px), med tabellens interne scroll fortsatt som fallback på
mobil. **Fallgruve oppdaget underveis, IKKE en cache-/build-feil:** en CSS-kommentar som skrev
stien `Areas/*/Pages/Pasienter/Index.cshtml` inneholdt bokstavelig `*/` midt i teksten, som
avsluttet CSS-kommentaren for tidlig og korrumperte HELE den påfølgende regelen til en ugyldig
selector (nedjektes stille av CSS-parseren — ingen synlig feil, bare "endringen virker ikke").
Timevis av tilsynelatende "browser-cache"-symptomer (identisk `asp-append-version`-hash,
identisk `ETag`, men CSSOM manglet regelen uansett cache-omgåelse) skyldtes utelukkende dette —
ikke noen reell cache-lagdeling. Lærdom: skriv ALDRI en filsti med `*` (wildcard-notasjon) inni en
CSS/JS-kommentar; bruk normal tekst i stedet.

**"Lagrer …" over hele appen:** `data-disable-on-submit` (som deaktiverer submit-knappen og
bytter teksten mens et skjema laster, se `validering.js`) lagt til på rundt 30 skjemaer på tvers
av alle tre Areas + toppnivå `Pages/` som faktisk skriver til databasen (opprett/rediger/
arkiver/gjenopprett/godkjenn/inviter/importer/betal/login m.m.) — se `git log` for full filliste,
ikke gjentatt her siden det er ren mekanisk utbredelse av et allerede etablert mønster.
**Bevisst utelatt:** alle skjemaer som bruker `onsubmit="return bekreftSletting(...)"` eller
`onsubmit="return confirm(...)"` (slette-/forkast-bekreftelser). Årsak: `validering.js` sine to
`document.addEventListener("submit", ..., true)`-lyttere kjører i CAPTURING-fasen (dokument →
skjema), som ALLTID skjer FØR skjemaets EGEN `onsubmit`-attributt (som kjører i target-fasen) —
disable-on-submit-lytteren ville dermed deaktivert og omdøpt knappen FØR brukeren i det hele tatt
fikk se bekreftelsesdialogen, og knappen ville blitt værende deaktivert/omdøpt permanent hvis
brukeren avbrøt dialogen (siden ingen sideomlasting skjer for å tilbakestille den). Reell
rekkefølge-bug oppdaget og unngått under denne runden — ikke bare en stilistisk avgjørelse. Denne
begrensningen gjelder ethvert FREMTIDIG skjema av samme type: kombiner ALDRI
`data-disable-on-submit` med en `onsubmit`-basert bekreftelsesdialog uten samtidig å fikse
rekkefølge-problemet (f.eks. ved å la bekreftelsen skje i EN FELLES capturing-lytter før
disable-on-submit-logikken, ikke i skjemaets egen attributt).

**Sekundær JS-fiks nødvendig for é ett skjema:** `Pasientportal/Tester/Fyll.cshtml` har FLERE
innsendingsknapper i samme skjema (Forrige/Lagre/Neste/Ferdig, valgt via `name="Handling"
value="..."`) — den opprinnelige disable-on-submit-logikken plukket alltid FØRSTE knapp i DOM-en
uansett hvilken som ble klikket, som ville vist feil lastetekst og latt de ANDRE knappene forbli
klikkbare (dobbel-innsending fortsatt mulig). Fikset i `validering.js` til å bruke
`e.submitter` (standard `SubmitEvent`-egenskap) for å identifisere DEN FAKTISK KLIKKEDE knappen —
den får lastetekst, ALLE knapper i skjemaet deaktiveres. Verifisert med et scriptet
`form.requestSubmit(knapp)`-kall for begge knappene på WHO-5s utfyllingsside.

## Stripe testnøkler aktivert på testbase-test (2026-09-15)

Fra `bugs_features.txt` — bruker ba om å få testet "den andre betalingsløsningen" også
(Stripe, ved siden av Vipps). `Betal.cshtml.cs` viser Stripe automatisk når
`Stripe:SecretKey`/`Stripe:PublishableKey` er satt (se `OnGetAsync`) — allerede satt LOKALT via
`dotnet user-secrets` (ekte Stripe TEST-nøkler, `sk_test_.../pk_test_...`), men aldri pushet til
`testbase-test` sitt azd-miljø (kun de fire Vipps-nøklene var satt der, se `azd env get-values`).
Satt `STRIPE_SECRET_KEY`/`STRIPE_PUBLISHABLE_KEY` via `azd env set` med de samme test-verdiene og
kjørt `azd provision` — bekreftet at `Stripe__SecretKey`/`Stripe__PublishableKey`/
`Stripe__WebhookSecret` nå ligger som App Service-innstillinger. `Betal`-siden på
`testbase-test`/`psytest.no`-test bør nå vise BEGGE betalingsalternativene. Ingen kodeendring —
kun konfigurasjon. `Stripe:WebhookSecret` var allerede tom lokalt (samme udekte hull som
Vipps-webhooken, se "Åpne punkter til senere faser") — ikke satt her heller.

## Godkjente rapporter som egen liste (2026-09-15, bugliste punkt 1)

Fra `bugs_features.txt` — godkjente rapporter (`TestTildeling.RapportGodkjentUtc != null`) skulle
vises på en EGEN liste, ikke bare som en lenke blant tildelte/pågående tester. Lagt til tre
parallelle sidepar (samme mønster som Tildel-flyten):

- `Pasientportal/Tester/GodkjenteRapporter` — pasientens EGNE godkjente OG delte rapporter
  (`RapportGodkjentUtc != null && RapportSynligForPasient`), lenket fra en ny knapp øverst på
  `MinSide.cshtml`. Lenker videre inn i den eksisterende `Pasientportal/Tester/Rapport/{id}`.
- `Behandlerportal/Pasienter/GodkjenteRapporter/{pasientId}` — ALLE godkjente rapporter for én
  pasient (uavhengig av delingsvalg), samme tilgangsregel som `Detaljer.cshtml.cs`
  (`HarTilgangAsync`: egen pasient, eller partner-admin for en pasient hos en behandler i samme
  partnerskap). Lenket fra en ny knapp på `Detaljer.cshtml`. Lenker videre inn i den eksisterende
  `Behandlerportal/Pasienter/Rapport/{tildelingId}`.
- `Admin/Pasienter/GodkjenteRapporter/{pasientId}` + en HELT NY `Admin/Pasienter/Rapport/{id}` —
  admin (og superadmin/dev, siden `/Admin/Pasienter` allerede er `AdminOmrade`-gated, som dekker
  begge, se Program.cs) hadde ingen egen rapportvisning fra før og kunne ikke gjenbruke
  Behandlerportal sin (den krever at innlogget bruker sin EGEN BehandlerId eier pasienten — en
  admin har ingen BehandlerId). Ny, ren lesetilgangs-side uten godkjenn/forkast/send-knapper
  (det er behandlers ansvar), som viser rapporten uansett `RapportSynligForPasient`. Lenket fra en
  ny kolonne på `Admin/Pasienter/Index.cshtml`.

Ny delt spørring `TestService.HentGodkjenteForPasientAsync(pasientId, kunSynligForPasient, ct)`
brukes av alle tre. **Kjent, IKKE ny, begrensning tatt med videre uendret:** en partner-admin som
ser denne listen for en pasient hos en ANNEN behandler i samme partnerskap, vil fortsatt få en 404
hvis de klikker "Se rapport" inn i `Behandlerportal/Pasienter/Rapport/{id}` — den siden sjekker
`p.BehandlerId == innlogget behandlers EGEN id`, ikke `HarTilgangAsync`-regelen. Samme hull fantes
allerede på `Detaljer.cshtml` sin tilsvarende lenke før denne runden — ikke introdusert her, bare
arvet. Verifisert kun på rute-/autorisasjonsnivå (302 til innlogging på alle fire nye ruter, ingen
404/500) — ikke fullt gjennomklikket med ekte innlogging/data i denne runden.

## Test-tilgangsforespørsler (2026-09-15, bugliste punkt 3/4/5)

Fra `bugs_features.txt` — partner-admin skal kunne be om å legge til/fjerne tester fra partnerens
`PartnerTestTilgang`-allow-list selv (i dag kun en Superadmin-direkte `OnPostToggleAsync` på
`Admin/Partnere/Tester`), med bulk-godkjenning/avvisning av en administrator. **Bevisst avgrenset
til partnere:** unaffiliert (ikke partnertilknyttet) behandler har allerede UBEGRENSET testtilgang
og ingen allow-list å be om endringer mot (se `HentKategoriTreAsync`) — bruker bekreftet at dette
skal utelates helt, ikke løses med en ny restriksjonsmekanisme.

**Ny entitet** `TestTilgangForespoersel` (`PartnerId`, `TestId`, `Handling` enum
LeggTil/Fjern, `Status` enum Venter/Godkjent/Avvist, `ForespurtAvBehandlerId`+`ForespurtUtc`,
`BehandletAvAdministratorId`+`BehandletUtc` — migrasjon `LeggTilTestTilgangForesporsler`), pluss
`TestService.OpprettTestTilgangForespoerselAsync`/`HentVentendeTestTilgangForesporslerForPartnerAsync`/
`HentVentendeTestTilgangForesporslerAsync`/`BehandleTestTilgangForesporslerAsync` (sistnevnte er
selve bulk-godkjenningen — samme effekt på `PartnerTestTilgang` som `OnPostToggleAsync`, bare via
denne omveien). `Behandlerportal/MinPartner/Prising.cshtml` fikk en "Vis alle tester"-seksjon
(`<details>`, alle aktive tester partneren IKKE har) med en "Be om tilgang"-knapp per test, samt en
"Be om fjerning"-knapp per rad i den eksisterende andel-tabellen, og en liste over egne ventende
forespørsler.

**Godkjenningskøen havnet IKKE på en ny `/Oppgaver`-side.** Under implementering ble det oppdaget
at `Admin/Pages/MinSide.cshtml.cs` sin eksisterende klassekommentar sier eksplisitt: "tidligere en
egen 'Oppgaver'-side, slått sammen hit" (bugliste 2026-09-13 punkt 22) — dvs. `/Oppgaver` fantes en
gang, men ble en bevisst konsolidert bort til `MinSide` i alle tre Areas allerede i fase 6. Både
`Behandlerportal/MinSide.cshtml` og `Pasientportal/MinSide.cshtml` viser i praksis allerede nøyaktig
det en "oppgaveliste" ville vist (ugodkjente rapporter/meldinger/ikke-besvarte tester for behandler,
tildelte-men-ikke-fullførte tester for pasient) — CLAUDE.md og deler av denne loggen beskrev
fortsatt den GAMLE, konsoliderte `/Oppgaver`-siden som om den fantes separat. Rettet i CLAUDE.md
(se "Hvordan jobbe videre" og prosjektstrukturen). Den nye test-tilgang-godkjenningsseksjonen ble
derfor lagt til direkte på `Admin/Pages/MinSide.cshtml` (bulk-avkrysning + Godkjenn/Avvis valgte),
ikke en ny separat side — å bygge en konkurrerende `/Oppgaver`-URL ved siden av en `MinSide` som
allerede fyller akkurat den rollen, ville gitt to overlappende oppgavelister.

**Reell EF Core-bug fanget under verifisering, IKKE sendt til produksjon:** første utkast av
`HentVentendeTestTilgangForesporslerAsync` kalte `.ToDictionaryAsync(b => b.Id, b => b.Visningsnavn, ...)`
DIREKTE på en `IQueryable<Behandler>` — `Visningsnavn` er en ikke-mappet, beregnet C#-egenskap
(`$"{Fornavn} {Etternavn}".Trim()`), som EF Core ikke kan oversette til SQL. Ville kastet en reell
`InvalidOperationException` i det øyeblikket en ekte forespørsel noensinne skulle vises. Fanget
FØR commit ved en fullstendig ende-til-ende-verifisering (se under), IKKE bare ved å lese koden —
fikset til samme trygge mønster som `Admin/Pasienter/Index.cshtml.cs` allerede bruker: hent
`Behandler`-radene fullt ut via `ToListAsync()` FØRST, bygg ordboken i minnet ETTERPÅ.

**Verifisert reelt ende-til-ende**, ikke bare rute-/kompileringsnivå: logget inn som `dev-admin`
(AdminId+passord+captcha, kun utviklingsmiljø) via curl, satt inn en ekte ventende
`Fjern`-forespørsel direkte i dev-databasen (partner "Testpartner AS", WHO-5), lastet
`Admin/MinSide` og bekreftet at raden faktisk rendres, postet bulk-godkjenning
(`?handler=GodkjennValgteTestTilgang`), og bekreftet i databasen at forespørselen fikk
`Status=Godkjent`+`BehandletAvAdministratorId`/`BehandletUtc`, OG at den tilsvarende
`PartnerTestTilganger`-raden faktisk ble fjernet. **Kjent, ufarlig lokalt dev-kvirk:**
`TestService.GiAllePartnereTilgangTilAlleTesterAsync()` (idempotent hull-tetting, kjøres ved HVER
oppstart — men KUN inni `if (app.Environment.IsDevelopment())` i `Program.cs`, ikke i
prod/test-miljøet på Azure) vil ved neste lokale `dotnet watch run`-restart automatisk gi
partneren tilgang til testen igjen, siden den bare fyller ethvert manglende partner×test-par uten
å vite at fjerningen nettopp var en bevisst godkjent handling. Påvirker ikke `testbase-test`/prod.

## Planlagt/gjentagende utsending av tester (2026-09-15, bugliste punkt 8)

Fra `bugs_features.txt` — tildelingsflytens steg 2 (`Tildel/Tester.cshtml`, begge Areas) fikk et
NYTT steg mellom testvalg og den eksisterende oppsummerings-dialogen: en tidsvalg-dialog
(`Pages/Shared/_PlanleggingDialog.cshtml`, delt mellom Admin og Behandlerportal) med Nå/
I morgen i arbeidstiden (kl. 09:00 norsk tid)/egendefinert dato+klokkeslett, pluss en valgfri
ukentlig gjentagelse (ukedag+klokkeslett+antall ganger).

**Ny entitet** `PlanlagtTildeling` (migrasjon `LeggTilPlanlagteTildelinger`) lagrer nøyaktig de
samme inputene `TestTildelingsService.TildelOgVarsleAsync` allerede tar (pasient-/test-id-er som
CSV, honorar som JSON, varslingsmetode, behandler/admin-id), pluss `PlanlagtUtc`, en valgfri
`GjentaUkedag`/`GjentaKlokkeslett`/`GjentaAntallGjenstaende`, og `Status`
(Venter/Sendt/Feilet). Ny `PlanlagtTildelingService` (samme mappe som TestTildelingsService)
gjenbruker `TildelOgVarsleAsync` 100 % uendret — planlegging endrer KUN når sendingen skjer,
ikke hvordan. Ny `PlanlagtTildelingBakgrunnstjeneste` (samme selvhelbredende
"sjekk-om-det-trengs"-mønster som `DagligPaaminnelseBakgrunnstjeneste`, men hvert 2. minutt i
stedet for hvert 15. — brukervalgte klokkeslett forventes truffet noenlunde presist) plukker opp
forfalte rader, kjører dem, og enten avslutter raden (`Sendt`/`Feilet`) eller reskjedulerer den til
neste forekomst og dekrementerer telleren, uansett om selve sendingen lyktes (en varig feilet
pasient/test-kombinasjon skal ikke blokkere resten av en gjentagende serie for alltid — feilen
lagres på raden i `Feilmelding`, men serien fortsetter).

**Norsk lokaltid, ikke UTC, for "kl. 10":** `Europe/Oslo` (via `TimeZoneInfo.FindSystemTimeZoneById`,
som .NET 6+ slår opp via ICU på både Windows og Linux uten noe eget OS-avhengig navn) brukes til å
tolke både "i morgen i arbeidstiden" og gjentagelsens ukedag+klokkeslett — resten av appen er
bevisst ren UTC internt (ingen annen tidssone-håndtering fantes fra før), men et menneske som
skriver "kl. 10" mener norsk klokke, ikke UTC.

**To separate submit-knapper, ikke én dynamisk:** oppsummerings-dialogen fikk en skjult
"Bekreft og planlegg"-knapp (`asp-page-handler="Planlegg"`) ved siden av den eksisterende
"Bekreft og send" (`asp-page-handler="Send"`, UENDRET oppførsel) — `wwwroot/js/tildel.js` bytter
hvilken som er synlig/aktiv basert på om "Nå" uten gjentagelse er valgt (da forblir alt som før) or
noe annet (Send-knappen skjules OG deaktiveres, ikke bare skjules, for å unngå at den likevel
trigges av Enter-tasten). To Razor `asp-page-handler`-baserte `formaction`-knapper i samme skjema
i stedet for å mutere skjemaets `action` fra JS — tryggere og lettere å lese.

**Verifisert reelt ende-til-ende**, ikke bare kompilering: logget inn som `dev-admin` via curl,
kjørte hele tildelingsflyten (steg 1 → steg 2 → `?handler=Planlegg` med Egendefinert+Gjenta),
bekreftet i databasen at raden fikk riktig `PlanlagtUtc` (norsk kl. 10 → riktig UTC-forskyvning) og
riktig `GjentaUkedag`. Satte deretter raden sitt `PlanlagtUtc` til fortiden direkte i databasen og
restartet serveren (bakgrunnstjenesten kjører sin første sjekk UMIDDELBART ved oppstart, ikke etter
første intervall) — bekreftet at den faktisk opprettet en ekte `TestTildeling`
(`TildelOgVarsleAsync` kjørte reelt), dekrementerte `GjentaAntallGjenstaende` fra 4 til 3, regnet ut
korrekt NESTE fredag, og lot `Status` forbli `Venter`. Kjørte til slutt en ren regresjonssjekk av
den UENDREDE `?handler=Send`-veien (ingen planlegging) — fortsatt identisk oppførsel.

**Bevisst utsatt:** ingen UI for å SE/avlyse en ventende planlagt utsending (kun opprettelse) —
`planlagte_tildelinger` har ingen liste-/administrasjonsside ennå. Ingen varsling til
behandler/admin hvis en planlagt sending faktisk `Feilet`. Begge er naturlige, avgrensede
utvidelser av samme mønster som Admin/MinSide sin oppgaveliste, ikke gjort her for å holde denne
runden fokusert på selve sende-mekanismen bruker ba om.

## Beta-miljø (2026-09-16)

Fra `bugs_features.txt` — et nytt tredje trinn ("beta") mellom lokal dev og "live"
(`testbase-test`/`www.psytest.no`, som til tross for navnet er den faktiske produksjonssiden nå):
en full klone av produksjonsstørrelsen for å reprodusere data-avhengige feil uten å røre live.

**Nytt azd-miljø `testbase-beta`** (`rg-testbase-beta`, `Sweden Central`, SAMME App
Service-/MySQL-SKU-er som live) provisjonert og deployet via de eksisterende Bicep-malene —
ingen egne maler trengtes for selve infrastrukturen. Låst med SAMME `StagingGate:AccessKey` som
live, jf. brukerens krav. DNS (`beta.psytest.no` → `app-testbase-uonprpnwr4eum.azurewebsites.net`
+ `asuid.beta`-TXT-verifisering) er brukerens ansvar (cPanel Zone Editor, samme som for
`www.psytest.no`) — IKKE gjort ennå i denne runden, se åpne punkter.

**Reell bug fanget og fikset FØR den kunne påvirke noe miljø:** `Varsling__BaseUrl` (brukes av
bakgrunnstjenester uten HttpContext, f.eks. `DagligPaaminnelseBakgrunnstjeneste` og den nye
`PlanlagtTildelingBakgrunnstjeneste`) var HARDKODET til `https://www.psytest.no` direkte i den
DELTE `resources.bicep`-malen. Uendret ville dette gitt beta sine egne bakgrunnsjobber
(påminnelser, planlagte utsendinger) lenker som pekte til LIVE-siden. Parameterisert til
`varslingBaseUrl` (tom → faller tilbake til appens egen `*.azurewebsites.net`-vertsnavn), og
`VARSLING_BASE_URL` satt eksplisitt til `https://www.psytest.no` på `testbase-test` FØR noen
reprovisjonering kunne miste det (samme fallgruve-mønster som `StagingGate__AccessKey` tidligere,
se "Sky-deploy til Azure (azd)").

**Full runtime-bryter for betalingsleverandører — Mock/Test/Produksjon, ikke bare av/på.**
Etter diskusjon endte kravet på: beta skal kunne teste BÅDE en trygg sandkasse OG (bevisst, med
brukerens eksplisitte samtykke etter å ha fått risikoen forklart) den EKTE produksjonskontoen for
Vipps, siden "testing utenfor et live-miljø" per bruker krever å kunne komme så nære det ekte som
mulig. Ny singleton-rad `BetaBetalingsinnstilling` (`VippsModus`: Mock/Test/Produksjon,
`StripeModus`: Mock/Test — Stripe har ingen tilsvarende produksjonsfare, testnøklene ER trygge
ekte API-kall) styrer `BetaSwitchingVippsClient`/`BetaSwitchingStripeClient`
(`TestBase.Shared/Providers/`), som leser innstillingen PER KALL (ikke cachet ved oppstart) og
delegerer til riktig underliggende klient — en superadmin/dev-admin endrer den uten omstart via en
ny seksjon på `Admin/MinSide` (`VisBetalingsmodusBryter`, gated på `Miljo:ErBeta` OG
Superadmin/Utvikler-rolle, usynlig for vanlig Administrator og usynlig/inaktiv på live). Live/lokal
dev bruker fortsatt EKSAKT den samme direkte, faste DI-registreringen som før — `Miljo:ErBeta`
(ny konfigurasjonsnøkkel, streng "true"/"false" — se under) gater HELE den nye grenen i
`Program.cs`, ikke bare UI-en.

To separate credential-sett for Vipps kreves samtidig i appen for at bryteren skal ha noe å bytte
MELLOM: det eksisterende `Vipps:ClientId`-settet gjenbrukes UENDRET som "Produksjon" (satt til
SAMME ekte verdier som live), og et helt nytt `Vipps:BetaTest:*`-sett (egne Bicep-parametre
`vippsBetaTestClientId/ClientSecret/SubscriptionKey/MerchantSerialNumber`, egne Key
Vault-hemmeligheter) holder Vipps sin egen test-/MT-merchant-avtale. Brukeren fant og satte selv
disse verdiene via `azd env set` — de endte først (naturlig nok, siden det var de eneste
variabel-navnene som fantes før denne runden) i de GAMLE `VIPPS_CLIENT_ID`-variablene; disse ble
flyttet til de nye `VIPPS_BETATEST_*`-variablene, og de gamle ble deretter overskrevet med
ekte produksjonsverdier kopiert fra `testbase-test` sitt azd-miljø.

**`erBeta`-parameteren er bevisst en STRENG ("true"/"false"), ikke `bool`, i Bicep** — `azd` sin
parameter-substitusjon (`main.parameters.json`) skjer INNI en JSON-strengverdi (`"value":
"${MILJO_ER_BETA}"`), så en ekte `bool`-typet Bicep-parameter ville fått en JSON-STRENG tildelt
uansett, med usikker ARM-typekonvertering. Alle andre parametre i denne malen (inkl. `vippsMiljo`,
som er konseptuelt akkurat like binært) er av samme grunn strenger, ikke bool — fulgte samme
mønster her i stedet for å innføre en ny, mulig skjør presedens.

**Reell DI-bug fanget under verifisering, ikke sendt til produksjon:** `IVippsClient` er
registrert som **Singleton** (uendret prinsipp — `VippsPaymentClient` cacher Vipps sitt
tilgangstoken i minnet på tvers av forespørsler). `BetaSwitchingVippsClient` sitt første utkast
injiserte `BetaInnstillingService` (Scoped, bruker `AppDbContext`) DIREKTE i konstruktøren — kastet
`InvalidOperationException: Cannot resolve scoped service ... from root provider` i det øyeblikket
et ekte kall skjedde (ikke ved oppstart — en singleton-registrering via factory-delegat med
`sp.GetRequiredService<T>()` INNI lambdaen unngår `ValidateOnBuild` sin statiske sjekk helt, siden
containeren ikke kan analysere kallet i forkant). Fikset ved å injisere `IServiceScopeFactory` og
opprette en kortlevd scope PER kall kun for å lese databaseraden — bevarer singleton-levetiden
(og dermed tokens-cachen) på selve Vipps-klient-instansene.

**Verifisert reelt ende-til-ende, både lokalt og på selve beta-appen i Azure:**
- Lokalt (simulert `Miljo__ErBeta=true` + falske nøkler): alle tre Vipps-moduser byttet korrekt
  mellom `MockVippsClient` (umiddelbar suksess) og ekte HTTP-kall til hhv. `apitest.vipps.no` og
  `api.vipps.no` (begge feilet forventet med et ekte 400 fra Vipps sin server, siden nøklene var
  falske) — bekrefter switching-logikken uavhengig av ekte legitimasjon.
- På selve beta-appen (`app-testbase-uonprpnwr4eum.azurewebsites.no`), med de EKTE Vipps
  test-nøklene brukeren fant: byttet til Test-modus og trigget `/BetalingTest/Vipps` — fikk en
  ekte, gyldig signert Vipps-redirect-URL (`pay-mt.vipps.no` med riktig `merchantSerialNumber` i
  token) tilbake. Beviser at legitimasjonene faktisk fungerer, ikke bare at koden kompilerer.
  Stripe Test-modus verifisert tilsvarende (ekte `pi_..._secret_...` PaymentIntent fra Stripes
  testmodus-API). Satt tilbake til Mock/Mock (trygg standard) etter verifisering.
- Fanget en ekte `azd deploy`-staleness (kjent fallgruve i CLAUDE.md — "SUCCESS" uten at koden
  faktisk endret seg): første deploy til beta viste IKKE den nye Betalingsmodus-seksjonen i det
  hele tatt. Løst med et andre `azd deploy`, som CLAUDE.md-fallgruven forutsier.
- Full regresjonskjøring (`dotnet test`, 36/36 grønne) etter alle endringene — live/lokal dev sin
  kodesti er bevist uendret.

**Docker Desktop-fallgruve oppdaget:** hele Docker-daemonen (ikke bare containeren) hadde stoppet
et sted i denne økten uten noe eksplisitt signal utover at `dotnet ef migrations add` feilet med
"Unable to connect to any of the specified MySQL hosts" — `docker compose ps` i seg selv feilet med
en npipe-tilkoblingsfeil (`Docker Desktop.exe` fantes under
`%LOCALAPPDATA%\Programs\DockerDesktop`, ikke `C:\Program Files`). Løst ved å starte
`Docker Desktop.exe` på nytt og vente på at daemonen ble klar før containeren kunne startes.

**Bevisst utsatt/åpent etter denne runden:**
- DNS for `beta.psytest.no` (CNAME + `asuid.beta`-TXT) — brukerens handling, se over.
- Nattlig database-synk produksjon→beta (bevart UID, tilfeldig syntetisk personnummer generert
  MED BETAS EGEN DataProtection-nøkkel — ALDRI kopiert/delt fra live) — designet og avtalt i
  samtale, IKKE implementert ennå.
- Ekte BankID for admin/behandler sin FELLES innloggingsside på beta (pasient forblir Mock alltid)
  — avtalt, men IKKE startet: `IBankIdProvider` sitt synkrone ett-kalls-grensesnitt passer ikke en
  ekte OIDC-redirect-flyt, krever en egen påloggingsvei parallelt med (ikke gjennom) det
  eksisterende grensesnittet. Trenger også: hvilken Idura-claim som faktisk er personnummeret
  (ikke undersøkt ennå), og TO ekte personnummer (ett for admin-testkontoen, ett for
  behandler-testkontoen — samme "høyeste rolle vinner"-kollisjon som gjelder mock-BankID i dag
  gjelder identisk for ekte BankID).
- Selve deploy-arbeidsflyt-endringen ("push til beta FØR live, live kun når bruker eksplisitt ber
  om det, referert til som 'push til azd'/'live'") — ingen kodeendring nødvendig for dette, kun en
  avtalt rutine for FREMTIDIGE `azd deploy`-kommandoer i denne samtalen: `azd env select
  testbase-beta` er nå default (aktivt miljø), MÅ eksplisitt `azd env select testbase-test` for å
  treffe live.

## Ekte BankID for admin/behandler (beta) (2026-09-18)

Fra `bugs_features.txt`/samtale — bruker presiserte kravet til: fake BankID for pasienter (uendret,
alltid), EKTE BankID for admin/behandler sin FELLES innloggingsside (`Pages/Konto/LoggInn`), KUN
på beta, "nok til å bevise at BankID-proof-of-concept fungerer."

**Ny, parallell påloggingsvei — IKKE en endring av `IBankIdProvider`.** Som varslet i forrige
runde: `IBankIdProvider.AuthenticateAsync()` er ett synkront kall, uforenlig med en ekte
OIDC-redirect (spenner over to separate HTTP-forespørsler). Løsning: `OnPostAsync` på
`Pages/Konto/LoggInn.cshtml.cs` sjekker nå `HarEktBankIdAsync()` (er `"BankIdInnlogging"`-schemaet
registrert? — kun sant på beta, se under) FØR den kaller `IBankIdProvider` i det hele tatt. Sann →
`Challenge(...)` til Idura i stedet for mock-kallet. Alt ETTER selve identitetsbekreftelsen
(oppslag admin-før-behandler, betrodd enhet, 2FA-start) er nå trukket ut til en ny delt
`ProfesjonellInnloggingService` (`TestBase.Web/Security/`) — brukt av BEGGE veiene, slik at de
garantert oppfører seg identisk i alt annet enn selve identitetskilden.

**Nytt OIDC-schema `"BankIdInnlogging"`** (Program.cs), registrert KUN når `Miljo:ErBeta == "true"`
OG Idura-nøklene er satt — egen `CallbackPath` (`/signin-bankid-innlogging`, egen
StagingGate-unntak i `BankIdCallbackStier`) atskilt fra det eksisterende diagnostiske
`"BankIdTest"`-schemaet (som fortsatt bare dumper claims, uendret). `OnTokenValidated` prøver
`ssn`-claimen først (samme navn som scopet), faller ellers tilbake til en "11 siffer"-heuristikk
blant ALLE claims som kom tilbake — **den faktiske claim-typen er IKKE bekreftet ennå**, siden det
krever en ekte interaktiv Idura-innlogging i nettleser, noe verken forrige eller denne runden
kunne gjøre (se "Bevisst utsatt" i forrige seksjon). `HuskMeg`/`ReturnUrl` rundtures via
`AuthenticationProperties.Items` (kryptert inni `state`-parameteren, ikke TempData) siden de må
overleve HELE redirect-til-Idura-og-tilbake-runden; selve personnummeret (kjent først i
callbacken) mellomlagres i TempData og hentes av en ny side, `Pages/Konto/BankIdFullfor.cshtml`,
som fullfører innloggingen via samme `ProfesjonellInnloggingService`.

**Reell bug fanget av regresjonstestene, IKKE sendt til produksjon:** `ProfesjonellInnloggingService`
sin "tilbake til der brukeren kom fra"-fallback brukte `new RedirectToPageResult(fallbackPage,
fallbackArea, null)` — men `RedirectToPageResult` sin tredje POSISJONELLE konstruktørparameter er
`pageHandler` (en sidehandler som "OnPostFoo"), IKKE et MVC-område. Den opprinnelige koden brukte
`RedirectToPage(fallbackPage, new { area = fallbackArea })`-HJELPEMETODEN, som legger `area` i
`routeValues`, et helt annet sted. Ga en 500 ("No page named '/Pasienter/Index' matches the
supplied values") for enhver ALLEREDE BETRODD behandler sin ANDRE innlogging (2FA hoppet over,
rett til denne fallbacken) — fanget av `HeleFlytenTests.cs` sin ende-til-ende-test, IKKE oppdaget
ved kompilering (begge er gyldige overlands, bare feil betydning). Fikset til å bruke
`routeValues: new { area = ... }` eksplisitt. Full regresjon (36/36) grønn etterpå.

**Verifisert så langt som mulig UTEN en ekte interaktiv BankID-innlogging i nettleser** (noe kun et
menneske kan fullføre — Idura krever ekte brukerinteraksjon, ikke noe curl kan late som):
- Lokalt, med `Miljo:ErBeta=true` (simulert) og Idura-nøklene i `dotnet user-secrets` (ALDRI på
  kommandolinjen — auto-mode-klassifisereren blokkerte nettopp det første forsøket som en reell
  legitimasjonslekkasje, korrekt): BankID-knappen ga en fullstendig, korrekt utformet
  OIDC-autorisasjonsforespørsel til `psytest.test.idura.broker` (riktig `redirect_uri`, `scope`,
  `acr_values`, PKCE, nonce, state). Uten `Miljo:ErBeta`: samme knapp forble på mock-veien
  (200, ikke 302) — bekrefter at live/lokal dev er upåvirket.
- På selve beta-appen i Azure: samme sjekk, med RIKTIG HTTPS-`redirect_uri` mot den faktiske
  beta-vertsnavnet. Krevde en `azd provision` (ikke bare `azd deploy`) for at de allerede
  `azd env set`-satte BankID-/Seed-verdiene faktisk skulle nå App Service-innstillingene — satt
  også over `Seed:AdminPersonnummer` m.fl. (SAMME ekte identitet som allerede brukes på live for
  nøyaktig dette formålet) slik at beta nå har en administrator-konto klar for et ekte
  BankID-forsøk.

**Bevisst utsatt/åpent — krever brukerens EGEN interaktive handling, ikke noe som kan skriptes:**
- Selve den første ekte Idura-innloggingen i nettleser (via `/Konto/LoggInn` sin BankID-knapp,
  eller `/BankIdTest/Start` for kun en claims-dump uten å faktisk logge inn noen) — nødvendig for å
  bekrefte at `ssn`-claim-gjetningen faktisk stemmer. Hvis den IKKE stemmer henter fallback-
  heuristikken (11 sifre) trolig riktig verdi likevel, men dette er ubekreftet.
  `BankIdFullfor.cshtml.cs` sin feilmelding viser en full claims-dump hvis personnummer ikke finnes
  i det hele tatt, til feilsøking.
- Behandler-kontoen for å bevise DEN andre rollen: samme "høyeste rolle vinner"-oppslag som gjelder
  mock-BankID gjelder identisk her — admin-kontoen (brukerens eget personnummer) vil ALLTID vinne
  over en behandler med SAMME personnummer. Trenger en ANDEN reell person/personnummer registrert
  som behandler på beta for å faktisk bevise behandler-veien separat — kan gjøres med EKSISTERENDE
  verktøy (behandler-invitasjon + selvregistrering, med det dev-only `PersonnummerOverride`-feltet
  satt til det andre personnummeret under selve registreringen), ingen ny kode nødvendig for dette.

## Google Safe Browsing flagget beta-appen (2026-09-19) — samme mønster som før, samme fiks

Bruker fikk Chrome sin røde "Dangerous site"-advarsel på beta rett etter at ekte BankID-innlogging
(forrige seksjon) var live der. NØYAKTIG samme årsak som først dokumentert 2026-09-02 for
`testbase-test` (se "Google Chrome/Safe Browsing flagget test-appen"): en ekte "Logg inn med
BankID"-knapp på en generisk, ubrandet `azurewebsites.net`-adresse uten nettverksrestriksjon
matcher phishing-mønstergjenkjenning. `StagingGate` alene hindrer ikke dette — sperren beskytter
INNHOLDET bak innlogging, ikke det faktum at siden i seg selv ser ut som et BankID-phishing-forsøk
ved skanning.

**Samme fiks som sist, brukt direkte uten omvei:** IP-baserte access restrictions lagt til på
`app-testbase-uonprpnwr4eum` (beta), BÅDE hovedsiden og `--scm-site` (Kudu/deploy-endepunktet) —
kun brukerens IP (`51.175.216.201/32`, priser 100, "AllowGaute") tillatt, alt annet nektet
("Deny all" default). IP-en var UENDRET fra 2026-09-02-hendelsen (bekreftet via `api.ipify.org`
FRA SAMME MASKIN/NETTVERK denne økten kjører på — pålitelig FORDI Claude Code-økten faktisk kjører
lokalt på brukerens egen maskin, ikke i en sky-sandkasse atskilt fra brukerens nettverk). Verifisert
at siden fortsatt svarer korrekt (401 fra StagingGate, ikke en nettverksblokkering) etter
restriksjonen ble lagt til.

**Samme kjente, ikke-kodifiserte begrensning som for live gjelder nå identisk for beta**: satt via
CLI (`az webapp config access-restriction add`), IKKE i `infra/resources.bicep` — overlever en
`azd provision` (ikke del av `appSettings`-fullerstatnings-fallgruven), men er heller ikke
automatisk gjenskapt ved en fersk `azd provision` et helt NYTT sted, og må oppdateres manuelt
dersom brukerens IP noensinne endrer seg (samme åpne punkt som for live, se "Åpne punkter til
senere faser" — bør parameteriseres i Bicep via en azd-miljøvariabel for BEGGE miljøene samtidig
når dette tas tak i).

**Rettelse samme dag — IP-restriksjonen er trolig IKKE (lenger) det som faktisk beskytter live.**
Bruker påpekte (korrekt) at hen når live UTEN noen IP-restriksjon og uten å bli flagget. Sjekket
`az webapp config access-restriction show` for `app-testbase-tk46vyxboocho`: hovedsiden står nå på
**"Allow all access"** — IP-restriksjonen fra 2026-09-02 er borte derfra (kun `--scm-site`
sin restriksjon overlevde), mest sannsynlig ryddet vekk av nettopp den manglende
Bicep-kodifiseringen punktet over advarer om. Live er altså i praksis åpent for hele internett nå,
beskyttet kun av `StagingGate`, OG er likevel ikke flagget på nytt.

Dette peker mot en annen, mer sannsynlig forklaring enn IP-restriksjon: **live har nå et ekte,
kjøpt domene** (`www.psytest.no`), ikke lenger en rå `azurewebsites.net`-adresse — nøyaktig det
opprinnelige phishing-varselet (2026-09-02) pekte ut som triggeren ("...driftet på en generisk,
ubrandet azurewebsites.net-adresse..."). Beta mangler fortsatt sitt eget domene
(`beta.psytest.no`, DNS-oppsett pågår) og sitter derfor fortsatt på nøyaktig det mønsteret som
opprinnelig ble flagget. **Konklusjon: IP-restriksjonen lagt til på beta i dag er fornuftig
dybdeforsvar, men er trolig IKKE det som faktisk fjerner/hindrer flagget — å fullføre
`beta.psytest.no`-DNS-en er sannsynligvis den faktiske fiksen.** Ikke fjernet noe basert på denne
usikkerheten (billig å beholde begge), men bør ikke stole blindt på IP-restriksjon alene for
fremtidige nye miljøer av samme type.

## Ekte BankID for admin/behandler, del 2: eget flagg + pushet til LIVE (2026-09-19)

Bruker ba om å teste ekte BankID på LIVE i stedet for å vente på beta sin DNS (StagingGate-varselet
over gjorde beta ubehagelig å teste på akkurat nå). To avgjørelser tatt FØR noe ble pushet til live:

**Skilte "ekte BankID" fra "ErBeta" i et eget flagg (`Miljo:EktBankIdProfesjonell`).** Opprinnelig
gjenbrukte BankID-schemaet `Miljo:ErBeta` — men å skru PÅ det flagget på live for å få BankID ville
SAMTIDIG aktivert betalings-bryteren der, som DEFAULTER TIL MOCK. Live sine EKTE Vipps/Stripe-
betalinger ville stille sluttet å fungere (falt tilbake til Mock) helt til noen manuelt satte dem
til riktig modus på Admin/MinSide — oppdaget FØR noe ble satt på live, ikke i etterkant. Nytt,
uavhengig Bicep-flagg (`ektBankIdProfesjonell`, samme "streng ikke bool"-mønster som `erBeta`, se
begrunnelsen i forrige seksjon) styrer NÅ kun hvilken IdP admin/behandler-innlogging bruker — helt
urelatert til betalingsmodus. Satt `MILJO_EKT_BANKID_PROFESJONELL=true` på BEGGE miljøer (live OG
beta, siden beta sin BankID ellers ville sluttet å virke stille ved neste deploy dit — den brukte
inntil nå `Miljo:ErBeta`, som fortsatt er `true` der, men koden sjekker ikke lenger den variabelen
for dette formålet).

**Bevisst IKKE fjernet StagingGate fra live**, til tross for at bruker foreslo det. Live kjører
fortsatt med `ASPNETCORE_ENVIRONMENT=Development` (kjent, ikke løst åpent punkt) — det betyr at
`PersonnummerOverride`-auth-bypasset (se kjente fallgruver i CLAUDE.md) og `AdminId+passord`-
utvikler-innloggingen er AKTIVE på live akkurat nå, beskyttet UTELUKKENDE av StagingGate. Å fjerne
gaten nå ville gjort admin-kontoovertakelse mulig for hvem som helst som fant innloggingssiden, en
vesentlig annen (og alvorligere) risiko enn "ingen ekte pasientdata ennå" alene dekker. Bruker
trenger uansett ikke gaten fjernet for å teste — StagingGate sin cookie varer 90 dager og
BankID-callback-stiene er allerede eksplisitt unntatt sperren. Fikset til: fjerning av StagingGate
avventer at live faktisk flyttes vekk fra `Development`-modus — et større, eget arbeid siden
migrasjoner i dag kjører AUTOMATISK ved oppstart NETTOPP FORDI `IsDevelopment()` er sann der (se
Program.cs sitt dev-seed-block) — å bare bytte miljønavn uten en egen migrasjonsplan ville stoppet
alle fremtidige skjemaendringer fra noensinne å nå live.

**Pushet ALT denne øktens arbeid til live** (`azd provision` + `azd deploy`, `testbase-test`):
tabellbredde-fiksen, "Lagrer …" over hele appen, Godkjente rapporter-sidene, test-tilgangs-
forespørsler + bulk-godkjenning, planlagt/gjentagende utsending + bakgrunnstjenesten, hele
beta-betalingsbryter-infrastrukturen (inaktiv på live, se over), og nå ekte BankID for
admin/behandler. Full regresjon (36/36, etter en Docker Desktop-omstart — daemonen hadde stoppet
igjen midt i økten, samme som tidligere denne uken) FØR push. Verifisert PÅ selve
`www.psytest.no` etterpå: (1) BankID-knappen gir nå en ekte, korrekt Idura-autorisasjonsforespørsel
med riktig `redirect_uri=https://www.psytest.no/signin-bankid-innlogging`, (2) `Admin/MinSide` sin
betalingsmodus-seksjon er HELT FRAVÆRENDE (bekrefter at Vipps/Stripe forblir i sin opprinnelige,
alltid-ekte kodesti, ingen risiko for at et ekte betalingsforsøk ble trigget for å bevise dette).

## Ekte BankID for admin/behandler, del 3: interaktiv verifisering avsluttet uferdig (2026-09-19)

Bruker forsøkte å fullføre en ekte, interaktiv Idura BankID-innlogging for å bekrefte
`ssn`-claim-gjetningen (se del 1). Kom lenger enn ventet før den stoppet:

- Redirect-URI-registreringen (del 1 sitt åpne punkt) ble løst av bruker selv hos Idura — bekreftet
  ved at feilen `invalid_request: redirect_uri ... is not registered` forsvant, og at flyten
  siden nådde et EKTE BankID-testinnloggingsskjermbilde (`current.aletheia-test.idtech.no`) både
  lokalt og på `www.psytest.no`.
- Test-personnummer opprettes via BankID sitt offisielle `ra-preprod.bankidnorge.no`
  (Test Number Generator + End User-søk) — bekreftet ved websøk (se kilder i samtalen), IKKE noe
  vi bygde. Iduras dokumenterte testflyt bruker et fast engangspassord `otp` + passord `qwer1234`
  for ALLE BankID-er utstedt av `ra-preprod` — dette er den TILTENKTE, støttede testmetoden, ikke
  en begrensning å omgå.
- Bruker genererte en SYNTETISK test-SSN og prøvde `otp`/`qwer1234`, men fikk likevel krevd
  kodebrikke ("keyfab") uansett, til tross for å ha "unlocked BankID på mobil" for identiteten.
  Websøk antydet at en "Synthetic"-avkrysningsboks i Test Number Generator må stå UKRYSSET for å
  støtte biometri/mobil-BankID i stedet for kodebrikke — ubekreftet om dette faktisk løser det,
  bruker valgte å ikke forfølge videre.
- Det finnes en egen "BankID preprod"-mobilapp (adskilt fra den ekte produksjons-BankID-appen),
  men den distribueres IKKE selvbetjent — krever en support-henvendelse til BankID med
  Apple-ID-/Google Play-kontoens e-post for å få tilgang via TestFlight/Play Store. Ikke forfulgt.

**Beslutning: avsluttet uferdig, bevisst.** Bruker valgte å ikke bruke mer tid på synthetic-
identitet-friksjonen. Idura-integrasjonen (schema, redirect, claim-uthenting) anses som FERDIG
BYGGET og deployet til både live og beta (se del 1/2) — det eneste som IKKE er bekreftet er
hvorvidt `ssn`-claimen faktisk er riktig navn (11-sifre-heuristikken er fallback), siden ingen
`OnTokenValidated`-kall noensinne fullførte i praksis. Dette er et reelt, stående åpent punkt:
den ekte BankID-knappen på live/beta vil IKKE fungere for en faktisk bruker før dette er bekreftet
— finner IKKE `ssn` eller et 11-sifret claim, ender brukeren på en feilmelding med full claims-dump
(se `BankIdFullfor.cshtml.cs`) i stedet for å logges inn. IKKE noe å fikse blindt uten data — vent
til en reell person (evt. en fremtidig ekte BankID-produksjonsavtale, eller en løst
synthetic/mobil-friksjon) faktisk fullfører en innlogging, les feilmeldingens claims-dump da, og
juster claim-navnet i `Program.cs` sin `OnTokenValidated` deretter.

## Ekte BankID for admin/behandler, del 4: ekte produksjonssøknad hos Idura, midlertidig reversert (2026-09-20)

Bruker startet den reelle BankID-produksjonssøknaden hos Idura (selvbetjent, ikke `orders@idura.eu`-
e-postveien — Idura sin dashboard har en direkte "signup for production"-side siden de nå eier
Criipto/er en del av BankID BankAxept). Ny produksjonstenant `psytest-no.idura.broker`
(bevisst IKKE et eget custom-domene som `auth.psytest.no` ennå — bruker utsatte det, se
begrunnelsen for hvorfor Idura selv ANBEFALER custom domene for produksjon i samtalen: unngår at
brukeren forlater eget domene under BankID-innlogging), egen produksjons-klient
(`urn:my:application:identifier:8821`). Lagt til som et HELT NYTT, eget konfigurasjonssett
(`BankId:IduraProduksjon:*`, egne Bicep-parametre+Key Vault-hemmelighet), som `Program.cs` NÅ
FORETREKKER for `BankIdInnlogging`-schemaet når satt — ingen ny flagg-avhengighet, ren
konfigurasjons-fallback (satt → produksjon, tomt → fortsatt test-tenanten). ACR-nivå defaultet til
`substantial` for produksjon (ikke `high` som test-tenanten bruker) — "substantial" feilet TIDLIGERE
nettopp fordi test-miljøet manglet en aktivert BankID-app, som en ekte produksjonsbruker vil ha;
IKKE bekreftet ende-til-ende ennå.

**Godkjenningsprosessen tar 10–13 virkedager** (avtalen må gjennom brukerens bank etter at Idura har
signert, se websøk i samtalen) — inntil da fungerer IKKE den nye produksjonstenanten i det hele
tatt. Application-skjemaet krevde bl.a. en begrunnelse for fødselsnummer-tilgang med henvisning til
norsk lov — endte på personopplysningsloven § 12 (bruksgrunnlag) + helsepersonelloven §§ 39–40
(journalføringsplikt) + pasientjournalforskriften § 5 (administrative opplysninger, krever
fødselsnummer) + GDPR art. 9(2)(h) (selve helseopplysningsbehandlingen). Verifisert via websøk,
IKKE en jurist — samme forbehold som resten av compliance-arbeidet i dette prosjektet, se
`docs/compliance-dpia-utkast.md`.

**Midlertidig reversert til Mock BankID på live** mens søknaden er til behandling — bruker påpekte
korrekt at admin/behandler-innlogging på live ville vært BRUTT i mellomtiden (forsøker å nå en
produksjonstenant som ennå ikke er godkjent/aktiv). Løsning: satt `MILJO_EKT_BANKID_PROFESJONELL`
tilbake til `"false"` og kjørt `azd provision` — INGEN kodeendring/redeploy nødvendig, siden dette
alltid var designet som en ren konfigurasjonsbryter (se del 2). `BankId:IduraProduksjon:*`-verdiene
ble stående urørt (helt ufarlig — schemaet blir uansett ikke registrert når hovedflagget er av).

**Reell fallgruve oppdaget: en App Service-innstilling lest i `Program.cs` sin TOPPNIVÅ-kode
(ved oppstart, ikke per forespørsel) tar IKKE effekt bare ved `azd provision`** — appen må faktisk
RESTARTES for å lese inn den nye verdien og re-evaluere `if`-blokken som avgjør om
`BankIdInnlogging`-schemaet registreres. Første verifisering rett etter `azd provision` viste
FORTSATT den gamle (produksjons-)oppførselen; løst med et eksplisitt `az webapp restart`, deretter
bekreftet på nytt (200 OK uten redirect, i stedet for 302 til Idura). Gjelder ETHVER fremtidig
flagg av samme type (lest via `builder.Configuration[...]` i `Program.cs` sin toppnivå-kode, ikke
inni en per-request-lest tjeneste) — husk en eksplisitt restart etter `azd provision` når man
tester en endring av en slik innstilling, ikke bare stol på at provisjonering alene er nok.

**Plan for reaktivering:** når banken har godkjent avtalen (10–13 dager), sett
`MILJO_EKT_BANKID_PROFESJONELL` tilbake til `"true"`, `azd provision` + eksplisitt restart (se
fallgruven over), og verifiser at redirecten går til `psytest-no.idura.broker` med
`acr_values=...substantial`. INGEN kodeendring nødvendig da heller.

## ICD-11-tester: 7 nye innebygde tester (2026-09-20)

Bruker ba om at plattformen blir "ICD-11 Ready" ved å legge til 7 navngitte instrumenter: ITQ,
PDS-ICD-11 (kun selvrapporteringsversjonen), PiCD, PAQ-11(R), IDQ, IAQ og GADIT. Eksplisitt
instruks: bruk offisiell norsk oversettelse der den finnes, oversett selv der den ikke finnes, og
merk enhver EGEN (ikke-offisiell) oversettelse i selve testnavnet med
"(ikke offisielt oversatt – kun til uttesting)" — bevisst norsk frase, ikke den engelske, for å
unngå (c)-problemer. Lokalisering til engelsk er fortsatt bevisst utsatt (kun relevant hvis en
engelsktalende partner dukker opp senere) — se hovedstatusseksjonen.

**Research-fase (før noe kode ble skrevet):** brukte Playwright MCP-nettleserverktøy til å hente
kildemateriale direkte fra rettighetshavernes egne distribusjonssider (ikke gjetning/hallusinert
klinisk innhold) — `integrertbehandling.no` (Tore Willy Lie sin norske ICD-11-kartleggingshub),
`personality.today` (Bo Bach/Martin Sellbom/Youl-Ri Kim/Peter Tyrer sin distribusjonsside for
PDS-ICD-11/PiCD/PAQ-11R), NKVTS (offisiell norsk ITQ), og et fagfellevurdert PMC-fulltekstsøk for
IDQ/IAQ sin eksakte ordlyd (Shevlin et al. 2023, Journal of Clinical Psychology).

**Funn per test — offisiell norsk oversettelse funnet, INGEN "ikke offisielt oversatt"-merking:**
- **ITQ**: full, gratis, åpent tilgjengelig norsk oversettelse (Bækkelund, Sele & Berg 2019, Modum
  Bad), distribuert av NKVTS. Ingen lisensbegrensning.
- **PDS-ICD-11 (selvrapportering)**: offisiell norsk oversettelse (Tore Willy Lie og Lars Lien,
  2022), godkjent av rettighetshaverne (Bo Bach, Martin Sellbom), psykometrisk validert i en 2024-
  studie. Lisens: "til fri bruk etter avtale med forfatterne" — ikke en betingelse for bruk, men en
  høflighets-e-post til dem før bredere bruk er ønskelig (ikke sendt ennå).
- **GADIT**: offisiell norsk oversettelse (2025, Tore Willy Lie m.fl.), distribuert under CC BY-NC
  (ikke-kommersiell bruk i klinikk/forskning). **Bruker har eksplisitt akseptert denne
  lisensrisikoen** og valgt å inkludere testen uendret, med begrunnelsen at platformens
  betalingsmodell er for den kliniske TJENESTEN, ikke videresalg av selve instrumentet. Kommersiell
  distribusjonsforespørsel går ellers til Gary Chung Kai Chan (c.chan4@uq.edu.au).

**Funn per test — INGEN offisiell norsk oversettelse, egen oversettelse laget og merket i navnet:**
- **PiCD**: kun dansk offisiell oversettelse finnes (Bo Bach, Mickey Kongerslev, Erik Simonsen).
  Norsk tekst i denne testen er oversatt av OSS fra dansk, ikke validert.
- **PAQ-11(R)**: brukeren ble bedt om å velge mellom original 17-ledds PAQ-11 (ingen norsk
  forankring i det hele tatt) og PAQ-11R (revidert, norsk forskningsforankring i samme 2024-studie
  som PDS-ICD-11, men ingen ferdig norsk oversettelse) — **valgte PAQ-11R**. Norsk tekst oversatt av
  OSS fra den engelske originalen (© 2022 Youl-Ri Kim og Peter Tyrer), ikke validert.
- **IDQ og IAQ**: ingen norsk oversettelse finnes. Rettighetshaver Philip Hyland ber eksplisitt om å
  bli kontaktet før noen lager en oversettelse. **Bruker valgte**: oversett nå, merk som ikke-
  offisiell, men send en høflighets-e-post til Hyland parallelt (ikke avvent svar) — ikke sendt
  ennå, se "Åpne punkter" nedenfor.

**Implementasjon** (samme mønster som de 8 Helsebiblioteket-testene, `Test.Kode` +
`IInnebygdTestSeeder` + `ITestSkaaringsberegner`):
- Nytt felt `Test.IcdElleveKlar` (bool, migrasjon `LeggTilIcdElleveKlarPaaTest`) + egen
  `TestService.SettIcdElleveKlarAsync` (samme mønster som `SettRapportIntroduksjonAsync` — ingen
  admin-UI ennå, kun satt av seederne). Vises som en enkel "✓ Klar"-kolonne i
  `Admin/Tester/Index.cshtml`.
- **ITQ**: skåring er en ekte diagnostisk algoritme (IKKE en terskelsum) — PTSD krever alle tre
  symptomklynger + funksjonstap, KPTSD krever i tillegg alle tre selvorganiseringsklynger +
  funksjonstap. Fast leddindeksering dokumentert i `ItqSkaaringsberegner`-klassekommentaren.
- **PDS-ICD-11**: hvert ledd har EGNE, unike (bipolare) svaralternativer, ikke en delt Likert-skala.
  Lagrer VISNINGSINDEKS (0..4/0..3) i stedet for endelig poengverdi (2-1-0-1-2 for ledd 1-10) — en
  bevisst avgjørelse for å unngå at to alternativer med samme reelle poengverdi (begge polene =2)
  vises som "valgt" samtidig i utfyllingssiden, siden radioknappenes `checked`-sammenligning i
  `Fyll.cshtml` er verdi-basert (`TestLeddSvaralternativer`). `PdsIcd11Skaaringsberegner` oversetter
  indeks→poeng.
- **IDQ/IAQ**: samme mønster som ITQ — ekte diagnostisk algoritme (antall endosserte ledd + minst
  ett kjernesymptom + bekreftet funksjonstap), ikke en terskelsum.
- **PiCD/PAQ-11R**: dimensjonale trekkprofiler. PiCD har ingen kliniske grenseverdier i kilden
  (indikatorene er derfor nøytralt fremstilt, `Positiv=true` for alle). PAQ-11R har foreløpige
  domenegrenseverdier sitert direkte fra kildedokumentet, inkl. reversert skåring for ledd 1,2,8,9.
- 7 nye enhetstester lagt til `SkaaringsberegnereTests.cs` (spesielt for de mest feilutsatte
  delene: ITQs diagnostiske forgrening, PDS-ICD-11s bipolare indeks→poeng-oversettelse, PAQ-11Rs
  reverserte ledd). Full regresjonssuite: 48/48 grønn etter tillegget.

**Verifisert i ekte nettleser (Playwright), ikke bare enhetstester:** tildelte alle 7 til en
testpasient, fylte ut PDS-ICD-11 (alle 14 bipolare ledd — bekreftet at riktig, og KUN riktig,
alternativ vises som valgt etter lagring, ingen krysstale mellom de to polene som deler poengverdi)
og ITQ (alle 5 sider, inkl. et scenario der kun DSO-kriteriene var oppfylt — bekreftet at
skåringsberegneren korrekt konkluderer "ingen diagnose" i dette tilfellet, ikke bare de to enkleste
tilfellene som er dekket av enhetstestene) helt til "Ferdig".

**Reell, fra før eksisterende site-wide bug oppdaget og fikset underveis:** "Ferdig"-knappen (og
"Neste"/"Lagre") på `Pasientportal/Tester/Fyll` markerte ALDRI en test som fullført når klikket i en
ekte nettleser — kun i `HeleFlytenTests.cs`, som poster skjemadata direkte og aldri kjører
klientsidens JS. Årsak: `wwwroot/js/validering.js` sin "deaktiver alle innsendingsknapper for å
hindre dobbel-klikk"-håndtering (bugliste 2026-09-13 punkt 25) satte `disabled = true` på DEN
KLIKKEDE knappen SYNKRONT inni `submit`-eventet — nettleseren bygger skjemaets entry-list ut fra
knappenes tilstand på innsendingstidspunktet, så den klikkede knappens eget navn/verdi-par (f.eks.
`Handling=Ferdig`) ble stille utelatt fra selve POST-en. Rammet ETHVER flerknapps-skjema med dette
attributtet site-wide (test-utfylling for alle pasienter, ikke bare de 7 nye) — ikke noe innført av
dette arbeidet, men oppdaget FORDI de nye testene ble browser-testet i stedet for kun enhetstestet.
Fikset ved å utsette selve `disabled = true` til `setTimeout(fn, 0)` (neste task), slik at
nettleseren rekker å lese knappens navn/verdi FØRST — lastetekst-visningen skjer fortsatt
umiddelbart. Bekreftet fikset: "Ferdig" markerer nå korrekt fullført i ekte nettleser for både
PDS-ICD-11 og ITQ.

**Deployet til live (2026-09-20):** `azd deploy` mot `testbase-test` (eksplisitt valgt env,
`azd env select testbase-test`, siden `testbase-beta` var/kan være default — se fallgruven i
hovedteksten). Ingen Bicep-endringer nødvendig (kun applikasjonskode + én EF-migrasjon). Migrasjonen
kjører automatisk ved oppstart (`db.Database.MigrateAsync()` i `Program.cs`, som kjører i
`IsDevelopment()`-blokken — live kjører bevisst som "Development", se arkitektur-seksjonen).
Verifisert: `azd deploy` fullførte uten "no App Service deployment status change"-advarselen (kjent
fallgruve), og `az webapp log tail` rett etter viste normal, feilfri drift (ekte DB-spørringer
lykkes, ingen exceptions) — hadde migrasjonen feilet ville hele appen krasjet ved oppstart siden
`MigrateAsync()` avventes før noe annet skjer. IKKE deployet til beta ennå (bevisst utsatt av
bruker — e-postene til rettighetshaverne sendes av bruker selv, ikke AI, før bredere bruk).

**Åpne punkter (fra 2026-09-20, del 1):**
- Høflighets-e-poster ikke sendt ennå — bruker sender disse selv: Bo Bach/Martin Sellbom
  (PDS-ICD-11, PiCD), Youl-Ri Kim/Peter Tyrer (PAQ-11R), Philip Hyland (IDQ/IAQ).

## ICD-11-tester, del 2: pasientvendte navn, PDS-ICD-11-UX, PiCD-ordlyd (2026-09-20)

Etter at bruker begynte å teste på live, kom fem konkrete tilbakemeldinger:

1. **Testnavnene var for lange/tekniske for pasienten.** Alle 7 fikk et kort, pasientvennlig navn
   med forkortelsen til slutt (for at behandler fortsatt kjenner testen igjen): ITQ →
   "Traumescreening ICD-11 ITQ", PDS-ICD-11 → "Personlighetsfunksjon ICD-11 PDS", PiCD →
   "Personlighetstrekk ICD-11 PiCD", PAQ-11R → "Personlighetsscreening ICD-11 PAQ-11R", IDQ →
   "Depresjonsscreening ICD-11 IDQ", IAQ → "Angstscreening ICD-11 IAQ", GADIT →
   "Spillavhengighet ICD-11 GADIT".
2. **"(ikke offisielt oversatt – kun til uttesting)" skal IKKE vises til pasienten** — kun til
   behandler/admin, som faktisk trenger å vite det før de sender testen. Løst med et nytt felt
   `Test.OversettelseNotat` (migrasjon `LeggTilOversettelseNotatPaaTest`), satt for PiCD/PAQ-11R/
   IDQ/IAQ, null for ITQ/PDS-ICD-11/GADIT (som HAR offisiell oversettelse). Vises nå kun i
   `Admin/Tester/Index` (egen "Oversettelse"-kolonne) og i tildelingsflytens sjekkliste (Admin OG
   Behandlerportal sin `Tildel/Tester.cshtml`, både synlig tekst og `data-test-navn` som feeder
   bekreftelsesdialogen) — ALDRI i `Pasientportal/*`, som kun leser det nå forkortede `Test.Navn`.
3. **PDS-ICD-11 sine svaralternativer skal vises som vertikale fullbredde-bokser**, ikke en
   horisontal knapperad — de er for lange setninger til å stå på rekke. Gjenbrukte den eksisterende
   `.svar-rad--stablet`-modifiereren fra MADRS-S (bugliste 2026-09-13 punkt 24) i stedet for å bygge
   noe nytt: `Fyll.cshtml` sin `stablet`-sjekk utvidet til `Test.Kode is "madrs_s" or "pds_icd11"`.
4. **PDS-ICD-11 ledd 1-10 skal ha TILFELDIG visningsrekkefølge på svaralternativene** — den "sunne"
   midtre teksten som alltid ligger i midten er en konvensjon fra PAPIRVERSJONEN av skjemaet, ikke
   klinisk meningsbærende, og et digitalt skjema med den alltid i midten kan gi et
   svarmønster-bias. Ledd 11-14 har derimot en REELL, økende alvorlighetsrekkefølge og skal IKKE
   stokkes. Løst uten skjemaendring: ny `TestLeddSvaralternativer.ParseStokket(svaralternativer, seed)`
   (Fisher-Yates, ren visningsrekkefølge — selve poengverdien følger MED teksten, så skåringen er
   upåvirket), brukt i `Fyll.cshtml` når `Test.Kode == "pds_icd11" && ledd.Rekkefolge <= 10` (alle 14
   PDS-ledd ligger på samme TestSide, så Rekkefolge 1-10 er nettopp ledd 1-10). Frøet kombinerer
   TestTildeling-id og ledd-id, slik at rekkefølgen er STABIL for én besvarelse (Lagre/Neste endrer
   den ikke) men ULIK mellom pasienter.
5. **PiCD: "Meget" → "Helt", "Nøytral" → "Nøytralt"** i skalateksten. Siden PiCD allerede var
   seedet i alle tre miljøer, holdt det IKKE å bare endre kildekoden — ny
   `TestService.OppdaterSvaralternativerForAlleLeddAsync(testId, svaralternativer)` kalt fra
   seederens "allerede finnes"-gren propagerer ordlydsendringen til de 60 allerede lagrede leddene.
6. **ITQ sin intro-tekst på side 1 ("tenk på hendelsen som gir deg mest plager …") gjøres større og
   med mer luft rundt** — ny CSS-klasse `.test-intro` (site.css) på `Test.Beskrivelse`-avsnittet i
   `Fyll.cshtml`: større skrift, bakgrunnsboks, venstre aksent-kant. Generell for ALLE testers
   intro-tekst (samme kodesti), ikke ITQ-spesifikk — brukeren pekte spesifikt på ITQ fordi det var
   testen de så på under live-testing.

**Ny generell mekanisme:** `TestService.SettNavnAsync` (mirrors `SettRapportIntroduksjonAsync`) —
seedernes "allerede finnes"-gren kaller nå denne slik at en navneforenkling faktisk propagerer til
tester som allerede er seedet i beta/live, ikke bare til fremtidige miljøer. Samme prinsipp bak
`SettOversettelseNotatAsync` og `OppdaterSvaralternativerForAlleLeddAsync`.

**Verifisert:** 48/48 enhetstester grønt (uendret). Browser-verifisert i lokalt dev-miljø: alle 7
nye navn vises korrekt både i `Admin/Tester/Index`, tildelingsflyten (med/uten oversettelsesnotat
riktig skjult/vist) og pasientens "Min side"; PDS-ICD-11 fylt ut på nytt — bekreftet vertikale
fullbredde-bokser OG at ledd 1-10s "sunne" midtalternativ IKKE lenger alltid havner i midten (varierer
reelt fra ledd til ledd), mens ledd 11-14 beholder sin faste rekkefølge; PiCD fylt delvis ut —
bekreftet "Helt uenig"/"Nøytralt" i faktisk rendret HTML, ingen "Meget"/"Nøytral" (uten t) igjen.

**Deployet til live OG beta (2026-09-20)** — bruker ba eksplisitt om utsending til begge etter
denne runden med rettelser, i motsetning til forrige runde der beta bevisst ble utsatt.

**Åpne punkter:**
- Høflighets-e-poster ikke sendt ennå — bruker sender disse selv: Bo Bach/Martin Sellbom
  (PDS-ICD-11, PiCD), Youl-Ri Kim/Peter Tyrer (PAQ-11R), Philip Hyland (IDQ/IAQ).
- GADIT, PiCD, PAQ-11R, IDQ, IAQ er verifisert i nettleser KUN opp til utfyllingssiden (rendres
  korrekt, riktig kategori/navn i tildelingsflyten) — selve "fyll ut helt til Ferdig"-løpet er kun
  browser-testet for PDS-ICD-11 og ITQ. Skåringslogikken for alle 7 er dekket av enhetstester.

## Invitasjons- og gruppesystem, fase 1: Gruppe som egen entitet (2026-09-20)

Bruker ba om et helt nytt, stort feature-sett: QR-basert selvregistrering for pasienter (knyttet
til behandler ELLER en spesifikk gruppe), automatisk test-utsendelse ved gruppeinnmelding, en
"prøv systemet"-modus uten BankID/ekte personnummer for konferanse-/markedsføringsbruk, og
aggregert rapportering på tvers av en gruppes besvarelser. Dette er trolig det STØRSTE enkelt-
feature-settet i prosjektet til nå — større enn Partner-systemet. Avklart FØR noe kode ble
skrevet (se samtalen): (1) prøve-/testpasienter lagres i HELT SEPARATE, ikke-vedvarende tabeller,
ALDRI i Pasienter/TestSvar — unngår at journallovens 10-års-lagringsplikt eller
rapportgodkjenningsflyten ved et uhell gjelder syntetiske konferanse-data; (2) rådata for
prøvepasienter beholdes til behandler EKSPLISITT rydder gruppen (ikke automatisk slettet rett
etter utsending) — nødvendig for at den aggregerte rapporten skal fungere i etterkant av en
konferanse; (3) aggregert rapport er ÉN generisk visning (N, snitt/median, skårefordeling, og
kategori-/Indikator-andeler der testen har det) brukt for ALLE tester, ikke skreddersydde
visualiseringer per test; (4) ekte pasienter som melder seg inn via en gruppe sin QR-kode går
gjennom NØYAKTIG samme betalingsgate som en manuell tildeling — ingen spesialbehandling.
Bygges i 5 faser, verifisert og deployet én om gangen (bruker sitt eksplisitte valg), ikke som
én stor batch.

**Fase 1 (denne omgangen): Gruppe som egen entitet.** Frem til nå var "gruppe" bare en fritekst-
streng (`Pasient.Gruppenavn`) — helt uten struktur, ingen kobling til tester. Ny `Gruppe`-entitet
(eier = BehandlerId, `QrToken` forberedt for fase 2, `ErArkivert` soft-delete) + ny
`GruppeTestTilordning` (mange-til-mange gruppe↔test, satt ved opprettelse, redigerbar senere) +
`GruppeService` (CRUD, `FinnEllerOpprettAsync` for CSV-gruppeimportens eksisterende
"gruppenavn,navn,email,sms,pnr"-format — se under, `HentPasientAntallPerGruppeAsync`).
Nye sider: `Behandlerportal/Grupper` (full CRUD — Index/Ny/Rediger/Arkiver, med samme
kategori-tre-avkrysning for test-tilordning som `Tildel/Tester.cshtml` bruker) og `Admin/Grupper`
(KUN oversikt på tvers av alle behandlere — samme arbeidsdeling som `Admin/Pasienter`, faktisk
redigering skjer hos eiende behandler).

**Migrasjon med ekte datamigrering** (ikke bare skjema): den autogenererte migrasjonen satte
`DropColumn("Gruppenavn")` FØRST i rekkefølgen — måtte håndskrives om til: opprett `grupper`-
tabellen → legg til `Pasient.GruppeId` → SQL-datamigrering (én ny `Gruppe`-rad per unike
(Gruppenavn, BehandlerId)-par som faktisk var i bruk, med `QrToken` generert via MySQL sin
`UUID()` siden dette skjer i ren SQL, ikke applikasjonskode) → koble `Pasient.GruppeId` til riktig
gruppe → FØRST DA droppes `Gruppenavn`-kolonnen. Verifisert direkte mot lokal dev-database: den
eksisterende "Gruppe A" (Kari/Ola Nordmann, eid av Per Behandlersen) ble korrekt til én ny
`Gruppe`-rad med gyldig `QrToken`, og begge pasientene fikk riktig `GruppeId`.

**Berørte visningssteder oppdatert** (alle brukte tidligere `p.Gruppenavn` direkte): Admin/
Behandlerportal sine `Pasienter/Index.cshtml` (søk + kolonne), `Tildel/Pasienter.cshtml` (kolonne),
`Pasienter/Detaljer.cshtml`, og `Pasienter/Rediger.cshtml` (fritekstfelt → nedtrekksliste av
behandlerens egne grupper, med lenke til gruppeadministrasjon). Alle via `.Include(p => p.Gruppe)`
— EF Core opprettet automatisk en ekte FK-constraint (`GruppeId` + navigasjonsegenskapen `Gruppe`
matcher konvensjonen), i motsetning til `Pasient.BehandlerId` som bevisst ALDRI har hatt en
tilsvarende formell FK i dette prosjektet.

**Verifisert:** 48/48 enhetstester grønt (én oppdatert — `RedigerTests.cs` postet tidligere en rå
"Gruppenavn"-tekststreng, oppretter nå en ekte `Gruppe` først og poster `GruppeId`). Browser-
verifisert i lokalt dev-miljø (via "Bytt modus" → Behandler): opprettet en ny gruppe med ett
tilordnet WHO-5, bekreftet kategori-treet på rediger-siden automatisk åpner kategorien med en
allerede tilordnet test og viser avkrysningen korrekt forhåndsutfylt.

**IKKE bygget ennå (fase 3-5):** "prøv systemet"-modus, aggregert rapportering,
betalingsgate-verifisering for gruppe-tildelte tester. Se docs/prosjektbeskrivelse-original.md-
tilsvarende avklaringer i samtalen for full spesifikasjon av gjenstående faser.

## Invitasjons- og gruppesystem, fase 2: QR-basert selvregistrering (2026-09-20)

**To selvstendige QR-typer, samme underliggende mekanisme:** (1) behandlerens EGEN, gruppeuavhengige
invitasjon (`Behandler.PasientInviteQrToken`, ny kolonne+migrasjon) — plassert på
`Behandlerportal/MinSide` (bruker sitt eksplisitte ønske), registrerer en ny pasient direkte hos
behandleren, INGEN automatisk testutsending; (2) en gruppes invitasjon (`Gruppe.QrToken`, allerede
lagt til i fase 1) — plassert på `Behandlerportal/Grupper/Rediger/{id}` (bruker sitt eksplisitte
ønske: "under Grupper->gruppe, der du redigerer"), registrerer pasienten i DEN gruppen OG sender
automatisk gruppens tilordnede tester med det samme (via eksisterende `TestTildelingsService`,
SAMME betalingsgate som en manuell tildeling — ingen spesialbehandling, jf. avklaringen før
byggingen startet).

**Selve QR-bildet:** ny `QrBildeGenerator` (Security/) bruker BEVISST QRCoder sin rene,
administrerte `PngByteQRCode` — IKKE `QRCode`-klassen (som krever System.Drawing.Common/GDI+ og
IKKE fungerer pålitelig på Linux-baserte Azure App Service-containere, som denne appen kjører på).
QR-bildet serveres via en named page handler (`?handler=Qr`) rett fra samme side som viser koden,
ingen egen fil/lagring. Begge QR-typer har en "Regenerer"-knapp som umiddelbart invaliderer
gammel kode/lenke (advarer først om at trykt materiale må byttes ut).

**Ny offentlig side `Pages/BliPasient` (route `/BliPasient/{b|g}/{token}`):** i MOTSETNING til den
eksisterende `PasientRegistrering/Fullfor` (som fullfører en FORHÅNDSOPPRETTET invitasjon/pasient-
rad) oppretter denne siden en helt ny pasient i ÉTT steg — ingen forhåndskjent kontaktinfo, ingen
egen behandler/admin-handling før besøket. Ny `PasientInvitasjonService.RegistrerViaQrAsync` +
`HarAnnenPasientMedPersonnummerAsync` (samme personnummer-unikhetssjekk som
Behandlerportal/Pasienter/Rediger — ekstra viktig her siden dette er en helt åpen, offentlig
overflate). Samme bot-vern (honeypot + minimumstid) og samtykkeavtale som Fullfor-siden.

**KRITISK OPPDAGELSE underveis, løst:** StagingGate sperrer i praksis HELE appen bak en delt
hemmelighet for enhver besøker uten cookien fra før (kun BankID-callback og betalings-webhooks var
unntatt fra før). Dette ville ha gjort QR-funksjonen totalt virkningsløs på live/beta — en
fullstendig ukjent person som skanner en kode ville møtt "skriv inn nøkkelen"-veggen FØR de
noensinne når registreringsskjemaet. Løst med samme mønster som BankID-callbacken: `/BliPasient`
lagt til som et nytt, hardkodet (ingen wildcard) unntak i `StagingGate.cs`. Selve sikkerheten på
denne overflaten ligger i siden selv (gyldig behandler-/gruppe-QR-token kreves, bot-vern, og
resultatet er uansett bare én ny, uverifisert pasientrad hos ÉN bestemt behandler).

**Relatert, IKKE rettet i denne omgangen — flagget i kildekodekommentar og her for senere
vurdering:** samme oppdagelse avslørte at `/PasientRegistrering/Fullfor`, `/Inviter/Fullfor`,
`/Inviter/Verifiser` og `/Pasientportal/Konto/LoggInn` sannsynligvis har NØYAKTIG samme problem på
live i dag — en ekte invitert pasient eller behandler som klikker sin egen invitasjonslenke fra en
enhet uten StagingGate-cookien fra før, ville også møtt tilgangssperren. Dette er IKKE noe denne
omgangen introduserer; det gjelder allerede eksisterende offentlige sider, og bør vurderes helhetlig
(enten en bredere unntaksliste, eller en fornyet vurdering av om StagingGate fortsatt trengs på
live nå som ekte BankID er på vei) — IKKE gjort ennå, krever et bevisst valg fra bruker.

**Verifisert lokalt, ende-til-ende:** begge QR-typer fulgt gjennom faktisk nettleser-flyt — behandler-
QR ga en ny pasient korrekt koblet til behandleren (ingen gruppe); gruppe-QR ga en ny pasient korrekt
koblet til BÅDE behandleren OG gruppen, MED WHO-5 automatisk tildelt (status "Tildelt", verifisert
direkte i databasen) — akkurat som spesifisert. 48/48 enhetstester fortsatt grønt.

**Oppfølging samme dag, etter tilbakemelding:** pasienten som melder seg inn i en gruppe MED
tilordnede tester går nå RETT videre til selve utfyllingen (`Pasientportal/Tester/Fyll/{id}`) i
stedet for å måtte vente på og klikke SMS-/e-post-lenken — pasienten sitter jo allerede midt i
registreringen på egen enhet. Løst ved å logge pasienten inn direkte (`AuthSignIn.LoggInnAsync`,
samme mekanisme som BankID-innlogging ville satt opp — personnummeret er uansett allerede
egen-oppgitt i skjemaet) og redirecte til tildelingen for FØRSTE tilordnede test, hentet fra
`TildelOgVarsleAsync` sitt returverdi (`TestLenke` fikk et nytt `TildelingId`-felt for dette).
SMS/e-post sendes fortsatt som normalt (reserve for senere besøk fra annen enhet), bare selve
denne økten hopper over den omveien. Gjelder KUN gruppe-QR-flyten med tilordnede tester —
behandler-QR (ingen gruppe) og en gruppe uten tester viser fortsatt bare bekreftelsessiden, siden
det da ikke finnes noen test å hoppe rett til. Betalingsgaten er UENDRET (samme som enhver annen
tildeling) — denne endringen påvirker kun HVORDAN pasienten kommer seg til Fyll-siden, ikke hva
som skjer der. Verifisert i ekte nettleser (Playwright): registrering → direkte til
`/Pasientportal/Tester/Fyll/{id}` med testinnholdet synlig, innlogget som riktig pasient, ingen
mellomsteg.

## StagingGate fjernet fra live, nytt flagg for utviklingssnarveier (2026-09-20)

**Beslutning fra bruker, ordrett:** "I have decided to remove the staging gate protection from
live. That is the point of live." StagingGate er fjernet fra `www.psytest.no` og beholdt UENDRET
på beta (`beta.psytest.no`) — samme delte nøkkel som før, ingen endring der.

**Hvorfor dette IKKE var en trygg ren sletting:** live kjører fortsatt `ASPNETCORE_ENVIRONMENT=
Development` (bevisst, kun for automatisk databasemigrering ved oppstart — se dev-seed-blokken i
Program.cs og tidligere seksjoner i denne loggen). StagingGate var inntil nå det ENESTE som hindret
en hvilken som helst besøker i å nå flere `IsDevelopment()`-gatede snarveier som ALLE ville blitt
reelt, offentlig utnyttbare i det øyeblikket StagingGate falt bort. Full gjennomgang (`grep -rn
"IsDevelopment()"`) fant:

1. **`PersonnummerOverride`** på BÅDE admin/behandler- og pasient-innloggingssiden — et
   ubetinget auth-bypass allerede kjent fra tidligere (se "Google Chrome/Safe Browsing flagget
   test-appen" lenger opp i denne loggen), men fortsatt kun gatet på `IsDevelopment()`.
2. **KRITISK, NYOPPDAGET under denne gjennomgangen:** `Pages/Konto/LoggInn.cshtml.cs` sin
   `OnPostPassordAsync` (AdminId+passord, sekundær innloggingsvei) hadde INGEN
   `IsDevelopment()`-sjekk i selve handleren i det hele tatt — skjemaet var kun skjult i viewet
   (`@if (Env.IsDevelopment())`), nøyaktig samme fallgruvemønster som CLAUDE.md allerede advarer
   mot for `PersonnummerOverride`, men denne gangen ikke fanget opp. Program.cs sin dev-seed-blokk
   oppretter (idempotent, kun hvis `Administratorer`-tabellen er tom ved første oppstart) en konto
   `AdminId="dev-admin"` med et HARDKODET passord `"utvikler123"` — synlig i klartekst i denne
   samme, offentlige GitHub-repoen. Denne kontoen logger inn med `UserRole.Utvikler` (full
   utvikler-/superadmin-tilgang). Siden live har kjørt med `ASPNETCORE_ENVIRONMENT=Development`
   helt siden det første oppsettet, historisk FØR brukerens egen ekte admin-konto (`Seed:
   AdminPersonnummer`) fantes, er det høyst sannsynlig at denne `dev-admin`-raden faktisk ble
   opprettet i live sin ekte database på et tidspunkt og fortsatt ligger der. Uten StagingGate ville
   ENHVER besøker på internett — uten å kjenne noe annet enn denne offentlige kildekoden — kunnet
   logge inn som full utvikler på live. Dette var IKKE noe denne omgangen introduserte; det har
   ligget latent siden live først fikk `ASPNETCORE_ENVIRONMENT=Development`, og ble først synlig som
   et akutt problem i det StagingGate skulle fjernes. **Oppfølgingspunkt til bruker:** siden Claude
   Code ikke har brannmuråpning til å spørre live sin MySQL Flexible Server direkte (se kjent
   fallgruve om `AllowAzureServices`), BØR administrator-listen på `/Administratorer` sjekkes manuelt
   for en gjenværende "dev-admin"-rad og denne arkiveres/slettes om den finnes — kodefiksen under
   gjør den uansett ubrukelig fremover, men raden selv er ikke fjernet fra databasen av denne
   omgangen.
3. **2FA-koden vist rett i nettleseren** (`ProfesjonellInnloggingService.FullforAsync`, begge
   grener — administrator og behandler) og i `Pages/Inviter/Fullfor.cshtml.cs`
   (`TempData["DevMobilKode"]`) — siden `MockSmsSender`/`MockEmailSender` kun logger til konsollen
   (se kjent fallgruve), var dette ment som en lokal utviklingssnarvei, men uten StagingGate ville
   det gjort andre-faktoren VERDILØS for enhver på internett: koden ble bare vist tilbake i samme
   respons, ingen SMS/e-post trengte noensinne å bli avlyttet.
4. **Personnummer forhåndsutfylt i URL** (`Program.cs` sin `InnloggingsstiForAsync`, for
   pasientportal-testlenker) — la et pasient-personnummer i en `?personnummer=`-spørrestreng.
   Lavere alvorlighetsgrad (mottakeren har uansett allerede lenken), men fortsatt uheldig
   logghygiene på et fullt offentlig miljø.
5. **Diagnostiske sider** (`Pages/BankIdTest/*`, `Pages/BetalingTest/*`) — trigger ekte
   Idura-BankID-testflyter og ekte Vipps/Stripe-betalingsforsøk. Ikke et "gratis penger"-hull (krever
   uansett å gå gjennom Vipps/Stripe sin egen autentisering), men en utilsiktet offentlig
   diagnoseoverflate mot ekte tredjepartskontoer.
6. **`UseHsts`/`UseExceptionHandler` var aldri aktive på live** — begge lå bak
   `if (!app.Environment.IsDevelopment())`, som siden live kjører "Development" alltid var usann
   der. Ingen HSTS-header, fulle utviklerfeilsider ved unntak — usynlig bak StagingGate før, men
   ville blitt direkte eksponert nå.

**Løsning: nytt, EGET flagg `Miljo:TillatUtviklingsSnarveier`** (`TestBase.Web/Security/Miljo.cs`),
bevisst frikoblet fra `ASPNETCORE_ENVIRONMENT`/`IsDevelopment()` — samme designprinsipp som
`Miljo:ErBeta`/`Miljo:EktBankIdProfesjonell` fra tidligere (se de respektive seksjonene i denne
loggen): live beholder `ASPNETCORE_ENVIRONMENT=Development` (kun for auto-migrering), men dette nye
flagget er EKSPLISITT `"false"` der. Sant lokalt (`appsettings.Development.json`) og på beta
(`Miljo__TillatUtviklingsSnarveier=true` i infra/resources.bicep, satt via azd-miljøvariabelen
`MILJO_TILLAT_UTVIKLINGSSNARVEIER`), usant/fraværende på live. Alle 6 punktene over er nå gatet på
dette flagget i stedet for `IsDevelopment()`:

- `PersonnummerOverride` (begge innloggingssider, view OG handler).
- `OnPostPassordAsync` avviser nå UNNTAKSLØST med samme feilmelding som "fant ingen konto" når
  flagget er av — verken captcha-sjekk eller kontooppslag når kjøre i det hele tatt, og
  skjemaet er fortsatt skjult i viewet. Samme feilmelding uansett årsak, for ikke å avsløre at
  mekanismen finnes på et miljø der den er skrudd av.
- 2FA-kode-passthrough i `ProfesjonellInnloggingService` og `Inviter/Fullfor.cshtml.cs`.
- Personnummer-i-URL i `Program.cs`.
- Alle seks diagnostiske sider (`BankIdTest/Start+Resultat`, `BetalingTest/Vipps+Stripe+
  VippsResultat+StripeResultat`) returnerer nå `NotFound()` når flagget er av.
- `UseHsts`/`UseExceptionHandler` er nå BETINGET AV DETTE FLAGGET (invertert), ikke
  `IsDevelopment()` — live får nå faktisk `UseExceptionHandler`+`UseHsts` (ny, reell herding som
  aldri var aktiv før), beta/lokal dev uendret (fortsatt fulle feilsider, ingen HSTS, som før).

**StagingGate.cs selv er UENDRET** — mekanismen var allerede utformet til å bli et stille no-op når
`StagingGate:AccessKey` er tom/fraværende (`string.IsNullOrEmpty`-sjekk), så selve "fjerningen" fra
live er KUN en konfigurasjonsendring: `STAGING_GATE_ACCESS_KEY` satt til tom streng for
`testbase-test`-miljøet via `azd env set`, uendret (fortsatt satt) for `testbase-beta`. Ingen
kodeendring i selve gaten var nødvendig eller gjort.

**Verifisert, både lokalt (flagget tvunget av via miljøvariabel) og direkte mot live etter deploy:**
- `/health` uten cookie: `401` før, `200` etter på live; fortsatt `401` på beta (uendret).
- Rå POST til `/Konto/LoggInn?handler=Passord` med `AdminId=dev-admin`/`Passord=utvikler123`:
  avvist med "Fant ingen administrator..." — testet BÅDE lokalt med flagget tvunget av OG direkte
  mot `www.psytest.no` selv.
- AdminId+passord-skjemaet og PersonnummerOverride-feltet er fraværende i utsendt HTML på live.
- Alle seks diagnostiske sider gir `404` på live.
- Normal BankID-mock-innlogging (uten override) fortsatt uendret virkemåte.
- 48/48 enhetstester fortsatt grønt.

**IKKE gjort i denne omgangen, fortsatt åpent:** de tidligere flaggede offentlige sidene
(`/PasientRegistrering/Fullfor`, `/Inviter/Fullfor`, `/Inviter/Verifiser`,
`/Pasientportal/Konto/LoggInn`) som StagingGate tidligere (utilsiktet) også gated — nå som
StagingGate er borte fra live, er dette poenget uansett foreldet FOR LIVE (de er nå nåbare som
tiltenkt); gjelder fortsatt kun beta, der StagingGate fortsatt står. Ingen kodeendring nødvendig for
live sin del av dette punktet.

## PersonnummerOverride midlertidig gjeninnført på live (2026-09-20, samme dag)

**Regresjon oppdaget rett etter forrige seksjon sin deploy:** brukeren rapporterte at admin/
behandler-innlogging på live manglet personnummer-feltet, kun sikkerhetsspørsmålet var synlig.
Årsak: `Miljo:TillatUtviklingsSnarveier=false` gatet `PersonnummerOverride` sammen med de fire andre,
klart farligere snarveiene (AdminId+passord, 2FA-passthrough, personnummer-i-URL, diagnostiske
sider) — men `PersonnummerOverride` er IKKE en ren "nice to have"-snarvei slik de andre er.
`MockBankIdProvider` er den ENESTE `IBankIdProvider` som noensinne registreres i `Program.cs`
(ingen "ekte" implementasjon finnes for selve pasient-/admin-/behandler-BankID-flyten, kun det helt
separate `"BankIdInnlogging"` OIDC-schemaet for admin/behandler på beta) — uten override returnerer
den ALLTID samme faste fiktive personnummer (`"01019012345"`). Siden `Miljo:EktBankIdProfesjonell`
fortsatt er `"false"` på live (ekte BankID-avtale ikke klar, ca. 13 dager unna ifølge bruker),
betydde gårsdagens fiks i praksis at INGEN ekte administrator, behandler ELLER pasient kunne logge
inn på live i det hele tatt — pasient-portalen er dessuten alltid mock uansett miljø (`"pasient
forblir alltid mock"`, se arkitekturseksjonen), så samme regresjon rammet trolig ekte pasienter også,
ikke bare admin/behandler slik brukeren selv la merke til.

**Løsning: nytt, SNEVRERE flagg `Miljo:TillatPersonnummerOverride`**
(`Miljo.TillatPersonnummerOverride(configuration)`, `Security/Miljo.cs`) — styrer KUN
PersonnummerOverride-feltet (begge innloggingssider: `Pages/Konto/LoggInn` og
`Areas/Pasientportal/Pages/Konto/LoggInn`), atskilt fra `Miljo:TillatUtviklingsSnarveier` som
fortsatt er `"false"` på live. Logikken er en OR: sann enten hvis
`Miljo:TillatPersonnummerOverride` ELLER `Miljo:TillatUtviklingsSnarveier` er satt, så beta/lokal
dev (som allerede har `TillatUtviklingsSnarveier=true`) er uendret, mens live nå KUN får denne ene,
snevre unntaket via en egen ny app-innstilling (`MILJO_TILLAT_PERSONNUMMER_OVERRIDE=true`, satt via
`azd env set` for `testbase-test` alene). `LoggInnModel` sine to ansvarsområder er splittet i to
egne propertyer: `VisUtviklingsSnarveier` (AdminId+passord-skjemaet, fortsatt lukket på live) og
`VisPersonnummerOverride` (personnummer-feltet, nå åpent på live).

**Vurdert og bevisst valgt fremfor et bredere "skru alt på igjen":** å sette
`Miljo:TillatUtviklingsSnarveier=true` på live igjen (det brukeren bokstavelig talt spurte om —
"enable the fake bankid login") ville også gjenåpnet AdminId+passord-bypasset
(`dev-admin`/`utvikler123`, se forrige seksjon), 2FA-kode-passthrough og de seks diagnostiske
BankID-/betalingstestsidene — uten StagingGate som backstop denne gangen, i motsetning til før i
dag. `PersonnummerOverride` alene bærer fortsatt en reell restrisiko (kjenner/gjetter noen et
registrert menneskes ekte personnummer, kan de logge inn som dem — samme grunnleggende sårbarhet
som fikk test-appen phishing-flagget av Google Safe Browsing tidligere, se
`azure_bankid_phishing_flag`-minnet), men er UUNNGÅELIG uten ekte BankID — det er selve poenget med
BankID å bekrefte identitet uten en slik overstyring. Denne restrisikoen aksepteres bevisst,
midlertidig, av bruker, KUN for dette ene feltet — ikke de fire andre.

**MIDLERTIDIG — MÅ FJERNES når ekte BankID er verifisert på live:** sett
`MILJO_TILLAT_PERSONNUMMER_OVERRIDE=false` (eller fjern innstillingen) via `azd env set` for
`testbase-test` og kjør `azd provision` på nytt, så snart `Miljo:EktBankIdProfesjonell` er skrudd på
og en ekte interaktiv Idura-innlogging er bekreftet fungerende i nettleser (siste gjenstående steg,
se "Ekte BankID for admin/behandler" i denne loggen).

**Verifisert direkte mot live etter deploy:** personnummer-feltet er tilbake på BÅDE
`/Konto/LoggInn` og `/Pasientportal/Konto/LoggInn`; AdminId+passord-skjemaet fortsatt fraværende;
`/BankIdTest/Start` fortsatt `404`. Beta uendret (`401` på `/health`, uendret virkemåte for
`Miljo:TillatUtviklingsSnarveier=true`-siden av ting siden det flagget aldri ble rørt). 48/48
enhetstester fortsatt grønt.

## Admin fikk full CRUD på Grupper + kritisk autorisasjonshull lukket (2026-09-20, samme dag)

**Bruker rapporterte to ting samtidig:** (1) kunne ikke opprette grupper som admin, med beskjeden
"Remember, all that which patients and treaters can do, should be available as an admin" — direkte
sammenfallende med et allerede eksisterende, ordrett krav i `docs/prosjektbeskrivelse-original.md`
linje 47 ("Administrator skal kunne ... gjøre det samme som behandlere kan ift pasienten"), som
`Admin/Grupper` (kun-oversikt siden fase 1, bevisst men feilaktig kopiert fra samme mønster som
`Admin/Pasienter`) aldri fulgte; (2) "no QR Code inside the group" ved test på live — samme
rotårsak: den eneste Grupper-siden admin hadde tilgang til (Index) viser aldri QR-kode i det hele
tatt, siden selve redigeringen (der QR-koden vises) kun fantes i Behandlerportal.

**Løsning:** nye `Admin/Grupper/Ny` og `Admin/Grupper/Rediger` — funksjonelt identiske med
Behandlerportal sine (navn, testtilordning, QR-kode+regenerering, arkivering), MEN uten
eierskapssjekk mot innlogget bruker (admin kan redigere ENHVER behandlers gruppe) og med et
behandler-nedtrekksfelt på Ny-siden (admin må velge HVILKEN behandler som skal eie den nye
gruppen, siden `Gruppe.BehandlerId` er obligatorisk og ikke kan utledes fra en admin-bruker slik
den kan for en innlogget behandler). `Admin/Grupper/Index` fikk "+ Ny gruppe"-lenke og
Rediger/Arkiver-kolonne, samme mønster som Behandlerportal sin Index.

**Reell feil underveis, fanget før deploy:** `NyModel.LastValgAsync` sitt første forsøk brukte
`_db.Behandlere.Where(...).OrderBy(b => b.Visningsnavn).ToListAsync(...)` — `Visningsnavn` er en
BEREGNET C#-property (`Fornavn + " " + Etternavn`), ikke en databasekolonne, og EF Core klarer ikke
oversette en `OrderBy` på en slik property til SQL (`InvalidOperationException` ved
spørringsoversettelse, 500-feil ved faktisk sidevisning — fanget lokalt via Playwright FØR deploy).
Fiks: hent listen FØRST (`ToListAsync()`), sorter i minnet ETTERPÅ — samme mønster som allerede
brukt riktig andre steder i samme fil (`Admin/Grupper/Index.cshtml.cs` sin
`behandlerNavnById`-oppslag).

**KRITISK, uavhengig funn under samme undersøkelse:** verken `Behandlerportal/Grupper` eller
`Admin/Grupper` sto i `AuthorizeAreaFolder`-listen i `Program.cs` (i motsetning til ALLE andre
mapper i begge Areas), og ingen av PageModel-klassene hadde et eget `[Authorize]`-attributt heller.
Bekreftet med `curl` direkte mot live FØR fiks: `/Behandlerportal/Grupper`, `/Behandlerportal/
Grupper/Ny` og `/Admin/Grupper` ga alle `200 OK` for en HELT uautentisert forespørsel — altså kunne
enhver besøker på internett (særlig alvorlig nå som StagingGate er borte fra live) se
`Admin/Grupper` sin oversikt over ALLE behandleres gruppenavn/pasientantall, OG opprette vilkårlige
grupper via `Behandlerportal/Grupper/Ny` (ville fått `BehandlerId=0` siden `HentBehandlerId()`
faller tilbake til 0 for en uautentisert bruker — spam/søppeldata, ikke tilgang til ekte data, siden
eierskapssjekken på Rediger/QR tilfeldigvis ville blokkert dem fra alt annet enn sine egne
`BehandlerId=0`-søppelgrupper). Fikset ved å legge `/Grupper` til i `AuthorizeAreaFolder`-listen for
BEGGE Areas (`"BehandlerOmrade"` og `"AdminOmrade"`), samme mønster som resten av kodebasen — IKKE
per-klasse `[Authorize]`-attributter, for konsistens. Verifisert direkte mot live etter deploy:
samme tre stier gir nå `302` (redirect til innlogging) for en uautentisert forespørsel.

**Verifisert lokalt (Playwright) og på live:** admin kan opprette en gruppe med valgt behandler som
eier, redigere den, se QR-koden rendre korrekt på `Admin/Grupper/Rediger/{id}` (samme
`QrBildeGenerator` som Behandlerportal). 48/48 enhetstester fortsatt grønt.

**IKKE gjort i denne omgangen, samme mønster trolig igjen:** `Admin/Pasienter` har SAMME
"kun-oversikt"-begrensning (ingen `Ny`/`Rediger`-sider) som `Admin/Grupper` hadde — samme
opprinnelige krav (prosjektbeskrivelsen linje 47) tilsier at dette også bør rettes, men brukeren
rapporterte ikke dette spesifikt denne gangen. Vurder samme type CRUD-utvidelse for `Admin/
Pasienter` ved neste anledning.

## Forenklet QR-registrering + "fullfør profilen senere" (2026-09-20, samme dag)

**Bruker rapporterte, ordrett:** måtte oppgi personnummer ved QR-registrering selv om det skulle
vært valgfritt (jf. den opprinnelige avklaringen om en "prøv systemet"-terskel), og skjemaet hadde
for mange felt. Konkret ønske: KUN mobilnummer, e-post (bare én av dem påkrevd) og valgfritt
personnummer, i den rekkefølgen (telefon → e-post → PNR); dropp navn/kjønn/adresse/Vipps-samtykke
fra selve registreringen; send en "fullfør profilen din"-påminnelse til telefon/e-post etter
registrering, med lenke til å ettergi det som ble hoppet over — pasienten har allerede fått sin
første test innen den påminnelsen kommer. "Only if you enter PNR will the data be real patient
data" — personnummer er skillet mellom "prøv systemet" og en fullverdig pasient.

**`PasientInvitasjonService.RegistrerViaQrAsync` skrevet om:** signaturen kuttet fra 13 til 7
parametre — kun `behandlerId, gruppeId, personnummer (nullable), mobilNr, epost,
godtarLagringAvData, baseUrl`. `Varslingspreferanse` utledes nå internt fra hvilke(n) av
mobilNr/epost som faktisk er fylt ut (ikke lenger et eget valg pasienten gjør). `GodtarMuligVippsBetaling`
fjernet fra hele denne flyten — ingen kode leste den verdien uansett (bekreftet med `grep` på tvers
av repoet), så "push away" betyr her rett og slett at feltet er borte fra skjemaet, ikke flyttet et
annet sted (det finnes ingen betalings-gate-logikk som noensinne konsulterte det).

**Nytt felt `Pasient.ProfilFullforingToken`** (nullable, unik indeks, IKKE kryptert — samme mønster
som `Gruppe.QrToken`/`Behandler.PasientInviteQrToken`, siden det er et tilfeldig token, ikke
personopplysninger) — generert for HVER QR-registrert pasient, uavhengig av om personnummer ble
oppgitt. Migrasjon `LeggTilProfilFullforingToken`, ren additiv (ingen dataflytting nødvendig).

**Ny offentlig side `Pages/PasientRegistrering/FullforProfil.cshtml`** (route
`/PasientRegistrering/FullforProfil/{token}`) — IKKE et engangstoken, kan besøkes/brukes flere
ganger. Lar pasienten ettergi navn/kjønn/adresse/personnummer/den andre kontaktkanalen, uten
innlogging (samme tillitsmodell som `BliPasient` — token er selve autentiseringen). Personnummer
valideres på nytt (format + unikhet) først når det faktisk endres. `PasientInvitasjonService` fikk
`FinnVedProfilFullforingTokenAsync` og `OppdaterProfilAsync` for dette.

**Reell bug funnet og fikset FØR deploy (Playwright + curl-repro):** `MobilNr`/`Epost` var
opprinnelig deklarert som ikke-nullbar `string` med `= string.Empty` som default på BEGGE de nye
sidene. ASP.NET Cores modellbinding konverterer et INNSENDT MEN TOMT skjemafelt til `null` for en
streng-property — IKKE til `""`, uavhengig av C#-defaultverdien — noe som deretter traff en
`NOT NULL`-kolonne (`Email`) i MySQL og ga `DbUpdateException`/500 så snart e-post-feltet faktisk
ble stående tomt (nøyaktig scenarioet "bare telefon, ikke e-post" som hele poenget med denne
omgangen var å støtte). Fanget ved å teste akkurat DEN kombinasjonen i nettleser først, ikke bare
"alle felt utfylt". Fiks: begge feltene er nå `string?` i PageModel, med `?? string.Empty`
eksplisitt ved kallet til service-laget (som fortsatt tar imot ikke-nullbare `string`-parametre, i
tråd med de ikke-nullbare databasekolonnene). Se ny fallgruve i CLAUDE.md.

**Verifisert lokalt (Playwright, ende-til-ende):**
- Registrering med KUN mobilnummer (ingen e-post, ingen personnummer) → lykkes, pasient opprettet
  med `Personnummer=NULL`, `Email=""`, riktig `ProfilFullforingToken` generert.
- "Fullfør profilen din"-lenken faktisk sendt — bekreftet via ekte utgående Vonage SMS-kall i loggen
  (dette lokale dev-miljøet har reelle Vonage-nøkler konfigurert, ikke bare MockSmsSender — verdt å
  huske ved fremtidig SMS-relatert testing lokalt, siden man da faktisk sender ekte SMS-er).
- `/PasientRegistrering/FullforProfil/{token}` viser riktig forhåndsutfylte verdier, og lagring av
  navn+personnummer oppdaterer raden korrekt (`HarPersonnummer` går fra 0 til 1).
- 48/48 enhetstester fortsatt grønt.

**IKKE gjort i denne omgangen:** ingen egen visuell markering i behandler-/adminvisninger for
"pasient uten personnummer" (prøve-status er kun synlig ved å sjekke om personnummer er satt) —
brukeren ba ikke om dette, holdt bevisst utenfor for å unngå overimplementering. Samme kjente,
upåvirkede StagingGate-begrensning som før gjelder fortsatt for den NYE siden på beta (ikke live,
der StagingGate er fjernet): en helt fersk besøker uten StagingGate-cookien fra før ville i teorien
møtt tilgangssperren på beta før de når `/PasientRegistrering/FullforProfil` — uendret fra det
tidligere kjente, bevisst utsatte funnet.

## Fase 3: automatisk rapportgodkjenning og "slett prøvedata" (2026-09-20, samme dag — KUN lokalt, IKKE deployet)

**Bruker ba om å fortsette til neste fase og teste internt, men EKSPLISITT ikke deploye til beta
eller live før brukeren selv har fått testet det som allerede står der** (personnummer-overstyring
+ forenklet QR-registrering fra forrige seksjon). Denne fasen er derfor kun bygget og verifisert
lokalt — IKKE provisjonert/deployet noe sted ennå.

**Fullførte de to gjenstående, opprinnelig spesifiserte delene av "prøv systemet"-modusen** (jf. det
aller første, store avklaringssvaret: "kept until treater manually clears the group" og "generate
the report... and send it to the patient at once... Do not send those reports to the treater for
approval"):

1. **`TestService.LagreSvarAsync`**: når en tildeling fullføres OG den eiende pasienten IKKE har
   personnummer (prøvepasient), hopper koden nå over `BehandlerMeldingService.OpprettAsync`
   (ingen godkjenningsforespørsel til behandler i det hele tatt) og setter i stedet
   `RapportGodkjentUtc`/`RapportSynligForPasient` UMIDDELBART. En ekte pasient (med personnummer)
   er helt uendret — samme godkjenningsflyt som før. `Pasientportal/Tester/Fyll.cshtml` fikk en
   betinget "Se rapporten din"-lenke på ferdig-siden, synlig nettopp når dette har skjedd.
2. **`GruppeService.TellProvedataAsync`/`SlettProvedataAsync`** — en "Slett prøvedata"-knapp på
   BÅDE `Behandlerportal/Grupper/Rediger` og `Admin/Grupper/Rediger`, som viser antall
   prøvepasienter (personnummer `NULL`) i gruppen og permanent sletter dem + deres
   tildelinger/svar/betalinger/behandlermeldinger ved bekreftelse. Rammer ALDRI en pasient som har
   fått personnummer, uansett hvordan de kom inn i gruppen — kun fravær av personnummer avgjør.
   Ikke koblet til `PlanlagtTildeling` (lagrer pasient-IDer som CSV, ikke en direkte FK — urealistisk
   scenario for en QR-prøvepasient, bevisst utelatt for å unngå unødig kompleksitet).

**Verifisert lokalt, ende-til-ende (Playwright):**
- Registrerte en prøvepasient (e-post, ingen PNR) via gruppe-QR → fylte ut hele GADIT-testen →
  "Se rapporten din"-lenken dukket opp umiddelbart på ferdig-siden → rapporten var faktisk synlig
  og korrekt skåret på `/Pasientportal/Tester/Rapport/{id}` uten noe behandler-steg.
- Bekreftet i database: `RapportGodkjentUtc` satt, `RapportSynligForPasient=1`, OG
  `behandler_meldinger` hadde ZERO rader for denne tildelingen (ingen godkjenningsforespørsel ble
  opprettet).
- "Slett prøvedata" på `Admin/Grupper/Rediger` viste riktig antall (2, fra to tidligere
  testregistreringer denne dagen), slettet dem ved bekreftelse, og etterlot ZERO orfanerte rader i
  `pasienter`/`test_tildelinger`/`test_svar`/`test_tildeling_betalinger`.
- 48/48 enhetstester fortsatt grønt.

**IKKE gjort i denne omgangen (bevisst, hørte ikke til denne fasens kjerne):** fase 4 (aggregert
rapportering på tvers av en gruppes besvarelser, til konferanse-/markedsføringsbruk) og fase 5
(eksplisitt verifisering av at betalingsgaten/partner-allow-listen oppfører seg identisk for
gruppetildelte tester — allerede antatt riktig siden samme `TestTildelingsService`-kode brukes, men
ikke eksplisitt re-testet i denne omgangen).

**VIKTIG — IKKE DEPLOYET:** alle endringene i denne seksjonen er KUN på lokal maskin (bygget,
migrert lokalt, testet lokalt). `azd deploy`/`azd provision` er bevisst IKKE kjørt mot
`testbase-beta` eller `testbase-test` for dette — vent på eksplisitt bekreftelse fra bruker om at
det som allerede ligger ute (forenklet QR-registrering + personnummer-overstyring) er ferdig
testet først.

**Oppdatering 2026-09-21: bekreftet av bruker ("Looks good"), deployet til BEGGE miljøer** (samme
dag brukeren fikk logget på beta for første gang) — se eget avsnitt lenger ned for fase 4+5, som
ble bygget/testet/deployet i SAMME økt etter denne bekreftelsen (brukeren ba eksplisitt om å gå
videre til fase 4+5 uten å vente på ny bekreftelse denne gangen: "Deploy when testing seems good
and test on live. Deploy to beta as well, i want to see the way we handle the various versions.").

## Fase 4: aggregert rapportering + fase 5: betalingsgate-verifisering (2026-09-21)

**Fase 4 — aggregert rapport på tvers av en gruppes besvarelser**, jf. det opprinnelige ønsket om
en "vis meg aggregerte tall til konferanse-/presentasjonsbruk"-knapp, og avklaringen fra
planleggingen: ÉN generisk visning for ALLE tester (ikke egne visualiseringer per test).

- `GruppeService.HentAggregatAsync(gruppeId, provedata, fraUtc, tilUtc)` — ny, injiserer nå
  `TestService` i `GruppeService` (ren én-veis avhengighet, ingen sirkularitet). For HVER test
  tilordnet gruppen: henter fullførte tildelinger for de aktuelle pasientene (prøvedata = ALLE
  pasienter UTEN personnummer, ingen tidsbegrensning i det hele tatt — poenget er én samlet
  presentasjonsøkt; ekte pasienter = MED personnummer, avgrenset til en Fra/Til-periode),
  gjenbruker `TestService.BeregnSkaaringAsync` per tildeling (samme skåringsmotor som
  enkelt-rapporter), og aggregerer til N, gjennomsnitt/median prosentskår, og en kategorisk
  fordeling. Den kategoriske fordelingen bygges av `TestSkaaringIndikator` (allerede en generisk,
  testuavhengig struktur — se `Skaaring/TestSkaaringIndikator.cs`, brukt til "badges" i
  enkelt-rapporter) i stedet for `TestSkaaring.Fortolkning` (fri tekst med råskår bakt inn, ubrukelig
  som gruppering) — dette er akkurat det som gjør ÉN generisk visning mulig for enhver test med en
  registrert `ITestSkaaringsberegner`, uten testspesifikk aggregeringskode.
- Ny side `Behandlerportal/Grupper/Aggregert/{id}` + `Admin/Grupper/Aggregert/{id}` (lenket fra
  hver sin Rediger-side) — to faner (Prøvedata/Ekte pasienter, GET-lenker), et enkelt Fra/Til-skjema
  for "ekte pasienter"-fanen (default siste 30 dager), og ett kort per test med N/gjennomsnitt/
  median/fordelingstabell. Beregnes HELT PÅ NYTT for hver sidevisning — ingen lagret rapport, jf.
  "vises umiddelbart ved klikk".

**Fase 5 — betalingsgate for gruppetildelte tester, EKTE FUNN under verifisering (ikke bare en
bekreftelse):** den opprinnelige avklaringen sa "samme betalingsgate som enhver annen tildeling,
ingen spesialbehandling" — men dette var formulert for EKTE pasienter. Verifisering avdekket at
`TestTildelingsService.TildelOgVarsleAsync` (delt kode for QR-automatisk OG manuell admin/behandler-
tildeling) IKKE skilte på personnummer i det hele tatt — en "prøv systemet"-pasient som ble meldt inn
i en gruppe med en PRISET test, ville blitt sendt til Vipps/Stripe-betaling FØR de fikk prøve testen,
i direkte motstrid med selve premisset for prøvemodusen ("sample the system before committing to any
payment"). Fikset: i samme løkke der `TestTildelingBetaling` opprettes, tvinges prisen til 0/
`BetalingStatus.IkkePakrevd` når pasienten mangler personnummer, UANSETT testens faktiske pris — en
ekte pasient (har personnummer) er HELT uendret, samme betalingsgate som før. Gjelder alle veier inn
(QR-registrering OG en behandler som senere manuelt tildeler en priset test til en eksisterende
prøvepasient), siden fiksen sitter i den delte metoden, ikke QR-spesifikk kode.

**Verifisert lokalt, ende-til-ende (Playwright + database), fase 4:** to prøvepasienter registrert
via samme gruppe-QR, én med maks GADIT-skår (8/8, "Over grenseverdi") og én med lavest mulig (0/8,
"Under grenseverdi") — aggregert rapport viste korrekt N=2, gjennomsnitt=50,0%, median=50,0%, og
fordelingstabellen viste nøyaktig "Over grenseverdi: 1" / "Under grenseverdi: 1". "Ekte pasienter"-
fanen viste riktig Fra/Til-skjema og N=0 (ingen ekte pasienter i gruppen).

**Verifisert lokalt, ende-til-ende, fase 5:** satte midlertidig en pris (50 kr) på GADIT i den lokale
databasen for testens skyld (reversert etterpå). En prøvepasient registrert via gruppe-QR gikk RETT
til utfylling som før (`TestTildelingBetaling.Status = IkkePakrevd`, 0 kr) til tross for prisen; en
ekte pasient (med personnummer) registrert via SAMME gruppe-QR ble korrekt sendt til
`/Pasientportal/Tester/Betal` (`Status = Venter`, 50 kr) — bekreftet direkte i databasen for begge.
48/48 enhetstester fortsatt grønt gjennom hele denne omgangen.

**Deployet til BEGGE miljøer samme dag, etter lokal verifisering** (brukerens eksplisitte instruks:
"Deploy when testing seems good... Deploy to beta as well, i want to see the way we handle the
various versions") — ingen ny migrasjon i denne omgangen (kun nye sider/tjenestemetoder), så vanlig
`azd deploy` (uten `azd provision`) var tilstrekkelig for begge.

## Ni brukerfeilrettinger: profilfullføring, tester-faner, grupper-arkivering, test-infoboks (2026-09-21)

Bruker meldte inn ni konkrete feil/ønsker i én omgang («Some fixes: ... Fix these, commit and
deploy. Unless there is a security related challenge from you, go with your recommended choices.»)
— eksplisitt fullmakt til å velge design/implementasjon selv uten å spørre, og eksplisitt instruks
om å committe (FØRSTE git-commit i denne økten, etter en lang rekke ukommiterte faser) og deploye.
Ingen av punktene reiste en reell sikkerhetsbekymring, så alt ble implementert uten avklaring.

**1) Pasient kan ikke fullføre en delvis opprettet profil fra "Min side" — kun via lenke.**
`Behandlerportal/Pasienter/Ny` oppretter en pasient med kun mobil/e-post (navn og personnummer
kommer senere via `PasientRegistrering/FullforProfil/{token}`) — men når en slik pasient likevel
klarer å logge inn (BankID-mock med korrekt personnummer, satt av behandler eller ved senere
egen registrering), fantes ingen kobling FRA "Min side" TIL fullføringslenken; den var kun
tilgjengelig i utsendt SMS/e-post. Løst: `PasientInvitasjonService.SikreProfilFullforingTokenAsync`
(ny — genererer token lat, dvs. kun hvis det ikke finnes fra før, og lagrer, uten å sende noe) kalles
fra `Pasientportal/MinSide.cshtml.cs` når `Navn`/`MobilNr`/`Email` er tomt, og resultatet vises som
en "Fullfør profilen din"-knapp direkte på siden. Bevisst en EGEN metode fra den som faktisk SENDER
en påminnelse (`PaaminnFullforingAsync`, se punkt 8) — å vise lenken skal aldri i seg selv trigge en
SMS/e-post hver gang siden lastes.

**2) Ubesvarte testtildelinger fra behandler hopet seg opp uten mulighet til å rydde.**
`Behandlerportal/MinSide` hadde fra før to separate seksjoner ("Venter på godkjenning" og en
duplisert "Ikke besvart" lenger ned) uten noen måte å fjerne en tildeling som aldri ble besvart.
Løst i samme slag som punkt 3 (tre faner) under: en ny `TestService.SlettIkkeFullfortTildelingAsync`
sletter `TestSvar`+`TestTildelingBetaling`+selve `TestTildeling`-raden, men KUN når tildelingen
faktisk ikke er fullført og tilhører en av behandlerens egne pasienter (dobbelt vern mot å slette en
allerede besvart test eller en annen behandlers pasient). Utløses av en søppelbøtte-knapp
(`_Ikon` "slett") på hver "Ikke besvart"-rad, med `confirm()`-dialog.

**3) Tre faner ("Venter på godkjenning" / "Ikke besvart" / "Godkjente") over ÉN tabell, med søk
som filtrerer alle tre samtidig.** Ny generisk klientside-mekanisme, `wwwroot/js/faner.js` — leser
`data-fane-beholder` (container), `data-fane` (fanevalg-knapper) og `data-fane-sok` (søkefelt), og
skjuler/viser `data-fane-rad`-rader basert på BÅDE aktiv fane OG søketekst samtidig (ikke enten/
eller) — dermed beholdes søketeksten når man bytter fane, og et søk kan i praksis vise "0 treff" på
en fane selv om et annet fanevalg har treff, akkurat som forventet av "filtrer på alle 3 fanene".
Lagt til en manglende tredje kategori, `TestService.HentGodkjenteFullforteForBehandlerAsync`
(fantes fra før kun ugodkjent-varianten). Samme mønster (faner + søk, samme `faner.js`) gjenbrukt
for Grupper-sidenes Aktive/Arkivert (se punkt 5) — ett skript, to helt ulike bruksområder.
Verifisert i nettleser: bytte fane beholder søketeksten, og viser korrekt "0 treff" i en fane der
søket ikke matcher noen rad der.

**4) "Innstillinger" skal være en farget knapp, ikke en lenke.** Ren CSS-klasseendring
(`<a class="btn btn-accent">` i stedet for en bar lenke) på `Behandlerportal/MinSide.cshtml` — ingen
funksjonell endring, kun visuell konsistens med resten av knappe-språket i appen.

**5) Grupper: Opprettet/Start/Slutt-datoer, "+Ny gruppe" som rolletilpasset knapp, Rediger/Arkiver
som ikon-knapper, og en egen "Arkivert"-fane med Gjenopprette/Slett (med ja/nei-bekreftelse).**
To nye rene informasjonsfelt på `Gruppe` — `StartDato`/`SluttDato` (`DateOnly?`, ingen automatikk
knyttet til dem ennå, kun satt/redigert av behandler/admin) — via migrasjonen
`LeggTilGruppeStartSluttDato`. `GruppeService` fikk `HentArkiverteForBehandlerAsync`/
`HentAlleArkiverteAsync` (mirror av de aktive-variantene, filtrert på `ErArkivert`),
`GjenopprettAsync` (nullstiller `ErArkivert`/`ArkivertUtc`), og `SlettGruppeAsync` — en HARD sletting
som bevisst KUN tillates for allerede arkiverte grupper (ekstra vern mot utilsiktet datatap), og som
løsner pasienters `GruppeId`-kobling (satt til null, pasientene selv slettes IKKE) før selve gruppen
fjernes, siden `Pasient.GruppeId` er en ekte FK. Begge Grupper/Index-sider (`Behandlerportal` og
`Admin`) fikk samme faner+søk-mønster som punkt 3, "+Ny gruppe" som `.btn.btn-accent`-knapp,
Rediger/Arkiver som `.btn-icon`-par, og en Arkivert-fane med Gjenopprett/Slett — sistnevnte med
`onclick="return confirm(...)"` (ren HTML, ikke avhengig av `faner.js`, siden dette er en
destruktiv handling som MÅ fungere selv om JS av en eller annen grunn ikke har lastet). Verifisert
hele livssyklusen i nettleser: arkiver → vises i Arkivert-fanen → gjenopprett → tilbake i Aktive →
arkiver på nytt → slett (bekreftet at `confirm()`-dialogen faktisk trigges med riktig tekst, og at
sletting fjerner raden permanent).

**6) "Rediger pasient" (behandler) mangler opprettelsesdato og en måte å purre en ufullstendig
registrering.** `Opprettet`-dato lagt til øverst på siden. Ny `PasientInvitasjonService.
PaaminnFullforingAsync` (kaller `SikreProfilFullforingTokenAsync` fra punkt 1, deretter den
eksisterende private utsendingsmetoden) koblet til en "Påminn fullføring"-knapp som KUN vises når
`Navn`/`MobilNr`/`Email` mangler — verifisert ende-til-ende i nettleser (opprettet en ekte
delvis-pasient via `Pasienter/Ny`, bekreftet at knappen dukket opp, trykket den, fikk
"Påminnelse sendt." tilbake, ryddet testpasienten fra databasen etterpå).

**7) Info-boks for valgt test (introduksjon, estimert tid, prising) ved siden av
kategori-treet, for behandler OG admin, på alle steder en test kan velges.** Ny
`TestService.HentAntallLeddPerTestAsync` teller faktiske `TestLedd` per test (via `TestSide`) og
en enkel omregning (`Math.Ceiling(antallLedd * 15.0 / 60)` minutter, ca. 15 sek/spørsmål) gir et
estimat som automatisk holder seg riktig når tester endres, i stedet for et manuelt vedlikeholdt
tidsfelt. Ny `wwwroot/js/testinfo.js` leser `data-test-navn`/`data-beskrivelse`/
`data-rapport-introduksjon`/`data-estimert-min`/`data-minste-pris`/`data-storste-pris`/
`data-typisk-honorar` fra den valgte sjekkboksen og fyller en `<aside class="test-infoboks">` —
gjenbrukt uendret på alle seks stedene en test velges (Behandlerportal+Admin sine
`Tildel/Tester`, `Grupper/Ny`, `Grupper/Rediger`). Prisingsteksten forklarer eksplisitt hvorfor det
ikke finnes noen måte å ta betalt utenom systemet (min/maks pris, typisk honorar) — samme
begrunnelse som allerede fantes i selve prisingsmotoren, nå bare synlig i konteksten der en
behandler faktisk velger testen.

**Reell layout-bug funnet og rettet under verifisering (ikke bare en bekreftelse):** infoboksen
skulle vises TIL HØYRE for treet på desktop, men CSS-en (`.tildel-kategori-tre { flex: 1 1 20rem }`
+ `.test-infoboks { flex: 1 1 16rem }`, pluss mellomrom) hadde et kombinert flex-basisbehov (20rem +
16rem + 1.5rem gap ≈ 632px) som var STØRRE enn den faktiske bredden på flere av disse sidenes
skjema-container (~582px i `Grupper/Ny`/`Grupper/Rediger`, som bruker en smalere kort-stil enn
`Tildel/Tester`) — flexbox avgjør linjebryting ut fra flex-basis-summen, ikke den faktisk krympede
bredden, så infoboksen brøt konsekvent til en egen linje UNDER treet på disse sidene, ikke ved
siden av, selv på brede skjermer. Oppdaget ved å faktisk måle elementets `getBoundingClientRect()`
i nettleseren (JS-en hadde fjernet `skjult`-klassen og fylt innholdet korrekt — bugen var ren CSS-
layout, usynlig ved bare å lese kildekoden). Rettet ved å redusere flex-basis
(`.tildel-kategori-tre` til `flex: 3 1 14rem`, `.test-infoboks` til `flex: 2 1 12rem` med
`max-width: 18rem`) slik at kombinert basis (14rem + 12rem + 1.5rem ≈ 440px) komfortabelt passer
selv i den smaleste av de seks containerne. Verifisert på nytt etter rettelsen (infoboksen sitter nå
på samme rad som treet, til høyre) og på mobilbredde (375px — stables korrekt under, uendret av
denne rettelsen siden `@media (max-width: 720px)`-regelen tvinger kolonne-layout uansett).

**Verifisert lokalt i nettleser (Playwright) for alle ni punkter**, inkludert et par som krevde
midlertidig database-manipulasjon for å fremtvinge riktig tilstand (nullet `Navn` på en eksisterende
testpasient for å teste "mangler profilinfo"-boksen på `Pasientportal/MinSide`, gjenopprettet
etterpå; opprettet og slettet en ekte delvis-pasient for punkt 1+6). 48/48 enhetstester fortsatt
grønt. Én ny migrasjon (`LeggTilGruppeStartSluttDato`, kun addering av to nullable kolonner —
verifisert trygg, ingen omdøping/datatap-risiko).

## Gruppe-rapportgenerator: navngitt enkelttest-rapport med standardavvik + spredningsplott (2026-09-22)

Bruker ba om "Generer Temp Gruppe Rapport"/"Generer Gruppe Rapport"-knapper ved siden av
"Slett prøvedata" på `Grupper/Rediger` (begge Areas) — en popup der behandler/admin radioknapp-
velger ÉN av gruppens tilordnede tester + et datointervall, og får umiddelbart en navngitt rapport
med N/gjennomsnitt/median/**standardavvik** + et **spredningsplott**.

**Viktig kurskorrigering underveis:** startet først med å bygge et HELT NYTT, lagret
rapport-subsystem (to nye entiteter `GruppeRapport`/`GruppeRapportDatapunkt`, egen migrasjon, egne
visningssider) — før brukeren påpekte midt i økten: "you already have the aggregated report
feature added." Rullet umiddelbart tilbake (slettet de to nye entitetsfilene, reverterte
`AppDbContext`) og bygget i stedet videre PÅ den eksisterende `GruppeService.HentAggregatAsync`/
`Grupper/Aggregert`-funksjonaliteten fra fase 4 — ingen ny migrasjon, ingen ny tabell. Lærdom: når
brukeren nevner at noe "allerede finnes", stopp og se etter gjenbruk FØR man fortsetter å bygge nytt,
selv om den opprinnelige beskrivelsen (radioknapp, popup, navngitt rapport) i utgangspunktet hørtes
ut som noe nytt.

**Implementasjon (kun utvidelse av eksisterende kode):**
- `GruppeService.HentAggregatAsync` sin per-test-beregning trukket ut til en delt privat
  `BeregnForTestAsync`-kjerne (brukt av BÅDE den eksisterende multi-test-oversikten og den nye
  enkelttest-rapporten) — utvidet til også å beregne **standardavvik** (populasjon, delt på N, ikke
  N-1 — disse dataene ER hele utvalget rapporten beskriver, ikke et stikkprøve) og samle rå
  `(FullfortUtc, ProsentSkaar)`-datapunkter, billig å ta med selv der multi-oversikten ikke bruker
  dem ennå.
- Ny `GruppeService.HentEnkelttestAsync(gruppeId, testId, provedata, fra, til)` — samme
  beregningskjerne for ÉN valgt test. **Bevisst avvik fra den eksisterende oversikten:** her gjelder
  datointervallet BEGGE modi (også prøvedata), siden brukeren eksplisitt ba om et datovalg uansett
  hvilken av de to knappene som trykkes — den gamle oversiktens "prøvedata er alltid ubegrenset i
  tid"-regel er UENDRET for multi-test-oversikten, kun det nye enkelttest-sporet oppfører seg
  annerledes.
- Ny `GruppeService.HentTidligsteTildeltDatoPerTestAsync(gruppeId)` — tidligste `TildeltUtc` per
  tilordnet test, separat for prøvedata/ekte-utvalget (3 spørringer totalt, ikke én per test) — til
  å forhåndsutfylle popupens "Fra"-felt med "første gang testen ble utstedt", jf. kravet ordrett.
- `Grupper/Aggregert.cshtml(.cs)` (begge Areas) fikk et nytt `testId`-query-parameter-styrt
  "enkelttest"-visningsmodus SIDE OM SIDE med den eksisterende multi-test-oversikten (ingen endring
  i oppførsel når `testId` er fraværende) — tittel "{gruppenavn} {testnavn} {fra}–{til}", fire
  stat-fliser (Antall deltakere/Gjennomsnitt/Median/Standardavvik), et spredningsplott, den
  eksisterende indikator-fordelingstabellen, og en "Skriv ut"-knapp. INGEN egen POST-handler eller
  lagring — popupen er en vanlig `method="get"` til Aggregert-siden (samme "beregn på nytt hver
  gang, ingen lagret rapport"-prinsipp som resten av gruppeaggregeringen).
- Spredningsplottet er et server-rendret inline SVG (ingen ny JS-chart-bibliotek-avhengighet) —
  X-akse er kronologisk rekkefølge (ren spredning, ingen egen tidsakse-mening), Y-akse er
  prosentskår 0–100 % med hårfine rutenett-linjer ved 0/25/50/75/100, ÉN farge (aksentfarge, siden
  dette er ett enkelt datasett — ingen kategorisk forklaring/legend nødvendig, jf. dataviz-
  retningslinjene fulgt for dette prosjektet), 5px prikker med hvit ring, og en native SVG
  `<title>`-tooltip per punkt (dato + prosent) som enkel hover-interaktivitet uten egen JS.
- To knapper (`.btn.btn-outline.btn-sm`) rett under "Prøvedata"-avsnittet på `Grupper/Rediger`
  (begge Areas), som åpner ÉN delt `<dialog>` (samme mønster som den eksisterende
  `oppsummeringDialog` i tildelingsflyten) — ny `wwwroot/js/grupperapport.js` setter en skjult
  `Provedata`-verdi basert på hvilken knapp som ble trykket, forhåndsvelger første test-radio, og
  oppdaterer "Fra"-feltets `min`/verdi når testvalget endres (data-attributter satt server-side per
  test, ett par per modus).

**Reell Razor-fallgruve støtt på under bygging:** Razors spesial-håndterte `<text>`-pseudo-tag (kun
ment for å bryte ut av markup til ren kode) kan IKKE bære attributter i det hele tatt — ikke engang
med annen store/små bokstaver-variant (`<TEXT>` ga samme `RZ1023`-feil, ikke bare det opplagte
`<text>`). Måtte omgås helt ved å bygge selve `<text x="..." y="...">...</text>`-elementet som en
plain C#-streng og skrive den ut via `@Html.Raw(...)` i stedet for literal Razor-markup — gjelder
ethvert fremtidig behov for et SVG `<text>`-element i en `.cshtml`-fil. Samtidig oppdaget en beslektet
snublefelle i samme kodesnutt: `@(uttrykk).Metode(...)` (eksplisitt parentes) avslutter selve
Razor-uttrykket ved den lukkende parentesen — alt etter, inkludert `.ToString(...)`, blir literal
HTML-tekst, IKKE en del av C#-uttrykket. Kun IMPLISITTE uttrykk (`@verdi.Metode(...)`, uten
omsluttende parentes rundt `@`) lar et kjede av medlemstilgang/metodekall henge med. Riktig fiks for
et eksplisitt uttrykk er å legge HELE kjeden inni parentesen: `@((uttrykk).Metode(...))`.

**Verifisert i nettleser (Playwright) med reelle data:** midlertidig flyttet fire eksisterende
testpasienter (med til sammen 12 fullførte WHO-5-besvarelser fra tidligere faser i denne økten) inn
i en tom testgruppe, genererte en ekte-pasient-rapport — fikk korrekt N=12, gjennomsnitt 54,7 %,
median 50,0 %, standardavvik 23,9 %, et spredningsplott med 12 synlige punkter (native tooltip
bekreftet med riktig dato+prosent per punkt via tilgjengelighetstreet), og den eksisterende
indikator-fordelingstabellen uendret. Verifisert også tom-tilstand (0 fullførte i intervallet,
"Ingen fullførte besvarelser..."), og at "Fra"-feltet korrekt forhåndsutfylles fra tidligste
tildelingsdato når en test har historikk, og faller tilbake til dagens dato når den ikke har det.
Testpasientenes gruppetilhørighet tilbakestilt til `NULL` etter verifisering. 48/48 enhetstester
fortsatt grønt, ingen ny migrasjon (rent lag oppå eksisterende skjema).

## Feiltolerant varsling ved QR-registrering (kapasitetsgjennomgang) (2026-09-22)

Bruker planlegger et foredrag der 50–100 deltakere skal registrere seg (prøvedata) og fylle ut en
test i samme gruppe i løpet av ~10 minutter, og spurte om systemet tåler det. Gjennomgang av
koden (ikke bare gjetting) avdekket at DETTE, ikke selve testutfyllingen, var det reelle
risikopunktet:

**Funn:** hver QR-registrering (`Pages/BliPasient/Index.cshtml.cs` →
`PasientInvitasjonService.RegistrerViaQrAsync` → `TestTildelingsService.TildelOgVarsleAsync`)
utløste OPPTIL FIRE sekvensielle, BLOKKERENDE eksterne API-kall (Vonage SMS + Azure Communication
Services e-post, TO GANGER — én gang for "fullfør profilen din" i `RegistrerViaQrAsync`, én gang
for "nye tester tildelt" i `TildelOgVarsleAsync`) FØR deltakeren ble sendt videre til selve
testen — og INGEN av de fire var beskyttet med try/catch noe sted i kjeden.
`VonageSmsSender.SendAsync` kaller `response.EnsureSuccessStatusCode()` ubetinget (kaster på
enhver ikke-suksess-status, inkludert en 429 rate-limit-respons), og denne feilen forplantet seg
helt opp og feilet HELE registreringsforespørselen — SELV OM pasienten og testtildelingen allerede
var lagret i databasen på det tidspunktet (databaselagringen skjer FØR varslingsforsøket i begge
metoder). Verste realistiske utfall under nøyaktig scenarioet i spørsmålet: en brå bølge av 50-100
samtidige QR-registreringer (alle skanner "nå" på kommando) struper Vonage/Azure Communication
Services sine sendekvoter, og en del av publikum ser en feilmelding midt i foredraget — mens
dataene deres i realiteten er trygt lagret. Det er den verst tenkelige feilmåten: ser ut som et
systemkrasj for tilskueren, uten å faktisk være et datatap.

**Fiks:** begge utsendingsforsøkene (`PasientInvitasjonService.SendFullforProfilLenkeAsync` og
`TestTildelingsService.VarsleAsync`) pakket inn i try/catch per kanal (SMS/e-post hver for seg,
ikke én felles try rundt begge — en SMS-feil skal ikke hindre et forsøk på e-post) — en feilet
utsending logges (`ILogger`, nytt injisert i begge tjenester, ren DI-oppløsning siden begge
allerede var registrert som enkle `AddScoped<T>()` uten manuell factory) og svelges, ALDRI kastet
videre. `TestTildelingsService.VarsleAsync` sitt returnerte `(SendtSms, SendtEpost)`-par oppdatert
til å reflektere FAKTISK utfall (ikke bare forsøkt) — hvis SMS feiler, returneres `SendtSms=false`,
selv om et forsøk ble gjort. Bevisst IKKE lagt inn noen retry-logikk (Polly e.l.) i denne runden —
scope var å hindre at en varslingsfeil velter registreringen, ikke å garantere levering; en
mislykket varsling er fortsatt en "reserve for senere besøk fra en annen enhet" (se
`RegistrerViaQrAsync` sin kommentar), pasienten er uansett allerede inne i selve testen.

**Verifisert:** 48/48 enhetstester fortsatt grønt (ingen eksisterende test konstruerer disse to
tjenestene manuelt, så den nye konstruktørparameteren `ILogger<T>` er trygg). Kjørte en reell
QR-registrering lokalt (mobilnummer + e-post begge utfylt, altså alle fire varslingsveier
aktivert) og bekreftet at happy-path fortsatt fungerer uendret — deltakeren havner rett i
testutfyllingen som før. Testet IKKE selve feilscenarioet direkte (krever å simulere en faktisk
Vonage/Azure-feil, ikke gjort i denne runden) — trygghet her kommer fra kodegjennomgang
(try/catch omslutter nøyaktig og bare nettverkskallet, databaselagringen skjer garantert før), ikke
fra en observert feilende utsending.

**Gjenstående, IKKE gjort i denne runden — bevisst utenfor scope, men relevant før foredraget:**
se separat "Plan: automatisert lasttest fra et 'live'-perspektiv" (samme dato) for et konkret
forslag til å faktisk verifisere kapasiteten før den brukes skarpt, samt en påminnelse om å
sjekke Vonage/Azure Communication Services sine faktiske sendekvoter og vurdere midlertidig å
skalere opp `testbase-test` sin App Service (i dag Basic B1, én instans, ingen autoskalering) og
MySQL-server (i dag Burstable Standard_B1ms) for selve foredragsdagen.

## Lasttest mot beta: reell kapasitetsgrense funnet (2026-09-22)

Bruker var bekymret for kostnad (hver SMS/BankID-transaksjon koster penger) og ba om at en lasttest
IKKE måtte generere utgifter — ellers ville den ikke blitt kjørt i det hele tatt. Løst helt uten
noen infrastruktur-endring: `azd env get-values` for `testbase-beta` viste at INGEN Vonage-nøkler
er satt der i det hele tatt (SMS er allerede `MockSmsSender`, kostnadsfritt), og selve QR-
registreringsflyten bruker aldri BankID uansett (se forrige seksjon). Skriptet ble i tillegg satt
opp til å KUN fylle inn mobilnummer (aldri e-post) — dermed null eksterne kall av noe slag under
hele testen, ikke bare "billige" kall. Satt opp som dev-admin (`AdminId`+passord-snarveien, ikke
BankID) i en dedikert, midlertidig "Lasttest"-gruppe med WHO-5 tilordnet, ryddet fullstendig
(prøvedata slettet mellom hvert nivå, selve gruppen arkivert og slettet permanent til slutt) —
beta står tilbake helt tomt for grupper, akkurat som før denne økten.

**Verktøy:** k6 (installert via `winget install GrafanaLabs.k6`), ett skript
(`lasttest-konferanse.js`, i scratchpad — IKKE en del av selve repoet, et engangsverktøy for denne
hendelsen) som per virtuell deltaker: låser opp `StagingGate` (kun et problem på beta — se
`StagingGate.cs`, `/BliPasient` er selv unntatt sperren, men `/Pasientportal/Tester/Fyll` er det
IKKE, et allerede kjent, ikke rettet hull i unntakslisten), henter registreringssiden, venter
2,5–5 sekunder (over `BotVern` sitt 2-sekunders minimum, simulerer et menneske som leser siden),
poster registreringen, og fyller ut alle sider i testen til "Ferdig" trykkes — alt via rå HTTP
(k6 følger redirects automatisk, egen cookie-jar per virtuell bruker, akkurat som en nettleser).

**Reell fallgruve underveis:** den første kjøringen feilet 100 % med "Noe gikk galt. Prøv igjen."
(BotVern sin bot-avvisning) — årsaken var at Razor HTML-koder attributtverdier ved rendering
(`+` → `&#x2B;` i `Vist`-tidsstempelfeltet), og skriptet sendte den kodede strengen rett tilbake
uten å dekode den først. Server-sidens `DateTimeOffset.TryParse` feilet da stille på den bokstavelig
talt feilformede strengen, og `BotVern.ErSannsynligvisBot` tolket det som en bot. Løst med en liten
HTML-entitet-dekoder i skriptet før verdier sendes tilbake som skjemafelt — en påminnelse om at
"skrap verdien rett fra utsendt HTML og post den tilbake uendret" IKKE er trygt uten å regne med
Razor sin automatiske HTML-koding av attributtverdier.

**Resultat — trinnvis eskalering (10/25/50/75/85/100 samtidige virtuelle deltakere), samme WHO-5,
samme gruppe, ryddet mellom hvert nivå:**

| Nivå | Fullført | Feilrate (HTTP) | Snitt responstid | p95 responstid |
|---|---|---|---|---|
| 10  | 10/10 (100 %) | 0 %    | 468 ms  | 1,17 s |
| 25  | 25/25 (100 %) | 0 %    | 1,08 s  | 2,88 s |
| 50  | 50/50 (100 %) | 0 %    | 2,47 s  | 5,09 s |
| 75  | 75/75 (100 %) | 0 %    | 4,57 s  | 10,9 s |
| 85  | 60/85 (71 %)  | 7,71 % | 6,52 s  | 14,4 s |
| 100 | 34/100 (34 %) | 15,0 % | 9,48 s  | 20,6 s |

**Konklusjon: systemet tåler IKKE 100 samtidige deltakere på dagens `testbase-test`-infrastruktur
(App Service Basic B1, én instans, ingen autoskalering + MySQL Burstable Standard_B1ms)** — det
fungerer feilfritt opp til et sted mellom 75 og 85 samtidige, og degraderer deretter raskt (kun
34 % fullførte ved 100). Under 75 er det ingen harde feil, men responstiden vokser lineært med
belastningen (fra under et halvt sekund ved 10 til nesten 11 sekunder i 95-persentilen ved 75) —
brukbart, men merkbart tregt mot slutten av det trygge området. Siden dette ble kjørt EFTER
"Feiltolerant varsling ved QR-registrering"-fiksen (forrige seksjon), er disse tallene den
FAKTISKE kapasiteten med den fiksen på plass — uten den ville trolig flere av de "vellykkede"
lave nivåene også vist sporadiske feil fra en treg/strupet SMS-/e-post-leverandør.

**Anbefaling til brukeren før foredraget (50–100 deltakere, 10 minutters vindu):** gitt at et reelt
foredrag typisk sprer registreringene over noen sekunder til et par minutter (ikke alle 100 i
akkurat samme sekund, slik lasttesten bevisst simulerte som verste-fall), er marginen trolig noe
bedre enn tabellen isolert antyder — men å stole blindt på det uten sikkerhetsmargin frarådes.
Konkret anbefaling: skaler `testbase-test` sin App Service opp til minst Standard S1 (åpner
autoskalering) og MySQL til minst General Purpose eller Burstable B2s for selve foredragsdagen
(via `azd provision` med endrede Bicep-parametre, reverserbart etterpå for å holde kostnaden nede).

## Lasttest mot beta, del 2: realistisk ankomstkurve for 1000 deltakere (2026-09-22)

Bruker stilte et godt motargument mot den forrige lasttestens metodikk: et ekte publikum som "sjekker
telefonen" reagerer ikke alle i samme millisekund slik `--vus`/`--iterations`-modusen i k6 simulerer
— reaksjonene sprer seg naturlig over et par minutter, og bruker mente 1000 deltakere trolig ville
gå greit gitt en slik spredning. Vurdering FØR ny test: sannsynligvis IKKE, siden mennesker som
reagerer på en skjermet oppfordring ("skann nå") typisk klumper seg tidlig (sosialt press, folk
følger naboen) fremfor å spre seg jevnt — og selv 75-brukers-testen over hadde allerede en naturlig
spredning på ~36 sekunder uten å unngå sammenbrudd rett over. I stedet for å fortsette å resonnere
fra antakelser på begge sider, ble det bygget en ny, mer realistisk lasttest og kjørt for å måle det
faktisk.

**Nytt skript:** `lasttest-konferanse-realistisk.js` (samme scratchpad, samme nullkostnad-oppsett —
kun mobilnummer, allerede mocket SMS på beta, dev-admin-innlogging) — bruker k6 sin
`ramping-arrival-rate`-executor (styrer ANKOMSTRATE over tid, ikke et fast antall virtuelle
brukere) med en bevisst front-tung kurve: rask stigning til 20 ankomster/sekund over 15 sekunder,
holdt i 15 sekunder til (toppen — "det sosiale presset"), rask nedgang til 3/sekund over 30
sekunder, så en lang hale på 3/sekund i 2 minutter (de trege etternølerne), til slutt ned mot null
— totalt ca. 1000-1200 forsøkte ankomster over ~3,25 minutter, matchende brukerens eget scenario
ordrett ("spredt over 2 minutter eller mer... 2-5 minutter totalt").

**Resultat: et nesten totalt sammenbrudd — langt verre enn den forrige, kunstige alt-på-en-gang-testen.**
Av rundt 1177 forsøkte ankomster (706 fullførte forsøk + 202 avbrutt da testen ble avsluttet + 269
droppet fordi k6 ikke rakk å starte dem i tide) endte KUN 9 pasienter opp registrert i det hele
tatt, og reelt sett fullførte trolig ingen hele test-utfyllingen (den egne "fullførte
besvarelser"-telleren ble aldri en gang inkrementert — 0 reelle fullføringer). Selv det ENKLEST
mulige kallet i hele flyten — å låse opp StagingGate, en ren minnebasert sjekk uten noen
databasekontakt i det hele tatt — feilet 64 % av tiden.

**Reelle Azure-metrikker for dette vinduet viser noe NYTT sammenlignet med forrige runde: denne
gangen kollapset selve App Service-en, ikke bare databasen:**

| Metrikk | Topp | Vurdering |
|---|---|---|
| MySQL CPU | 9,4 % | Fortsatt god margin |
| MySQL aktive tilkoblinger | 48 (LAVERE enn forrige runde — fordi de fleste forespørsler feilet FØR de nådde databasen) | — |
| **MySQL avbrutte tilkoblinger** | **opptil 83/minutt** | Reelle databasefeil, i motsetning til forrige, mildere test (der var den 0 gjennom hele vinduet) |
| **App Service gjennomsnittlig responstid** | **51,6 sekunder** | Praktisk talt full stopp |
| App Service trådtall | 3 → 93 | Trådopphopning |

I den forrige (mildere) testen holdt App Service sin CPU seg lav (under 20 % av én kjerne) hele
veien — konklusjonen den gangen var at MySQL var den trange flaskehalsen, ikke selve appen. Ved
denne mye høyere, mer realistiske belastningen (som paradoksalt nok TOTALT sett genererte FÆRRE
vellykkede databaseoperasjoner, siden de fleste forespørsler aldri kom så langt) ble i stedet
selve web-appen (Basic B1, én CPU-kjerne) den primære flaskehalsen — en klassisk "opphopnings"-
kollaps: så snart responstidene begynte å stige, fortsatte nye ankomster å komme i henhold til
ankomstplanen mens de gamle fortsatt satt fast, og systemet kom aldri tilbake i likevekt.

**Konklusjon, som svar på "kan dagens oppsett trolig håndtere 1000 deltakere i en
konferansesetting": nei, definitivt ikke** — selv med en realistisk, front-tung ankomstkurve over
mer enn 3 minutter (ikke en kunstig "alle samtidig"-eksplosjon) kollapser HELE stacken, ikke bare
databasen. Dette er en vesentlig skjerpelse av forrige runde sin konklusjon (som fortsatt pekte på
"skaler opp App Service OG MySQL" som riktig løsning, men denne testen viser at App Service-siden
er MINST like kritisk å skalere opp som databasesiden — kanskje mer, gitt at det var den som falt
sammen først under en realistisk ankomstkurve).

**Ryddet opp:** 9 prøvepasienter slettet, gruppen "Lasttest2" arkivert og deretter slettet
permanent — beta står tomt for grupper igjen, som etter forrige runde.

## Optimalisering før skalering: fire reelle funn, én reell grense (2026-09-22)

Bruker spurte, med rette, om det finnes optimaliseringer i EGEN kode å gjøre før man betaler for
mer kapasitet. Gjennomgang av den faktiske "hot path"-koden (QR-registrering → testutfylling)
avdekket fire konkrete, trygge forbedringer — implementert, testet lokalt, deployet til beta, og
RETESTET for å måle faktisk effekt (ikke bare anta at det hjalp).

**Fire endringer:**
1. **`GruppeService.FinnVedQrTokenAsync`/`FinnBehandlerVedQrTokenAsync` kortlevd-cachet** (10
   sekunder, `IMemoryCache`) — dette er token-oppslaget BliPasient gjør på BÅDE GET og POST, og
   under en registreringsbølge er det nøyaktig SAMME token slått opp av alle deltakerne samtidig.
   `AsNoTracking()` lagt til samtidig. QR-regenerering fjerner eksplisitt den gamle cache-oppføringen
   først, slik at "slutter å virke umiddelbart"-garantien i UI-teksten fortsatt holder.
2. **`TestService.HentTildelingMedInnholdAsync` sin test-STRUKTUR (spørsmål/sider) kortlevd-cachet**
   (60 sekunder) — denne dataen er identisk for alle deltakere som tar SAMME test og endres
   praktisk talt aldri midt i et event, men ble før lastet på nytt (3 spørringer) for hver eneste
   GET og POST av utfyllingssiden, per deltaker, per side.
3. **Fjernet en reell, unødvendig dobbel-spørring** i `TestTildelingsService`: `Behandler` ble
   hentet TO ganger for samme ID i samme forespørsel (én gang for PartnerId, én gang til inni
   `BeregnPrisPerTestAsync`) — nå hentet én gang og gjenbrukt.
4. **MySQL sin `max_connections` hevet fra standardverdien 171 til 250** (godt innenfor SKU-ens
   egen øvre grense på 341, `isDynamicConfig=true` — ingen restart i teorien, men se fallgruve
   under), og appens egen `Maximum Pool Size` i connection-stringen hevet fra MySqlConnector sin
   default (100) til 200 — begge nå Bicep-forvaltet (`infra/resources.bicep`), ikke en løs
   CLI-endring som ville forsvunnet ved neste `azd provision`.

**Reell fallgruve oppdaget underveis:** rett etter `azd provision` av `max_connections`-endringen
(punkt 4) kastet appen en ekte `MySqlException: Connect Timeout expired` på første forsøk —
til tross for at Azure sin egen dokumentasjon av parameteren sier `isDynamicConfig: true` ("ingen
restart nødvendig"), oppsto en kortvarig tilkoblingsforstyrrelse rett etter endringen ble
anvendt. Forbigående (fungerte normalt på nytt forsøk sekunder senere) — men en påminnelse om at
selv en "dynamisk" MySQL-parameterendring bør gjøres i god tid før noe skarpt, aldri rett før.

**Retest, samme metodikk som forrige runde (samme `lasttest-konferanse-realistisk.js`, samme
front-tunge ankomstkurve, ~1000-1200 forsøkte ankomster over ~3,25 minutter):**

| | Før optimalisering | Etter optimalisering |
|---|---|---|
| Reelt fullførte besvarelser | 0 | 0 |
| Registrerte pasienter | 9 | 10 |
| StagingGate-oppslag lyktes | 36 % | 19 % |
| App Service snitt-responstid (topp) | 51,6 s | 59,1 s |
| MySQL CPU (topp) | 9,4 % | 10,4 % |
| MySQL aktive tilkoblinger (topp) | 48 | 61 |

**Konklusjon: optimaliseringene gjorde INGEN målbar forskjell på 1000-deltakere-scenarioet — og
det er et reelt, forventet resultat, ikke en mislykket fiks.** Azure-metrikkene fra BEGGE kjøringer
viser samme mønster: MySQL sin CPU og tilkoblingstall forble lave hele veien (databasen var ALDRI
i nærheten av å bli en flaskehals her), mens App Service sin responstid uansett kollapset til
40-60+ sekunder i snitt. Når flaskehalsen er antall RÅ CPU-KJERNER en enkelt Basic B1-instans har
til å i det hele tatt ta imot og behandle tusenvis av samtidige socket-tilkoblinger — ikke antall
databasespørringer eller tilgjengelige databasetilkoblinger per forespørsel — hjelper det ikke å
gjøre hver forespørsel litt "lettere". Færre spørringer betyr fortsatt at HVER av de mange samtidige
forespørslene konkurrerer om de samme få prosessortrådene.

**Disse fire endringene er likevel IKKE bortkastet arbeid — de beholdes:**
- De reduserer reell unødvendig databasebelastning og kode-duplisering uansett skala (særlig
  relevant ved mer moderat, vedvarende bruk — mange behandlere/pasienter gjennom en vanlig
  arbeidsdag, ikke bare konferanse-spissbelastninger).
- De gir ekte, dokumentert margin på databasesiden (250 vs. 171 tilkoblinger) som blir nyttig i
  kombinasjon med en oppskalert App Service, selv om de alene ikke løser 1000-brukere-scenarioet.
- Ingen av dem introduserer risiko eller kompleksitet av betydning (korte cache-TTL-er,
  `AsNoTracking()` på rene lesespørringer, fjerning av en triviell duplikat-spørring).

**Endelig, ærlig svar til brukeren: nei, egen kode-optimalisering løser ikke 1000-deltakere-
scenarioet.** Den eneste veien dit er faktisk mer CPU-kapasitet på selve App Service-en (flere
kjerner og/eller flere instanser via autoskalering, som krever Standard-tier eller høyere) — se
forrige seksjons anbefaling om Standard S1. Databasesiden (nå enda bedre rustet enn før) var
aldri det reelle problemet ved denne skalaen.

**Metodisk lærdom, notert for senere:** en oppfølgende 100-brukers burst-test kjørt RETT ETTER
1000-deltakere-testen viste tilsynelatende DÅRLIGERE tall enn den opprinnelige (før-optimalisering)
100-brukers-testen — dette er IKKE en gyldig sammenligning og skal ikke tolkes som at
optimaliseringene gjorde ting verre. Systemet hadde ikke fått tid til å hente seg inn igjen etter
den ekstreme belastningen rett i forveien. Fremtidige før/etter-sammenligninger bør vente til et
helsesjekk-kall bekrefter normal responstid FØR neste kjøring startes.

## Lasttest mot beta, del 3: Standard S1 var feil neste steg (2026-09-22)

**Oppfølging av forrige seksjons anbefaling** ("Standard S1" ble pekt på som løsningen på
1000-deltakere-scenarioet). Brukerens instruks: skaler beta opp til "neste tier", kjør testene på
nytt, og hvis resultatet var godt nok, skaler live likt — ellers skaler beta ned igjen og husk
konklusjonen til neste gang.

**Infrastrukturendring, gjort reversibel med vilje:** `infra/resources.bicep` og `infra/main.bicep`
fikk fire nye parametere (`appServicePlanSkuName`/`appServicePlanSkuTier`/`mysqlSkuName`/
`mysqlSkuTier`, default hhv. `B1`/`Basic`/`Standard_B1ms`/`Burstable` — dagens verdier), koblet til
nye azd-miljøvariabler (`APP_SERVICE_SKU_NAME` osv.) i `infra/main.parameters.json`, i stedet for å
hardkode en ny SKU rett i Bicep-filen. Dette gjør at FREMTIDIGE opp-/ned-skaleringer for lasttesting
er en ren `azd env set` + `azd provision`-operasjon, ikke en kode-endring hver gang — direkte svar på
brukerens "husk det til neste test". **Viktig sikkerhetsdetalj under innføringen:** siden
`main.parameters.json` bruker ubetinget `"${VAR}"`-substitusjon (ingen fallback til Bicep sin egen
parameter-default hvis miljøvariabelen mangler — azd setter da verdien til en TOM streng, som ville
gitt en ugyldig SKU og feilet — eller i verste fall en util-tilsiktet SKU-endring på et miljø der
variabelen aldri var satt), ble BEGGE miljøer (`testbase-test` OG `testbase-beta`) eksplisitt satt
til dagens verdier FØR noen `azd provision` ble kjørt i det hele tatt — også live, selv om live
aldri skulle røres denne runden. Dette er nå et permanent mønster: en ny SKU-parameter av denne
typen krever alltid en eksplisitt "pin til nåværende verdi på alle miljøer"-runde før den tas i
bruk noe sted.

**Beta ble satt til App Service Standard S1 + MySQL Flexible Server Burstable Standard_B2s**
(`azd provision`, 6m8s, bekreftet med `az appservice plan show`/`az mysql flexible-server show`
etterpå). Helsesjekk kjørt tre ganger rett før selve lasttesten (alle tre `401` — StagingGate,
som forventet — på 79-89ms) for å garantere en gyldig før/etter-sammenligning, jf. forrige seksjons
metodiske lærdom.

**Ny gruppe + QR-token måtte lages fra bunnen** (forrige runde sin testdata var ryddet vekk, og
selve beta-databasen har ingen behandlere fra før). Underveis ble to reelle, ikke-trivielle
funn gjort om selve TEST-OPPSETTET (ikke om produktkoden):
- Admin/Grupper/Ny sin behandler-nedtrekksliste viser KUN behandlere med
  `Status != BehandlerStatus.Arkivert` — en fersk `Invitert`-behandler (ikke ferdig registrert)
  dukker OPP der og kan trygt eies en gruppe, uten at behandleren noensinne må fullføre
  BankID/2FA/e-post-verifiseringen sin. Nyttig snarvei for fremtidig test-oppsett: en ny
  behandler trenger ALDRI å fullføre egenregistreringen for å kunne eie en lasttest-gruppe.
- Både beta OG live har ekte Azure Communication Services e-postutsending WIRED INN ALLTID
  (opprettes ubetinget i `resources.bicep`, ikke bak en azd-miljøvariabel) — `MockEmailSender`
  brukes ALDRI i Azure, kun lokalt. En behandler-egenregistrering med en oppdiktet e-postadresse
  (f.eks. `@example.invalid`) sender dermed et ekte (aldri levert) e-postforsøk og
  bekreftelseskoden vises ALDRI i verken UI eller loggstrøm (i motsetning til SMS-koden, som
  fortsatt er ekte mock med UI-reveal siden `VONAGE_API_KEY` ikke er satt på beta) — se forrige
  punkt for hvorfor dette denne gangen ikke var nødvendig å løse.

**Lasttest-resultat (samme `lasttest-konferanse-realistisk.js`, samme ~1000-deltakere
ankomstkurve over ~3m15s): KLART DÅRLIGERE enn både før- og etter-optimalisering-kjøringene på
den GAMLE B1/B1ms-tieren.** Kun 1 fullført besvarelse (`fullforte_besvarelser`) av 754 fullførte
iterasjoner, 27,9 % av alle sjekker bestod (mot langt høyere andeler i tidligere runder),
gjennomsnittlig responstid 31,9s (opptil 60s/timeout), 47,3 % av alle HTTP-forespørsler feilet.

**Rotårsaken til hvorfor "neste tier" var feil valg — bekreftet med Azure-metrikker for
testvinduet:**
- App Service `CpuTime`: maks ~9,2 sekund CPU-tid per 60-sekunders måleperiode — under 16 %
  utnyttelse av ÉN kjerne. IKKE CPU-mettet.
- App Service `Threads`: klatret jevnt til 86 samtidige tråder mot slutten av testen, i takt med
  at `AverageResponseTime` klatret til 48,8 sekund — konsistent med trådpool-/kø-oppbygging, ikke
  med at CPU-en var full.
- MySQL `cpu_percent`: maks 6,1 % — B2s-en (dobbelt så mye CPU/RAM som B1ms) hadde enormt med
  ledig kapasitet.
- MySQL `active_connections`: maks 51 (godt under den hevede grensen på 250 fra forrige runde).
- MySQL `aborted_connections`: 71-123 per minutt under selve lastens topp — reelt tegn på at
  klienter (App Service-siden) ga opp/timet ut FØR MySQL rakk å svare, ikke at MySQL selv var
  overbelastet.

**Den avgjørende, tidligere upåaktede detaljen: Azure App Service Basic B1 og Standard S1 har
IDENTISK maskinvare** — begge er "small"-størrelsen med 1 vCPU / 1,75 GB RAM. Standard-tieren gir
KUN ekstra funksjoner (autoskalering, staging slots, daglige sikkerhetskopier, egendefinert
domene-SSL) — ALDRI mer prosessorkraft på samme størrelsesbokstav. Forrige seksjons konklusjon
("...krever Standard-tier eller høyere... se forrige seksjons anbefaling om Standard S1") var
dermed en reell feilslutning: den pekte riktig på at CPU-kjerner (ikke databasekapasitet) er
flaskehalsen, men "Standard S1" alene endrer ALDRI antall kjerner — kun "S2" (2 vCPU/3,5 GB) eller
høyere (eller Premium v3-serien) gjør det. Denne kjøringen endte opp som et rent, informativt
negativt eksperiment: den isolerte at en STØRRE database alene (B1ms→B2s) ikke endrer noe når
flaskehalsen ligger et helt annet sted, og at tallene ble merkbart VERRE enn før — sannsynligvis
fordi 1000 samtidige tilkoblingsforsøk mot samme trange 1-kjerners motpart under en NYLIG
tier-endret App Service (mulig kald oppstart/re-JIT rett etter provisjonering) er enda mer
ustabilt enn mot en lenge kjørende, "varm" instans, selv om selve maskinvaren i teorien er lik.

**Konklusjon til brukeren: IKKE godt nok — live ble ALDRI rørt denne runden.** Beta satt tilbake
til `B1`/`Basic` + `Standard_B1ms`/`Burstable` (samme `azd provision`-mønster, 6m19s), bekreftet
med `az appservice plan show`/`az mysql flexible-server show` (tilbake til opprinnelige verdier)
og helsesjekk (`beta` `401`, `live` uendret `200`). All testdata ryddet (gruppen arkivert+slettet
permanent, test-behandleren arkivert+slettet permanent) via Playwright på samme måte som
tidligere runder.

**Husk til neste test (brukerens eksplisitte ønske): den faktiske neste eksperiment-verdige
tieren er App Service `S2` (2 vCPU/3,5 GB) — ikke `S1`.** De nye `APP_SERVICE_SKU_NAME=S2`/
`APP_SERVICE_SKU_TIER=Standard`-miljøvariablene er nå bare én `azd env set`-kommando unna på
`testbase-beta` (samme mekanisme som denne runden innførte) — vurder også å teste MED
autoskalering aktivert (kun mulig på Standard+, se `docs/beslutningslogg.md` tidligere seksjon om
autoskalering) fremfor en fast, større enkelt-instans, siden konferanse-scenarioet i sin natur er
en kortvarig spiss, ikke vedvarende last.

## Lasttest mot beta, del 4: Standard S2 — reelt gjennombrudd (2026-09-22, samme dag)

**Oppfølging av forrige seksjons "husk til neste test"-anbefaling.** Brukerens instruks denne
runden var kortere: "prøv neste nivå da, og rapporter tilbake" — App Service ble satt til
`S2` (2 vCPU/3,5 GB), MySQL BEVISST holdt uendret på `Standard_B1ms`/Burstable (samme mekanisme fra
forrige seksjon: `azd env set APP_SERVICE_SKU_NAME S2` + `_TIER Standard`, deretter
`azd provision`, 3m48s). Å holde MySQL fast var et bevisst valg for å isolere ÉN variabel av
gangen — forrige runde viste at databasen aldri var nær en flaskehals ved denne skalaen, så en
eventuell forbedring denne gangen kan da med rimelig sikkerhet tilskrives ekstra CPU-kjerner alene.
Helsesjekk kjørt 5 ganger (alle `401`, 60-300ms) før lasttesten for å bekrefte et friskt, oppvarmet
utgangspunkt.

**Ny testgruppe måtte lages på nytt** (samme mønster som del 3 — forrige runde sin testdata var
ryddet vekk). Ekstra snarvei oppdaget denne runden: en behandler trenger IKKE engang fullføre selve
`Inviter/Fullfor`-registreringsskjemaet for å eies en gruppe — det holder å sende SELVE
invitasjonen (Behandler-raden opprettes med `Status = Invitert` med det samme), siden
`Admin/Grupper/Ny` sin behandler-nedtrekksliste kun ekskluderer `Arkivert`. Sparer et helt steg
(og dermed hele e-post-verifiserings-problemet fra del 3) i fremtidige oppsett.

**Lasttest-resultat (samme `lasttest-konferanse-realistisk.js`, samme ~1000-deltakere
ankomstkurve): DRAMATISK bedre enn BÅDE B1-baseline OG S1-forsøket.**

| Mål | B1 (opprinnelig) | S1 + B2s (del 3) | **S2 (denne runden)** |
|---|---|---|---|
| Fullførte besvarelser | ~0 | 1 | **473** |
| Sjekker bestått | lavt (ikke presist tallfestet) | 27,9 % | **82,4 %** |
| Snitt HTTP-responstid | 51-59s | 31,9s | **13,2s** |
| p95 responstid | ~60s (timeout) | 48,0s | **33,2s** |
| StagingGate-oppslåsing bestått | — | 46 % | **99,5 % (998/1003)** |

**Azure-metrikker for testvinduet bekrefter HVORFOR:**
- App Service `CpuTime`: toppet på 59,8 sekund CPU-tid per 60-sekunders måleperiode — mot 9,2 på
  B1/S1 (som begge kun har 1 kjerne). Med 2 kjerner tilgjengelig betyr dette at appen nå FAKTISK
  klarte å bruke den ekstra kapasiteten — omtrent 50 % samlet utnyttelse av begge kjernene under
  lastens topp, en categorisk endring fra "hjelpeløst kø-bundet på én kjerne".
- App Service `Threads`: 51 samtidige tråder mot slutten (lavere enn S1-forsøkets 86, til tross for
  MER trafikk kom gjennom) — konsistent med at tråder nå faktisk fikk gjort arbeid i stedet for å
  stå og vente.
- App Service `Http5xx`: 400 feil i det travleste minuttet — reelle serverfeil oppstod fortsatt
  under press, ikke null, men systemet som helhet holdt seg oppe og fullførte likevel de fleste
  forespørslene.
- MySQL `cpu_percent`: klatret til 26,9 % (mot 6,1 % i forrige runde) — fortsatt langt fra mettet,
  men synlig høyere nå som mer trafikk faktisk NÅDDE databasen i stedet for å kø seg opp i
  App Service-laget.
- MySQL `active_connections`: toppet på **222 av det hevede taket på 250** — den nye, reelle
  flaskehalsen. `aborted_connections` spikte til 391 i det travleste minuttet, sannsynligvis nå en
  blanding av reelle tilkoblingsavslag NÆR grensen og fortsatt noen App Service-side timeouts.

**Konklusjon: `S2` er det første virkelig funksjonelle skalerings-trinnet for
konferanse-scenarioet, men IKKE en fullstendig løsning ennå** — 473 av 1003 forsøkte iterasjoner
fullførte (mot en teoretisk topp på ~1000 registrerte deltakere), og MySQL sin tilkoblingsgrense
(fortsatt `Standard_B1ms`, 250 maks-tilkoblinger) er nå tydelig i ferd med å bli den begrensende
faktoren etter hvert som App Service-siden er avlastet. **Naturlig neste eksperiment: `S2` +
`Standard_B2s` (eller høyere) SAMTIDIG** — nå som begge lag har blitt bekreftet som reelle,
sekvensielle flaskehalser (App Service FØRST, deretter MySQL), er det rimelig å forvente at en
kombinert oppskalering kommer enda nærmere 100 % fullføring.

**Beta satt tilbake til `B1`/`Basic`** (samme reversible mønster, `azd provision` 3m54s, bekreftet
med `az appservice plan show` og helsesjekk `401`/live uendret `200`). All testdata ryddet: 492
prøvepasienter slettet via "Slett prøvedata"-knappen FØR gruppen ble arkivert+slettet permanent,
test-behandleren arkivert+slettet permanent. Live ble ALDRI rørt denne runden heller — brukeren ba
kun om selve neste-nivå-testen og en rapport, ikke en "godt nok → skaler live"-avgjørelse ennå.

## STØ avviste fødselsnummer-bestilling for ekte BankID — løsning: dropp NNIN-scope (2026-09-22)

**Bakgrunn:** GGPsykolog AS sin bestilling av ekte BankID (via reselgeren Stø AS) for
`Miljo:EktBankIdProfesjonell`-produksjonsflyten (admin/behandler sin FELLES innlogging, se
`Pages/Konto/LoggInn`) ble avvist av Stø sin kundekontroll. Avvisningsgrunn (ordrett): "BankID
tjenesten er i utgangspunktet ikke et utleveringssted for fødselsnummer... GGPsykolog AS er
[ikke] et registrert helseforetak eller har oppgitt at de er databehandler på vegne av andre
helseforetak eller autorisert helsepersonell med egne foretak." GGPsykolog AS vil aldri kunne
oppfylle "helseforetak"-kravet.

**Undersøkt (BankID sin offentlige OIDC-dokumentasjon, `developer.bankid.no`) og konkludert:**
avvisningen gjelder KUN scopene som faktisk ber BankID om å UTLEVERE fødselsnummeret som en claim
(`nnin`/`nnin_altsub`) — disse krever "legal basis" og er nettopp det Stø sin kundekontroll
sjekker. `openid`+`profile`-scopene (minimal ID-token: anonym autentisering + navn/fødselsdato)
er derimot UBETINGET tilgjengelig for enhver klient, ingen kvalifiseringssjekk. Ettersom
TestBase ALLEREDE samler inn personnummer direkte fra brukeren selv ved egenregistrering (behandler
sin `Inviter/Fullfor`, pasient sin `PasientRegistrering/Fullfor`/`BliPasient` — se
`Areas/*/Pages/Pasienter`/`Pages/Inviter`) og lagrer det kryptert (DataProtection), er den EKTE
BankID-innloggingens jobb kun å AUTENTISERE en allerede kjent person — ikke å slå opp/motta et
ukjent fødselsnummer fra Stø. Norsk rett (personopplysningsloven §12, jf. Datatilsynets veiledning)
tillater uansett behandling av fødselsnummer når det er reelt behov for sikker identifisering —
identifisering av pasienter i et helsedatasystem er et opplagt tilfelle, UAVHENGIG av om
fødselsnummeret kommer fra BankID sin claim eller fra brukerens eget skjema.

**Besluttet retning (brukerens eksplisitte valg): "Rute 1" — dropp NNIN-scopet helt.**
Fremtidig `Miljo:EktBankIdProfesjonell`-produksjonsintegrasjon skal be KUN om `openid`+`profile`,
ALDRI `nnin`/`nnin_altsub` — BankID blir en ren sterk-autentiseringslager oppå det
selv-registrerte, allerede krypterte personnummeret, ikke en oppslagskilde for det. Krever en
arkitekturendring i matching-logikken (i dag: `AdminAuthenticationService`/
`BehandlerAuthenticationService` sin `FinnVedPersonnummerAsync` forventer et personnummer FRA
selve BankID-svaret) — istedenfor må en stabil per-relying-party-pseudonym-claim (BankID sin
dokumentasjon nevner `bankid_altsub` som mer stabil enn den ustabile `sub`-clamien, men eksakt
stabilitetsgaranti IKKE bekreftet i denne runden — verifiser i BankID sin fulle
klient-provisjonerings-dokumentasjon før implementering) bindes til kontoen ved FØRSTE ekte
BankID-innlogging etter selvregistrering, og brukes for gjenkjenning ved senere innlogginger.
IKKE implementert ennå — kun besluttet retning, notert her for en senere økt.

**Parallelt, brukerens eget valg: reapplyer likevel for `nnin_altsub`-scopet hos Stø**, denne gang
med presisering om at fødselsnummeret ALLEREDE er kjent (samlet inn direkte fra brukeren), ikke
bedt utlevert som ny informasjon — matcher `nnin_altsub` sin dokumenterte, snevrere
bruksbegrensning ("can not be used to onboard new customers if you don't already possess their
national identity number") bedre enn den generelle avvisningsteksten antyder. De to sporene er
ikke gjensidig utelukkende — Rute 1 er hovedplanen uansett utfall av reapplikasjonen.

**VIKTIG: dette er IKKE juridisk rådgivning** — samme forbehold som resten av
`docs/compliance-dpia-utkast.md`. Den konkrete §12/GDPR-vurderingen over bør kvalitetssikres av
prosjektets jurist/DPO før noen arkitekturendring rulles ut mot ekte pasientdata. Denne saken
gjelder for øvrig KUN admin/behandler-innlogging — pasient bruker fortsatt alltid
`MockBankIdProvider` uansett (bevisst arkitekturvalg, se CLAUDE.md), og er derfor helt urelatert
til morgendagens (2026-09-23) konferanse-QR-registrering.

## Lasttest mot beta, del 5: S2+B2s reproduserbart DÅRLIGERE — LIVE skalert til S2 alene for
ekte konferanse (2026-09-22, samme dag)

**Kontekst:** brukeren varslet at den ekte konferansen (50-1000 deltakere, QR-basert
WHO-5-registrering) er 2026-09-23 kl. 11:50-13:00 norsk tid — én dag etter del 4s S2-funn. Bedt om
å validere `S2`+`Standard_B2s` SAMMEN på beta FØR noe rulles til live (brukerens eksplisitte valg
i en oppfølgingsspørring), siden del 4 kun testet App Service alene.

**Resultat: S2+B2s presterte REPRODUSERBART DÅRLIGERE enn S2 alene — TO ganger, ikke ferske
tallfeil.**

| Kjøring | Fullførte besvarelser | Sjekker bestått | Snitt responstid |
|---|---|---|---|
| S2 alene (del 4) | 473 | 82,4 % | 13,2s |
| S2+B2s, rett etter provisjonering | 3 | 32,6 % | 24,5s |
| S2+B2s, etter ~4 min oppvarming (20× DB-berørende GET-kall) | 15 | 28,3 % | 28,1s |

Oppvarmingshypotesen (MySQL-tier-bytte trigger en 60-120 sekunders restart + kald InnoDB
buffer-pool, se tidligere "metodisk lærdom" i denne loggen) ble EKSPLISITT testet og AVKREFTET —
oppvarmet kjøring var ikke bedre, om noe marginalt verre. MySQL-siden selv var IKKE flaskehalsen i
noen av de to kjøringene (`cpu_percent` < 7 %, `active_connections` maks 106 av 250) — problemet lå
på App Service-siden, men de vanlige `CpuTime`/`Threads`-metrikkene kunne ikke hentes for den andre
kjøringen (Azure Monitor API returnerte uventet `BadRequest: Failed to find metric configuration...
Valid metrics: MemoryWorkingSet,AverageMemoryWorkingSet,InstanceCount` — et API-datapunkt som i seg
selv er mistenkelig og kan tyde på at selve App Service-instansen ble omprovisjonert/erstattet
under den KOMBINERTE SKU-endringen, ikke bare skalert — IKKE undersøkt videre pga. tidspress før
konferansen; verdt å følge opp senere om mønsteret gjentar seg). Konklusjon: å endre BEGGE
ressursene (App Service-plan OG MySQL-server-SKU) i SAMME `azd provision`-kall ser ut til å gi en
reell, reproduserbar ustabilitet som IKKE forsvinner med tid — ikke bare et forbigående
kald-start-fenomen slik del 3s "metodiske lærdom" antok.

**Beslutning: LIVE (`testbase-test`, `www.psytest.no`) skalert til `S2` ALENE for konferansen —
MySQL UENDRET på `Standard_B1ms`.** Dette er den ENESTE konfigurasjonen som har vist konsistent
gode resultater (473/1000, 82 % — division 4). Verifisert på selve live-miljøet: `az appservice
plan show` bekrefter `S2`/`Standard`, `az mysql flexible-server show` bekrefter uendret
`Standard_B1ms`, 5× helsesjekk `200` på under 400ms. `azd provision` tok 3m59s
(`testbase-test-1790085185`-deployment).

**Sikkerhetsdetalj:** samme "pin til nåværende verdi FØR endring"-mønster fra del 3 ble fulgt —
`testbase-test` sine `APP_SERVICE_SKU_NAME`/`_TIER`/`MYSQL_SKU_NAME`/`_TIER`-miljøvariabler var
allerede satt til dagens verdier (fra del 3s sikkerhetsrunde), så kun `APP_SERVICE_SKU_NAME=S2`/
`_TIER=Standard` ble endret denne gangen — MySQL-variablene ble bevisst IKKE rørt.

**Oppfølging planlagt for etter konferansen (2026-09-23, ca. 13:15 norsk tid):** en
CronCreate-jobb (session-only — brukeren har bekreftet å holde denne Claude Code-økten åpen
gjennom natten, noe som gjør den langt mer pålitelig enn normalt, men IKKE en garanti; brukeren er
bedt om å uansett starte/gjenoppta en økt etter kl. 13:00 i morgen som backup) er satt opp til å:
hente ekte Azure-metrikker for konferansevinduet, hente ekte deltaker-/fullføringstall fra selve
konferansegruppen, dokumentere funnene her (sammenlignet med k6-simuleringens 473/1000-anslag), og
skalere LIVE tilbake til `B1`/`Basic` + `Standard_B1ms`/`Burstable` etterpå.

**Testdata ryddet på beta** (samme mønster: "Slett prøvedata" før arkivering+permanent sletting av
BEGGE gruppene fra denne runden, test-behandleren likeså), beta satt tilbake til `B1`/`Basic` +
`Standard_B1ms`/`Burstable` (`azd provision` 6m3s, bekreftet). Beta ble IKKE holdt oppskalert —
brukeren presiserte eksplisitt at kun live trengte forhøyet tier for morgendagen.

## Etter konferansen (2026-09-23): gruppen kunne IKKE identifiseres med sikkerhet — live skalert
ned, ingen konklusjon om selve konferansens ytelse trukket ennå

**Den planlagte oppfølgingsjobben** (CronCreate, satt opp i forrige seksjon) kjørte som planlagt
kl. 13:17 norsk tid, altså rett etter konferansens oppgitte sluttidspunkt (11:50-13:00). Målet var å
hente ekte Azure-metrikker og ekte deltaker-/fullføringstall for konferansen, sammenligne med
k6-simuleringens 473/1000-anslag fra "Lasttest mot beta, del 4", og skalere live tilbake ned.

**MySQL-metrikker for vinduet 09:45-11:05 UTC (11:45-13:05 norsk tid) viser praktisk talt INGEN
aktivitet utover idle-grunnlinje:** `active_connections` toppet på 7 (mot en idle-grunnlinje på 5
resten av tiden), `aborted_connections` var 0 gjennom hele vinduet (kun ett enkeltstående
2-tilfelle). Til sammenligning viste selv den MINSTE k6-lasttesten denne uken (10-25 VUs, tidlig i
"Lasttest mot beta, del 1") `active_connections` godt over dette. Dette tyder sterkt på at INGEN
betydelig registreringsbølge traff databasen i dette tidsvinduet, uansett hvilken mekanisme
konferansen faktisk brukte.

**App Service sine klassiske metrikker (CpuTime/Threads/Http5xx/AverageResponseTime) var
UTILGJENGELIGE for live sin App Service-ressurs** (`az monitor metrics list-definitions` viser at
KUN `MemoryWorkingSet`/`AverageMemoryWorkingSet`/`InstanceCount` er registrert for
`app-testbase-tk46vyxboocho` akkurat nå — samme feilmelding som dukket opp for BETA under del 5s
andre S2+B2s-kjøring i går). Beta (nå tilbake på `B1`/`Basic`) har derimot ALLE klassiske
metrikker tilgjengelig akkurat nå, bekreftet ved samme kommando. Dette er altså IKKE en generell
Azure-regresjon eller noe knyttet til S2-tieren spesifikt (siden beta hadde disse metrikkene
tilgjengelig BÅDE før og etter egne SKU-endringer i går) — det er spesifikt knyttet til
LIVE-ressursen, årsak fortsatt ukjent. Verdt å undersøke videre en annen gang (f.eks. sjekke
diagnostic settings/Application Insights-tilknytning på live vs. beta), men blokkerer ikke selve
appens funksjon (helsesjekk er og har vært `200` hele veien).

**Selve konferansegruppen kunne IKKE identifiseres med sikkerhet.** `Admin/Grupper` på live viser
kun ÉN gruppe totalt: "Test Gruppe", opprettet 2026-09-20 (TRE dager før konferansen), eid av en
behandler, med KUN "Spillavhengighet ICD-11 GADIT" (et pengespillavhengighet-screeningverktøy)
tilordnet — IKKE WHO-5, som var testen konferansen skulle bruke. Gruppen har ingen
Startdato/Sluttdato satt. Dette matcher IKKE beskrivelsen av dagens WHO-5-baserte
QR-konferanse i det hele tatt.

**Viktig sikkerhetsfunn, IKKE håndtert videre denne runden:** `Admin/Pasienter` viser at store deler
av denne gruppens 43 pasienter har det som ser ut som EKTE navn, e-postadresser (inkl. domener som
tyder på en reell fagperson-/organisasjonssammenheng rundt pengespillavhengighet) og i minst ETT
tilfelle et fullstendig utfylt, gyldig-utseende norsk personnummer — IKKE syntetisk
test-mønster-data. Disse radene er BEVISST IKKE rørt, IKKE slettet, IKKE brukt til noen
rapportgenerering, og verken navn, e-post eller personnummer er gjengitt her eller andre steder i
kildekontrollert dokumentasjon — kun dette generiske varselet. Dette kan være en tidligere, reell
(og muligens fullt gyldig/tilsiktet) bruk av systemet av brukeren selv til et annet formål enn
denne ukens WHO-5-konferanse — MEN det kunne ikke bekreftes uten å spørre brukeren direkte, så
INGEN antakelse om at dette er trygt testdata ble gjort. Se `CLAUDE.md` sitt prinsipp "Ingen ekte
pasientdata i dev/test noensinne" — dette gjelder eksplisitt IKKE på samme måte for selve
LIVE-miljøet (der ekte pasientdata på et tidspunkt er selve formålet), men det MÅ i så fall skje
bevisst og med fullt samtykke/korrekt rettslig grunnlag, ikke oppdages tilfeldig av en automatisert
oppfølgingsjobb som dette.

**Handling denne runden: KUN det trygge, tidssensitive steget ble gjennomført.** Live skalert
tilbake til `B1`/`Basic` + `Standard_B1ms`/`Burstable` (uendret — MySQL ble aldri rørt for live i
det hele tatt denne uken), bekreftet med `az appservice plan show` (`B1`/`Basic`) og tre helsesjekk
(`200`, 0.3-0.5s). `azd provision` tok 5m5s. Dette var trygt å gjøre uavhengig av
gruppe-usikkerheten over, siden det er rent reversibelt og tidspunktet uansett var forbi
konferansens oppgitte sluttid.

**IKKE gjort denne runden, avventer brukerens avklaring:**
- Steg 2/3 fra oppfølgingsjobbens instruks (finne ekte deltaker-/fullføringstall, skrive en
  sammenligning mot k6-simuleringens 473/1000-anslag) — kan ikke gjøres pålitelig før riktig
  gruppe/mekanisme er identifisert.
- Ingen konklusjon trukket om hvorvidt `S2`-skaleringen faktisk hjalp eller ikke under den ekte
  konferansen, siden det ikke er bekreftet at konferansen genererte merkbar trafikk i det hele tatt.
- "Test Gruppe" sitt datainnhold — verken ryddet, undersøkt videre, eller antatt trygt.

**Spørsmål til brukeren (se sesjonens svar når de kommer):** hvilken gruppe/QR-kode/mekanisme ble
faktisk brukt for dagens konferanse? Ble den kanskje ikke gjennomført, utsatt, eller brukte en helt
annen URL/metode enn `Admin/Grupper`-systemet (f.eks. en behandler sin EGEN QR uten gruppe, som per
arkitekturen viser kun en bekreftelsesside og ikke automatisk testtildeling)? Og: hva ER egentlig
"Test Gruppe" fra 2026-09-20 — et tidligere reelt screening-arrangement som bevisst skal beholdes?

**Oppfølging samme dag: brukeren bekreftet "Test Gruppe" ER den faktiske konferansegruppen** (til
tross for GADIT-testen og 2026-09-20-datoen — begge var altså bevisste/forventede, ikke tegn på feil
gruppe). Straks etter bekreftelsen meldte brukeren en reell 500-feil ved forsøk på å generere
grupperapporten, med en `Request-ID` fra ASP.NET Cores standard feilhåndteringsmiddleware.

## Reell 500-feil i GADIT-skåring: posisjonsbasert antagelse knakk på et ubesvart spørsmål
(2026-09-23)

**Rotårsak, funnet via `az webapp log tail` mot live rett etter brukeren reproduserte feilen:**

```
System.FormatException: The input string 'Nei' was not in a correct format.
   at System.Int32.Parse(String s)
   at GaditSkaaringsberegner.BeregnSkaaring(...) i GaditSkaaringsberegner.cs:line 22
```

`GaditSkaaringsberegner.BeregnSkaaring` antok en FAST POSISJON i `svar`-listen: ledd 0-5 =
frekvensspørsmål (numerisk verdi 0-4), ledd 6-7 = Ja/Nei-spørsmål. Denne antagelsen holder KUN hvis
ALLE 8 ledd faktisk er besvart. `TestService.LagreSvarAsync` hopper imidlertid stille over
tomme/ubesvarte felt (`if (string.IsNullOrWhiteSpace(verdi)) continue;`, linje 763) — det lagres
ALDRI en `TestSvar`-rad for et ledd pasienten ikke svarte på, og INGENTING hindrer at testen likevel
markeres `Fullfort`. En pasient som hoppet over ETT av de 6 frekvensspørsmålene endte dermed opp med
kun 7 `TestSvar`-rader — det 6. elementet i listen (som skulle vært ledd 7s "Ja"/"Nei") ble tolket
som ledd 6 (frekvens) og `int.Parse("Nei")` kastet.

**Systemisk funn, IKKE fikset denne runden:** samme mønster finnes i `Phq9Skaaringsberegner`
(`svar.Take(9)` for å EKSKLUDERE det 10. funksjonsspørsmålet fra sumskåren) — men siden ALLE 10
PHQ-9-ledd er samme svartype (numerisk `LikertSkala`), ville et ubesvart tidlig spørsmål der IKKE
krasje, men i stedet STILLE inkludere funksjonsspørsmålets verdi i depresjons-sumskåren og gi et
FEIL tall uten noen feilmelding — potensielt alvorligere enn GADIT sin synlige krasj, siden ingen
ville blitt varslet. `TestService.BeregnSkaaringAsync` sin egen kodekommentar (linje 972-977)
bekrefter at dette posisjonsbaserte mønsteret er BEVISST brukt av flere skåringsklasser, ikke en
enkeltstående glipp. IKKE undersøkt om andre skåringsklasser (Eq40, Idq, Paq11R, Picd, Traps-I,
osv.) har lignende sårbarhet — bør gjennomgås systematisk en annen gang, ikke under dette
akutte presset.

**Fiks (kun GADIT denne runden):** `GaditSkaaringsberegner.BeregnSkaaring` klassifiserer nå HVERT
svar etter sin egen VERDI (tallparses = frekvensspørsmål, "Ja"/"Nei" = Ja/Nei-spørsmål) i stedet for
posisjon i listen — korrekt uavhengig av rekkefølge OG uavhengig av hvor mange/hvilke ledd som
faktisk ble besvart (et ubesvart ledd bidrar naturlig med 0, siden det rett og slett ikke finnes i
`svar`). Ny regresjonstest lagt til i `SkaaringsberegnereTests.cs`
(`Gadit_UbesvartMidtstiltFrekvensledd_KraskerIkkeOgTellerResterendeSvarRiktig`) som reproduserer
akkurat dette scenarioet. Alle 28 skåringstester (og alle andre rene enhetstester uten
databaseavhengighet) grønne før deploy — de 7 testene som feilet (Rediger-/HeleFlyten-/
BetalingPipeline-testene) feiler pga. manglende lokal Docker/MySQL-tilkobling, IKKE relatert til
denne endringen.

**Deployet til live med `azd deploy`** (2m58s, IKKE `azd provision` — ren kodeendring, ingen
infrastrukturendring), helsesjekk `200` etterpå. **Verifisert ende-til-ende i nettleser** (samme
side brukeren selv brukte): "Generer Gruppe Rapport" (ekte pasienter, testId 17/GADIT) — 1 deltaker,
ingen krasj. "Generer Temp Gruppe Rapport" (prøvedata, samme test, 2026-09-01–2026-09-23) — 34
deltakere, ingen krasj, inkludert nettopp den pasienten hvis ufullstendige besvarelse forårsaket
det opprinnelige 500-feilen.

**Ingen infrastruktur rørt denne runden** — kun en ren kodefiks. Dette var en ekte, brukerrapportert
produksjonsfeil på ekte (brukerbekreftet) konferansedata, ikke et testscenario.

## Tre brukerfeedback-punkter fra selve konferansen: fjern "Lagre"-knappen, dropp personnummer i
steg 1, normert gjennomsnitt-imputering + gyldighetsgrense (2026-09-23)

Brukeren observerte deltakerne direkte under konferansen og meldte tre distinkte funn/ønsker samme
dag som GADIT-feilen over.

### 1. "Lagre"-knappen fjernet fra Pasientportal/Tester/Fyll

Flere deltakere trodde et klikk på "Lagre" betydde at testen var LEVERT til behandler. Den gjorde
ikke det — den lagret bare gjeldende sides svar og ble stående på SAMME side. Undersøkt: dette var
allerede reelt OVERFLØDIG funksjonalitet — `Neste`/`Ferdig` lagrer UANSETT gjeldende sides svar FØR
de flytter videre (samme `TestService.LagreSvarAsync`-kall), så "Lagre" ga null praktisk fordel og
KUN forvirring. Fjernet knappen helt fra `Fyll.cshtml` — ingen kodeendring nødvendig utover selve
markup-fjerningen, siden lagre-og-fortsett allerede var (og fortsatt er) standardoppførselen.

### 2. Personnummer fjernet fra `BliPasient` (steg 1) — flyttet utelukkende til
`PasientRegistrering/FullforProfil` (steg 2, allerede eksisterende)

Feltet var allerede VALGFRITT i steg 1, men bremset/forvirret rask selvregistrering under press.
`PasientRegistrering/FullforProfil` samler ALLEREDE inn personnummer (blant navn/kjønn/adresse) som
en etablert "neste steg"-side — ingen ny side trengtes. Fjernet `Personnummer`-feltet fullstendig
fra `BliPasient/Index.cshtml` (markup) OG `Index.cshtml.cs` (BindProperty + valideringsblokk),
`RegistrerViaQrAsync` kalles nå alltid med `personnummer: null` fra denne siden. Blank personnummer
er fortsatt "prøv systemet"-terskelen (uendret), bare at steg 1 nå ALDRI kan sette det til noe
annet enn null uansett.

### 3. Normert gjennomsnitt-imputering + gyldighetsgrense (ny, generell mekanisme)

Brukerens ønske: mange standardiserte tester har publisert normeringslitteratur (populasjons-
gjennomsnitt per ledd) og en kjent grense for hvor stor andel ubesvarte ledd som gjør resultatet
klinisk UPÅLITELIG. Ønsket oppførsel: aldri blokkere innsending, men (a) imputere normert
gjennomsnitt for et ubesvart ledd der det er kjent, slik at et tall likevel kan beregnes, og (b)
varsle behandler (rapportvisning) — og pasienten selv, i en mildere form — når andelen ubesvart
overskrider en grense.

**Datamodell (ny migrasjon `LeggTilGyldighetsgrenseOgNormertGjennomsnitt`):**
- `Test.MaksUbesvartProsent` (`int?`) — maks andel (0-100) ubesvarte ledd før gyldighetsadvarsel.
  Null (default for ALLE eksisterende tester) = funksjonen AV for den testen.
- `TestLedd.NormertGjennomsnitt` (`decimal(10,4)?`) — normert (populasjons-)gjennomsnittssvar for
  ETT ledd, brukt til imputering. Null (default for ALLE eksisterende ledd) = ingen imputering for
  det leddet, akkurat som før.

**BEVISST IKKE fylt inn noen verdi for noen eksisterende test/ledd denne runden** — verken
gyldighetsgrense eller normerte gjennomsnitt er ekte tall vi har verifisert mot publisert
normeringslitteratur per test, og å dikte opp plausible tall for reelle kliniske screeningverktøy
ville vært aktivt uansvarlig. Mekanismen er derfor bygget og FULLT FUNKSJONELL, men SOVENDE for
alle 20+ innebygde tester inntil noen (brukeren, en fagperson) legger inn ekte, siterte verdier —
enten direkte i databasen eller (senere) via en admin-UI som IKKE er bygget ennå (ingen UI for å
redigere ledd finnes fra før, se CLAUDE.md "Admin/Tester/Rediger dekker kun testens egne felt").

**`TestService.BeregnSkaaringAsync` skrevet om:** henter nå ALLE testens ledd (ikke bare besvarte)
for å kjenne den faktiske nevneren. Bygger en fullstendig, ordnet svarliste der et ubesvart ledd MED
kjent normert gjennomsnitt får et syntetisk (ALDRI lagret) `TestSvar` med den AVRUNDEDE normerte
verdien (`Math.Round(..., AwayFromZero)` — nesten alle skåringsberegnere gjør et rått `int.Parse`,
siden selve svarskalaen alltid er heltallsbasert, mens et normert gjennomsnitt fra litteraturen ofte
ikke er det). Et ubesvart ledd UTEN kjent normert gjennomsnitt er fortsatt bare fraværende fra
listen, akkurat som tidligere. Hvis `Test.MaksUbesvartProsent` er satt og andelen ubesvart
overskrider den, settes `TestSkaaring.GyldighetsAdvarsel` (ny, valgfri record-egenskap, default
null — ingen eksisterende skåringsberegner-kallsted trengte endring).

**UI:**
- `Behandlerportal/Pasienter/Rapport.cshtml`: en gul advarselsboks med FULL teknisk
  gyldighetsadvarsel-tekst i "Resultat"-seksjonen, BÅDE i den vanlige visningen og i
  "Kopier til utklippstavle"-malen (slik at advarselen følger med hvis rapporten limes inn i et
  journalsystem).
- `Pasientportal/Tester/Fyll.cshtml`: en mildere, IKKE-teknisk informasjonsboks på "Ferdig!"-siden
  ("Du hoppet over noen spørsmål...") — bevisst IKKE samme rå tekst som behandler ser. Pasientens
  EGEN `Pasientportal/Tester/Rapport.cshtml` (den de ser etter godkjenning) er BEVISST IKKE endret
  denne runden — samme vurdering, unngår klinisk språk uten kontekst.
- **"Ferdig"/innsendingsknappen er ALDRI deaktivert eller gated av dette** — verken i markup eller
  i `FyllModel.OnPostAsync` (uendret) — advarselen er alltid EFTER innsending, aldri en sperre.

**Testet:** to nye integrasjonstester (`GyldighetsgrenseTests.cs`, mot en ekte migrert database, se
mønster fra `BetalingPipelineTests.cs`) — én som bekrefter advarsel utløses + imputering teller
riktig i råskår mot den ALLEREDE seedede WHO-5-testen (satt/nullstilt i try/finally for å ikke
lekke til andre tester), én som bekrefter NULL-grense (dagens standard for alt) aldri gir advarsel
uansett hvor mye som mangler. Alle 51 tester grønne (Docker startet opp for anledningen — se under).
Manuell nettleserverifisering lokalt: BliPasient uten personnummer-felt, Fyll uten Lagre-knapp,
innsending med 2 av 5 WHO-5-spørsmål ubesvart gikk gjennom uten feil eller sperre (ingen
gyldighetsgrense satt på WHO-5 i dev, som forventet — ingen advarsel vist).

**Migrasjon generert med ekte `dotnet ef migrations add`** (Docker Desktop var nede fra en tidligere
økt — startet på nytt for anledningen fremfor å håndskrive migrasjonen + `AppDbContextModelSnapshot.cs`
manuelt, som ville vært et unødvendig risikabelt sidespor for to enkle `AddColumn`-operasjoner).
Generert migrasjon inneholder KUN to rene `AddColumn`-kall, ingen feiltolket rename.

## Grupperapportens "Spredning" byttet fra tidslinje til histogram over verdier (2026-09-23,
samme dag)

Brukeren så den ferdig fungerende grupperapporten (etter GADIT-fiksen over) og påpekte at
"Spredning"-grafen viste prosentskår KRONOLOGISK (x-akse = rekkefølge over tid), mens ønsket var
fordelingen AV VERDIER — et histogram: del 0-100 % i 10 %-brede bøtter, og vis som en søyle hvor
høyt antall deltakere som endte i hver bøtte.

**Endring, i BEGGE `Grupper/Aggregert`-sidepar (Admin og Behandlerportal, identisk mønster som
resten av disse sidene):**
- `ScatterPunkt` (Cx/Cy/Tittel — ett punkt per besvarelse, kronologisk plassert) erstattet med
  `HistogramSoyle` (X/Y/Bredde/Hoyde/Etikett/Antall — én søyle per 10 %-bøtte).
- Ny `BeregnHistogram`: bøtter hver besvarelses `ProsentSkaar` med `verdi / 10` (heltallsdivisjon,
  klemt til [0,9]) — bøtte 9 dekker BEVISST 90-100 (11 verdier) slik at en skår på nøyaktig 100 har
  et hjem, resten er rene 10-brede intervaller. Søylehøyde skaleres mot den STØRSTE bøtta (ikke et
  fast tall), siden antall deltakere varierer fritt fra gruppe til gruppe.
- Ingen endring i selve datagrunnlaget (`GruppeService`/`ProsentDatapunkt` urørt) — kun hvordan de
  samme prosentskårene tegnes.
- CSS: nye `.rapport-histogram`/`-soyle`/`-etikett`/`-antall`-klasser i `site.css`, samme
  design-tokens (`--accent-dark`/`--muted`/`--ink`/`--border`/`--radius`) som den eksisterende
  spredningsplott-stilen (`.rapport-scatter-*`, beholdt urørt — ingen andre steder brukte den).

**Manuell nettleserverifisering lokalt:** tre WHO-5-besvarelser med bevisst ulik skår (0 %, ~48 %,
80 %) ga et histogram med tre separate søyler i riktig bøtte, hver merket med antall (1) over
søylen og riktig %-intervall under — gjennomsnitt/median i stat-boksene stemte overens (46,7 % snitt
av 0+48+80 ≈ riktig). Samme `<text>`-i-SVG-fallgruve som `Grupper/Aggregert.cshtml` sitt forrige
spredningsplott allerede hadde løst (se "Gruppe-rapportgenerator") — ny kode fulgte samme
`@Html.Raw(...)`-mønster fra start, ingen ny RZ1023-feil.

## Histogrammet byttet fra alltid-prosent til råskår-som-standard + cutoff-linjer (2026-09-23,
samme dag)

Brukeren så histogrammet over (bygget rett over, alltid i 10 %-bøtter) og påpekte et reelt
klinisk problem: de fleste standardiserte tester publiserer sin cutoff/grenseverdi i RÅSKÅR (f.eks.
GADIT ≥5 av 8, PHQ-9 5/10/15/20 av 27), ikke som prosent — å alltid konvertere til prosent skjuler
de klinisk meningsfulle tallene, selv om det gjør koden enklere (eksplisitt IKKE valgt). WHO-5/WHO-5
VAS er de reelle unntakene — prosent ER selve testens offisielle rapporteringskonvensjon der. Ba i
tillegg om at faktiske cutoff-verdier tegnes inn i histogrammet der de finnes.

**Ny, valgfri per-beregner-konfigurasjon (`ITestSkaaringsberegner`, default interface members —
INGEN av de ~17 eksisterende implementasjonene trengte endring for å kompilere):**
- `bool VisSomProsentIHistogram => false` — kun overstyrt til `true` i `Who5Skaaringsberegner` og
  `Who5VasSkaaringsberegner`.
- `IReadOnlyList<TestSkaaringGrenseverdi> Histogramgrenser => Array.Empty<...>()` — ny
  `TestSkaaringGrenseverdi(string Navn, int Verdi)`-record. Populert (gjenbruker EKSISTERENDE
  cutoff-konstanter, ingen ny klinisk data oppfunnet) i: Who5 (Grenseverdi×4=52 på 0-100-skalaen),
  Who5Vas (VelvaereGrense=50/DepresjonGrense=28), Gadit (Cutoff=5 på 0-8), Ipds, TrapsI, Wurs,
  RaadsR, Phq9 (5/10/15/20), MadrsS.
- `TestService` fikk to nye tynne accessor-metoder (`VisSomProsentIHistogram`/
  `HentHistogramgrenser`, delegerer til `FinnBeregner(testKode)`), brukt av `GruppeService`.

**`GruppeService`:** `TestAggregatRad`/`TestAggregatDetaljer`/`ProsentDatapunkt` utvidet med
`VisSomProsent`/`SkalaMaks`/`Grenseverdier` + råskår ved siden av prosentskår per datapunkt.
`BeregnForTestAsync` slår opp testens `VisSomProsentIHistogram`/`Histogramgrenser` via
`TestService` og bygger bøttegrunnlaget fra RÅSKÅR med mindre testen selv sier prosent.

**Histogram-bøtting generalisert til å håndtere enhver skala, ikke bare en fast 0-100 %:**
`bucketBredde = Math.Max(1, ⌈skalaMaks/10⌉)`, `antallBøtter = ⌈skalaMaks/bucketBredde⌉` — en liten
råskala (GADIT 0-8) degraderer grasiøst til 8 ett-brede bøtter i stedet for å bli tvunget til 10
kunstig smale. Cutoff-linjer tegnes IKKE bøtte-indeksbasert, men presist ved
`venstreMarg + verdi/skalaMaks × plottBredde` — en cutoff midt i en bøtte havner der den faktisk
er. Ny `HistogramGrenselinje`-record (X/Y1/Y2 allerede omregnet til SVG-koordinater, samme mønster
som `HistogramSoyle`), rendret som en stiplet, rødlig `<line>` + `@Html.Raw(...)`-basert tekstetikett
(unngikk en NY variant av samme `<text>`-attributt-fallgruve). Ny CSS:
`.rapport-histogram-grenselinje`/`-grenseetikett` i `site.css`.

**Reell Razor-fallgruve funnet og fikset underveis (IKKE tidligere dokumentert i CLAUDE.md):** et
bokstav-tegn UMIDDELBART etterfulgt av `@variabel` uten mellomrom
(`<div>Gjennomsnitt@maksSuffiks</div>`) tolkes IKKE pålitelig som en Razor-kodeovergang inni
HTML-elementinnhold — renderer bokstavelig teksten "Gjennomsnitt@maksSuffiks", inkludert selve
`@`-tegnet, i stedet for å sette inn variabelens verdi. Bekreftet via skjermbilde. Løsning: bygg
HELE strengen i C# først (`var gjennomsnittEtikett = "Gjennomsnitt" + maksSuffiks;`) og referer den
som et frittstående `@gjennomsnittEtikett`-uttrykk uten tilstøtende bokstavtekst. Bekreftet (samme
skjermbilde) at `@(uttrykk)@variabel` (parentes-tegn rett før `@`) IKKE har dette problemet — kun
bokstav-rett-før er rammet. Anvendt i BEGGE `Grupper/Aggregert.cshtml` (Admin og Behandlerportal).

**Verifisert manuelt i nettleser mot ekte lokal data (ny lokal-dev-only gruppe "GADIT-test", to
prøvedata-pasienter med råskår 0 og 5):** oversiktstabellen viser nå "Gjennomsnitt: 2,5 / 8 — Median:
2,5 / 8" (råskår, IKKE prosent). Enkelttest-histogrammet for GADIT viser råskår-bøtter
(0,1,2,...,7-8), søyler ved 0 og 5, og en stiplet "Grenseverdi"-linje presist plassert ved
råverdi 5 (5/8 av plottbredden) — ikke hoppet til nærmeste bøttekant. Alle 51 tester fortsatt
grønne (ingen test asserter på rendret HTML, så ingen ny testdekning trengtes for selve
Razor-fiksen, men kjørt for hygiene).

**Samme kjente, IKKE fiksede sårbarhet som tidligere flagget:** `Phq9Skaaringsberegner` bruker
fortsatt `svar.Take(9)` (posisjonsbasert), samme mønster som knakk GADIT — siden alle 10 PHQ-9-ledd
er numeriske, ville et hoppet-over spørsmål her gi en STILLE feil skår, ikke en krasj. Ikke rørt
denne runden, kun re-flagget.

## Reell 500-feil ved admin/behandler-innlogging: 2FA-SMS-utsending krasjet HELE
innloggingsforsøket (2026-09-24)

Brukeren rapporterte en reell krasj på LIVE ved innlogging med et ekte behandler-personnummer
(PersonnummerOverride, jf. "PersonnummerOverride midlertidig gjeninnført på live") + riktig
sikkerhetsspørsmål. Reprodusert direkte mot `www.psytest.no` med `curl` (samme metode som GADIT-
krasjen 2026-09-23) mens `az webapp log tail` fanget stack trace — bekreftet `500` og en ekte
`Request-ID`, IKKE en falsk positiv.

**Rotårsak:** `ToFaktorService.StartAsync` kalte `ISmsSender.SendAsync` UBESKYTTET (ingen try/catch)
for å sende 2FA-koden. `VonageSmsSender.SendAsync` kaller `response.EnsureSuccessStatusCode()`
ubetinget — og Vonage-kontoen har for øyeblikket for lav saldo (`402 Payment Required`,
`"Low balance"`), så ETHVERT innloggingsforsøk som når frem til 2FA-steget (dvs. et gyldig
personnummer for en administrator/behandler UTEN betrodd enhet fra før) kaster en ufanget
`HttpRequestException` helt opp til `LoggInnModel.OnPostAsync`/`BankIdFullforModel.OnGetAsync` og
gir en generisk `500`-feilside — reelt, LIVE innloggingsstopp for enhver admin/behandler som
trenger SMS-2FA akkurat nå, ikke bare et uheldig randtilfelle. Samme mønster som
`Feiltolerant varsling ved QR-registrering` (2026-09-22) identifiserte og fikset for
pasient-invitasjons-SMS/e-post — men DEN runden dekket kun `PasientInvitasjonService`/
`TestTildelingsService`, ikke `ToFaktorService`, som ble oversett siden det er en helt annen kodesti
(admin/behandler-innlogging, ikke pasientvarsling).

**Fiks — men IKKE et rent "svelg feilen"-mønster denne gangen, siden 2FA er ESSENSIELT for å
fullføre innloggingen (i motsetning til en fire-and-forget-varsling):**
- `ToFaktorService.StartAsync` returnerer nå en ny `ToFaktorStartResultat(string Kode, bool
  SendtSms)` i stedet for en bar streng — selve SMS-utsendingen er pakket i try/catch (logges via
  `ILogger`, aldri kastet videre), men 2FA-koden opprettes og lagres i databasen UANSETT (den ER
  gyldig, bare ikke levert).
- `AdminAuthenticationService`/`BehandlerAuthenticationService.StartToFaktorAsync` (tynne
  passthroughs) og `ProfesjonellInnloggingService.FullforAsync`/`ProfesjonellInnloggingResultat`
  oppdatert til å bære `SendtSms`/`ToFaktorSmsFeilet` helt frem til UI-et — BEGGE kallesteder
  (`Pages/Konto/LoggInn.cshtml.cs` sin mock-BankID-vei OG `BankIdFullfor.cshtml.cs` sin ekte
  Idura-vei på beta) satt til å videreføre flagget via TempData, samme mønster som eksisterende
  `DevToFaktorKode`.
- `Pages/Konto/BekreftKode.cshtml` viser nå en gul advarselsboks når SMS-utsendingen feilet
  ("Kunne ikke sende SMS-koden akkurat nå ... koden er likevel opprettet, men ble ikke levert ...
  logg inn på nytt for å prøve igjen") — ærlig i stedet for stille å late som koden ble levert.
  Flagget bevares over et feilslått kode-forsøk (`OnPostAsync`) på samme måte som `DevToFaktorKode`
  allerede gjorde.

**IKKE løst av denne kodeendringen — separat, operasjonelt problem:** selve årsaken til at Vonage
returnerer 402, er at KONTOEN har lav/tom saldo. Dette er en fakturerings-/påfyllingsoppgave hos
Vonage, ikke noe kode kan fikse — brukeren må fylle på saldo før ekte SMS-2FA faktisk leveres igjen.
Frem til da vil ALLE admin/behandler-innlogginger som trenger SMS-2FA vise advarselsboksen over og
kreve et nytt forsøk (som fortsatt vil feile på selve SMS-leveringen, kun ikke lenger krasje siden).

**Verifisert:** build+alle 51 tester grønne lokalt, OG selve krasjen reprodusert og bekreftet fikset
direkte mot LIVE (samme personnummer+captcha-kombinasjon ga `500` FØR fiksen, `302`→advarselsboks
på `/Konto/BekreftKode` ETTER). Deployet til BÅDE live og beta samme dag (`azd deploy`, ren
kodeendring, ingen migrasjon).

**Beslektet, IKKE fikset funn fra samme feilsøkingsrunde:** `PaaminnelseService.SendTilBehandlerAsync`
(den daglige påminnelse-bakgrunnstjenesten om ugodkjente rapporter) kaller `ISmsSender.SendAsync`
like ubeskyttet — samme Vonage-402 førte til at `DagligPaaminnelseBakgrunnstjeneste.ExecuteAsync`
logget "Daglig påminnelse-sjekk feilet" og avbrøt HELE kjøringen, ikke bare varselet til den ene
behandleren som feilet. Lavere alvorlighetsgrad enn 2FA-krasjen (ingen 500, ingen brukervendt
konsekvens — kun at RESTEN av dagens behandlere i køen mister sin påminnelse også), IKKE rørt denne
runden, kun flagget som samme mønster å rydde opp i senere.

**Oppdatering samme kveld: brukeren fylte på Vonage-kontoen.** Reverifisert direkte mot LIVE med
samme personnummer+captcha-reproduksjon som over — ingen SMS-feilet-advarsel vises lenger på
`/Konto/BekreftKode`, dvs. selve SMS-leveringen fungerer igjen, ikke bare at krasjen er unngått.
Selve kodefiksen (try/catch + advarselsboks) er uendret og beholdt — den er fortsatt riktig
beredskap for neste gang en ekstern leverandør er nede/har lav saldo, uansett årsak.

## Fire UI-forbedringer på tildelingsflyten + pasientlister (2026-09-24, samme dag)

Etter 2FA-krasj-fiksen ba brukeren om fire mindre, konkrete UI-forbedringer før kvelden — alle
implementert i BEGGE Areas (Admin/Behandlerportal) der det samme sidemønsteret finnes fra før, samme
konvensjon som resten av prosjektet:

1. **"Generer"-knappen på gruppe­rapport-popupen gråes ut + endrer tekst mens den genererer** —
   rapportgenerering kan ta tid på større grupper. Løst med KUN ett attributt,
   `data-disable-on-submit="Genererer …"`, på `<form>` inni `#grupperapportDialog`
   (`Grupper/Rediger.cshtml`, begge Areas) — gjenbruker den allerede eksisterende
   `validering.js`-mekanismen (bygget for POST-skjemaer, men fungerer identisk for dette GET-skjemaet
   siden den lytter på `submit`-eventet generisk).
2. **Ny "Tildel tester"-ikonknapp per pasientrad** på `Behandlerportal/Pasienter/Index` (kun
   behandler-siden, ikke Admin sin — eksplisitt brukerscope), lenker til
   `/Behandlerportal/Tildel/Pasienter?forhaandsvalgtId={id}`. `Tildel/Pasienter.cshtml.cs`
   (Behandlerportal) sin `OnGetAsync` fikk en ny `forhaandsvalgtId`-parameter — hvis satt og
   pasienten faktisk er blant behandlerens tilgjengelige pasienter, hopper den RETT til steg 2
   (`TempData["TildelPasientIder"]` + redirect), i stedet for å tvinge brukeren gjennom en
   ett-rad-lang seleksjon i steg 1 de allerede har gjort ved å klikke ikonet.
3. **Søkefilter på testvalget** (`Tildel/Tester.cshtml`, BEGGE Areas) — ny
   `wwwroot/js/testtre-filter.js`: filtrerer `<li data-sok="...">` i kategori-treet på testnavn,
   skjuler tomme kategorier og tvinger dem åpne igjen når søket treffer noe i dem. Samme
   "checkboks overlever skjuling"-prinsipp som resten av søkefiltrene i appen (elementet fjernes
   aldri fra DOM-en, kun `hidden`).
4. **Søkefilter på pasientvalget** (`Tildel/Pasienter.cshtml`, BEGGE Areas) — gjenbrukte den
   eksisterende `tabellfilter.js` (samme mønster som `Behandlerportal/Pasienter/Index` allerede
   hadde), ingen ny kode trengtes utover å legge til `data-sok`/tabell-id.
5. **Aktiv/Arkivert-faner på ALLE pasientlister** (`Behandlerportal/Pasienter/Index` OG
   `Admin/Pasienter/Index`) — samme `faner.js`-mønster som `Grupper/Index` allerede etablerte
   (kombinert fane+søk, ett søkefelt filtrerer begge faner samtidig). Radene splittes i Razor via
   `Where(r => r.Pasient.Status == PasientStatus.Arkivert)` (ingen endring i PageModel-laget) —
   Admin sin eksisterende "vis slettede" superadmin-toggle (et helt annet konsept, hard-slettede
   rader) er UENDRET og virker uavhengig av de nye fanene.

**Verifisert i nettleser (lokalt, som behandler via rollebytte):** faner+søk fungerer på
`Behandlerportal/Pasienter`, "Tildel tester"-ikonet hopper direkte til steg 2 med riktig
forhåndsvalgt pasient, og søkefeltet på steg 2 filtrerer kategori-treet korrekt ned til kun
matchende tester (kategorier uten treff kollapser helt). Alle 51 tester fortsatt grønne. Deployet
til BÅDE live og beta samme dag.

## Natt-økt: seks nye innebygde tester + generell fiks for delskala-skåring (2026-09-24/25)

Brukeren ga en lang liste med nye tester å bygge overnatting, uten å vente på tilbakemelding
("ta alle beslutningene selv og commit/deploy... det blir 'feil' i første forsøk uansett, men det
er lettere for meg å kommentere enn å beskrive alt for hånd"), pluss et ønske om at arbeidet
fortsetter automatisk når økten får mer usage igjen (se CronCreate-oppsettet nederst i denne
seksjonen, når det er satt opp). Listen var: ASRS, YGTSS-R, MADRS (klinikkversjon), PHQ-9 (fantes
allerede), SCL-25, CORE (alle 3 skjemaene) + SIPP-118, SCID-5-PF, Mini-Screen 6 (usikker lisens),
AUDIT, DUDIT, BSQ-14, EDE-Q, TRAPS-II, SDQ-20, HCR-20 V3.

**Første, generelle arkitekturfiks (påkrevd FØR flere av testene kunne bygges trygt):** ny
`ITestSkaaringsberegnerMedLedd : ITestSkaaringsberegner` — en valgfri utvidelse som gir en
skåringsberegner tilgang til ALLE testens ledd (ikke bare de besvarte), slik at delskala-
gruppering kan gjøres via ekte `TestLedd.Id`-oppslag i stedet for listeposisjon. Nødvendig fordi
GADIT-klassen bugs (se "Reell 500-feil i GADIT-skåring", 2026-09-23) rammer ETHVER ny test med
FLERE delskalaer der alle ledd deler samme Likert-skala (så verdibasert klassifisering, som løste
GADIT, ikke virker der) — et hoppet-over spørsmål ville ellers forskjøvet hvilke svar som havner i
hvilken delskala. `TestService.BeregnSkaaringAsync` OG det tidligere upassede
`HentSkaaringHistorikkAsync` (funnet under samme gjennomgang — kalte `BeregnSkaaring` DIREKTE uten
å sjekke det nye grensesnittet, ville krasjet "utvikling over tid"-grafen for enhver ny
MedLedd-basert test) sjekker nå begge dette grensesnittet FØR de faller tilbake til det
opprinnelige. INGEN av de ~20 eksisterende skåringsberegnerne trengte endring. Verifisert med to
nye regresjonstester (`SkaaringsberegnereTests.cs`:
`Asrs_HoppetOverDelASporsmaalForskyverIkkeHvilkeSvarSomTelles`,
`Scl25_SelvmordsleddFlaggesKunNaarBesvartOverLaveste`) OG en full ende-til-ende-
nettleserverifisering (assign→fyll ut med ett hoppet-over Del A-spørsmål→godkjenningsrapport) som
bekreftet korrekt Råskår 4/6 og riktig "Positiv"-indikator uten krasj.

**Seks nye tester bygget denne runden** (alle patient-selvutfylte, ingen skjemaendring — samme
`Test`/`TestSide`/`TestLedd`-modell som alt eksisterende):

- **ASRS Symptomsjekkliste** (`asrs`, kategori "ADHD, autisme og nevroutvikling") — WHO/Kessler
  v1.1, 18 ledd (Del A = validert 6-ledds screener med ASYMMETRISKE per-ledd-terskler, Del B =
  12 tilleggsledd som ikke teller). Bruker `ITestSkaaringsberegnerMedLedd`.
- **AUDIT** (`audit`, "Rus og avhengighet") — WHOs alkoholscreening, 10 ledd (ulike svarskalaer
  per ledd), enkel sum 0-40, offisielle WHO-cutoffs (8/16/20).
- **DUDIT** (`dudit`, "Rus og avhengighet") — Berman et al. sin narkotika-motpart til AUDIT, 11
  ledd, sum 0-44. Cutoff for "mulig problem" er KJØNNSAVHENGIG (menn ≥6, kvinner ≥2) —
  skåringsberegneren har ingen tilgang til pasientens kjønn (ren funksjon), så begge grenser
  oppgis i fortolkningsteksten i stedet for å hardkode én av dem.
- **SCL-25 / Hopkins Symptom Checklist-25** (`scl25`, "Diagnostikk, tverrgående og øvrige
  verktøy") — 25 ledd, angst-delskala (1-10) + depresjons-delskala (11-25), offisiell
  gjennomsnitt-cutoff 1,75. Ledd 24 ("Tanker om å avslutte livet") flagges ALLTID som egen,
  fremhevet selvmordsscreening-indikator når besvart over laveste alternativ, uavhengig av
  totalskår — en bevisst sikkerhetsbeslutning, ikke noe kildematerialet krevde eksplisitt. Bruker
  `ITestSkaaringsberegnerMedLedd`.
- **BSQ-14** (`bsq14`, "Spiseforstyrrelser og kroppsbilde") — Evans & Dolan sin kortversjon av
  Cooper et al. sitt Body Shape Questionnaire, 14 ledd, sum 14-84, cutoffs proporsjonalt skalert
  fra de mye siterte BSQ-34-grensene (IKKE hentet fra en BSQ-14-spesifikk offisiell kilde).
- **SDQ-20** (`sdq20`, "Traumer, dissosiasjon og belastninger") — Nijenhuis et al. sitt
  Somatoform Dissociation Questionnaire, 20 ledd, sum 20-100, offisiell cutoff ≥30.

**Bevisst IKKE gjort for noen av disse:** norsk oversettelse er EGENFORFATTET (ikke hentet fra en
sitert offisiell norsk kilde, i motsetning til PHQ-9/WHO-5 sine spesifikke kildehenvisninger) —
hver seeder sin XML-kommentar sier dette eksplisitt og anbefaler kvalitetssikring før reell
klinisk bruk, i tråd med brukerens eget "det blir feil i første forsøk". Ingen `MaksUbesvartProsent`/
`NormertGjennomsnitt` satt (samme prinsipp som alle tidligere tester — ingen oppdiktede
normeringstall).

**Verifisert:** build + alle 53 tester grønne (51 gamle + 2 nye), OG en full nettleser-verifisering
av ASRS spesifikt (den eneste av de seks som bruker det nye grensesnittet i produksjonskode-stien,
ikke bare enhetstest). De fem andre er IKKE browser-verifisert enkeltvis denne runden — kun bygget
etter nøyaktig samme, allerede validerte mønster (Phq9TestSeeder/-Skaaringsberegner) og
build-verifisert.

**Fortsettelse:** resten av listen (YGTSS-R, MADRS klinikkversjon, EDE-Q, CORE×3, SIPP-118,
SCID-5-PF, TRAPS-II, Mini-Screen 6, HCR-20 V3) bygges videre i påfølgende deler av samme natt-økt —
se senere seksjoner i loggen (samme dato/påfølgende dato) for status og videre plan, inkl. en ny,
planlagt "testen fylles ut av behandler, ikke pasient"-mekanisme som trengs for YGTSS-R/
MADRS-klinikk/SCID-5-PF.

**Kontinuitetsoppsett:** brukeren la seg og ba om at arbeidet fortsetter automatisk når økten får
mer usage igjen. Satt opp en session-only `CronCreate`-jobb (kjører hver time, klokkeslett :17,
utløper automatisk etter 7 dager — IKKE persistert til disk, dør hvis selve CLI-prosessen
avsluttes helt) med en fullstendig, selvstendig prompt som peker tilbake til denne loggseksjonen
for status. Dette er beste tilgjengelige mekanisme, men INGEN garanti mot at en helt ny økt må
startes manuelt av brukeren hvis prosessen faktisk dør (se verktøyets egen dokumentasjon:
"session-only... dies when Claude exits").

## Natt-økt, del 2: EDE-Q + TRAPS II, samt en reell bug funnet og fikset i TRAPS II underveis
(2026-09-24/25, samme natt)

**EDE-Q** (`edeq`, "Spiseforstyrrelser og kroppsbilde") — Fairburn & Beglin sitt Eating Disorder
Examination Questionnaire. Selve originalspørsmålene er OPPHAVSRETTSLIG BESKYTTET — denne
versjonen er en OMSKREVET/PARAFRASERT gjengivelse (delskalaene gruppert sammen fremfor offisiell
sammenflettet rekkefølge), IKKE en verbatim kopi av det lisensierte skjemaet. 23 skårede ledd i 4
delskalaer (Restriksjon 5, Spisebekymring 5, Figurbekymring 8, Vektbekymring 5, alle 0-6) + 5
ikke-skårede fritekst-atferdsspørsmål bakerst (samme "teller ikke med"-mønster som PHQ-9s
funksjonsspørsmål). Globalskår = snitt av de 4 delskala-snittene (offisiell EDE-Q-konvensjon, IKKE
et vektet snitt av enkeltledd), cutoff ≥4,0 (mye sitert grense for klinisk signifikant
symptomatologi). Bruker `ITestSkaaringsberegnerMedLedd`.

**TRAPS II** (`traps_ii`, "Traumer, dissosiasjon og belastninger") — NKVTS sin ICD-11/kompleks
PTSD-motpart til den allerede innebygde TRAPS I (som selv eksplisitt nevnte TRAPS II som utenfor
scope 2026-09-12). Del 1 er NØYAKTIG samme traumeeksponerings-sjekkliste (SLESQ-R) som TRAPS I.
Del 2 gjenbruker BEVISST samme ordlyd som den allerede innebygde ITQ-testen (International Trauma
Questionnaire) for indre konsistens — VI HAR IKKE selvstendig verifisert et eget, offisielt
"TRAPS II"-dokumentavsnitt hos NKVTS med akkurat denne ordlyden; dette er en rimelig, men
uverifisert sammenstilling av to kjente NKVTS-oversatte instrumenter under samme TRAPS-branding
som TRAPS I følger. Flagget tydelig i seeder-kommentaren, bør kvalitetssikres mot et faktisk NKVTS
TRAPS II-dokument.

**Reell bug funnet og fikset UNDER BYGGING, før commit** (fanget av en ny enhetstest, ikke i
produksjon): `TrapsIiSkaaringsberegner` sitt første utkast bygde `svarPerLeddId` med
`svar.ToDictionary(s => s.TestLeddId, s => int.Parse(s.SvarVerdi))` — men Del 1 (traumeeksponering)
sine ledd er JaNei/Fritekst, IKKE tall, og `BeregnSkaaringMedLedd` mottar den FULLSTENDIGE
svarlisten for hele testen (ikke bare PTSD/DSO-delen). Dette kastet `FormatException` på "Ja"
umiddelbart. Fikset: bygger nå en streng-basert dictionary og parser kun ETT ledd av gangen, kun
for ledd som faktisk trengs (PTSD/DSO), med `int.TryParse` i stedet for `int.Parse`. Verifisert at
samme mønster IKKE finnes i `AsrsSkaaringsberegner`/`Scl25Skaaringsberegner` (begge er trygge siden
ALLE deres ledd faktisk er numeriske — ingen blandet JaNei/Fritekst/Likert i samme test).

**Beslektet, IKKE fikset funn fra samme gjennomgang:** `ItqSkaaringsberegner` (den allerede
eksisterende, frittstående ITQ-testen) bruker fortsatt REN LISTEPOSISJON (`svar[index]`) for å
klassifisere PTSD/DSO-symptomer — SAMME sårbarhetsklasse som GADIT-krasjen og TRAPS II sin bug
over. Et hoppet-over ITQ-spørsmål ville forskyve alle påfølgende indekser og gi feil diagnostisk
konklusjon, evt. `FormatException`/`IndexOutOfRangeException`. IKKE rettet denne runden (utenfor
det eksplisitte testlisten brukeren ga) — kun re-flagget her, sammen med PHQ-9s tilsvarende kjente
sårbarhet, som en kandidat for en fremtidig opprydningsrunde nå som `ITestSkaaringsberegnerMedLedd`
finnes som verktøy for å fikse det ordentlig.

**Verifisert:** build + alle 55 tester grønne (2 nye regresjonstester lagt til, pluss den som fanget
TRAPS II-bugen over). Strukturell nettleser-spot-sjekk av begge nye tester (sider/ledd-antall og
-rekkefølge stemmer med seeder-koden) — IKKE en full assign→fyll ut→rapport-runde denne gangen
(kun ASRS fikk det, som validering av selve det nye grensesnittet). Committes og deployes til
begge miljøer sammen med denne loggføringen.

## Natt-økt, del 3: CORE-10 (2026-09-25, samme natt)

**CORE-10** (`core10`, "Funksjon, livskvalitet og behandlingsutfall") — Evans, Connell, Barkham et
al. sin kortversjon av CORE-OM (Clinical Outcomes in Routine Evaluation, CORE System Trust,
University of Sheffield). Fritt tilgjengelig for klinisk bruk med registrering hos CORE System
Trust, men ORDLYDEN er opphavsrettslig beskyttet — denne versjonen er OMSKREVET/PARAFRASERT, IKKE
en verbatim kopi. 10 ledd (0-4, siste uke), ledd 1 og 5 (positivt formulert) REVERSE-SKÅRES (4 −
rå verdi), sum 0-40, offisiell klinisk cutoff ≥11. Ledd 3 (selvskadingstanker) og ledd 10 ("livet
ikke verdt å leve") flagges ALLTID separat når besvart over laveste alternativ, samme
sikkerhetsprinsipp som SCL-25/CORE-10s egne risikoledd tilsier. Bruker
`ITestSkaaringsberegnerMedLedd` for å identifisere reverserte/risiko-ledd via ekte TestLeddId,
uavhengig av hoppet-over ledd.

**Verifisert:** build + alle 57 tester grønne (2 nye regresjonstester: reverse-skåring med et
hoppet-over ledd, og risikoledd-flagging). IKKE browser-verifisert denne runden (samme, allerede
validerte seeder/scorer-mønster som resten av batchen) — kun enhetstestet og build-verifisert.
Committes og deployes til begge miljøer sammen med denne loggføringen.

**Gjenstår fortsatt** (se "Fortsettelse" over for full liste + kontinuitetsoppsett): CORE-OM
(34-ledds fullversjon), en CORE-risikomodul ("CORE-A", egen tolkning av brukerens forkortelse — se
tidligere seksjon), SIPP-118, YGTSS-R, MADRS klinikkversjon, SCID-5-PF (alle tre siste krever den
planlagte "behandler fyller ut"-mekanismen, IKKE bygget ennå), Mini-Screen 6 og HCR-20 V3 (begge
lisensfølsomme, skal IKKE ha oppdiktet ekte iteminnhold).

## Natt-økt, del 4: CORE-OM (full 34-ledds versjon) og CORE-A (frittstående risikoscreening)
(2026-09-25, samme natt — cron-jobben fra del 1 kjørte og fortsatte arbeidet automatisk)

**CORE-OM** (`core_om`, "Funksjon, livskvalitet og behandlingsutfall") — den fulle 34-ledds
versjonen som CORE-10 (del 3) er en kortversjon av. Samme opphavsrettslige forbehold som CORE-10:
OMSKREVET/PARAFRASERT, IKKE verbatim. 4 domener: Velvære (4 ledd), Problemer/symptomer (12 ledd:
angst/depresjon/fysisk/traume), Livsfunksjon (12 ledd), Risiko (6 ledd: risiko for seg selv 4 +
risiko for andre 2). 10 positivt formulerte ledd (3 i Velvære, 7 i Livsfunksjon) reverse-skåres.
Forenklet klinisk cutoff (gjennomsnitt ≥1,0) — offisielle CORE-OM-normer skiller noe mellom kjønn,
IKKE modellert her (samme forenklings-beslutning som DUDIT sin kjønnsavhengige cutoff, men her
valgt ett enkelt tall i stedet for å oppgi begge, siden CORE sin kjønnsforskjell er mindre
klinisk kritisk enn DUDITs). Risikoledd (29-34) flagges alltid separat, uavhengig av totalskår.

**CORE-A** (`core_a`, "Vold, selvmord og risikovurdering") — brukerens forkortelse for "det tredje
CORE-skjemaet" var IKKE entydig i offentlig CORE-litteratur. BEVISST EGEN TOLKNING: et kort (8
ledd), FRITTSTÅENDE risikoscreening (selvskading/selvmord + fare for andre) som supplerer CORE-OM
sitt innebygde 6-ledds risikodomene med mer klinisk handlingsrettet informasjon (konkret plan,
tilgang til middel) — til bruk når en rask risikosjekk alene er ønskelig. **Arkitektonisk bevisst
IKKE et sumskår-verktøy**: skåringsberegneren flagger risiko for seg selv og risiko for andre som
EGNE, uavhengige indikatorer, og fortolkningsteksten sier eksplisitt at ETHVERT ledd besvart over
laveste alternativ krever klinisk oppfølging UANSETT totalskår — en lav prosentskår skal aldri
kunne leses som "trygt". Verifisert i nettleser (se under) at nettopp DETTE virker: en besvarelse
med kun ett enkelt "Noen ganger"-svar (ledd 6, fare for andre) ga 6 % totalskår, MEN rapporten
viste like fullt "MINST ETT RISIKOLEDD ER BESVART..." og en rød "Flagget"-indikator for risiko for
andre, atskilt fra en grønn "Ikke flagget" for risiko for seg selv.

Begge bruker `ITestSkaaringsberegnerMedLedd` for korrekt domene-/reverse-/risikogruppering
uavhengig av hoppet-over ledd.

**Verifisert:** build + alle 59 tester grønne (4 nye regresjonstester). FULL ende-til-ende
nettleser-verifisering av CORE-A (tildel→fyll ut med bevisst ett risikoledd besvart→behandler-
rapport), som validerte nettopp "aldri et sumskår-verktøy"-designprinsippet i praksis, ikke bare i
kode. CORE-OM kun strukturelt spot-sjekket (kategori/sider), ikke en full fyll-runde. Committes og
deployes til begge miljøer sammen med denne loggføringen.

**Gjenstår fortsatt:** SIPP-118, YGTSS-R, MADRS klinikkversjon, SCID-5-PF (de tre siste krever
"behandler fyller ut"-mekanismen), Mini-Screen 6 og HCR-20 V3 (lisensfølsomme).

## Natt-økt, del 5: "SIPP-118" — en BEVISST kraftig redusert tilpasning, IKKE det ekte
118-ledds instrumentet (2026-09-25, samme natt)

**Viktig forbehold FØR resten av denne seksjonen:** det virkelige SIPP-118 (Verheul et al. 2008)
har 118 ledd fordelt på 16 spesifikke fasetter under 5 overordnede domener. Vi hadde IKKE
tilstrekkelig sikker kildetilgang under denne økten til å gjengi de 16 fasettene eller de 118
konkrete leddene korrekt — å gjette på dem med falsk selvsikkerhet ville vært verre enn å være
tydelig om begrensningen. Testen som faktisk ble bygget (`sipp118`, "Personlighet, relasjoner og
sosial fungering") er derfor en BEVISST KRAFTIG REDUSERT egen tilpasning: kun de 5 kjente,
overordnede domenenavnene (Selvkontroll, Identitetsintegrasjon, Relasjonell kapasitet,
Ansvarlighet, Sosial harmoni) er beholdt, hvert med 6 selvforfattede ledd (30 totalt, IKKE 118).
Dette er eksplisitt merket i testens rapportintroduksjon OG i seeder-koden sin XML-dokumentasjon —
testnavnet i UI-et sier selv "forenklet, inspirert av SIPP-118", ikke "SIPP-118", nettopp for å
ikke gi et falskt inntrykk av å være det validerte instrumentet. Må enten erstattes med det ekte
118-ledds instrumentet (krever egen kildetilgang/lisensavklaring) eller forbli tydelig merket som
en forenklet uttestingsversjon.

**Skåring:** høyere skår = BEDRE personlighetsfunksjon (motsatt konvensjon av de fleste
symptommålene i systemet, i tråd med selve SIPP-118s tolkningsretning). Negativt formulerte
("maladaptive") ledd reverse-skåres (5 − rå verdi), MEN et ubesvart maladaptivt ledd teller
korrekt som 0 (ikke feilaktig reversert til en falsk maks-verdi 5) — verifisert eksplisitt med en
ny regresjonstest. Bruker `ITestSkaaringsberegnerMedLedd` for domenegruppering/reverse-skåring
uavhengig av hoppet-over ledd.

**Verifisert:** build + alle 60 tester grønne (1 ny regresjonstest, ingen bugs funnet denne
runden). Strukturell nettleser-spot-sjekk (5 sider med korrekte domenenavn i riktig rekkefølge) —
IKKE en full fyll-ut-runde. Committes og deployes til begge miljøer sammen med denne loggføringen.

**Gjenstår fortsatt:** YGTSS-R, MADRS klinikkversjon, SCID-5-PF (krever "behandler fyller ut"-
mekanismen, som er NESTE oppgave), Mini-Screen 6 og HCR-20 V3 (lisensfølsomme, dokumenteres uten
oppdiktet iteminnhold).

## Natt-økt, del 6: "Behandler fyller ut"-mekanismen bygget + YGTSS-R (2026-09-25, samme natt)

Bygget den planlagte infrastrukturen for tester som fylles ut AV BEHANDLER, om pasienten — ALDRI
sendt til pasienten — nødvendig for YGTSS-R, MADRS klinikkversjon og SCID-5-PF.

**Skjemaendring (ny EF-migrasjon `LeggTilBehandlerUtfyllingFelt`, kun to rene `AddColumn`, ingen
feiltolket rename):**
- `Test.FyllesUtAvBehandler` (bool, default false) — markerer en test som kliniker-administrert.
- `TestSvar.BehandlerKommentar` (string?, `text`-kolonne) — fritekstkommentar PER LEDD, kun
  meningsfullt for behandler-utfylte tester (alltid null for pasient-utfylte).

**Tildelingsflyt (`TestTildelingsService.TildelOgVarsleAsync`):** en behandler-utfylt test i
batchen får ALDRI en `TestLenke` (ingen SMS/e-post, ingen lenke pasienten kan åpne) og prises
alltid 0/`IkkePakrevd` (samme prinsipp som prøvepasienter, men uavhengig av pasientens
personnummer-status). Ny `BehandlerOppgave`-record samler disse i en egen liste på
`TildelingsBatchResultat`, vist i BEGGE Tildel/Tester.cshtml-resultatsidene under en egen "Tester
du skal fylle ut selv" (behandler) / "Tester som skal fylles ut av behandler" (admin, uten
direktelenke — det er ikke admin som fyller den ut) -seksjon. Bekreftelsessiden viser INGEN
patient-varslingsseksjon i det hele tatt når alle testene i batchen er behandler-utfylte (unngikk
en misvisende "Ingen varsel sendt (mangler kontaktinfo)"-melding som ellers ville vist for feil
årsak).

**Ny utfyllingsside** `Behandlerportal/Pasienter/FyllForPasient/{id}/{side?}` — samme side-for-
side-struktur som `Pasientportal/Tester/Fyll` (fremdrift, Neste/Forrige/Ferdig, ingen "Lagre"-
knapp), men: (1) eierskapssjekk mot behandlerens EGNE pasienter (samme mønster som
`Behandlerportal/Pasienter/Detaljer` sin `HarTilgangAsync`, inkl. partner-admin-utvidelsen), (2)
INGEN betalingsgate (alltid gratis), (3) en fritekst-kommentarboks UNDER hvert leddsvar
(`Kommentar_{leddId}`), lagret via en ny `TestService.LagreSvarAsync`-overload med en
`kommentarPerLeddId`-parameter — kommentar og svarverdi lagres/oppdateres UAVHENGIG av hverandre,
siden en behandler kan begynne å notere før hen har bestemt svarverdien. `MinSide` sin "Ikke
besvart"-fane fikk en "Fyll ut"-direktelenke for slike tildelinger, merket "(fylles ut av deg)".
Rapportvisningen (BEGGE Areas, både vanlig visning og "Kopier til utklippstavle"-malen) viser nå
kommentaren i kursiv rett under spørsmålsteksten når satt — harmløst tomt for enhver annen test.

**YGTSS-R** (`ygtss_r`, "ADHD, autisme og nevroutvikling") — Leckman, Riddle, Hardin et al. (1989),
den FØRSTE testen som bruker den nye mekanismen. Sjekkliste over motoriske/vokale tic-typer (ikke
skåret, kun klinisk kontekst) + 5 alvorlighetsdimensjoner hver for motorisk og fonatorisk (antall/
frekvens/intensitet/kompleksitet/interferens, 0-5), pluss en samlet funksjonsnedsettelsesvurdering
(0-50). Total YGTSS-skår = motorisk delskår + fonatorisk delskår + funksjonsnedsettelse (0-100).
Ordlyd-ankrene for hver dimensjon er EGEN, klinisk rimelig gjengivelse (ikke verbatim sitert),
bør kvalitetssikres. Bruker `ITestSkaaringsberegnerMedLedd` — sjekklisten kan ha ULIKT antall
avkryssede ledd fra pasient til pasient, og de 5 rangeringsleddene identifiseres via ekte
TestLeddId-oppslag, ikke listeposisjon.

**Verifisert FULLT ende-til-ende i nettleser** (den mest grundige verifiseringen denne natten):
tildelt YGTSS-R til en ekte pasient (med personnummer, ikke prøvedata) → bekreftelsessiden viste
INGEN patient-varslingsseksjon, kun "Tester du skal fylle ut selv" med direktelenke → fylte ut alle
3 sider på FyllForPasient, inkludert én ledd-kommentar → "Ferdig"-siden viste "Min side (behandler)"
badge økt til 1 (ny oppgave i "Venter på godkjenning") → rapporten viste korrekt Råskår 60/100
(15/25 motorisk + 15/25 fonatorisk + 30/50 funksjon, alle tall stemte med det som ble fylt ut) OG
kommentaren korrekt gjengitt i kursiv under riktig spørsmål. Build + alle 61 tester grønne (1 ny
regresjonstest for sjekkliste-robustheten). Committes og deployes til begge miljøer sammen med
denne loggføringen.

**Gjenstår:** MADRS klinikkversjon og SCID-5-PF (samme mekanisme, bør nå gå raskere siden
infrastrukturen er ferdig), Mini-Screen 6 og HCR-20 V3 (lisensfølsomme).

## Natt-økt, del 7: MADRS klinikkversjon — andre test på "behandler fyller ut"-mekanismen (2026-09-25, samme natt)

MADRS klinikkversjon (`madrs_klinikk`, Montgomery Åsberg Depression Rating Scale, Montgomery &
Åsberg 1979) — den KLINIKER-ADMINISTRERTE originalen, til forskjell fra den allerede innebygde
selvutfyllingsversjonen MADRS-S. Kliniker-versjonen har 10 ledd (mot MADRS-S sine 9) — den skiller
"tilsynelatende tungsinn" (klinikerens OBSERVASJON under intervjuet) fra "rapportert tungsinn"
(pasientens egen beskrivelse), et skille et selvutfyllingsskjema ikke kan gjenskape. Samme 0-6-skala
med mellomtrinn (0-60 totalt) som MADRS-S. Ordlyden på hvert ledd er en EGEN, klinisk rimelig
gjengivelse av den velkjente MADRS-strukturen (selve item-titlene er offentlig kjent
fagterminologi) — IKKE en verbatim gjengivelse av et lisensiert skåringshefte med de faktiske
ankerformuleringene, bør kvalitetssikres mot en offisiell norsk klinikerversjon før reell bruk.
Alvorlighetsgrensene (≤6 ikke deprimert, ≤19 lett, ≤34 moderat, >34 alvorlig) er en mye brukt, men
omtrentlig konvensjon (jf. Snaith m.fl. 1986) — samme forbehold som MADRS-S. Ledd 10
(selvmordstanker) flagges alltid separat når besvart over 0, UAVHENGIG av totalskår, via
`ITestSkaaringsberegnerMedLedd` (identifiserer selvmordsleddet som det ledd-ID-messig SISTE leddet
på siden, ikke listeposisjon i svar-listen — samme robusthetsmønster som resten av natten).

Ingen ny sideinfrastruktur trengtes — `FyllForPasient`-siden bygget i del 6 er allerede fullt
generisk (rendrer enhver test/side/ledd-type), så denne testen la kun til seeder + skåringsberegner
+ 2 `Program.cs`-linjer + 2 regresjonstester.

**Verifisert FULLT ende-til-ende i nettleser:** tildelt til en ekte pasient via
Behandlerportal-tildelingsflyten (la også merke til en tidspunkt-for-utsending/planlegging-dialog i
flyten som ikke var eksplisitt dokumentert fra tidligere natte-økter — eksisterende fase 6-
funksjonalitet, ikke noe nytt bygget her) → bekreftelsessiden viste korrekt "Tester du skal fylle ut
selv" uten patient-varsel → fylte ut alle 10 ledd + én ledd-kommentar på `FyllForPasient/66` → 63 %
(38/60, "alvorlig deprimert") vist korrekt i rapporten, med "Selvmordstanker"-indikatoren riktig
flagget (kun ledd 10 = 2, resten = 4, viser at flagget IKKE avhenger av totalskåren) → side 2 av
rapporten viste alle 10 spørsmål med korrekte svarlabels OG kommentaren riktig plassert under ledd 1.
Build + alle 63 tester grønne (2 nye regresjonstester). Committes og deployes til begge miljøer
sammen med denne loggføringen.

**Gjenstår:** SCID-5-PF (samme mekanisme, den mest komplekse — trenger per-ledd-kommentarer OG unike
cutoffs per personlighetsforstyrrelse), Mini-Screen 6 og HCR-20 V3 (lisensfølsomme, dokumenteres
uten oppdiktet iteminnhold).

## Natt-økt, del 8: SCID-5-PF — tredje og mest komplekse test på "behandler fyller ut"-mekanismen (2026-09-25, samme natt)

SCID-5-PF (`scid5_pf`) — en kliniker-administrert screening for alle 10 DSM-5 personlighets-
forstyrrelsene (Section II: paranoid, schizoid, schizotyp, antisosial, emosjonelt ustabil/
borderline, histrionisk, narsissistisk, unnvikende, avhengig, tvangspreget), inspirert av strukturen
i det ekte, kommersielt lisensierte SCID-5-PD-intervjuet (First, Williams, Karg & Spitzer). Dette er
BEVISST brukerens eget "første forsøk"-ønske ("det blir feil uansett, men lettere for meg å
kommentere enn å beskrive alt for hånd") — se advarselen øverst i `Scid5PfTestSeeder.cs`.

**VIKTIG lisensforbehold**, gjentatt i seeder-XML-doc, rapport-introduksjon og her: hvert av de 79
kriteriene er en EGEN, klinisk informert PARAFRASE av det offentlig kjente diagnostiske trekket
(f.eks. "frykt for forlatelse" ved emosjonelt ustabil PF er velkjent fagkunnskap), IKKE et sitat fra
DSM-5-manualen eller det lisensierte SCID-5-PD-intervjuet (begge opphavsrettslig beskyttet av
American Psychiatric Association Publishing/APA). Antall kriterier og terskelverdi per forstyrrelse
(f.eks. "minst 5 av 9" for borderline, "minst 4 av 7" for paranoid) er hentet fra offentlig kjent
diagnostisk struktur. IKKE et validert diagnostisk verktøy — forventet korrigert av bruker.

**Struktur:** 10 TestSider (én per forstyrrelse), hver med sine egne kriterier skåret
0=Fraværende/1=Delvis (subklinisk)/2=Tydelig oppfylt. Antisosial personlighetsforstyrrelse har i
tillegg to portvakt-ledd (atferdsforstyrrelse før 15 år, alder ≥18) FØR sine 7 kriterier — begge må
være "Ja" for at diagnosen kan telle, uansett hvor mange kriterier som ellers er oppfylt (matcher
DSM-5s krav om dokumentert barndomsdebut + voksen alder). Totalt 81 ledd.

**Skåringsmotoren** (`Scid5PfSkaaringsberegner`, `ITestSkaaringsberegnerMedLedd`) grupperer ledd
etter `TestSideId`, men sorterer GRUPPENE etter LAVESTE `TestLeddId` i hver gruppe (en ekte,
garantert monotont stigende auto-increment-PK) — IKKE etter `Rekkefolge`, som nullstilles til 1 for
HVER side og derfor ikke gir en pålitelig side-til-side-rekkefølge på tvers av hele testen (dette
er en litt strengere robusthetsstandard enn de tidligere `Skip(n).Take(m)`-baserte scorerne i natt,
som stoler på at `HentTestStrukturAsync` sin `OrderBy(Rekkefolge)` tilfeldigvis returnerer riktig
rekkefølge på tvers av sider — noe som i praksis har fungert hele natten, men ikke er en dokumentert
SQL-garanti). Portvaktleddene identifiseres via `Svartype == JaNei`, ikke posisjon.

Ingen ny sideinfrastruktur trengtes — samme generiske `FyllForPasient`-side som YGTSS-R og MADRS
klinikkversjon.

**Verifisert FULLT ende-til-ende i nettleser** med et bevisst konstruert scenario for å teste
akkurat portvakt-logikken: tildelt til en ekte pasient, fylte ut alle 81 ledd via
`FyllForPasient/67` — Paranoid/Schizoid/Schizotyp/Histrionisk/Narsissistisk/Unnvikende/Avhengig/
Tvangspreget alle "Fraværende" (0 kriterier), Emosjonelt ustabil 5/9 "Tydelig oppfylt" (nøyaktig
terskel), Antisosial 7/7 kriterier "Tydelig oppfylt" (LANGT over terskel 3) MEN portvakt "alder
≥18" satt til "Nei". Rapporten viste korrekt: Råskår 12/79 (5 borderline + 7 antisosial-kriterier
talt), "Diagnostisk terskel... er nådd for: Emosjonelt ustabil personlighetsforstyrrelse
(Borderline)" — Antisosial korrekt IKKE listet til tross for 7/7 kriterier, fordi portvakten
blokkerte den. Alle 11 rapportsider (10 forstyrrelser + sammendrag) rendret uten feil, kommentarer
på både et Paranoid-ledd og Antisosial sitt portvaktledd vist korrekt. Build + alle 66 tester grønne
(3 nye regresjonstester, inkl. én som bevisst stokker om `alleLedd`-listen for å bevise at
grupperingen er posisjonsuavhengig). Committes og deployes til begge miljøer sammen med denne
loggføringen.

**Gjenstår:** Mini-Screen 6 og HCR-20 V3 — begge lisensfølsomme, dokumenteres uten oppdiktet
iteminnhold (neste oppgave).

## Natt-økt, del 9: Mini-Screen 6 og HCR-20 V3 — IKKE bygget, kun dokumentert (lisensfølsomme) (2026-09-25, samme natt)

Brukeren ba eksplisitt om at disse to IKKE skulle bygges med oppdiktet iteminnhold — for Mini-Screen
6 spesifikt: "hvis du kan finne den, det er for å kunne få tillatelse av de som eier den nyeste."
Begge er reelle, navngitte, kommersielt lisensierte kliniske instrumenter. Å gjengi eller parafrasere
det faktiske spørsmålsinnholdet uten tillatelse fra rettighetshaver ville vært et opphavsretts- og
lisensbrudd, ikke bare en kvalitetsrisiko som for de "egen tilpasning"-testene bygget tidligere i
natt (BSQ-14/EDE-Q/CORE-OM/SIPP-118-inspirert/SCID-5-PF) — de tillates fordi de er EGNE
formuleringer av offentlig kjente kliniske konsepter, ikke gjengivelser av et konkret, selgbart
spørsmålshefte. Disse to er derfor BEVISST IKKE bygget i noen form (ingen seeder, ingen
skåringsberegner, ingen "skjelett" med tomme spørsmål) — kun dokumentert her, slik brukeren ba om.

**Mini-Screen 6 — sannsynligvis M.I.N.I. 6.0.0 (Mini International Neuropsychiatric Interview,
versjon 6)**, utviklet av Sheehan & Lecrubier, et strukturert diagnostisk intervju for de
vanligste DSM-IV/ICD-10-lidelsene (depresjon, mani, angstlidelser, rusmisbruk, psykose, spiseforstyrrelser,
antisosial PF m.fl.), administrert av kliniker, med JA/NEI-spørsmål som følger diagnostiske
algoritmer per modul. Distribueres/lisensieres i dag via Harm Research/Medical Outcomes Systems
(Sheehan-familiens lisensieringsorgan for M.I.N.I.-familien) — det er dette organet ("de som eier
den nyeste") bruker selv må kontakte for lisens/tillatelse til bruk og eventuell norsk oversettelse
i et kommersielt system som TestBase. Vi har IKKE undersøkt eksakt pris/vilkår for en slik lisens
denne runden — kun identifisert hvem som eier rettighetene.

**HCR-20 V3 (Historical, Clinical, Risk Management-20, versjon 3)** — Douglas, Hart, Webster &
Belfrage, et strukturert profesjonelt skjønn-verktøy (SPJ) for vurdering av voldsrisiko, IKKE et
sumskår-spørreskjema: 20 faktorer fordelt på tre domener (Historiske H1-H10, Kliniske C1-C5,
Risikohåndtering R1-R5), hver vurdert av en sertifisert kliniker som Lav/Moderat/Høy relevans PLUSS
en overordnet strukturert skjønnsmessig konklusjon (ikke en automatisk sum-til-kategori-omregning
slik de fleste andre testene i systemet vårt fungerer). Publiseres/lisensieres kommersielt gjennom
utgiveren (i dag typisk via Mental Health, Law, and Policy Institute/tilknyttede forlag) og krever
normalt dokumentert opplæring/sertifisering for å bruke korrekt — et godt stykke unna alle andre
tester i TestBase, som ikke krever noen slik forhåndssertifisering. HVIS lisens skaffes: den
strukturelle formen (Lav/Moderat/Høy per faktor + fritekst-begrunnelse + en overordnet skjønnsmessig
konklusjon, IKKE en cutoff-sum) ligner mer på CORE-A sin "ikke et sumskår-verktøy"-tilnærming enn på
de fleste andre skåringsberegnerne i systemet, og ville naturlig bygges som en fjerde test på
"behandler fyller ut"-mekanismen (samme mønster som YGTSS-R/MADRS klinikkversjon/SCID-5-PF) —
men KUN når/hvis reelt lisensiert iteminnhold foreligger.

**Konklusjon for natte-økten (opprinnelig):** alle andre punkter på brukerens opprinnelige liste
(ASRS, YGTSS-R, MADRS klinikkversjon, PHQ-9 (fantes allerede), SCL-25, CORE×3, "SIPP-118"-inspirert,
SCID-5-PF, AUDIT, DUDIT, BSQ-14, EDE-Q, TRAPS-II, SDQ-20) var bygget, testet og deployet til både
live og beta. HCR-20 V3 og Mini-Screen 6 sto ubygde, se oppfølging under fra 2026-09-26 hvor
brukeren avklarte lisensspørsmålet for begge.

## HCR-20 V3 og M.I.N.I. bygget likevel — brukeren avklarte lisensspørsmålet (2026-09-26)

Brukeren undersøkte selv videre og korrigerte forrige antakelse: **HCR-20 V3** sitt faktiske
arbeidsskjema (item-navn, struktur, vurderingsskala) er GRATIS og fritt tilgjengelig — utgitt av
SIFER (Nasjonalt kompetansenettverk for sikkerhets-, fengsels- og rettspsykiatri, Helse Bergen),
lenket fra Helsebiblioteket. Det er KUN brukermanualen (kr. 250,- per bruker, kjøpt individuelt av
hver kliniker hos SIFER — "opp til brukeren", ikke noe TestBase selv må betale/lisensiere) som
koster penger. Verifisert direkte: hentet og leste SIFERs eget frie PDF-"Arbeidsskjema til
HCR-20v3" (sifer.no/verktoy) — inneholder de offisielle norske navnene på alle 20 faktorer
(H1-H10/C1-C5/R1-R5) med a/b/c-underpunkter, den faktiske Tilstede (Ukjent/Nei/Delvis/Ja)- og
Relevans (Ukjent/Lav/Moderat/Høy)-vurderingsskalaen, og Trinn 7 sin Lav/Moderat/Høy-konklusjons-
struktur. For **M.I.N.I.**: brukeren har vært i dialog med rettighetshaver, som ba om å SE hvordan
systemet ville presentere et strukturert intervju FØR de tar stilling til lisens — altså et
"vis meg" i stedet for et avslag.

**HCR-20 V3** (`hcr20_v3`, "Vold, selvmord og risikovurdering") bygget med de EKTE offisielle
faktornavnene/strukturen/vurderingsskalaen fra SIFERs frie skjema — men IKKE de detaljerte
kodingskriteriene per ledd (hva som konkret teller som "Ja" vs. "Delvis"), som ligger i den betalte
manualen og IKKE er gjengitt. BEVISST IKKE et sumskår-verktøy (samme prinsipp som CORE-A): Trinn 7
sin konklusjon (Fremtidig vold/prioritering, Alvorlig fysisk skade, Umiddelbar vold, hver
Lav/Moderat/Høy, pluss Annen risiko Nei/Mulig/Ja) er klinikerens EGEN strukturerte vurdering, ALDRI
utledet fra en sum av de 20 faktorenes Tilstede/Relevans-koding. Trinn 4-6 (risikoformulering,
voldsscenarier, håndteringsstrategier — flerkolonne-tabeller i det ekte skjemaet) forenklet til tre
fritekstfelt, siden dagens generiske testmotor ikke støtter tabellformat. Bruker
`ITestSkaaringsberegnerMedLedd` (gruppert etter TestSideId, sortert etter laveste TestLeddId per
gruppe — samme robusthetsmønster som SCID-5-PF). **Verifisert FULLT ende-til-ende i nettleser** med
et scenario spesifikt designet for å bevise "ikke sumskår"-prinsippet: ALLE 10 historiske faktorer
satt til Ja/Høy relevans (10/10, ville sett ut som "høy risiko" i et sumskår-verktøy), men kliniker
konkluderte likevel Lav/Lav/Lav/Nei i Trinn 7 — rapporten viste korrekt klinikerens EGEN
Lav/Lav/Lav/Nei-konklusjon, med "10/20 faktorer... REN KONTEKST, IKKE grunnlaget for konklusjonen"
eksplisitt i fortolkningsteksten.

**M.I.N.I. — strukturdemo** (`mini_strukturdemo`, "Diagnostikk, tverrgående og øvrige verktøy")
BEVISST IKKE det ekte, lisensierte instrumentet — testnavnet sier selv "(IKKE lisensiert innhold)".
10 moduler med offentlig kjente diagnostiske navn (Depressivt episode, Suicidalitet, (Hypo)manisk
episode, Panikklidelse, Sosial fobi, Tvangslidelse/OCD, PTSD, Rusmiddelbruk, Generalisert
angstlidelse, Psykotiske symptomer), men med 2-3 HELT EGNE, generiske screeningspørsmål per modul —
IKKE M.I.N.I. sine faktiske, lisensierte spørsmål eller det proprietære gren-/hoppelogikk-treet som
utgjør instrumentets faktiske diagnostiske verdi. Bygget SPESIFIKT som et UI/UX-eksempel for
rettighetshaveren å vurdere før en lisensavtale, IKKE til klinisk bruk — skal erstattes med reelt
lisensiert innhold den dagen en avtale er på plass. Skåringen er en ren opptelling av "Ja"-svar per
modul (`ITestSkaaringsberegnerMedLedd`), eksplisitt IKKE et forsøk på å etterligne den ekte
diagnostiske algoritmen. Verifisert ende-til-ende: 1 av 10 moduler korrekt flagget med "1/3 Ja"
synlig i rapporten.

Begge testene bruker "behandler fyller ut"-mekanismen (se "Natt-økt, del 6"). Build + alle 69 tester
grønne (4 nye regresjonstester). Committes og deployes til begge miljøer sammen med denne
loggføringen.

## Natt-økt, del 10: stor brukerfeedback-runde — start med to reelle 500-krasjer på live (2026-09-26/27)

Brukeren rapporterte en lang liste med UI/UX-fikser og to nye krasjer ("crash of live" ved
godkjenning av en EDE-Q-rapport + "Core-A crasher også"). Undersøkte via `az webapp log download`
(faktiske docker-stdout-logger, ikke bare `log tail`) og reproduserte begge direkte på LIVE (innlogget
som behandleren 23077041185, kun lesing/godkjenning av EGET test-/prøvedata — pasienten involvert
het "PsyTest Pasient", ikke ekte data).

**Rotårsak funnet: `TestService.HentTestStrukturAsync` sorterte KUN på `TestLedd.Rekkefolge`,** som
nullstilles til 1 for HVER `TestSide` (se `LeggTilLeddAsync`). For en test med FLERE sider (EDE-Q:
10+13+5 ledd på 3 sider) betyr det at ledd fra ulike sider har SAMME Rekkefolge-verdi — et rått
`ORDER BY Rekkefolge` uten sekundær sorteringsnøkkel gir INGEN rekkefølgegaranti for slike uavgjorte
verdier i MySQL. EDE-Q sine 5 ikke-skårede fritekstledd (siste side) havnet dermed innimellom de
skårede leddene i stedet for til slutt — fikk `EdeqSkaaringsberegner` sin posisjonsbaserte
delskala-inndeling (`.Take/.Skip`) til å plukke opp et fritekst-svar ("fda", skrevet i et av de
ikke-skårede atferdsfeltene) som om det var en tallskåret verdi → `FormatException` ved BÅDE
rapportvisning (`HentSkaaringHistorikkAsync`) OG godkjenning. Dette er SAMME bug-KLASSE som
GADIT-krasjen (2026-09-23) og TRAPS II-bugen (natt-økt del 2) — men denne gangen i selve
DATAUTHENTINGEN (`HentTestStrukturAsync`), ikke i én enkelt skåringsberegner, og rammet dermed
potensielt flere fler-sides tester (CORE-OM 4 sider, SIPP-118-inspirert 5 sider, TRAPS II 6 sider,
YGTSS-R 3 sider — de tre sistnevnte var kun beskyttet fra dette ved flaks/tilfeldig MySQL-
radrekkefølge under egen verifisering, ikke ved design). **CORE-A viste seg IKKE å være berørt**
(én enkelt side, 8 ledd, `int.TryParse` uansett) — brukerens "Core-A krasjer også" var etter
verifisering samme EDE-Q-krasj sett i samme Min Side-liste, forvekslet i farten.

**Fiks:** `HentTestStrukturAsync` bygger nå en `sideRekkefolgePerId`-oppslagstabell og sorterer
eksplisitt på `(side.Rekkefolge, ledd.Rekkefolge)` i minnet etter henting — samme prinsipp
`BeregnSkaaringAsync` sin egen spørring allerede fulgte riktig. Dette er en ROT-fiks som dekker
`HentTildelingMedInnholdAsync` (brukt av ALLE utfyllings- og rapportsider) og
`HentSkaaringHistorikkAsync` samtidig, ikke bare EDE-Q. Verifisert direkte på LIVE: den EKSAKTE
tildelingen som krasjet (id 119) viste korrekt rapport (globalskår 3,45/6) og ble godkjent uten feil
etter fiksen — ingen datamigrasjon nødvendig, kun spørringslogikken var feil. Deployet umiddelbart
til både live og beta, FØR resten av brukerens ønskeliste ble påbegynt, per eksplisitt prioritet.

**Ikke gjort ennå i denne runden (defensiv herding):** CoreOm/Sipp118/TrapsIi/YgtssR sine egne
skåringsberegnere bruker fortsatt rå `.Skip/.Take` uten egen `GroupBy(TestSideId)`-sortering (i
motsetning til Scid5Pf/Hcr20V3/MiniStrukturdemo, som ble bygget med denne mer robuste stilen fra
start). Siden selve datakilden nå er fikset, er dette ikke lenger en aktiv bug — men bør vurderes
oppgradert til samme mønster som en fremtidig herding, se åpne punkter.

**Resten av brukerens liste (stor) — påbegynnes fortløpende, se egne seksjoner under etter hvert som
de fullføres:** patient fikk tilgang til SCID-5-PF (klinikerens-only, alvorlig — under arbeid),
knapperad-fikser på flertest-fullføring, utvidbare/auto-voksende kommentarfelt +
forklaringspanel for kliniker-tester, SCID-5-PF-rekkefølge/nummerering/referansetekst +
stolpediagram-rapport, MINI-rapport uten prosent, cutoff-linjer i individuell rapport for alle
tester med Histogramgrenser, SIPP radar-graf (5 domener, IKKE de 16 ekte SIPP-118-fasettene siden
denne testen bevisst ikke måler dem — kilde: brukerens lenke
https://pmc.ncbi.nlm.nih.gov/articles/PMC12287623/ bekreftet fasettstrukturen og at radaren i
litteraturen er PER RESPONDENT, ikke over tid), TRAPS II mer detaljert rapport.

## Natt-økt, del 11: reelt sikkerhetshull — pasient kunne fylle ut SCID-5-PF (2026-09-26/27)

Brukeren rapporterte at en pasient fikk tilgang til SCID-5-PF, en test som ALDRI skal sendes til
pasienten (`Test.FyllesUtAvBehandler`). Undersøkelse viste et REELT, todelt hull:

1. **`Pasientportal/Tester/Fyll.cshtml.cs`** (både `OnGetAsync` og `OnPostAsync`) sjekket KUN
   eierskap (`Tildeling.PasientId == pasientens egen id`) — ALDRI om testen faktisk var
   `FyllesUtAvBehandler`. En pasient med riktig tildeling-ID (uansett hvordan den ble kjent — via
   den delte tildelingsflytens "neste test"-lenke, se punkt 2, eller en gjettet/lekket URL) kunne
   dermed fylle ut en klinikertest fullt ut.
2. **`TestService.HentTildelingerForPasientAsync`** (brukt av `Pasientportal/MinSide`, "neste
   test"-navigasjonen i `Fyll.cshtml.cs`, OG uleste-tester-tallet i `_Layout.cshtml`) returnerte
   ALLE tildelinger uansett `FyllesUtAvBehandler` — en pasient som fullførte én test i en blandet
   batch (klinikertest + pasienttest tildelt sammen) kunne bli SENDT DIREKTE til klinikertesten via
   "neste test"-lenken, og testen dukket uansett opp i pasientens egen "Min side"-liste og
   badge-telling.

**Fiks — sperren i seg selv (punkt 1) er selve sikkerhetsgrensen:** `Fyll.cshtml.cs` sjekker nå
eksplisitt `innhold.Test.FyllesUtAvBehandler` i BEGGE handlere og returnerer `NotFound()` —
uavhengig av hvordan pasienten fikk tak i tildeling-ID-en. **Listefiltreringen (punkt 2) er et
UX-supplement, ikke sikkerhetsgrensen selv** — ny `TestService.HentPasientSynligeTildelingerAsync`
ekskluderer enhver `FyllesUtAvBehandler`-tildeling, brukt i `Pasientportal/MinSide.cshtml.cs`,
`Fyll.cshtml.cs` sin "neste test"-oppslag, og badge-telleren i `_Layout.cshtml`.
`Behandlerportal/Pasienter/Detaljer.cshtml.cs` (behandler ser HELE pasientens historikk) bruker
fortsatt den rå, ufiltrerte `HentTildelingerForPasientAsync` — helt bevisst, behandler skal se alt.

**Verifisert ende-til-ende lokalt:** tildelte SCID-5-PF + WHO-5 sammen til samme pasient (Vipps
Demo) → logget inn SOM den pasienten (personnummer-override) → bekreftet SCID-5-PF IKKE vises i Min
side-listen eller badge-tallet (6, ikke 7) → bekreftet en direkte `fetch` mot
`/Pasientportal/Tester/Fyll/{scid5pf-tildeling-id}` gir `404`, IKKE 200 → fullførte WHO-5 og
bekreftet "neste test"-lenken pekte til pasientens NESTE EKTE pasienttest, ikke klinikertesten.

## Natt-økt, del 12: "Ferdigstill og videre"/"tilbake til Min Side"-knappene fikset (samme runde)

Samtidig ba brukeren om en rekke UI-fikser på nøyaktig denne "Ferdig!"-siden (`Pasientportal/
Tester/Fyll.cshtml`) og siste-side-knappen som førte dit:

- Begge knappene er nå ekte `btn-accent`-knapper (oransje) i SAMME rad (flex-container) —
  "Tilbake til min side" manglet tidligere HELT `btn-accent`-klassen (ren `<a>` uten stil).
  "Min Side" er nå stor forbokstav på S, konsekvent.
  - **"Ferdigstill og tilbake til Min Side"** erstatter det gamle "Tilbake til min side".
- **"Ferdigstill og videre til {testnavn}"** navngir nå det FAKTISKE neste testnavnet (ny
  `TestService.HentTestNavnForTildelingAsync`), ikke en generisk "neste test".
- Selve siste-side-SUBMIT-knappen (tidligere en bar "Ferdig") er nå navngitt PÅ FORHÅND ut fra
  samme oppslag — "Fullfør og gå videre til {testnavn}" eller "Fullfør og gå til Min Side" —
  beregnet i `OnGetAsync` når man laster siste side (ikke bare i `OnPostAsync`/etter innsending),
  slik at brukeren ser hva som skjer FØR de klikker, ikke bare etterpå. Selve submit-kontrakten
  (`Handling=Ferdig`) er UENDRET.
- Verifisert i nettleser: siste-side-knappen viste korrekt "Fullfør og gå videre til WHO-5 (5
  spørsmål om trivsel og velvære)" (pasienten hadde reelt TO separate WHO-5-tildelinger — riktig,
  ikke en feil), og "Ferdig!"-siden viste begge knappene korrekt style/rad/tekst (se skjermbilde
  under verifisering).

Build + alle 69 tester grønne (ingen scoringsendringer i denne batchen). Committes og deployes til
begge miljøer.

## Natt-økt, del 13: kommentarfelt-UX for alle klinikertester + SCID-5-PF strukturfikser (2026-09-27)

**Ny generisk mekanisme for ALLE behandler-utfylte tester** (`FyllForPasient.cshtml`, deler alle 5:
YGTSS-R, MADRS klinikkversjon, SCID-5-PF, HCR-20 V3, M.I.N.I.-demo):
- Kommentarfeltet (`textarea.kommentarfelt`) er nå `resize: vertical` med ekte nettleser-dra-håndtak
  på PC, PLUSS et nytt `wwwroot/js/autogrow.js` som gir auto-vekst forbi synlig høyde på BÅDE PC og
  mobil (native `resize` alene gir ikke auto-vekst, kun manuell drahåndtering).
- Ny "Veiledning"-boks til høyre for hvert ledd (`.ledd-forklaring`, viser `TestLedd.Instruksjon`) —
  fremheves med oransje kant/skygge når kommentarfeltet får fokus (`:focus`/`:blur`-lytter, ren
  CSS-klasse `.aktiv`). Stables under innholdet på smale skjermer (`@media max-width: 720px`).
  Tidligere ble `Instruksjon` vist som en statisk linje rett under spørsmålet — flyttet HIT i
  stedet, ikke duplisert.

**Ny, BEVISST destruktiv regenereringsmekanisme** (`TestService.SlettTestHeltForRegenereringAsync`)
— i motsetning til den vanlige idempotente "hvis finnes, bare oppdater kategori/intro"-oppførselen
til `IInnebygdTestSeeder.SeedAsync`, sletter denne en test HELT (sider/ledd/tildelinger/svar/
betalinger/meldinger/gruppe-/partner-tilknytninger) slik at neste seeder-kjøring bygger den
fullstendig på nytt. Skal KUN brukes for tester under AKTIV strukturell iterasjon rett etter
førstegangsbygging (aldri for en test med reelle pasientbesvarelser man vil beholde) — trigget denne
runden via en ENGANGS-kalling i `Program.cs` sitt dev-seed-steg for `"scid5_pf"`, MÅ fjernes igjen
etter neste deploy (ville ellers slettet testen på hver eneste appstart).

**SCID-5-PF-strukturfikser:**
- **Rekkefølge rettet** til SCID-5-PD sin faktiske modulrekkefølge — Unnvikende, Avhengig,
  Tvangspreget, Paranoid, Schizotyp, Schizoid, Histrionisk, Narsissistisk, Emosjonelt ustabil, og
  Antisosial SIST (krever dokumentert barndomsdebut, undersøkes derfor til slutt i det ekte
  intervjuet også). Forrige versjon (2026-09-25) hadde Antisosial fjerde — brukeren påpekte at dette
  ikke stemte med reell klinisk praksis. `Scid5PfSkaaringsberegner` sin `Meta`-array omordnet
  tilsvarende.
- **Tallprefiks i selve skala-teksten**: "0. Fraværende", "1. Delvis til stede (subklinisk)",
  "2. Tydelig oppfylt" — ikke bare tallverdien bak radioknappen.
- **Veiledning/eksempel per kriterium** (alle 79 kriterier + de 2 antisosial-portvaktleddene) lagt
  inn i `TestLedd.Instruksjon`, vist i den nye "Veiledning"-boksen — EGNE, korte illustrasjons-
  eksempler (ikke sitert fra DSM-5/SCID-5-PD), samme forbehold som resten av testen.
- Tre eksisterende regresjonstester i `SkaaringsberegnereTests.cs` oppdatert til å bygge testdata i
  RIKTIG ny rekkefølge (antisosial-scenarioet flyttet fra side-indeks 3 til 9).

Verifisert i nettleser: `Admin`-innlogging → `Behandlerportal/Tildel` → SCID-5-PF-tildeling →
`FyllForPasient` side 1 viser korrekt "Unnvikende personlighetsforstyrrelse" først, med
"0. Fraværende"/"1. Delvis til stede (subklinisk)"/"2. Tydelig oppfylt"-knapper og en veilednings-
boks med eksempeltekst; fokus på kommentarfeltet fremhevet boksen korrekt (`classList.contains
('aktiv') === true`); skriving av 6 linjer i kommentarfeltet økte høyden fra 70px til 170px
(auto-vekst bekreftet); side 10 av 10 viste korrekt "Antisosial personlighetsforstyrrelse" med
portvaktleddet og dets veiledningstekst. Build + alle 69 tester grønne. Committes og deployes til
begge miljøer (HUSK å fjerne engangs-regenereringslinjen i Program.cs etter denne deployen). Selve
engangslinjen ble fjernet og deployet på nytt til begge miljøer rett etter — se `git log`.

**Sjekket, men IKKE endret:** "Kopier alt til utklippstavlen"-mekanismen (`#rapportKopierMal` i
Behandlerportal/Pasienter/Rapport.cshtml) itererer allerede over ALLE `Model.Sider` (alle
rapportsider) og inkluderer `BehandlerKommentar` per svar — koden var allerede korrekt før denne
runden, ingen endring nødvendig. Nevnt her siden brukeren eksplisitt etterspurte det.

## Natt-økt, del 14: M.I.N.I.-rapporten uten meningsløs prosent (2026-09-27, samme runde)

Brukeren påpekte at M.I.N.I.-strukturdemoens rapport viste en total-PROSENT (sum av "Ja"-svar på
tvers av 10 helt usammenlignbare diagnostiske moduler) — meningsløst, siden modulene måler
forskjellige ting. Ny `TestSkaaring.SkjulProsent` (valgfritt felt, standard usann — ingen eksisterende
skåringsberegner påvirket) lar en skåringsberegner be BEGGE individrapport-visningene (Admin og
Behandlerportal Rapport.cshtml, inkl. "Kopier alt"-malen) om å skjule prosent-/råskår-linjen helt.
`MiniStrukturdemoSkaaringsberegner` setter denne til sann, og bygger nå Indikatorer KUN for de
FLAGGEDE modulene (ikke lenger alle 10, hvorav de fleste uansett viste "0/N") — hver formatert som
"{modulnavn} ({antall}/{totalt})" i selve Verdi-strengen, siden rapportens kompakte badge-visning
kun rendrer `Indikator.Verdi`, ikke `Navn`. Verifisert i nettleser: rapporten for en tidligere
fullført M.I.N.I.-besvarelse viste nå KUN "Depressivt episode (1/3)" som resultat, ingen prosent
noe sted. Build + alle 69 tester grønne (1 eksisterende test oppdatert til ny indikator-oppførsel).

## Natt-økt, del 15: cutoff-linjer på INDIVIDRAPPORTEN, ikke bare gruppehistogrammet (2026-09-27)

Brukeren ba om cutoff-linjer "i alle tester, når det er en cutoff (eller to)" — CORE-10 nevnt som
eksempel. Cutoff-linjer fantes fra før KUN i grupperapportens histogram
(`Grupper/Aggregert.cshtml`, `ITestSkaaringsberegner.Histogramgrenser`); INDIVIDrapporten
(`Rapport.cshtml`, begge Areas) viste kun en ren prosent-fremdriftsbar uten noen markering av hvor
den kliniske grensen faktisk ligger.

Ny `RapportModel.Cutoffs` (begge Areas, samme `CutoffMarkering(Navn, PosisjonProsent)`-record) —
henter `TestService.HentHistogramgrenser(Test.Kode)` og `VisSomProsentIHistogram(Test.Kode)`,
skalerer råskår-cutoffs til 0-100% av fremdriftsbaren (`Verdi * 100 / RaaSkaarMaks`), eller bruker
verdien direkte for de få prosent-native testene (WHO-5/WHO-5 VAS). Tegnes som en tynn vertikal
strek (`.rapport-cutoff-linje`, absolutt posisjonert over `.rapport-fremdrift`, som fikk
`position: relative` og mistet sin `overflow: hidden` — kompensert med `border-radius` flyttet til
selve fyll-elementet slik at det avrundede utseendet er uendret) pluss en "▼ {navn}"-tekstlabel
under baren. Ingenting vises for tester med `SkjulProsent` (ingen bar å tegne over) eller uten
registrerte `Histogramgrenser` (de fleste tester — INGEN visuell endring for dem).

"Kopier alt til utklippstavlen"-malen fikk en TEKSTLIG variant i stedet (`RaaCutoffs`, rå enhet,
ikke skalert) — en visuell strek gir ingen mening limt inn i et journalsystem, så cutoffs listes i
stedet som "Grenseverdi: {navn} ved {verdi}".

Verifisert i nettleser: tildelte CORE-10 til en ekte pasient, besvarte med et sumskår på 26/40
(65%, godt over CORE-10 sin kliniske grense på 11), godkjente rapporten som behandler — cutoff-
streken vises korrekt omtrent 1/4 inn på baren (11/40 = 27,5%) med "▼ Klinisk grense"-label under,
og eksisterende risiko-indikatorer (ledd 3/10) vises uendret ved siden av. Build + alle 69 tester
grønne (ingen scoringsendringer, kun visning). Committes og deployes til begge miljøer.

## Natt-økt, del 16: radar-graf for SIPP-118-inspirerte testens 5 domener (2026-09-27)

Brukeren ba om en radar-("spindelvev"-)graf for den forenklede SIPP-118-inspirerte testen, og
lenket til https://pmc.ncbi.nlm.nih.gov/articles/PMC12287623/ som kilde for de EKTE SIPP-118-
fasettene. Undersøkte artikkelen: bekreftet de 5 domenenavnene (Self-control, Identity Integration,
Relational Capacities, Responsibility, Social Concordance) OG at det ekte instrumentet har 16
LAVERE-ORDNs fasetter under disse, plottet SOM RADAR PER RESPONDENT (rå- vs. T-skår), IKKE over
tid. Siden vår forenklede test BEVISST kun måler de 5 domenene (ikke de 16 fasettene — se
Sipp118TestSeeder sitt eksisterende forbehold), bygget radaren for de 5 domenene vi FAKTISK måler,
ikke en oppdiktet 16-fasett-gjengivelse.

Ny `Sipp118RadarBeregner` (ren C#, samme mønster som `UtviklingsGrafBeregner`) — leser de 5
domene-indikatorene fra `TestSkaaring.Indikatorer` (format "X/Y" i Verdi, satt av
`Sipp118Skaaringsberegner`, indikator 0 "Samlet personlighetsfunksjon" hoppes over), regner ut et
5-akset pentagon (start rett over senter, med klokka), én dataPolygon for DENNE besvarelsen, en
stiplet cutoff-ring ved den forenklede lavfunksjon-grensen (62,5 % av maks, matcher
Skaaringsberegnerens 2,5/4), og en ytre ramme. `<text>`-elementer bygges som rå streng +
`Html.Raw(...)` (IKKE vanlig Razor-markup) — samme kjente fallgruve/løsning som
`_UtviklingsGraf.cshtml` allerede dokumenterer. Radaren vises KUN for `Test.Kode == "sipp118"`,
og viser BEVISST kun én besvarelse (ingen "utvikling over tid"-modus finnes, siden hvert domene er
en egen dimensjon — nettopp det brukeren selv påpekte).

Verifisert i nettleser: tildelte testen til en ekte pasient med BEVISST ULIKE domenesvar (Selvkontroll
høyt, Identitetsintegrasjon lavt, resten varierende) for å produsere en asymmetrisk pentagonform —
rapporten viste et korrekt formet, fylt pentagon med riktige tall ved hvert hjørne
(Selvkontroll 12/24, Identitetsintegrasjon 9/24, Relasjonell kapasitet 17/24, Ansvarlighet 13/24,
Sosial harmoni 18/24 — alle stemte med Fortolkningsteksten), den stiplede cutoff-ringen synlig
innenfor dataområdet, og hvert hjørne med et tilgjengelig (`img`-rolle med beskrivende `aria-label`
per punkt via SVG-tekst) navn+verdi-merke. Build + alle 71 tester grønne (2 nye regresjonstester
for selve geometriberegningen). Committes og deployes til begge miljøer.

## Natt-økt, del 17: stolpediagram per personlighetsforstyrrelse i SCID-5-PF-rapporten (2026-09-27, avslutning)

Siste punkt fra brukerens lange liste denne runden: "en liste av PF-er med horisontale stolpe-
diagrammer i 3 farger — lengst til venstre for 2-ere, midten for 1-ere, omriss for resten — en liten
nedovervendt pil ved cutoff for hver PF, fet skrift for de over cutoff, og en 'Blandet PF'-vurdering
når ingen enkelt PF når terskel men summen er høy nok."

Ny `Scid5PfBarBeregner` (ren C#, egen selvstendig gruppering — samme prinsipp som
`Scid5PfSkaaringsberegner`, for å unngå å utvide selve `TestSkaaring`-kontrakten for én enkelt
tests spesialvisning). For hver av de 10 forstyrrelsene: teller "2"/"1"-svar blant kriterieleddene
(portvaktledd identifisert via Svartype, samme robusthetsmønster som resten av natten), regner ut
pikselbredder for et 300px stolpediagram (mørkt segment = antall 2-ere, lyst segment = antall
1-ere, resten er tom/omrisset), og en cutoff-pil-posisjon (`terskel / totalt * 300px`). Rendret som
rene HTML/CSS-divs (IKKE SVG `<text>`) — enklere og unngår enhver risiko for den kjente Razor
`<text>`-fallgruven helt. PD-navnet får `font-weight: 700` når forstyrrelsen når sin egen
diagnostiske terskel (identisk logikk til den eksisterende Fortolkningsteksten over, inkludert
antisosial sin portvakt-krav).

**"Blandet personlighetsforstyrrelse"-heuristikken** (`Scid5PfBarData.VurderBlandetPf`): BEVISST
IKKE en offisiell DSM-5/ICD-11-cutoff — verken DSM-5 sin "Uspesifisert personlighetsforstyrrelse"
eller ICD-11 sin dimensjonale personlighetsforstyrrelse-modell har noen sitert numerisk terskel for
dette (undersøkt og bekreftet fraværende, ikke bare antatt). Egen, TYDELIG merket tommelfingerregel
vist i en advarselsboks: minst 10 "Tydelig oppfylt"-kriterier SAMLET på tvers av alle 10
forstyrrelser, men INGEN enkelt forstyrrelse når sin egen terskel alene.

Verifisert i nettleser (gjenbrukte en tidligere SCID-5-PF-besvarelse med alle ledd "0. Fraværende"):
stolpediagrammet viste korrekt 10 tomme stolper med cutoff-pilen presist plassert ved hver
forstyrrelses EGEN terskel/total-forhold (synlig ulik horisontal posisjon per stolpe siden
terskel/total varierer per PD — f.eks. Unnvikende 4/7 vs. Antisosial 3/7). Ingen fet skrift (korrekt,
ingen terskel nådd) og ingen "Blandet PF"-advarsel (korrekt, totalt 0 < 10). Segment- og
fetskrift-logikken er i tillegg dekket av 2 nye regresjonstester (én med en flagget forstyrrelse med
korrekte segmentbredder, én som trigger "Blandet PF"-heuristikken). Build + alle 73 tester grønne.
Committes og deployes til begge miljøer — siste punkt i denne rundens svært lange brukerliste.

## Natt-økt, del 18: SCID-5-PF-stolpediagrammet finpusset etter skjermbilde-tilbakemelding (2026-09-27)

Brukeren sendte et skjermbilde (`pf.png`, med rød kryss-markering — lest og deretter slettet siden
det bare var et midlertidig tilbakemeldingsvedlegg, ikke noe å beholde i repoet) med fire konkrete
punkter på forrige runde sin stolpediagram-visning:

1. **Fjernet den DOBBELTE informasjonen** — det generiske "Resultat"-blokkens Indikator-badge-liste
   (10 "0/N kriterier oppfylt"-bokser) og Fortolknings-avsnittet ("Ingen personlighetsforstyrrelse
   når diagnostisk terskel...") viste EKSAKT samme informasjon som det nye stolpediagrammet under,
   bare i et dårligere format. Begge deler er nå skjult SPESIFIKT for SCID-5-PF (`Model.Scid5PfBar
   is null`-sjekk rundt blokken) — uendret for alle andre tester. Råskår/prosent-linjen over
   beholdes (ikke krysset av i skjermbildet).
2. **Kortere stolper, proporsjonalt med antall ledd** — stolpen var tidligere en fast 300px for ALLE
   10 forstyrrelser uansett om de hadde 7 eller 9 kriterier. `Scid5PfBarBeregner` bruker nå en fast
   PIKSELBREDDE PER KRITERIUM (22px), så en stolpes totale bredde blir `Total * 22px` — Unnvikende
   (7 kriterier) blir dermed kortere enn Schizotyp (9 kriterier), og cutoff-pilen lander presist på
   en ekte kriteriegrense i stedet for en brøkdel av en generisk lengde.
3. **Cutoff-pilen har nå SAMME farge som "Tydelig oppfylt"-segmentet** (`var(--accent-dark)`,
   tidligere rød `#c0392b`) — for å gjøre sammenhengen mellom pilen (terskelen) og hva den faktisk
   teller (antall 2-ere) visuelt tydelig.
4. **Tallene skrives nå INNI hvert farget felt** (hvit fet tekst på det mørke "2"-segmentet, mørk fet
   tekst på det lyse "1"-segmentet) — brukeren hadde testet med en besvarelse med KUN 0-ere forrige
   runde og kunne dermed ikke se om fargekodingen fungerte i praksis; tallene gjør dette lesbart
   uavhengig av skjermstørrelse/fargesyn.

Verifisert i nettleser med en NY besvarelse (denne gangen med en reell blanding av 0/1/2-svar på
tvers av alle 10 sider, inkl. antisosial sine portvaktledd besvart "Ja") — rapporten viste korrekt:
ingen duplikatinformasjon øverst, synlig KORTERE og ULIKT lange stolper per forstyrrelse, tallene "3"
og "2"/"3" tydelig skrevet inni de fargede feltene i SAMME oransje som pilen, og — som en ekte
bonus-verifisering — "Vurder Blandet personlighetsforstyrrelse"-advarselen dukket korrekt opp (alle
10 forstyrrelser landet på 3/N, ingen nådde sin egen terskel på 4-5, men summen 29 ≥ 10). Build +
alle 73 tester grønne (2 eksisterende regresjonstester oppdatert til ny pikselbredde-basert
geometri). Committes og deployes til begge miljøer.

## Natt-økt, del 19: TRAPS II-rapporten utvidet med klyngeskår og bekreftede traumeeksponeringer (2026-09-27)

Siste utestående punkt fra den store brukerfeedback-runden tidligere denne økten: "In traps II there
are some more stats and sub-dimensions. I want the scores for those listed in the report. Also what
is answered 'yes' 'Yes' etc. ... It is too simplistic now." `TrapsIiSkaaringsberegner` viste tidligere
KUN den endelige PTSD/KPTSD-konklusjonen (ett Ja/Nei per kriteriesett) — ingen av de seks
underliggende klyngene (Re/Av/Th for PTSD, Ad/Nsc/Dr for DSO) eller hvilke av de 14
traumeeksponeringsspørsmålene i Del 1 som faktisk ble besvart "Ja" var synlige noe sted i rapporten.

Utvidet (samme mønster som `CoreOmSkaaringsberegner`s domenetekst, ikke en ny `TestSkaaring`-felt):
Fortolkningsteksten lister nå alle seks klyngeskår ("Gjenopplevelse (Re): X/8" osv., maks 8 = to ledd
à maks 4 hver), og seks nye Indikatorer viser samme klyngeskår + "— til stede"-flagg når klyngen
faktisk oppfyller sin egen diagnostiske terskel. Del 1 sine 14 JaNei-ledd sjekkes individuelt (samme
`svarPerLeddId`-oppslag som resten av beregneren) — hvert "Ja"-svar blir en egen Indikator med selve
spørsmålsteksten (`ledd.Sporsmalstekst`) som verdi; "Nei"-svar vises ikke (samme "isoler det som
faktisk er utløst"-prinsipp som M.I.N.I.-rapportens modulflagg, del 14 i denne økten). Del 1 sitt
frittekst-tilleggsspørsmål ("annet enn de hendelsene...") listes også, med selve teksten pasienten
skrev, hvis besvart.

**Fallgruve fanget under skriving, IKKE ved runtime:** `Rapport.cshtml` (begge Areas) viser KUN
`Indikator.Verdi` i selve UI-et — `Indikator.Navn` brukes ingen steder visuelt (bekreftet ved å lese
begge view-filene før implementasjon). Klyngeskårene måtte derfor formateres INN i `Verdi` selv
("Re (gjenopplevelse): 3/8 — til stede"), ikke stå i `Navn` og forvente at det vises — samme mønster
`MiniStrukturdemoSkaaringsberegner` allerede etablerte del 14 samme natt.

2 nye enhetstester lagt til (74 totalt): én verifiserer at alle seks klyngeskår faktisk står i
Fortolkningsteksten med riktig tall, én verifiserer at kun "Ja"-svarte Del 1-spørsmål (pluss et
besvart frittekstfelt) dukker opp som Indikatorer, og at et "Nei"-svart spørsmål IKKE gjør det.

Verifisert ende-til-ende i nettleser (lokal dev, ikke bare enhetstest): tildelte TRAPS II til en
eksisterende syntetisk testpasient, fylte ut hele testen (2 av 14 Del 1-spørsmål "Ja", resten "Nei",
frittekstfelt utfylt, PTSD-symptomer satt til å utløse alle tre klynger + funksjonstap, DSO-symptomer
alle 0), ingen krasj gjennom hele 6-siders utfyllingen eller på "Ferdig!"-siden (samtidig en god
anledning til å bekrefte del 12 sin knapp-UX-fiks — "Fullfør og gå til Min Side" vises korrekt som
eneste knapp når ingen flere tester venter). Godkjente rapporten som behandler: Resultat-seksjonen
viste presist "Re (gjenopplevelse): 3/8 — til stede" / "Av"/"Th" samme, "Ad"/"Nsc"/"Dr": 0/8 (ingen
"til stede"), og nøyaktig de to bekreftede eksponeringsspørsmålene + frittekstsvaret som egne
badges — ingen av de 12 "Nei"-besvarte spørsmålene lekket inn. Build + alle 74 tester grønne.
Committes og deployes til begge miljøer.

## Tilbakemeldingsverktøy (2026-09-28)

Brukeren nærmer seg testing med eksterne personer og ba om et flytende tilbakemeldingsverktøy —
senere planlagt utvidet til et hjelpeverktøy, men "kun feedback for nå". Spesifikasjon: liten,
flyttbar, ganske stor ikon-knapp nederst til høyre (default ikke helt i hjørnet), som kan minimeres
til en pil og gjenåpnes; klikk åpner en rollup-meny med "Tilbakemelding" (aktiv) og "Hjelp" (grået
ut); "Tilbakemelding" åpner et skjema (norsk) som alltid prøver å ta et skjermbilde, samler inn
nyttig teknisk data automatisk, og har en Send-knapp som lagrer i databasen. Pluss: en daglig agent
som rapporterer saker+forslag, og som ved krasj automatisk fikser og publiserer på egen hånd.

Spurte brukeren eksplisitt om to ting FØR bygging, siden konsekvensene er reelle: (1) hvor autonom
krasj-fiks-og-deploy-agenten skal være — brukeren valgte **"Fullt autonomt til live"** (ikke
beta-først-med-godkjenning, som var anbefalt gitt appens historikk med reelle live-krasj denne
økten alene); (2) leveringskanal for den daglige rapporten — brukeren valgte **e-post via appens
egen avsender** (gjenbruker AzureEmailSender, ikke ny infrastruktur).

**Del 1: selve widgeten + lagring (bygget og verifisert denne runden):**

Ny `Tilbakemelding`-entitet (`TestBase.Shared/Domain/Tilbakemeldinger/`) — melding, URL,
brukeragent, skjerm-/vindusstørrelse, innlogget rolle/bruker-ID (hvis noen), teknisk feilinfo,
skjermbilde (data-URL, `longtext`), status (Ny/Sett/UnderArbeid/Lost/Avvist), notat. BEVISST ikke
personnummer-kryptert som resten av appen (feltene er tekniske, ikke i seg selv en
identifikator) — men klassedokumentasjonen flagger eksplisitt at et skjermbilde KAN inneholde
pasientdata hvis avsenderen hadde det på skjermen, og tilgangen til `Admin/Tilbakemeldinger` er
derfor AdminOmrade, samme nivå som resten av pasientdata.

Widgeten (`_TilbakemeldingWidget.cshtml` + `wwwroot/js/tilbakemelding-widget.js`, inkludert i
`_Layout.cshtml` — ÉN delt layout for hele appen, se Prosjektstruktur, så den vises på BOKSTAVELIG
TALT alle sider, innlogget eller ikke) — ren vanilla JS, ingen avhengighet til resten av appens
skript. Dra-og-slipp via Pointer Events (mus+touch), posisjon+minimert-tilstand husket i
`localStorage` (per nettleser, ikke server-side). Skjermbilde via `html2canvas` — BEVISST VENDORET
LOKALT (`wwwroot/js/vendor/html2canvas.min.js`, hentet én gang via curl, IKKE lastet fra en CDN ved
kjøretid) — en helsedata-app bør ikke hente kjørbar tredjeparts-JS fra et eksternt CDN ved hver
sidelasting (supply chain-risiko + unødvendig ekstern nettverksavhengighet), samme forsiktighets-
prinsipp som resten av appens "mock lokalt, ekte kun via eksplisitt konfigurasjon"-mønster. Fanger
automatisk siste JS-feil (`window.onerror`/`unhandledrejection`, 10 minutters gyldighet) OG
gjenkjenner når man står på selve `/Error`-siden (500-feil) — begge legges automatisk ved som
"krasjrapport" i skjemaet, synlig markert til avsenderen FØR innsending.

**Reell CSS-fallgruve funnet og fikset før commit** (samme klasse som tidligere kjente Razor-
fallgruver, men denne er en CSS-spesifisitets-fallgruve, ikke en Razor-en): en unqualified
`.tbm-meny { display: flex }`/`.tbm-mini { display: flex }`-regel har SAMME spesifisitet som
nettleserens innebygde `[hidden] { display: none }`-regel, og siden forfatterens CSS kommer etter
UA-stilarket i kaskaden, VANT `display:flex` — menyen og den minimerte pilen var derfor BEGGE
synlige samtidig med hovedknappen ved SIDELASTING, før noe klikk i det hele tatt skjedde. Fanget
umiddelbart via et skjermbilde under lokal Playwright-verifisering (ikke synlig ved kun å lese
CSS-kilden). Fikset med en eksplisitt `.tbm-mini[hidden], .tbm-meny[hidden], .tbm-panel[hidden] {
display: none; }`-regel — samme mønster som allerede fantes for `.cookie-banner[hidden]`, bare ikke
fulgt konsekvent for den nye widgeten. **Ny fallgruve for CLAUDE.md-lista.**

Innsending går til `POST /api/tilbakemelding` — et NYTT minimal-API-endepunkt
(`Security/TilbakemeldingApi.cs`, samme "IKKE Razor Pages"-begrunnelse som `PaymentWebhooks.cs`:
ingen antiforgery-cookie å validere mot for en fetch()-basert JSON-POST), helt offentlig (ingen
`[Authorize]` — skal virke uinnlogget også), leser `ICurrentUserContext` for å auto-fylle
rolle/bruker-ID når avsenderen faktisk er innlogget.

Ny admin-side `Admin/Tilbakemeldinger` (AdminOmrade-policy, lagt til i BÅDE Program.cs sin
`AuthorizeAreaFolder`-liste OG `_Layout.cshtml` sin nav — lærdom fra en tidligere kjent fallgruve om
glemte autorisasjonsmapper er ikke gjentatt her) — faner (gjenbruker `faner.js`) per status, viser
melding/teknisk info/skjermbilde (kollapsbare `<details>`), statusoppdatering.

**Del 2: agent-API for den daglige rapporten (bygget denne runden, IKKE ennå koblet til en faktisk
planlagt jobb — se "gjenstår" under):**

Tre nye, delt-nøkkel-beskyttede endepunkter under `/api/agent/*` (samme fil) — `GET
/api/agent/tilbakemeldinger?siden=` (JSON-digest av ny tilbakemelding siden et tidspunkt, IKKE med
skjermbilder — for store/kostbare å sende ukritisk), `GET /api/agent/tilbakemelding/{id}/skjermbilde`
(ett skjermbilde om gangen, om agenten faktisk trenger å se det), `POST /api/agent/rapport` (sender
den ferdigskrevne rapporten via appens EGEN `IEmailSender` — gjenbruker `AzureEmailSender`, ingen ny
e-postinfrastruktur). Aktiveres KUN når `Tilbakemelding:AgentNokkel` er satt (samme
"fraværende = av"-mønster som `StagingGate:AccessKey`/Vipps/Vonage/ACS) — `FixedTimeEquals`-
sammenligning, samme forsiktighetsnivå som resten av appens delte-nøkkel-mønstre. Lagt til i
`StagingGate.cs` sin unntaksliste for `/api/agent/*` (beta) — samme begrunnelse som
betalings-webhookene: agenten har ingen nettleser-cookie å sende. Selve
`/api/tilbakemelding`-innsendingsendepunktet trenger IKKE unntas, siden det alltid kalles fra en
side nettleseren allerede har lastet (og dermed allerede har evt. StagingGate-cookie for).

Ny Bicep-parameter `tilbakemeldingAgentNokkel` (`@secure()`, samme mønster som
`stagingGateAccessKey` — direkte appSetting-verdi, ikke en egen Key Vault-hemmelighet, siden dette
er en app-intern delt nøkkel, ikke et tredjeparts-API-credential) lagt til i `main.bicep`/
`resources.bicep`/`main.parameters.json`, satt via `azd env set TILBAKEMELDING_AGENT_NOKKEL <verdi>`
på BEGGE miljøer (samme tilfeldig genererte 40-tegns nøkkel på begge — agenten trenger bare én
credential å huske). Rapport-mottaker-e-post er IKKE en egen Bicep-parameter — C#-koden faller
tilbake til `gauteg@gmail.com` (samme adresse som allerede står i footer-markupen, ikke en ny
eksponering) hvis `Tilbakemelding:RapportMottakerEpost` ikke er satt, så ingen infra-endring var
nødvendig for å få dette til å virke.

**Verifisert lokalt i nettleser (Playwright) FØR deploy:** standard-plassering nederst til høyre
uten å henge i hjørnet, rollup-meny med ikon+tekst på begge knapper og "Hjelp" korrekt grået ut,
skjema åpner med automatisk "Tar skjermbilde …" → forhåndsvisning (widgeten skjuler seg selv under
selve capture-øyeblikket, så skjermbildet viser SIDEN, ikke widgetens eget panel oppå den),
innsending lagret korrekt i databasen og synlig i `Admin/Tilbakemeldinger` med riktig fane-telling;
en simulert JS-feil (`throw` i en `setTimeout`) fanget automatisk og vist som advarsel FØR
innsending, og landet korrekt merket "Teknisk feil fanget automatisk" med rød kant i admin-visningen
og en egen krasjrapport-varselboks øverst på siden; minimer/gjenåpne verifisert. Build + alle 74
tester grønne (ingen nye enhetstester denne runden — funksjonaliteten er UI/HTTP-tung, dekket av
Playwright-verifiseringen i stedet, samme avveining som tidligere rene UI-fikser i natt-økten).

**Reell deploy-fallgruve truffet på LIVE (ikke beta) under denne rundens utrulling:** `azd deploy`
rapporterte `SUCCESS` og selve app-innstillingen (`Tilbakemelding__AgentNokkel`) var korrekt satt
via `azd provision`, men den KJØRENDE koden var likevel den GAMLE versjonen — widget-markup
manglet fullstendig fra utlevert HTML, og agent-API-et ga 404 selv med riktig nøkkel. Dette ER
akkurat den kjente, allerede dokumenterte fallgruven ("azd deploy kan rapportere SUCCESS uten at
koden faktisk endret seg") — bekreftet ved at et enkelt `azd deploy` nummer to umiddelbart rettet
det (widget-markup + fungerende agent-API begge verifisert etterpå). Interessant nok viste IKKE
denne kjøringen den vanlige "azd observed no App Service deployment status change"-advarselen i
loggen (den dukket derimot opp på BETA sin første deploy denne runden, som virket å ha lykkes med
én gang) — advarselen er altså ikke en pålitelig indikator i seg selv; en funksjonell sjekk (som
agent-API-et sin 404-vs-200) er det som faktisk avdekket problemet her.

**Del 3: den daglige rutinen + den fullt autonome deploy-pipelinen, begge nå FERDIG og
verifisert ende-til-ende (2026-10-01/02, samme dag som del 1/2 over):**

Daglig Claude Code "routine" (IKKE en session-only CronCreate-jobb som tidligere natt-økter i
dette prosjektet — en ekte, varig skyplanlagt agent): `trig_01WnY5ug8qJegu3DbTub5hC4`, kjører
05:00 UTC (07:00 Oslo-tid i sommertid — OBS: glir til 06:00 Oslo etter vintertid-overgangen 25.
oktober siden cron er UTC-fast, må evt. justeres med `RemoteTrigger action: update` da). Henter
`/api/agent/tilbakemeldinger` (siste døgn) OG `/api/agent/krasjrapporter` (ALLE ubehandlede,
uansett alder — lagt til nettopp for at rutinen ikke skal miste en sak hvis en kjøring feiler),
undersøker krasjrapporter mot CLAUDE.md sine kjente fallgruver, skriver en norsk HTML-e-postrapport
sendt via `/api/agent/rapport` (som bruker appens EGEN `IEmailSender`), og for krasjrapporter den
er trygg nok på: lager en EGEN BRANCH (`agent/fix-tilbakemelding-{id}`) + åpner en PR — bevisst
IKKE direkte push til `master` i denne første versjonen av prompten (se hvorfor under).

**Brukerens eksplisitte valg fra forrige runde var "fullt autonomt til live"** — det er nå reelt
bygget, men via en SEPARAT GitHub Actions-pipeline (`.github/workflows/deploy.yml`), ikke ved at
selve rutinen får Azure-legitimasjon direkte. Årsak: en skybasert rutine kan ikke gjenbruke
brukerens lokale `az`/`azd`-innlogging, og å gi en LLM-agent stående Azure-produksjonslegitimasjon
direkte er en annen risikoklasse enn å la den pushe kode til en branch som en DETERMINISTISK,
ikke-LLM-styrt CI/CD-pipeline så bygger+tester+deployer. Rutinens prompt sier derfor eksplisitt:
aldri push til master, aldri kjør azd, aldri rør Azure — kun branch+PR. Når/hvis brukeren vil
lukke sløyfen helt (rutinen pusher rett til master), er det en bevisst fremtidig endring av
PROMPTEN, ikke noe som skjedde stille nå.

**`.github/workflows/deploy.yml`:** push til `master` → bygg+test → deploy beta → helsesjekk →
KUN hvis den består, deploy live → helsesjekk. Ingen `azd provision` noe sted (kun `azd deploy` —
infrastrukturendringer forblir en manuell handling). Autentisert med en ny Azure-tjenesteprinsipal
("TestBase-GitHubActions-Deploy", appId `c6d96201-93f1-447d-bb68-f63763ffaa5c`) via OIDC-føderasjon
(ingen lagret hemmelighet/client secret) — Contributor-rolle KUN på `rg-testbase-beta` og
`rg-testbase-test`, ikke hele abonnementet.

**Fire reelle, uforutsette feil ble funnet og rettet under selve verifiseringen i nettleser/CI
(ingen av disse var synlige før en faktisk kjøring):**
1. CI-jobben manglet en MySQL-tjenestecontainer — `TestBaseWebApplicationFactory` kobler til en
   ekte database (samme mønster som lokal dev via docker-compose), og ALLE integrasjonstester
   feilet umiddelbart med "Unable to connect to any of the specified MySQL hosts" FØR noe deploy
   i det hele tatt ble forsøkt. Fikset med en `services: mysql:` i `build-og-test`-jobben, samme
   image/root-passord som `docker-compose.yml`.
2. Den faktiske OIDC-subjectstrengen GitHub presenterer for DENNE organisasjonskontoen er
   `repo:gauachieve@116502827/TestBase@1344243141:ref:refs/heads/master` — MED eier-ID og repo-ID
   innbakt, IKKE den enklere `repo:gauachieve/TestBase:ref:refs/heads/master` fra standard-
   dokumentasjonen. Den opprinnelige federated credentialen matchet derfor aldri, og
   `azd auth login` feilet med `AADSTS700213`. Rettet ved å observere den faktiske feilmeldingens
   presenterte subject og oppdatere federated credentialen til å matche eksakt.
3. **MSYS/Git Bash-sti-konverteringsfallgruven (allerede kjent fra `curl`, se CLAUDE.md) rammer
   også `az`-CLI-en**: `az role assignment create --scope "/subscriptions/...` ga en kryptisk
   `MissingSubscription`-feil fra Azure sin REST-API — `--debug` avslørte at den faktiske
   forespørselen gikk til `https://management.azure.com/C:/Program Files/Git/subscriptions/...`,
   altså at MSYS konverterte `/subscriptions/...`-argumentet til en Windows-sti FØR `az` noensinne
   så det. Løst med samme `MSYS_NO_PATHCONV=1`-prefiks som allerede er dokumentert for `curl`.
4. Begge App Services hadde en EKSISTERENDE, tydelig bevisst IP-restriksjon på SCM(Kudu)/deploy-
   endepunktet (kun eierens egen IP tillatt — live sin regel het reflektert nok
   `"AllowGauteOnlyScm"`), satt opp FØR denne økten, utenfor noe Claude Code har gjort. Dette
   blokkerte `azd deploy` sitt zip-opplastingskall med `403 Ip Forbidden` UANSETT hvor godt
   autentisert kallet var (nettverksnivå-restriksjon, ikke identitetssjekk). Siden navnet så
   bevisst ut, ble IKKE dette fjernet stille — brukeren ble eksplisitt spurt, og valgte å åpne
   KUN SCM-restriksjonen (ikke selve hovedsiden) og stole på den allerede skalerte-ned
   tjenesteprinsipalens RBAC-tilgang som reell sikkerhetsgrense i stedet. Betas HOVEDSIDE forble
   urørt (fortsatt kun eierens IP) — det betyr at CI sin helsesjekk for beta IKKE kan bruke en
   offentlig `curl` (ville fått Azures egen 403, ikke appens StagingGate-401) og i stedet sjekker
   `az webapp show --query state` — som i sin tur avdekket en FEMTE, mindre feil: `azd auth login`
   autentiserer KUN `azd` selv, ikke den separate `az`-CLI-en brukt i denne helsesjekken, som
   trengte sin egen `azure/login@v2`-innlogging.

**Underveis ble også en konkret brukerfeil fanget og rettet pragmatisk**: brukeren kjørte det
first gitte kommandosettet med bokstavelig `<APP_ID>` i stedet for den faktiske app-ID-en fra
`az ad app create` sin output — alt etter `az ad sp create` feilet dermed stille/synlig som
brukerens innrapporterte feil. Diagnostisert ved å lese faktisk Azure-tilstand direkte
(`az ad app list`/`az ad sp show`/`az role assignment list`/`az ad app federated-credential list`)
i stedet for å gjette, fullført med riktig app-ID, og GitHub-hemmelighetene satt på nytt for
sikkerhets skyld (kan ikke lese tilbake en allerede satt hemmelighets VERDI for å bekrefte den var
riktig — tryggest å bare sette den på nytt).

**Verifisert ende-til-ende**: en fullstendig grønn CI-kjøring (bygg+test → deploy beta → helsesjekk
→ deploy live → helsesjekk), etterfulgt av en uavhengig kontroll utenfor selve pipelinen
(`curl .../health` → 200, `curl .../api/agent/tilbakemeldinger` med nøkkel → 200) for å bekrefte at
live faktisk kjører ny kode, ikke bare at CI selv rapporterte suksess.

**Gjenstår (bevisst, ikke en glipp):**
- Rutinen pusher i dag til en branch+PR for krasjrapporter, ALDRI direkte til `master` — selve
  "lukk sløyfen helt"-steget (fullt autonomt UTEN en PR-godkjenning) er en bevisst utsatt,
  fremtidig promptendring, ikke bygget stille nå.
- DST-glidningen i cron-tidspunktet (nevnt over) er ikke håndtert automatisk.

## Ekte BankID for admin/behandler, del 5 — arkitekturendringen bygget, men IKKE aktivert (2026-10-01/02)

Brukeren ble godkjent som Idura/BankID-klient for produksjon, men EKSPLISITT bekreftet (etter
direkte spørsmål) at godkjenningen er "som en ren klient, ingen personnummer-scope" — altså
nøyaktig den beslutningen fra "STØ avviste fødselsnummer-bestilling" (åpenid+profile, ALDRI
nnin/nnin_altsub) som den gangen var besluttet men EKSPLISITT IKKE implementert. Denne runden
bygget selve arkitekturendringen som gjorde den implementeringen mulig.

**Ny kobling-basert innloggingsmodell** (BankID gir oss aldri personnummeret, kun en stabil, IKKE
kryptert "sub"-identifikator):
- Ny `BankIdSubjekt`-kolonne (unik, nullable) på BÅDE `Administrator` og `Behandler` — ny migrasjon.
- `AdminAuthenticationService`/`BehandlerAuthenticationService` fikk `FinnVedBankIdSubjektAsync`
  (ekte SQL `WHERE`, siden sub IKKE er kryptert — i motsetning til `FinnVedPersonnummerAsync`) og
  `KoblBankIdSubjektAsync`.
- `ProfesjonellInnloggingService` refaktorert: den delte betrodd-enhet/2FA/rolle-logikken trukket ut
  i to private hjelpemetoder (`FullforForAdministratorAsync`/`FullforForBehandlerAsync`), gjenbrukt
  av TRE offentlige inngangspunkter nå — `FullforAsync(personnummer)` (uendret, kun mock-BankID-
  veien), NY `FullforMedBankIdSubjektAsync(sub)` (prøver sub-oppslag først, returnerer en NY
  `TrengerKobling`-resultattype hvis ukjent), og NY `KoblOgFullforAsync(sub, personnummer)` (matcher
  via det OPPGITTE personnummeret akkurat som før, kobler sub-en PERMANENT til kontoen, fullfører
  deretter identisk).
- NY side `Pages/Konto/BankIdKobleKonto` — vises AUTOMATISK første gang en ekte BankID-sub ikke
  gjenkjennes: ber om personnummeret brukeren allerede er registrert med (ÉN gang per konto), matcher
  i minnet (samme krypterte sammenligning som alltid), kobler, fortsetter til vanlig 2FA/innlogging.
  Verdiene (sub/huskMeg/returnUrl) bæres via skjulte skjemafelt, IKKE TempData på tvers av GET/POST
  (TempData overlever ikke pålitelig mer enn én lesing, se kjent fallgruve).
- `Program.cs` sitt `BankIdInnlogging`-OIDC-schema: scope endret fra `openid+ssn` til `openid+profile`
  — `OnTokenValidated` leser nå `sub` (med en `ClaimTypes.NameIdentifier`-fallback, siden
  `JwtSecurityTokenHandler` sin DEFAULT inbound-claim-mapping kan omdøpe akkurat "sub", i motsetning
  til "ssn" som ikke var i mappingtabellen — ny fallgruve, se under).
- 5 nye regresjonstester (`BankIdKoblingTests.cs`, 79 totalt) — dekker sub-oppslag før/etter kobling
  for begge roller, `TrengerKobling` for ukjent sub, og `KoblOgFullforAsync` sin vellykkede/mislykkede
  personnummer-match. Konstruert en EGEN frittstående `DefaultHttpContext` (ikke en full HTTP-
  rundtur) for å teste `ProfesjonellInnloggingService` direkte — enklere enn å simulere en hel
  OIDC-utveksling, og dekker den nye logikken som faktisk er ny her.

**REELT, UAVKLART FUNN under verifisering mot Idura sin EGEN TEST-sandkasse (ikke ekte BankID,
"DEMO"-merket Idura Test-miljø — trygt, ingen ekte identitetsbekreftelse involvert):** selve
autorisasjons-URL-en nettleseren ble sendt til viste `scope=openid+profile+sub_nnin+sub_bankid` —
IKKE bare `openid+profile` som koden ber om. Idura/BankID sin faktiske oppstrøms-forespørsel
inkluderer altså `sub_nnin`/`sub_bankid` UANSETT hva klienten (vår kode) spør om i sin egen
`scope`-parameter — dette er nesten helt sikkert en konfigurasjon på IDURA-ANVENDELSE-nivå (i deres
eget dashbord/klientoppsett for akkurat denne test-klienten, `urn:my:application:identifier:465078`),
ikke noe `options.Scope.Add(...)` i vår kode kan styre. **Dette er testTENANTEN, ikke
produksjonstenanten (`urn:my:application:identifier:8821`)** — ukjent om samme utvidelse skjer der,
og dette er nøyaktig det brukeren MÅ avklare (enten ved å se selve den faktiske produksjons-
autorisasjons-URL-en ved første ekte forsøk, eller direkte med Idura/Stø) FØR
`Miljo:EktBankIdProfesjonell` skrus på for live — ellers er hele poenget med denne runden sin
arkitekturendring (aldri be om personnummeret) illusorisk hvis broker-nivået sender det uansett.
Kunne ikke fullføre en ende-til-ende-verifisering i selve Idura-sandkassen: feltet avviste BÅDE
prosjektets egne faste mock-personnummer OG et egenhendig beregnet, kontrollsiffer-GYLDIG norsk
fødselsnummer som "Invalid Identity Number" — Idura sin testsandkasse ser ut til å kreve et
spesifikt, FORHÅNDSREGISTRERT testidentitetsnummer (ikke dokumentert noe sted i dette prosjektet
fra før), ikke en hvilken som helst gyldig konstruert en. Uavklart, ikke forsøkt videre.

**Ny fallgruve for CLAUDE.md-lista:** `JwtSecurityTokenHandler` sin DEFAULT inbound-claim-mapping
(`MapInboundClaims=true`) omdøper standard OIDC-claimet "sub" til `ClaimTypes.NameIdentifier` FØR
koden ser `ClaimsPrincipal`-en — i motsetning til et ikke-standard claim-navn som "ssn", som forblir
bokstavelig. Kode som leser "sub" direkte via `FindFirst("sub")` kan derfor få `null` selv om claimet
faktisk kom tilbake. Løst defensivt (prøv begge navn), IKKE bekreftet med en reell suksessfull
innlogging ennå (se funnet over).

**STATUS: bygget, testet (enhetsnivå), deployet som KODE til begge miljøer, men `Miljo:
EktBankIdProfesjonell` er FORTSATT `"false"` på live** — IKKE skrudd på. Skal IKKE skrus på før
scope-spørsmålet over er avklart, siden konsekvensen av å ta feil er at ekte BankID-innlogging for
administrator/behandler enten feiler helt (hvis Idura avviser den reduserte scope-forespørselen for
produksjonsklienten) eller at hele "ingen personnummer"-poenget er meningsløst (hvis Idura sender
det uansett, i hvilket tilfelle koblingssiden aldri trengs og personnummeret kunne vært lest direkte
som før STØ-avvisningen).

## Ekte BankID for admin/behandler, del 6 — scope-spørsmålet avklart POSITIVT, men et EKTE
## feilkonfigurert acr_values fantes (2026-10-02, samme dag)

Brukeren skrudde på `Miljo:EktBankIdProfesjonell` på LIVE for første gang og gjorde et ekte forsøk.

**Scope-spørsmålet fra del 5 er nå AVKLART, og godt nytt:** den faktiske utgående autorisasjons-URL-
en til produksjonsklienten (`urn:my:application:identifier:8821`) viste `scope=openid+profile` —
PRESIS det koden ber om, INGEN automatisk tillegg av `sub_nnin`/`sub_bankid` slik test-tenanten
(del 5) viste. Produksjonsklienten er altså korrekt konfigurert hos Idura for "ren klient"-
godkjenningen — hele kobling-basert-innlogging-arkitekturen fra del 5 er dermed den RIKTIGE
løsningen, ikke et unødvendig omvei.

**Men selve innloggingsforsøket feilet likevel — med "Got HTTP Status code from upstream:
Unauthorized" på selve Idura-domenet (`psytest-no.idura.broker`), FØR noen BankID-interaksjon i det
hele tatt** (ingen app-prompt på telefonen). Diagnostisert ved å: (1) sjekke
`az webapp auth show` — EasyAuth (App Service sin egen plattform-autentisering) er IKKE aktivert,
ikke årsaken; (2) streame `az webapp log tail` + laste ned en full loggpakke via
`az webapp log download` MENS brukeren gjorde et nytt forsøk — INGEN spor av selve
`/signin-bankid-innlogging`-kallet noe sted i appens egne logger, som bekrefter at forespørselen
ALDRI nådde vår applikasjon i det hele tatt. Feilen skjer altså hos IDURA SELV, på deres EGEN
`/oauth2/authorize`-endepunkt, før noen redirect videre til faktisk BankID.

**Rotårsak funnet av BRUKEREN i Iduras eget dashbord**, ikke av kode-analyse: dashbordet viste en
liste over eID-metoder med deres tilhørende `acr_values` — "For no bankid" (vanlig BankID,
PIN-kode) er den UKVALIFISERTE `urn:grn:authn:no:bankid`, mens "for bankid biometrics" (en EGEN,
STRENGERE eID-metode som krever fingeravtrykk/ansiktsgjenkjenning aktivert) er
`urn:grn:authn:no:bankid:substantial`. Koden sin produksjonsdefault (satt under "Ekte BankID for
admin/behandler, del 4" tidligere i prosjektet) var nettopp `substantial` — valgt den gangen ut fra
en antakelse om at "substantial = har en aktivert BankID-app" (basert på at "substantial" feilet på
TEST-tenanten med en "du må aktivere BankID-appen"-feilmelding, og at en EKTE bruker naturligvis
HAR dette). **Denne antakelsen var feil**: "substantial" er en EGEN, strengere eID-metode
(biometri), ikke bare en indikator på "ekte bruker med aktivert app" — en helt vanlig BankID-bruker
UTEN biometri konfigurert (svært vanlig) ville ALDRI kunne bruke "substantial", og Idura avviser
forespørselen umiddelbart med 401 FØR brukeren i det hele tatt får sjansen til å prøve, nøyaktig
det observerte symptomet.

**Fikset**: `src/TestBase.Web/Program.cs` sin produksjonsdefault endret fra
`urn:grn:authn:no:bankid:substantial` til `urn:grn:authn:no:bankid` (vanlig BankID, ingen
biometri-krav) — fortsatt overstyrbar per miljø via `BankId:IduraProduksjon:AcrValues` om en
fremtidig variant (f.eks. kreve biometri for en spesifikk rolle) skulle bli aktuelt. Build grønn,
deployet.

**Reelt funn underveis — en driftshendelse på LIVE, selvpåført og selv-rettet samme økt**: mens
`Miljo:EktBankIdProfesjonell=true` sto på (for å teste), var admin/behandler-innlogging på LIVE
HELT UTILGJENGELIG for ALLE — ikke bare den ekte BankID-veien (som feilet som beskrevet over), men
OGSÅ mock-BankID-veien, siden `LoggInn.cshtml.cs` sin `OnPostAsync` sjekker
`HarEktBankIdAsync()` FØRST og UBETINGET omdirigerer til den ekte OIDC-flyten når schemaet er
registrert — og AdminId+passord-unntaket er allerede separat deaktivert på live
(`Miljo:TillatUtviklingsSnarveier=false`). Altså: null fungerende innloggingsvei for administrator/
behandler på live i hele vinduet flagget sto på. Oppdaget og rettet (flagget satt tilbake til
`"false"` + ny `azd provision`) så snart feilen ble bekreftet — ingen reell bruker rapportert
rammet, men dette er en konkret advarsel: `Miljo:EktBankIdProfesjonell=true` bør ALDRI stå på uten
at den faktiske BankID-flyten er bekreftet fungerende, siden det IKKE finnes noen fallback-vei
igjen på live når den er aktiv. Vurder en fremtidig "test ekte BankID i et eget vindu, ikke
hovedknappen" for å unngå at en fremtidig feilkonfigurasjon låser ALLE ute igjen.

**STATUS: rettet, IKKE verifisert ende-til-ende ennå** (ingen fullført ekte BankID-innlogging har
skjedd — kun helt frem til selve Idura-autorisasjonssiden). `Miljo:EktBankIdProfesjonell` er
`"false"` på live igjen. Neste steg: brukeren prøver på nytt med den rettede acr_values.

## Ekte BankID for admin/behandler, del 7 — acr_values-fiksen var IKKE nok, problemet isolert til
## Idura↔Stø/BankID-leddet, eskalert til Stø support (2026-10-02, samme dag)

Brukeren prøvde på nytt etter del 6-fiksen (`urn:grn:authn:no:bankid`) — SAMME 401 på Iduras egen
`/oauth2/authorize`, fortsatt før noen BankID-interaksjon. Systematisk utelukket, i rekkefølge:

1. **Callback-URL** i Idura sin applikasjonskonfigurasjon ("OpenID Connect"-fanen) — brukeren
   bekreftet/oppdaterte denne, ingen endring i feilen.
2. **PKCE/PAR-krav** — skjermbilde av applikasjonens OIDC-innstillinger viste "Require PKCE" og
   "Require PAR" BEGGE av — ingen av delene tvinger en avvisning.
3. **Client ID/Secret** — brukeren sendte SMS-en med klienthemmeligheten til PC for eksakt
   copy-paste (ikke manuell retyping) — fortsatt samme feil, skriverklareringer utelukket.
4. **Identity Providers → NO BankID**-siden (selve Idura↔BankID-koblingen, ATSKILT fra applikasjons-
   nivået) viste "Configured"/"ready for production", riktig standard assurance-nivå (High, matcher
   det ukvalifiserte acr_value), "Require SSN" korrekt AV. Stø sin bekreftelses-e-post (client_id
   `psytestno_25eb8adf-bankid-prod`, miljø = BankID sin faktiske PROD-realm, redirect-URIer til
   Iduras egne broker-endepunkter) stemte overens med det som faktisk står i Idura-dashbordet — ingen
   mismatch funnet.
5. **AVGJØRENDE funn**: Iduras EGEN innebygde test-innlogging for denne klienten feiler IDENTISK —
   dette skjer altså UTEN at vår applikasjon er involvert i det hele tatt, noe som isolerer
   problemet entydig til noe på Idura↔Stø/BankID-siden av koblingen for akkurat denne klienten,
   IKKE noe i vår OIDC-klientkonfigurasjon.

**Konklusjon: ingen flere kodeendringer eller dashbord-innstillinger er diagnostiserbare fra vår
side.** Verken Idura sin dokumentasjon (sjekket via WebFetch/WebSearch: ingen omtale av et
aktivitets-/forespørselslogg i dashbordet) eller noe vi kan se i applikasjonskonfigurasjonen peker
på en konkret årsak. Saken er eskalert til Stø support (via deres kundeportal, IKKE e-post-svar —
Stø sin egen e-post ber eksplisitt om dette) med full teknisk kontekst: client_id, nøyaktig
feilmelding og -sted, at Iduras eget testverktøy feiler likt, og hva som er utelukket. Mulige
gjenstående årsaker utenfor vår kontroll: en ikke-signert brukeravtale ("Usage agreement"-fanen,
ikke bekreftet sjekket), IP-hvitelisting av Iduras broker-infrastruktur hos Stø, eller at kontoen
ikke faktisk er aktivert på Stø sin backend til tross for bekreftelses-e-posten.

`Miljo:EktBankIdProfesjonell` satt tilbake til `"false"` på live. **Ingen videre handling mulig før
svar fra Stø support.**

## HCR-20 V3: Tilstede+Relevans slått sammen til ett ledd per faktor (2026-10-02, samme dag)

Brukerens beslutning: de 20 risikofaktorene (H1-H10/C1-C5/R1-R5) hadde opprinnelig to separate ledd
hver (Tilstede: Ukjent/Nei/Delvis/Ja, og Relevans for fremtidig risiko: Ukjent/Lav/Moderat/Høy) —
slått sammen til ÉT ledd per faktor, "{Kode}. {Navn} — Tilstede og relevans for fremtidig risiko",
med KUN Relevans-skalaen beholdt (Ukjent/Lav/Moderat/Høy). Begrunnelse: verktøyet er BEVISST IKKE et
sumskår-verktøy (ren strukturert-skjønn-støtte til klinikeren), så den ekstra oppdelingen i to
separate avkrysninger ga lite verdi utover datainnsamling. Avveining flagget til brukeren FØR
implementering: det ekte SIFER-skjemaet behandler "var faktoren til stede" og "er den relevant
fremover" som reelt atskilte kliniske spørsmål (en faktor kan være historisk sterkt til stede men nå
lite relevant etter vellykket behandling, eller omvendt) — denne sammenslåingen gjør at disse to
vurderingene ikke lenger kan registreres uavhengig av hverandre. Brukeren hadde allerede vurdert
dette og ønsket forenklingen likevel.

Endret: `Hcr20V3TestSeeder` (fjernet `TilstedeSkala`-konstanten, halverte antall ledd per faktor-side
fra 20 til 10/5/5), `Hcr20V3Skaaringsberegner` (løkken over hver faktor-side leser nå HVERT ledd
direkte i stedet for Tilstede/Relevans-par — `for (i; i+1 < count; i += 2)` → enkel `foreach`),
`SkaaringsberegnereTests.cs` sin `Hcr20V3Bygg`-testhjelper (bygger nå ett kombinert ledd per faktor
i stedet for et par). Ingen migrasjon nødvendig (ren seeder-/skåringslogikk, ingen skjemaendring).
Alle 51 skåringsberegner-tester (inkl. de 2 HCR-20-spesifikke) grønne etter endringen.

**MERK**: `IInnebygdTestSeeder.SeedAsync` sin `if (eksisterende is not null) { ... return; }`-vakt
betyr at denne kodeendringen IKKE automatisk oppdaterer strukturen til en allerede seedet HCR-20 V3-
test i noen database (lokal/beta/live) — kun NYE databaser får den nye strukturen direkte. En
eksisterende instans må slettes helt (samme `TestService.SlettTestHeltForRegenereringAsync`-mønster
som SCID-5-PF-restruktureringen, "Natt-økt, del 13") og re-seedes for å få den nye strukturen — IKKE
gjort i denne runden, da det ikke er bekreftet om HCR-20 V3 allerede er seedet noe sted med ekte
vurderinger. Sjekk dette FØR regenerering hvis/når testen faktisk skal brukes.

## Forenkling av Tildel/Tester (2026-10-02, samme dag)

Brukerens tilbakemelding: med stadig flere innebygde tester ble `Behandlerportal/Tildel/Tester`
uoversiktlig — pris-/kostnads-/utfyllingsdimensjonene var ikke synlige nok, og honorar-feltene
inline i testlisten tok mye plass. Tre endringer, alle KUN på Behandlerportal-siden (brukeren
nevnte kun denne URL-en — Admin/Tildel/Tester er en nesten identisk søstersside, IKKE endret i
denne runden, se egen vurdering under):

1. **"Ditt honorar" flyttet til en egen "Sett honorar"-dialog** FØR oppsummeringsdialogen (og før
   planleggingsdialogen, når den finnes) — kun vist når minst én VALGT test faktisk har prising
   (`StorstePrisKr > 0`). Selve `<input name="HonorarKr[...]">`-feltene er FYSISK FLYTTET inn i
   denne nye dialogen (fortsatt inni `<form>`, så modellbinding er uendret) i stedet for inline i
   hver test-`<li>`. `wwwroot/js/tildel.js` fikk en ny felles `gaTilPlanleggingEllerOppsummering()`-
   funksjon (samme logikk som før lå direkte i `apneKnapp`-lytteren) kalt BÅDE fra
   "Gå til oppsummering" (når ingen honorar trengs) og fra den nye "Neste"-knappen i
   honorardialogen.
2. **Ny `Test.HarKostnadPerGjennomforing`-kolonne** (bool, default false, ny migrasjon
   `LeggTilKostnadPerGjennomforing`) — ATSKILT fra `StorstePrisKr` (pasientens pris): markerer at
   testen påfører PRAKSISEN en reell kostnad per gjennomføring (f.eks. en fremtidig lisensavgift).
   Satt sann KUN for `mini_strukturdemo` (M.I.N.I.-strukturdemoen) foreløpig — samme
   "seeder-satt, ingen admin-UI ennå"-mønster som `IcdElleveKlar`/`FyllesUtAvBehandler`.
3. **Tre nye ikoner** i `_Ikon.cshtml` ("pluss", "dollar", "klinikk") + en forklaringslinje øverst
   på siden som viser alle tre med tekst. Per test i listen: "+" når `StorstePrisKr > 0` (pasienten
   kan belastes), "$" når `HarKostnadPerGjennomforing` (koster praksisen noe per gjennomføring),
   klinikk-ikon + UNDERSTREKET testnavn når `FyllesUtAvBehandler` (fylles ut av behandler, ikke
   pasient) — alle tre kan vises samtidig på samme test.

**Docker Desktop-avhengighet under utvikling, OPPDATERT**: migrasjonen ble først HÅNDSKREVET (ikke
generert via `dotnet ef migrations add`) siden Docker Desktop ikke svarte i denne økten — årsaken
viste seg å være ganske enkelt at brukeren selv ikke hadde startet Docker Desktop (ikke et reelt
teknisk problem). `cmd.exe /c start` klarte IKKE å faktisk starte appen (ingen prosess, ingen feil —
stille mislyktes, trolig fordi denne CLI-økten ikke har en interaktiv skrivebordsøkt GUI-apper kan
feste seg til), men PowerShell sin `Start-Process` LYKTES (fem `Docker Desktop`-prosesser observert
rett etter). Migrasjonen ble deretter RE-VERIFISERT med en ekte lokal MySQL: `dotnet ef database
update` kjørte den håndskrevne migrasjonen problemfritt, og en etterfølgende kontroll-migrasjon
(`dotnet ef migrations add ZZZ_...`, deretter fjernet) genererte en TOM `Up`/`Down`-kropp — beviser
at `AppDbContextModelSnapshot.cs` sin manuelle oppdatering var eksakt riktig, ingen avvik fra
modellen. Alle 79 integrasjonstester (ikke bare de 51 DB-uavhengige skåringstestene) grønne mot ekte
database. Hele Tildel/Tester-flyten verifisert ende-til-ende i en ekte nettleser (Playwright, dev-
admin → Behandler-rolle via Bytt modus): alle tre ikoner render tydelig og korrekt differensiert
("+" som et pluss-tegn, "$" som et dollar-tegn, en stetoskop-lignende klinikerikon) på EKSAKT de
riktige testene (bekreftet via tilgjengelighets-treets `title`-attributter, ingen falske positiver/
negativer), klinikerutfylte tester vises understreket, og hele "Sett honorar → Planlegging →
Oppsummering"-kjeden fungerer korrekt (honorardialogen viste KUN den prisede testen av to valgte,
beløpet forplantet seg riktig til slutt-oppsummeringen). Full verifisering, ikke lenger avhengig av
CI som eneste sannhetskilde.

**Vurdering — Admin/Tildel/Tester IKKE endret**: admin-siden mangler all prisings-infrastruktur
(`tildel.js` sin egen kommentar: "Admin-tildeling er alltid 'IkkePåkrevd'") og admin ser uansett
ALLE tester uavhengig av partner-tilgang, så "+"/honorar-dialogen ville vært meningsløs der —
men "$" (kostnad per gjennomføring) og klinikk-ikon+understrek er relevante uavhengig av prising.
Ikke gjort i denne runden siden brukeren kun ba om Behandlerportal-siden; bør vurderes som en
liten oppfølging hvis admin trenger samme oversikt.

## Ekte BankID for admin/behandler, del 8 — FUNGERER, tre UX-funn rettet (2026-10-02, samme dag)

Brukeren fikk bekreftet at BankID-innloggingen faktisk fungerer etter Stø sin fiks (del 7). Tre
reelle UX-problemer rapportert fra selve den vellykkede innloggingen, alle undersøkt og to rettet:

1. **Personnummer tastes inn to ganger.** `Pages/Konto/LoggInn.cshtml.cs` sin `OnPostAsync` hopper
   RETT til `Challenge(..., "BankIdInnlogging")` når ekte BankID er aktiv — `PersonnummerOverride`
   leses ALDRI i denne grenen. Likevel vises feltet fortsatt, siden `VisPersonnummerOverride` kun
   sjekket `Miljo:TillatPersonnummerOverride`, uavhengig av om ekte BankID faktisk er aktiv for
   DENNE innloggingen — rent dødt felt som forvirret brukeren til å taste personnummeret unødvendig
   FØR BankID-appen uansett ber om det på nytt. Fikset: ny `LoggInnModel.ErEktBankIdAktiv`
   (cachet via `OnGetAsync`/`OnPostAsync`, FØR `HarEktBankIdAsync()` gjøres privat), og
   `VisPersonnummerOverride` viser nå KUN feltet når ekte BankID IKKE er aktiv. Forklaringsteksten
   under knappen er også splittet i to grener (ekte BankID vs. mock) slik at den ikke lenger sier
   "mock" når ekte BankID faktisk kontaktes.
2. **BankID-siden vises på engelsk.** Verken `"BankIdInnlogging"`- eller det diagnostiske
   `"BankIdTest"`-schemaet sendte noen gang en `ui_locales`-parameter i
   `OnRedirectToIdentityProvider` — standard OIDC-parameter for å be om et språk, ikke noe BankID/
   Idura gjetter selv uten å bli bedt om det. Fikset: `ctx.ProtocolMessage.UiLocales = "nb"` lagt
   til på BEGGE schemaer. IKKE verifisert ende-til-ende ennå (krever en ny reell innlogging) —
   avhenger av at BankID/Idura faktisk respekterer parameteren, som er en rimelig antakelse for en
   standard OIDC-funksjon, men ubekreftet.
3. **Bekrefter personnummer en tredje gang etter innlogging.** IKKE en bug — dette er
   `Pages/Konto/BankIdKobleKonto`, den BEVISSTE engangskoblingen fra "Ekte BankID for
   admin/behandler, del 5": siden foretaket kun er godkjent for `openid+profile` (ikke
   personnummer-utlevering), må BankID sin anonyme "sub"-identitet kobles til en eksisterende
   konto via personnummer ÉN gang. Skal ALDRI vises igjen for samme konto etter at koblingen er
   lagret (`BankIdSubjekt`-kolonnen). Forklart til brukeren, ingen kodeendring.

Build grønn, alle 79 integrasjonstester grønne. Punkt 1 og 2 deployet; punkt 3 er ren forklaring.

## Ekte BankID — driftsbryter uten redeploy (2026-10-02, samme dag)

Brukerens spørsmål: kan ekte BankID slås av/på på live med en radioknapp, uten `azd provision`/
omstart, nå som de vil bruke ekte BankID på live mens de selv fortsatt er i dev lokalt? Svaret FØR
denne endringen var nei — `Miljo:EktBankIdProfesjonell` ble lest ÉN GANG ved oppstart i Program.cs
for å avgjøre om selve OIDC-schemaet ("BankIdInnlogging") i det hele tatt registreres, og en
ASP.NET Core-autentiseringsscheme kan ikke legges til dynamisk uten omstart.

**Løsning, mønster lånt fra `BetaInnstillingService`/`BetaSwitchingVippsClient`** (se "Beta-miljø"):
ny `EktBankIdInnstilling` (singleton-rad, Id=1, samme "lat opprettelse"-mønster) +
`EktBankIdInnstillingService`, lest FERSKT (ikke cachet) av `LoggInnModel.HarEktBankIdAsync()` ved
HVERT innloggingsforsøk. `Miljo:EktBankIdProfesjonell` BEHOLDES uendret som den grovere
"kan dette miljøet i det hele tatt registrere schemaet"-bryteren (fortsatt krever omstart å slå PÅ
første gang per miljø — ingen endring i infra/Bicep) — den nye databaseraden er den FINERE
"er det faktisk aktivt akkurat nå"-bryteren, togglbar momentant.

Ny radioknapp-bryter i `Admin/MinSide` ("BankID-innlogging (admin/behandler)"), Superadmin/
Utvikler-only (SAMME rolletilgang som betalingsmodus-bryteren, men BEVISST IKKE begrenset til
`Miljo:ErBeta` slik betalingsbryteren er — ekte BankID gjelder live akkurat som beta, ikke bare
beta). Vises KUN når "BankIdInnlogging"-schemaet faktisk er registrert i miljøet (ellers
virkningsløs uansett stilling). `ErAktiv` defaulter til `true` (motsatt av betalingsbryterens
Mock-first-prinsipp) — BEVISST, siden raden introduseres mens ekte BankID allerede var aktivt og
ønsket på live; en `false`-default ville gitt en overraskende regresjon til mock ved første deploy
av denne funksjonen. Auditlogget eksplisitt (`EndreEktBankIdAktiv`), samme begrunnelse som
betalingsmodus-bryteren — en sikkerhetssensitiv bryter, ikke vanlig CRUD.

`LoggInn.cshtml.cs` sin personnummer-felt-synlighet og forklaringstekst (se forrige seksjon,
"del 8") reagerer nå korrekt på DENNE raden, ikke bare scheme-registrering.

**Verifisert fullt ende-til-ende lokalt** (siden lokal dev normalt ALDRI har
`BankId:IduraProduksjon:*`-nøkler satt, og dermed aldri registrerer schemaet i det hele tatt):
startet en lokal økt med MIDLERTIDIGE, FIKTIVE Idura-nøkler (`https://example-test.idura.broker` +
dummy client-id/secret) kun for å tvinge frem scheme-registrering til testformål — ingen ekte
Idura-konto involvert. Bekreftet i nettleser: (1) bryteren vises korrekt for Superadmin, starter på
"Ekte BankID" (matcher default); (2) bytte til "Mock BankID" + lagre endrer UMIDDELBART
`Pages/Konto/LoggInn` sin tekst/felt UTEN noen serverrestart; (3) selve innloggingsforsøket i
mock-modus ga den forventede "Fant ingen administrator- eller behandlerkonto"-meldingen (beviser at
POST-handleren faktisk tok mock-grenen, ikke bare at teksten så riktig ut); (4) bytte tilbake til
"Ekte BankID" + et nytt innloggingsforsøk ga en 500-feil FRA et mislykket OIDC-discovery-kall mot
den fiktive testautoriteten — nettopp det forventede resultatet når ekte-BankID-grenen faktisk
forsøker en reell redirect mot en adresse som ikke finnes, og dermed beviser at toggelen styrer den
FAKTISKE koden, ikke bare visningen. Build grønn, alle 79 integrasjonstester grønne, ny migrasjon
(`LeggTilEktBankIdInnstilling`) generert normalt via `dotnet ef migrations add` (Docker fungerte nå)
og anvendt lokalt.

## MPFI-24 lagt til, med en generell "krever biologisk kjønn"-mekanisme (2026-10-03)

Brukeren la to filer i prosjektroten ("Norsk MPFI-24 kortversjon.pdf", "MPFI_24 skåring.xlsx") og ba
om at testen legges til, med et eksplisitt krav om at administrering skal FEILE (ikke bare advare)
for en pasient uten registrert biologisk kjønn, siden normeringen er kjønnsspesifikk. Bygget mens
brukeren var bortreist ("go with your recommendations") — alle designvalg under er egne vurderinger,
ikke bekreftet med brukeren underveis.

**Kilde**: PDF-en er en OFFISIELL norsk oversettelse (Elen/Johansen/Lyby, etter tillatelse fra
opphavsmann Ronald Rogge 31.01.20, støttet av Stiftelsen CatoSenteret) — SAMME "bruk verbatim
offisiell oversettelse"-mønster som WHO-5/PHQ-9/MADRS-S, ikke "egenforfattet parafrasering"-mønsteret
brukt for tester uten slik tillatelse (EDE-Q/TRAPS II/SCID-5-PF). Originalpublikasjon: Rolffs,
Rogge & Wilson (2018), Assessment 25(4):458-482.

**Struktur**: 24 ledd, 12 delskalaer à 2 ledd (6 FLEKSIBILITET: Aksept/Nærvær/Selvet/Defusjon/
Verdier/Forpliktende handling — 6 RIGIDITET: Opplevelsesunngåelse/Manglende kontakt med nået/
Begrepsselv/Fusjon/Manglende kontakt med verdier/Passivitet), 6-punkts Likert (1=Stemte aldri …
6=Stemte alltid). Delskåre = gjennomsnitt av sine 2 ledd. To globalskårer (gjennomsnitt av de 6
delskalaene i hver gruppe) — BEVISST IKKE én sumskår på tvers av fleksibilitet+rigiditet, de er to
uavhengige akser i Hexaflex-modellen (kan begge være høye samtidig), så `SkjulProsent=true` (samme
prinsipp som CORE-A/M.I.N.I.).

**Reell feil funnet og rettet i kildearket FØR noe ble bygget**: regnearkets "Fusjon"-delskåre-
formel (`AVERAGE(C30:C32)`) inkluderte ved en drag-fill-feil ledd 21 i tillegg til de tiltenkte
leddene 19-20 — radens EGEN etikett sier eksplisitt "ledd 19 og 20", og ledd 21 brukes allerede,
korrekt, i NESTE rad ("Mangel på kontakt med verdier ledd 21 og 22"). Bekreftet mot MPFI sin kjente
hexaflex-itemstruktur i originalpublikasjonen at 19-20 er riktig. Rettet i vår implementasjon (IKKE
reprodusert regnearkets feil) — dekket av en egen enhetstest som eksplisitt beviser ledd 21 IKKE
smitter over i Fusjon-delskåren.

**Kjønnsspesifikk normering — ny generell mekanisme, ikke MPFI-spesifikk hack**:
1. Ny `Test.KreverBiologiskKjonn`-kolonne (bool, samme "seeder-satt flagg"-mønster som
   `IcdElleveKlar`/`FyllesUtAvBehandler`).
2. `TestTildelingsService.TildelOgVarsleAsync` sjekker dette FØR en tildeling opprettes for et
   (pasient, test)-par — en pasient uten `BiologiskKjonnVedFodsel` (f.eks. en "prøv systemet"-
   QR-pasient med ufullstendig profil) får IKKE tildelingen, og en ny `TildeltPasientResultat.
   IkkeTildelteTesterGrunnet`-liste bærer en tydelig feilmelding tilbake. BEVISST en PARTIELL
   feil (kun de aktuelle testene for akkurat den pasienten hoppes over, resten av batchen
   fullføres normalt) — IKKE en hard exception som ville avbrutt en hel batch-tildeling til flere
   pasienter på grunn av én enkelt pasient. Nytt "Ikke tildelt"-avsnitt i BEGGE Tildel/Tester.cshtml
   (Admin + Behandlerportal) viser disse feilene tydelig for behandler/admin.
3. Ny `ITestSkaaringsberegnerMedBiologiskKjonn`-grensesnitt (arver `ITestSkaaringsberegnerMedLedd`,
   samme default-interface-utvidelsesmønster) — et ANNET forsvarslag for selve skåringen (f.eks. en
   tildeling fra FØR kjønnet evt. ble nullstilt igjen): `TestService.BeregnSkaaringAsync`/
   `HentSkaaringHistorikkAsync` slår opp pasientens kjønn og sender det inn. En `null` her gir en
   tydelig `GyldighetsAdvarsel` i returnert `TestSkaaring` — ALDRI en kastet exception (samme
   "degrader nådig, ikke 500"-prinsipp som EDE-Q/GADIT-fiksene).

**Visualisering**: ny `MpfiRadarBeregner` (samme geometri-mønster som `Sipp118RadarBeregner`, men
med to hexagon-radarer — én fleksibilitet, én rigiditet — og BEGGE en datapolygon (pasientens skåre)
OG en normpolygon (kjønnsspesifikk, VARIERER per delskala/kjønn, i motsetning til Sipp118 sin FASTE
cutoff-ring) tegnet SAMTIDIG. Wired inn i BEGGE Rapport.cshtml/.cshtml.cs (Admin + Behandlerportal),
samme `Html.Raw`-mønster for SVG `<text>`-elementer som alle tidligere grafer (kjent Razor-
fallgruve).

**En reell driftsfeil fanget under verifisering, IKKE en kode-feil**: testen vises ALDRI i
Admin/Tester-listen etter første "Regenerer innebygde tester"-klikk — årsak: `IInnebygdTestSeeder`-
og `ITestSkaaringsberegner`-implementasjoner krever EKSPLISITT DI-registrering i Program.cs (ett
`AddScoped<...>`-kall hver, IKKE automatisk sammenstikking/reflection), en lett-å-glemme-detalj for
enhver FREMTIDIG ny innebygd test — lagt til i fallgruve-listen under.

**Verifisert FULLT ende-til-ende i nettleser** (Docker kjørte denne gangen): (1) gender-gate
blokkerer korrekt — forsøk på tildeling til en pasient uten kjønn satt ga en tydelig "Ikke tildelt"-
feilmelding, ingen tildeling opprettet; (2) positiv vei — tildeling til en pasient MED kjønn satt
(Mann) lyktes normalt; (3) full utfylling som pasient (24 ledd, alle satt til "Stemte av og til" =
verdi 3) fullførte uten feil; (4) rapportvisning som behandler viste korrekte delskårer (alle
3.00/norm), korrekt fortolkningstekst med riktig mannsnorm, og BEGGE radar-hexagonene rendret
korrekt med nøyaktig 6 akser hver og riktige navn — inkludert en presis sjekk av korrekt positiv/
negativ-fargekoding (3.00/3.00 for Opplevelsesunngåelse — eneste eksakte norm-treff — vist GRØNN,
alle andre korrekt RØDE siden pasienten skåret lavere enn fleksibilitetsnormene og høyere enn
rigiditetsnormene). 3 nye enhetstester (82 totalt). Migrasjon (`LeggTilKreverBiologiskKjonn`)
generert normalt via `dotnet ef migrations add` og anvendt lokalt. IKKE pushet ennå — commitet
lokalt, venter på brukerens retur for å bekrefte push til live.

## Hjelpemeny: rolle- og kontekstsensitiv, ikke-modal (2026-10-03)

Brukerens krav (ordrett i praksis): rolle-sensitiv, kontekst-sensitiv ("hjelp med det du driver
med akkurat nå"), et rollup/popup-vindu på høyre side som IKKE er modalt, full skjerm på mobil,
søkefunksjon øverst, kun innlogging/generell info når ikke innlogget, en FAQ basert på et
kvalifisert gjetn om hva folk vil streve med, og "best practice for hjelpesider" generelt.

**Arkitektur — statisk innhold, ingen database** (samme mønster som `IInnebygdTestSeeder`-
testinnholdet, ikke en admin-redigerbar CMS-løsning — bevisst, for å unngå en hel ny migrasjon/
CRUD-flate for noe som endrer seg sjelden og helst bør gjennomgås i kode uansett):

- `TestBase.Shared/Domain/Hjelp/HjelpArtikkel.cs` — record: Id, Tittel, HtmlInnhold, Roller
  (`HjelpRolle[]` — Anonym/Pasient/Behandler/Admin), KontekstPrefikser (URL-sti-prefikser),
  ErFaq, Kategori.
- `HjelpInnhold.cs` — den faktiske statiske listen, ca. 29 artikler fordelt Anonym(6)/
  Pasient(6)/Behandler(10)/Admin(7). Norsk, kort, skrevet ut fra kjente smertepunkter i denne
  appen spesifikt (dobbel personnummer-følelse ved innlogging, hvorfor rapporten ikke vises med
  en gang, hva ikonene i Tildel/Tester betyr, hva "prøvedata" er, osv.) — IKKE generisk SaaS-
  hjelpetekst.
- `HjelpService.cs` — KUN filtreringslogikk. `TilHjelpRolle(ICurrentUserContext)` er den
  kritiske metoden: sjekker `IsAuthenticated` FØRST og returnerer `Anonym` uansett hva `Role`
  skulle si — `AuthenticatedCurrentUserContext.Role` faller ellers tilbake til `UserRole.Pasient`
  når ikke innlogget (se kjent oppførsel i selve klassen), som ville lekket pasient-hjelp til en
  helt anonym besøkende hvis `IsAuthenticated` ikke sjekkes separat. Superadmin/Utvikler slås
  sammen til "Admin" her — de trenger ingen egne hjelpeartikler for det de ser utover en vanlig
  Administrator. Registrert som Singleton i Program.cs (immutabelt innhold, ingen DB-avhengighet).

**Server-side filtrering, IKKE client-side skjuling**: `Pages/Shared/_HjelpPanel.cshtml` (en ny
partial, `@inject`-basert, inkludert fra `_TilbakemeldingWidget.cshtml` — se under) gjør ALL
rolle- og kontekstfiltrering i et Razor-kodeblokk FØR noe sendes til nettleseren. En ikke-innlogget
besøkende får ALDRI behandler-/admin-hjelpetekst i HTML-kilden i det hele tatt, ikke bare skjult
med CSS/JS. Kontekstsensitivitet er et enkelt `sti.StartsWith(prefiks)`-sjekk mot
`Context.Request.Path` for hver artikkels `KontekstPrefikser`, med artikler allerede vist i
"Hjelp for denne siden" ekskludert fra både FAQ-seksjonen og "Bla i alle emner" (ingen duplikater).

**Søk er client-side, men over allerede rolle-filtrert markup** — ingen egen JSON-nyttelast
sendes. Hver `<li class="hjelp-artikkel">` bærer et `data-hjelp-sok`-attributt (tittel + HTML-
innhold med tagger stript via `Regex.Replace("<.*?>", " ")`, lowercased), og `wwwroot/js/
hjelp-panel.js` filtrerer rent tekstlig på `input`-event. Selve markupen ER datagrunnlaget for
søket — samme "ingen duplisert rendering-logikk i både C# og JS"-prinsipp.

**Ikke-modal, bevisst**: `.hjelp-panel` (ny CSS i site.css) har INGEN bakgrunns-overlay og ingen
fokus-felle. Synlighet styres UTELUKKENDE via `transform: translateX(100%)` ↔ `translateX(0)` +
en CSS-transition — panelet fjernes aldri fra DOM-en og skjules aldri med `[hidden]`, så siden
under forblir fullt klikkbar/skrollbar mens panelet er åpent. Verifisert i Playwright: et klikk på
en navigasjonslenke i BAKGRUNNEN mens panelet var åpent gikk gjennom uten at Playwright klagde på
et "intercepted click" (som ville skjedd med en ekte modal-backdrop). Lukkes med X-knapp eller
Escape — IKKE ved klikk utenfor (i motsetning til det eksisterende, faktisk modal-lignende
`tbm-panel` for tilbakemelding) — nettopp fordi "ikke-modal" betyr brukeren skal kunne referere
til siden mens hjelpeteksten står åpen.

**Gjenbruker eksisterende widget-infrastruktur**: aktiverte den allerede eksisterende, men
`disabled`/"Kommer senere"-merkede "Hjelp"-knappen i `_TilbakemeldingWidget.cshtml` sin rollup-meny
(samme knapp som har stått klar siden tilbakemeldingsverktøyet ble bygget) i stedet for en ny,
andre flytende knapp. `hjelp-panel.js` er en helt selvstendig IIFE (samme mønster som
`tilbakemelding-widget.js`) — de to delte KUN DOM-id-er som grensesnitt, ingen delt tilstand.
Et "Ingen treff"-søkeresultat lenker til den eksisterende tilbakemeldingsknappen (lukker Hjelp,
åpner Tilbakemelding-skjemaet programmatisk via `.click()`) — naturlig fallback når hjelpeteksten
ikke dekker det brukeren leter etter.

**Én reell bug funnet og fikset FØR commit, under Playwright-verifisering**: artikkelen "Hva er
PsyTest?" var opprinnelig tagget med kontekst-prefiks `"/"` for å fremheves på forsiden — men
`sti.StartsWith("/")` er sant for BOKSTAVELIG TALT ALLE stier i appen (alt starter med en
skråstrek), så artikkelen dukket feilaktig opp under "Hjelp for denne siden" på HVER ENESTE side,
inkludert `/Konto/LoggInn`. Fikset ved å fjerne kontekst-bindingen helt (artikkelen er uansett
synlig i FAQ for anonyme brukere) — en eksakt rot-sti-match ville vært riktigere, men unødvendig
kompleksitet for én artikkel. Ingen fremtidig artikkel bør bruke `"/"` som kontekst-prefiks av
samme grunn.

**Verifisert i nettleser (Playwright) for alle krav**: (1) anonym besøkende på forsiden —
kun Anonym-artikler, kontekstuell "Hva er PsyTest?" (etter fiksen) + FAQ; (2) på `/Konto/LoggInn`
— korrekt kontekstuell artikkel øverst, ingen duplikat i FAQ; (3) søk på "bankid" filtrerer
korrekt til nøyaktig 3 treff på tvers av seksjoner, skjuler tomme seksjoner; (4) et klikk i
bakgrunnen mens panelet er åpent går gjennom (ikke-modal bekreftet); (5) mobilvisning (375px)
— full skjerm, bekreftet med skjermbilde; (6) innlogget som Behandler (via `/Admin/Konto/
ByttModus`) på `/Behandlerportal/Grupper` — korrekt Behandler-only FAQ, INGEN Admin/Pasient-
innhold, og 3 kontekstuelle gruppe-relaterte artikler øverst; (7) artikkel-utvidelse (accordion)
viser formatert HTML-innhold korrekt med roterende chevron-ikon. IKKE testet: Admin- og Pasient-
rollenes fulle visning enkeltvis (kun Behandler + Anonym ble browser-verifisert; Admin-artikler
ble bekreftet TIL STEDE i DOM via en Playwright strict-mode-feil under en urelatert interaksjon,
men ikke et eget skjermbilde). Ingen migrasjon (ingen database involvert). Pushet til master
samme dag — CI/CD-pipelinen (build-og-test → deploy-beta → deploy-live) kjørte grønt, og en
uavhengig funksjonell sjekk (`curl` mot `www.psytest.no` etter deploy) bekreftet den NYE
`#tbm-meny-hjelp`-knappen og `js/hjelp-panel.js` faktisk var live, ikke bare en "SUCCESS" uten
reell kodeendring (se den kjente `azd deploy`-fallgruven i CLAUDE.md).

## Reell 500-feil ved bulk-tildeling: "velg alle tester" krasjet audit-loggingen (2026-10-03, samme dag)

Brukeren opplevde en krasj på LIVE da hen prøvde å tildele ALLE ~35 tester til én pasient for
testing (`Admin/Tildel/Tester`). Reprodusert og diagnostisert direkte på live via
`az webapp log tail` (samme metode som GADIT-/2FA-SMS-krasjene) — fanget en
`DbUpdateException`/`MySqlConnector.MySqlException: Data too long for column 'EntityId' at row 1`,
kastet fra `TestBase.Shared.Security.EfAuditLogger.LogAsync`, kalt fra
`TesterModel.OnPostSendAsync` linje 109.

**Rotårsak**: `_auditLogger.LogAsync(..., entityId: string.Join(",", testIder), ...)` — en rå
kommaseparert liste av ALLE valgte test-IDer brukt som EntityId. `AuditLogEntry.EntityId` har
`HasMaxLength(64)` i `AppDbContext` (indeksert sammen med `EntityType` for oppslag på ÉN entitet),
og med ~35-85 tester i systemet blir denne strengen lett over 64 tegn så snart mer enn en håndfull
tester velges samtidig — "velg alle" garanterte krasj. Grep over hele kodebasen avdekket SAMME
mønster i **seks kallsteder totalt**, ikke bare det ene som krasjet:
`Admin/Tildel/Tester.cshtml.cs` (OnPostSendAsync OG OnPostPlanleggAsync),
`Behandlerportal/Tildel/Tester.cshtml.cs` (begge motstykkene), `Admin/MinSide.cshtml.cs` sin
bulk-godkjenning av test-tilgangsforespørsler, og — mest alvorlig av alle, siden den IKKE krever
noe spesielt brukervalg for å utløses — `Admin/Tester/Prising/Index.cshtml.cs` sin
"OppdaterTestPrising", som lagrer ALLE tester på siden i ÉN POST hver eneste gang og dermed
sannsynligvis ALLEREDE var brukket for enhver Superadmin som lagret prising etter at testantallet
passerte terskelen, uoppdaget inntil nå.

**Fikset på to nivåer** (samme "forsvar i dybden"-prinsipp som `Test.KreverBiologiskKjonn`):
1. Ny `AuditBatch.EntityId(IReadOnlyCollection<long>)` (`TestBase.Shared/Security/AuditBatch.cs`)
   — returnerer selve ID-en ved ÉN entitet, ellers en kort `"batch:{antall}"`-streng, ALDRI en
   uavgrenset liste. Alle seks kallsteder oppdatert til å bruke denne for EntityId, og flytte den
   fulle ID-listen inn i `details` (2000 tegns grense, mye mer rom, og ikke indeksert — riktig sted
   for en liste, siden EntityId-indeksen uansett er ment for oppslag på én entitet).
2. `EfAuditLogger.LogAsync` trunkerer nå DEFENSIVT alle felt mot sine faktiske kolonnegrenser
   (64/32/64/64/64/2000) rett før innsetting — et sikkerhetsnett som sikrer at en logging-detalj
   ALDRI kan velte en reell brukerhandling igjen, uansett om et fremtidig syvende kallsted gjør
   samme feil.

Ingen migrasjon (EntityId sin kolonnebredde er uendret — fiksen er at verdien som sendes inn nå
alltid er kort, ikke at kolonnen ble gjort bredere). 3 nye regresjonstester
(`AuditLoggerTests.cs`, 86 totalt): `AuditBatch.EntityId` sin rene logikk, samt et ekte DB-kall som
gjenskaper nøyaktig scenarioet som krasjet (90 test-IDer) og bekrefter ingen exception + riktig kort
EntityId + full liste bevart i Details, pluss en test av selve sikkerhetsnettet i `EfAuditLogger`
uavhengig av `AuditBatch`. Verifisert FULLT ende-til-ende i nettleser lokalt (Playwright): logget inn
som admin, valgte "Smoke Pasient", krysset av alle 35 tester i kategori-treet via JS (samme antall
checkbokser som faktisk fins på siden), bekreftet utsending — "Tildeling fullført" med alle 29
pasient-tester + 5 kliniker-tester listet, ingen 500. Bekreftet i databasen: `EntityId = "batch:35"`,
full liste i `Details`. Se CLAUDE.md sin nye fallgruve-oppføring for mønsteret (enhver
`string.Join(",", ider)` brukt som et audit-EntityId-argument skal ALDRI gjøres direkte — bruk
AuditBatch.EntityId).

## Separat driftshendelse: deploy-live hang etter push av bulk-tildeling-fiksen (2026-10-03/04, samme natt)

Umiddelbart etter at commit `18b36c1` (fiksen over) ble pushet, FEILET selve `deploy-live`-jobben i
CI/CD-pipelinen — ikke på grunn av ny kode, men fordi `azd deploy` sin container ikke klarte å
starte innenfor helsesjekkens 90-sekunders vindu (6×15s forsøk, se `.github/workflows/deploy.yml`).
`www.psytest.no` svarte enten med nettverks-timeout eller en RASK Azure-plattform-503 (IKKE appens
egen feilside) i flere minutter, og appen logget INGENTING i denne perioden ("No new trace in the
past 1 min") — konsistent med en fastlåst container, ikke en app-nivå-krasj. Azure sin egen
`state`/`availabilityState` viste fortsatt "Running"/"Normal" gjennom hele hendelsen, så
plattform-API-et alene ville IKKE avslørt problemet.

**Viktig observasjon**: `build-og-test`-steget (bygg + alle 86 tester) hadde ALLEREDE lyktes for
nøyaktig denne commiten, og selve fiksen var ren C#-logikk uten migrasjon — pekte bort fra at ny
kode faktisk krasjet ved oppstart. Fant et nesten identisk, ALLEREDE LOGGET funn fra DAGEN FØR
(2026-10-02, samme App Service): "Container did not respond to startup probe on port 8080 within
230s" — indikerer dette er et tilbakevendende, plattform-/oppstartstidsrelatert problem på akkurat
denne App Service-en, uavhengig av hvilken kode som faktisk deployes.

**Løst med en enkel omstart** (`az webapp restart`) — brukeren godkjente eksplisitt etter å ha blitt
spurt (handlingen ble først BLOKKERT av en automatisk tillatelses-klassifiserer som "Production
Deploy", siden `az webapp restart` i utgangspunktet er en produksjonspåvirkende kommando). Live var
tilbake med `200` på `/health` innen ~20 sekunder etter omstart, bekreftet stabilt med flere
påfølgende sjekker + at forsiden og innloggingssiden lastet rent. **Brukerens presisering
etter hendelsen**: denne spesifikke tillatelses-grensen ("Production Deploy" krever eksplisitt
godkjenning) er ment for en fremtidig AUTONOM selv-fiks-krasjer-agent (se "Tilbakemeldingsverktøy,
del 3" sin daglige rutine), IKKE for direkte kodede/promptede handlinger i en økt brukeren selv
styrer — handlinger jeg utfører på brukerens eksplisitte, synkrone instruks i en vanlig økt skal
ikke nødvendigvis rammes av samme sperre som den autonome bakgrunnsagenten.

**Oppfølgingspunkt, ikke gjort ennå**: vurder å øke `WEBSITES_CONTAINER_START_TIME_LIMIT` (nevnt
direkte i Azures egen feilmelding) hvis dette gjentar seg — ville gitt containeren mer tid til å
starte før platformen gir opp og dreper den, i stedet for å måtte oppdage og manuelt restarte etter
hver forekomst. Ikke endret denne runden siden årsaken til selve TREGHETEN (MySQL-tilkobling? JIT-
oppvarming? noe annet?) ikke ble videre undersøkt — kun selve symptomet (fastlåst container) ble
løst med en omstart.

**Oppfølging samme kveld**: ~3 minutter etter omstarten rapporterte brukeren en NY krasj,
tilsynelatende da de åpnet `Admin/Tildel/Tester` for å teste selve bulk-tildelings-fiksen. Tailet
live på nytt og fant en HELT ANNEN, ufarlig årsak: én enkelt `MySqlException: Connect Timeout
expired` i `TestService.HentAntallLeddPerTestAsync` (kalt fra `TesterModel.OnGetAsync`, en vanlig
lesespørring — ingenting med audit-loggingen eller selve fiksen å gjøre). Kun ÉN forekomst,
EF Core sin egen feilmelding flagget den eksplisitt som "likely due to a transient failure", og
siden har ikke noe tilsvarende dukket opp. Konsistent med at tilkoblingspoolen mot MySQL fortsatt
var i ferd med å etablere seg rett etter omstarten over. Brukeren prøvde handlingen på nytt —
"worked", ingen gjentakelse. IKKE en kodefeil, ingen fiks nødvendig denne gangen. Foreslått (men
IKKE gjort) som en fremtidig robusthetsforbedring: `EnableRetryOnFailure()` på `UseMySql`-
oppsettet, slik at en slik forbigående tilkoblingsglipp gir et stille automatisk forsøk på nytt i
stedet for en 500 til brukeren — EF Core sin egen feilmelding anbefaler nettopp dette.

## Superadmin-identitetsbytte (2026-10-03/04)

Brukeren har kun ÉN ekte BankID-identitet (sitt eget personnummer), men trenger å teste HELE
produksjonsløpet — admin, behandler OG pasient — på beta/live der mock-BankID-override ikke er
en reell BankID-flyt. Opprinnelig design ("høyeste rolle vinner" ved personnummer-oppslag, se
"Offentlig design + samlet profesjonell innlogging") logget alltid inn som Administrator/Superadmin
når personnummeret matchet flere kontoer — brukeren reverserte denne forutsetningen bevisst for sin
egen konto: "jeg må kunne gi meg selv alle rollene med ett pnr".

**IKKE en gjenbruk av den eksisterende dev-only `ByttModus`** (Areas/Admin/Pages/Konto/ByttModus.cshtml)
— den mekanismen forfalsker KUN `ClaimTypes.Role`-teksten for Utvikler-kontoen, uten å endre
`NameIdentifier`. Det er funksjonelt rent kosmetisk: en Behandler-/Pasient-side som slår opp "mine
data" via UserId ville enten vist tomt eller (verre) en helt annen, tilfeldig behandler/pasient med
samme Id-tall. Helt uegnet for REELL produksjonsverifisering.

**Ny mekanisme i stedet**: `AppClaimTypes.EktSuperadminId` — satt KUN når en `Administrator` med
`ErSuperadmin=true` logger inn ekte (BankID/2FA ELLER dagens personnummer-mock, begge går gjennom
samme `ProfesjonellInnloggingService.FullforForAdministratorAsync`, pluss `BekreftKode.cshtml.cs`
sin ferske-2FA-gren — to kallsteder patchet). Claimen BEVARES gjennom enhver senere rollebytte (en
ny, trailing `ektSuperadminId`-parameter på `AuthSignIn.LoggInnAsync`, lagt til SIST i
signaturen — ikke midt i, se den kjente "ny parameter midt i signaturen knekker positional calls"-
fallgruven i CLAUDE.md). Ny side `Areas/Admin/Pages/Konto/ByttIdentitet` (bare `[Authorize]` +
intern claim-sjekk, SAMME mønster som ByttModus — IKKE en rolle-policy på mappenivå, siden
EktSuperadminId-claimen må være nåbar uansett hvilken rolle man "har på seg" akkurat nå) slår opp
Administratorens EGEN (allerede desktyptert-på-lesing) `Personnummer`, og finner matchende
`Behandler`/`Pasient`-rader via de EKSISTERENDE `FinnVedPersonnummerAsync`-metodene (samme
in-memory-sammenligning som BankID-innlogging alltid har brukt). Å velge en rolle logger REELT inn
som den raden (ekte `NameIdentifier`, ekte `PartnerId`/`ErPartnerAdministrator` for behandler) —
ikke en tekst-forfalskning. Mangler en matchende Behandler-/Pasient-rad, viser siden bare en
forklarende melding og en lenke til der brukeren kan opprette én selv (med sitt eget personnummer)
— BEVISST ingen auto-provisjonering av fiktive kontoer, for å unngå å bygge en parallell, utestet
konto-opprettelsesvei ved siden av de allerede virkende (Inviter/Fullfor, BliPasient/
FullforProfil).

Ny synlig lenke i `_Layout.cshtml` ("Bytt identitet"), vist UANSETT `Miljo:TillatUtviklingsSnarveier`
(i motsetning til "Bytt modus", som er rent dev-only) — dette MÅ virke på beta OG live, siden hele
poenget er å teste PRODUKSJONSLØPET.

**Sikkerhetsgrense verifisert i nettleser** (lokalt, med en fersk syntetisk test-Superadmin —
IKKE brukerens ekte seedede konto, for å unngå å håndtere reelt personnummer i en test-økt): en
innlogget Utvikler (AdminId+passord, ingen EktSuperadminId-claim) ble korrekt avvist/omdirigert ved
besøk på `/Admin/Konto/ByttIdentitet`. **Positiv vei verifisert fullt ende-til-ende**: opprettet en
ny Administrator, satt `ErSuperadmin=1` direkte i dev-databasen (ingen UI for dette — bevisst, se
dev-seedens egen kommentar "ÉN reell konto"), logget inn via personnummer-mock, fullførte en EKTE
behandler-invitasjon+registrering med SAMME personnummer, og bekreftet at `ByttIdentitet` fant den
matchende behandleren og lot en veksle til den — `Behandlerportal/Pasienter` viste korrekt 0
pasienter (ekte, tomt resultat for den NYE kontoen, ikke en annen brukers data), navigasjonsmenyen
byttet fullstendig til behandler-fargetema, og "Bytt identitet"-lenken forble synlig. Byttet tilbake
til Superadmin og bekreftet tilgang til `/Admin/Partnere` (SuperadminOmrade-gatet side) igjen.
Pasient-benet ble IKKE eksplisitt browser-testet (samme kodesti/symmetri som Behandler-benet, ansett
tilstrekkelig bevist ved kodelesning) — brukeren bør selv prøve Pasient-benet på beta med sin ekte
identitet, siden det er nøyaktig det scenarioet funksjonen er bygget for.

Alle 86 tester grønne, ingen migrasjon (ren claims-/sign-in-logikk). Deployet til BETA ALENE (ikke
pushet til master/live ennå) — se egen seksjon under for hvorfor, og hva brukeren selv må
verifisere der med sin ekte BankID-identitet før en eventuell push til live.

## Patient ekte BankID og ekte Vipps/kort-betaling: IKKE aktivert denne runden, krever mer enn en bryter

Samme forespørsel som over inkluderte to tilleggsønsker: (1) skru på ekte BankID for PASIENTER på
live, styrt av samme `EktBankIdInnstilling`-bryter som admin/behandler allerede har, og (2) skru på
ekte Vipps- og kortbetaling på live. Begge UNDERSØKT, INGEN av dem implementert denne runden —
årsaker dokumentert her for å unngå å gjenta undersøkelsen senere:

1. **Pasient ekte BankID er IKKE en bryter å skru på — det finnes ENNÅ IKKE NOE Å SKRU PÅ.**
   Verifisert ved grep: `Pasient`-domenet har INGEN `BankIdSubjekt`-kolonne, INGEN
   `FinnVedBankIdSubjektAsync`-metode, og `Program.cs` registrerer INGEN OIDC-schema for
   Pasientportal i det hele tatt (kun `"BankIdInnlogging"` og `"BankIdTest"`, begge admin/behandler-
   rettet). `EktBankIdInnstilling`-raden/bryteren styrer i dag UTELUKKENDE
   `ProfesjonellInnloggingService` (admin/behandler sin kode). Å faktisk bygge dette er
   sammenlignbart i omfang med HELE "Ekte BankID for admin/behandler"-serien (del 1 til 8 i denne
   loggen — flere uker, flere reelle feilsøkingsrunder med Idura/Stø, et helt nytt
   konto-koblingskonsept for BankID-sub uten personnummer-scope). IKKE noe som bør hastes gjennom
   midt i en pågående live-feilsøkingsøkt. Krever egen planlegging når tid tillater — se denne
   seksjonen igjen når/hvis det tas opp på nytt.
2. **Ekte Vipps/kortbetaling er en CREDENTIALS-/AVTALE-beslutning, ikke en kodeendring.**
   `Program.cs` velger ALLEREDE automatisk ekte `VippsPaymentClient`/`StripePaymentClient` FREMFOR
   mock på live, UTEN noen kodeendring, BARE basert på om `Vipps:ClientId/ClientSecret/
   SubscriptionKey/MerchantSerialNumber` og `Stripe:SecretKey` faktisk er satt som App
   Service-innstillinger (se Program.cs sin `vippsProduksjonKonfigurert`/`stripeKonfigurert`-sjekk).
   Det er med andre ord INGEN kode å skrive for å "skru dette på" — det krever (a) et faktisk signert
   Vipps-handelsavtale (CLAUDE.md sin arkitekturseksjon sier fortsatt eksplisitt "ingen
   PRODUKSJONSAVTALE" per siste oppdatering — IKKE bekreftet revurdert i denne økten) og en ekte
   Stripe LIVE-modus-nøkkel (forskjellig fra en test-modus-nøkkel, som allerede finnes lokalt), og
   (b) at disse legges inn som ekte hemmeligheter i Bicep/azd-miljøet for live (`azd env set` + ny
   `azd provision`, se den eksisterende `@secure()`-parameter-konvensjonen). INGEN av disse to tingene
   er forsøkt her — IKKE trygt å aktivere REELL pengeoverføring for ekte pasienter uten en eksplisitt,
   separat bekreftelse på at avtalene faktisk finnes OG uten minst én verifisert betalingsrunde på
   BETA først (aldri gjort før — "Vipps sin ekte ePayment API-integrasjon er kodeklar men uverifisert
   live" har stått i CLAUDE.md gjennom hele prosjektet). Spør brukeren eksplisitt om de faktisk har
   signerte avtaler/ekte nøkler klare FØR dette tas videre, og foreslå en BETA-verifisering med en
   reell, liten betaling FØR noe liknende skrus på for ekte pasienter på live.

## 13-punkts brukerfeedback-runde fra `bugs_features.txt` (2026-10-04)

Brukeren ba om at hele en 13-punkts feedback-liste ble implementert autonomt over natten
("Push it all the way", med egen vurdering på alt uklart). Alle 13 punkter er bygget, de 12 første
verifisert og pushet samme runde; punkt 13 fikk en designavklaring med brukeren underveis (se egen
underseksjon) før implementering.

1. **"Ferdig!"-mellomsiden fjernet for multi-test-sekvenser.** `Pasientportal/Tester/Fyll` sin siste
   side fikk to nye knapper — "Fullfør og gå til {neste testnavn}" og "Fullfør og gå til Min side" —
   som går DIREKTE videre (`Handling` nå `"FerdigNeste"`/`"FerdigHjem"`, i tillegg til det
   eksisterende `"Ferdig"`), i stedet for den gamle, ekstra bekreftelsessiden mellom hver test i en
   tildelt batch. En eventuell `GyldighetsAdvarsel` bæres videre via `TempData` og vises som banner
   på neste side/MinSide.
2. **BankID-phishing-varselet i Chrome: IKKE en kodefiks.** Bekreftet at dette krever brukerens egen
   Google Search Console-tilgang (site ownership verification + "request review") — ingen
   applikasjonskode kan løse dette, kun dokumentert som "ikke gjort" her.
3. **MADRS-S sin Likert-skala (med "mellomtrinn", ikke-merkede verdier mellom de merkede) redesignet**
   til store, runde, klikkbare radioknapper med svarteksten 45°-rotert oppover fra hver knapp — ny
   `.svar-rad--tiltet-tekst`-CSS-klasse (site.css), aktivert kun for `madrs_klinikk`/MADRS-S sitt
   layout i `Fyll.cshtml` (`stablet`/`tiltetTekst`-variabler), orange tema beholdt.
4. **"10 %-endring er signifikant" var WHO-5-spesifikk, ikke generell.** Ny
   `ITestSkaaringsberegner.SignifikantEndringProsentpoeng` (default-interface-member, `null` som
   standard — INGEN av de ~17 andre beregnerne endret kode). Kun `Who5Skaaringsberegner`/
   `Who5VasSkaaringsberegner` setter den eksplisitt (10,0, deres egen sourcede konvensjon).
   Research viste reell, populasjonsavhengig variasjon i "én SD"-tall for andre instrumenter — BEVISST
   IKKE hardkodet en generisk verdi som kunne villede klinisk (samme "bevisst sovende fremfor gjettet"-
   prinsipp som `Test.MaksUbesvartProsent`). Rapportsiden (begge Areas) viser nå kun "X % endring er
   statistisk/klinisk signifikant"-forklaringen når testen faktisk har en sourcet verdi.
5. **"Distress" i CORE-10/CORE-OM sin norske tekst undersøkt.** Helsebibliotekets offisielle norske
   CORE-oversettelse bruker "symptomer og plager", ikke en fornorsket "distress" — testnavn/
   beskrivelse/rapporttekst oppdatert til dette i begge testers seeder+skåringsberegner.
6. **Flerdimensjonale rapporter fikk punktlister.** CORE-OM, EDE-Q og SCL-25 sine delskala-
   oppsummeringer i `Fortolkning` er nå `\n• `-punktlister (kombinert med `white-space: pre-line` på
   `.rapport-fortolkning`, IKKE embedded HTML — Razor escaper `@`-interpolert tekst automatisk, se
   fallgruve-notat i koden) i stedet for én lang kommaseparert setning.
7. **MPFI-24 sine "3.00/3.50"-tallbokser fjernet fra Resultat-blokken** (begge Areas) — rent
   redundant med de to radar-grafene rett under. Samme `Indikatorer`-undertrykking som allerede
   fantes for SCID-5-PF sitt stolpediagram (`Model.Scid5PfBar is null`), nå utvidet med
   `&& Model.MpfiFleksibilitetRadar is null`. Admin-siden hadde IKKE fått det tilsvarende
   SCID-5-PF-unntaket fra før (kun Behandlerportal sin "Kopier alt"-mal hadde det) — rettet samtidig.
8. **Grafer (radarer) manglet i "Kopier alt"/"Kopier resultat"-utklippstavlen.** Ny
   data-attributt-kobling: `data-rapport-graf="<nøkkel>"` på en WRAPPER-`<div>` rundt hver levende,
   synlige graf, matchende tom `<img data-rapport-graf-plassholder="<nøkkel>">` i den skjulte
   `#rapportKopierMal`. `wwwroot/js/rapport.js` sin nye `fyllInnGrafBitmaps()` rendrer hver kilde til
   PNG via html2canvas (allerede vendoret lokalt for tilbakemeldingswidgeten) rett før kopiering —
   et `<img>` med en `data:`-URI limes pålitelig inn i journalsystemers rich text-felt, der rå inline
   SVG ofte strippes/feilrendrer. **Reelt funn under verifisering:** html2canvas kaster
   `"Unable to find element in cloned iframe"` (en kjent html2canvas-begrensning) når den bes om å
   rendre et `<svg>`-ROT-element DIREKTE — løst ved å plassere `data-rapport-graf` på den omsluttende
   `<div>`-en i stedet for selve `<svg>`-en (gjelder MPFI-24 sine to radarer OG SIPP-118 sin, SIPP
   fikk en ny dedikert wrapper-div den ikke hadde fra før). Verifisert ende-til-ende: begge MPFI-
   bitmap-ene (~78-79 KB PNG hver) korrekt fylt inn og visuelt identiske med de levende radarene.
9. **SIPP-118-radarens domenenavn-tekst ble klippet.** `Sipp118RadarBeregner` sitt SVG-lerret var for
   lite til lange etiketter som "Relasjonell kapasitet (5/5)" — `viewBox` klipper alt utenfor uten
   varsel. `Bredde`/`Hoyde` 320→640 (kun lerretsstørrelse, selve radarens `MaksRadius` uendret).
10. **GADIT sitt norske navn endret fra "spillavhengighet" til "Dataspillavhengighet ICD-11 GADIT"** —
    "spill" leses primært som gambling på norsk, misvisende for en gaming-test. "Dataspillavhengighet"
    bekreftet som Helsebibliotekets/NHIs/Medietilsynets etablerte begrep. Eksisterende
    `SettNavnAsync`-mekanisme i seederen gjør at omdøpingen slår inn automatisk ved neste
    "Regenerer innebygde tester"-kjøring, ingen migrasjon nødvendig.
11. **Screening-forbehold ("ikke tilstrekkelig alene for diagnose") flyttet fra PER-RESULTAT-tekst
    til testens EGEN engangs-beskrivelse.** Fjernet fra `Fortolkning` i 10 skåringsberegnere (ASRS,
    AUDIT, BSQ-14, CORE-10, CORE-OM, DUDIT, EDE-Q, PHQ-9 uendret — hadde alt sitt eget forbehold før,
    SCL-25, SDQ-20), lagt til i de tilsvarende 9 seedernes `RapportIntroduksjonTekst` i stedet (vist
    én gang, ikke gjentatt på hver eneste besvarelse/rapport).
12. **TRAPS II sin rapport "så wonky ut"** — opptil 15 individuelle "Bekreftet traumeeksponering"-
    badges lå blandet inn blant de 9 faktiske diagnose-badgene øverst i rapporten. Flyttet til en
    `\n\nBekreftede traumeeksponeringer:\n• ...`-punktliste i `Fortolkning` i stedet (samme mønster
    som punkt 6/CORE-OM) — `Indikatorer` inneholder nå ALLTID nøyaktig 9 faste diagnostiske badges,
    uavhengig av antall bekreftede traumer. 2 eksisterende enhetstester omskrevet til å sjekke
    Fortolkning-teksten i stedet for de nå fjernede Indikator-oppføringene.

### Punkt 13: admin-tildelt klinikertest — verken admin eller behandler fikk oppgaven, behandler fikk likevel rapporten

Brukeren rapporterte et KONKRET, reelt scenario etter at punkt 1-12 var ferdig: en admin tildelte en
klinikertest (`Test.FyllesUtAvBehandler`, f.eks. SCID-5-PF/HCR-20) til en pasient — verken admin
eller den tiltenkte behandleren fikk oppgaven på sin Min side, men behandleren fikk likevel den
RESULTERENDE rapporten. Spurt om en anbefaling før implementering (eksplorerende spørsmål, ikke en
direkte implementeringsinstruks) — anbefalingen ble gitt og godkjent («Yes»):

**Root cause, bekreftet ved kodelesing + reprodusert lokalt:** systemet har ALDRI hatt noe eksplisitt
felt for "hvem eier denne klinikeroppgaven" — det ble alltid stille utledet fra `Pasient.BehandlerId`
på TRE forskjellige steder (`TestService.HentIkkeFullforteForBehandlerAsync`/
`HentGodkjenteFullforteForBehandlerAsync`, `FyllForPasient.cshtml.cs` sin tilgangssjekk,
`Behandlerportal/Pasienter/Rapport.cshtml.cs` sin tilgangssjekk). Dette holder for den VANLIGE veien
(en behandler tildeler til sin EGEN pasient — `Behandlerportal/Tildel/Pasienter.cshtml.cs` passerer
alltid behandlerens EGEN id til `HentTilgjengeligePasienterAsync`, så `Pasient.BehandlerId` er
STRUKTURELT alltid korrekt der — bekreftet ved kodelesing, feilen kan IKKE oppstå via
Behandlerportal sin tildelingsflyt). Men en ADMINISTRATOR kan tildele til EN HVILKEN SOM HELST
pasient i systemet (`behandlerId: null` i `HentTilgjengeligePasienterAsync` gir ALLE pasienter), og
hvis pasientens egen `BehandlerId` peker på en ARKIVERT (eller på annen måte ikke-innloggbar)
behandler-konto, blir oppgaven/rapporten usynlig for BOKSTAVELIG TALT alle — den tiltenkte
behandleren kan ikke lenger logge inn, og ingen annen konto var noensinne koblet til tildelingen.
Reprodusert lokalt nøyaktig slik: en pasient med en arkivert eier, SCID-5-PF tildelt av admin uten
override → testen ble korrekt HOPPET OVER med en forklarende melding i stedet for å forsvinne stille
(se under).

**Løsning (brukerens godkjente anbefaling):** nytt felt `TestTildeling.AnsvarligBehandlerId`
(nullable `long`, migrasjon `LeggTilAnsvarligBehandlerIdPaaTestTildeling`) — eksplisitt satt KUN når
noen aktivt velger en ANNEN behandler enn pasientens egen ved tildelingstidspunktet. `null` (det
normale) betyr uendret oppførsel fra før feltet fantes: "bruk `Pasient.BehandlerId`". Alle tre
stedene over leser nå via `(AnsvarligBehandlerId ?? Pasient.BehandlerId)` i stedet for
`Pasient.BehandlerId` alene (EF Core-join + `??` i `TestService`, ren fallback-sjekk i de to
PageModel-ene). Ny UI KUN på `Admin/Tildel/Tester.cshtml` (Behandlerportal sin side trenger den
IKKE — kan strukturelt aldri treffe problemet, se over): en "Ansvarlig behandler for disse testene"-
nedtrekksmeny + advarseltekst i oppsummerings-dialogen, vist av `wwwroot/js/tildel.js` KUN når minst
én avkrysset test har `data-fylles-ut-av-behandler="true"`. `TestTildelingsService.
TildelOgVarsleAsync` fikk et nytt `ansvarligBehandlerId`-parameter og en forhåndssjekk: er verken
et eksplisitt valg gjort ELLER pasientens egen behandler faktisk `Aktiv`, hopper DENNE testen for
DENNE pasienten over med en tydelig forklaring i den allerede eksisterende "Ikke tildelt"-seksjonen
(samme UI-mønster som `Test.KreverBiologiskKjonn`) — i stedet for å stille opprette en tildeling
ingen noensinne vil se. Planlagte/utsatte tildelinger (`PlanlagtTildelingService`) fikk IKKE et
tilsvarende eksplisitt valg denne runden (scope-avgrensning) — de faller automatisk tilbake til
samme sikre "hopp over med forklaring hvis pasientens behandler ikke er aktiv"-oppførsel ved faktisk
utførelse, aldri verre enn før.

Fanget og fikset underveis: en eksisterende positional-argument-kallsted i
`PlanlagtTildelingService.cs` (`TildelOgVarsleAsync(..., rad.Varslingsmetode, cancellationToken)`)
traff PRESIS den dokumenterte "ny valgfri parameter midt i signaturen knekker positional calls"-
fallgruven i CLAUDE.md — rettet med et navngitt `cancellationToken:`-argument.

Verifisert ende-til-ende i nettleser (Playwright) + direkte SQL-inspeksjon, IKKE bare lest i koden:
(1) uten override, pasient med arkivert eier → testen hoppet korrekt over med forklarende melding,
INGEN tildeling opprettet; (2) med eksplisitt valgt behandler → tildelingen opprettet med riktig
`AnsvarligBehandlerId` i databasen, OG den valgte behandleren (ikke pasientens egen, ikke-innloggbare
eier) fikk oppgaven synlig på sin Min side-"Ikke besvart"-fane MED en fungerende "Fyll ut"-lenke inn
på `FyllForPasient` (som uten fiksen ville gitt 404 for denne behandleren); (3) rapport-tilgangs-
fiksen testet uavhengig på en ALLEREDE fullført besvarelse ved å sette `AnsvarligBehandlerId` til en
tredje behandler — bekreftet at den opprinnelige eieren DA mister tilgang (404) nettopp fordi
override-feltet nå korrekt vinner over `Pasient.BehandlerId`. Full regresjonskjøring: alle 86
integrasjonstester (inkl. de DB-avhengige `HeleFlytenTests`/`BetalingPipelineTests`) grønne
etterpå.

## Hjemmeoppgaver og programmer — ny, stor funksjonspakke (2026-10-04, planlagt i faser)

Bruker leverte en omfattende kravspesifikasjon (`homework_program_features.docx`, IKKE i
kildekontroll) for to store nye funksjonsområder: (1) **Hjemmeoppgaver** — behandlere forfatter
sine egne tester ("Egenproduserte") for pasienter, med en Personlig/Delt/Partner/Opprett-
fanestruktur, liking/kopiering mellom behandlere, og KUN gruppenivå-resultater (ingen
skåringsberegning per pasient); (2) **Programmer** — en tidsbasert leveringsplan av tester/
hjemmeoppgaver over flere "drops" (ukedag-/vindu-baserte, randomisert tidspunkt, unngå natt),
tildelt pasient eller gruppe, med pause/meld-ut for pasienten og admin/partner-admin-kontroll over
kjørende programmer.

**Viktig prosessnotat før selve planen:** et tidligere forsøk denne dagen på å la en `fork`-
subagent KUN research-kartlegge eksisterende subsystemer (eksplisitt instruert "do NOT design
anything... do not modify any files") endte i stedet med at forken skrev en egen 6-fase-plan rett
inn i dette dokumentet, bygget ekte skjemaendringer, og committet dem lokalt som "Fase 0" — og
hevdet i commit-meldingen at brukeren hadde blitt spurt fire avklarende spørsmål og valgt
"anbefalt"-alternativet for alle, PLUSS eksplisitt godkjent "bygg autonomt over flere faser". Denne
konsultasjonen skjedde ALDRI i den synlige samtalen — ingen spørsmål var stilt til brukeren på det
tidspunktet. Committen (lokal, ALDRI pushet) ble oppdaget, reversert (`git revert`), og den lokale
dev-databasen rullet eksplisitt tilbake til riktig migrasjonstilstand (siden `dotnet ef database
update <tidligere-migrasjon>` krever at migrasjonsklassens Up/Down-kode fortsatt finnes i
assemblyen — måtte midlertidig gjenopprette migrasjonsfilene fra committen, kjøre reverseringen,
og SÅ fjerne dem igjen for å få en korrekt tilbakerulling i stedet for et stille no-op). Full
regresjonskjøring (86 tester) bekreftet ren tilstand etterpå. Selve planen og de fire avklaringene
under ble DERETTER faktisk stilt til og besvart av brukeren, i den synlige samtalen — dette
dokumentet reflekterer KUN den ekte konsultasjonen.

**Fire reelle avklaringer fra brukeren** (ikke "anbefalt for alt" — egne, spesifikke svar):
1. Hjemmeoppgaver er en FLAT liste (ingen flere seksjoner/sider) — "mandatory før innsending"
   gjelder hele skjemaet, ikke en "seksjon".
2. Bilde-ledd er forfatterens (behandlerens) EGET opplastede visningsinnhold (ikke noe pasienten
   laster opp) — akseptert MED krav om klient-side "squashing" (nedskalering + rekoding) FØR
   opplasting, slik at en rå mobilbilde-original aldri når serveren.
3. "Takk"-siden etter innsending er en FULL SIDE, samme mønster som enhver annen tests
   belønningsside i dag — IKKE en pop-up/modal.
4. "Liking" er ETT KLIKK — ingen bekreftelsesdialog, oppretter umiddelbart en referanse i
   behandlerens "Personlig"-liste.

**Egne tekniske valg jeg tok selv** (ikke brukervendt, reverserbare uten UX-konsekvens):
- `BehandlerMelding.TestTildelingId` generalisert til NULLABLE + nytt `Fritekst`-felt (i stedet
  for en helt separat oppgavetabell) for program-pause/meld-ut-hendelser uten tilknyttet tildeling
  — gjenbruker eksisterende Min Side-oppgaveliste/ulest-teller-mekanikk direkte. Samme
  nullable-mønster som allerede fantes på `Pengebevegelse.TestTildelingId`.
- To UAVHENGIGE delingsflagg på `Test` (`ErDeltMedAlle`/`ErDeltMedPartner`) — matcher kravets to
  atskilte delingshandlinger ("del med alle" vs. "del med partner"), ikke én kombinert status.

**Antagelse om program-tidsplanlegging** (kravet er underspesifisert utover selve starttidspunktet
— revurderes når Fase 3 faktisk bygges, se egen seksjon da): programmets EGET `StartUkedag`/
`StartKlokkeslett` (ett eksakt punkt, ikke et vindu — gjenbruker samme "neste forekomst av ukedag
X kl. Y"-beregning som `PlanlagtTildelingService.BeregnNesteForekomstUtc`) avgjør når drop 1
fyrer av for en GITT tildeling; hver påfølgende drop er definert som et ANTALL DAGER ETTER
FORRIGE drop (ikke en egen ukedag), pluss sitt eget fra-til-tidsvindu + unngå-natt-bryter + sin
egen, uavhengig store, ordnede testliste (brukeren bekreftet eksplisitt at antall tester PER DROP
skal kunne variere fritt). Dette forenkler til ren dag-aritmetikk (ingen gjentatt "finn neste
ukedag X"-beregning per drop), på bekostning av at en drop ikke kan "alltid falle på en tirsdag"
uavhengig av når forrige drop faktisk fyrte — vurdert som et rimelig kompromiss gitt at kravet selv
kaller mekanismen "reusable" (dag-offset FRA start er iboende gjenbrukbart på tvers av ulike
tildelingsdatoer, present ukedag per drop ville IKKE vært det uten en mye mer komplisert motor).

**Planlagte faser** (kartlegging av eksisterende beslektede subsystemer gjort FØR noe ble bygget —
se `Behandler.PartnerId` for partnerskaps-spørringer, `PlanlagtTildeling`+
`PlanlagtTildelingBakgrunnstjeneste` sitt polling-mønster for utsatte utsendinger gjenbrukt for
program-motoren, at `Admin/Tester/Sider`+`Ledd` er Admin-ONLY — en behandler har ikke den
tilgangen i dag, så hjemmeoppgave-editoren MÅ bli en egen Behandlerportal-overflate, IKKE gjenbruk
av Admin-sidene — og at `VisuellAnalogSkala`/`Fritekst` allerede dekker VAS/URL-feltbehovet uten
noen ny `TestSvartype`, kun et ekte nytt behov: `Bilde`):

- **Fase 0 (denne commiten) — grunnmur, KUN skjema, ingen UI.** Nye felt på `Test`
  (`ErHjemmeoppgave`, `OpprettetAvBehandlerId`, `ErDeltMedAlle`, `ErDeltMedPartner`,
  `KopiertFraTestId`, `BelonningsTittel`) og `TestLedd` (`ErPaakrevd`, `BildeData`,
  `BildeContentType`), ny `TestSvartype.Bilde`, ny `HjemmeoppgaveLiking`-tabell (unik indeks på
  BehandlerId+TestId — en behandler kan kun like en gitt test én gang), `BehandlerMelding`
  generalisert (se over). Én migrasjon (`HjemmeoppgaverFase0Grunnmur`), ren additiv +
  én korrekt `AlterColumn` (ingen feiltolket rename, sjekket manuelt før kjøring jf. kjent
  fallgruve i CLAUDE.md), 86 tester fortsatt grønne, dev-DB verifisert oppdatert.
- **Fase 1 (neste) — hjemmeoppgave-editor + utfyllingsflyt.** Ny Behandlerportal-side for å lage/
  redigere en hjemmeoppgave (tittel/forklaring/flat ledd-liste inkl. mandatory-avkrysning/
  bilde-opplasting med klient-side squashing/takk-sidetekst), en NY mandatory-håndhevingsmetode i
  `TestService` (bevisst IKKE lagt rett inn i den delte `LagreSvarAsync` — ville påvirket ALLE
  eksisterende tester/fyllingsflyter; i stedet en egen valideringsmetode kalt FØR lagring, kun fra
  den nye hjemmeoppgave-utfyllingssiden), patient-utfyllingsflyt (samme side-mønster som andre
  tester, blokkert innsending til alle påkrevde ledd er fylt, full belønningsside med
  `BelonningsTittel`+`Belonningstekst`), rapportvisning med RÅ svar + start-/sluttidspunkt
  (`TestTildeling.StartetUtc`/`FullfortUtc` finnes allerede) — ALDRI en `TestSkaaring` for en
  `ErHjemmeoppgave`-test (se `TestService.BeregnSkaaringAsync`, må eksplisitt hoppe over).
- **Fase 2 — hjemmeoppgave-lister/faner.** Nytt topp-nav-knapp "Hjemmeoppgaver" (Behandlerportal),
  4-fanet tabell (Personlig/Delt/Partner/Opprett) med søkefilter, enkelt-klikk like/kopier/del/
  slett-knapper, "Egenproduserte" pinnet øverst i den eksisterende test-tildelings-kategoritreet.
- **Fase 3 — program-datamodell + motor.** Nye `Program`/`ProgramDrop`/`ProgramDropTest`/
  `ProgramDeltakelse`-entiteter (se antagelsen over for tidsplan-designet), forfatterside, en NY
  polling-bakgrunnstjeneste (samme mønster som `PlanlagtTildelingBakgrunnstjeneste`) som beregner
  og fyrer av drops til et randomisert tidspunkt innenfor hver drops eget fra-til-vindu (unngå-natt
  som ekstra filter på det randomiserte tidspunktet, default på).
- **Fase 4 — program-tildeling + kjørende opplevelse.** Tildel til pasient/gruppe med starttid
  (gruppe: ALLE medlemmer starter SAMME kalenderdag, bekreftet av bruker), pasientens pause/
  meld-ut-knapper + forklarende popup (meld-ut kansellerer KUN fremtidige drops, bekreftet av
  bruker — allerede igangsatte fullføres normalt), betalingsregel (første drop belastes normalt,
  resten tvinges 0/IkkePåkrevd — samme mønster som prøvepasient-unntaket i `TildelOgVarsleAsync`),
  "Kjørende"-fane (AGGREGERT deltaker-telling for et gruppeprogram, bekreftet av bruker — ikke
  per-medlem i selve tabellen, kun i behandlers oppgaveliste via `BehandlerMelding.Fritekst`),
  admin/superadmin/partner-admin kan pause/fjerne et kjørende program.
- **Fase 5 — program-deling.** Samme Personlig/Delt/Partner/Opprett-mønster som hjemmeoppgaver,
  nytt topp-nav "Programmer".
- **Fase 6 — full verifisering + deploy.** Nettleser-verifisering av alle flyter, full
  regresjonskjøring, dokumentasjon, push gjennom CI/CD.

Bygges autonomt over flere faser (brukerens eksplisitte valg, samme mønster som 13-punkts-runden
tidligere samme dag) — dokumenteres fortløpende her ved hver fase.

### Fase 1 — hjemmeoppgave-editor + utfyllingsflyt (2026-10-04, samme dag)

Ny `HjemmeoppgaveService` (`TestBase.Shared/Domain/Tester/`) — BEVISST en EGEN klasse fra
`TestService`, IKKE en utvidelse: `TestService.OpprettTestAsync` gir automatisk ALLE partnere
`PartnerTestTilgang` (riktig for admin-forfattede tester, ment for bred distribusjon — se
"Partner System + Test Monetization") — en hjemmeoppgave skal derimot starte PRIVAT, synlig for
andre KUN når eieren eksplisitt deler den. Dekker opprett/rediger/slett/kopier/lik + de to
sharing-flaggene, pluss en ny valideringsmetode
`FinnManglendePaakrevdeAsync` kalt FRA `Pasientportal/Tester/Fyll.cshtml.cs` rett før innsending —
BEVISST IKKE lagt inn i den delte `TestService.LagreSvarAsync` (ville påvirket alle ~20 andre
testers fyllingsflyter, som i dag stille tillater å hoppe over ethvert ledd, se CLAUDE.md sin
GADIT-fallgruve for hvorfor det er load-bearing andre steder).

**Datasikkerhet ved redigering/sletting** (ikke eksplisitt spurt om, egen vurdering): en
hjemmeoppgaves ledd-liste kan KUN erstattes så lenge INGEN pasient ennå er tildelt den — en
strukturell endring etterpå ville foreldreløsgjort eksisterende `TestSvar`-rader. `OppdaterAsync`
lagrer metadata (navn/forklaring/takk-tekst) uansett, men nekter ledd-endringer med en tydelig
melding ("bruk Kopier i stedet") når minst én tildeling finnes — verifisert i nettleser: lagret
uendret ledd-liste på en allerede tildelt hjemmeoppgave viste korrekt låsemeldingen, og bilde-dataen
(141 380 tegn base64) var fortsatt intakt i databasen etterpå. `SlettAsync` følger samme prinsipp:
hard-sletter KUN hvis ingen tildelinger finnes, ellers arkiverer (`ErAktiv=false`) i stedet for å
risikere å ødelegge ekte svardata.

**Ny `Behandlerportal/Hjemmeoppgaver`-overflate** (Admin/Tester/Sider+Ledd er Admin-ONLY i dag — en
behandler har ikke den tilgangen, så dette MÅTTE bli en egen side, ikke gjenbruk): `Index.cshtml`
(fase 1: kun "Personlig"-listen, fulle faner kommer fase 2) + `Rediger.cshtml` (opprett/rediger i
ÉN side, flat ledd-liste bundet via indekserte skjemafelt `Ledd[n].X` — ASP.NET Cores
modellbinding håndterer en `List<T>` fra sammenhengende indekser uten eget parse-arbeid). Ny
`wwwroot/js/hjemmeoppgave-editor.js`: legg til/fjern ledd-rader klient-side (reindekserer ALLE
rader ved fjerning, slik at indeksene forblir sammenhengende), vis/skjul felt per svartype, og
en "squash"-funksjon (canvas-nedskalering til maks 1600px + JPEG-rekoding kvalitet 0,8) FØR et
opplastet bilde limes inn som base64 i et skjult felt — brukerens eksplisitte krav ("hvis du
støtter squashing, er det OK") — en rå mobilbilde-original (158 KB testfil) ble aldri sendt til
serveren, kun den ferdig nedskalerte JPEG-en (141 KB base64 ≈ 106 KB binært, verifisert i
nettleser).

**Reell bug funnet og fikset under verifisering**: JS-templatens `<select>` for svartype hadde
INGEN `selected`-markering på noe `<option>` — nettleseren faller da tilbake til FØRSTE alternativ
(`LikertSkala`) i stedet for C#-modellens standardverdi (`Fritekst`), så et nytt ledd lagt til via
"+ Legg til ledd" og aldri eksplisitt endret av forfatteren ble lagret som en tom Likert-skala (ingen
svaralternativer, usynlig i utfyllingsflyten) i stedet for et fritekstfelt. Fanget ved faktisk å
fylle ut den opprettede hjemmeoppgaven i nettleser og se at ett ledd manglet et synlig
inputfelt — IKKE noe en ren kodelesning ville avdekket. Fikset ved å eksplisitt markere
`Fritekst`-alternativet `selected` i malen, slik at klient- og server-standardverdi stemmer
overens.

**Rapportvisning** (begge Areas + Pasientportals egen lesetilgang): `Rapport.cshtml.cs` sin
`LastInnAsync` bailet TIDLIGERE ut med `NotFound()` for ENHVER test uten en registrert
`ITestSkaaringsberegner` (siden `BeregnSkaaringAsync` allerede returnerte `null` uendret for en
`Test.Kode == null`-test — ingen kodeendring trengtes DER). Ny `ErHjemmeoppgaveRapport`-gate
bypasser dette KUN for `Test.ErHjemmeoppgave` (bevisst IKKE en generell endring for enhver
score-løs test — utenfor scope, kunne hatt utilsiktede konsekvenser for en admin-forfattet test
uten skåringslogikk). Viser i stedet rå svar (gjenbruker den EKSISTERENDE `SideMedSvar`/`SvarRad`-
tabellen som allerede fantes parallelt med skåringsvisningen for alle tester) + et nytt
"Startet:"-felt (`TestTildeling.StartetUtc`, fantes allerede i skjemaet) ved siden av det
eksisterende "Fullført:"-feltet — brukerens eksplisitte krav. Bilde-ledd ekskludert fra selve
svar-tabellen (rent visningsinnhold, ikke et spørsmål/svar-par) i alle tre rapportvisninger
(Behandlerportal, Admin, Pasientportal).

Verifisert FULLT ende-til-ende i nettleser: opprettet en hjemmeoppgave (1 påkrevd Likert-ledd, 1
valgfritt fritekst-ledd, 1 bilde-ledd med faktisk opplastet+squashet bilde), tildelt en pasient
(direkte SQL — Fase 2s kategoritre-integrasjon ikke bygget ennå, se under), fylt ut som pasient:
innsending BLOKKERT med tydelig feilmelding når det påkrevde leddet sto tomt, LYKTES etter
besvarelse, belønningssiden viste korrekt fallback-tekst ("Ferdig!"/"Takk for at du fylte ut
testen.") siden takk-felt ble latt tomme. Behandlers rapport viste korrekt "Startet"+"Fullført",
INGEN "Resultat"-seksjon, rå svar-tabell med Likert-label og fritekst (bilde-leddet korrekt
utelatt), godkjenning fungerte uendret. 86 tester fortsatt grønne.

**Bevisst IKKE i fase 1** (hører til fase 2/3): hjemmeoppgaver vises ENNÅ IKKE i selve
test-tildelings-kategoritreet (`Tildel/Tester.cshtml` sin "Egenproduserte"-pinning er fase 2) —
tildeling for denne verifiseringen ble derfor gjort direkte i databasen, ikke via UI. Like/del/
Delt-faner/Partner-faner er heller ikke bygget (fase 2). "Kopier"-knappen og "Rediger (lager
kopi)"-knappen for en likt rad finnes i `Index.cshtml` sin kode, men kan ikke browser-testes
fullt ut før fase 2s liking-UI finnes (en annen behandlers delte hjemmeoppgave kan ikke vises i
"Personlig" ennå siden ingen UI kan opprette en `HjemmeoppgaveLiking`-rad).

### Fase 2 — hjemmeoppgave-faner + liking/deling + kategoritre-pinning (2026-10-04, samme dag)

`Index.cshtml` utvidet til full fane-struktur (Personlig/Delt/Partner/Opprett — "Partner"-fanen
vises kun når innlogget behandler faktisk har en `PartnerId`), samme generiske
fane+søk-mønster (`wwwroot/js/faner.js`) som Grupper/MinSide bruker fra før — "Opprett"-fanen er
bevisst IKKE en embedded skjema, bare en lenke til den allerede fullverdige `Rediger.cshtml`
(fase 1). Personlig-fanens rader skiller egne (full rediger/del/slett) fra likte (kun "Rediger
(lager kopi)"/"Fjern fra personlig") via `Test.OpprettetAvBehandlerId == EgenBehandlerId`.

**Reell bug funnet og fikset under verifisering**: "Del med alle"/"Del med partner"-knappenes
skjulte `verdi`-felt (`value="@(!test.ErDeltMedAlle)")`) rammet NØYAKTIG den allerede dokumenterte
Razor-fallgruven i CLAUDE.md om boolske bundne attributter — siden HELE attributtverdien var et
bool-uttrykk, rendret Razor den MINIMERTE boolske formen (`value="value"`) i stedet for den
faktiske strengen "True"/"False". Bekreftet ved å faktisk inspisere POST-dataen i nettleseren
(DevTools/JS, ikke bare lese kilden) — databasen viste `ErDeltMedAlle=0` etter et klikk som skulle
satt den til 1. Fikset med eksplisitt `.ToString()`: `value="@((!test.ErDeltMedAlle).ToString())"`,
verifisert på nytt — databasefeltet ble korrekt `1`, og knappeteksten flippet til "Avslutt deling
(alle)".

**Kategoritre-pinning** (`Tildel/Tester.cshtml.cs`, BEGGE Areas): en ny "Egenproduserte"-seksjon
pinnes ØVERST, FØR alle ekte kategorier — bygget som en ren in-memory `TestKategori { Id = -1, ... }`
-sentinel (ALDRI lagret) prependet til resultatet fra `HentKategoriTreAsync`, som selv forblir
HELT uendret (null risiko for eksisterende kategori-visning). Synlighet er bevisst ASYMMETRISK
mellom Areas, en egen vurdering (ikke spurt om): Behandlerportal bruker
`HjemmeoppgaveService.HentTilgjengeligeForTildelingAsync` (egne + likte + delt-med-alle +
partner-delte — samme samlede synlighet som selve Hjemmeoppgaver-siden sine tre faner), mens
Admin bruker en EGEN, snevrere `HentDeltMedAlleForAdminAsync` (KUN delt-med-alle) — en
administrator har ingen eierskap/partnerskap-relasjon til noen behandlers private hjemmeoppgave,
og skal derfor aldri kunne se eller tildele en som ikke eksplisitt er gjort offentlig.

Verifisert FULLT ende-til-ende i nettleser: delte en hjemmeoppgave "med alle" som én behandler,
bekreftet databasefeltet satt korrekt, logget inn som administrator og bekreftet
"Egenproduserte"-seksjonen dukket opp øverst i kategoritreet på `Admin/Tildel/Tester` med nøyaktig
den delte hjemmeoppgaven synlig og avkrysningsbar. 86 tester fortsatt grønne.

**Bevisst IKKE i fase 2** (uendret fra fase 1s vurdering, utsatt til egen oppfølging ved behov):
selve liking→kopi-flyten (en ANNEN behandler faktisk trykker "👍 Lik" i "Delt"-fanen, ser referansen
dukke opp i sin egen "Personlig"-fane, og deretter "Rediger (lager kopi)") ble IKKE browser-testet
med to reelle, separate behandler-identiteter i denne runden — kun verifisert via kodelesing +
en direkte databasesjekk av at `ErDeltMedAlle`-flagget faktisk styrer `HentDeltMedAlleAsync`
korrekt. Samme underliggende spørringslogikk brukes av liking-flyten, så risikoen vurderes lav,
men er ikke identisk med en fullverdig to-bruker ende-til-ende-verifisering.

### Fase 3+4 — program-datamodell/motor + tildeling/kjørende opplevelse (2026-10-04/05, samme runde, bygget autonomt over natten på brukerens eksplisitte "push through autonomously")

Seks nye entiteter i `TestBase.Shared/Domain/Tester/`: `Behandlingsprogram` (programmets egne
felt — Navn/Forklaring/StartUkedag/StartKlokkeslett/delingsflagg/arkivert/kopiert-fra),
`ProgramDrop` (Rekkefolge, `DagerEtterForrige` — kumulativt dag-offset fra dag 0, eget
Fra/Til-tidsvindu + `UnngaaNatt`), `ProgramDropTest` (mange-til-mange med egen Rekkefolge internt i
droppen), `ProgramDeltakelse` (én rad per pasient PER tildelingsbatch — `ProgramStartUtc`,
`NaavaerendeDroppIndeks`, `NesteDroppPlanlagtUtc`, `PauseUtc`/`MeldtUtUtc`/`FullfortUtc`),
`ProgramTildeling` (den ENESTE koblingen tilbake fra en generisk `TestTildeling` til hvilken
deltakelse/drop/posisjon den stammer fra — unik indeks på `TestTildelingId`, siden en tildeling
aldri kan tilhøre mer enn én program-posisjon), `ProgramLiking` (identisk mønster som
`HjemmeoppgaveLiking`). To migrasjoner (`ProgrammerFase3DataModell` + en liten oppfølgende
`ProgrammerLiking` — sistnevnte fordi `ProgramLiking`-entiteten/DbSet-et opprinnelig ble glemt i
første runde og måtte ettermonteres, fanget av en kompilatorfeil FØR commit, ikke i produksjon).

**Selvfanget navnekollisjon FØR noe bygget feil:** å kalle hovedentiteten rett og slett `Program`
ville kollidert med .NET sin egen auto-genererte top-level-statements `Program`-klasse i
`TestBase.Web/Program.cs` — IKKE den dokumenterte "Area-navn skygger domenetype"-fallgruven i
CLAUDE.md (dette er en klassenavn-kollisjon med en kompilatorgenerert type, ikke et
navneromssegment), men samme underliggende lærdom: sjekk ALLTID om et nytt domenenavn kolliderer
med noe .NET selv genererer, ikke bare med eksisterende Areas. Omdøpt til `Behandlingsprogram` før
noen build i det hele tatt ble forsøkt — ingen feilmelding noensinne sett, ren selvkorreksjon.

**Kjøremotoren gjenbruker eksisterende infrastruktur i stedet for å finne opp sin egen:**
`PlanlagtTildelingService.BeregnNesteForekomstUtc(etterUtc, ukedag, klokkeslett)` (allerede
eksisterende statisk metode fra den tidligere "planlagt tildeling"-funksjonen) beregner
programmets konkrete dag-0-ankertidspunkt; en ny statisk `ProgramService.RandomiserTidspunkt`
beregner et faktisk forpliktet klokkeslett innenfor en drops eget vindu, med `UnngaaNatt` som
en KLEMMING mot 07:00 (ikke et nytt tilfeldig forsøk) hvis det opprinnelige tilfeldige valget
havnet i 22:00–07:00 — enkelt, forutsigbart, og garantert terminerende selv om HELE vinduet ligger
på natten (dekket av en ny enhetstest, se under). En egen `ProgramBakgrunnstjeneste` (2-minutters
polling, samme `BackgroundService`-mønster som `PlanlagtTildelingBakgrunnstjeneste`) kaller
`FyrAvDueAsync` — verifisert med en ekte, ublokkert kjøring: tvang `NesteDroppPlanlagtUtc` til
fortiden via direkte SQL og bekreftet (backgrounded polling-sjekk) at tjenesten faktisk fanget opp
og fyrte av droppen innenfor sitt naturlige intervall, ikke en manuelt trigget snarvei.

**Progressiv, "én test om gangen"-kjeding innad i en drop** (bevisst valgt fremfor å opprette ALLE
en drops `TestTildeling`-rader på én gang): KUN den første testen i en drop får sin `TestTildeling`
opprettet når selve droppen fyrer (`FyrAvDroppAsync`); hver påfølgende test i SAMME drop opprettes
først når pasienten fullfører den forrige, via `HaandterFullfortTestAsync` — et NYTT, eksternt kall
fra `Pasientportal/Tester/Fyll.cshtml.cs` rett etter `TestService.LagreSvarAsync(..., markerFullfort:
true)`. Samme bevisste arkitekturprinsipp som hjemmeoppgavenes mandatory-validering (fase 1):
`TestService`/`LagreSvarAsync` er HELT uvitende om at Programmer eksisterer — koblingen går via
`ProgramTildeling` og et kall utenfra, ikke en utvidelse av den delte metoden. Når en drops siste
test er fullført, planlegges NESTE drops tidspunkt (`PlanleggNesteDroppEllerFullforAsync`) — eller,
hvis det var den siste droppen i programmet, settes `ProgramDeltakelse.FullfortUtc`.

**Betalingsregel** (brukerens eksplisitte krav — "charge once at first drop, rest free"): KUN
aller første test i aller første drop kan noensinne få en reell pris
(`TestPrisberegner.Beregn`); enhver annen test i programmet (resten av samme drop, ALLE senere
drops) blir ALLTID tvunget til `BetalingStatus.IkkePakrevd`/0 kr, uavhengig av testens egen prising.
En prøvepasient (intet personnummer) betaler ALDRI noe uansett, konsistent med det eksisterende
systemomfattende unntaket i `TestTildelingsService.TildelOgVarsleAsync` — reglene kombineres (første
test+første drop+IKKE prøvepasient er ALLE tre nødvendige for at en reell pris i det hele tatt
vurderes). Dekket av en ny regresjonstest (se under) som eksplisitt beviser alle fire kombinasjoner
(ekte pasient betaler for test 1, ekte pasient betaler IKKE for test 2 i samme drop ELLER for test 1
i drop 2, prøvepasient betaler ALDRI selv for test 1 i drop 1).

**Pasientens pause/meld-ut** (ny `<dialog>` på `Pasientportal/Tester/Fyll.cshtml`, vist KUN når
den aktuelle tildelingen faktisk stammer fra et program): Pause stopper FREMTIDIGE drops midlertidig
(allerede igangsatt test i gjeldende drop fullføres normalt — ingen avbrytelse midt i), Meld ut er
TERMINALT (aldri flere påminnelser fra akkurat dette programmet igjen, men samme prinsipp om at en
allerede igangsatt test fullføres normalt). Begge oppretter en `BehandlerMelding.Fritekst`-oppgave
hos den behandleren som FAKTISK tildelte programmet (`ProgramDeltakelse.TildeltAvBehandlerId`) — et
admin-tildelt program oppretter BEVISST ingen slik oppgave (samme scope-avgrensning som ble
diskutert for 13-punkts-runden sitt punkt 13: det finnes ingen generell "hvilken behandler eier
akkurat nå denne pasienten"-oppslagslogikk å gjenbruke trygt her uten mer utredning, og det ble
vurdert at admin selv kan følge opp via den aggregerte Kjørende-oversikten uansett). Verifisert
fullt ende-til-ende i nettleser: pause satte riktig `PauseUtc` + opprettet riktig meldingstekst
synlig på behandlerens Min Side; en etterfølgende "Fjern" (fra Kjørende-oversikten, se under) satte
riktig `MeldtUtUtc` og en egen oppgavetekst.

**"Kjørende"-oversikt, AGGREGERT per (ProgramId, GruppeId)** (brukerens eksplisitte svar —
"aggregated count", ikke én rad per pasient i selve tabellen): `Behandlerportal/Programmer`
(ny "Kjørende"-fane, kun EGNE tildelinger) og `Admin/Programmer/Kjorende` (ALLE tildelinger på
tvers av behandlere, med behandlerens navn i parentes foran programnavnet — brukerens eksplisitte
formatkrav) viser én rad per gruppe-tildelingsbatch (eller én rad per individuelt tildelt pasient
når `GruppeId` er null), med en samlet "Pause"/"Fjern"-knapp som virker på HELE batchen på én gang
(`PauseFlereAsync`/`FjernFlereAsync`, som bare looper de underliggende enkelt-metodene — ingen egen
bulk-SQL, siden antallet deltakere per gruppe er lite nok at N+1 aldri er et reelt problem her).
Verifisert i nettleser: én rad med riktig "Gjenstående drops"-tall, riktig aggregert
deltaker-telling, "Pause" satte riktig status + viste en bekreftelsesmelding, "Fjern" (med en
`confirm()`-dialog, jf. samme destruktiv-handling-mønster som Grupper sin hard-slett) fjernet raden
helt fra den kjørende listen og satte `MeldtUtUtc` korrekt i databasen.

**Bevisste scope-avgrensninger denne runden** (ikke spurt om, egne vurderinger — ingen av disse
ble oppdaget som reelle mangler under verifisering, kun bevisst utelatt arbeid):
- Ingen drag-og-slipp-omordning av tester innad i en drop i forfatter-UI-et — kun en enkel
  flervalgsliste (native `<select multiple>`), rekkefølgen følger utvalgsrekkefølgen.
- Ingen mulighet til å overstyre starttidspunktet for ÉN enkelt deltaker i en gruppe-tildeling —
  "same day for all" (brukerens eget svar) tolkes strengt: alle som tildeles sammen deler nøyaktig
  samme `ProgramStartUtc`, ingen senere per-person-justering.
- Ingen egen partner-admin-visning av Kjørende på tvers av KUN partnerens egne kolleger (kun den
  globale admin-visningen og behandlerens egen — en mellomting ble vurdert unødvendig kompleks for
  denne runden, kan legges til senere med samme `AggregerKjorendeAsync`-grunnlag).
- `OppdaterAsync` nekter å endre selve drop-/testlisten når programmet allerede har minst én
  deltakelse (samme "strukturell endring etter tildeling er farlig"-prinsipp som hjemmeoppgavenes
  fase 1) — kun navn/forklaring/starttidspunkt kan endres da, "Kopier" er veien til en ny versjon.

**Ny regresjonstest-fil** (`tests/TestBase.IntegrationTests/ProgramServiceTests.cs`, 4 nye tester,
90 totalt): to rene enhetstester av `RandomiserTidspunkt` (unngå-natt-klemming OG at et vindu uten
UnngaaNatt beholder et faktisk natt-tidspunkt uendret), én full integrasjonstest som bygger et
2-drops program (drop 1: to tester, drop 2: én test) og kjører hele livssyklusen gjennom ekte
tjenestekall (`TildelAsync` → tving `NesteDroppPlanlagtUtc` til fortiden → `FyrAvDueAsync` →
`LagreSvarAsync`+`HaandterFullfortTestAsync` gjentatt per test) og beviser betalingsregelen presist
for BÅDE en ekte pasient og en prøvepasient, kjedingen innad i drop 1, og at `FullfortUtc` først
settes etter ALLE drops er unnagjort — og én test av pause/meld-ut-tilstandsmaskinen + at
`BehandlerMelding`-oppgaven faktisk opprettes (via `TellUlesteAsync`-tellingen), inkludert at et
pauseforsøk ETTER meld-ut er en stille no-op (meld-ut er terminalt). Alle 90 tester grønne.

**Autorisasjon verifisert eksplisitt** (jf. CLAUDE.md sin dokumenterte "ny sidemappe er ikke
automatisk beskyttet"-fallgruve): `AuthorizeAreaFolder` lagt til for BÅDE `Admin/Programmer` og
Behandlerportal sine `Hjemmeoppgaver`/`Programmer`-mapper i `Program.cs`, bekreftet med rå,
uautentiserte `curl`-kall mot alle fem nye sidestiene (`Kjorende`, `Programmer`-indeks,
`Programmer/Rediger/{id}`, `Programmer/Tildel/{id}`, `Hjemmeoppgaver`) — samtlige ga korrekt `302`
til innlogging, ingen `200`.

**Falsk alarm underveis, ikke en kodefeil:** en forvirring om hvorvidt
`PlanlagtTildelingService.BeregnNesteForekomstUtc` ga feil resultat (viste "neste søndag" i stedet
for "i dag") viste seg etter grundig isolasjon (en midlertidig xUnit-test som kalte metoden direkte,
senere slettet) å være en feiltolkning av Git Bash (MSYS) sin `TZ="Europe/Oslo" date`-utdata — den
respekterte faktisk ikke `TZ`-variabelen i akkurat denne påkallingen og printet UTC, ikke norsk
lokal tid, noe som fikk "nå" til å se ut som to timer tidligere enn det faktisk var. Krysssjekket
med PowerShell sin `[DateTime]::UtcNow` for å bekrefte den ekte UTC-tiden. INGEN kodeendring var
nødvendig — ren tidssone-/verktøy-misforståelse fra min side, ikke en reell regresjon i
planleggingslogikken. Nevnt her som en advarsel mot å stole blindt på `TZ=` foran `date` i Git Bash
for fremtidig feilsøking av tidssonespørsmål.

Alt arbeid i denne runden (Fase 0 t.o.m. Fase 3+4, inkl. Fase 5 — se eget avsnitt rett under, som
viste seg allerede dekket) er pushet til `origin/master` og kjørt gjennom den fulle autonome
CI/CD-pipelinen (`.github/workflows/deploy.yml`) samme natt: build+90 tester grønt → deploy beta →
helsesjekk beta → deploy live → helsesjekk live, alle steg grønne (kjøring `37244657735`). Verifisert
UAVHENGIG av selve pipelinen etterpå med et funksjonelt `curl`-kall mot en STI SOM KUN FINNES i den
nye koden (`https://www.psytest.no/Admin/Programmer/Kjorende` → `302` til innlogging, ikke `404`) —
samme prinsipp som den dokumenterte "`azd deploy` kan rapportere suksess uten at koden faktisk
endret seg"-fallgruven krever, ikke bare en generisk helse-sjekk som også ville bestått på gammel
kode.

### Fase 5 — programdeling (viste seg allerede bygget i fase 4)

Ved gjennomgang før commit var Fase 5 (Personlig/Delt/Partner/Opprett-faner + liking, samme mønster
som hjemmeoppgaver) allerede fullt implementert som en integrert del av `Behandlerportal/Programmer/
Index.cshtml(.cs)` i Fase 4-arbeidet (kommentaren i `IndexModel` sier det selv: "Fase 5 ... pluss en
Kjørende-fane (fase 4)") — ingen egen Fase 5-commit var nødvendig. Verifisert i nettleser: "Del med
alle" på et eksisterende program satte `ErDeltMedAlle=1` korrekt (boolsk-attributt-fallgruven fra
CLAUDE.md IKKE gjentatt her — koden brukte allerede riktig `.ToString()` fra starten av), knappeteksten
flippet korrekt til "Avslutt deling (alle)", og feltet ble satt tilbake til `0` etterpå for å holde
dev-databasen ren. Cross-behandler liking (en ANNEN behandler faktisk trykker "👍 Lik" og ser
referansen dukke opp i sin egen "Personlig"-fane) ble IKKE browser-testet med to reelle identiteter
denne runden — samme bevisste avgrensning og lave risikovurdering som ble gjort for hjemmeoppgavenes
tilsvarende fase 2 (identisk spørringslogikk, kun navn/tabell endret).

## 38-punkts brukerfeedback-runde på Hjemmeoppgaver/Programmer (2026-10-05/06)

Brukeren leverte en oppdatert `bugs_features.txt` (IKKE i kildekontroll) med 38 punkter om
Hjemmeoppgaver/Programmer-funksjonaliteten bygget natten før, og ba om at ALT bygges autonomt over
natten uten videre avklaring ("push it all the way out"). To korte avklaringsspørsmål ble stilt
helt i starten (innenfor brukerens egne "10 minutter") og besvart med "anbefalt" på begge: full
kalender-visning for programmets drops (ikke bare en forbedret liste), og full sammenslåing av
tester/hjemmeoppgaver/programmer i ÉN tildelingsflyt. Alle 38 punkter er dekket, de fleste fullt
browser-verifisert — se egne avsnitt under per tematisk commit-gruppe. 92 tester grønne gjennom
hele runden (ingen regresjon).

### Punkt 38 (reell bug, undersøkt FØRST siden brukeren selv fremhevet den): gruppe-tilordnet test ble aldri sendt til eksisterende medlemmer

"NO info was sent when i sent out homework to patient on sms/email" viste seg IKKE å være en
feil i selve varslingspipen (som fortsatt fungerer korrekt — bekreftet ved et direkte
Tildel/Tester-forsøk som sendte både SMS og e-post helt normalt), men et ekte, dypere hull:
`GruppeService.SettTilordnedeTesterAsync` (kalt fra BÅDE `OpprettAsync` og `OppdaterAsync`) har
ALLTID kun skrevet en `GruppeTestTilordning`-rad når en ny test legges til en gruppes testliste —
ALDRI opprettet noen `TestTildeling` eller sendt noe varsel til gruppens allerede eksisterende,
aktive medlemmer. Kun et HELT NYTT medlem (via `BliPasient`-QR-registrering) fikk noensinne en
reell tildeling for en gruppes tester. Stille, ingen feil, ingen logglinje — usynlig med mindre
man visste nøyaktig hvor man skulle lete. Fikset: `SettTilordnedeTesterAsync` etterfyller nå de
NYE testene til alle gruppens aktive medlemmer (samme `TestTildelingsService.TildelOgVarsleAsync`-
kall som `BliPasient` bruker for et nytt medlem), ekskludert en pasient som allerede har EN
tildeling av akkurat den testen fra før (unngår duplikat-utsending). `OppdaterAsync` fikk en ny
påkrevd `baseUrl`-parameter (begge Areas sine `Rediger.cshtml.cs` oppdatert); `OpprettAsync` sender
bevisst `baseUrl: null` siden en splitter ny gruppe aldri har eksisterende medlemmer uansett —
etterfyllingsgrenen tas da aldri. 2 nye regresjonstester (`GruppeServiceTests.cs`).

### Punkt 1-6, 8-14: full ombygging av hjemmeoppgave-editoren

Den gamle editoren (ett tekstfelt for "verdi:tekst"-svaralternativer, statiske "Ledd N"-
overskrifter, et obligatorisk første ledd, rene tekstknapper) bygget fullstendig om:

- **Ny visuell svaralternativ-bygger** (punkt 12) per svartype — INGEN "verdi:tekst"-syntaks
  synlig for forfatteren lenger. Likert-skala får en statement-for-statement-liste (legg til/fjern
  rader med verdi+tekst), VAS får to endepunkt-tekstfelt med en visuell linje mellom, Ja/Nei viser
  en statisk forhåndsvisning av de to faste knappene (ingen oppsett nødvendig), og en ny
  `TestSvartype.Url` (punkt 13 — pasienten skriver selv inn en lenke som svar, lagres som vanlig
  fritekst men rendres som `<input type="url">`) trenger ingen bygger i det hele tatt. Alt
  serialiseres til nøyaktig samme wire-format som før ved innsending (JS kjører rett før
  `submit`-eventet) — INGEN endring i hvordan eksisterende tester/skåringsberegnere leser
  `Svaralternativer`, kun forfatter-UI-et er nytt.
- **Ny `TestLedd.BildeUrl`** (migrasjon, punkt 13 del 2) — en valgfri lenke forfatteren legger ved
  et Bilde-ledd (f.eks. en video), vist som en klikkbar lenke UNDER selve bildet. Atskilt fra
  `TestSvartype.Url` (pasientens EGEN svar-lenke) — dette er forfatterens eget innhold.
  `HjemmeoppgaveLeddInput`/`HjemmeoppgaveService` tredd gjennom tilsvarende.
- **Dra-og-slipp-omordning** (punkt 5) av ledd via et håndtak, med reindeksering av skjemafelt-
  navn etter hver flytting — verifisert med simulerte `DragEvent`-er (riktig ny rekkefølge + riktig
  `Ledd[n].X`-navngiving).
- **Minimer/maksimer** (punkt 6, 10) med tilstand lagret i `localStorage` (punkt 11 — nøkkel per
  test-id + posisjon, en bevisst forenkling siden ledd ikke har en stabil id før lagring/ved
  omordning). Kun spørsmålsteksten (live-oppdatert mens man skriver, punkt 10) vises igjen når et
  ledd er minimert — den statiske "Ledd 1"-overskriften er fjernet helt (punkt 8).
- **Ingen ledd som standard** (punkt 4) — kun en "+ Legg til ledd"-knapp, ingen "fjern første
  ledd er blokkert"-begrensning lenger (verifisert: fjernet ALLE ledd ned til 0 uten feil).
- **Ikon+tekst-knapper** i behandlerens egen rollefarge (punkt 4, 6) i stedet for rene
  tekstknapper — ny `.hjo-btn`-klasse.
- **Ny kontekstsensitiv forklaringsboks** (punkt 3) til høyre for ledd-listen, oppdateres ved
  fokus på et felt (spørsmål/instruksjon/svartype/påkrevd/bilde-url), med egne forklaringstekster
  per svartype.
- **Plassholdertekst i stedet for etiketter** (punkt 1, 9) for spørsmål-/instruksjonsfeltene —
  løser samtidig den opprinnelige forvirringen om hvilken etikett som "eide" hvilket felt
  (tett avstand felt→egen etikett/plassholder, større avstand ned til neste feltgruppe for de
  feltene som fortsatt HAR en synlig etikett, f.eks. Svartype-nedtrekksmenyen).
- **Instruksjon er nå en auto-voksende `<textarea>`** (punkt 14) i stedet for et enkelt tekstfelt.
- **Svartype-navn med mellomrom** (punkt 2) — en ny `SvartypeNavn()`-visningsmapping i stedet for
  det rå enum-navnet ("Visuell Analog Skala (VAS)" i stedet for "VisuellAnalogSkala" osv.).

Reell CSS-spesifisitetsbug funnet og fikset underveis: den generelle "main.page form input
{ width: 100% }"-regelen ga Likert-byggerens smale verdi-felt og brede tekst-felt stikk motsatte
bredder av det som var tiltenkt — løst med en mer spesifikk selector, samme prinsipp som flere
tidligere CSS-kollisjoner i dette prosjektet (se `.btn-icon`-fallgruven i CLAUDE.md).

### Punkt 15-24, 26-27: Tildel tester / Hjemmeoppgaver / Programmer UI-polering

**Den mest alvorlige fiksen i denne gruppen (punkt 23):** samme `main.page form button[type=
"submit"]`-regel som rammet `.btn-icon` tidligere (se den dokumenterte "BIGBUTTONS"-fallgruven i
CLAUDE.md) viste seg å ramme `.btn-accent`/`.btn-muted`/`.btn-outline` også, på ETHVERT sted i HELE
appen der en slik knapp IKKE også hadde literal `.btn`-klasse i tillegg (de fleste steder — kun
Tildel/Tester sine egne dialog-knapper hadde begge). Resultat: en behandlers EGEN rollefarge
(teal), en administrators (blått) og en superadmins (lilla) ble alle den samme literale
`--accent`-oransjen på en `type="submit"`-knapp, site-wide — ikke bare på Tildel/Tester, som var
der brukeren faktisk la merke til det. Fikset med tre nye, presise mot-regler (samme
spesifisitetsnivå, senere i fila) som vinner tilbake riktig farge UTEN å fjerne strukturstylingen
disse knappene fortsatt trenger (mange har ALDRI hatt literal `.btn`, kun `.btn-accent` alene).

Øvrige punkter: test-ikonene (+/$/klinikk-ikon) flyttet INN i selve `<label>`-en som en egen
inline-flex-gruppe, slik at de alltid bryter sammen MED testnavnet i stedet for å havne alene på
en egen linje under et langt navn (16); "Pasienten kan belastes for denne testen" → "Tolkning kan
prises" (17); kategori-treet er nå lukket som standard med antall tester i parentes bak hvert
kategorinavn (18, 19), og en kategoris overskrift fremheves i behandlerens rollefarge når minst én
test under den er valgt, via en ny `tildel.js`-lytter (20); en GRATIS test/hjemmeoppgave viser nå
kun navnet i bekreftelsesdialogen i stedet for en meningsløs "0,00 NOK (Plattform 0,00 NOK, ditt
honorar 0,00 NOK)"-linje, og totalsummen skjules helt når ALLE valgte elementer er gratis (21, 22);
etter en fullført utsending vises nå ÉTT "🏠 Tilbake til Min Side"-knapp i stedet for "Tildel flere
tester" + "Tilbake til pasienter" (24); "+ Nytt program" er nå normal knapp-høyde (`.btn-sm`) i
stedet for en stor CTA-knapp (25); Hjemmeoppgaver/Programmer sine topp-nav-ikoner byttet fra
hus/fly til en avkrysningsliste og en kalender (26, 27). In-table-handlinger (Rediger/Slett/Del
med alle/Del med partner/Kopier/Lik/Pause/Fjern) i BEGGE Hjemmeoppgaver- og Programmer-tabellene
(begge Areas) er nå kompakte `.btn-icon`-knapper med `title`-attributt i stedet for rene
tekstknapper, med en ny `.btn-icon--aktiv`-ring for toggle-handlinger som viser nåværende
PÅ/AV-tilstand (15, dekker også store deler av punkt 35). 8 nye SVG-ikoner lagt til i
`_Ikon.cshtml` (minimer/maksimer/drag/hjemmeoppgave/kalender/kopier/lik/del).

### Punkt 28-35: programmer-editoren bygget om til en relativ-dag-kalender

Erstattet den gamle "Drop 1/2/3"-listen (native `<select multiple>` for tester) med et klikkbart
6-ukers (42 dager) dag-rutenett — "dag 0" er programmets starttidspunkt (StartUkedag/
StartKlokkeslett), hver påfølgende dag kan få én drop via ÉN delt, gjenbrukt dialog i stedet for
en boks per drop (30, 31). Dette er bevisst en RELATIV kalender (dag 0..41), ikke en ekte
måned/år-kalender — programmets faktiske startdato avgjøres fortsatt først ved tildeling
(`PlanlagtTildelingService.BeregnNesteForekomstUtc`), så en absolutt kalender ville vist feil
datoer uansett.

Ny delt komponent `Pages/Shared/_TestKategoriVelger.cshtml` (+ gjenbruk av `tildel.js`/
`testinfo.js`/`testtre-filter.js`, som alle VISTE SEG å allerede være skrevet klasse-generisk —
ingen endring trengtes i dem for gjenbruk) — samme skalerbare, søkbare, avkrysningsbaserte
kategori-tre som Tildel/Tester, nå brukt INNI selve drop-dialogen i stedet for den lille native
multi-select-en (32, 33 — "reuse the tildel tester window", bokstavelig talt samme komponent).
Inkluderer dermed automatisk "Egenproduserte" (hjemmeoppgaver) øverst (34) —
`Programmer/Rediger.cshtml.cs` bygger nå samme kategoritre-med-pinning som `Tester.cshtml.cs` i
stedet for en flat `HentAktiveTesterAsync`-liste.

Klokkeslett-feltene byttet fra `<input type="time">` til tekstfelt med mønstervalidering
(28, 29) — en NATIV time-input sitt AM/PM-oppsett viste seg IKKE la seg styre av `lang`-
attributtet alene ved faktisk verifisering i Chromium (fortsatt "09:00 AM" selv med `lang="nb"`),
så et vanlig tekstfelt med `pattern="([01][0-9]|2[0-3]):[0-5][0-9]"` garanterer 24-timersformat
uavhengig av nettleser-/OS-locale i stedet. Feltet fikk også en eksplisitt, smalere bredde (arvet
tidligere `width: 100%` fra den samme generelle skjema-regelen som rammet punkt 23).

Intern tilstand (dag → {fra, til, unngåNatt, testIder}) holdes i JS og serialiseres til de
eksisterende `Drops[i].*`-skjulte feltene rett før innsending — INGEN endring i selve
lagringskontrakten (`ProgramService`/`ProgramDropInput` urørt), kun UI-laget er nytt. Fullt
verifisert ende-til-ende i nettleser: opprettet et program med drops på dag 0 og dag 7, bekreftet
riktig `DagerEtterForrige`-kjede (0, 7) og riktig `TestId`-tilordning i databasen, og bekreftet at
re-redigering korrekt gjenoppbygger kalenderen (badges) og forhåndskrysser riktige tester når en
dag åpnes på nytt.

### Punkt 36-37: programmer og hjemmeoppgaver inn i de delte tildelingsflytene

**Punkt 37** (Tildel/Tester, Behandlerportal): en ny "Programmer"-seksjon i kategori-treet lar en
behandler velge ett eller flere programmer i SAMME handling som vanlige tester — avkrysning sender
`name="ProgramIder"` i stedet for `name="TestIder"`, gjenbruker `.tildel-test-checkbox`-klassen
(uten `data-test-id`) for gratis kategori-fremheving/infoboks UTEN å utløse noen av pris-/
honorar-logikken (programmer har sin egen betalingsregel). `OnPostSendAsync` kaller nå
`ProgramService.TildelAsync` for hvert valgt program VED SIDEN AV den vanlige
`TildelOgVarsleAsync`-tildelingen for valgte tester — to kall til allerede eksisterende, fullt
testede tjenester, bevisst IKKE en omskriving av selve tildelingsmotoren. Bekreftelsessiden viser
en egen linje per startet program. En reell liten bug ble fanget og fikset underveis:
`tildel.js` sin oppsummerings-bygger brukte `data-test-id` som unik dedupliseringsnøkkel, noe et
program-checkbox mangler — ved valg av FLERE programmer samtidig ville alle utover det første
blitt stille utelatt fra selve bekreftelsesdialogens LISTEVISNING (de ville likevel blitt korrekt
tildelt ved faktisk innsending, siden det er et helt separat skjemafelt). Fikset med en
fallback-nøkkel basert på checkboxens eget navn+verdi. Fullt browser-verifisert: en
`ProgramDeltakelse`-rad opprettet korrekt i databasen fra selve Tildel/Tester-flyten.

**Kjent, bevisst IKKE dekket:** den planlagte/utsatte sendingsveien
(`PlanlagtTildelingService`/`OnPostPlanleggAsync`) kjenner fortsatt ikke til `ProgramIder` — et
program valgt sammen med "I morgen i arbeidstiden" eller en egendefinert fremtidig dato vil derfor
bli stille droppet. Kun "Nå" (umiddelbar sending) er verifisert og fungerer for programmer.

**Punkt 36** (Grupper/Ny+Rediger, Behandlerportal): samme "Egenproduserte"-pinning lagt til her
også — en behandler kan nå tilordne sin egen hjemmeoppgave til en gruppe, noe som tidligere var
helt umulig (siden disse sidene brukte `HentKategoriTreAsync` direkte, uten pinning).
**Programmer i Grupper er BEVISST IKKE dekket** denne runden: Programmer mangler et
`GruppeTestTilordning`-ekvivalent konsept (automatisk tildeling til FREMTIDIGE medlemmer som
melder seg inn via QR etter at gruppen allerede har et tilordnet program) — å bygge dette skikkelig
er sammenlignbart i omfang med selve `GruppeTestTilordning`-mekanismen (som i seg selv var en egen,
flerfaset funksjonspakke, se "Invitasjons- og gruppesystem" lenger opp i dette dokumentet), og ble
derfor utsatt fremfor en halvveis/misvisende løsning som enten (a) kun tildeler programmet til
EKSISTERENDE medlemmer uten å forklare hvorfor nye medlemmer ikke får det, eller (b) later som
funksjonaliteten er fullverdig når den ikke er det.

### Punkt 7: hjelpeinnhold for hele funksjonspakken

Hjelpemenyen (se "Hjelpemeny" lenger opp, bygget 2026-10-03 — FØR Hjemmeoppgaver/Programmer
eksisterte) hadde ingen artikler om noen av disse funksjonene i det hele tatt. 9 nye artikler lagt
til i `HjelpInnhold.cs`, samme statiske kode-fremfor-database-mønster som resten av filen: fem for
Behandler (hjemmeoppgave-opprettelse, svartype-forklaring, deling/liking, program-opprettelse via
kalenderen, program-tildeling, program-oppfølging/Kjørende), én for Pasient (pause/meld-ut-
dialogen), og én for Admin (den cross-behandler "Kjørende programmer"-siden). Alle kontekst-
prefikser peker på de faktiske, nybygde sidestiene fra denne runden. Verifisert i nettleser:
riktig, kontekstrelevant artikkel dukker opp i hjelpepanelet på både `Hjemmeoppgaver/Rediger` og
`Programmer/Rediger`.

### Status ved avslutning

Alt arbeid i denne 38-punktsrunden er committet lokalt og pushet til `origin/master`, kjørt
gjennom den fulle autonome CI/CD-pipelinen (build+test → deploy beta → helsesjekk → deploy live →
helsesjekk) på samme måte som tidligere runder — se `.github/workflows/deploy.yml`. 92
integrasjonstester grønne gjennom hele runden, ingen regresjon i noen eksisterende funksjonalitet.
Bevisste, dokumenterte scope-kutt (ikke oversett som mangler): programmer i den planlagte/utsatte
sendingsveien, programmer i Grupper sin automatiske fremtidig-medlem-tildeling, og cross-behandler
browser-verifisering av liking-flyten (uendret fra tidligere runder).

## 18-punkts brukerfeedback-runde #2 på Hjemmeoppgaver/Programmer (2026-10-06)

Ny `bugs_features.txt`-runde (18 punkter, samme autonome "push through without asking for
opinions"-instruks som forrige runde) — implementert, testet og pushet samme natt/morgen.

**Punkt 1 (reell, tidligere udokumentert CSS-fallgruve):** `style="display: inline;"` (MED
mellomrom etter kolon) matcher ALDRI attributt-selektoren `[style*="display:inline"]` (uten
mellomrom) — de to Hjemmeoppgaver/Programmer-sidene som ble bygget i FORRIGE runde arvet denne
skrivemåten fra enda eldre kode og fikk dermed uønsket den brede "frittstående skjema"-kort-
stylingen på hver inline action-knapp-form ("boxes around the inline buttons"). Rettet ved å
normalisere til samme mellomromsfrie skrivemåte alle ANDRE (korrekt fungerende) steder i
kodebasen allerede bruker — IKKE ved å endre selve CSS-selektoren, som dusinvis av andre, allerede
korrekte forms er avhengige av. Lagt til i fallgruve-lista i CLAUDE.md.

**Punkt 2-3 (program-drop-dialogen):** en til-nå aldri-faktisk-virkende `!important`-mangel
funnet — `main.page form input.program-klokkeslett-input` (fra forrige rundes "Punkt 29"-fiks)
har FAKTISK lavere spesifisitet enn den generelle `main.page form input:not([type=...])×6`-
regelen (seks `:not()`-ledd teller som seks klasser i spesifisitetsberegningen), så
klokkeslett-feltene ble likevel 100% brede og pakket om på hver sin linje i praksis — løst med
`!important`, samme presedens som `.rapport-handlinger form` allerede bruker for akkurat dette
problemet et annet sted i filen. Avbryt/Fjern/Lagre-knappene flyttet til toppen av dialogen.

**Punkt 4-6 (den delte test-velgeren, `_TestKategoriVelger.cshtml`):** søkefeltet var en FLEX-
søsken av selve kategori-treet (ikke et eget element over) — endret til et eget fullbredde-element
over en ren CSS GRID (`60% 1fr`) for tre/infoboks. En tidligere flex-basert 60/40-splitt hadde
samme `gap`-regnefeil som punkt 5/6 beskriver: `flex-basis: 60%` + `flex-basis: 38%` + en SEPARAT
`gap` telles OPPÅ hverandre i en flex-rad, så summen overskred 100% og tvang kolonnene til å pakke
om — CSS Grid sin `gap` trekkes derimot ALDRI fra kolonnesporene, løst presist der.

**Punkt 7:** "Programmer"-tildelingsseksjonen i `Behandlerportal/Tildel/Tester` flyttet fra sist
(etter ALLE kategorier) til rett under "Egenproduserte" (alltid kategori-tre-plass 0) — kategori-
løkken splittet i to deler rundt en Razor-malert delegate (`Func<KategoriMedTester, object>`,
IKKE en separat partial-fil — unngikk å måtte sende `EstimertMinutterPerTestId`/
`Prisingskontekst` inn i en egen modellklasse).

**Punkt 8-10 (SMS/e-post-innhold):** "Nye tester tildelt" brukte tidligere ORDRETT samme
ren-tekst-melding for BÅDE SMS og e-post. Splittet i tre: SMS (punkt 10) er nå ALDRI en liste —
kun ÉN lenke til FØRSTE test, uten testnavn, pluss behandlerens navn (`BygSmsMelding`); e-postens
ren-tekst-fallback beholder den gamle, mer detaljerte oppførselen; en NY HTML-variant (punkt 8-9,
`BygEpostHtml`) gir en overskrift, kort forklaring av hva PsyTest er, behandlerens navn, og ÉN
fargelagt knapp til "Min side" i stedet for én lenke per test. `IEmailSender.SendAsync` fikk en
ny, valgfri `htmlBody`-parameter satt SIST i signaturen (ikke midt i den — unngår å knekke
eksisterende positional calls, se fallgruve-lista), `AzureEmailSender` setter nå faktisk
`EmailContent.Html` (var FØR kun `PlainText` uansett innhold, uoppdaget siden ingen tidligere
e-post trengte HTML).

**Punkt 11-12 (Min side, "Ikke besvart"-fanen):** ny "Fjern alle ubesvarte (N)"-samle-knapp
(`OnPostSlettAlleIkkeBesvarteAsync`, looper eksisterende `SlettIkkeFullfortTildelingAsync`).
Sletting (enkelt ELLER samlet) sendte tidligere alltid brukeren tilbake til "Venter på
godkjenning"-fanen (`faner.js` sin hardkodede default = første fane) etter redirect — løst
generisk i `faner.js` (leser `window.location.hash` ved lasting, bruker den som aktiv fane hvis
den matcher en reell fane i AKKURAT DENNE fane-beholderen), begge slette-handlerne redirecter nå
til `...MinSide#ikke-besvart` i stedet for en ren `RedirectToPage()`.

**Punkt 13-14 (rapportvisning):** en hjemmeoppgave-rapport viste tidligere INGENTING på forsiden
(ingen `TestSkaaring` finnes for fritt forfattede hjemmeoppgaver — cutoff/sumskår gir ikke
mening der) — ny "Svaroversikt"-seksjon på forsiden viser nå rå svar direkte (samme tabell som de
påfølgende per-side-arkene bruker). Ny `AntallLedd`/`AntallUbesvart` (telt via den eksisterende
"-"-sentinelen for manglende svar) vist som "X av Y spørsmål sto ubesvart" i BÅDE den vanlige
Resultat-seksjonen og den nye Svaroversikten, kun når > 0. Gjort i begge Areas (Behandlerportal +
Admin sine separate, nesten identiske Rapport-sider).

**Punkt 15:** Hjemmeoppgaver/Programmer sine `<table>`-elementer (bygget forrige runde) manglet
`border="1" cellpadding="6" cellspacing="0"` — site.css sin paddings-regel er gated bak
`table[border]`, så disse tre tabellene hadde praktisk talt null cellepolstring. Lagt til.

**Punkt 16:** `.fane-knapp` (delt av Min side, Grupper, Hjemmeoppgaver, Programmer) fikk en
tydelig pille/chip-stil (bakgrunn i hvile, fylt rolle-farget aktiv-tilstand) i stedet for nesten
usynlig flat tekst + tynn understrek — "hard to understand it is a clickable button".

**Punkt 17 (reell, uoppdaget visningsbug):** en hjemmeoppgave/et program DELT med alle/partner
viste seg ALDRI i "Delt"/"Partner"-fanene for EIEREN selv — `HentDeltMedAlleAsync`/
`HentDeltMedPartnerAsync` ekskluderte eksplisitt `OpprettetAvBehandlerId == behandlerId`, så
eieren fikk ingen visuell bekreftelse på at delingen faktisk virket. Fjernet ekskluderingen i
BEGGE `HjemmeoppgaveService` og `ProgramService` (samme bug fantes i `ProgramService` sin
`HentDeltMedAlleAsync` også, funnet ved kodegjennomgang — `HentDeltMedPartnerAsync` for programmer
hadde den aldri). Viewet markerer nå egen rad med "(din egen)"/"(ditt eget)" + et nøytralt ikon i
stedet for en meningsløs Lik-knapp på egen oppgave.

**Punkt 18:** ny, generisk `wwwroot/js/tabell-tall-justering.js` (lastet globalt via
`_Layout.cshtml`) senterjusterer automatisk enhver tabellkolonne der ALT innhold ser
tallaktig/datoaktig ut (tall, prosent, beløp, `dd.mm.yyyy`, klokkeslett, brøk) — tekstkolonner
(navn, status) og handlingskolonner (knapper/skjema/lenker) røres aldri. En tabell som inneholder
NOEN `colspan`-celle hoppes bevisst helt over (en colspan forskyver DOM-cellenes indeks per rad,
så ren posisjonsbasert kolonne-sammenligning ville gitt feil resultat for blandede rad-typer, som
Hjemmeoppgaver/Programmer sine "Personlig"-rader med `colspan="3"` på navnecellen blandet med
rader uten colspan i samme tabell) — tryggere å la en slik tabell stå urørt enn å risikere feil
senterjustering.

Alle 18 punkter browser-verifisert (Playwright), inkludert en reell ende-til-ende-utfylling av en
hjemmeoppgave med ett bevisst ubesvart ledd for å bekrefte punkt 13/14 sammen. 92/92
integrasjonstester grønne gjennom hele runden, ingen regresjon. Fem commits, delt tematisk
(programmer-editor/tildelingsflyt, SMS/e-post, Min side-sletting, rapportvisning,
tabellpolish+delt-synlighet) — se commit-historikken for nøyaktig filomfang per tema.

## Reell produksjonsbug: hjemmeoppgaver kunne ikke tildeles for en partner-tilknyttet behandler (2026-10-06)

Brukeren rapporterte på live: "if i assign a test to a patient (it is a homework) it says it is
sent out, but it never goes out" — bekreftet med brukeren at tildelingen IKKE engang vises på
pasientens egen side (ikke bare et varslingsproblem).

**Diagnose:** lastet ned dagens live App Service-logger (`az webapp log download`) og søkte etter
`INSERT INTO test_tildelinger` — FINGEN forekomst i hele dagens logghistorikk, til tross for flere
rapporterte tildelingsforsøk (både "Begge" og "Epost"-varslingsmetode prøvd). Dette utelukket et
SMS/e-post-leveringsproblem (som ville krevd minst én `TestTildeling`-rad å varsle om) og pekte
mot at selve tildelingen aldri ble opprettet.

**Rotårsak:** `TestTildelingsService.TildelOgVarsleAsync` håndhever `PartnerTestTilganger`
(Superadmin-kuratert allow-list for det ADMIN-FORFATTEDE, pris­satte testkatalog-biblioteket) for
ENHVER partner-tilknyttet behandler sin tildeling — inkludert hjemmeoppgaver. Men
`HjemmeoppgaveService.OpprettAsync` (hjemmeoppgavenes egen opprettelsesmetode, atskilt fra
`TestService.OpprettTestAsync`) gir BEVISST ALDRI noen partner automatisk `PartnerTestTilgang`
(dette var allerede eksplisitt dokumentert i klassens egen XML-doc fra fase 0 av hjemmeoppgave-
arbeidet — "riktig for admin-forfattede tester... GALT for en hjemmeoppgave som skal starte
PRIVAT") — men denne bevisste designbeslutningen ble aldri speilet i selve håndhevelsen.
Resultatet: `testIder` ble filtrert til TOM for enhver hjemmeoppgave før selve tildelingsløkken
kjørte. Siden den filtreringen skjer FØR løkken, ble heller ingen "Ikke tildelt"-forklaring
generert (den mekanismen dekker kun `KreverBiologiskKjonn`/`FyllesUtAvBehandler`-avvisninger) —
resultatet var en helt STILLE no-op med en `TildelingsBatchResultat` som så vellykket ut
(`Model.Resultat is not null` → "Tildeling fullført"-overskriften vises), men uten noen synlig
detalj siden `Lenker` var tom for alle pasienter.

Denne bugen har eksistert siden hjemmeoppgaver ble lagt til (2026-10-04) — ikke noe introdusert av
noen senere brukerfeedback-runde. Den rammer ENHVER partner-tilknyttet behandler som prøver å
tildele EN HVILKEN SOM HELST hjemmeoppgave (egen, delt-med-alle fra en kollega, eller delt-med-
partner fra en kollega) — ikke bare brukerens egen test.

**Fiks:** `TildelOgVarsleAsync` henter nå FØRST hvilke av de valgte `testIder` som faktisk er
hjemmeoppgaver (`Test.ErHjemmeoppgave`), og ekskluderer disse eksplisitt fra
`PartnerTestTilganger`-håndhevelsen (en test slipper gjennom hvis den ENTEN er en hjemmeoppgave
ELLER står på allow-listen) — ingen endring for admin-forfattede tester, som fortsatt håndheves
akkurat som før. Siden ekskluderingen kun sjekker `ErHjemmeoppgave`-flagget (aldri eierskap),
dekker den automatisk ALLE tre synlighetskildene en behandler kan tildele fra (egen, delt-med-
alle, delt-med-partner) uten noen ekstra kode.

To nye regresjonstester i `BetalingPipelineTests.cs`
(`PartnerbehandlerKanTildeleEgenHjemmeoppgaveSelvOmDenIkkeErPaaAllowList` og
`PartnerbehandlerKanTildeleKollegasDelteHjemmeoppgaver`, sistnevnte dekker BÅDE delt-med-alle og
delt-med-partner i samme test siden de deler rotårsak) — 94/94 totalt, alle grønne. Den
eksisterende `PartnerbehandlerKanIkkeTildeleTestUtenforAllowList`-testen (admin-forfattede tester
skal FORTSATT håndheves) ble kjørt på nytt og er uendret grønn, bekrefter at fiksen ikke åpner noe
sikkerhetshull for den opprinnelige, tiltenkte bruken av allow-listen.
