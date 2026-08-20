# Utkast til risikovurdering (DPIA) og Normen-tilpasning — Online Testesystem

*Sist oppdatert: 2026-08-19 (revidert etter hosting-pivot til Azure). Dette er et startpunkt utarbeidet av Claude basert på offentlig tilgjengelig informasjon om Normen, GDPR og norsk helselovgivning. Det er ikke juridisk rådgivning og erstatter ikke en vurdering fra jurist eller personvernombud (DPO) — men gir et konkret grunnlag den vurderingen kan ta utgangspunkt i.*

## 1. Formål og omfang

Systemet skal behandle helseopplysninger (testresultater, psykologiske skårer, journalrelatert informasjon) om pasienter hos en privatpraktiserende autorisert psykologspesialist og kontorfellesskapets øvrige behandlere. Produksjonsdata lagres i en administrert skyløsning (Azure, Norway-region — se beslutningsloggen), ikke lenger på egen server. Lokalt utviklingsmiljø bruker kun syntetiske/fiktive data.

Dette gjør systemet omfattet av:

- **GDPR artikkel 9** — helseopplysninger er en særlig kategori personopplysninger, med et generelt forbud mot behandling som kun kan unntas på nærmere bestemte grunnlag (bl.a. helsepersonells yrkesutøvelse og formål knyttet til helsehjelp).
- **Helsepersonelloven og pasientjournalloven** — psykologer har lovpålagt journalføringsplikt og taushetsplikt.
- **Normen** (Norm for informasjonssikkerhet og personvern i helse- og omsorgssektoren) — en bransjestandard mange virksomheter i helsesektoren følger, inkludert veiledning spesifikt for små helsevirksomheter. Normen er ikke i seg selv lov, men brukes ofte som referanse for hva som regnes som forsvarlig sikkerhetsnivå, og henger sammen med NSMs grunnprinsipper, ISO 27001/27002 og Cloud Controls Matrix.

**Anbefaling:** Fordi systemet er nytt (ikke et etablert journalsystem), bør det gjennomføres en formell DPIA (Data Protection Impact Assessment / personvernkonsekvensvurdering) før pasientdata går i reell drift — dette er sannsynligvis *pliktig* etter GDPR art. 35 når man innfører et nytt system for særlige kategorier personopplysninger i stor skala. Dette dokumentet er et arbeidsutkast som gjør den jobben lettere, ikke en erstatning for den.

## 2. Datakategorier

- **Direkte identifiserende data:** navn, personnummer/fødselsnummer, mobilnummer, e-post, adresse, biologisk kjønn ved fødsel, kjønn.
- **Helseopplysninger (særlig kategori):** testresultater, skårer, rapporter, journalnotater knyttet til testene.
- **Behandlerdata:** HPR-nummer, kontonummer, arbeidsadresse — persondata, men ikke særlig kategori.
- **Betalingsdata:** transaksjonsreferanser fra Vipps (selve korttall/betalingsdetaljer bør aldri lagres i egen database — Vipps håndterer selve betalingen).
- **Innloggingsdata:** BankID-verifiserte identitetsdata, sesjonstokens, IP-adresser i logger.

## 3. Behandlingsgrunnlag

For pasienters helseopplysninger: behandling nødvendig for helsehjelp/yrkesutøvelse (GDPR art. 9(2)(h), jf. helsepersonelloven), samt eksplisitt samtykke fra pasient til lagring og til testgjennomføring (systemet ber allerede om dette ved registrering, jf. prosjektbeskrivelsen). Begge grunnlag bør dokumenteres samtidig — samtykke alene er sårbart (kan trekkes tilbake), mens helsehjelp-grunnlaget er mer robust for selve journalføringen.

## 4. Tekniske og organisatoriske tiltak (Normen-inspirert)

**Kryptering:**
- All trafikk over TLS 1.2+ (HTTPS).
- Kryptering av data i hvile: dekkes som standardfunksjon av Azure Database for MySQL – Flexible Server, pluss vurdering av kolonnenivå-kryptering for de mest sensitive feltene (personnummer, testresultater) i tillegg.
- Krypterte, geo-redundante backuper (innebygd i Azure-hostingen), med jevnlig testet restore.
- Nøkkelhåndtering via Azure Key Vault, adskilt fra applikasjonskode og data.

**Tilgangsstyring:**
- Rollebasert tilgangskontroll (admin/utvikler, behandler, pasient) som beskrevet i prosjektbeskrivelsen, med prinsippet om minste privilegium — en behandler skal kun se egne pasienter, ikke andre behandleres.
- Sterk autentisering: BankID for admin og behandler, BankID (eller tilsvarende sterk verifisering) for pasient ved testgjennomføring, med tofaktor i tillegg for admin/behandler.
- Fullstendig audit-logg over hvem som har åpnet/endret hvilke pasientdata, når — dette er et kjernekrav i Normen og avgjørende ved eventuelle avvikssaker. Bygges inn i kodearkitekturen fra fase 1 (se `del1-utviklingsmiljo-plan.md`), aktiv i alle miljøer inkludert lokal utvikling.
- Azure IAM/rollebasert tilgang til selve sky-ressursene (hvem kan nå produksjonsdatabasen, hvem kan deploye), adskilt fra applikasjonens egen brukerrolle-modell.

