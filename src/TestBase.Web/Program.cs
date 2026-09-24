using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.InnebygdeTester;
using TestBase.Shared.Domain.Tester.Skaaring;
using TestBase.Shared.Providers;
using TestBase.Shared.Providers.Mock;
using TestBase.Shared.Security;
using TestBase.Web;
using TestBase.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// --- Database -----------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Mangler tilkoblingsstreng 'DefaultConnection'. Se appsettings.Development.json / docker-compose.yml.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// --- Kjerne-tjenester (aktive i ALLE miljøer, jf. beslutningsloggen) ----
builder.Services.AddScoped<IAuditLogger, EfAuditLogger>();

// Data Protection brukes til å kryptere personnummer i hvile (se AppDbContext).
// I dev bruker den automatisk en lokal, filbasert nøkkelring; i prod pekes
// samme kode senere mot Azure Key Vault ved konfigurasjon alene.
builder.Services.AddDataProtection();

// --- Autentisering og autorisasjon (fase 2) -------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, AuthenticatedCurrentUserContext>();
builder.Services.AddScoped<ToFaktorService>();
// I-minne-cache for kortlevde, høyfrekvente oppslag under en brå trafikktopp
// (f.eks. mange samtidige QR-registreringer på samme token, eller gjentatt
// lasting av samme tests faste struktur) — se GruppeService/TestService for
// bruk, og docs/beslutningslogg.md "Optimalisering før skalering" for
// begrunnelsen (funnet via reell lasttest, ikke forhåndsantatt).
builder.Services.AddMemoryCache();

builder.Services.AddScoped<AdminAuthenticationService>();
builder.Services.AddScoped<BehandlerAuthenticationService>();
builder.Services.AddScoped<BehandlerInvitasjonService>();
builder.Services.AddScoped<PasientAuthenticationService>();
builder.Services.AddScoped<PasientInvitasjonService>();
builder.Services.AddScoped<GruppeService>();
builder.Services.AddScoped<TestService>();
builder.Services.AddScoped<TestTildelingsService>();
builder.Services.AddSingleton<TestPrisberegner>();
builder.Services.AddScoped<BehandlerMeldingService>();
builder.Services.AddScoped<PaaminnelseService>();
builder.Services.AddHostedService<DagligPaaminnelseBakgrunnstjeneste>();
builder.Services.AddScoped<PlanlagtTildelingService>();
builder.Services.AddHostedService<PlanlagtTildelingBakgrunnstjeneste>();

// Skåringsmotor og innebygde, kode-definerte tester (fase 5 — bevist ut med WHO-5).
builder.Services.AddScoped<ITestSkaaringsberegner, Who5Skaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, Who5TestSeeder>();

// VAS-variant av WHO-5 for gjentatt måling over tid (2026-09-14), se Who5VasTestSeeder.
builder.Services.AddScoped<ITestSkaaringsberegner, Who5VasSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, Who5VasTestSeeder>();

// Åtte tester fra Helsebiblioteket (2026-09), se docs/beslutningslogg.md.
builder.Services.AddScoped<ITestSkaaringsberegner, Phq9Skaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, Phq9TestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, IpdsSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, IpdsTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, MadrsSSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, MadrsSTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, TrapsISkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, TrapsITestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, RaadsRSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, RaadsRTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, WursSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, WursTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, Eq40Skaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, Eq40TestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, SovnSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, SovnTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, ItqSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, ItqTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, PdsIcd11Skaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, PdsIcd11TestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, PicdSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, PicdTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, Paq11RSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, Paq11RTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, IdqSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, IdqTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, IaqSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, IaqTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, GaditSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, GaditTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, AsrsSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, AsrsTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, AuditSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, AuditTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, DuditSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, DuditTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, Scl25Skaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, Scl25TestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, Bsq14Skaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, Bsq14TestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, Sdq20Skaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, Sdq20TestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, EdeqSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, EdeqTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, TrapsIiSkaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, TrapsIiTestSeeder>();
builder.Services.AddScoped<ITestSkaaringsberegner, Core10Skaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, Core10TestSeeder>();

