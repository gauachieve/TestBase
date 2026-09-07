using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Partnere;

/// <summary>
/// Rediger partnerens egne felt/abonnement, og administrer hvilke behandlere
/// som er tilknyttet partneren (kun Superadmin gjør dette — partner-admin sin
/// egen, snevrere variant er Behandlerportal/MinPartner/Behandlere.cshtml).
/// </summary>
public sealed class RedigerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;
    private readonly BehandlerInvitasjonService _invitasjonService;

    public RedigerModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser, BehandlerInvitasjonService invitasjonService)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _invitasjonService = invitasjonService;
    }

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public string? KontaktpersonNavn { get; set; }

    [BindProperty]
    public string? KontaktEpost { get; set; }

    [BindProperty]
    public string? KontaktMobilNr { get; set; }

    [BindProperty]
    public bool HarAktivtAbonnement { get; set; }

    public long PartnerId { get; private set; }
    public string PartnerNavn { get; private set; } = string.Empty;
    public List<Behandler> TilknyttedeBehandlere { get; private set; } = new();
    public string? Feilmelding { get; private set; }
    public string? Infomelding { get; private set; }

    /// <summary>Satt når et innsendt e-postforsøk traff en eksisterende, uavhengig behandler — venter på eksplisitt bekreftelse før faktisk kobling, se OnPostInviterEllerKobleAsync.</summary>
    public Behandler? MatchFunnet { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is null)
        {
            return NotFound();
        }

        PartnerId = partner.Id;
        PartnerNavn = partner.Navn;
        Navn = partner.Navn;
        KontaktpersonNavn = partner.KontaktpersonNavn;
        KontaktEpost = partner.KontaktEpost;
        KontaktMobilNr = partner.KontaktMobilNr;
        HarAktivtAbonnement = partner.HarAktivtAbonnement;

        await LastBehandlerlisterAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Navn er obligatorisk.";
            PartnerId = id;
            PartnerNavn = partner.Navn;
            await LastBehandlerlisterAsync(id, cancellationToken);
            return Page();
        }

        partner.Navn = Navn;
        partner.KontaktpersonNavn = KontaktpersonNavn;
        partner.KontaktEpost = KontaktEpost;
        partner.KontaktMobilNr = KontaktMobilNr;
        partner.HarAktivtAbonnement = HarAktivtAbonnement;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterPartner",
            nameof(Partner), partner.Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostLeggTilBehandlerAsync(long id, long behandlerId, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId && b.PartnerId == null, cancellationToken);
        if (behandler is not null)
        {
            behandler.PartnerId = id;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "LeggBehandlerTilPartner",
                nameof(Behandler), behandler.Id.ToString(), $"PartnerId {id}", cancellationToken);
        }

        return RedirectToPage(new { id });
    }

    /// <summary>Fjerner (unlinker) behandleren fra partneren — arkiverer IKKE kontoen, se docs/beslutningslogg.md.</summary>
    public async Task<IActionResult> OnPostFjernBehandlerAsync(long id, long behandlerId, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId && b.PartnerId == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.PartnerId = null;
            behandler.ErPartnerAdministrator = false;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "FjernBehandlerFraPartner",
                nameof(Behandler), behandler.Id.ToString(), $"PartnerId {id}", cancellationToken);
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostTogglePartnerAdminAsync(long id, long behandlerId, CancellationToken cancellationToken)
    {
        var behandler = await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == behandlerId && b.PartnerId == id, cancellationToken);
        if (behandler is not null)
        {
            behandler.ErPartnerAdministrator = !behandler.ErPartnerAdministrator;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "EndrePartnerAdministrator",
                nameof(Behandler), behandler.Id.ToString(), behandler.ErPartnerAdministrator.ToString(), cancellationToken);
        }

        return RedirectToPage(new { id });
    }

    /// <summary>
    /// Legg til/inviter behandler via e-post i stedet for en nedtrekksliste (jf.
    /// docs/beslutningslogg.md). Treffer e-posten en eksisterende UAVHENGIG
    /// behandler, kobles den IKKE stille — siden viser i stedet en eksplisitt
    /// bekreftelsesprompt (MatchFunnet), og selve koblingen skjer først når
    /// Superadmin trykker "Bekreft" (som poster til den eksisterende
    /// OnPostLeggTilBehandlerAsync-handleren over).
    /// </summary>
    public async Task<IActionResult> OnPostInviterEllerKobleAsync(long id, string epost, CancellationToken cancellationToken)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is null)
        {
            return NotFound();
        }

        epost = epost.Trim();
        PartnerId = id;
        PartnerNavn = partner.Navn;
        Navn = partner.Navn;
        KontaktpersonNavn = partner.KontaktpersonNavn;
        KontaktEpost = partner.KontaktEpost;
        KontaktMobilNr = partner.KontaktMobilNr;
        HarAktivtAbonnement = partner.HarAktivtAbonnement;

        if (string.IsNullOrWhiteSpace(epost))
        {
            Feilmelding = "Skriv inn en e-postadresse.";
            await LastBehandlerlisterAsync(id, cancellationToken);
            return Page();
        }

        if (await _db.Administratorer.AnyAsync(a => a.Email == epost, cancellationToken))
        {
            Feilmelding = $"{epost} tilhører en administrator-konto og kan ikke gjøres om til behandler her.";
            await LastBehandlerlisterAsync(id, cancellationToken);
            return Page();
        }

        var eksisterende = await _db.Behandlere.FirstOrDefaultAsync(b => b.Email == epost, cancellationToken);
        if (eksisterende is not null)
        {
            if (eksisterende.PartnerId == id)
            {
                Feilmelding = $"{epost} er allerede tilknyttet denne partneren.";
            }
            else if (eksisterende.PartnerId is not null)
            {
                Feilmelding = $"{epost} tilhører allerede en annen partner — fjern koblingen der først.";
            }
            else
            {
                MatchFunnet = eksisterende;
            }

            await LastBehandlerlisterAsync(id, cancellationToken);
            return Page();
        }

        var resultat = await _invitasjonService.InviterAsync(
            mobilNr: null, epost: epost, administratorId: HentAdministratorId(), behandlerId: null,
            baseUrl: $"{Request.Scheme}://{Request.Host}", partnerId: id, cancellationToken: cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "InviterBehandlerTilPartner",
            nameof(Behandler), resultat.Behandler.Id.ToString(), $"PartnerId {id}", cancellationToken);

        Infomelding = $"Invitasjon sendt til {epost}.";
        await LastBehandlerlisterAsync(id, cancellationToken);
        return Page();
    }

    /// <summary>
    /// Bulk-invitasjon: én e-post per linje (eller komma-separert). Uavhengige
    /// behandlere som allerede finnes med en av e-postene kobles rett til
    /// partneren (ingen enkeltvis bekreftelse i bulk-flyten — Superadmin har
    /// allerede skrevet inn listen med vilje); e-poster som tilhører en
    /// administrator eller en annen partner hoppes over og rapporteres i
    /// oppsummeringen.
    /// </summary>
    public async Task<IActionResult> OnPostBulkInviterAsync(long id, string epostListe, CancellationToken cancellationToken)
    {
        var partner = await _db.Partnere.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partner is null)
        {
            return NotFound();
        }

        PartnerId = id;
        PartnerNavn = partner.Navn;
        Navn = partner.Navn;
        KontaktpersonNavn = partner.KontaktpersonNavn;
        KontaktEpost = partner.KontaktEpost;
        KontaktMobilNr = partner.KontaktMobilNr;
        HarAktivtAbonnement = partner.HarAktivtAbonnement;

        var eposter = epostListe
            .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var koblet = 0;
        var invitert = 0;
        var hoppetOver = new List<string>();

        foreach (var epost in eposter)
        {
            if (await _db.Administratorer.AnyAsync(a => a.Email == epost, cancellationToken))
            {
                hoppetOver.Add($"{epost} (administrator-konto)");
                continue;
            }

            var eksisterende = await _db.Behandlere.FirstOrDefaultAsync(b => b.Email == epost, cancellationToken);
            if (eksisterende is not null)
            {
                if (eksisterende.PartnerId == id)
                {
                    hoppetOver.Add($"{epost} (allerede tilknyttet)");
                }
                else if (eksisterende.PartnerId is not null)
                {
                    hoppetOver.Add($"{epost} (tilhører en annen partner)");
                }
                else
                {
                    eksisterende.PartnerId = id;
                    koblet++;
                }

                continue;
            }

            await _invitasjonService.InviterAsync(
                mobilNr: null, epost: epost, administratorId: HentAdministratorId(), behandlerId: null,
                baseUrl: $"{Request.Scheme}://{Request.Host}", partnerId: id, cancellationToken: cancellationToken);
            invitert++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "BulkInviterBehandlereTilPartner",
            nameof(Partner), id.ToString(), $"Koblet {koblet}, invitert {invitert}, hoppet over {hoppetOver.Count}", cancellationToken);

        Infomelding = $"{koblet} eksisterende behandler(e) koblet direkte, {invitert} invitert på e-post."
            + (hoppetOver.Count > 0 ? $" Hoppet over: {string.Join(", ", hoppetOver)}." : string.Empty);

        await LastBehandlerlisterAsync(id, cancellationToken);
        return Page();
    }

    private long HentAdministratorId() =>
        long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var aid) ? aid : 0;

    private async Task LastBehandlerlisterAsync(long partnerId, CancellationToken cancellationToken)
    {
        TilknyttedeBehandlere = await _db.Behandlere.Where(b => b.PartnerId == partnerId).OrderBy(b => b.Etternavn).ToListAsync(cancellationToken);
    }
}