**Driftssikkerhet:**
- Administrert patching/herding følger med Azure App Service og Azure Database for MySQL som plattformtjenester — bortfaller som eget driftsansvar sammenlignet med egen Windows Server.
- Egne, isolerte ressursgrupper for dev/test vs. produksjon i Azure.
- Application Insights for strukturert logging/observability fra dag én, slik at feil kan diagnostiseres uten direkte feilsøking mot live-instansen.

**Avviksbehandling:**
- Rutine for å oppdage, vurdere og varsle sikkerhetsbrudd til Datatilsynet innen 72 timer der det er påkrevd (GDPR art. 33). Kryptering av data kan i praksis frita fra plikten til å varsle *de berørte* (art. 34), men ikke nødvendigvis Datatilsynet.

**Databehandleravtaler:**
- Skriftlig databehandleravtale (DPA) må inngås med enhver tredjepart som får tilgang til eller lagrer helseopplysninger på vegne av virksomheten: Microsoft/Azure (standard DPA tilgjengelig, må aksepteres/gjennomgås), SMS-/e-postleverandør (hvis de ser innhold, ikke bare formidler), BankID-leverandør, og Vipps. Dette bør kartlegges konkret når hver leverandør velges (fase 1 for Azure, fase 2 og 6 for de øvrige).

## 5. Lagringstid

Prosjektbeskrivelsen spesifiserer automatisk sletting av pasientdata etter 10 år, som stemmer med den generelle normen for oppbevaring av pasientjournaler i privat praksis (minst 10 år etter siste journalføring, jf. Psykologforeningens veiledning). Arkiverte (men ikke slettede) pasienter bør ha samme sikkerhetsnivå som aktive.

## 6. Foreløpig risikobilde

| Risiko | Vurdering | Tiltak |
|---|---|---|
| Manglende formell DPIA/jurist-sign-off før produksjonssetting | Høy | Ikke sett pasientdata i reell drift før en jurist/DPO har kvalitetssikret dette dokumentet |
| Mange tredjepartsintegrasjoner (BankID, Vipps, SMS, e-post) uten avtaler ennå | Middels | Kartlegg og signer databehandleravtaler før hver integrasjon settes i produksjon |
| Databehandleravtale med Microsoft/Azure ikke gjennomgått ennå | Middels | Gjennomgå Microsofts standard DPA og datalagringsvilkår før produksjonsdata legges inn, som del av fase 1 |
| Ingen dedikert internkontrollrutine/avvikssystem beskrevet ennå | Middels | Etabler enkel internkontroll (dokumentasjon, avvikslogg) parallelt med teknisk utvikling, jf. Psykologforeningens veiledning for psykologvirksomheter |
| ~~Egen Windows-server som eneste driftsmiljø~~ | Løst 2026-08-19 | Produksjon flyttet til administrert skyløsning (Azure), se hosting-pivot i beslutningsloggen |

## 7. Anbefalt neste steg

1. Bruker deler dette dokumentet med en jurist eller personvernombud for kvalitetssikring, gjerne kombinert med gjeldende Normen v7.0-dokumentasjon fra Helsedirektoratet og Microsofts DPA for Azure.
2. Vi bygger utviklings- og testmiljø (fase 1) med syntetiske/fiktive data — ingen ekte pasientdata før DPIA er sluttført.
3. Endelig teknisk sikkerhetsdesign (kryptering, logging, tilgangsstyring) spesifiseres i detalj når vi når fase 2–4, med dette dokumentet som rammeverk.

## Kilder

- [Normen — Helsedirektoratet](https://www.helsedirektoratet.no/digitalisering-og-e-helse/normen-personvern-og-informasjonssikkerhet/normen)
- [En veileder for psykologvirksomheter — Psykologforeningen](https://www.psykologforeningen.no/medlem/personvern/en-veileder-for-psykologvirksomheter)
- [Oppbevaring av journaler i privat praksis — Psykologforeningen](https://www.psykologforeningen.no/lonn-og-arbeid/privat-praksis/oppstart-av-privat-praksis/oppbevaring-av-journaler-i-privat-praksis)
- [Særlige kategorier av personopplysninger — Datatilsynet](https://www.datatilsynet.no/rettigheter-og-plikter/virksomhetenes-plikter/om-behandlingsgrunnlag/spesielt-om-sarlige-kategorier-av-personopplysninger/)
