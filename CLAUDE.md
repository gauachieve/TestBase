# TestBase — Online testesystem for psykologiske tester

Dette er et flerfase-prosjekt for en privatpraktiserende autorisert psykologspesialist i Norge og kontorfellesskapet hans: et system for å sende psykologiske tester til pasienter online, med skåring, rapporter, sikker lagring av helseopplysninger og betaling (Vipps).

**Les dette dokumentet i sin helhet før du gjør noe.** Prosjektet ble startet i Claude (Cowork) og er nettopp konvertert til Claude Code — alt av beslutninger, arkitektur og status fra det arbeidet er samlet her og i `docs/`, slik at ingenting går tapt i overgangen. Ikke anta noe om prosjektet som ikke står her eller i `docs/` — spesielt ikke om de delene som ennå ikke er bygget (fase 2–6), der er `docs/prosjektbeskrivelse-original.md` fasiten på krav, ikke hukommelse eller gjetting.

## Dokumenter i `docs/` — les i denne rekkefølgen ved behov

1. `docs/prosjektbeskrivelse-original.md` — det opprinnelige kravdokumentet (fra bruker, ordrett). Kilden til sannhet for ALLE funksjonskrav, spesielt Del 2–4 og testdefinisjonen, som ikke er designet i detalj ennå. Les den relevante delen herfra før du designer noe i fase 2 og utover.
2. `docs/beslutningslogg.md` — full historikk over beslutninger tatt, hva som er ferdig, kjente feilsøkingspunkter fra oppsett, og åpne punkter. Dette er masterdokumentet for prosjektstatus fra nå av — hold det oppdatert etter hvert som dere jobber videre.
3. `docs/compliance-dpia-utkast.md` — utkast til risikovurdering (DPIA) for helsedata, basert på Normen og GDPR. Ikke juridisk rådgivning; bør kvalitetssikres av jurist/DPO før ekte pasientdata går i produksjon.
4. `docs/del1-utviklingsmiljo-plan.md` — den opprinnelige planen for utviklingsmiljøet (delvis historisk — se beslutningsloggen for hva som faktisk endte opp implementert).

## Status akkurat nå (2026-08-24)

- **Fase 0** (arkitektur/compliance-grunnlag): ferdig.
- **Fase 1** (lokalt utviklingsmiljø): ferdig og verifisert lokalt. Sky-deploy til Azure er planlagt men ikke satt opp.
- **Fase 2** (admin-skjelett + BankID/2FA-autentisering): **første slice ferdig og verifisert lokalt** — datamodell (Administrator/Behandler/invitasjon/2FA-kode), ekte cookie-basert innlogging (passord i utviklingsmodus, BankID+SMS-2FA-mock i produksjonsmodus), rollebytte for utvikler, og minimal admin-CRUD (opprett/arkiver administrator, inviter/frys/arkiver behandler).
- **Fase 3** (behandlersystem): **første slice ferdig og verifisert lokalt** — BankID+2FA-innlogging for behandler (intet passord-unntak), utvidet egenregistrering (flere felt, brukeravtale, e-post/mobil-verifisering), HPR-godkjenningsflyt (7-dagers prøveperiode), og grunnleggende pasient-CRUD (legg til enkeltvis/gruppeimport, arkiver/gjenopprett). Behandlere kan invitere kollegaer på samme måte som admin.
- For begge faser: pris per test, økonomiske rapporter, backup/restore, organisasjonsstøtte, automatiske test-utsendelser og 10-års auto-sletting er bevisst IKKE gjort — se `docs/beslutningslogg.md` under "Del 2 (slice 1)"/"Del 3 (slice 1)" og "Åpne punkter til senere faser" for detaljer og resterende arbeid.
- Fase 4–6: ikke startet.

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
    Areas/Admin/Pages/     Admin-portalen: Konto (LoggInn/BekreftKode/LoggUt/ByttModus),
                           Administratorer (Index/Ny), Behandlere (Index m/ HPR-godkjenning/Inviter)
                           — beskyttet av "AdminOmrade"-policyen, se Program.cs
    Areas/Behandlerportal/Pages/  Behandler-portalen (fase 3) — MERK: heter "Behandlerportal",
                           ikke "Behandler", for å unngå at Area-navnerommet skygger for
                           domenetypen Behandler (se beslutningsloggen "Del 3 (slice 1)").
                           Konto (LoggInn/BekreftKode/LoggUt/GodkjennAvtale — BankID+2FA kun,
                           intet passord), Behandlere/Inviter (kollega), Pasienter
                           (Index/Ny/Gruppeimport/Detaljer) — "BehandlerOmrade"-policyen
    Pages/                 Forside, /DevDemo, /health, Inviter/Fullfor+Verifiser (offentlig,
                           behandler fullfører egen registrering + kontaktverifisering via
                           invitasjonslenke, med enkelt bot-vern)
    Security/AuthSignIn.cs   Utsteder innloggingscookien (delt mellom admin passord/BankID+2FA
                           og behandler BankID+2FA)
    Security/BotVern.cs      Honeypot + minimumstid-vern for offentlige skjemaer
    Properties/launchSettings.json   (setter ASPNETCORE_ENVIRONMENT=Development)
    appsettings.json / appsettings.Development.json
  TestBase.Shared/       Klassebibliotek (har FrameworkReference til Microsoft.AspNetCore.App
                         for Identity/DataProtection/HttpContextAccessor uten å være Sdk.Web)
    Security/             ICurrentUserContext (+ AuthenticatedCurrentUserContext),
                           IAuditLogger/EfAuditLogger, AuditLogEntry, AppClaimTypes,
                           AdminAuthenticationService, BehandlerAuthenticationService,
                           ToFaktorService (delt 2FA-logikk for begge kontotyper)
    Domain/Administrasjon/  Administrator, Behandler (utvidet i fase 3), BehandlerInvitasjon,
                             BehandlerKontaktVerifisering, ToFaktorKode, Brukeravtale (versjonert
                             lisensavtale-tekst), BehandlerInvitasjonService
    Domain/Pasienter/       Pasient, PasientStatus, PasientInvitasjon, PasientInvitasjonService
    Providers/             IBankIdProvider, IVippsClient, ISmsSender, IEmailSender
    Providers/Mock/        Mock-implementasjoner av alle fire, brukt i dev
    Data/AppDbContext.cs   EF Core-kontekst — ALL databasetilgang skal gå gjennom denne.
                           Personnummer krypteres i hvile via DataProtection (se beslutningsloggen)
    Migrations/            EF Core migrations (generert med dotnet ef)
