using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TestBase.IntegrationTests.Infrastructure;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Providers;
using Xunit;

namespace TestBase.IntegrationTests;

/// <summary>
/// Dekker VippsBetalingBekrefterService — den delte "sjekk status hos Vipps, fang
/// (capture) om nødvendig, marker betalt"-logikken brukt av BÅDE Vipps-webhooken
/// og BetalResultat-siden (se docs/beslutningslogg.md "Vipps-webhook registrert
/// aldri hos Vipps"). Bruker en lokal FakeVippsClient (ikke MockVippsClient fra
/// DI) siden MockVippsClient alltid rapporterer "Fanget" med det samme og derfor
/// aldri ville øvd på selve AUTORISERT→fang-stien denne tjenesten finnes for å
/// dekke korrekt.
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class VippsBetalingBekrefterServiceTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public VippsBetalingBekrefterServiceTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AutorisertStatus_FangerOgMarkererBetalt()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();
        var tildelingId = await OpprettBetalbarTildelingAsync(db, testService, tildelingsService, "vipps-ok");

        var fakeVipps = new FakeVippsClient(VippsBetalingsstatus.Autorisert, fangingLykkes: true);
        var bekrefter = new VippsBetalingBekrefterService(testService, fakeVipps, NullLogger<VippsBetalingBekrefterService>.Instance);

        var resultat = await bekrefter.BekreftOgFangAsync(tildelingId, "vipps-ref-ok");

        Assert.True(resultat.Betalt);
        Assert.False(resultat.VarAlleredeBetalt);
        Assert.Null(resultat.Feilmelding);
        Assert.Equal(1, fakeVipps.FangKallCount);

        var betaling = await testService.HentBetalingAsync(tildelingId);
        Assert.Equal(BetalingStatus.Betalt, betaling!.Status);
        Assert.Equal(BetalingMetode.Vipps, betaling.Metode);
    }

    [Fact]
    public async Task AutorisertStatusMenFangingFeiler_MarkererIkkeBetalt()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();
        var tildelingId = await OpprettBetalbarTildelingAsync(db, testService, tildelingsService, "vipps-feil");

        var fakeVipps = new FakeVippsClient(VippsBetalingsstatus.Autorisert, fangingLykkes: false);
        var bekrefter = new VippsBetalingBekrefterService(testService, fakeVipps, NullLogger<VippsBetalingBekrefterService>.Instance);

        var resultat = await bekrefter.BekreftOgFangAsync(tildelingId, "vipps-ref-feil");

        Assert.False(resultat.Betalt);
        Assert.NotNull(resultat.Feilmelding);

        // Avgjørende: betalingen skal IKKE være markert Betalt i vår database når
        // fangingen hos Vipps faktisk feilet — dette er nøyaktig scenariet
        // webhook-fiksen finnes for å unngå (se "Kritisk bugfiks: Vipps-betaling
        // ble aldri fanget", docs/beslutningslogg.md).
        var betaling = await testService.HentBetalingAsync(tildelingId);
        Assert.Equal(BetalingStatus.Venter, betaling!.Status);
    }

    [Fact]
    public async Task AlleredeBetalt_ErIdempotentOgFangerIkkePaNytt()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var testService = scope.ServiceProvider.GetRequiredService<TestService>();
        var tildelingsService = scope.ServiceProvider.GetRequiredService<TestTildelingsService>();
        var tildelingId = await OpprettBetalbarTildelingAsync(db, testService, tildelingsService, "vipps-idempotent");
        await testService.MarkerBetalingBetaltAsync(tildelingId, BetalingMetode.Vipps, "vipps-ref-idempotent");

        var fakeVipps = new FakeVippsClient(VippsBetalingsstatus.Autorisert, fangingLykkes: true);
        var bekrefter = new VippsBetalingBekrefterService(testService, fakeVipps, NullLogger<VippsBetalingBekrefterService>.Instance);

        var resultat = await bekrefter.BekreftOgFangAsync(tildelingId, "vipps-ref-idempotent");

        Assert.True(resultat.Betalt);
        Assert.True(resultat.VarAlleredeBetalt);
        // Allerede Betalt skal kortslutte FØR noe Vipps-kall i det hele tatt gjøres.
        Assert.Equal(0, fakeVipps.HentStatusKallCount);
        Assert.Equal(0, fakeVipps.FangKallCount);
    }

    private static async Task<long> OpprettBetalbarTildelingAsync(
        AppDbContext db, TestService testService, TestTildelingsService tildelingsService, string unikSuffiks)
    {
        var tall = Math.Abs(unikSuffiks.GetHashCode()) % 10000;

        var behandler = new Behandler
        {
            MobilNr = $"+479{tall:00000000}",
            Email = $"{unikSuffiks}-behandler@example.test",
            Fornavn = "Vipps",
            Etternavn = "Testbehandler",
            Status = BehandlerStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        var test = await testService.OpprettTestAsync($"Vipps-bekreftelsestest {unikSuffiks}", null, null, kode: null);
        test.MinstePrisKr = 20m;
        test.StorstePrisKr = 500m;
        test.TypiskBehandlerHonorarKr = 80m;
        await db.SaveChangesAsync();

        var pasient = new Pasient
        {
            Personnummer = $"0101909{tall:0000}",
            MobilNr = $"+479{(tall + 1):00000000}",
            Email = $"{unikSuffiks}-pasient@example.test",
            Navn = "Vipps Testpasient",
            BehandlerId = behandler.Id,
            Status = PasientStatus.Aktiv,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(pasient);
        await db.SaveChangesAsync();

        var resultat = await tildelingsService.TildelOgVarsleAsync(
            new[] { pasient.Id }, new[] { test.Id }, behandlerId: behandler.Id, administratorId: null,
            onsketHonorarKrPerTestId: new Dictionary<long, decimal?> { [test.Id] = 80m },
            baseUrl: "https://localhost", cancellationToken: CancellationToken.None);

        var tildelingIdStreng = resultat.PerPasient.Single().Lenker.Single().Lenke.Split('/').Last();
        return long.Parse(tildelingIdStreng);
    }
}

/// <summary>Kontrollerbar IVippsClient KUN for VippsBetalingBekrefterServiceTests — se klassekommentaren der for hvorfor MockVippsClient ikke dekker AUTORISERT→fang-stien.</summary>
internal sealed class FakeVippsClient : IVippsClient
{
    private readonly VippsBetalingsstatus _status;
    private readonly bool _fangingLykkes;

    public int HentStatusKallCount { get; private set; }
    public int FangKallCount { get; private set; }

    public FakeVippsClient(VippsBetalingsstatus status, bool fangingLykkes)
    {
        _status = status;
        _fangingLykkes = fangingLykkes;
    }

    public Task<VippsOpprettetBetaling> OpprettBetalingAsync(string referanse, decimal belopNok, string beskrivelse, string returUrl, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<VippsStatusResultat> HentStatusAsync(string referanse, CancellationToken cancellationToken = default)
    {
        HentStatusKallCount++;
        return Task.FromResult(new VippsStatusResultat(true, _status, null));
    }

    public Task<VippsFangetResultat> FangBetalingAsync(string referanse, decimal belopNok, CancellationToken cancellationToken = default)
    {
        FangKallCount++;
        return Task.FromResult(_fangingLykkes
            ? new VippsFangetResultat(true, null)
            : new VippsFangetResultat(false, "Simulert fangingsfeil (test)"));
    }

    public Task<VippsWebhookRegistrertResultat> RegistrerWebhookAsync(string url, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<VippsWebhookListeResultat> HentWebhookerAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<bool> SlettWebhookAsync(string webhookId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
