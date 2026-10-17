# CE-merking — veikart og statusoversikt

Levende internt dokument (oppdateres etter hvert som arbeidet skrider frem) — motstykket til
`docs/mdr-klassifiseringsvurdering-iib-begrunnelse.pdf` (som er den ferdige, daterte
klassifiseringsbegrunnelsen) og `docs/beslutningslogg.md` (kronologisk historikk). Dette
dokumentet er en **tverrsnitts-statusoversikt**: hva som er gjort, hva som gjenstår, og hvem som
gjør hva, strukturert etter kravene i MDR for en klasse IIb-enhet.

Sist oppdatert: 2026-10-18.

## 1. Kvalifisering og klassifisering

| Punkt | Status | Referanse |
|---|---|---|
| Kvalifisering som MDSW avklart | ✅ Ferdig | DMP veiledningssvar 15.10.2026, egen vurdering |
| Risikoklasse fastsatt | ✅ Ferdig — **klasse IIb**, lagt til grunn for hele skåringsfunksjonen (ingen modulavgrensning) | `docs/mdr-klassifiseringsvurdering-iib-begrunnelse.pdf` |
| Tiltenkt formål (intended purpose) — endelig, låst ordlyd | 🟡 Utkast finnes, bør kvalitetssikres sammen med ekstern rådgiver før det låses i teknisk dokumentasjon | Punkt 2 i klassifiseringsdokumentet |

## 2. Kvalitetsstyringssystem (QMS)

| Punkt | Status |
|---|---|
| ISO 13485-basert kvalitetsstyringssystem | ❌ Ikke startet — kreves for samsvarsvurdering (vedlegg IX) uansett klasse II/III |
| PRRC (Person Responsible for Regulatory Compliance, MDR art. 15) utpekt | ❌ Ikke startet — kan være en ekstern konsulent for et mikroforetak (art. 15(2)) |

## 3. Teknisk dokumentasjonsfil (MDR vedlegg II/III)

| Komponent | Status | Merknad |
|---|---|---|
| Produktbeskrivelse / tiltenkt formål | 🟡 Utkast | Se klassifiseringsdokumentet punkt 2 |
| GSPR-sjekkliste (vedlegg I) | ❌ Ikke startet | |
| Risikostyringsfil (ISO 14971) | ❌ Ikke startet | **Kjent funn som MÅ inn her og utbedres før designfrys:** posisjonsbasert svarindeksering i `ItqSkaaringsberegner`/`Phq9Skaaringsberegner` (se CLAUDE.md "Kjente fallgruver" og beslutningsloggen) — en pasient som hopper over ett ledd kan gi stille feil skår. |
| Programvarelivssyklus-dokumentasjon (IEC 62304) | ❌ Ikke startet | Programvareklasse (A/B/C) må fastsettes først |
| Brukervennlighetsfil (IEC 62366-1) | ❌ Ikke startet | |
| Klinisk evaluering (vedlegg XIV / MEDDEV 2.7/1 Rev. 4) | ❌ Ikke startet | Trolig hovedsakelig litteraturbasert på de enkelte testinstrumentenes egen publiserte validering, pluss egen ytelsesdokumentasjon av skåringsalgoritmen — se punkt 5 under for hvordan ekte pasientdata kan bidra her |

## 4. Samsvarsvurdering og CE-merking

| Punkt | Status |
|---|---|
| Meldt organ valgt | ❌ Ikke startet — DNV (NB 2460, Norge) anbefalt som førstevalg, se `docs/partner-briefing-regulatorisk-radgiver.pdf` |
| QA/RA-rådgiver engasjert | ❌ Ikke startet — se partner-briefingen for forslag |
| Samsvarsvurdering gjennomført (vedlegg IX) | ❌ Ikke startet |
| EU-samsvarserklæring utstedt | ❌ Ikke startet |
| EUDAMED-registrering (SRN) | ❌ Ikke startet |

## 5. Klinisk validering med ekte pasienter (før/parallelt med sertifisering)

Se `docs/beslutningslogg.md` "MDR-klassifisering av TestBase sin skåringsfunksjon" for full
drøfting. Tre spor identifisert:

- **Spor A — tilgjengelig nå, implementert:** skåringsbryteren (`DriftsbryterInnstilling.
  AutomatiskSkaaringErAktiv`, default av) lar ekte behandlere/pasienter bruke hele plattformen i
  live nå, uten at beregnet skåre vises for ekte pasienter.
- **Spor B — ikke igangsatt:** en liten, avgrenset klinisk utprøving (MDR art. 62, søknad til
  BÅDE DMP og REK KULMU) med manuell reskåring som sammenligningsmetode, for å generere ekte
  pasientdata til den kliniske evalueringen. Brukeren har bevisst ikke gått videre med dette ennå.
- **Spor C — ikke igangsatt:** retrospektiv algoritmevalidering på allerede innsamlet data
  (faller trolig utenfor MDR siden ingen "forsøksperson" er involvert).

## 6. Bevisst IKKE forfulgt (brukerbeslutning 2026-10-17)

- Modulavgrensning av testbiblioteket for å gi enkelte lavrisikotester en lavere klasse (IIa) —
  droppet til fordel for én klasse (IIb) for hele produktet, for enkelhets skyld.
- Et eget "fare-alarm"-varslingssystem til behandler ved bekymringsfulle svar (selvmordsrisiko
  m.m.) — vurdert og bevisst forkastet, se beslutningsloggen for den fulle juridiske og kliniske
  begrunnelsen (bransjepraksis hos CheckWare lagt til grunn, brukeren tar selv ansvaret for denne
  avgjørelsen).

## 7. Offentlig kommunisert status

- Ny side `/regulatorisk` på selve nettstedet (lenket fra footer og en "Beta"-merking ved
  logoen i header) — forklarer i enkelt språk at skåringsfunksjonen er under vurdering for
  CE-merking, og at automatisk skåre ikke vises til ekte pasienter før dette er på plass.
