# Prosjektstatus og beslutningslogg — Online Testesystem

*Sist oppdatert: 2026-08-23. Dette dokumentet lever gjennom hele prosjektet og skal til enhver tid kunne brukes til å regenerere løsningen med alle beslutninger tatt. Denne kopien ble tatt med inn i Git-repoet 2026-08-20 da prosjektet ble konvertert fra Claude (Cowork) til Claude Code — masterversjonen lå tidligere kun i et claude.ai-prosjekt ("Testdatabase"), som ikke er tilgjengelig fra Claude Code. Denne filen ER nå masterversjonen; oppdater den videre her.*

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
Fase 1: Del 1 — lokalt utviklingsmiljø. **Ferdig (lokal del) 2026-08-20.** Sky-deploy-delen (Azure) er ikke satt opp ennå og tas når vi trenger å driftsette noe.
Fase 2: Del 2 — admin-skjelett + BankID/2FA-autentisering. **Første slice ferdig 2026-08-23** (se "Del 2 (slice 1)" under) — pris/rapporter/backup/org-støtte er bevisst utsatt, se "Åpne punkter".
Fase 3: Del 3 — behandlersystem.
Fase 4: Del 4 — pasientsystem + testmotor (generisk rammeverk).
Fase 5: Første konkrete test (WHO-5) ende-til-ende som mal for fremtidige tester.
Fase 6: Betaling (VIPPS), fakturering, økonomiske rapporter.

Fulle krav for hver del ligger i `docs/prosjektbeskrivelse-original.md` — les den delen som er relevant før du designer videre, ikke stol på hukommelse/sammendrag alene.

## Beslutninger tatt

