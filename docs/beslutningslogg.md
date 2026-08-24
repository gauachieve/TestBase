# Prosjektstatus og beslutningslogg — Online Testesystem

*Sist oppdatert: 2026-08-24 (fase 4). Dette dokumentet lever gjennom hele prosjektet og skal til enhver tid kunne brukes til å regenerere løsningen med alle beslutninger tatt. Denne kopien ble tatt med inn i Git-repoet 2026-08-20 da prosjektet ble konvertert fra Claude (Cowork) til Claude Code — masterversjonen lå tidligere kun i et claude.ai-prosjekt ("Testdatabase"), som ikke er tilgjengelig fra Claude Code. Denne filen ER nå masterversjonen; oppdater den videre her.*

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
Fase 3: Del 3 — behandlersystem. **Første slice ferdig 2026-08-24** (se "Del 3 (slice 1)" under) — rapporter/økonomi/automatiske utsendelser er bevisst utsatt, se "Åpne punkter".
Fase 4: Del 4 — pasientsystem + testmotor (generisk rammeverk). **Første slice ferdig 2026-08-24** (se "Del 4 (slice 1)" under) — lokalisering, Vipps-betalingssperre, påminnelser og skåring/rapporter er bevisst utsatt, se "Åpne punkter".
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
- Enhetstester for alle tjenestene i `Security`/`Domain` (ren logikk, ingen `HttpContext`-
  avhengighet — bevisst designet for å være lett å teste, men ikke gjort ennå):
  `AdminAuthenticationService`, `BehandlerAuthenticationService`, `PasientAuthenticationService`,
  `ToFaktorService`, `BehandlerInvitasjonService`, `PasientInvitasjonService`, `TestService`.
- Resten av Del 3: rapporter (per pasient/samlet), økonomi-oversikt (genererte tester/forventet
  utbetaling), automatiske test-utsendelser med påminnelser, 10-års auto-sletting av arkiverte
  pasienter (driftsjobb, hører sammen med fase 6).
- Resten av Del 4: Vipps-betalingssperre før utfylling (gjenbruk `IVippsClient`/`MockVippsClient`),
  påminnelser (frist/varighet lagres på `TestTildeling` men håndheves/varsles ikke ennå),
  lokalisering av tester til flere språk (bevisst utsatt til et konkret andrespråk finnes å
  designe mot, trolig med WHO-5 i fase 5).
- Ekte CAPTCHA (hCaptcha/Turnstile) i stedet for dagens enkle honeypot+tidssjekk-vern
  (`BotVern.cs`) — leverandørbeslutning på linje med BankID/Vipps/SMS.
- Skåring og rapportoppsett (per besvarelse og over tid) — bevises ut med WHO-5 i fase 5, jf.
  faseplanen. `TestSvar` lagrer rå svarverdier allerede, klare til å skåres når metodikken finnes.
- Rediger/slett av tester/sider/ledd i admin-forfatterverktøyet (kun opprett i dag).
- Polering av admin-/behandlerportal-/pasientportal-UI (dagens sider er funksjonelle, ikke visuelt ferdige).
- Azure-konto opprettes av bruker når vi når sky-deploy-delen.
