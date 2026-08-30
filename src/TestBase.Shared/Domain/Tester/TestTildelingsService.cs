using Microsoft.EntityFrameworkCore;
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
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;

    public TestTildelingsService(AppDbContext db, TestService testService, ISmsSender sms, IEmailSender email)
    {
        _db = db;
        _testService = testService;
        _sms = sms;
        _email = email;
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

    /// <summary>Oppretter én TestTildeling per (pasient × test) og varsler hver pasient med lenker til sine nye tester.</summary>
    public async Task<TildelingsBatchResultat> TildelOgVarsleAsync(
        IReadOnlyList<long> pasientIder,
        IReadOnlyList<long> testIder,
        long? behandlerId,
        long? administratorId,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var pasienter = await _db.Pasienter.Where(p => pasientIder.Contains(p.Id)).ToListAsync(cancellationToken);
        var testNavnById = await _db.Tester.Where(t => testIder.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);

        var perPasient = new List<TildeltPasientResultat>();
        foreach (var pasient in pasienter)
        {
            var lenker = new List<TestLenke>();
            foreach (var testId in testIder)
            {
                var tildeling = await _testService.TildelAsync(
                    testId, pasient.Id, behandlerId: behandlerId, administratorId: administratorId,
                    frist: null, varighetMinutter: null, cancellationToken: cancellationToken);
                lenker.Add(new TestLenke(
                    testNavnById.GetValueOrDefault(testId, "(ukjent test)"),
                    $"{baseUrl.TrimEnd('/')}/Pasientportal/Tester/Fyll/{tildeling.Id}"));
            }

            var (sendtSms, sendtEpost) = await VarsleAsync(pasient, lenker, cancellationToken);
            perPasient.Add(new TildeltPasientResultat(pasient.Id, pasient.Navn, lenker, sendtSms, sendtEpost));
        }

        return new TildelingsBatchResultat(perPasient);
    }

    private async Task<(bool SendtSms, bool SendtEpost)> VarsleAsync(
        Pasient pasient, IReadOnlyList<TestLenke> lenker, CancellationToken cancellationToken)
    {
        var harMobil = !string.IsNullOrWhiteSpace(pasient.MobilNr);
        var harEpost = !string.IsNullOrWhiteSpace(pasient.Email);

        var vilSms = pasient.Varslingspreferanse is Varslingspreferanse.Sms or Varslingspreferanse.Begge;
        var vilEpost = pasient.Varslingspreferanse is Varslingspreferanse.Epost or Varslingspreferanse.Begge;

        // Hvis preferansen ikke kan oppfylles i det hele tatt (f.eks. "kun SMS" men
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
            await _sms.SendAsync(pasient.MobilNr, BygMelding(lenker), cancellationToken);
        }

        if (sendEpost)
        {
            await _email.SendAsync(pasient.Email, "Nye tester tildelt i TestBase", BygMelding(lenker), cancellationToken);
        }

        return (sendSms, sendEpost);
    }

    private static string BygMelding(IReadOnlyList<TestLenke> lenker)
    {
        if (lenker.Count == 1)
        {
            return $"Du har fått en ny test i TestBase: {lenker[0].TestNavn}. Fyll den ut her: {lenker[0].Lenke}";
        }

        var linjer = lenker.Select(l => $"- {l.TestNavn}: {l.Lenke}");
        return "Du har fått nye tester i TestBase:\n" + string.Join("\n", linjer);
    }
}
