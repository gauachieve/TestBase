using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestBase.IntegrationTests.Infrastructure;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;
using TestBase.Web.Security;
using Xunit;

namespace TestBase.IntegrationTests;

/// <summary>
/// Regresjonstest for den ekte BankID-koblingsflyten lagt til da foretaket ble
/// godkjent av Idura KUN for openid+profile (ikke nnin/nnin_altsub) — se
/// docs/beslutningslogg.md "Tilbakemeldingsverktøy, del 3"/ekte BankID-
/// aktivering. BankID gir oss aldri personnummeret lenger, kun en stabil
/// "sub"-identitet, som må kobles til en eksisterende konto via
/// personnummer ÉN gang (ProfesjonellInnloggingService.KoblOgFullforAsync)
/// før den kan brukes direkte (FullforMedBankIdSubjektAsync). Egen klasse,
/// delt database (se TestBaseCollection) — unike personnummer/AdminId per test.
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class BankIdKoblingTests
{
    private readonly TestBaseWebApplicationFactory _factory;

    public BankIdKoblingTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static HttpContext NyFrittstaendeHttpContext(IServiceProvider services) =>
        new DefaultHttpContext { RequestServices = services };

    [Fact]
    public async Task AdminAuthenticationService_FinnVedBankIdSubjekt_FinnerIngenFoerKoblingOgFinnerEtterKobling()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adminAuth = scope.ServiceProvider.GetRequiredService<AdminAuthenticationService>();

        var admin = new Administrator
        {
            AdminId = "bankid-kobling-admin", MobilNr = "+4790060001", Email = "bankid-kobling-admin@integrationtest.local",
            FulltNavn = "Kobling Testsen", Personnummer = "01012222222", HprNr = "2222222", OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Administratorer.Add(admin);
        await db.SaveChangesAsync();

        Assert.Null(await adminAuth.FinnVedBankIdSubjektAsync("sub-admin-123"));

        await adminAuth.KoblBankIdSubjektAsync(admin.Id, "sub-admin-123");

        var funnet = await adminAuth.FinnVedBankIdSubjektAsync("sub-admin-123");
        Assert.NotNull(funnet);
        Assert.Equal(admin.Id, funnet!.Id);
    }

    [Fact]
    public async Task BehandlerAuthenticationService_FinnVedBankIdSubjekt_FinnerIngenFoerKoblingOgFinnerEtterKobling()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var behandlerAuth = scope.ServiceProvider.GetRequiredService<BehandlerAuthenticationService>();

        var behandler = new Behandler
        {
            MobilNr = "+4790060002", Email = "bankid-kobling-behandler@integrationtest.local",
            Fornavn = "Kobling", Etternavn = "Behandlersen", Personnummer = "01013333333",
            Status = BehandlerStatus.Aktiv, OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Behandlere.Add(behandler);
        await db.SaveChangesAsync();

        Assert.Null(await behandlerAuth.FinnVedBankIdSubjektAsync("sub-behandler-456"));

        await behandlerAuth.KoblBankIdSubjektAsync(behandler.Id, "sub-behandler-456");

        var funnet = await behandlerAuth.FinnVedBankIdSubjektAsync("sub-behandler-456");
        Assert.NotNull(funnet);
        Assert.Equal(behandler.Id, funnet!.Id);
    }

    [Fact]
    public async Task ProfesjonellInnlogging_UkjentBankIdSubjekt_ReturnererTrengerKobling()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var innlogging = scope.ServiceProvider.GetRequiredService<ProfesjonellInnloggingService>();
        var httpContext = NyFrittstaendeHttpContext(scope.ServiceProvider);

        var resultat = await innlogging.FullforMedBankIdSubjektAsync(
            "sub-helt-ukjent-789", huskMeg: false, returnUrl: null, auditlogKilde: "Test", httpContext, CancellationToken.None);

        Assert.True(resultat.TrengerKoblingFlagg);
        Assert.Equal("sub-helt-ukjent-789", resultat.KoblingBankIdSubjekt);
        Assert.False(resultat.ErFerdig);
        Assert.False(resultat.TrengerToFaktorFlagg);
    }

    [Fact]
    public async Task ProfesjonellInnlogging_KoblOgFullforMedRiktigPersonnummer_KoblerKontoenOgKreverToFaktor()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var innlogging = scope.ServiceProvider.GetRequiredService<ProfesjonellInnloggingService>();

        var admin = new Administrator
        {
            AdminId = "bankid-kobleogfullfor-admin", MobilNr = "+4790060003", Email = "bankid-kobleogfullfor@integrationtest.local",
            FulltNavn = "KoblOgFullfor Testsen", Personnummer = "01014444444", HprNr = "4444444", OpprettetUtc = DateTimeOffset.UtcNow
        };
        db.Administratorer.Add(admin);
        await db.SaveChangesAsync();

        var httpContext = NyFrittstaendeHttpContext(scope.ServiceProvider);
        var resultat = await innlogging.KoblOgFullforAsync(
            "sub-skal-kobles-til-admin", "01014444444", huskMeg: false, returnUrl: null, auditlogKilde: "Test", httpContext, CancellationToken.None);

        // Ingen betrodd-enhet-cookie i en fersk HttpContext -> 2FA kreves, IKKE umiddelbart innlogget.
        Assert.True(resultat.TrengerToFaktorFlagg);
        Assert.Equal(admin.Id, resultat.ToFaktorId);

        // Men selve koblingen MÅ ha skjedd uansett — en senere, direkte sub-basert innlogging skal nå finne kontoen.
        var adminAuth = scope.ServiceProvider.GetRequiredService<AdminAuthenticationService>();
        var koblet = await adminAuth.FinnVedBankIdSubjektAsync("sub-skal-kobles-til-admin");
        Assert.NotNull(koblet);
        Assert.Equal(admin.Id, koblet!.Id);
    }

    [Fact]
    public async Task ProfesjonellInnlogging_KoblOgFullforMedFeilPersonnummer_ReturnererFeilOgKobler_Ingenting()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var innlogging = scope.ServiceProvider.GetRequiredService<ProfesjonellInnloggingService>();
        var httpContext = NyFrittstaendeHttpContext(scope.ServiceProvider);

        var resultat = await innlogging.KoblOgFullforAsync(
            "sub-feil-personnummer", "01019999999", huskMeg: false, returnUrl: null, auditlogKilde: "Test", httpContext, CancellationToken.None);

        Assert.False(resultat.ErFerdig);
        Assert.False(resultat.TrengerToFaktorFlagg);
        Assert.False(resultat.TrengerKoblingFlagg);
        Assert.Equal("Fant ingen administrator- eller behandlerkonto med dette personnummeret.", resultat.Feilmelding);

        var adminAuth = scope.ServiceProvider.GetRequiredService<AdminAuthenticationService>();
        Assert.Null(await adminAuth.FinnVedBankIdSubjektAsync("sub-feil-personnummer"));
    }
}
