# TestBase — Online testesystem for psykologiske tester

Dette er et flerfase-prosjekt for en privatpraktiserende autorisert psykologspesialist i Norge og kontorfellesskapet hans: et system for å sende psykologiske tester til pasienter online, med skåring, rapporter, sikker lagring av helseopplysninger og betaling (Vipps).

**Les dette dokumentet i sin helhet før du gjør noe.** Prosjektet ble startet i Claude (Cowork) og er nettopp konvertert til Claude Code — alt av beslutninger, arkitektur og status fra det arbeidet er samlet her og i `docs/`, slik at ingenting går tapt i overgangen. Ikke anta noe om prosjektet som ikke står her eller i `docs/` — spesielt ikke om de delene som ennå ikke er bygget (fase 2–6), der er `docs/prosjektbeskrivelse-original.md` fasiten på krav, ikke hukommelse eller gjetting.

## Dokumenter i `docs/` — les i denne rekkefølgen ved behov

1. `docs/prosjektbeskrivelse-original.md` — det opprinnelige kravdokumentet (fra bruker, ordrett). Kilden til sannhet for ALLE funksjonskrav, spesielt Del 2–4 og testdefinisjonen, som ikke er designet i detalj ennå. Les den relevante delen herfra før du designer noe i fase 2 og utover.
2. `docs/beslutningslogg.md` — full historikk over beslutninger tatt, hva som er ferdig, kjente feilsøkingspunkter fra oppsett, og åpne punkter. Dette er masterdokumentet for prosjektstatus fra nå av — hold det oppdatert etter hvert som dere jobber videre.
3. `docs/compliance-dpia-utkast.md` — utkast til risikovurdering (DPIA) for helsedata, basert på Normen og GDPR. Ikke juridisk rådgivning; bør kvalitetssikres av jurist/DPO før ekte pasientdata går i produksjon.
4. `docs/del1-utviklingsmiljo-plan.md` — den opprinnelige planen for utviklingsmiljøet (delvis historisk — se beslutningsloggen for hva som faktisk endte opp implementert).

## Status akkurat nå (2026-08-30)

- **Fase 0** (arkitektur/compliance-grunnlag): ferdig.
- **Fase 1** (lokalt utviklingsmiljø): ferdig og verifisert lokalt. Sky-deploy til Azure er planlagt men ikke satt opp.
- **Fase 2** (admin-skjelett + BankID/2FA-autentisering): **første slice ferdig og verifisert lokalt** — datamodell (Administrator/Behandler/invitasjon/2FA-kode), ekte cookie-basert innlogging (passord i utviklingsmodus, BankID+SMS-2FA-mock i produksjonsmodus), rollebytte for utvikler, og minimal admin-CRUD (opprett/arkiver administrator, inviter/frys/arkiver behandler).
- **Fase 3** (behandlersystem): **første slice ferdig og verifisert lokalt** — BankID+2FA-innlogging for behandler (intet passord-unntak), utvidet egenregistrering (flere felt, brukeravtale, e-post/mobil-verifisering), HPR-godkjenningsflyt (7-dagers prøveperiode), og grunnleggende pasient-CRUD (legg til enkeltvis/gruppeimport, arkiver/gjenopprett). Behandlere kan invitere kollegaer på samme måte som admin.
- **Fase 4** (pasientsystem + testmotor): **første slice ferdig og verifisert lokalt** — pasient-egenregistrering (uten kontaktverifisering, BankID etterpå er identitetsbekreftelsen), BankID-innlogging UTEN 2FA (bevisst, jf. kravet), pasientens egen side ("Min side"), og et generisk testmotor-skjelett: admin forfatter tester (sider/ledd/svartyper), behandler tildeler dem til pasienter, pasienten fyller ut side for side med fremdrift/lagre/belønningsside.
- **Fase 5** (WHO-5 ende-til-ende): **første slice ferdig og verifisert lokalt** — generalisert Likert-skala (`LikertSkala`, data-drevet N-punkts skala via `"verdi:tekst"`-par, avdekket av WHO-5s 6-punkts 0–5-skala), en pluggbar skåringsmotor (`ITestSkaaringsberegner`) og regenereringsmekanisme (`IInnebygdTestSeeder`, både dev-seed og admin-knapp) nøkkelen `Test.Kode`, og en behandler-rapportside (per besvarelse + "utvikling over tid" med 10%-signifikansmarkering). WHO-5 er første og eneste innebygde test så langt.
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

Prosjektet er et Git-repo i `C:\code\TestBase`.

## Arkitektur — kort versjon (full begrunnelse i beslutningsloggen)

