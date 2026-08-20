# TestBase — Online Testesystem

Kodeskjelett for Del 1 (lokalt utviklingsmiljø). Se prosjektdokumentene for full kontekst:
`prosjektstatus-og-beslutningslogg.md`, `del1-utviklingsmiljo-plan.md` og
`compliance-risikovurdering-utkast.md`.

## Viktig — les dette først

Dette skjelettet er skrevet i et miljø uten .NET SDK installert, så **koden
er ikke bygget/kompilert eller kjørt her**. Første steg lokalt bør derfor
være `dotnet build` for å fange opp eventuelle skrivefeil, før du går videre.
Meld gjerne tilbake eventuelle feilmeldinger, så retter jeg dem i neste økt.

## Struktur

```
TestBase.sln
src/
  TestBase.Web/       ASP.NET Core Razor Pages-app (kjørbar)
  TestBase.Shared/     Klassebibliotek: audit-logg, brukerkontekst,
                        leverandørgrensesnitt (BankID/Vipps/SMS/e-post) og
                        mock-implementasjoner av disse
docker-compose.yml      Lokal MySQL-database
```

`Admin/`, `Behandler/`, `Pasient/` og `TestEngine/`-prosjektene legges til
i fase 2–4, når de faktiske funksjonene bygges. `TestBase.Web` er
inngangspunktet foreløpig.

## Kom i gang lokalt

Forutsetter [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) og
Docker Desktop installert.

```bash
# 1. Start lokal MySQL i Docker
docker compose up -d

# 2. Gå til web-prosjektet
cd src/TestBase.Web

# 3. Lag database-migrasjon (kun første gang / etter modellendringer)
dotnet tool install --global dotnet-ef   # hvis du ikke har den fra før
dotnet ef migrations add InitialCreate --project ../TestBase.Shared --startup-project .
dotnet ef database update --project ../TestBase.Shared --startup-project .

# 4. Kjør appen med hot reload
dotnet watch run
```

Åpne `https://localhost:5001` (eller porten som vises i terminalen).
Forsiden viser at oppsettet virker. `/DevDemo` viser mock-implementasjonene
av BankID, Vipps, SMS og e-post i aksjon. `/health` bekrefter at appen når
MySQL-containeren.

## Prinsippet bak koden

- **Ingen ekte eksterne tjenester i dev.** `IBankIdProvider`, `IVippsClient`,
  `ISmsSender` og `IEmailSender` er alle registrert med mock-implementasjoner
  i `Program.cs`. Ekte implementasjoner kobles inn når leverandøravtaler er
  på plass (fase 2 for BankID/SMS/e-post, fase 6 for Vipps) — appkoden som
  bruker grensesnittene trenger ikke endres.
- **Sikkerhetskode er aktiv fra dag én, også i dev.** `IAuditLogger` skriver
  til den lokale MySQL-databasen ved hver relevant handling — samme kode som
  kjører i produksjon, bare mot en lokal, ukryptert database i stedet for
  Azure. `ICurrentUserContext` er i dag en fast dev-stub
  (`DevCurrentUserContext`); fase 2 bytter den ut med ekte BankID-basert
  autentisering.
- **All databasetilgang går gjennom `AppDbContext`** (Entity Framework Core),
  ikke rå SQL spredt i koden — det er dette som gjør det billig å legge til
  kolonnenivå-kryptering for sensitive felt senere, ett sted, i stedet for å
  måtte endre overalt.

## Neste steg (fase 2)

Se `prosjektstatus-og-beslutningslogg.md` for full faseplan. Fase 2 legger
til `TestBase.Admin`-prosjektet, ekte BankID/2FA-autentisering, og
datamodellen for administrator/behandler.
