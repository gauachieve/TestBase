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
/// Ende-til-ende-regresjonstest for hele systemet slik det står etter fase 4:
/// admin (passord + BankID+2FA) → inviterer behandler → behandler fullfører
/// registrering+brukeravtale+kontaktverifisering → logger inn med BankID+2FA →
/// legger til pasient (enkeltvis + gruppeimport) → inviterer kollega-behandler
/// → admin oppretter en test og godkjenner HPR → behandler tildeler testen →
/// pasient fullfører egenregistrering → logger inn med BankID (uten 2FA) →
/// fyller ut testen side for side → belønningsside vises.
///
/// Dette er BEVISST én sammenhengende test, ikke mange små — historien bygger
/// på delt tilstand steg for steg (samme mønster som den manuelle
/// curl-baserte verifiseringen gjort under selve utviklingen av fase 2–4), og
/// xUnit garanterer ikke rekkefølge mellom uavhengige [Fact]-metoder i samme
/// klasse. Kjør denne etter enhver endring som rører autentisering,
/// invitasjons-/registreringsflytene, eller testmotoren.
///
/// Krever Docker-MySQL kjørende (samme container som lokal dev, se
/// TestBaseWebApplicationFactory for hvorfor egen database brukes).
/// </summary>
[Collection(TestBaseCollection.Navn)]
public sealed class HeleFlytenTests
{
    // Det faste testpersonnummeret MockBankIdProvider alltid returnerer.
    // Trygt å gjenbruke på tvers av administrator-/behandler-/pasient-tabellene
    // (hver BankID-innlogging slår kun opp i sin egen tabell) — MEN aldri to
    // ganger i SAMME tabell, se kjente fallgruver i CLAUDE.md.
    private const string MockPersonnummer = "01019012345";

    private static readonly Regex SeksSifretKode = new(@"\b(\d{6})\b", RegexOptions.Compiled);
    private static readonly Regex InvitasjonsToken = new(@"/(?:Inviter|PasientRegistrering)/Fullfor/([A-F0-9]+)", RegexOptions.Compiled);

    private readonly TestBaseWebApplicationFactory _factory;

    public HeleFlytenTests(TestBaseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HeleFlyten_AdminBehandlerPasientOgTestmotor_FungererEndeTilEnde()
    {
        var client = _factory.CreateClient();

        // === A: Admin — passordinnlogging (utviklingsmodus, dev-seed) ===========
        // Samlet innlogging (/Konto/LoggInn): AdminId+passord er ett sekundært,
        // ett-stegs skjema (asp-page-handler="Passord") på samme side som
        // BankID-hovedflyten, ikke lenger et eget to-stegs skjema.
        var (loginHtml, loginToken, loginCaptchaSvar, loginCaptchaFasit) =
            await SkjemaHjelper.LastInnloggingsskjemaAsync(client, "/Konto/LoggInn");
        var steg1 = await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/LoggInn?handler=Passord",
            SkjemaHjelper.Felter(
                ("AdminId", "dev-admin"), ("Passord", "utvikler123"),
                ("CaptchaSvar", loginCaptchaSvar), ("CaptchaSignertFasit", loginCaptchaFasit)), loginToken);
        var html1 = await SkjemaHjelper.LesHtmlAsync(steg1);
        Assert.True(steg1.IsSuccessStatusCode);
        Assert.Contains("Dev Administrator", html1);
        Assert.Contains("Passord (utviklingsmodus)", html1);

        // === B: Admin — BankID+2FA-innlogging =====================================
        var nyAdminToken = await SkjemaHjelper.HentTokenAsync(client, "/Admin/Administratorer/Ny");
        await SkjemaHjelper.PostMedTokenAsync(client, "/Admin/Administratorer/Ny", SkjemaHjelper.Felter(
            ("AdminId", "test-bankid-admin"),
            ("MobilNr", "+4790010001"),
            ("Email", "bankid-admin@integrationtest.local"),
            ("FulltNavn", "BankID Testadministrator"),
            ("Personnummer", MockPersonnummer),
            ("HprNr", "1000001")), nyAdminToken);

        await client.GetAsync("/Konto/LoggUt");

        // Samlet innlogging har ingen ID-inntasting for BankID-veien — den finner
        // personen selv via MockBankIdProvider sitt faste personnummer, som kun
        // matcher "test-bankid-admin" i administrator-tabellen på dette tidspunktet.
        var (bidHtml0, bidToken0, bidCaptchaSvar, bidCaptchaFasit) =
            await SkjemaHjelper.LastInnloggingsskjemaAsync(client, "/Konto/LoggInn");
        var bidSteg1 = await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/LoggInn",
            SkjemaHjelper.Felter(("CaptchaSvar", bidCaptchaSvar), ("CaptchaSignertFasit", bidCaptchaFasit)), bidToken0);
        var bidHtml1 = await SkjemaHjelper.LesHtmlAsync(bidSteg1);
        Assert.Contains("Bekreft SMS-kode", bidHtml1);

