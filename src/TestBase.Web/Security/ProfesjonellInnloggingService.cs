using Microsoft.AspNetCore.Mvc;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Security;

/// <summary>
/// Det som skjer ETTER at en personnummer er bekreftet for en administrator/
/// behandler ("profesjonell bruker") — uavhengig av OM personnummeret kom fra
/// MockBankIdProvider (Pages/Konto/LoggInn.cshtml.cs sin OnPostAsync) eller fra
/// en ekte Idura BankID-innlogging (Pages/Konto/BankIdFullfor.cshtml.cs, kun
/// aktiv på beta — se Program.cs "BankIdInnlogging"-schema og
/// docs/beslutningslogg.md "Ekte BankID for admin/behandler (beta)").
/// Trukket ut hit slik at begge veiene garantert oppfører seg IDENTISK
/// (samme "høyeste rolle vinner"-oppslag, samme betrodd-enhet/2FA-logikk),
/// i stedet for å risikere at de to stiene sklir fra hverandre over tid.
/// </summary>
public sealed class ProfesjonellInnloggingService
{
    private readonly AdminAuthenticationService _adminAuth;
    private readonly BehandlerAuthenticationService _behandlerAuth;
    private readonly IAuditLogger _auditLogger;
    private readonly IConfiguration _configuration;

    public ProfesjonellInnloggingService(
        AdminAuthenticationService adminAuth, BehandlerAuthenticationService behandlerAuth,
        IAuditLogger auditLogger, IConfiguration configuration)
    {
        _adminAuth = adminAuth;
        _behandlerAuth = behandlerAuth;
        _auditLogger = auditLogger;
        _configuration = configuration;
    }

    /// <summary>Auditlogg-kilden vises i AuditLogEntry sitt "detaljer"-felt — hold korte, konsistente strenger her.</summary>
    public async Task<ProfesjonellInnloggingResultat> FullforAsync(
        string personnummer, bool huskMeg, string? returnUrl, string auditlogKilde, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var administrator = await _adminAuth.FinnVedPersonnummerAsync(personnummer, cancellationToken);
        if (administrator is not null)
        {
            var administratorRolle = administrator.ErSuperadmin ? UserRole.Superadmin : UserRole.Administrator;
            if (BetroddEnhet.ErBetrodd(httpContext, ToFaktorPrincipalType.Administrator, administrator.Id))
            {
                await AuthSignIn.LoggInnAsync(httpContext, "administrator", administrator.Id, administrator.FulltNavn, administratorRolle, huskMeg);
                await _auditLogger.LogAsync(
                    administrator.AdminId, administratorRolle.ToString(), "InnloggingOk",
                    nameof(Administrator), administrator.Id.ToString(), $"{auditlogKilde} (betrodd enhet — 2FA hoppet over)", cancellationToken);
                return ProfesjonellInnloggingResultat.FerdigInnlogget(MaalEtterInnlogging(returnUrl, "Admin", "/Administratorer/Index"));
            }

            var kode = await _adminAuth.StartToFaktorAsync(administrator, cancellationToken);
            return ProfesjonellInnloggingResultat.TrengerToFaktor(
                administratorRolle, administrator.Id, huskMeg, returnUrl, Miljo.TillatUtviklingsSnarveier(_configuration) ? kode : null);
        }

        var behandler = await _behandlerAuth.FinnVedPersonnummerAsync(personnummer, cancellationToken);
        if (behandler is not null)
        {
            switch (behandler.Status)
            {
                case BehandlerStatus.Invitert:
                    return ProfesjonellInnloggingResultat.Feil("Du har ikke fullført registreringen ennå. Bruk invitasjonslenken du mottok på SMS/e-post.");
                case BehandlerStatus.Fryst:
                    return ProfesjonellInnloggingResultat.Feil("Kontoen din er fryst. Kontakt administrator.");
                case BehandlerStatus.Arkivert:
                    return ProfesjonellInnloggingResultat.Feil("Kontoen din er arkivert.");
            }

            if (BetroddEnhet.ErBetrodd(httpContext, ToFaktorPrincipalType.Behandler, behandler.Id))
            {
                await AuthSignIn.LoggInnAsync(
                    httpContext, "behandler", behandler.Id, behandler.Visningsnavn ?? "Behandler", UserRole.Behandler, huskMeg,
                    behandler.PartnerId, behandler.ErPartnerAdministrator);
                await _auditLogger.LogAsync(
                    $"behandler:{behandler.Id}", nameof(UserRole.Behandler), "InnloggingOk",
                    nameof(Behandler), behandler.Id.ToString(), $"{auditlogKilde} (betrodd enhet — 2FA hoppet over)", cancellationToken);

                if (behandler.BrukeravtaleGodkjentVersjon != Brukeravtale.GjeldendeVersjon)
                {
                    return ProfesjonellInnloggingResultat.FerdigInnlogget(new RedirectToPageResult("/Konto/GodkjennAvtale", routeValues: new { area = "Behandlerportal" }));
                }
                return ProfesjonellInnloggingResultat.FerdigInnlogget(MaalEtterInnlogging(returnUrl, "Behandlerportal", "/Pasienter/Index"));
            }

            var kode = await _behandlerAuth.StartToFaktorAsync(behandler, cancellationToken);
            return ProfesjonellInnloggingResultat.TrengerToFaktor(
                UserRole.Behandler, behandler.Id, huskMeg, returnUrl, Miljo.TillatUtviklingsSnarveier(_configuration) ? kode : null);
        }

        return ProfesjonellInnloggingResultat.Feil("Fant ingen administrator- eller behandlerkonto for denne BankID-personen.");
    }