// Admin og Behandlerportal deler nå én samlet innloggingsside (/Konto/LoggInn
// — BankID finner personen og logger inn på høyeste rolle selv, uten at
// brukeren velger portal, se Pages/Konto/LoggInn.cshtml.cs). Pasientportal har
// fortsatt egen inngang, siden pasienter er en separat gruppe med egen
// landingsside (/Pasienter) — se beslutningsloggen. Redirect til login bærer
// alltid med seg en returnUrl (lest av begge LoggInn-sidene, validert med
// Url.IsLocalUrl før bruk) slik at man havner tilbake der man egentlig skulle
// etter innlogging — og for pasientportalens tildelte-test-lenker (jf.
// beslutningsloggen "BankID personnr-forhåndsutfylling fra testlenke") slår
// vi i tillegg opp riktig personnummer for AKKURAT den tildelingen (kun når
// Miljo:TillatUtviklingsSnarveier er satt — se Security/Miljo.cs, ALDRI på
// live) slik at pasienten ikke selv må vite/skrive inn sitt (mock-)
// personnummer for å logge inn på en lenke hen fikk tilsendt.
static async Task<string> InnloggingsstiForAsync(HttpContext httpContext, PathString sti, QueryString opprinneligQuery)
{
    var innloggingssti = sti.StartsWithSegments("/Pasientportal") ? "/Pasientportal/Konto/LoggInn" : "/Konto/LoggInn";
    var returnerTil = $"{sti}{opprinneligQuery}";
    var query = QueryString.Create("returnUrl", returnerTil);

    var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
    if (TestBase.Web.Security.Miljo.TillatUtviklingsSnarveier(configuration) && sti.StartsWithSegments("/Pasientportal/Tester/Fyll", out var rest))
    {
        var tildelingIdSegment = rest.Value?.Trim('/').Split('/').FirstOrDefault();
        if (long.TryParse(tildelingIdSegment, out var tildelingId))
        {
            var db = httpContext.RequestServices.GetRequiredService<AppDbContext>();
            var tildeling = await db.TestTildelinger.FirstOrDefaultAsync(t => t.Id == tildelingId);
            var pasient = tildeling is null ? null : await db.Pasienter.FirstOrDefaultAsync(p => p.Id == tildeling.PasientId);
            if (pasient is not null)
            {
                query = query.Add("personnummer", pasient.Personnummer);
            }
        }
    }

    return innloggingssti + query;
}

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Konto/LoggInn";
        options.LogoutPath = "/Konto/LoggUt";
        options.AccessDeniedPath = "/Konto/LoggInn";
        options.ExpireTimeSpan = TimeSpan.FromDays(builder.Configuration.GetValue("Auth:RememberMeDays", 30));
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = async context =>
        {
            var url = await InnloggingsstiForAsync(context.HttpContext, context.Request.Path, context.Request.QueryString);
            context.Response.Redirect(url);
        };
        options.Events.OnRedirectToAccessDenied = async context =>
        {
            var url = await InnloggingsstiForAsync(context.HttpContext, context.Request.Path, context.Request.QueryString);
            context.Response.Redirect(url);
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Superadmin er et strengt supersett av Administrator — inkludert her slik at en
    // Superadmin ALDRI blir utestengt fra en vanlig admin-side, se docs/beslutningslogg.md
    // "Partner System + Test Monetization".
    options.AddPolicy("AdminOmrade", policy =>
        policy.RequireRole(nameof(UserRole.Administrator), nameof(UserRole.Superadmin), nameof(UserRole.Utvikler)));
    options.AddPolicy("BehandlerOmrade", policy =>
        policy.RequireRole(nameof(UserRole.Behandler), nameof(UserRole.Utvikler)));
    options.AddPolicy("PasientOmrade", policy =>
        policy.RequireRole(nameof(UserRole.Pasient), nameof(UserRole.Utvikler)));
    options.AddPolicy("SuperadminOmrade", policy =>
        policy.RequireRole(nameof(UserRole.Superadmin), nameof(UserRole.Utvikler)));

    // Partner-admin er bevisst IKKE en egen UserRole (fortsatt en Behandler, bare med en
    // ekstra evne) — se AppClaimTypes.ErPartnerAdministrator/PartnerId og
    // docs/beslutningslogg.md "Partner System + Test Monetization". RequireAssertion
    // fremfor en egen IAuthorizationRequirement-klasse siden dette er én enkel claim-sjekk.
    options.AddPolicy("PartnerAdminOmrade", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole(nameof(UserRole.Utvikler)) ||
            (ctx.User.IsInRole(nameof(UserRole.Behandler)) &&
             ctx.User.FindFirstValue(AppClaimTypes.ErPartnerAdministrator) == bool.TrueString)));
});

