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
