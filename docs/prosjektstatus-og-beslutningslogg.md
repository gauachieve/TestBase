# Prosjektstatus og beslutningslogg — Online Testesystem

*Sist oppdatert: 2026-08-19 (revidert samme dag). Dette dokumentet lever gjennom hele prosjektet, oppdateres i hver sesjon, og skal til enhver tid kunne brukes til å regenerere løsningen med alle beslutninger tatt (jf. krav i original prosjektbeskrivelse).*

## Kilde

Basert på "Prosjektbeskrivelse Online Testesystem.docx", lastet opp 2026-08-18. Merk: original prosjektbeskrivelse forutsatte egen Windows Server 2016/IIS for alt. Dette er siden revidert av bruker for produksjonsmiljø — se "Hosting-pivot" under.

## Oppsummering av omfang

Et system for at en privatpraktiserende autorisert psykologspesialist og kontorfellesskapet hans kan gjennomføre psykologiske tester online med pasienter, med skåring, rapportgenerering, sikker lagring av helseopplysninger, og betaling. Fire delsystemer:

1. **Utviklingsmiljø** — automatisert deploy/versjonering, fjerntilgang.
2. **Administrasjon** — BankID-pålogging, behandler-/testadministrasjon, prising, økonomiske rapporter, brukerstyring.
3. **Behandlersystem** — pasientadministrasjon, tildeling av tester, rapporter, arkivering.
4. **Pasientsystem** — registrering, gjennomføring av tester, betaling (VIPPS), sletting av egne data.

Pluss et generelt **rammeverk for å definere psykologiske tester** (skåring, rapport, lokalisering), med WHO-5 som første eksempel-test.

Krav på tvers: HTML-grensesnitt (responsivt pc/mobil), kryptering iht. norske lovkrav for helseopplysninger, BankID + 2FA, VIPPS, SMS, e-post, fakturagenerering, automatisk backup/restore, versjonering av alle deler.

## Min vurdering av gjennomførbarhet

Dette er ikke et ett-økt-prosjekt. Det er et flerspors utviklingsprogram som deles i faser over mange sesjoner, med denne loggen som lim mellom sesjonene. Jeg bidrar med arkitektur, datamodell, kode/scaffolding per delsystem, dokumentasjon, og en konkret plan for testrammeverket (inkl. WHO-5). Jeg kan ikke inngå avtaler med BankID-leverandør/Vipps/SMS-leverandør/skyleverandør, eller erstatte juridisk/DPO-vurdering av helsedatahåndtering — dette må brukeren selv gjøre, med kode/instruksjoner fra meg som støtte.

## Foreslått metodikk (faseplan)

Fase 0: Arkitektur- og compliance-grunnlag (denne loggen, datamodell, sikkerhetskrav, valg av teknologistack, DPIA-utkast). **Ferdig.**
Fase 1: Del 1 — lokalt utviklingsmiljø + sky-deploy-pipeline. **Pågår — kodeskjelett levert 2026-08-19.**
Fase 2: Del 2 — admin-skjelett + BankID/2FA-autentisering.
Fase 3: Del 3 — behandlersystem.
Fase 4: Del 4 — pasientsystem + testmotor (generisk rammeverk).
Fase 5: Første konkrete test (WHO-5) ende-til-ende som mal for fremtidige tester.
Fase 6: Betaling (VIPPS), fakturering, økonomiske rapporter.

## Beslutninger tatt