// --- BankID-testintegrasjon (diagnostisk, IKKE koblet til produksjonsinnlogging) ---
// Ekte BankID via Idura sitt OIDC-endepunkt — kun for å bekrefte at selve integrasjonen
// fungerer teknisk. IKKE en erstatning for MockBankIdProvider/IBankIdProvider ennå: dette
// er en gratis Idura TEST-konto, ingen reell BankID-produksjonsavtale (se beslutningsloggen
// "BankID-testintegrasjon via Idura"). Nås kun via /DevDemo → /BankIdTest/Start, gatet på
// Miljo:TillatUtviklingsSnarveier i selve handlerne (ikke bare skjult i UI, se Security/Miljo.cs
// og kjente fallgruver i CLAUDE.md).
var iduraAuthority = builder.Configuration["BankId:Idura:Authority"];
var iduraClientId = builder.Configuration["BankId:Idura:ClientId"];
var iduraClientSecret = builder.Configuration["BankId:Idura:ClientSecret"];
if (!string.IsNullOrWhiteSpace(iduraAuthority) && !string.IsNullOrWhiteSpace(iduraClientId) && !string.IsNullOrWhiteSpace(iduraClientSecret))
{
    var iduraAcrValues = builder.Configuration["BankId:Idura:AcrValues"] ?? "urn:grn:authn:no:bankid:high";
    builder.Services.AddAuthentication().AddOpenIdConnect("BankIdTest", options =>
    {
        options.Authority = iduraAuthority;
        options.ClientId = iduraClientId;
        options.ClientSecret = iduraClientSecret;
        options.ResponseType = "code";
        options.ResponseMode = "form_post";
        options.CallbackPath = "/signin-bankid-test";
        options.SaveTokens = false;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("ssn");

        // form_post svarer med en cross-site POST fra Idura sitt domene — korrelasjons-/
        // nonce-cookien MÅ tillate dette, ellers feiler valideringen ("Correlation failed").
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.NonceCookie.SameSite = SameSiteMode.None;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = ctx =>
            {
                ctx.ProtocolMessage.AcrValues = iduraAcrValues;
                return Task.CompletedTask;
            },
            OnTokenValidated = async ctx =>
            {
                var claimsTekst = string.Join('\n', ctx.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}") ?? []);
                var tempData = ctx.HttpContext.RequestServices
                    .GetRequiredService<ITempDataDictionaryFactory>()
                    .GetTempData(ctx.HttpContext);
                tempData["BankIdTestClaims"] = claimsTekst;
                tempData.Save();
                ctx.HandleResponse();
                ctx.Response.Redirect("/BankIdTest/Resultat");
                await Task.CompletedTask;
            },
            OnRemoteFailure = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.Redirect("/BankIdTest/Resultat?feil=" + Uri.EscapeDataString(ctx.Failure?.Message ?? "Ukjent feil"));
                return Task.CompletedTask;
            }
        };
    });
}

builder.Services.AddScoped<ProfesjonellInnloggingService>();

// --- Ekte BankID for admin/behandler sin FELLES innloggingsside (Pages/Konto/LoggInn). Se
// docs/beslutningslogg.md "Ekte BankID for admin/behandler (beta)". Bruker SAMME Idura-tenant/
// nøkler som diagnose-schemaet over, men EGEN scheme/CallbackPath — diagnose-schemaet skal
// fortsatt kun dumpe claims, denne skal faktisk logge noen inn. IKKE koblet til IBankIdProvider:
// det grensesnittet er ett synkront kall og passer ikke en ekte OIDC-redirect-flyt (se
// beslutningsloggen for hvorfor), så dette er en HELT EGEN påloggingsvei parallelt med
// LoggInn.cshtml.cs sin MockBankIdProvider-gren — begge ender i SAMME
// ProfesjonellInnloggingService for oppslag/betrodd enhet/2FA.
//
// BEVISST EGEN FLAGG (Miljo:EktBankIdProfesjonell), IKKE gjenbruk av Miljo:ErBeta — de to styrer
// helt urelaterte ting (denne: hvilken IdP admin/behandler-innlogging bruker; ErBeta: hvilken
// betalingsmodus som er AKTIV, som DEFAULTER TIL MOCK). Å gjenbruke ErBeta her ville betydd at
// å skru på ekte BankID på LIVE samtidig og stille satt live sine EKTE Vipps/Stripe-betalinger i
// Mock-modus til noen manuelt skrudde dem tilbake på Admin/MinSide — oppdaget og unngått
// 2026-09-19 FØR det ble satt på live, se beslutningsloggen "Ekte BankID for admin/behandler,
// del 2".
// EKTE Idura BankID-produksjonstenant (kun satt på live, se docs/beslutningslogg.md "Ekte BankID
// for admin/behandler, del 4") FORETREKKES her når satt — beta/lokal dev har ALDRI disse satt og
// fortsetter derfor uendret på test-tenanten, ingen egen "er dette produksjon"-flagg nødvendig.
var iduraProduksjonAuthority = builder.Configuration["BankId:IduraProduksjon:Authority"];
var iduraProduksjonClientId = builder.Configuration["BankId:IduraProduksjon:ClientId"];
var iduraProduksjonClientSecret = builder.Configuration["BankId:IduraProduksjon:ClientSecret"];
var iduraProduksjonKonfigurert =
    !string.IsNullOrWhiteSpace(iduraProduksjonAuthority) && !string.IsNullOrWhiteSpace(iduraProduksjonClientId) &&
    !string.IsNullOrWhiteSpace(iduraProduksjonClientSecret);

