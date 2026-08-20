# Del 1 — Utviklingsmiljø og deploy-pipeline (revidert)

*Sist oppdatert: 2026-08-19 (revidert etter hosting-pivot til Azure). Antagelse markert med [ANTAKELSE] må bekreftes med bruker før implementering.*

## Mål

Rask, enkel lokal utvikling — uten VPN, uten fjernservere, uten treg feilsøking — kombinert med en automatisk, sikker deploy-pipeline til en administrert skyløsning (Azure) når kode er klar til å deles/testes av andre eller gå i produksjon.

## Byggeblokker

**1. Kodestruktur (monorepo).** Én Git-repo med mappene `Admin/`, `Behandler/`, `Pasient/`, `TestEngine/`, `Shared/`, hver som et ASP.NET Core-prosjekt i én .NET-solution. [ANTAKELSE: brukers eksisterende versjonskontrollsystem er Git — bekreftet 2026-08-19.]

**2. Lokalt utviklingsmiljø (rent lokalt, ingen sky).**
   - `docker-compose.yml` med en MySQL-container (samme versjon som brukes i Azure Database for MySQL, for å unngå "virker lokalt, ikke i sky"-overraskelser) fylt med syntetiske testdata — aldri ekte pasientdata lokalt.
   - `dotnet watch` for hot reload — kodeendring til synlig resultat i nettleser på sekunder, helt uavhengig av nett/sky.
   - Mock-implementasjoner av BankID, Vipps, SMS og e-post bak samme grensesnitt (`IBankIdProvider`, `IVippsClient`, osv.) som brukes i prod — i dev logges "sendte meldinger" og "innlogginger" bare til konsoll/en lokal debug-side i stedet for å faktisk kalle eksterne tjenester. Ingen ventetid, ingen kostnad, ingen avhengighet av ekte avtaler for å kunne utvikle.
   - Autorisasjons- og audit-logg-kode er alltid aktiv, også lokalt (se prinsippet i beslutningsloggen) — men peker på en lokal, ukryptert nøkkel i dev i stedet for Azure Key Vault.

**3. Bygg.** Standard `dotnet publish`. Ingen egen zip/kopier-til-server-logikk lenger nødvendig — det håndteres av sky-deployen.

**4. Sky-deploy (Azure).**
   - Applikasjon: **Azure App Service** (Linux, .NET-runtime), én instans for dev/test-miljø og én for produksjon (separate App Service-slots eller separate apper), i **Norway East**-regionen.
   - Database: **Azure Database for MySQL – Flexible Server**, samme region, med automatisk kryptering i hvile og geo-redundant backup innebygd.
   - Hemmeligheter/nøkler: **Azure Key Vault** — ingen passord eller API-nøkler i kode eller config-filer.
   - Deploy skjer via `git push` til Azure (Azure App Service støtter direkte Git-deploy) eller et enkelt GitHub Actions-workflow som bygger og deployer automatisk ved push til `main`/ved ny tag — uansett hvor bruker sitter, ingen VPN eller RDP nødvendig, siden det ikke er noen fysisk server å nå.
   - Database-migrations (EF Core) kjøres automatisk som del av deploy-steget, mot dev/test-databasen ved push til en utviklingsgren, og mot prod-databasen kun ved en tagget release.

**5. Versjonering.** Trunk-based utvikling med korte feature-branches. Hver produksjonssetting skjer fra en Git-tag (`v0.1.0`, `v0.2.0` …) — aldri fra en uttagget commit. Azure App Service beholder tidligere deployments/slots, så rollback er en swap tilbake til forrige slot, ikke en ny utrulling.

**6. Miljøadskillelse.** Egen dev/test App Service + egen dev/test-database, adskilt fra produksjon på ressursnivå i Azure (egne resource groups), ikke bare egne skjema. Ingen ekte pasientdata i dev/test noensinne.

**7. Observability fra dag én.** Application Insights koblet til App Service — strukturerte logger, request-traces, feilrapportering. Målet er at feil som kun opptrer i sky-miljøet kan diagnostiseres via logger i stedet for direkte feilsøking mot live-instansen.

**8. Backup.** Dekkes nå av Azure Database for MySQL sin innebygde geo-redundante backup (kravet om "automatisk backup til ekstern sky" er dermed i praksis oppfylt av selve hostingvalget) — vi bør likevel definere en eksplisitt restore-test-rutine i fase 2/3, siden en backup man aldri har testet å gjenopprette fra ikke er pålitelig.

## Brukerens arbeidsflyt etter dette er på plass

1. Skriv kode lokalt mot Docker-compose-MySQL, se endringer live med `dotnet watch` — ingen nett/sky nødvendig i det hele tatt for vanlig utvikling.
2. Commit og push til en feature-branch → automatisk deploy til dev/test-miljø i Azure for å se koden kjøre i et miljø som ligner prod.
3. Tag en release (`v0.x.0`) og merge til `main` → automatisk deploy til produksjon.
4. Ved feil som kun opptrer i sky-miljøet: se Application Insights-logger/traces først, fremfor å feilsøke direkte mot live-instansen.

## Status 2026-08-19

Kodeskjelettet for punkt 1–2 (kodestruktur, lokalt utviklingsmiljø) er levert som en zip-fil / mappe (`TestBase.sln`, `src/TestBase.Web`, `src/TestBase.Shared`, `docker-compose.yml`) — se `README.md` i rotmappen for hvordan du kommer i gang. Punkt 4 (sky-deploy til Azure) gjenstår og tas når brukers Azure-konto er opprettet.

## Åpne spørsmål før videre implementering

- Har bruker allerede en Azure-konto/abonnement, eller må dette opprettes?
- Ønsket deploy-mekanisme: enkel `git push` direkte til Azure, eller GitHub Actions-workflow (litt mer oppsett, men bedre historikk/kontroll over hva som deployes når)?
- Bekreft at `dotnet build` går gjennom uten feil på brukers maskin.