        var adminKode = SeksSifretKode.Match(_factory.Sms.SisteMeldingTil("+4790010001") ?? "").Groups[1].Value;
        Assert.NotEmpty(adminKode);

        var bidToken2 = SkjemaHjelper.HentToken(bidHtml1);
        var bidSteg2 = await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/BekreftKode",
            SkjemaHjelper.Felter(("Kode", adminKode)), bidToken2);
        var bidHtml2 = await SkjemaHjelper.LesHtmlAsync(bidSteg2);
        Assert.Contains("BankID Testadministrator", bidHtml2);
        Assert.Contains("Administrator", bidHtml2);

        // === C: Inviter behandler, fullfør registrering, verifiser kontakt =======
        var invBehToken = await SkjemaHjelper.HentTokenAsync(client, "/Admin/Behandlere/Inviter");
        var behMobil = "+4790020001";
        var invBehResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Admin/Behandlere/Inviter",
            SkjemaHjelper.Felter(("MobilNr", behMobil)), invBehToken);
        Assert.Contains("Invitasjon", await SkjemaHjelper.LesHtmlAsync(invBehResp));

        var behMelding = _factory.Sms.SisteMeldingTil(behMobil) ?? "";
        var behToken = InvitasjonsToken.Match(behMelding).Groups[1].Value;
        Assert.NotEmpty(behToken);

        var fullforUrl = $"/Inviter/Fullfor/{behToken}";
        var fullforHtml = await SkjemaHjelper.GetHtmlAsync(client, fullforUrl);
        var fullforToken = SkjemaHjelper.HentToken(fullforHtml);
        var eldreTidspunkt = DateTimeOffset.UtcNow.AddSeconds(-5).ToString("O");
        var behEpost = "behandler@integrationtest.local";

        var fullforResp = await SkjemaHjelper.PostMedTokenAsync(client, fullforUrl, SkjemaHjelper.Felter(
            ("Fornavn", "Test"), ("Etternavn", "Behandlersen"), ("Personnummer", MockPersonnummer),
            ("MobilNr", behMobil), ("Epost", behEpost), ("HprNr", "9999999"), ("Kontonummer", "12345678901"),
            ("GodtarAvtale", "true"), ("Vist", eldreTidspunkt), ("Nettside", "")), fullforToken);
        var verifiserUrl = fullforResp.RequestMessage!.RequestUri!.PathAndQuery;
        Assert.Contains("/Inviter/Verifiser/", verifiserUrl);

        var verifiserHtml = await SkjemaHjelper.LesHtmlAsync(fullforResp);
        var verifiserToken = SkjemaHjelper.HentToken(verifiserHtml);
        var mobilKode = SeksSifretKode.Match(_factory.Sms.SisteMeldingTil(behMobil) ?? "").Groups[1].Value;
        var epostKode = SeksSifretKode.Match(_factory.Epost.SisteMeldingTil(behEpost) ?? "").Groups[1].Value;

        var verifiserResp = await SkjemaHjelper.PostMedTokenAsync(client, verifiserUrl, SkjemaHjelper.Felter(
            ("MobilKode", mobilKode), ("EpostKode", epostKode)), verifiserToken);
        Assert.Contains("Kontoen din er nå aktiv", await SkjemaHjelper.LesHtmlAsync(verifiserResp));

        // HPR-varsling skal ha gått til administratorene.
        Assert.Contains(_factory.Epost.AlleSendte(), e =>
            e.Emne == "Ny behandler venter HPR-godkjenning" && e.Til == "bankid-admin@integrationtest.local");

        // === D: Behandler logger inn med BankID+2FA ================================
        // "test-bankid-admin" og den nye behandleren deler MockBankIdProvider sitt
        // faste personnummer (se MockPersonnummer-kommentaren øverst). Samlet
        // innlogging prioriterer administrator over behandler ("høyeste rolle",
        // jf. Pages/Konto/LoggInn.cshtml.cs) — arkiver derfor testadministratoren
        // først, ellers ville BankID-innlogging her feilaktig logget inn som admin
        // igjen i stedet for å nå behandler-grenen vi faktisk vil teste.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testBankIdAdmin = await db.Administratorer.SingleAsync(a => a.AdminId == "test-bankid-admin");
            testBankIdAdmin.ErArkivert = true;
            await db.SaveChangesAsync();
        }

        await client.GetAsync("/Konto/LoggUt");

        var (behLoginHtml0, behLoginToken, behCaptchaSvar, behCaptchaFasit) =
            await SkjemaHjelper.LastInnloggingsskjemaAsync(client, "/Konto/LoggInn");
        var behLoginResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/LoggInn",
            SkjemaHjelper.Felter(("CaptchaSvar", behCaptchaSvar), ("CaptchaSignertFasit", behCaptchaFasit)), behLoginToken);
        var behLoginHtml = await SkjemaHjelper.LesHtmlAsync(behLoginResp);
        var behKodeToken = SkjemaHjelper.HentToken(behLoginHtml);
        var behKode = SeksSifretKode.Match(_factory.Sms.SisteMeldingTil(behMobil) ?? "").Groups[1].Value;

        var behBekreftResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/BekreftKode",
            SkjemaHjelper.Felter(("Kode", behKode)), behKodeToken);
        var behHjemHtml = await SkjemaHjelper.LesHtmlAsync(behBekreftResp);
        Assert.Contains("Test Behandlersen", behHjemHtml);
        Assert.Contains("Behandler", behHjemHtml);

        // === E: Behandler legger til pasient (enkeltvis) + gruppeimport ==========
        var pasMobil = "+4790030001";
        var nyPasToken = await SkjemaHjelper.HentTokenAsync(client, "/Behandlerportal/Pasienter/Ny");
        var nyPasResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Behandlerportal/Pasienter/Ny", SkjemaHjelper.Felter(
            ("Personnummer", MockPersonnummer), ("MobilNr", pasMobil),
            ("Epost", "pasient@integrationtest.local"), ("Varslingskanal", "Sms")), nyPasToken);
        Assert.Contains("/Behandlerportal/Pasienter", nyPasResp.RequestMessage!.RequestUri!.PathAndQuery);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var opprettet = await db.Pasienter.SingleAsync(p => p.MobilNr == pasMobil);
            Assert.Equal(PasientStatus.Invitert, opprettet.Status);
            Assert.Equal(MockPersonnummer, opprettet.Personnummer);
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var pasientAuth = scope.ServiceProvider.GetRequiredService<TestBase.Shared.Security.PasientAuthenticationService>();
            var funnet = await pasientAuth.FinnVedPersonnummerAsync(MockPersonnummer);
            Assert.NotNull(funnet);
            Assert.Equal(pasMobil, funnet!.MobilNr);
        }

        // Regresjonsvern: pasienten er lagt til (personnummeret er allerede i databasen),
        // men har ikke fullført egenregistreringen (Status=Invitert) — BankID-innlogging skal
        // avvises med en tydelig melding, ikke slippe gjennom eller krasje.
        await client.GetAsync("/Konto/LoggUt");
        var (forTidligHtml0, forTidligToken, forTidligCaptchaSvar, forTidligCaptchaFasit) =
            await SkjemaHjelper.LastInnloggingsskjemaAsync(client, "/Pasientportal/Konto/LoggInn");
        var forTidligResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Pasientportal/Konto/LoggInn",
            SkjemaHjelper.Felter(("CaptchaSvar", forTidligCaptchaSvar), ("CaptchaSignertFasit", forTidligCaptchaFasit)), forTidligToken);
        Assert.Contains("Du har ikke fullført registreringen", await SkjemaHjelper.LesHtmlAsync(forTidligResp));

        var (behLoginIgjenHtml0, behLoginIgjenToken, behIgjenCaptchaSvar, behIgjenCaptchaFasit) =
            await SkjemaHjelper.LastInnloggingsskjemaAsync(client, "/Konto/LoggInn");
        var behLoginIgjenResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/LoggInn",
            SkjemaHjelper.Felter(("CaptchaSvar", behIgjenCaptchaSvar), ("CaptchaSignertFasit", behIgjenCaptchaFasit)), behLoginIgjenToken);
        var behKodeIgjenToken = SkjemaHjelper.HentToken(await SkjemaHjelper.LesHtmlAsync(behLoginIgjenResp));
        var behKodeIgjen = SeksSifretKode.Match(_factory.Sms.SisteMeldingTil(behMobil) ?? "").Groups[1].Value;
        await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/BekreftKode",
            SkjemaHjelper.Felter(("Kode", behKodeIgjen)), behKodeIgjenToken);

        var gruppeToken = await SkjemaHjelper.HentTokenAsync(client, "/Behandlerportal/Pasienter/Gruppeimport");
        var gruppeliste = "Gruppe A,Ola Testesen,ola@integrationtest.local,+4790030002,02029000001\n" +
                           "Gruppe A,Kari Testesen,kari@integrationtest.local,+4790030003,03039000001\n" +
                           "For faa felt her";
        var gruppeResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Behandlerportal/Pasienter/Gruppeimport",
            SkjemaHjelper.Felter(("Liste", gruppeliste)), gruppeToken);
        var gruppeHtml = await SkjemaHjelper.LesHtmlAsync(gruppeResp);
        Assert.Contains("2 pasient(er) opprettet", gruppeHtml);
        Assert.Contains("For faa felt her", gruppeHtml);

        // === F: Behandler inviterer en kollega — bruker samme tjeneste som admin ==
        var kollegaMobil = "+4790040001";
        var inviterKollegaToken = await SkjemaHjelper.HentTokenAsync(client, "/Behandlerportal/Behandlere/Inviter");
        await SkjemaHjelper.PostMedTokenAsync(client, "/Behandlerportal/Behandlere/Inviter",
            SkjemaHjelper.Felter(("MobilNr", kollegaMobil)), inviterKollegaToken);

        // === G: Admin godkjenner HPR for behandleren ==============================
        long behandlerId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var behandler = await db.Behandlere.SingleAsync(b => b.MobilNr == behMobil);
            behandlerId = behandler.Id;

            var kollega = await db.Behandlere.SingleAsync(b => b.MobilNr == kollegaMobil);
            Assert.Equal(behandlerId, kollega.InvitertAvBehandlerId);
            Assert.Null(kollega.InvitertAvAdministratorId);
        }

        await client.GetAsync("/Konto/LoggUt");
        var (adminLoginHtml0, adminLoginToken, adminCaptchaSvar, adminCaptchaFasit) =
            await SkjemaHjelper.LastInnloggingsskjemaAsync(client, "/Konto/LoggInn");
        await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/LoggInn?handler=Passord",
            SkjemaHjelper.Felter(
                ("AdminId", "dev-admin"), ("Passord", "utvikler123"),
                ("CaptchaSvar", adminCaptchaSvar), ("CaptchaSignertFasit", adminCaptchaFasit)), adminLoginToken);

        var godkjennHprToken = await SkjemaHjelper.HentTokenAsync(client, "/Admin/Behandlere");
        await SkjemaHjelper.PostMedTokenAsync(client, "/Admin/Behandlere?handler=GodkjennHpr",
            SkjemaHjelper.Felter(("id", behandlerId.ToString())), godkjennHprToken);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var behandler = await db.Behandlere.SingleAsync(b => b.Id == behandlerId);
            Assert.True(behandler.HprGodkjent);
        }

        // === H: Admin oppretter en test (2 sider, fire svartyper) ================
        var nyTestToken = await SkjemaHjelper.HentTokenAsync(client, "/Admin/Tester/Ny");
        var nyTestResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Admin/Tester/Ny", SkjemaHjelper.Felter(
            ("Navn", "Integrasjonstest"), ("Beskrivelse", "En enkel test"), ("Belonningstekst", "Bra jobbet!")),
            nyTestToken);
        var sider1Url = nyTestResp.RequestMessage!.RequestUri!.PathAndQuery;
        Assert.Contains("/Admin/Tester/Sider/", sider1Url);
        var testId = long.Parse(sider1Url.Split('/').Last());

        var side1Token = await SkjemaHjelper.HentTokenAsync(client, sider1Url);
        await SkjemaHjelper.PostMedTokenAsync(client, sider1Url,
            SkjemaHjelper.Felter(("Navn", "Side 1"), ("Instruksjon", "Svar ærlig")), side1Token);
        var side2Token = await SkjemaHjelper.HentTokenAsync(client, sider1Url);
        await SkjemaHjelper.PostMedTokenAsync(client, sider1Url,
            SkjemaHjelper.Felter(("Navn", "Side 2"), ("Instruksjon", "Siste side")), side2Token);

        long side1Id, side2Id;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sider = await db.TestSider.Where(s => s.TestId == testId).OrderBy(s => s.Rekkefolge).ToListAsync();
            Assert.Equal(2, sider.Count);
            side1Id = sider[0].Id;
            side2Id = sider[1].Id;
        }

        async Task LeggTilLeddAsync(long sideId, string sporsmal, TestSvartype svartype, string? svaralternativer = null)
        {
            var url = $"/Admin/Tester/Ledd/{sideId}";
            var token = await SkjemaHjelper.HentTokenAsync(client, url);
            await SkjemaHjelper.PostMedTokenAsync(client, url, SkjemaHjelper.Felter(
                ("Sporsmalstekst", sporsmal), ("Svartype", svartype.ToString()), ("Svaralternativer", svaralternativer ?? "")),
                token);
        }

        await LeggTilLeddAsync(side1Id, "Hvor ofte har du følt deg glad?", TestSvartype.LikertSkala, "5:Alltid,4:Ofte,3:Av og til,2:Sjelden,1:Aldri");
        await LeggTilLeddAsync(side1Id, "Har du sovet godt?", TestSvartype.JaNei);
        await LeggTilLeddAsync(side2Id, "Hvor bekymret er du akkurat nå?", TestSvartype.VisuellAnalogSkala);
        await LeggTilLeddAsync(side2Id, "Noe du vil legge til?", TestSvartype.Fritekst);

        List<long> leddIder;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            leddIder = await db.TestLedd.Where(l => l.TestSideId == side1Id || l.TestSideId == side2Id)
                .OrderBy(l => l.TestSideId).ThenBy(l => l.Rekkefolge).Select(l => l.Id).ToListAsync();
            Assert.Equal(4, leddIder.Count);
        }

        // === I: Behandler tildeler testen til pasienten med det faste personnummeret ==
        await client.GetAsync("/Konto/LoggUt");
        var (behLogin2Html0, behLogin2Token, behLogin2CaptchaSvar, behLogin2CaptchaFasit) =
            await SkjemaHjelper.LastInnloggingsskjemaAsync(client, "/Konto/LoggInn");
        var behLogin2Resp = await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/LoggInn",
            SkjemaHjelper.Felter(("CaptchaSvar", behLogin2CaptchaSvar), ("CaptchaSignertFasit", behLogin2CaptchaFasit)), behLogin2Token);
        var behKode2Token = SkjemaHjelper.HentToken(await SkjemaHjelper.LesHtmlAsync(behLogin2Resp));
        var behKode2 = SeksSifretKode.Match(_factory.Sms.SisteMeldingTil(behMobil) ?? "").Groups[1].Value;
        await SkjemaHjelper.PostMedTokenAsync(client, "/Konto/BekreftKode",
            SkjemaHjelper.Felter(("Kode", behKode2)), behKode2Token);

        long pasientId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            pasientId = (await db.Pasienter.SingleAsync(p => p.MobilNr == pasMobil)).Id;
        }

        var detaljerUrl = $"/Behandlerportal/Pasienter/Detaljer/{pasientId}";
        var tildelToken = await SkjemaHjelper.HentTokenAsync(client, detaljerUrl);
        await SkjemaHjelper.PostMedTokenAsync(client, detaljerUrl,
            SkjemaHjelper.Felter(("TestId", testId.ToString())), tildelToken);

        // === J: Pasienten fullfører egenregistrering (ingen kontaktverifisering) ===
        var pasMelding = _factory.Sms.SisteMeldingTil(pasMobil) ?? "";
        var pasToken = InvitasjonsToken.Match(pasMelding).Groups[1].Value;
        Assert.NotEmpty(pasToken);

        await client.GetAsync("/Konto/LoggUt");

        var pasFullforUrl = $"/PasientRegistrering/Fullfor/{pasToken}";
        var pasFullforHtml = await SkjemaHjelper.GetHtmlAsync(client, pasFullforUrl);
        var pasFullforToken = SkjemaHjelper.HentToken(pasFullforHtml);

        var pasFullforResp = await SkjemaHjelper.PostMedTokenAsync(client, pasFullforUrl, SkjemaHjelper.Felter(
            ("Navn", "Integrasjonstest Pasientsen"), ("Personnummer", MockPersonnummer), ("MobilNr", pasMobil),
            ("Epost", "pasient@integrationtest.local"), ("BiologiskKjonnVedFodsel", "Mann"),
            ("GodtarLagringAvData", "true"), ("GodtarMuligVippsBetaling", "true"),
            ("Vist", eldreTidspunkt), ("Nettside", "")), pasFullforToken);
        var pasFullforRespHtml = await SkjemaHjelper.LesHtmlAsync(pasFullforResp);
        Assert.True(pasFullforResp.IsSuccessStatusCode,
            $"Status {pasFullforResp.StatusCode}, url {pasFullforResp.RequestMessage?.RequestUri}, body-lengde {pasFullforRespHtml.Length}");
        Assert.Contains("Takk!", pasFullforRespHtml);

        // === K: Pasienten logger inn med BankID (INGEN 2FA) =========================
        var (pasLoginHtml0, pasLoginToken, pasCaptchaSvar, pasCaptchaFasit) =
            await SkjemaHjelper.LastInnloggingsskjemaAsync(client, "/Pasientportal/Konto/LoggInn");
        var pasLoginResp = await SkjemaHjelper.PostMedTokenAsync(client, "/Pasientportal/Konto/LoggInn",
            SkjemaHjelper.Felter(("CaptchaSvar", pasCaptchaSvar), ("CaptchaSignertFasit", pasCaptchaFasit)), pasLoginToken);
        Assert.Contains("/Pasientportal/MinSide", pasLoginResp.RequestMessage!.RequestUri!.PathAndQuery);
        var minSideHtml = await SkjemaHjelper.LesHtmlAsync(pasLoginResp);
        Assert.Contains("Integrasjonstest", minSideHtml);
        Assert.Contains("Tildelt", minSideHtml);

        long tildelingId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            tildelingId = (await db.TestTildelinger.SingleAsync(t => t.PasientId == pasientId)).Id;
        }

        // === L: Pasienten fyller ut testen side for side ===========================
        var fyllSide1Url = $"/Pasientportal/Tester/Fyll/{tildelingId}/1";
        var fyllSide1Html = await SkjemaHjelper.GetHtmlAsync(client, fyllSide1Url);
        Assert.Contains("Side 1 av 2", fyllSide1Html);
        var fyllSide1Token = SkjemaHjelper.HentToken(fyllSide1Html);

        var side1Resp = await SkjemaHjelper.PostMedTokenAsync(client, fyllSide1Url, SkjemaHjelper.Felter(
            ($"Svar_{leddIder[0]}", "4"), ($"Svar_{leddIder[1]}", "Ja"), ("Handling", "Neste")), fyllSide1Token);
        Assert.Contains("/Pasientportal/Tester/Fyll/", side1Resp.RequestMessage!.RequestUri!.PathAndQuery);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tildeling = await db.TestTildelinger.SingleAsync(t => t.Id == tildelingId);
            Assert.Equal(TestTildelingStatus.Startet, tildeling.Status);
            Assert.NotNull(tildeling.StartetUtc);
        }

        var fyllSide2Html = await SkjemaHjelper.LesHtmlAsync(side1Resp);
        Assert.Contains("Side 2 av 2", fyllSide2Html);
        var fyllSide2Token = SkjemaHjelper.HentToken(fyllSide2Html);
        var fyllSide2Url = $"/Pasientportal/Tester/Fyll/{tildelingId}/2";

        var ferdigResp = await SkjemaHjelper.PostMedTokenAsync(client, fyllSide2Url, SkjemaHjelper.Felter(
            ($"Svar_{leddIder[2]}", "75"), ($"Svar_{leddIder[3]}", "Alt bra."), ("Handling", "Ferdig")), fyllSide2Token);
        var ferdigHtml = await SkjemaHjelper.LesHtmlAsync(ferdigResp);
        Assert.Contains("Ferdig!", ferdigHtml);
        Assert.Contains("Bra jobbet!", ferdigHtml);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tildeling = await db.TestTildelinger.SingleAsync(t => t.Id == tildelingId);
            Assert.Equal(TestTildelingStatus.Fullfort, tildeling.Status);
            Assert.NotNull(tildeling.FullfortUtc);

            var svar = await db.TestSvar.Where(s => s.TestTildelingId == tildelingId).ToDictionaryAsync(s => s.TestLeddId, s => s.SvarVerdi);
            Assert.Equal(4, svar.Count);
            Assert.Equal("4", svar[leddIder[0]]);
            Assert.Equal("Ja", svar[leddIder[1]]);
            Assert.Equal("75", svar[leddIder[2]]);
            Assert.Equal("Alt bra.", svar[leddIder[3]]);
        }

        // === M: Audit-loggen skal ha rader for de viktigste handlingene =============
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var handlinger = await db.AuditLogEntries.Select(e => e.Action).ToListAsync();
            Assert.Contains("InnloggingOk", handlinger);
            Assert.Contains("OpprettAdministrator", handlinger);
            Assert.Contains("InviterBehandler", handlinger);
            Assert.Contains("LeggTilPasient", handlinger);
            Assert.Contains("GodkjennHpr", handlinger);
            Assert.Contains("TildelTest", handlinger);
            Assert.Contains("FullforTest", handlinger);
        }
    }
}