var iduraAuthorityInnlogging = iduraProduksjonKonfigurert ? iduraProduksjonAuthority : iduraAuthority;
var iduraClientIdInnlogging = iduraProduksjonKonfigurert ? iduraProduksjonClientId : iduraClientId;
var iduraClientSecretInnlogging = iduraProduksjonKonfigurert ? iduraProduksjonClientSecret : iduraClientSecret;

var ektBankIdProfesjonellAktiv = builder.Configuration["Miljo:EktBankIdProfesjonell"] == "true";
if (ektBankIdProfesjonellAktiv && !string.IsNullOrWhiteSpace(iduraAuthorityInnlogging) && !string.IsNullOrWhiteSpace(iduraClientIdInnlogging) && !string.IsNullOrWhiteSpace(iduraClientSecretInnlogging))
{
    // "high" (godtar engangskode+passord, ingen app) er RIKTIG for test-tenanten — en ekte
    // produksjonsbruker har derimot en aktivert BankID-app, så "substantial" er default for
    // produksjon (se docs/beslutningslogg.md "BankID-testintegrasjon via Idura":
    // "substantial" feilet FOR TEST med "You must activate the BankID app" — nettopp det en ekte
    // bruker HAR). Overstyrbar per miljø via BankId:IduraProduksjon:AcrValues om dette skulle vise
    // seg feil — IKKE verifisert ende-til-ende ennå, se beslutningsloggen.
    var iduraAcrValuesInnlogging = iduraProduksjonKonfigurert
        ? (builder.Configuration["BankId:IduraProduksjon:AcrValues"] ?? "urn:grn:authn:no:bankid:substantial")
        : (builder.Configuration["BankId:Idura:AcrValues"] ?? "urn:grn:authn:no:bankid:high");
    builder.Services.AddAuthentication().AddOpenIdConnect("BankIdInnlogging", options =>
    {
        options.Authority = iduraAuthorityInnlogging;
        options.ClientId = iduraClientIdInnlogging;
        options.ClientSecret = iduraClientSecretInnlogging;
        options.ResponseType = "code";
        options.ResponseMode = "form_post";
        options.CallbackPath = "/signin-bankid-innlogging";
        options.SaveTokens = false;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("ssn");

        // Samme begrunnelse som "BankIdTest"-schemaet over — form_post er en cross-site POST.
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.NonceCookie.SameSite = SameSiteMode.None;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = ctx =>
            {
                ctx.ProtocolMessage.AcrValues = iduraAcrValuesInnlogging;
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                // Nøyaktig HVILKEN claim som bærer personnummeret er ikke bekreftet ennå (krever
                // en reell interaktiv Idura-innlogging å observere, se beslutningsloggen) — prøver
                // "ssn" (samme navn som scopet vi ber om) først, faller ellers tilbake til en
                // generisk "11 siffer"-heuristikk (norske fødselsnummer er alltid 11 siffer) blant
                // ALLE claims som faktisk kom tilbake, i stedet for å anta feil og bare feile.
                var personnummer = ctx.Principal?.FindFirst("ssn")?.Value
                    ?? ctx.Principal?.Claims.FirstOrDefault(c => c.Value.Length == 11 && c.Value.All(char.IsDigit))?.Value;

                var tempData = ctx.HttpContext.RequestServices
                    .GetRequiredService<ITempDataDictionaryFactory>()
                    .GetTempData(ctx.HttpContext);

                if (personnummer is null)
                {
                    var claimsTekst = string.Join('\n', ctx.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}") ?? []);
                    tempData["BankIdFeilmelding"] = "Fant ikke personnummer i BankID-svaret. Rå claims (kun synlig for feilsøking): " + claimsTekst;
                    ctx.HandleResponse();
                    ctx.Response.Redirect("/Konto/LoggInn");
                    return Task.CompletedTask;
                }

                tempData["EktBankIdPersonnummer"] = personnummer;
                tempData["EktBankIdHuskMeg"] = ctx.Properties is not null && ctx.Properties.Items.TryGetValue("huskMeg", out var huskMegVerdi) ? huskMegVerdi : null;
                tempData["EktBankIdReturnUrl"] = ctx.Properties is not null && ctx.Properties.Items.TryGetValue("returnUrl", out var returnUrlVerdi) ? returnUrlVerdi : null;
                tempData.Save();

                ctx.HandleResponse();
                ctx.Response.Redirect("/Konto/BankIdFullfor");
                return Task.CompletedTask;
            },
            OnRemoteFailure = ctx =>
            {
                var tempData = ctx.HttpContext.RequestServices
                    .GetRequiredService<ITempDataDictionaryFactory>()
                    .GetTempData(ctx.HttpContext);
                tempData["BankIdFeilmelding"] = ctx.Failure?.Message ?? "BankID-innlogging feilet.";
                tempData.Save();
                ctx.HandleResponse();
                ctx.Response.Redirect("/Konto/LoggInn");
                return Task.CompletedTask;
            }
        };
    });
}

