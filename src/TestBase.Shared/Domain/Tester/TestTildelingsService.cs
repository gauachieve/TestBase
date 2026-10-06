using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Én pasient i tildelingsflytens pasient-steg, med behandlernavn slått opp
/// (kun fylt ut når admin ser ALLE pasienter på tvers av behandlere — null
/// når en behandler kun ser sine egne, se <see cref="TestTildelingsService.HentTilgjengeligePasienterAsync"/>).
/// </summary>
public sealed record PasientMedBehandlernavn(Pasient Pasient, string? BehandlerNavn);

public sealed record TestLenke(long TildelingId, string TestNavn, string Lenke);

/// <summary>
/// Resultatet for én pasient etter en batch-tildeling — se <see cref="TestTildelingsService.TildelOgVarsleAsync"/>.
/// <paramref name="IkkeTildelteTesterGrunnet"/> er null/tom i det vanlige tilfellet — fylt ut KUN
/// når én eller flere av de valgte testene ble HOPPET OVER for akkurat denne pasienten (per nå:
/// Test.KreverBiologiskKjonn uten Pasient.BiologiskKjonnVedFodsel satt), slik at behandler/admin
/// får en tydelig forklaring i stedet for at tildelingen stille forsvinner.
/// </summary>
public sealed record TildeltPasientResultat(
    long PasientId, string? Navn, IReadOnlyList<TestLenke> Lenker, bool SendtSms, bool SendtEpost,
    IReadOnlyList<string>? IkkeTildelteTesterGrunnet = null);

/// <summary>
/// Én behandler-utfylt test (se Test.FyllesUtAvBehandler) opprettet i denne
/// batchen — ALDRI sendt til pasienten (ingen lenke, ingen SMS/e-post), lenken
/// her peker til Behandlerportal/Pasienter/FyllForPasient i stedet for
/// Pasientportal/Tester/Fyll. Se TestTildelingsService.TildelOgVarsleAsync.
/// </summary>
public sealed record BehandlerOppgave(long TildelingId, string TestNavn, long PasientId, string? PasientNavn, string Lenke);

public sealed record TildelingsBatchResultat(IReadOnlyList<TildeltPasientResultat> PerPasient, IReadOnlyList<BehandlerOppgave>? BehandlerOppgaver = null);

/// <summary>
/// Prisingskonteksten for ÉN behandler, brukt til å vise en levende
/// pris-oppsummering i tildelingsdialogen FØR innsending (se
/// wwwroot/js/tildel.js og Behandlerportal/Tildel/Tester.cshtml) — samme
/// input-verdier som TestTildelingsService sin egen (private)
/// BeregnPrisPerTestAsync bruker på skrivetidspunktet.
/// </summary>
public sealed record PrisingskontekstForBehandler(
    bool DekketAvAbonnement, IReadOnlyDictionary<long, decimal> EffektivPartnerAndelPerTestId, decimal SmsGebyrKr);

/// <summary>
/// Tildelingsflyten: behandler ELLER admin velger flere pasienter og flere
/// tester (via kategori-treet, se TestService.HentKategoriTreAsync) og sender
/// dem i ett steg, jf. beslutningsloggen "Tildelingsflyt for tester". Bygger
/// videre på TestService.TildelAsync (én tildeling om gangen) med
/// kryssproduktet av valgte pasienter × tester, og varsler hver pasient på
/// kanalen(e) hen valgte ved registrering (se Varslingspreferanse) — med
/// fallback til hva pasienten faktisk har av kontaktinfo hvis preferansen
/// ikke kan oppfylles (f.eks. "kun SMS" valgt, men mobilnummer mangler).
/// </summary>
public sealed class TestTildelingsService
{
    private readonly AppDbContext _db;
    private readonly TestService _testService;
    private readonly TestPrisberegner _prisberegner;
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestTildelingsService> _logger;

