using Microsoft.AspNetCore.Authentication.Cookies;
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
builder.Services.AddScoped<AdminAuthenticationService>();
builder.Services.AddScoped<BehandlerAuthenticationService>();
builder.Services.AddScoped<BehandlerInvitasjonService>();
builder.Services.AddScoped<PasientAuthenticationService>();
builder.Services.AddScoped<PasientInvitasjonService>();
builder.Services.AddScoped<TestService>();
builder.Services.AddScoped<TestTildelingsService>();
builder.Services.AddScoped<BehandlerMeldingService>();
builder.Services.AddScoped<PaaminnelseService>();
builder.Services.AddHostedService<DagligPaaminnelseBakgrunnstjeneste>();

// Skåringsmotor og innebygde, kode-definerte tester (fase 5 — bevist ut med WHO-5).
builder.Services.AddScoped<ITestSkaaringsberegner, Who5Skaaringsberegner>();
builder.Services.AddScoped<IInnebygdTestSeeder, Who5TestSeeder>();

// Admin og Behandlerportal deler nå én samlet innloggingsside (/Konto/LoggInn
// — BankID finner personen og logger inn på høyeste rolle selv, uten at
// brukeren velger portal, se Pages/Konto/LoggInn.cshtml.cs). Pasientportal har
// fortsatt egen inngang, siden pasienter er en separat gruppe med egen
// landingsside (/Pasienter) — se beslutningsloggen. Redirect til login bærer
// alltid med seg en returnUrl (lest av begge LoggInn-sidene, validert med
// Url.IsLocalUrl før bruk) slik at man havner tilbake der man egentlig skulle
// etter innlogging — og for pasientportalens tildelte-test-lenker (jf.
// beslutningsloggen "BankID personnr-forhåndsutfylling fra testlenke") slår
// vi i tillegg opp riktig personnummer for AKKURAT den tildelingen (kun i
// Development, aldri i produksjon) slik at pasienten ikke selv må vite/skrive
// inn sitt (mock-)personnummer for å logge inn på en lenke hen fikk tilsendt.
static async Task<string> InnloggingsstiForAsync(HttpContext httpContext, PathString sti, QueryString opprinneligQuery)
{
    var innloggingssti = sti.StartsWithSegments("/Pasientportal") ? "/Pasientportal/Konto/LoggInn" : "/Konto/LoggInn";
    var returnerTil = $"{sti}{opprinneligQuery}";
    var query = QueryString.Create("returnUrl", returnerTil);

    var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
    if (env.IsDevelopment() && sti.StartsWithSegments("/Pasientportal/Tester/Fyll", out var rest))
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
    options.AddPolicy("AdminOmrade", policy =>
        policy.RequireRole(nameof(UserRole.Administrator), nameof(UserRole.Utvikler)));
    options.AddPolicy("BehandlerOmrade", policy =>
        policy.RequireRole(nameof(UserRole.Behandler), nameof(UserRole.Utvikler)));
    options.AddPolicy("PasientOmrade", policy =>
        policy.RequireRole(nameof(UserRole.Pasient), nameof(UserRole.Utvikler)));
});

// --- Eksterne leverandører: mock i dev/test til ekte avtaler er på plass -
// TODO (fase 2/6): registrer ekte implementasjoner her, gatet på
// builder.Environment.IsDevelopment(), når BankID-/Vipps-/SMS-/e-post-
// leverandør er valgt og avtale signert (se beslutningsloggen).
builder.Services.AddScoped<IBankIdProvider, MockBankIdProvider>();
builder.Services.AddScoped<IVippsClient, MockVippsClient>();
builder.Services.AddScoped<ISmsSender, MockSmsSender>();
builder.Services.AddScoped<IEmailSender, MockEmailSender>();
builder.Services.AddScoped<ICaptchaProvider, MockCaptchaProvider>();

// --- Web ------------------------------------------------------------------
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/Administratorer", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Behandlere", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Pasienter", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Tester", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Tildel", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Behandlerportal", "/Behandlere", "BehandlerOmrade");
    options.Conventions.AuthorizeAreaFolder("Behandlerportal", "/Pasienter", "BehandlerOmrade");
    options.Conventions.AuthorizeAreaFolder("Behandlerportal", "/Tildel", "BehandlerOmrade");
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("mysql");

var app = builder.Build();

app.UseStagingGate();

if (!app.Environment.IsDevelopment())
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

// --- Dev-seed: fiktiv administrator slik at innlogging virker uten manuelle
// steg lokalt. KUN i Development, og KUN syntetisk testdata (fiktivt
// personnummer) — jf. "ingen ekte pasientdata i dev/test noensinne".
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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

    // Regenerer innebygde tester (WHO-5 m.fl.) — samme idempotente mekanisme
    // som også er tilgjengelig via en admin-knapp i alle miljøer, se
    // Areas/Admin/Pages/Tester/Index.cshtml.cs.
    var testService = scope.ServiceProvider.GetRequiredService<TestService>();
    foreach (var seeder in scope.ServiceProvider.GetServices<IInnebygdTestSeeder>())
    {
        await seeder.SeedAsync(testService);
    }
}

app.Run();

// Gjør Program-klassen offentlig og referérbar for WebApplicationFactory<Program>
// i tests/TestBase.IntegrationTests — endrer ikke oppførsel, kun synlighet.
public partial class Program;
