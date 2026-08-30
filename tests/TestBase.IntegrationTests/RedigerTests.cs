using System.Text.RegularExpressions;
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
/// Regresjonstest for "Rediger"-sidene (administrator/test/pasient) lagt til
/// sammen med den grønne Rediger-knappen ved siden av Arkiver. Egen klasse
/// fra HeleFlytenTests siden disse testene er uavhengige av hverandre, men
/// deler samme collection/database (se TestBaseCollection) — derfor bruker
/// hver test unike AdminId-/e-post-/mobilverdier, og RedigerPasient-testen
/// arkiverer eventuell tidligere aktiv behandler med samme faste
/// BankID-testpersonnummer i stedet for å anta den er alene om det (se
/// samme mønster i HeleFlytenTests for hvorfor det trengs).
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class RedigerTests
{
    private const string MockPersonnummer = "01019012345";
    private static readonly Regex SeksSifretKode = new(@"\b(\d{6})\b", RegexOptions.Compiled);

    private readonly TestBaseWebApplicationFactory _factory;

    public RedigerTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RedigerAdministrator_ForhandsutfyllerOgLagrerEndringer()
    {
        var client = _factory.CreateClient();
        await LoggInnSomDevAdminAsync(client);

        var nyToken = await SkjemaHjelper.HentTokenAsync(client, "/Admin/Administratorer/Ny");
        await SkjemaHjelper.PostMedTokenAsync(client, "/Admin/Administratorer/Ny", SkjemaHjelper.Felter(
            ("AdminId", "rediger-admin"), ("MobilNr", "+4790050001"), ("Email", "rediger-admin@integrationtest.local"),
            ("FulltNavn", "Original Navn"), ("Personnummer", "01011111111"), ("HprNr", "1111111")), nyToken);

        long adminId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            adminId = (await db.Administratorer.SingleAsync(a => a.AdminId == "rediger-admin")).Id;
        }

        var redigerUrl = $"/Admin/Administratorer/Rediger/{adminId}";
        var redigerHtml = await SkjemaHjelper.GetHtmlAsync(client, redigerUrl);
        Assert.Contains("rediger-admin", redigerHtml);
        Assert.Contains("Original Navn", redigerHtml);

        var redigerToken = SkjemaHjelper.HentToken(redigerHtml);
        var lagreResp = await SkjemaHjelper.PostMedTokenAsync(client, redigerUrl, SkjemaHjelper.Felter(
            ("Id", adminId.ToString()), ("AdminId", "rediger-admin"), ("MobilNr", "+4790050002"),
            ("Email", "rediger-admin@integrationtest.local"), ("FulltNavn", "Nytt Navn"),
            ("Personnummer", "01011111111"), ("HprNr", "1111111")), redigerToken);

        var indexHtml = await SkjemaHjelper.LesHtmlAsync(lagreResp);
        Assert.Contains("Nytt Navn", indexHtml);
        Assert.Contains("btn-icon", indexHtml);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var oppdatert = await db.Administratorer.SingleAsync(a => a.Id == adminId);
            Assert.Equal("Nytt Navn", oppdatert.FulltNavn);
            Assert.Equal("+4790050002", oppdatert.MobilNr);
        }
    }

    [Fact]
    public async Task RedigerTest_ForhandsutfyllerOgLagrerEndringer()
    {
        var client = _factory.CreateClient();
        await LoggInnSomDevAdminAsync(client);

        var nyTestToken = await SkjemaHjelper.HentTokenAsync(client, "/Admin/Tester/Ny");
        var nyTestResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Admin/Tester/Ny", SkjemaHjelper.Felter(
            ("Navn", "Original Testnavn"), ("Beskrivelse", "Original beskrivelse"), ("Belonningstekst", "Bra jobbet")),
            nyTestToken);
        var testId = long.Parse(nyTestResp.RequestMessage!.RequestUri!.PathAndQuery.Split('/').Last());

        var redigerUrl = $"/Admin/Tester/Rediger/{testId}";
        var redigerHtml = await SkjemaHjelper.GetHtmlAsync(client, redigerUrl);
        Assert.Contains("Original Testnavn", redigerHtml);

        var redigerToken = SkjemaHjelper.HentToken(redigerHtml);
        var lagreResp = await SkjemaHjelper.PostMedTokenAsync(client, redigerUrl, SkjemaHjelper.Felter(
            ("Id", testId.ToString()), ("Navn", "Nytt Testnavn"), ("Beskrivelse", "Ny beskrivelse"),
            ("Belonningstekst", "Bra jobbet")), redigerToken);

        var indexHtml = await SkjemaHjelper.LesHtmlAsync(lagreResp);
        Assert.Contains("Nytt Testnavn", indexHtml);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var oppdatert = await db.Tester.SingleAsync(t => t.Id == testId);
            Assert.Equal("Nytt Testnavn", oppdatert.Navn);
            // ErAktiv-checkboksen ble ikke sendt med (avkrysset) -> skal lagres som false.
            Assert.False(oppdatert.ErAktiv);
        }
    }

    [Fact]
    public async Task RedigerPasient_ForhandsutfyllerOgLagrerEndringer()
    {
        long behandlerId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Arkiver enhver tidligere aktiv behandler/administrator med samme faste
            // BankID-testpersonnummer, slik at unified-innlogging entydig finner
            // BEHANDLEREN vi oppretter her (se HeleFlytenTests for samme prinsipp).
            var eksisterendeAdmin = await db.Administratorer.Where(a => !a.ErArkivert).ToListAsync();
            foreach (var a in eksisterendeAdmin.Where(a => a.Personnummer == MockPersonnummer))
            {
                a.ErArkivert = true;
            }

            var eksisterendeBehandlere = await db.Behandlere.Where(b => b.Status != BehandlerStatus.Arkivert).ToListAsync();
            foreach (var b in eksisterendeBehandlere.Where(b => b.Personnummer == MockPersonnummer))
            {
                b.Status = BehandlerStatus.Arkivert;
            }

            await db.SaveChangesAsync();

            var behandler = new Behandler
            {
                MobilNr = "+4790060001",
                Email = "rediger-behandler@integrationtest.local",
                Fornavn = "Rediger",
                Etternavn = "Behandlersen",
                Personnummer = MockPersonnummer,
                HprNr = "2222222",
                RegistrertUtc = DateTimeOffset.UtcNow,
                BrukeravtaleGodkjentVersjon = Brukeravtale.GjeldendeVersjon,
                BrukeravtaleGodkjentUtc = DateTimeOffset.UtcNow,
                Status = BehandlerStatus.Aktiv,
                OpprettetUtc = DateTimeOffset.UtcNow
            };
            db.Behandlere.Add(behandler);
            await db.SaveChangesAsync();
            behandlerId = behandler.Id;
        }

        var client = _factory.CreateClient();

        var (loginHtml0, loginToken, captchaSvar, captchaFasit) =
            await SkjemaHjelper.LastInnloggingsskjemaAsync(client, "/Konto/LoggInn");
        var loginResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/LoggInn",
            SkjemaHjelper.Felter(("CaptchaSvar", captchaSvar), ("CaptchaSignertFasit", captchaFasit)), loginToken);
        var loginHtml = await SkjemaHjelper.LesHtmlAsync(loginResp);
        Assert.Contains("Bekreft SMS-kode", loginHtml);

        var kodeToken = SkjemaHjelper.HentToken(loginHtml);
        var kode = SeksSifretKode.Match(_factory.Sms.SisteMeldingTil("+4790060001") ?? "").Groups[1].Value;
        Assert.NotEmpty(kode);
        await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/BekreftKode", SkjemaHjelper.Felter(("Kode", kode)), kodeToken);

        var nyPasToken = await SkjemaHjelper.HentTokenAsync(client, "/Behandlerportal/Pasienter/Ny");
        await SkjemaHjelper.PostMedTokenAsync(client, "/Behandlerportal/Pasienter/Ny", SkjemaHjelper.Felter(
            ("Personnummer", "01013333333"), ("MobilNr", "+4790060002"),
            ("Epost", "rediger-pasient@integrationtest.local"), ("Varslingskanal", "Sms")), nyPasToken);

        long pasientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            pasientId = (await db.Pasienter.SingleAsync(p => p.BehandlerId == behandlerId && p.MobilNr == "+4790060002")).Id;
        }

        var redigerUrl = $"/Behandlerportal/Pasienter/Rediger/{pasientId}";
        var redigerHtml = await SkjemaHjelper.GetHtmlAsync(client, redigerUrl);
        Assert.Contains("01013333333", redigerHtml);

        var redigerToken = SkjemaHjelper.HentToken(redigerHtml);
        var lagreResp = await SkjemaHjelper.PostMedTokenAsync(client, redigerUrl, SkjemaHjelper.Felter(
            ("Id", pasientId.ToString()), ("Navn", "Redigert Pasientnavn"), ("Gruppenavn", "Gruppe X"),
            ("Personnummer", "01013333333"), ("MobilNr", "+4790060003"),
            ("Epost", "rediger-pasient@integrationtest.local")), redigerToken);

        var indexHtml = await SkjemaHjelper.LesHtmlAsync(lagreResp);
        Assert.Contains("Redigert Pasientnavn", indexHtml);
        Assert.Contains("btn-icon", indexHtml);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var oppdatert = await db.Pasienter.SingleAsync(p => p.Id == pasientId);
            Assert.Equal("Redigert Pasientnavn", oppdatert.Navn);
            Assert.Equal("Gruppe X", oppdatert.Gruppenavn);
            Assert.Equal("+4790060003", oppdatert.MobilNr);

            // Arkiver behandleren vi opprettet her igjen — deler samme faste
            // BankID-testpersonnummer som andre tester i denne collection-en (se
            // TestBaseCollection), og skal ikke stå igjen som en tvetydig aktiv
            // match for BankID-oppslag i tester som kjører etterpå.
            var behandler = await db.Behandlere.SingleAsync(b => b.Id == behandlerId);
            behandler.Status = BehandlerStatus.Arkivert;
            await db.SaveChangesAsync();
        }
    }

    private static async Task LoggInnSomDevAdminAsync(HttpClient client)
    {
        var (loginHtml, loginToken, captchaSvar, captchaFasit) =
            await SkjemaHjelper.LastInnloggingsskjemaAsync(client, "/Konto/LoggInn");
        await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/LoggInn?handler=Passord", SkjemaHjelper.Felter(
            ("AdminId", "dev-admin"), ("Passord", "utvikler123"),
            ("CaptchaSvar", captchaSvar), ("CaptchaSignertFasit", captchaFasit)), loginToken);
    }
}