// --- Eksterne leverandører: mock i dev/test til ekte avtaler er på plass -
// TODO (fase 2/6): registrer ekte implementasjoner her, gatet på
// builder.Environment.IsDevelopment(), når BankID-/Vipps-/SMS-leverandør er
// valgt og avtale signert (se beslutningsloggen).
builder.Services.AddScoped<IBankIdProvider, MockBankIdProvider>();
builder.Services.AddScoped<ICaptchaProvider, MockCaptchaProvider>();

// Vipps: ekte ePayment API-klient når Vipps:ClientId/ClientSecret/SubscriptionKey/
// MerchantSerialNumber alle er satt, ellers MockVippsClient. Registrert som
// Singleton siden VippsPaymentClient cacher tilgangstoken i minnet på tvers av
// forespørsler (se VippsPaymentClient). Miljø (test/produksjon) styres av
// Vipps:Miljo — "Test" (standard) mot apitest.vipps.no, "Produksjon" mot api.vipps.no.
//
// BETA-MILJØ (Miljo:ErBeta, se docs/beslutningslogg.md "Beta-miljø"): i stedet
// for å velge ÉN klient for godt ved oppstart, kan Vipps:ClientId-settet
// (gjenbrukt som "Produksjon" — SAMME ekte konto som live) OG et separat
// Vipps:BetaTest-sett (Vipps sin egen test-merchant-avtale) begge være
// konfigurert samtidig, og en superadmin/dev-admin bytter mellom
// Mock/Test/Produksjon i live drift via Admin/MinSide — se
// BetaSwitchingVippsClient. Live/lokal dev er HELT UPÅVIRKET av dette:
// samme faste valg-ved-oppstart som før, leser aldri databasen.
var erBeta = builder.Configuration.GetValue("Miljo:ErBeta", false);

var vippsClientId = builder.Configuration["Vipps:ClientId"];
var vippsClientSecret = builder.Configuration["Vipps:ClientSecret"];
var vippsSubscriptionKey = builder.Configuration["Vipps:SubscriptionKey"];
var vippsMerchantSerialNumber = builder.Configuration["Vipps:MerchantSerialNumber"];
var vippsProduksjonKonfigurert =
    !string.IsNullOrWhiteSpace(vippsClientId) && !string.IsNullOrWhiteSpace(vippsClientSecret) &&
    !string.IsNullOrWhiteSpace(vippsSubscriptionKey) && !string.IsNullOrWhiteSpace(vippsMerchantSerialNumber);

var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
var stripeKonfigurert = !string.IsNullOrWhiteSpace(stripeSecretKey);

// Alltid registrert (uansett erBeta) — Admin/MinSide viser/skjuler bryter-UI-en basert på
// erBeta, men trenger denne tjenesten injisert ubetinget. Ufarlig å ha liggende ubrukt på
// live/lokal dev: den styrer ALDRI hvilken IVippsClient/IStripeClient som faktisk velges der.
builder.Services.AddScoped<BetaInnstillingService>();

