using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Providers;

namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Én pasient i tildelingsflytens pasient-steg, med behandlernavn slått opp
/// (kun fylt ut når admin ser ALLE pasienter på tvers av behandlere — null
/// når en behandler kun ser sine egne, se <see cref="TestTildelingsService.HentTilgjengeligePasienterAsync"/>).
/// </summary>
public sealed record PasientMedBehandlernavn(Pasient Pasient, string? BehandlerNavn);

public sealed record TestLenke(string TestNavn, string Lenke);

/// <summary>Resultatet for én pasient etter en batch-tildeling — se <see cref="TestTildelingsService.TildelOgVarsleAsync"/>.</summary>
public sealed record TildeltPasientResultat(
    long PasientId, string? Navn, IReadOnlyList<TestLenke> Lenker, bool SendtSms, bool SendtEpost);

public sealed record TildelingsBatchResultat(IReadOnlyList<TildeltPasientResultat> PerPasient);

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

    public TestTildelingsService(AppDbContext db, TestService testService, TestPrisberegner prisberegner, ISmsSender sms, IEmailSender email, IConfiguration configuration)
    {
        _db = db;
        _testService = testService;
        _prisberegner = prisberegner;
        _sms = sms;
        _email = email;
        _configuration = configuration;
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
        var sporring = _db.Pasienter.Where(p => p.Status != PasientStatus.Arkivert);
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
        CancellationToken cancellationToken = default)
    {
        var pasienter = await _db.Pasienter.Where(p => pasientIder.Contains(p.Id)).ToListAsync(cancellationToken);

        var behandlerPartnerId = behandlerId is null
            ? null
            : (await _db.Behandlere.Where(b => b.Id == behandlerId).Select(b => b.PartnerId).FirstOrDefaultAsync(cancellationToken));

        // Håndhever partnerens test-allow-list HER også, ikke bare i tre-visningen
        // (HentKategoriTreAsync) — en rå POST med en testId utenfor
        // PartnerTestTilgang skal ikke kunne opprette en tildeling for den, se
        // kjent fallgruve i CLAUDE.md om å kun gate i viewet.
        if (behandlerPartnerId is not null)
        {
            var tillatteTestIder = await _db.PartnerTestTilganger
                .Where(t => t.PartnerId == behandlerPartnerId.Value)
                .Select(t => t.TestId)
                .ToListAsync(cancellationToken);
            testIder = testIder.Where(tillatteTestIder.Contains).ToList();
        }

        var tester = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);
        var inkludererSms = varslingsmetode is Varslingspreferanse.Sms or Varslingspreferanse.Begge;
        var prisPerTestId = await BeregnPrisPerTestAsync(behandlerId, tester, onsketHonorarKrPerTestId, inkludererSms, cancellationToken);

        var perPasient = new List<TildeltPasientResultat>();
        foreach (var pasient in pasienter)
        {
            var lenker = new List<TestLenke>();
            foreach (var testId in testIder)
            {
                var tildeling = await _testService.TildelAsync(
                    testId, pasient.Id, behandlerId: behandlerId, administratorId: administratorId,
                    frist: null, varighetMinutter: null, cancellationToken: cancellationToken);

                var pris = prisPerTestId.GetValueOrDefault(testId);
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

                lenker.Add(new TestLenke(
                    tester.GetValueOrDefault(testId)?.Navn ?? "(ukjent test)",
                    $"{baseUrl.TrimEnd('/')}/Pasientportal/Tester/Fyll/{tildeling.Id}"));
            }
            await _db.SaveChangesAsync(cancellationToken);

            var (sendtSms, sendtEpost) = await VarsleAsync(pasient, BygMelding(lenker), "Nye tester tildelt i PsyTest", varslingsmetode, cancellationToken);
            perPasient.Add(new TildeltPasientResultat(pasient.Id, pasient.Navn, lenker, sendtSms, sendtEpost));
        }

        return new TildelingsBatchResultat(perPasient);
    }

    /// <summary>Beregner prisen for hver test ÉN gang for hele batchen — se TildelOgVarsleAsync.</summary>
    private async Task<Dictionary<long, PrisberegningResultat>> BeregnPrisPerTestAsync(
        long? behandlerId, IReadOnlyDictionary<long, Test> tester,
        IReadOnlyDictionary<long, decimal?> onsketHonorarKrPerTestId, bool inkludererSms, CancellationToken cancellationToken)
    {
        var resultat = new Dictionary<long, PrisberegningResultat>();
        if (behandlerId is null)
        {
            return resultat;
        }

        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId, cancellationToken);
        if (behandler is null)
        {
            return resultat;
        }

        var partner = behandler.PartnerId is null
            ? null
            : await _db.Partnere.FirstOrDefaultAsync(p => p.Id == behandler.PartnerId, cancellationToken);
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
        var (sendtSms, sendtEpost) = await VarsleAsync(pasient, melding, "Rapporten din er klar i PsyTest", pasient.Varslingspreferanse, cancellationToken);
        return sendtSms || sendtEpost;
    }

    /// <summary>
    /// <paramref name="varslingsmetode"/> er ALLTID et eksplisitt valg fra
    /// kalleren — for TildelOgVarsleAsync er det behandlerens/adminens valg i
    /// tildelingsdialogen (ikke lenger pasientens lagrede
    /// Varslingspreferanse), for SendRapportKopiAsync er det fortsatt
    /// pasientens egen lagrede preferanse (uendret oppførsel der).
    /// </summary>
    private async Task<(bool SendtSms, bool SendtEpost)> VarsleAsync(
        Pasient pasient, string meldingstekst, string epostEmne, Varslingspreferanse varslingsmetode, CancellationToken cancellationToken)
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

        if (sendSms)
        {
            await _sms.SendAsync(pasient.MobilNr, meldingstekst, cancellationToken);
        }

        if (sendEpost)
        {
            await _email.SendAsync(pasient.Email, epostEmne, meldingstekst, cancellationToken);
        }

        return (sendSms, sendEpost);
    }

    private static string BygMelding(IReadOnlyList<TestLenke> lenker)
    {
        if (lenker.Count == 1)
        {
            return $"Du har fått en ny test i PsyTest: {lenker[0].TestNavn}. Fyll den ut her: {lenker[0].Lenke}";
        }

        var linjer = lenker.Select(l => $"- {l.TestNavn}: {l.Lenke}");
        return "Du har fått nye tester i PsyTest:\n" + string.Join("\n", linjer);
    }
}
