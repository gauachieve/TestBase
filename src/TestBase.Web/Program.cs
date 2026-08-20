using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
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

// --- Bruker-kontekst ------------------------------------------------------
// TODO (fase 2): erstatt DevCurrentUserContext med en ekte implementasjon
// (BankID i prod, passord i dev-modus for admin) når autentisering bygges.
builder.Services.AddScoped<ICurrentUserContext, DevCurrentUserContext>();

// --- Eksterne leverandører: mock i dev/test til ekte avtaler er på plass -
// TODO (fase 2/6): registrer ekte implementasjoner her, gatet på
// builder.Environment.IsDevelopment(), når BankID-/Vipps-/SMS-/e-post-
// leverandør er valgt og avtale signert (se beslutningsloggen).
builder.Services.AddScoped<IBankIdProvider, MockBankIdProvider>();
builder.Services.AddScoped<IVippsClient, MockVippsClient>();
builder.Services.AddScoped<ISmsSender, MockSmsSender>();
builder.Services.AddScoped<IEmailSender, MockEmailSender>();

// --- Web ------------------------------------------------------------------
builder.Services.AddRazorPages();
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
app.UseAuthorization();

app.MapRazorPages();
app.MapHealthChecks("/health");

app.Run();