if (erBeta)
{
    var vippsBetaTestClientId = builder.Configuration["Vipps:BetaTest:ClientId"];
    var vippsBetaTestClientSecret = builder.Configuration["Vipps:BetaTest:ClientSecret"];
    var vippsBetaTestSubscriptionKey = builder.Configuration["Vipps:BetaTest:SubscriptionKey"];
    var vippsBetaTestMerchantSerialNumber = builder.Configuration["Vipps:BetaTest:MerchantSerialNumber"];
    var vippsTestKonfigurert =
        !string.IsNullOrWhiteSpace(vippsBetaTestClientId) && !string.IsNullOrWhiteSpace(vippsBetaTestClientSecret) &&
        !string.IsNullOrWhiteSpace(vippsBetaTestSubscriptionKey) && !string.IsNullOrWhiteSpace(vippsBetaTestMerchantSerialNumber);

    builder.Services.AddSingleton<IVippsClient>(sp =>
    {
        var mock = new MockVippsClient(sp.GetRequiredService<ILogger<MockVippsClient>>());
        IVippsClient? test = vippsTestKonfigurert
            ? new VippsPaymentClient(
                new HttpClient(), "https://apitest.vipps.no", vippsBetaTestClientId!, vippsBetaTestClientSecret!,
                vippsBetaTestSubscriptionKey!, vippsBetaTestMerchantSerialNumber!, sp.GetRequiredService<ILogger<VippsPaymentClient>>())
            : null;
        // "Produksjon" på beta er BEVISST samme ekte konto som live — se
        // beslutningsloggen. Krever at Vipps:ClientId-settet faktisk er satt
        // på beta (ikke bare live) for at denne modusen skal ha noe å gjøre.
        IVippsClient? produksjon = vippsProduksjonKonfigurert
            ? new VippsPaymentClient(
                new HttpClient(), "https://api.vipps.no", vippsClientId!, vippsClientSecret!,
                vippsSubscriptionKey!, vippsMerchantSerialNumber!, sp.GetRequiredService<ILogger<VippsPaymentClient>>())
            : null;
        return new BetaSwitchingVippsClient(sp.GetRequiredService<IServiceScopeFactory>(), mock, test, produksjon);
    });

    builder.Services.AddScoped<IStripeClient>(sp =>
    {
        var mock = new MockStripeClient(sp.GetRequiredService<ILogger<MockStripeClient>>());
        IStripeClient? test = stripeKonfigurert
            ? new StripePaymentClient(stripeSecretKey!, sp.GetRequiredService<ILogger<StripePaymentClient>>())
            : null;
        return new BetaSwitchingStripeClient(sp.GetRequiredService<BetaInnstillingService>(), mock, test);
    });
}
else
{
    if (vippsProduksjonKonfigurert)
    {
        var vippsBaseUrl = builder.Configuration["Vipps:Miljo"] == "Produksjon"
            ? "https://api.vipps.no"
            : "https://apitest.vipps.no";
        builder.Services.AddSingleton<IVippsClient>(sp => new VippsPaymentClient(
            new HttpClient(), vippsBaseUrl, vippsClientId!, vippsClientSecret!, vippsSubscriptionKey!, vippsMerchantSerialNumber!,
            sp.GetRequiredService<ILogger<VippsPaymentClient>>()));
    }
    else
    {
        builder.Services.AddScoped<IVippsClient, MockVippsClient>();
    }

    // Stripe (kort/Apple Pay/Google Pay): ekte klient når Stripe:SecretKey er satt,
    // ellers MockStripeClient. Apple Pay krever i tillegg domeneverifisering i
    // Stripe Dashboard (se docs/beslutningslogg.md) — ikke noe som kan kodifiseres her.
    if (stripeKonfigurert)
    {
        builder.Services.AddScoped<IStripeClient>(sp =>
            new StripePaymentClient(stripeSecretKey!, sp.GetRequiredService<ILogger<StripePaymentClient>>()));
    }
    else
    {
        builder.Services.AddScoped<IStripeClient, MockStripeClient>();
    }
}

// E-post: ekte utsending via Azure Communication Services når "Acs:ConnectionString"
// er satt (kun i Azure test-App Service, aldri lokalt), ellers MockEmailSender —
// se AzureEmailSender og docs/beslutningslogg.md.
var acsConnectionString = builder.Configuration["Acs:ConnectionString"];
if (!string.IsNullOrEmpty(acsConnectionString))
{
    var emailSenderAddress = builder.Configuration["Email:SenderAddress"]
        ?? throw new InvalidOperationException("Acs:ConnectionString er satt, men Email:SenderAddress mangler.");
    builder.Services.AddScoped<IEmailSender>(sp =>
        new AzureEmailSender(acsConnectionString, emailSenderAddress, sp.GetRequiredService<ILogger<AzureEmailSender>>()));
}
else
{
    builder.Services.AddScoped<IEmailSender, MockEmailSender>();
}

