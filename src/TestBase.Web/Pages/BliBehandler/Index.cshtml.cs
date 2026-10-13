using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.BliBehandler;

/// <summary>
/// Offentlig, uautentisert selvregistreringsside for en uavhengig behandler
/// (ikke invitert av en administrator/kollega) — se docs/beslutningslogg.md
/// "Abonnement/subscription-system". Oppretter KUN kontakt-stubben + sender
/// verifiseringskoder, nøyaktig som BehandlerInvitasjonService.InviterAsync
/// gjør for en administrator-invitert behandler — bare med
/// InvitertAvAdministratorId/InvitertAvBehandlerId BEGGE null (selvregistrert),
/// og uten å vente på at man selv klikker lenken i SMS/e-post: man sendes
/// rett videre til den eksisterende Pages/Inviter/Fullfor-siden siden man
/// allerede sitter med token.
///
/// Ekte Stripe-kortopptak (SetupIntent) er IKKE bygget ennå — alle som
/// registrerer seg her starter i "test-modus" (Behandler.HarKortLagret=false)
/// til en Superadmin manuelt slår det på (Admin/Behandlere/Rediger) eller
/// ekte kortintegrasjon er på plass. Dette er tydelig kommunisert på siden,
/// ikke en skjult begrensning.
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly BehandlerInvitasjonService _invitasjonService;

    public IndexModel(BehandlerInvitasjonService invitasjonService)
    {
        _invitasjonService = invitasjonService;
    }

    [BindProperty]
    public string? MobilNr { get; set; }

    [BindProperty]
    public string? Epost { get; set; }

    // Bot-vern (se BotVern) — Nettside er et honeypot-felt som skal stå tomt.
    [BindProperty]
    public string? Nettside { get; set; }

    [BindProperty]
    public string Vist { get; set; } = string.Empty;

    public string? Feilmelding { get; private set; }

    public void OnGet()
    {
        Vist = BotVern.NyttVisningstidspunkt();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (BotVern.ErSannsynligvisBot(Nettside, Vist))
        {
            Feilmelding = "Noe gikk galt. Prøv igjen.";
            Vist = BotVern.NyttVisningstidspunkt();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(MobilNr) && string.IsNullOrWhiteSpace(Epost))
        {
            Feilmelding = "Skriv inn mobilnummer eller e-post for å starte.";
            Vist = BotVern.NyttVisningstidspunkt();
            return Page();
        }

        var resultat = await _invitasjonService.InviterAsync(
            mobilNr: string.IsNullOrWhiteSpace(MobilNr) ? null : MobilNr,
            epost: string.IsNullOrWhiteSpace(Epost) ? null : Epost,
            administratorId: null, behandlerId: null,
            baseUrl: $"{Request.Scheme}://{Request.Host}", cancellationToken: cancellationToken);

        var token = resultat.Lenke.Split('/').Last();
        return RedirectToPage("/Inviter/Fullfor", new { token });
    }
}