    private static IActionResult MaalEtterInnlogging(string? returnUrl, string fallbackArea, string fallbackPage) =>
        !string.IsNullOrEmpty(returnUrl) && ErLokalUrl(returnUrl)
            ? new LocalRedirectResult(returnUrl)
            : new RedirectToPageResult(fallbackPage, routeValues: new { area = fallbackArea });

    /// <summary>Samme regel som Microsoft.AspNetCore.Mvc.Routing.UrlHelperBase.IsLocalUrl — reimplementert her for å unngå å måtte DI-registrere IUrlHelperFactory/IActionContextAccessor bare for denne ene sjekken utenfor en PageModel-kontekst.</summary>
    private static bool ErLokalUrl(string url) =>
        !string.IsNullOrEmpty(url) &&
        ((url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) ||
         (url[0] == '~' && url.Length > 1 && url[1] == '/'));
}

public sealed class ProfesjonellInnloggingResultat
{
    public bool ErFerdig { get; private init; }
    public IActionResult? FerdigResultat { get; private init; }

    public bool TrengerToFaktorFlagg { get; private init; }
    public UserRole? ToFaktorRolle { get; private init; }
    public long? ToFaktorId { get; private init; }
    public bool ToFaktorHuskMeg { get; private init; }
    public string? ToFaktorReturnUrl { get; private init; }
    public string? DevToFaktorKode { get; private init; }

    public string? Feilmelding { get; private init; }

    public static ProfesjonellInnloggingResultat FerdigInnlogget(IActionResult resultat) =>
        new() { ErFerdig = true, FerdigResultat = resultat };

    public static ProfesjonellInnloggingResultat TrengerToFaktor(UserRole rolle, long id, bool huskMeg, string? returnUrl, string? devKode) =>
        new()
        {
            TrengerToFaktorFlagg = true, ToFaktorRolle = rolle, ToFaktorId = id,
            ToFaktorHuskMeg = huskMeg, ToFaktorReturnUrl = returnUrl, DevToFaktorKode = devKode
        };

    public static ProfesjonellInnloggingResultat Feil(string melding) => new() { Feilmelding = melding };
}