// SMS: ekte utsending via Vonage Messages API når "Vonage:ApiKey"/
// "Vonage:ApiSecret"/"Sms:SenderId" alle er satt, ellers MockSmsSender.
// Valgt fremfor Azure Communication Services — Norge krever forhånds-
// registrert alfanumerisk avsender-ID der (6–8 uker), mens Vonage
// aksepterer et fritt avsendernavn til Norge umiddelbart, verifisert
// manuelt (se docs/beslutningslogg.md "SMS-integrasjon").
builder.Services.AddHttpClient();
var vonageApiKey = builder.Configuration["Vonage:ApiKey"];
var vonageApiSecret = builder.Configuration["Vonage:ApiSecret"];
var smsSenderId = builder.Configuration["Sms:SenderId"];
if (!string.IsNullOrWhiteSpace(vonageApiKey) && !string.IsNullOrWhiteSpace(vonageApiSecret) && !string.IsNullOrWhiteSpace(smsSenderId))
{
    builder.Services.AddScoped<ISmsSender>(sp => new VonageSmsSender(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        vonageApiKey, vonageApiSecret, smsSenderId,
        sp.GetRequiredService<ILogger<VonageSmsSender>>()));
}
else
{
    builder.Services.AddScoped<ISmsSender, MockSmsSender>();
}

// --- Web ------------------------------------------------------------------
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/Administratorer", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Behandlere", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Pasienter", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Grupper", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Tester", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Tildel", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Partnere", "SuperadminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Tester/Prising", "SuperadminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Okonomi", "SuperadminOmrade");
    options.Conventions.AuthorizeAreaFolder("Behandlerportal", "/Behandlere", "BehandlerOmrade");
    options.Conventions.AuthorizeAreaFolder("Behandlerportal", "/Pasienter", "BehandlerOmrade");
    options.Conventions.AuthorizeAreaFolder("Behandlerportal", "/Grupper", "BehandlerOmrade");
    options.Conventions.AuthorizeAreaFolder("Behandlerportal", "/Tildel", "BehandlerOmrade");
    options.Conventions.AuthorizeAreaFolder("Behandlerportal", "/MinPartner", "PartnerAdminOmrade");
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("mysql");

var app = builder.Build();

app.UseStagingGate();

// BEVISST styrt av Miljo:TillatUtviklingsSnarveier, IKKE app.Environment.IsDevelopment() —
// live kjører "Development" KUN for auto-migrering ved oppstart (se dev-seed-blokken under)
// og skal likevel ha full produksjonsherding nå som StagingGate ikke lenger dekker den, se
// Security/Miljo.cs og docs/beslutningslogg.md "StagingGate fjernet fra live".
if (!TestBase.Web.Security.Miljo.TillatUtviklingsSnarveier(app.Configuration))
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHealthChecks("/health");
app.MapPaymentWebhooks();