docker-compose.yml        Lokal MySQL
docs/                      Se over
```

Fremtidige prosjekter (`TestBase.Pasient`, `TestBase.TestEngine` e.l.) er ikke opprettet ennå for
fase 4 — admin- og behandlerflatene endte i stedet opp som Razor Pages Areas
(`Areas/Admin`, `Areas/Behandlerportal`) inni `TestBase.Web`, ikke egne prosjekter. **Viktig:**
ikke gi et fremtidig Area samme navn som en domeneentitet (f.eks. ikke `Areas/Pasient` hvis
`Pasient`-klassen brukes ukvalifisert i kode nestet under `Areas/*`) — se fallgruven under.

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
- Razor Pages' standard `TempData`-serialisering støtter ikke `long` (kaster `InvalidOperationException` ved lagring) — lagre som `string` og parse tilbake med `long.TryParse`, se `Areas/Admin/Pages/Konto/LoggInn.cshtml.cs`/`BekreftKode.cshtml.cs`.
- Personnummer og andre DataProtection-krypterte kolonner kan IKKE slås opp med SQL `WHERE` eller håndheves unikt med en databaseindeks (krypteringen er ikke deterministisk) — sammenlign i minnet i stedet, se `AdminAuthenticationService.FinnVedPersonnummerAsync`/`BehandlerAuthenticationService.FinnVedPersonnummerAsync`.
- Å navngi et Razor Pages Area likt en domeneentitet (f.eks. `Areas/Behandler` når klassen `Behandler` finnes) gjør at C#s navneromsoppslag lar Area-navnerommet skygge for typen i ALL kode nestet under `Areas/*` — kompilatorfeil `CS0118 '<Navn>' is a namespace but is used like a type`. Løst ved å kalle arealet `Behandlerportal` i stedet. Ikke gjenta mønsteret for fremtidige Areas.
- `dotnet ef migrations add` kan feiltolke en kolonne-fjerning + en urelatert ny kolonne som en **rename** når flere kolonner endres samtidig på samme tabell (så skjedde med `FulltNavn`→`Arbeidsadresse` på `behandlere` i fase 3-migrasjonen — ville ha flyttet data feil vei). Les alltid gjennom en generert migrasjon med flere samtidige kolonneendringer før den kjøres; fiks manuelt til drop+add hvis feltene ikke faktisk er samme data.
- Razor Pages' automatiske antiforgery-token vises IDENTISK i flere `<form>`-elementer på samme side (f.eks. én per rad i en tabell) — ved skripting/testing med curl: bruk `grep -o ... | head -1` for å hente kun ÉN forekomst før bruk. Fanger man opp alle forekomster i én shell-variabel, får man et flerlinjers, korrupt token og et 400-svar som ser ut som en ekte antiforgery-feil, men ikke er det.

## Hvordan jobbe videre

1. Resten av Del 3 (behandlersystem) ELLER gå videre til fase 4 (Del 4 — pasientsystem) — begge
   er rimelige neste steg, avhengig av hva bruker prioriterer. Pasientens egen
   fullføringsside/portal (Del 4) er en naturlig fortsettelse siden `PasientInvitasjonService`
   allerede lagrer et gjenbrukbart invitasjonstoken.
2. Rapporter/økonomi/automatiske utsendelser for Del 2 OG Del 3 er begge bevisst utsatt til de
   kan bygges sammen — de er tett koblet til testrammeverket (fase 4–5) og betaling (fase 6).
   Les kravene i `docs/prosjektbeskrivelse-original.md` nøye — det er fortsatt fasiten. Se
   `docs/beslutningslogg.md` under "Åpne punkter til senere faser" for full liste, inkl. mindre
   ting som ble bevisst utsatt (enhetstester, ekte BankID/SMS/e-post-leverandør, ekte CAPTCHA).
3. Oppdater `docs/beslutningslogg.md` etter hvert som beslutninger tas — det er masterdokumentet for prosjektstatus fra nå av, siden Claude Code ikke har tilgang til det opprinnelige claude.ai-prosjektet ("Testdatabase") arbeidet startet i.
4. Bruk samme mønster som i Del 1–3: mock-implementasjoner bak grensesnitt for alt som krever ekte tredjepartsavtaler, ekte pasientdata aldri i dev/test, sikkerhetskode (inkl. kryptering) aktiv i alle miljøer fra starten, og IKKE gi et nytt Razor Pages Area samme navn som en domeneentitet (se fallgruven over).
