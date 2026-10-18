using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestBase.IntegrationTests.Infrastructure;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using Xunit;

namespace TestBase.IntegrationTests;

/// <summary>
/// Verifiserer "KOR/FIT til neste time" (se docs/beslutningslogg.md 2026-10-18) —
/// PlanlagtTildelingService.OpprettKorFitAsync skal opprette NØYAKTIG to PlanlagtTildeling-
/// rader (ORS før, SRS etter) med de superadmin-konfigurerbare offsettene faktisk anvendt,
/// ikke hardkodet.
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class KorFitPlanleggingTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public KorFitPlanleggingTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpprettKorFitAsync_OppretterToRader_MedKonfigurerteOffsets()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var planlagtTildelingService = scope.ServiceProvider.GetRequiredService<PlanlagtTildelingService>();
        var korFitOppsettService = scope.ServiceProvider.GetRequiredService<KorFitOppsettService>();

        // Konfigurerbare offsets — bevisst ANDRE verdier enn default (2t/15min), for å bevise at
        // de faktisk LESES, ikke bare matcher default-verdien ved en tilfeldighet. Verdiene
        // (ikke selve entiteten — EF Core sin identity map ville latt en senere "gjenopprett fra
        // samme objekt" bli en no-op) hentes ut FØR mutasjonen, for en reell opprydding etterpå.
        var opprinneligOrsOffset = (await korFitOppsettService.HentAsync()).OrsTimerForTimeOffset;
        var opprinneligSrsOffset = (await korFitOppsettService.HentAsync()).SrsMinutterEtterTimeOffset;
        await korFitOppsettService.LagreAsync(
            new KorFitOppsett { OrsTimerForTimeOffset = 3.0, SrsMinutterEtterTimeOffset = 20 }, endretAvUserId: 1);

        var behandler = new Behandler
        {
            MobilNr = "+4790" + Random.Shared.Next(100000, 999999),
            Email = $"korfit-{Guid.NewGuid():N}@example.test",
            Status = BehandlerStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        var pasient = new Pasient
        {
            Personnummer = null,
            MobilNr = "+4791" + Random.Shared.Next(100000, 999999),
            Email = $"korfit-pasient-{Guid.NewGuid():N}@example.test",
            Navn = "KOR/FIT Testpasient",
            BehandlerId = behandler.Id,
            Status = PasientStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(pasient);
        await db.SaveChangesAsync();

        var nesteTimeUtc = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
        await planlagtTildelingService.OpprettKorFitAsync(
            new[] { pasient.Id }, behandlerId: behandler.Id, administratorId: null, nesteTimeUtc,
            Varslingspreferanse.Begge, opprettetAvUserId: behandler.Id);

        var rader = await db.PlanlagteTildelinger
            .Where(p => p.BehandlerId == behandler.Id)
            .OrderBy(p => p.PlanlagtUtc)
            .ToListAsync();

        Assert.Equal(2, rader.Count);

        var orsTestId = await db.Tester.Where(t => t.Kode == "ors").Select(t => t.Id).FirstAsync();
        var srsTestId = await db.Tester.Where(t => t.Kode == "srs").Select(t => t.Id).FirstAsync();

        var orsRad = Assert.Single(rader, r => r.TestIderCsv == orsTestId.ToString());
        var srsRad = Assert.Single(rader, r => r.TestIderCsv == srsTestId.ToString());

        Assert.Equal(nesteTimeUtc.AddHours(-3.0), orsRad.PlanlagtUtc);
        Assert.Equal(nesteTimeUtc.AddMinutes(20), srsRad.PlanlagtUtc);
        Assert.Equal(pasient.Id.ToString(), orsRad.PasientIderCsv);
        Assert.Equal(pasient.Id.ToString(), srsRad.PasientIderCsv);

        // Rydd opp konfigurasjonen tilbake til det den var for andre tester i samme kjøring.
        await korFitOppsettService.LagreAsync(
            new KorFitOppsett { OrsTimerForTimeOffset = opprinneligOrsOffset, SrsMinutterEtterTimeOffset = opprinneligSrsOffset },
            endretAvUserId: 1);
    }
}