    public TestTildelingsService(
        AppDbContext db, TestService testService, TestPrisberegner prisberegner, ISmsSender sms, IEmailSender email,
        IConfiguration configuration, ILogger<TestTildelingsService> logger)
    {
        _db = db;
        _testService = testService;
        _prisberegner = prisberegner;
        _sms = sms;
        _email = email;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gebyr lagt til pasientens totalpris når varslingsmetoden for batchen
    /// inkluderer SMS (se TildelOgVarsleAsync/Beregn) — dekker den reelle
    /// kostnaden ved SMS-utsending (Vonage). Konfigurerbar uten redeploy
    /// (samme mønster som Varsling:BaseUrl), 0 kr inntil satt til et reelt tall.
    /// </summary>
    private decimal HentSmsGebyrKr() =>
        _configuration.GetValue<decimal?>("Priser:SmsGebyrKr") ?? 0m;

    /// <summary>
    /// Leses av tildelingssiden sin OnGetAsync for å bygge en levende
    /// pris-forhåndsvisning i oppsummerings-dialogen (JS speiler
    /// TestPrisberegner.Beregn) FØR noe faktisk sendes inn — se
    /// PrisingskontekstForBehandler.
    /// </summary>
    public async Task<PrisingskontekstForBehandler> HentPrisingskontekstAsync(
        long behandlerId, IReadOnlyList<long> testIder, CancellationToken cancellationToken = default)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
        if (behandler is null)
        {
            return new PrisingskontekstForBehandler(false, new Dictionary<long, decimal>(), HentSmsGebyrKr());
        }

        var partner = behandler.PartnerId is null
            ? null
            : await _db.Partnere.FirstOrDefaultAsync(p => p.Id == behandler.PartnerId, cancellationToken);
        var dekketAvAbonnement = behandler.HarEgetAbonnement || (partner?.HarAktivtAbonnement ?? false);

        var effektivPartnerAndel = new Dictionary<long, decimal>();
        if (partner is not null)
        {
            var tester = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);
            var andeler = await _db.PartnerTestAndeler
                .Where(a => a.PartnerId == partner.Id && testIder.Contains(a.TestId))
                .ToDictionaryAsync(a => a.TestId, cancellationToken);

            foreach (var testId in testIder)
            {
                if (!tester.TryGetValue(testId, out var test))
                {
                    continue;
                }

                effektivPartnerAndel[testId] = Math.Max(andeler.GetValueOrDefault(testId)?.AndelKr ?? 0m, test.MinstePartnerAndelKr);
            }
        }

