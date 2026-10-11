using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;

namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Bugliste 2026-10-11 punkt 4 — klinikk-admin konfigurerer to lister (ved ny pasient / ved
/// avsluttet), og denne tjenesten utfører selve auto-utsendelsen når en av de to hendelsene
/// faktisk skjer (kalt FRA Pasienter/Ny.cshtml.cs, Pasienter/Gruppeimport.cshtml.cs og
/// Pasienter/Index.cshtml.cs sine Arkiver-handlere — se PartnerAutomatiskUtsendelse sin egen
/// XML-doc). Bygger videre på den allerede eksisterende TestTildelingsService.TildelOgVarsleAsync
/// (samme gjenbruk-prinsipp som ProgramService/GruppeService), ingen ny tildelings-/varslingslogikk.
/// </summary>
public sealed class PartnerAutomatiskUtsendelseService
{
    private readonly AppDbContext _db;
    private readonly TestTildelingsService _tildelingsService;

    public PartnerAutomatiskUtsendelseService(AppDbContext db, TestTildelingsService tildelingsService)
    {
        _db = db;
        _tildelingsService = tildelingsService;
    }

    public sealed record KonfigurertTest(long TestId, string TestNavn);

    public async Task<IReadOnlyList<KonfigurertTest>> HentForPartnerAsync(
        long partnerId, PartnerAutomatiskUtsendelseUtloser utloser, CancellationToken cancellationToken = default)
    {
        var rader = await (
            from u in _db.PartnerAutomatiskUtsendelser
            join t in _db.Tester on u.TestId equals t.Id
            where u.PartnerId == partnerId && u.Utloser == utloser
            orderby t.Navn
            select new KonfigurertTest(t.Id, t.Navn)
        ).ToListAsync(cancellationToken);
        return rader;
    }

    public IReadOnlySet<long> ForhaandsvalgteTestIder(IReadOnlyList<KonfigurertTest> konfigurert) =>
        konfigurert.Select(k => k.TestId).ToHashSet();

    /// <summary>Erstatter HELE listen for denne klinikken+utløseren med nøyaktig de valgte testene — enklere og mer forutsigbart enn å diffe inn/ut.</summary>
    public async Task OppdaterAsync(
        long partnerId, PartnerAutomatiskUtsendelseUtloser utloser, IReadOnlyList<long> testIder,
        long opprettetAvBehandlerId, CancellationToken cancellationToken = default)
    {
        var eksisterende = await _db.PartnerAutomatiskUtsendelser
            .Where(u => u.PartnerId == partnerId && u.Utloser == utloser)
            .ToListAsync(cancellationToken);
        _db.PartnerAutomatiskUtsendelser.RemoveRange(eksisterende);

        foreach (var testId in testIder.Distinct())
        {
            _db.PartnerAutomatiskUtsendelser.Add(new PartnerAutomatiskUtsendelse
            {
                PartnerId = partnerId,
                TestId = testId,
                Utloser = utloser,
                OpprettetAvBehandlerId = opprettetAvBehandlerId,
                OpprettetUtc = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stille no-op hvis pasientens behandler ikke tilhører en klinikk, eller klinikken ikke har
    /// konfigurert noen tester for denne utløseren — BEVISST feiltolerant på samme måte som resten
    /// av varslingskjeden (se TestTildelingsService.VarsleAsync sin egen kommentar): dette kalles
    /// fra midten av en ellers vellykket pasient-opprettelse/arkivering, og skal ALDRI kunne velte
    /// den.
    /// </summary>
    public async Task UtforTriggerAsync(
        Pasient pasient, PartnerAutomatiskUtsendelseUtloser utloser, string baseUrl, CancellationToken cancellationToken = default)
    {
        var partnerId = await _db.Behandlere.Where(b => b.Id == pasient.BehandlerId).Select(b => b.PartnerId).FirstOrDefaultAsync(cancellationToken);
        if (partnerId is null)
        {
            return;
        }

        var testIder = await _db.PartnerAutomatiskUtsendelser
            .Where(u => u.PartnerId == partnerId.Value && u.Utloser == utloser)
            .Select(u => u.TestId)
            .ToListAsync(cancellationToken);
        if (testIder.Count == 0)
        {
            return;
        }

        await _tildelingsService.TildelOgVarsleAsync(
            new[] { pasient.Id }, testIder, behandlerId: pasient.BehandlerId, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?>(), baseUrl: baseUrl,
            varslingsmetode: pasient.Varslingspreferanse, cancellationToken: cancellationToken);
    }
}