**Teknologistack (2026-08-19):** Bruker overlot valget til meg. Jeg velger **ASP.NET Core (C#)** som backend-rammeverk. Begrunnelse: sterk typing og modenhet for et system som skal driftes i mange år av én person, godt bibliotekstøtte for kryptering (`Microsoft.AspNetCore.DataProtection`, `System.Security.Cryptography`), BankID-integrasjonsbiblioteker finnes for .NET, god MySQL-støtte via **Pomelo.EntityFrameworkCore.MySql**, og kjører like godt som administrert sky-tjeneste som på IIS (se hosting-pivot under). Razor Pages/MVC for admin- og behandlerflater; ren HTML/JS (evt. lettvekts frontend som Alpine.js/HTMX) for pasient- og testsider for å holde det enkelt og raskt på mobil.

**Database:** MySQL, tilgang via Entity Framework Core + Pomelo-provider, med migrations for versjonering av skjema (dekker kravet om "versjoner alle deler" for datamodellen).

**Compliance-tilnærming (2026-08-19):** Bruker har ikke jurist/DPO-vurdering på plass ennå. Vi tar med et utkast til risikovurdering (DPIA) og Normen-tilpasning som del av fase 0 — se eget dokument `compliance-risikovurdering-utkast.md`. Dette er et startpunkt bruker bør la en jurist/DPO kvalitetssikre før pasientdata går i reell produksjon — det er ikke i seg selv juridisk rådgivning.

**Leverandørstatus (2026-08-19):** Ingen avtaler på plass ennå for BankID-integrasjon, Vipps-forhandler, eller SMS/e-post-utsending. Anskaffelse tas inn som egne deloppgaver, tidligst relevant i fase 2 (BankID/2FA) og fase 6 (Vipps/fakturering). Kandidater å vurdere da: BankID via Signicat eller Criipto, SMS via Link Mobility eller Twilio.

**Versjonskontroll og kodested (2026-08-19):** Bekreftet Git. Kode leveres som denne mappen/zip-filen for `git init` i `C:\code\TestBase` på brukers maskin (enhetsbroen til brukers PC var midlertidig utilgjengelig da dette ble bygget).

### Hosting-pivot (2026-08-19, revidert samme dag)

Bruker har revidert det opprinnelige kravet om egen Windows Server 2016/IIS for produksjon. Ny beslutning:

- **Produksjon:** Flyttes til en administrert skyløsning — **Azure** (App Service for applikasjonen, Azure Database for MySQL – Flexible Server for databasen), i **Norway East/West**-regionen for datalagringssted. Begrunnelse: kryptering i hvile, geo-redundant backup, tilgangsstyring (IAM) og sikkerhetsoppdateringer følger med som administrerte tjenester, i stedet for at bruker må bygge og drifte dette selv på en Windows-boks. Dette adresserer direkte risikopunktet "egen server som eneste driftsmiljø" fra `compliance-risikovurdering-utkast.md`. Egen Windows Server droppes helt for produksjon — dermed bortfaller også behovet for VPN/RDP-tilgang til en hjemme-/kontorserver som var planlagt i første utkast av Del 1.
- **Utvikling:** Skjer fortsatt **lokalt**, ikke i skyen — bruker har erfaring med at sky-basert utvikling/debugging er tregt, noe jeg er enig i for selve den daglige kodesyklusen (attache debugger, vente på logger, nettverkslatens). Lokalt utviklingsmiljø: Docker Compose med MySQL i container, `dotnet watch` for rask iterasjon, og mock-implementasjoner av BankID/Vipps/SMS/e-post bak samme grensesnitt som brukes i prod — appen "vet ikke" om den snakker med ekte eller falske tjenester. Se `del1-utviklingsmiljo-plan.md`.
- **Prinsipp for sikkerhet — arkitektur nå, infrastruktur senere:** Tilgangsstyrings- og auditlogg-kode bygges inn fra dag én (sentral data-tilgangs-lag, autorisasjonstjeneste, append-only audit-logg), men kjører i dev mot enkle lokale dummy-nøkler/ukryptert lokal database. Samme kode kjører i prod, bare koblet til ekte nøkler (Azure Key Vault) og ekte tilgangsstyring (Azure IAM). Dette gjør at vi slipper både "bygg alt sikkert fra dag én er tregt" og "vi glemte å bygge det inn og må skrive om alt senere" — det er kun *infrastrukturen bak* koden som trappes opp fra dev til prod, ikke selve kodestien. Dette er nå implementert i kodeskjelettet: se `TestBase.Shared/Security/`.
- **Om sky-debugging:** Vurderingen er at mye av smerten ved å feilsøke sky-hostede systemer kommer av manglende observability, ikke av skyen i seg selv. Vi setter opp strukturert logging + Application Insights fra dag én i prod/staging, slik at feil som kun opptrer i skyen kan diagnostiseres via logger/traces uten å "debugge i skyen" direkte.

## Relaterte prosjektdokumenter

- `compliance-risikovurdering-utkast.md` — utkast til risikovurdering/DPIA basert på Normen og GDPR.
- `del1-utviklingsmiljo-plan.md` — konkret plan for lokalt utviklingsmiljø og sky-deploy-pipeline (Del 1).
- `../README.md` — hvordan kjøre kodeskjelettet som ble levert 2026-08-19.

## Åpne punkter til senere faser

- Konkret databasedesign (skjema) for administrator/behandler/pasient/test/rapport — tas i fase 2–4.
- Detaljert BankID- og Vipps-integrasjonsvalg — tas når vi når fase 2 og 6.
- Testrammeverkets datamodell (ledd, sider, skåringsregler, lokalisering) — tas i fase 4–5 sammen med WHO-5.
- Bekreft at `dotnet build` går gjennom uten feil på brukers maskin (kunne ikke verifiseres i miljøet koden ble skrevet i).
- Azure-konto opprettes av bruker når vi når sky-deploy-delen av fase 1.
