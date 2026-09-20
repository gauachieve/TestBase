using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Domain;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Web.Security;

namespace TestBase.Web.Pages.PasientRegistrering;

/// <summary>
/// Offentlig, uautentisert side en pasient åpner fra "fullfør profilen din"-
/// påminnelsen sendt etter QR-selvregistrering (se PasientInvitasjonService.
/// RegistrerViaQrAsync/SendFullforProfilLenkeAsync). I MOTSETNING til
/// Fullfor.cshtml (som fullfører en behandler-opprettet invitasjon FØR
/// pasienten er aktiv) fyller denne siden inn felter som BEVISST ble hoppet
/// over ved selve QR-registreringen, for en pasient som ALLEREDE er aktiv og
/// kan bruke systemet — ingen hastverk, samme lenke kan brukes flere ganger.
/// </summary>
public sealed class FullforProfilModel : PageModel
{
    private readonly PasientInvitasjonService _pasientService;

    public FullforProfilModel(PasientInvitasjonService pasientService)
    {
        _pasientService = pasientService;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    public string? Navn { get; set; }

    // Nullable, IKKE string med default "" — se kjente fallgruver i CLAUDE.md om
    // at et tomt skjemafelt bindes til null, ikke "", for en string-property.
    [BindProperty]
    public string? MobilNr { get; set; }

    [BindProperty]
    public string? Epost { get; set; }

    [BindProperty]
    public string? Personnummer { get; set; }

    [BindProperty]
    public BiologiskKjonn? BiologiskKjonnVedFodsel { get; set; }

    [BindProperty]
    public Kjonnsidentitet? Kjonnsidentitet { get; set; }

    [BindProperty]
    public string? KjonnsidentitetSpesifisert { get; set; }

    [BindProperty]
    public string? Adresse { get; set; }

    // Bot-vern (se BotVern) — Nettside er et honeypot-felt som skal stå tomt. Ekstra
    // viktig her siden dette er en helt åpen, ugatede skjema-side (ingen innlogging kreves).
    [BindProperty]
    public string? Nettside { get; set; }

    [BindProperty]
    public string Vist { get; set; } = string.Empty;

    public bool GyldigLenke { get; private set; }
    public bool Oppdatert { get; private set; }
    public string? Feilmelding { get; private set; }
    public bool HaddeAlleredePersonnummer { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var pasient = await _pasientService.FinnVedProfilFullforingTokenAsync(Token, cancellationToken);
        if (pasient is null)
        {
            Feilmelding = "Lenken er ugyldig.";
            return Page();
        }

        GyldigLenke = true;
        Navn = pasient.Navn;
        MobilNr = pasient.MobilNr;
        Epost = pasient.Email;
        Personnummer = pasient.Personnummer;
        HaddeAlleredePersonnummer = !string.IsNullOrWhiteSpace(pasient.Personnummer);
        BiologiskKjonnVedFodsel = pasient.BiologiskKjonnVedFodsel;
        Kjonnsidentitet = pasient.Kjonnsidentitet;
        KjonnsidentitetSpesifisert = pasient.KjonnsidentitetSpesifisert;
        Adresse = pasient.Adresse;
        Vist = BotVern.NyttVisningstidspunkt();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var pasient = await _pasientService.FinnVedProfilFullforingTokenAsync(Token, cancellationToken);
        if (pasient is null)
        {
            Feilmelding = "Lenken er ugyldig.";
            return Page();
        }

        GyldigLenke = true;
        HaddeAlleredePersonnummer = !string.IsNullOrWhiteSpace(pasient.Personnummer);

        if (BotVern.ErSannsynligvisBot(Nettside, Vist))
        {
            Feilmelding = "Noe gikk galt. Prøv igjen.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(MobilNr) && string.IsNullOrWhiteSpace(Epost))
        {
            Feilmelding = "Oppgi enten mobilnummer eller e-post (minst én av dem).";
            return Page();
        }

        var nyttPersonnummer = string.IsNullOrWhiteSpace(Personnummer) ? null : Personnummer.Trim();
        if (nyttPersonnummer is not null && nyttPersonnummer != pasient.Personnummer)
        {
            if (!PersonnummerValidator.ErGyldigFormat(nyttPersonnummer))
            {
                Feilmelding = "Personnummer må bestå av nøyaktig 11 siffer — eller la feltet stå tomt.";
                return Page();
            }

            if (await _pasientService.HarAnnenPasientMedPersonnummerAsync(nyttPersonnummer, cancellationToken))
            {
                Feilmelding = "Det finnes allerede en pasient med dette personnummeret. Ta kontakt med din behandler.";
                return Page();
            }
        }

        await _pasientService.OppdaterProfilAsync(
            pasient, Navn, nyttPersonnummer, MobilNr ?? string.Empty, Epost ?? string.Empty,
            BiologiskKjonnVedFodsel, Kjonnsidentitet, KjonnsidentitetSpesifisert, Adresse, cancellationToken);

        Oppdatert = true;
        return Page();
    }
}
