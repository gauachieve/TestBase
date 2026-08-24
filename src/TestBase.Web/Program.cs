using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Providers;
using TestBase.Shared.Providers.Mock;
using TestBase.Shared.Security;

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

// Admin-, Behandlerportal- og Pasientportal-området deler samme cookie-scheme
// (én ICurrentUserContext-implementasjon dekker alle tre, se
// AuthenticatedCurrentUserContext), men har hver sin innloggingsside —
// omdiriger til riktig portal basert på hvilket område forespørselen gjaldt,
// i stedet for én global LoginPath som alltid går til Admin.
static string InnloggingsstiFor(PathString sti) =>
    sti.StartsWithSegments("/Behandlerportal") ? "/Behandlerportal/Konto/LoggInn" :
    sti.StartsWithSegments("/Pasientportal") ? "/Pasientportal/Konto/LoggInn" :
    "/Admin/Konto/LoggInn";

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Konto/LoggInn";
        options.LogoutPath = "/Admin/Konto/LoggUt";
        options.AccessDeniedPath = "/Admin/Konto/LoggInn";
        options.ExpireTimeSpan = TimeSpan.FromDays(builder.Configuration.GetValue("Auth:RememberMeDays", 30));
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.Redirect(InnloggingsstiFor(context.Request.Path));
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect(InnloggingsstiFor(context.Request.Path));
            return Task.CompletedTask;
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

// --- Web ------------------------------------------------------------------
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Admin", "/Administratorer", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Behandlere", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Admin", "/Tester", "AdminOmrade");
    options.Conventions.AuthorizeAreaFolder("Behandlerportal", "/Behandlere", "BehandlerOmrade");
    options.Conventions.AuthorizeAreaFolder("Behandlerportal", "/Pasienter", "BehandlerOmrade");
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("mysql");

var app = builder.Build();

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
}

app.Run();

// Gjør Program-klassen offentlig og referérbar for WebApplicationFactory<Program>
// i tests/TestBase.IntegrationTests — endrer ikke oppførsel, kun synlighet.
public partial class Program;