// --- Dev-seed: fiktiv administrator slik at innlogging virker uten manuelle
// steg lokalt. KUN i Development, og KUN syntetisk testdata (fiktivt
// personnummer) — jf. "ingen ekte pasientdata i dev/test noensinne".
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Kjør ventende EF Core-migrasjoner automatisk ved oppstart. Nødvendig fordi
    // Azure test-App Service sin MySQL Flexible Server-brannmur kun har
    // "AllowAzureServices" (ingen regel for utviklerens lokale maskin) — det
    // finnes ingen annen etablert vei for å få nye migrasjoner til psytest.no.
    // Trygt her: kun syntetisk testdata i dette miljøet (jf. "ingen ekte
    // pasientdata i dev/test noensinne"), og MigrateAsync er idempotent —
    // ingenting skjer på oppstarter uten ventende migrasjoner.
    await db.Database.MigrateAsync();

    if (!await db.Administratorer.AnyAsync())
    {
        var authService = scope.ServiceProvider.GetRequiredService<AdminAuthenticationService>();
        var devAdmin = new Administrator
        {
            AdminId = "dev-admin",
            MobilNr = "+4700000001",
            Email = "dev-admin@example.test",
            FulltNavn = "Dev Administrator",
            // Bevisst forskjellig fra MockBankIdProvider sitt faste testpersonnummer
            // (01019012345) — denne kontoen logger uansett inn med passord, ikke
            // BankID, men to administratorer med samme personnummer ville gjort
            // BankID-oppslag (FinnVedPersonnummerAsync) tvetydig.
            Personnummer = "01010000000",
            HprNr = "0000000",
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        devAdmin.PasswordHash = authService.HashPassord(devAdmin, "utvikler123");

        db.Administratorer.Add(devAdmin);
        await db.SaveChangesAsync();
    }

    // Seed av brukerens EGEN administrator-konto (ekte personnummer, IKKE syntetisk
    // testdata som resten av dev-seedingen) — ALDRI en literal her, kun konfigurasjon
    // (dotnet user-secrets lokalt, Key Vault-hemmeligheter i Azure), siden dette
    // repoet er offentlig på GitHub. Fraværende konfigurasjon = av, samme mønster som
    // Vonage/ACS/Idura. Kjører idempotent ved HVER oppstart, uavhengig av
    // dev-admin-blokken over, slik at kontoen overlever enhver database-
    // gjenoppretting (lokalt eller i Azure test-App Service, som fortsatt kjører i
    // Development-modus) uten manuell gjeninnlegging. Se beslutningsloggen
    // "Seed av brukerens egen admin-konto". Bør revurderes den dagen en reell
    // produksjonssetting med ekte BankID-avtale kommer — da bør kontoen opprettes via
    // den faktiske produksjonsflyten, ikke fortsette å leve som en oppstartsseed.
    var seedAdminPersonnummer = app.Configuration["Seed:AdminPersonnummer"];
    if (!string.IsNullOrWhiteSpace(seedAdminPersonnummer))
    {
        var seedAuthService = scope.ServiceProvider.GetRequiredService<AdminAuthenticationService>();
        var seedAdminNavn = app.Configuration["Seed:AdminNavn"] ?? "Administrator";
        var seedAdminId = app.Configuration["Seed:AdminId"] ?? seedAdminNavn.ToLowerInvariant().Replace(' ', '-');

        var seedAdmin = await seedAuthService.FinnVedPersonnummerAsync(seedAdminPersonnummer)
            // FinnVedPersonnummerAsync ekskluderer arkiverte kontoer — hvis denne
            // spesifikke kontoen av en eller annen grunn ble arkivert, ville en blind
            // "opprett ny" her krasje hele oppstarten på AdminId sin unike indeks
            // (skjedde reelt 2026-09-13). Fall tilbake til å finne den uansett
            // arkiveringsstatus via AdminId, og selvhelbrede (gjenopprett + rett opp
            // personnummer) fremfor å krasje eller stille feile.
            ?? await db.Administratorer.FirstOrDefaultAsync(a => a.AdminId == seedAdminId);
        if (seedAdmin is null)
        {
            seedAdmin = new Administrator
            {
                AdminId = seedAdminId,
                MobilNr = app.Configuration["Seed:AdminMobilNr"] ?? "+4700000000",
                Email = app.Configuration["Seed:AdminEpost"] ?? "admin@example.test",
                FulltNavn = seedAdminNavn,
                Personnummer = seedAdminPersonnummer,
                HprNr = "0000000",
                OpprettetUtc = DateTimeOffset.UtcNow
            };
            db.Administratorer.Add(seedAdmin);
        }
        else
        {
            seedAdmin.ErArkivert = false;
            seedAdmin.ArkivertUtc = null;
            seedAdmin.Personnummer = seedAdminPersonnummer;
        }

        // Denne kontoen er den eneste som noensinne skal ha Superadmin (se
        // docs/beslutningslogg.md "Partner System + Test Monetization") — satt/
        // selvhelbredende her ved hver oppstart, samme mønster som resten av denne seeden.
        if (!seedAdmin.ErSuperadmin)
        {
            seedAdmin.ErSuperadmin = true;
        }

        await db.SaveChangesAsync();
    }

    // Regenerer innebygde tester (WHO-5 m.fl.) — samme idempotente mekanisme
    // som også er tilgjengelig via en admin-knapp i alle miljøer, se
    // Areas/Admin/Pages/Tester/Index.cshtml.cs.
    var testService = scope.ServiceProvider.GetRequiredService<TestService>();
    foreach (var seeder in scope.ServiceProvider.GetServices<IInnebygdTestSeeder>())
    {
        await seeder.SeedAsync(testService);
    }

    // Retter opp eksisterende hull + dekker tester opprettet før denne
    // regelen fantes — se TestService.GiAllePartnereTilgangTilAlleTesterAsync.
    await testService.GiAllePartnereTilgangTilAlleTesterAsync();
}

app.Run();

// Gjør Program-klassen offentlig og referérbar for WebApplicationFactory<Program>
// i tests/TestBase.IntegrationTests — endrer ikke oppførsel, kun synlighet.
public partial class Program;
