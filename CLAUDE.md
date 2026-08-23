# TestBase — Online testesystem for psykologiske tester

Dette er et flerfase-prosjekt for en privatpraktiserende autorisert psykologspesialist i Norge og kontorfellesskapet hans: et system for å sende psykologiske tester til pasienter online, med skåring, rapporter, sikker lagring av helseopplysninger og betaling (Vipps).

**Les dette dokumentet i sin helhet før du gjør noe.** Prosjektet ble startet i Claude (Cowork) og er nettopp konvertert til Claude Code — alt av beslutninger, arkitektur og status fra det arbeidet er samlet her og i `docs/`, slik at ingenting går tapt i overgangen. Ikke anta noe om prosjektet som ikke står her eller i `docs/` — spesielt ikke om de delene som ennå ikke er bygget (fase 2–6), der er `docs/prosjektbeskrivelse-original.md` fasiten på krav, ikke hukommelse eller gjetting.

## Dokumenter i `docs/` — les i denne rekkefølgen ved behov

1. `docs/prosjektbeskrivelse-original.md` — det opprinnelige kravdokumentet (fra bruker, ordrett). Kilden til sannhet for ALLE funksjonskrav, spesielt Del 2–4 og testdefinisjonen, som ikke er designet i detalj ennå. Les den relevante delen herfra før du designer noe i fase 2 og utover.
2. `docs/beslutningslogg.md` — full historikk over beslutninger tatt, hva som er ferdig, kjente feilsøkingspunkter fra oppsett, og åpne punkter. Dette er masterdokumentet for prosjektstatus fra nå av — hold det oppdatert etter hvert som dere jobber videre.
3. `docs/compliance-dpia-utkast.md` — utkast til risikovurdering (DPIA) for helsedata, basert på Normen og GDPR. Ikke juridisk rådgivning; bør kvalitetssikres av jurist/DPO før ekte pasientdata går i produksjon.
4. `docs/del1-utviklingsmiljo-plan.md` — den opprinnelige planen for utviklingsmiljøet (delvis historisk — se beslutningsloggen for hva som faktisk endte opp implementert).

## Status akkurat nå (2026-08-23)

- **Fase 0** (arkitektur/compliance-grunnlag): ferdig.
- **Fase 1** (lokalt utviklingsmiljø): ferdig og verifisert lokalt. Sky-deploy til Azure er planlagt men ikke satt opp.
- **Fase 2** (admin-skjelett + BankID/2FA-autentisering): **første slice ferdig og verifisert lokalt** — datamodell (Administrator/Behandler/invitasjon/2FA-kode), ekte cookie-basert innlogging (passord i utviklingsmodus, BankID+SMS-2FA-mock i produksjonsmodus), rollebytte for utvikler, og minimal admin-CRUD (opprett/arkiver administrator, inviter/frys/arkiver behandler). Pris per test, økonomiske rapporter, backup/restore og organisasjonsstøtte er bevisst IKKE del av dette — se `docs/beslutningslogg.md` under "Del 2 (slice 1)" og "Åpne punkter til senere faser" for detaljer og resterende arbeid.
- Fase 3–6: ikke startet.

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
    Program.cs            Wiring: DB, DataProtection, cookie-auth, autorisasjon, dev-seed
    Areas/Admin/Pages/     Admin-området (fase 2): Konto (LoggInn/BekreftKode/LoggUt/ByttModus),
                           Administratorer (Index/Ny), Behandlere (Index/Inviter) — beskyttet
                           av "AdminOmrade"-policyen, se Program.cs
    Pages/                 Forside, /DevDemo, /health, Inviter/Fullfor (offentlig, behandler
                           fullfører egne stamdata via invitasjonslenke)
    Security/AdminSignIn.cs  Utsteder innloggingscookien (delt mellom passord- og BankID-stien)
    Properties/launchSettings.json   (setter ASPNETCORE_ENVIRONMENT=Development)
    appsettings.json / appsettings.Development.json
  TestBase.Shared/       Klassebibliotek (har FrameworkReference til Microsoft.AspNetCore.App
                         for Identity/DataProtection/HttpContextAccessor uten å være Sdk.Web)
    Security/             ICurrentUserContext (+ AuthenticatedCurrentUserContext, ekte
                           implementasjon fra fase 2), IAuditLogger/EfAuditLogger, AuditLogEntry,
                           AdminAuthenticationService (oppslag/passord/2FA — ren logikk),
                           AdminClaimTypes
    Domain/Administrasjon/  Administrator, Behandler, BehandlerInvitasjon, ToFaktorKode,
                             BehandlerInvitasjonService
    Providers/             IBankIdProvider, IVippsClient, ISmsSender, IEmailSender
    Providers/Mock/        Mock-implementasjoner av alle fire, brukt i dev
    Data/AppDbContext.cs   EF Core-kontekst — ALL databasetilgang skal gå gjennom denne.
                           Personnummer krypteres i hvile via DataProtection (se beslutningsloggen)
    Migrations/            EF Core migrations (generert med dotnet ef)
docker-compose.yml        Lokal MySQL
docs/                      Se over
```

Fremtidige prosjekter (`TestBase.Behandler`, `TestBase.Pasient`, `TestBase.TestEngine` e.l.) er ikke opprettet ennå for fase 3–4 — admin-området endte i stedet opp som et Razor Pages Area (`Areas/Admin`) inni `TestBase.Web`, ikke et eget prosjekt; vurder om samme mønster passer for behandler/pasient-flatene når fase 3 starter, eller om de bør være egne prosjekter.

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
- Personnummer og andre DataProtection-krypterte kolonner kan IKKE slås opp med SQL `WHERE` eller håndheves unikt med en databaseindeks (krypteringen er ikke deterministisk) — sammenlign i minnet i stedet, se `AdminAuthenticationService.FinnVedPersonnummerAsync`.

## Hvordan jobbe videre

1. Resten av Del 2 (les `docs/prosjektbeskrivelse-original.md` nøye — det er fortsatt fasiten):
   pris per test (fordeling test-system/behandler), økonomiske rapporter
   (uke/måned/kvartal/år), (halv-)automatisk bokføring/utbetaling, backup/restore av
   administrator, organisasjonsstøtte. Se `docs/beslutningslogg.md` under "Åpne punkter til
   senere faser" for full liste, inkl. mindre ting som ble bevisst utsatt (enhetstester for
   `AdminAuthenticationService`/`BehandlerInvitasjonService`, ekte BankID/SMS/e-post-leverandør).
   Naturlig å gjøre dette sammen med/rett før fase 6 (Vipps/fakturering), siden det er tett
   koblet til betaling.
2. Eller gå videre til fase 3 (Del 3 — behandlersystem) hvis bruker heller vil det — behandler-
   invitasjon (stamdata) finnes allerede fra fase 2, men selve behandler-innlogging/-portalen er
   ikke bygget.
3. Oppdater `docs/beslutningslogg.md` etter hvert som beslutninger tas — det er masterdokumentet for prosjektstatus fra nå av, siden Claude Code ikke har tilgang til det opprinnelige claude.ai-prosjektet ("Testdatabase") arbeidet startet i.
4. Bruk samme mønster som i Del 1 og 2: mock-implementasjoner bak grensesnitt for alt som krever ekte tredjepartsavtaler, ekte pasientdata aldri i dev/test, sikkerhetskode (inkl. kryptering) aktiv i alle miljøer fra starten.
