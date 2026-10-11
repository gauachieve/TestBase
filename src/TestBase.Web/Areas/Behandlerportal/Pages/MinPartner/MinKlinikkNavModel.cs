namespace TestBase.Web.Areas.Behandlerportal.Pages.MinPartner;

/// <summary>
/// Modell for den delte, persistente "Min klinikk"-navigasjonen
/// (_MinKlinikkNav.cshtml) — vist på TOPPEN av hver side under MinPartner,
/// se "Klinikk-oppdatering, del 3" i docs/beslutningslogg.md. Hver PageModel
/// under MinPartner henter AntallTilgjengeligeTester selv (ren
/// COUNT-spørring, ikke verdt en delt tjeneste for én linje).
/// </summary>
public sealed record MinKlinikkNavModel(bool ErPartnerAdministrator, int AntallTilgjengeligeTester);
