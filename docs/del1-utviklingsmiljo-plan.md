# Del 1 — Utviklingsmiljø og deploy-pipeline (plan)

*Sist oppdatert: 2026-08-19 (revidert etter hosting-pivot til Azure). Se `beslutningslogg.md` for hva som faktisk er implementert og verifisert per 2026-08-20 — denne filen er den opprinnelige planen, ikke en statusrapport.*

## Mål

Rask, enkel lokal utvikling — uten VPN, uten fjernservere, uten treg feilsøking — kombinert med en automatisk, sikker deploy-pipeline til en administrert skyløsning (Azure) når kode er klar til å deles/testes av andre eller gå i produksjon.

## Byggeblokker

**1. Kodestruktur.** Én Git-repo. I praksis implementert som `TestBase.sln` med `TestBase.Web` (Razor Pages-app) og `TestBase.Shared` (klassebibliotek: sikkerhet, providers, data) — separate prosjekter for Admin/Behandler/Pasient/TestEngine legges til etter hvert som fase 2–4 bygges, ikke opprettet ennå.

**2. Lokalt utviklingsmiljø (rent lokalt, ingen sky).**
   - `docker-compose.yml` med en MySQL-container, fylt med syntetiske testdata — aldri ekte pasientdata lokalt.
   - `dotnet watch` for hot reload.
   - Mock-implementasjoner av BankID, Vipps, SMS og e-post bak samme grensesnitt (`IBankIdProvider`, `IVippsClient`, `ISmsSender`, `IEmailSender`) som brukes i prod — implementert i `TestBase.Shared/Providers/Mock/`, demonstrert på `/DevDemo`-siden.
   - Autorisasjons- og audit-logg-kode er alltid aktiv, også lokalt — implementert i `TestBase.Shared/Security/`.

**3. Bygg.** Standard `dotnet publish`.

**4. Sky-deploy (Azure) — IKKE implementert ennå, kun planlagt:**
   - Applikasjon: Azure App Service (Linux, .NET-runtime), Norway East-regionen.
   - Database: Azure Database for MySQL – Flexible Server, samme region.
   - Hemmeligheter/nøkler: Azure Key Vault.
   - Deploy via `git push` til Azure eller GitHub Actions.
   - Database-migrations (EF Core) kjøres automatisk som del av deploy-steget.

**5. Versjonering.** Trunk-based utvikling med korte feature-branches. Produksjonssetting fra Git-tag, ikke fra uttagget commit.

**6. Miljøadskillelse.** Egen dev/test App Service + egen dev/test-database, adskilt fra produksjon på ressursnivå i Azure.

**7. Observability.** Application Insights koblet til App Service.

**8. Backup.** Dekkes av Azure Database for MySQL sin innebygde geo-redundante backup.

## Åpne spørsmål (fra opprinnelig planlegging — noen er nå avklart, se beslutningsloggen)

- Azure-konto: bruker har ikke opprettet ennå (per 2026-08-20).
- Deploy-mekanisme: `git push` direkte vs. GitHub Actions — ikke bestemt ennå.
