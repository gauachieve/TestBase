using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Admin.Pages.Administratorer;

public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;
    private readonly ICaptchaProvider _captcha;

    public IndexModel(AppDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUser, ICaptchaProvider captcha)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _captcha = captcha;
    }

    public List<Administrator> Administratorer { get; private set; } = new();

    /// <summary>Kun Superadmin kan se slettede rader — se docs/beslutningslogg.md "Bugliste 2026-09-13".</summary>
    public bool ErSuperadmin => _currentUser.Role == UserRole.Superadmin;
    public bool VisSlettede { get; private set; }

    public string CaptchaSporsmal { get; private set; } = string.Empty;

    [BindProperty]
    public string CaptchaSignertFasit { get; set; } = string.Empty;

    [BindProperty]
    public string? CaptchaSvar { get; set; }

    public string? Feilmelding { get; private set; }

    public async Task OnGetAsync(bool visSlettede, CancellationToken cancellationToken)
    {
        VisSlettede = visSlettede && ErSuperadmin;

        var sporring = _db.Administratorer.AsQueryable();
        if (!VisSlettede)
        {
            sporring = sporring.Where(a => !a.ErSlettet);
        }

        Administratorer = await sporring.OrderBy(a => a.AdminId).ToListAsync(cancellationToken);

        var utfordring = _captcha.LagUtfordring();
        CaptchaSporsmal = utfordring.SporsmalTekst;
        CaptchaSignertFasit = utfordring.SignertFasit;
    }

    public async Task<IActionResult> OnPostArkiverAsync(long id, CancellationToken cancellationToken)
    {
        var administrator = await _db.Administratorer.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (administrator is not null)
        {
            // Se docs/beslutningslogg.md "Produksjonsutfall: krasj ved oppstart pga.
            // arkivert seed-admin-konto" — en arkivert Superadmin-konto krasjet HELE
            // appen ved neste omstart. Blokker dette ved kilden i stedet for å stole
            // på selvhelbredingen i Program.cs alene.
            if (administrator.ErSuperadmin)
            {
                await OnGetAsync(false, cancellationToken);
                Feilmelding = "Superadmin-kontoen kan ikke arkiveres.";
                return Page();
            }

            administrator.ErArkivert = !administrator.ErArkivert;
            administrator.ArkivertUtc = administrator.ErArkivert ? DateTimeOffset.UtcNow : null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId,
                _currentUser.Role.ToString(),
                administrator.ErArkivert ? "ArkiverAdministrator" : "GjenopprettAdministrator",
                nameof(Administrator),
                administrator.Id.ToString(),
                cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    /// <summary>Kun mulig når raden allerede er arkivert — håndhevet server-side, ikke bare ved at knappen er grået ut i UI.</summary>
    public async Task<IActionResult> OnPostSlettAsync(long id, CancellationToken cancellationToken)
    {
        if (!_captcha.Verifiser(CaptchaSignertFasit, CaptchaSvar))
        {
            // ModelState.Clear() er nødvendig FØR ny OnGetAsync-visning her: asp-for
            // gjengir ellers den POSTEDE (utdaterte) verdien fra ModelState i stedet
            // for den ferske modell-egenskapen — CaptchaSporsmal ville da vist et NYTT
            // spørsmål mens det skjulte CaptchaSignertFasit-feltet fortsatt viste det
            // GAMLE, signerte svaret, slik at selv et korrekt svar alltid feilet.
            ModelState.Clear();
            await OnGetAsync(false, cancellationToken);
            Feilmelding = "Feil svar på sikkerhetsspørsmålet — sletting avbrutt.";
            return Page();
        }

        var administrator = await _db.Administratorer.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (administrator is not null && administrator.ErArkivert && !administrator.ErSuperadmin)
        {
            administrator.ErSlettet = true;
            administrator.SlettetUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "SlettAdministrator",
                nameof(Administrator), administrator.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage();
    }

    /// <summary>Kun Superadmin — gjenoppretter BÅDE slettet- og arkivert-status til aktiv.</summary>
    public async Task<IActionResult> OnPostGjenopprettFraSlettetAsync(long id, CancellationToken cancellationToken)
    {
        if (!ErSuperadmin)
        {
            return Forbid();
        }

        var administrator = await _db.Administratorer.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (administrator is not null)
        {
            administrator.ErSlettet = false;
            administrator.SlettetUtc = null;
            administrator.ErArkivert = false;
            administrator.ArkivertUtc = null;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                _currentUser.UserId, _currentUser.Role.ToString(), "GjenopprettSlettetAdministrator",
                nameof(Administrator), administrator.Id.ToString(), cancellationToken: cancellationToken);
        }

        return RedirectToPage(new { visSlettede = true });
    }
}