        return new PrisingskontekstForBehandler(dekketAvAbonnement, effektivPartnerAndel, HentSmsGebyrKr());
    }

    /// <summary>
    /// <paramref name="behandlerId"/> null → admin ser ALLE ikke-arkiverte pasienter
    /// (med behandlernavn slått opp); satt → behandler ser kun sine egne.
    /// </summary>
    public async Task<IReadOnlyList<PasientMedBehandlernavn>> HentTilgjengeligePasienterAsync(
        long? behandlerId, CancellationToken cancellationToken = default)
    {
        var sporring = _db.Pasienter.Include(p => p.Gruppe).Where(p => p.Status != PasientStatus.Arkivert);
        if (behandlerId is not null)
        {
            sporring = sporring.Where(p => p.BehandlerId == behandlerId.Value);
        }

        var pasienter = await sporring.OrderBy(p => p.Navn).ToListAsync(cancellationToken);

        if (behandlerId is not null)
        {
            return pasienter.Select(p => new PasientMedBehandlernavn(p, null)).ToList();
        }

        var behandlere = await _db.Behandlere.ToListAsync(cancellationToken);
        var behandlerNavnById = behandlere.ToDictionary(b => b.Id, b => b.Visningsnavn);
        return pasienter.Select(p => new PasientMedBehandlernavn(p, behandlerNavnById.GetValueOrDefault(p.BehandlerId))).ToList();
    }

    /// <summary>
    /// Oppretter én TestTildeling per (pasient × test) og varsler hver pasient
    /// med lenker til sine nye tester. Når <paramref name="behandlerId"/> er satt,
    /// beregnes og snapshottes også prisen for hver test ÉN gang (identisk for
    /// alle pasienter i denne batchen fra samme behandler, se
    /// docs/beslutningslogg.md "Partner System + Test Monetization") — admin-
    /// direkte tildeling (<paramref name="administratorId"/>) hopper over
    /// prising helt, det finnes ingen behandler å prise på vegne av.
    /// </summary>
    public async Task<TildelingsBatchResultat> TildelOgVarsleAsync(
        IReadOnlyList<long> pasientIder,
        IReadOnlyList<long> testIder,
        long? behandlerId,
        long? administratorId,
        IReadOnlyDictionary<long, decimal?> onsketHonorarKrPerTestId,
        string baseUrl,
        Varslingspreferanse varslingsmetode = Varslingspreferanse.Begge,
        long? ansvarligBehandlerId = null,
        CancellationToken cancellationToken = default)
    {
        var pasienter = await _db.Pasienter.Where(p => pasientIder.Contains(p.Id)).ToListAsync(cancellationToken);

        // Bugliste punkt 13: hvem som skal motta OPPGAVEN+RAPPORTEN for en
        // Test.FyllesUtAvBehandler-tildeling er IKKE lenger blindt "pasientens egen behandler" —
        // se TestTildeling.AnsvarligBehandlerId sin XML-doc for hele rotårsaken. Slår opp status
        // for ALLE involverte behandlere (pasientenes egne + en evt. eksplisitt valgt) i ÉN
        // spørring her, FØR per-pasient-løkken, for å unngå N+1.
        var involverteBehandlerIder = pasienter.Select(p => p.BehandlerId).Distinct().ToList();
        if (ansvarligBehandlerId is not null && !involverteBehandlerIder.Contains(ansvarligBehandlerId.Value))
        {
            involverteBehandlerIder.Add(ansvarligBehandlerId.Value);
        }
        var behandlerStatusPerId = await _db.Behandlere
            .Where(b => involverteBehandlerIder.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Status, cancellationToken);

        // Hentet ÉN gang her (i stedet for én gang for PartnerId + én gang til
        // inni BeregnPrisPerTestAsync) — samme entitet gjenbrukt begge steder,
        // se docs/beslutningslogg.md "Optimalisering før skalering". AsNoTracking
        // siden den kun leses, aldri muteres, i denne metoden.
        var behandler = behandlerId is null
            ? null
            : await _db.Behandlere.AsNoTracking().FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
        var behandlerPartnerId = behandler?.PartnerId;

        // Håndhever partnerens test-allow-list HER også, ikke bare i tre-visningen
        // (HentKategoriTreAsync) — en rå POST med en testId utenfor
        // PartnerTestTilgang skal ikke kunne opprette en tildeling for den, se
        // kjent fallgruve i CLAUDE.md om å kun gate i viewet.
        //
        // REELL BUG funnet og fikset 2026-10-06 (brukeren rapporterte: "assigns a homework test,
        // says sent, never goes out" — bekreftet på live: INGEN ny TestTildeling-rad ble
        // opprettet i det hele tatt, likevel viste siden "Tildeling fullført" uten feilmelding).
        // PartnerTestTilganger er en Superadmin-kuratert allow-list for det ADMIN-FORFATTEDE,
        // PRISEDE testkatalog-biblioteket (se docs/beslutningslogg.md "Partner System + Test
        // Monetization") — en hjemmeoppgave (Test.ErHjemmeoppgave) havner ALDRI der, den har sin
        // EGEN, separate eierskaps-/delingsmodell (Personlig/Delt/Partner-faner, se
        // HjemmeoppgaveService). Filtreringen under FJERNET dermed stille enhver valgt
        // hjemmeoppgave for en partner-tilknyttet behandler FØR selve tildelingsløkken — ingen
        // TestTildeling ble opprettet, ingen varsel ble forsøkt (lenker.Count == 0), OG ingen
        // "Ikke tildelt"-forklaring ble vist (den mekanismen dekker kun KreverBiologiskKjonn/
        // FyllesUtAvBehandler-avvisninger, ikke denne tidligere filtreringen) — resultatet var en
        // helt STILLE no-op med en misvisende suksessmelding. Hjemmeoppgaver ekskluderes nå
        // eksplisitt fra denne allow-list-håndhevelsen.
        var hjemmeoppgaveTestIder = await _db.Tester
            .Where(t => testIder.Contains(t.Id) && t.ErHjemmeoppgave)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (behandlerPartnerId is not null)
        {
            var tillatteTestIder = await _db.PartnerTestTilganger
                .Where(t => t.PartnerId == behandlerPartnerId.Value)
                .Select(t => t.TestId)
                .ToListAsync(cancellationToken);
            testIder = testIder.Where(id => hjemmeoppgaveTestIder.Contains(id) || tillatteTestIder.Contains(id)).ToList();
        }

        var tester = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);
        var inkludererSms = varslingsmetode is Varslingspreferanse.Sms or Varslingspreferanse.Begge;
        var prisPerTestId = await BeregnPrisPerTestAsync(behandler, tester, onsketHonorarKrPerTestId, inkludererSms, cancellationToken);

        var perPasient = new List<TildeltPasientResultat>();
        var behandlerOppgaver = new List<BehandlerOppgave>();
        foreach (var pasient in pasienter)
        {
            var lenker = new List<TestLenke>();
            var ikkeTildelteTester = new List<string>();
            foreach (var testId in testIder)
            {
                var valgtTest = tester.GetValueOrDefault(testId);
                if (valgtTest is { KreverBiologiskKjonn: true } && pasient.BiologiskKjonnVedFodsel is null)
                {
                    ikkeTildelteTester.Add(
                        $"{valgtTest.Navn}: krever registrert biologisk kjønn (kjønnsspesifikk normering), " +
                        $"som {(pasient.Navn ?? "denne pasienten")} ikke har satt ennå.");
                    continue;
                }

                // Behandler-utfylte tester (se Test.FyllesUtAvBehandler — YGTSS-R/MADRS
                // klinikkversjon/SCID-5-PF) sendes ALDRI til pasienten: ingen pris (samme
                // "IkkePakrevd"-prinsipp som prøvepasienter under), ingen lenke i SMS/e-post,
                // ingen oppføring i selve varslingsmeldingen. Behandleren fyller den ut selv
                // på Behandlerportal/Pasienter/FyllForPasient, se BehandlerOppgave.
                var erBehandlerUtfylt = tester.GetValueOrDefault(testId)?.FyllesUtAvBehandler ?? false;

                // Bugliste punkt 13: for en behandler-utfylt test MÅ vi kunne navngi NOEN aktiv
                // behandler som får oppgaven+rapporten — ansvarligBehandlerId (eksplisitt valgt av
                // kalleren) vinner, ellers faller vi tilbake til pasientens egen behandler, MEN
                // kun hvis den kontoen faktisk er aktiv. Er verken valgt eller pasientens egen
                // behandler aktiv, hopper vi over DENNE testen for DENNE pasienten i stedet for å
                // stille opprette en tildeling ingen noensinne vil se (se TestTildeling.
                // AnsvarligBehandlerId sin XML-doc for hele bakgrunnen).
                if (erBehandlerUtfylt)
                {
                    var pasientensBehandlerErAktiv = behandlerStatusPerId.GetValueOrDefault(pasient.BehandlerId) == BehandlerStatus.Aktiv;
                    if (ansvarligBehandlerId is null && !pasientensBehandlerErAktiv)
                    {
                        ikkeTildelteTester.Add(
                            $"{valgtTest?.Navn}: fylles ut av behandler, men {(pasient.Navn ?? "denne pasienten")} sin egen " +
                            "behandler er ikke aktiv (arkivert/ikke fullført registrering) — velg en ansvarlig behandler manuelt for denne testen.");
                        continue;
                    }
                    if (ansvarligBehandlerId is not null && behandlerStatusPerId.GetValueOrDefault(ansvarligBehandlerId.Value) != BehandlerStatus.Aktiv)
                    {
                        ikkeTildelteTester.Add($"{valgtTest?.Navn}: den valgte ansvarlige behandleren er ikke en aktiv konto.");
                        continue;
                    }
                }

                var tildeling = await _testService.TildelAsync(
                    testId, pasient.Id, behandlerId: behandlerId, administratorId: administratorId,
                    frist: null, varighetMinutter: null,
                    ansvarligBehandlerId: erBehandlerUtfylt ? ansvarligBehandlerId : null,
                    cancellationToken: cancellationToken);

                // "Prøv systemet"-pasient (intet personnummer, se PasientInvitasjonService.
                // RegistrerViaQrAsync) skal ALDRI møte betalingsgaten, uansett testens pris —
                // det er selve poenget med terskelen ("sample the system before committing to
                // any payment"). En ekte pasient (har personnummer) følger SAMME betalingsgate
                // som enhver annen tildeling, ingen spesialbehandling — se
                // docs/beslutningslogg.md "Fase 5: betalingsgate for gruppetildelte tester".
                var erProvepasient = string.IsNullOrWhiteSpace(pasient.Personnummer);
                var pris = (erProvepasient || erBehandlerUtfylt) ? null : prisPerTestId.GetValueOrDefault(testId);
                _db.TestTildelingBetalinger.Add(new TestTildelingBetaling
                {
                    TestTildelingId = tildeling.Id,
                    PasientTotalprisKr = pris?.PasientTotalprisKr ?? 0m,
                    BehandlerHonorarKr = pris?.BehandlerHonorarKr ?? 0m,
                    PlattformAndelKr = pris?.PlattformAndelKr ?? 0m,
                    PartnerAndelKr = pris?.PartnerAndelKr,
                    PartnerId = behandlerPartnerId,
                    DekketAvAbonnement = pris?.DekketAvAbonnement ?? false,
                    Status = pris is null || pris.PasientTotalprisKr <= 0m ? BetalingStatus.IkkePakrevd : BetalingStatus.Venter,
                    OpprettetUtc = DateTimeOffset.UtcNow
                });

                if (erBehandlerUtfylt)
                {
                    behandlerOppgaver.Add(new BehandlerOppgave(
                        tildeling.Id,
                        tester.GetValueOrDefault(testId)?.Navn ?? "(ukjent test)",
                        pasient.Id, pasient.Navn,
                        $"{baseUrl.TrimEnd('/')}/Behandlerportal/Pasienter/FyllForPasient/{tildeling.Id}"));
                }
                else
                {
                    lenker.Add(new TestLenke(
                        tildeling.Id,
                        tester.GetValueOrDefault(testId)?.Navn ?? "(ukjent test)",
                        $"{baseUrl.TrimEnd('/')}/Pasientportal/Tester/Fyll/{tildeling.Id}"));
                }
            }
            await _db.SaveChangesAsync(cancellationToken);

            var (sendtSms, sendtEpost) = lenker.Count == 0
                ? (false, false)
                : await VarsleAsync(
                    pasient,
                    BygSmsMelding(behandler?.Visningsnavn, lenker[0]),
                    "Nye tester tildelt i PsyTest",
                    BygEpostTekst(lenker),
                    BygEpostHtml(behandler?.Visningsnavn, baseUrl),
                    varslingsmetode,
                    cancellationToken);
            perPasient.Add(new TildeltPasientResultat(
                pasient.Id, pasient.Navn, lenker, sendtSms, sendtEpost,
                ikkeTildelteTester.Count > 0 ? ikkeTildelteTester : null));
        }

        return new TildelingsBatchResultat(perPasient, behandlerOppgaver);
    }

    /// <summary>
    /// Beregner prisen for hver test ÉN gang for hele batchen — se
    /// TildelOgVarsleAsync. Tar imot en ALLEREDE innlastet <paramref name="behandler"/>
    /// (kalleren har den fra før — ikke hent den på nytt her, se
    /// docs/beslutningslogg.md "Optimalisering før skalering").
    /// </summary>
    private async Task<Dictionary<long, PrisberegningResultat>> BeregnPrisPerTestAsync(
        Behandler? behandler, IReadOnlyDictionary<long, Test> tester,
        IReadOnlyDictionary<long, decimal?> onsketHonorarKrPerTestId, bool inkludererSms, CancellationToken cancellationToken)
    {
        var resultat = new Dictionary<long, PrisberegningResultat>();
        if (behandler is null)
        {
            return resultat;
        }

        var partner = behandler.PartnerId is null
            ? null
            : await _db.Partnere.AsNoTracking().FirstOrDefaultAsync(p => p.Id == behandler.PartnerId, cancellationToken);
        var dekketAvAbonnement = behandler.HarEgetAbonnement || (partner?.HarAktivtAbonnement ?? false);

        foreach (var (testId, test) in tester)
        {
            decimal? effektivPartnerAndelKr = null;
            if (partner is not null)
            {
                var partnerAndel = await _db.PartnerTestAndeler
                    .FirstOrDefaultAsync(a => a.PartnerId == partner.Id && a.TestId == testId, cancellationToken);
                effektivPartnerAndelKr = Math.Max(partnerAndel?.AndelKr ?? 0m, test.MinstePartnerAndelKr);
            }

            resultat[testId] = _prisberegner.Beregn(
                test, dekketAvAbonnement, onsketHonorarKrPerTestId.GetValueOrDefault(testId), effektivPartnerAndelKr,
                smsGebyrKr: inkludererSms ? HentSmsGebyrKr() : 0m);
        }

        return resultat;
    }

    /// <summary>
    /// "Send kopi til pasient" på en godkjent rapport (se Behandlerportal/Pasienter/Rapport.cshtml.cs):
    /// gjør rapporten synlig (samme flagg som den manuelle synlighetsbryteren)
    /// OG varsler pasienten med en lenke, i motsetning til den stille
    /// synlighetsbryteren alene.
    /// </summary>
    public async Task<bool> SendRapportKopiAsync(long tildelingId, string baseUrl, CancellationToken cancellationToken = default)
    {
        var tildeling = await _db.TestTildelinger.FirstOrDefaultAsync(t => t.Id == tildelingId, cancellationToken);
        if (tildeling is null || tildeling.RapportGodkjentUtc is null)
        {
            return false;
        }

        var pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == tildeling.PasientId, cancellationToken);
        if (pasient is null)
        {
            return false;
        }

        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == tildeling.TestId, cancellationToken);
        var lenke = $"{baseUrl.TrimEnd('/')}/Pasientportal/Tester/Rapport/{tildelingId}";
        var melding = $"Rapporten din for {test?.Navn ?? "en test"} er klar. Se den her: {lenke}";

        await _testService.SettRapportSynlighetAsync(tildelingId, true, cancellationToken);
        var (sendtSms, sendtEpost) = await VarsleAsync(pasient, melding, "Rapporten din er klar i PsyTest", melding, null, pasient.Varslingspreferanse, cancellationToken);
        return sendtSms || sendtEpost;
    }

    /// <summary>
    /// <paramref name="varslingsmetode"/> er ALLTID et eksplisitt valg fra
    /// kalleren — for TildelOgVarsleAsync er det behandlerens/adminens valg i
    /// tildelingsdialogen (ikke lenger pasientens lagrede
    /// Varslingspreferanse), for SendRapportKopiAsync er det fortsatt
    /// pasientens egen lagrede preferanse (uendret oppførsel der).
    /// <paramref name="smsTekst"/> og <paramref name="epostTekst"/> er BEVISST separate (bugliste
    /// 2026-10-06 punkt 8-10) — SMS skal være kort og ikke liste opp tester, e-post kan være
    /// lengre og får i tillegg <paramref name="epostHtml"/> (valgfri rikere HTML-variant med en
    /// fargelagt knapp, se BygEpostHtml).
    /// </summary>
    private async Task<(bool SendtSms, bool SendtEpost)> VarsleAsync(
        Pasient pasient, string smsTekst, string epostEmne, string epostTekst, string? epostHtml,
        Varslingspreferanse varslingsmetode, CancellationToken cancellationToken)
    {
        var harMobil = !string.IsNullOrWhiteSpace(pasient.MobilNr);
        var harEpost = !string.IsNullOrWhiteSpace(pasient.Email);

        var vilSms = varslingsmetode is Varslingspreferanse.Sms or Varslingspreferanse.Begge;
        var vilEpost = varslingsmetode is Varslingspreferanse.Epost or Varslingspreferanse.Begge;

        // Hvis valgt metode ikke kan oppfylles i det hele tatt (f.eks. "kun SMS" men
        // mobilnummer mangler), fall tilbake til hva pasienten faktisk har registrert
        // — bedre å varsle på en annen kanal enn å ikke varsle i det hele tatt.
        if (!(vilSms && harMobil) && !(vilEpost && harEpost))
        {
            vilSms = harMobil;
            vilEpost = harEpost;
        }

        var sendSms = vilSms && harMobil;
        var sendEpost = vilEpost && harEpost;

        // BEVISST feiltolerant (try/catch rundt hvert utsendingsforsøk) — se
        // PasientInvitasjonService.SendFullforProfilLenkeAsync sin klassekommentar
        // for full begrunnelse (samme prinsipp her): en SMS-/e-post-leverandør som
        // strupes eller feiler under en brå bølge av samtidige tildelinger (f.eks.
        // et foredrag der 50-100 deltakere registrerer seg i samme QR-gruppe i
        // løpet av minutter) skal ALDRI kunne velte selve tildelingen, som allerede
        // er lagret i databasen på dette tidspunktet. Returnerer faktisk utfall
        // (ikke bare forsøkt), slik at SendtSms/SendtEpost forblir sannferdig.
        var smsOk = false;
        if (sendSms)
        {
            try
            {
                await _sms.SendAsync(pasient.MobilNr, smsTekst, cancellationToken);
                smsOk = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Kunne ikke sende tildelings-SMS til pasient {PasientId} — tildelingen er likevel lagret.", pasient.Id);
            }
        }

        var epostOk = false;
        if (sendEpost)
        {
            try
            {
                await _email.SendAsync(pasient.Email, epostEmne, epostTekst, cancellationToken, epostHtml);
                epostOk = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Kunne ikke sende tildelings-e-post til pasient {PasientId} — tildelingen er likevel lagret.", pasient.Id);
            }
        }

        return (smsOk, epostOk);
    }

    /// <summary>
    /// Ren tekst — fortsatt brukt som e-postens PlainText-fallback (se BygEpostHtml for selve
    /// HTML-varianten) og kan fortsatt liste flere tester, siden en e-post leses i eget tempo.
    /// </summary>
    private static string BygEpostTekst(IReadOnlyList<TestLenke> lenker)
    {
        if (lenker.Count == 1)
        {
            return $"Du har fått en ny test i PsyTest: {lenker[0].TestNavn}. Fyll den ut her: {lenker[0].Lenke}";
        }

        var linjer = lenker.Select(l => $"- {l.TestNavn}: {l.Lenke}");
        return "Du har fått nye tester i PsyTest:\n" + string.Join("\n", linjer);
    }

    /// <summary>
    /// Bugliste 2026-10-06 punkt 10: SMS skal ALDRI liste opp flere tester (for kort/upraktisk
    /// format til det) — kun ÉN lenke til den FØRSTE testen, uten å nevne testens navn, pluss
    /// behandlerens navn når en behandler faktisk gjorde tildelingen (null ved admin-direkte
    /// tildeling, der nevnes PsyTest i stedet).
    /// </summary>
    private static string BygSmsMelding(string? behandlerNavn, TestLenke forsteLenke)
    {
        var avsender = string.IsNullOrWhiteSpace(behandlerNavn) ? "behandleren din i PsyTest" : $"{behandlerNavn} i PsyTest";
        return $"Du har fått en ny test fra {avsender}. Fyll den ut her: {forsteLenke.Lenke}";
    }

    /// <summary>
    /// Bugliste 2026-10-06 punkt 8-9: en EGEN, rikere HTML-variant av "nye tester tildelt"-
    /// e-posten — overskrift, en kort forklaring av hva PsyTest er, behandlerens navn, og en
    /// fargelagt KNAPP til pasientens samlede testliste (Min side) i stedet for å liste opp hver
    /// enkelt test med egen lenke (det gjør fortsatt PlainText-fallbacken, se BygEpostTekst, for
    /// e-postklienter uten HTML-støtte). Inline CSS — e-postklienter respekterer ikke
    /// eksterne/head-plasserte stilark pålitelig.
    /// </summary>
    private static string BygEpostHtml(string? behandlerNavn, string baseUrl)
    {
        var minSideLenke = $"{baseUrl.TrimEnd('/')}/Pasientportal/MinSide";
        var behandlerSetning = string.IsNullOrWhiteSpace(behandlerNavn)
            ? "Behandleren din har sendt deg nye tester å fylle ut."
            : $"<strong>{System.Net.WebUtility.HtmlEncode(behandlerNavn)}</strong> har sendt deg nye tester å fylle ut.";

        return $$"""
            <div style="font-family: Arial, Helvetica, sans-serif; max-width: 480px; margin: 0 auto; color: #1a1a1a;">
                <h1 style="font-size: 1.3rem; color: #0f6d5e;">Nye tester venter på deg</h1>
                <p>{{behandlerSetning}}</p>
                <p style="color: #555;">PsyTest er et sikkert, digitalt testsystem som behandleren din bruker til å sende deg
                   psykologiske tester og spørreskjemaer, og til å dele resultatene med deg etterpå.</p>
                <p style="text-align: center; margin: 2rem 0;">
                    <a href="{{minSideLenke}}"
                       style="background: #0f6d5e; color: #ffffff; text-decoration: none; padding: 0.85rem 1.75rem;
                              border-radius: 999px; font-weight: bold; display: inline-block;">
                        Se dine tester
                    </a>
                </p>
                <p style="color: #888; font-size: 0.85rem;">Fungerer ikke knappen? Lim inn denne lenken i nettleseren: {{minSideLenke}}</p>
            </div>
            """;
    }
}