- **Backend:** ASP.NET Core (C#), .NET 8. Razor Pages.
- **Database:** MySQL via Entity Framework Core + Pomelo-provider, EF Core migrations for skjemaversjonering.
- **Produksjon (planlagt, ikke satt opp):** Azure App Service + Azure Database for MySQL – Flexible Server, Norway-region. IKKE egen Windows Server/IIS — det opprinnelige kravet om dette er revidert bort.
- **Lokal utvikling:** Docker Compose (lokal MySQL-container) + `dotnet watch run`. Bevisst holdt enkelt og sky-fritt for rask iterasjon.
- **Sikkerhetsprinsipp — arkitektur nå, infrastruktur senere:** Tilgangsstyring og audit-logging er bygget inn i kodearkitekturen fra dag én (`TestBase.Shared/Security/`: `ICurrentUserContext`, `IAuditLogger`) og er aktiv i ALLE miljøer, også lokalt — men peker på enkle lokale dummy-nøkler i dev og ekte Azure Key Vault/IAM i prod. Følg dette mønsteret videre: ny sikkerhetsrelatert kode skal alltid være aktiv i dev også, bare med enklere infrastruktur bak.
- **Eksterne leverandører (BankID, Vipps, SMS, e-post):** ingen avtaler inngått ennå. All kode mot disse skal gå bak grensesnitt (`IBankIdProvider`, `IVippsClient`, `ISmsSender`, `IEmailSender`) med mock-implementasjoner i `TestBase.Shared/Providers/Mock/` som brukes i dev, slik at utvikling ikke er avhengig av ekte avtaler. Se `/DevDemo`-siden for eksempel på bruk.
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
                           over tid" ved flere fullførte besvarelser av samme test
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
    Providers/             IBankIdProvider, IVippsClient, ISmsSender, IEmailSender
    Providers/Mock/        Mock-implementasjoner av alle fire, brukt i dev
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
- Etter en Area-omdøping (f.eks. fase 3s `Behandler`→`Behandlerportal`): kjør `grep -r 'href="/GamleNavn/'` over HELE `src/`, ikke stol på å ha funnet alle harde lenker manuelt. Fire slike lenker (`/Behandler/Pasienter/...` i stedet for `/Behandlerportal/Pasienter/...`) overlevde fra fase 3 til fase 5 og ga 404 på "Legg til pasient" — fase 4s opprydding fanget kun ett av flere tilsvarende tilfeller.
- Mock-leverandørene (`MockSmsSender`/`MockEmailSender`) logger KUN via `ILogger` — usynlig i selve nettleser-UI-et, kun synlig i konsollen der `dotnet watch run` kjører. En invitasjonslenke som kun finnes der er i praksis ubrukelig for reell manuell testing i nettleser. Slike tjenester bør returnere lenken/meldingen til kalleren (se `BehandlerInvitasjonResultat`/`PasientInvitasjonResultat` i fase 5s feilrettinger) slik at UI-et kan vise den direkte, i tillegg til mock-loggingen.
- Hvis nettleser-testing ikke reflekterer nylige kodeendringer selv om `dotnet watch run` "kjører": sjekk (1) at nettleseren faktisk peker på porten fra `Properties/launchSettings.json` (`https://localhost:7257`/`http://localhost:5257`) og ikke en gammel manuelt overstyrt port fra en tidligere økt, og (2) om flere/hengende `TestBase.Web.exe`-prosesser (`tasklist`, `netstat -ano | grep <port>`) låser build-outputen uten selv å svare på riktig port — drep de gamle prosessene og start `dotnet watch run` på nytt uten portoverstyring.
- Git Bash (MSYS) konverterer automatisk et kommandolinje-argument som begynner med `/` (f.eks. `curl --data-urlencode "ReturnUrl=/Pasientportal/..."`) til en Windows-sti FØR curl noensinne ser det — verdien som faktisk sendes blir korrupt (`C:/Program Files/Git/Pasientportal/...`), noe som ser ut som en server-side bug (feltet "bindes ikke") men egentlig er testverktøyet som lyver om hva som ble sendt. Sett `MSYS_NO_PATHCONV=1` foran curl-kommandoer som poster verdier med innledende skråstrek.
- Manuell `dotnet run --no-launch-profile --urls "http://localhost:5257"` kan likevel plutselig begynne å 307-redirecte til `https://localhost:7257` (via `app.UseHttpsRedirection()`, som er ubetinget i `Program.cs` — kun `UseHsts`/`UseExceptionHandler` er bak `!IsDevelopment()`) selv når ingen launch-profil brukes. Sett `ASPNETCORE_HTTPS_PORT=` (tom) i tillegg til `--urls` for å hindre at middlewaren likevel klarer å gjette et https-mål å omdirigere til.

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
