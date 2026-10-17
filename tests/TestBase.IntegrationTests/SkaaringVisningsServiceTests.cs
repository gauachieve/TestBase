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
/// MDR-gating (2026-10-17, se docs/mdr-klassifiseringsvurdering-iib-begrunnelse.pdf):
/// SkaaringVisningsService skal skjule beregnet skåre for en EKTE pasient (har
/// personnummer) med mindre Superadmin eksplisitt har slått DriftsbryterInnstilling.
/// AutomatiskSkaaringErAktiv PÅ for dette miljøet — mens en "prøvepasient" (intet
/// personnummer, se GruppeService.TellProvedataAsync) alltid skal se skåring.
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class SkaaringVisningsServiceTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public SkaaringVisningsServiceTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Behandler> OpprettBehandlerAsync(AppDbContext db, string suffix)
    {
        var behandler = new Behandler
        {
            MobilNr = $"+479000{suffix}", Email = $"skaaringvisning-{suffix}@example.test",
            Fornavn = "Skaaringvisning", Etternavn = "Testbehandler",
            Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();
        return behandler;
    }

    [Fact]
    public async Task EktPasientMedPersonnummer_SkalIkkeViseSkaaring_NaarDriftsbryterErAv()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var skaaringVisning = scope.ServiceProvider.GetRequiredService<SkaaringVisningsService>();
        var behandler = await OpprettBehandlerAsync(db, "001");

        var pasient = new Pasient
        {
            Personnummer = "01019011111", MobilNr = "+4791000011", Email = "ekte-av@example.test",
            Navn = "Ekte Pasient Av", BehandlerId = behandler.Id,
            Status = PasientStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(pasient);
        await db.SaveChangesAsync();

        // DriftsbryterInnstilling.AutomatiskSkaaringErAktiv defaulter til false — ingen eksplisitt setting her.
        var skalVise = await skaaringVisning.SkalViseSkaaringAsync(pasient.Id);

        Assert.False(skalVise);
    }

    [Fact]
    public async Task EktPasientMedPersonnummer_SkalViseSkaaring_NaarDriftsbryterErEksplisittPaa()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var skaaringVisning = scope.ServiceProvider.GetRequiredService<SkaaringVisningsService>();
        var driftsbrytere = scope.ServiceProvider.GetRequiredService<DriftsbryterInnstillingService>();
        var behandler = await OpprettBehandlerAsync(db, "002");

        var pasient = new Pasient
        {
            Personnummer = "01019022222", MobilNr = "+4791000022", Email = "ekte-paa@example.test",
            Navn = "Ekte Pasient Paa", BehandlerId = behandler.Id,
            Status = PasientStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(pasient);
        await db.SaveChangesAsync();

        await driftsbrytere.SettAutomatiskSkaaringErAktivAsync(true, endretAvUserId: 1);

        var skalVise = await skaaringVisning.SkalViseSkaaringAsync(pasient.Id);

        Assert.True(skalVise);

        // Rydd opp slik at påfølgende tester i samme delte database ikke påvirkes
        // av at bryteren ble stående på (xUnit kjører denne samlingen sekvensielt,
        // men rekkefølgen mellom testklasser er ikke garantert).
        await driftsbrytere.SettAutomatiskSkaaringErAktivAsync(false, endretAvUserId: 1);
    }

    [Fact]
    public async Task PrøvepasientUtenPersonnummer_SkalAlltidViseSkaaring_UansettDriftsbryter()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var skaaringVisning = scope.ServiceProvider.GetRequiredService<SkaaringVisningsService>();
        var driftsbrytere = scope.ServiceProvider.GetRequiredService<DriftsbryterInnstillingService>();
        var behandler = await OpprettBehandlerAsync(db, "003");

        // Eksplisitt AV, for å bevise at prøvepasienter er uavhengig av bryteren.
        await driftsbrytere.SettAutomatiskSkaaringErAktivAsync(false, endretAvUserId: 1);

        var provepasient = new Pasient
        {
            Personnummer = null, MobilNr = "+4791000033", Email = "prove-pasient@example.test",
            Navn = "Prøv Systemet Pasient", BehandlerId = behandler.Id,
            Status = PasientStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Pasienter.Add(provepasient);
        await db.SaveChangesAsync();

        var skalVise = await skaaringVisning.SkalViseSkaaringAsync(provepasient.Id);

        Assert.True(skalVise);
    }

    [Fact]
    public async Task SettAutomatiskSkaaringErAktivAsync_PersisteresOgReflekteresINesteHentAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var driftsbrytere = scope.ServiceProvider.GetRequiredService<DriftsbryterInnstillingService>();

        await driftsbrytere.SettAutomatiskSkaaringErAktivAsync(true, endretAvUserId: 42);
        var etterPaa = await driftsbrytere.HentAsync();
        Assert.True(etterPaa.AutomatiskSkaaringErAktiv);
        Assert.Equal(42, etterPaa.SistEndretAvUserId);

        await driftsbrytere.SettAutomatiskSkaaringErAktivAsync(false, endretAvUserId: 42);
        var etterAv = await driftsbrytere.HentAsync();
        Assert.False(etterAv.AutomatiskSkaaringErAktiv);
    }
}