**Teknologistack:** ASP.NET Core (C#) som backend-rammeverk. Begrunnelse: sterk typing og modenhet for et system som skal driftes i mange år av én person, godt bibliotekstøtte for kryptering (`Microsoft.AspNetCore.DataProtection`, `System.Security.Cryptography`), BankID-integrasjonsbiblioteker finnes for .NET, god MySQL-støtte via **Pomelo.EntityFrameworkCore.MySql**. Razor Pages/MVC for admin- og behandlerflater; ren HTML/JS (evt. lettvekts frontend som Alpine.js/HTMX) for pasient- og testsider for å holde det enkelt og raskt på mobil.

**Database:** MySQL, tilgang via Entity Framework Core + Pomelo-provider, med migrations for versjonering av skjema.

**Compliance-tilnærming:** Bruker har ikke jurist/DPO-vurdering på plass ennå. Et utkast til risikovurdering (DPIA) og Normen-tilpasning ligger i `docs/compliance-dpia-utkast.md`. Dette er et startpunkt bruker bør la en jurist/DPO kvalitetssikre før pasientdata går i reell produksjon — det er ikke i seg selv juridisk rådgivning. **Ingen ekte pasientdata i dev/test noensinne.**

**Leverandørstatus:** Ingen avtaler på plass ennå for BankID-integrasjon, Vipps-forhandler, eller SMS/e-post-utsending. Anskaffelse tas inn som egne deloppgaver, tidligst relevant i fase 2 (BankID/2FA) og fase 6 (Vipps/fakturering). Kandidater å vurdere da: BankID via Signicat eller Criipto, SMS via Link Mobility eller Twilio.

**Versjonskontroll:** Git. Prosjektet ligger i `C:\code\TestBase` på brukers maskin (`gaute-pc`), under versjonskontroll med en fungerende første commit fra 2026-08-20.

**Skifte til Claude Code (2026-08-20):** Prosjektet ble startet i Claude (Cowork), der en sky-til-PC-"bro" for direkte filtilgang aldri fikk kontakt gjennom hele Del 1-arbeidet (til tross for flere forsøk, inkl. reinstallasjon av Claude-appen). Kode ble derfor levert som zip-filer, og bruker kjørte kommandoer selv i egen terminal med veiledning. Bruker konverterte deretter til Claude Code, som kjører direkte lokalt uten noen bro-mekanisme. Denne filen og resten av `docs/`-mappen ble skrevet for at ingenting av konteksten skulle gå tapt i overgangen.

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

**Rollebytte for utvikler:** en egen claim (`AdminClaimTypes.BaseRolle`) lagrer rollen kontoen
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

**Funnet og fikset underveis:** ASP.NET Core sin standard TempData-serialisering
(`DefaultTempDataSerializer`) støtter IKKE `long` — måtte lagres som `string` og parses tilbake
(brukt til å bære administrator-id mellom `Logg-inn` og `Bekreft-kode`-stegene).

**Ikke gjort i denne slicen (se "Åpne punkter"):** ekte BankID-/SMS-/e-post-leverandør (fortsatt
mock), pris per test, økonomiske rapporter, backup/restore, organisasjonsstøtte, enhetstester
for `AdminAuthenticationService`/`BehandlerInvitasjonService`, og polert admin-UI (dagens sider
er funksjonelle, ikke visuelt ferdige).

## Kjente feilsøkingspunkter fra oppsett (til referanse)

- **Docker Desktop "Virtualization support not detected":** Løst ved å aktivere Windows-funksjonene `VirtualMachinePlatform` og `Microsoft-Windows-Subsystem-Linux` via PowerShell (admin) + omstart, selv om Intel VMX/VT-x allerede var aktivert i BIOS.
- **`dotnet ef` "Unable to connect to any of the specified MySQL hosts"**: Oppstår hvis migrasjons-kommandoene kjøres før `docker compose up -d` har startet MySQL-containeren — `ServerVersion.AutoDetect(...)` i `Program.cs` krever en faktisk databasetilkobling selv for `migrations add`.
- **Manglende `launchSettings.json`**: Uten den defaulter appen til Production-miljø (ingen tilkoblingsstreng der) i stedet for Development. Nå lagt til i repoet.
- **"Table ... doesn't exist"**: Skjer hvis migrasjonene ikke er kjørt etter at Docker/MySQL-containeren startet. Kjør `dotnet ef database update` på nytt.
- **`dotnet ef database update` → "Build failed" uten detaljer**: Skjer hvis `dotnet watch run` kjører i et annet vindu og låser build-output-filene (vanlig på Windows). Stopp `dotnet watch run` midlertidig (Ctrl+C), kjør migrasjonen, start appen igjen.

## Åpne punkter til senere faser

- Sky-deploy-pipeline til Azure (resten av Del 1) — tas når vi faktisk trenger å driftsette noe.
- Resten av Del 2: pris per test (fordeling test-system/behandler), økonomiske rapporter
  (uke/måned/kvartal/år), (halv-)automatisk bokføring/utbetaling, backup/restore av
  administrator, organisasjonsstøtte (eksplisitt "skal ikke støttes pt." i kravdokumentet) — alt
  naturlig hjemmehørende sammen med fase 6 (Vipps/fakturering) eller egne deloppgaver.
- Konkret databasedesign (skjema) for pasient/test/rapport — tas i fase 3–4.
- Detaljert BankID- og Vipps-leverandørvalg (Signicat/Criipto, Link Mobility/Twilio) — ekte
  implementasjoner bak `IBankIdProvider`/`ISmsSender`/`IEmailSender`/`IVippsClient` byttes inn
  når avtale er signert; mock brukes fortsatt i dev/test uansett (se prinsippet i toppen av
  dette dokumentet).
- Enhetstester for `AdminAuthenticationService` og `BehandlerInvitasjonService` (ren logikk, ingen
  `HttpContext`-avhengighet — bevisst designet for å være lett å teste, men ikke gjort ennå).
- Behandler-innlogging/-portal (Del 3) — `/Inviter/Fullfor` fullfører i dag kun stamdata, ingen
  reell pålogging for behandlere finnes ennå.
- Polering av admin-UI (dagens Admin-sider er funksjonelle, ikke visuelt ferdige).
- Testrammeverkets datamodell (ledd, sider, skåringsregler, lokalisering) — tas i fase 4–5 sammen med WHO-5.
- Azure-konto opprettes av bruker når vi når sky-deploy-delen.
