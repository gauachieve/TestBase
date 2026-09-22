using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Security;
using TestBase.Web.Security;

namespace TestBase.Web.Areas.Admin.Pages.Grupper;

/// <summary>
/// Admin sin redigering — samme funksjonalitet som Behandlerportal/Grupper/
/// Rediger (navn, tester, QR-kode, arkivering), men UTEN eierskapssjekk mot
/// innlogget bruker, siden admin skal kunne redigere ENHVER behandlers
/// gruppe (se prosjektbeskrivelsen: admin gjør det samme som behandler kan).
/// </summary>
public sealed class RedigerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly GruppeService _grupper;
    private readonly TestService _testService;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;

    public RedigerModel(AppDbContext db, GruppeService grupper, TestService testService, IAuditLogger auditLogger, ICurrentUserContext currentUser)
    {
        _db = db;
        _grupper = grupper;
        _testService = testService;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    [BindProperty]
    public long Id { get; set; }

    [BindProperty]
    public string Navn { get; set; } = string.Empty;

    [BindProperty]
    public DateOnly? StartDato { get; set; }

    [BindProperty]
    public DateOnly? SluttDato { get; set; }

    [BindProperty]
    public List<long> TestIder { get; set; } = new();

    public string? BehandlerNavn { get; private set; }
    public IReadOnlyList<TestService.KategoriMedTester> KategoriTre { get; private set; } = Array.Empty<TestService.KategoriMedTester>();
    public IReadOnlyDictionary<long, int> EstimertMinutterPerTestId { get; private set; } = new Dictionary<long, int>();
    public string? Feilmelding { get; private set; }
    public string? Melding { get; private set; }
    public string? InvitasjonsLenke { get; private set; }
    public int AntallProvedata { get; private set; }
    public DateTimeOffset Opprettet { get; private set; }

    /// <summary>Tester tilordnet DENNE gruppen — til radioknapp-valget i "Generer gruppe­rapport"-popupen, se Rediger.cshtml.</summary>
    public IReadOnlyList<Test> TilordnedeTester { get; private set; } = Array.Empty<Test>();
    public IReadOnlyDictionary<long, (DateOnly? Provedata, DateOnly? Ekte)> TidligsteDatoPerTest { get; private set; } = new Dictionary<long, (DateOnly?, DateOnly?)>();

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var innhold = await _grupper.HentMedTesterAsync(id, cancellationToken);
        if (innhold is null)
        {
            return RedirectToPage("Index");
        }

        Id = innhold.Gruppe.Id;
        Navn = innhold.Gruppe.Navn;
        StartDato = innhold.Gruppe.StartDato;
        SluttDato = innhold.Gruppe.SluttDato;
        Opprettet = innhold.Gruppe.OpprettetUtc;
        TestIder = innhold.Tester.Select(t => t.Id).ToList();
        BehandlerNavn = (await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == innhold.Gruppe.BehandlerId, cancellationToken))?.Visningsnavn;
        await LastKategoriTreAsync(cancellationToken);
        InvitasjonsLenke = $"{Request.Scheme}://{Request.Host}/BliPasient/g/{innhold.Gruppe.QrToken}";
        AntallProvedata = await _grupper.TellProvedataAsync(id, cancellationToken);
        await LastRapportgrunnlagAsync(innhold, cancellationToken);
        return Page();
    }

    /// <summary>Sletter permanent alle "prøv systemet"-pasienter (intet personnummer) i gruppen — se GruppeService.SlettProvedataAsync.</summary>
    public async Task<IActionResult> OnPostSlettProvedataAsync(long id, CancellationToken cancellationToken)
    {
        var innhold = await _grupper.HentMedTesterAsync(id, cancellationToken);
        if (innhold is null)
        {
            return RedirectToPage("Index");
        }

        var antallSlettet = await _grupper.SlettProvedataAsync(id, cancellationToken);
        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "SlettGruppeProvedata",
            nameof(Gruppe), id.ToString(), $"Antall pasienter slettet: {antallSlettet}", cancellationToken);

        Id = innhold.Gruppe.Id;
        Navn = innhold.Gruppe.Navn;
        StartDato = innhold.Gruppe.StartDato;
        SluttDato = innhold.Gruppe.SluttDato;
        Opprettet = innhold.Gruppe.OpprettetUtc;
        TestIder = innhold.Tester.Select(t => t.Id).ToList();
        BehandlerNavn = (await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == innhold.Gruppe.BehandlerId, cancellationToken))?.Visningsnavn;
        await LastKategoriTreAsync(cancellationToken);
        InvitasjonsLenke = $"{Request.Scheme}://{Request.Host}/BliPasient/g/{innhold.Gruppe.QrToken}";
        AntallProvedata = 0;
        Melding = $"{antallSlettet} prøvepasient(er) slettet.";
        await LastRapportgrunnlagAsync(innhold, cancellationToken);
        return Page();
    }

    /// <summary>Invaliderer gjeldende QR-kode/lenke umiddelbart — se Gruppe.QrToken.</summary>
    public async Task<IActionResult> OnPostRegenererQrAsync(long id, CancellationToken cancellationToken)
    {
        var nyttToken = await _grupper.RegenererQrTokenAsync(id, cancellationToken);
        if (nyttToken is null)
        {
            return RedirectToPage("Index");
        }

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "RegenererGruppeQr",
            nameof(Gruppe), id.ToString(), cancellationToken: cancellationToken);
        return RedirectToPage(new { id });
    }

    /// <summary>Selve QR-bildet — se QrBildeGenerator.</summary>
    public async Task<IActionResult> OnGetQrAsync(long id, CancellationToken cancellationToken)
    {
        var innhold = await _grupper.HentMedTesterAsync(id, cancellationToken);
        if (innhold is null)
        {
            return NotFound();
        }

        var lenke = $"{Request.Scheme}://{Request.Host}/BliPasient/g/{innhold.Gruppe.QrToken}";
        return File(QrBildeGenerator.GenererPng(lenke), "image/png");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var innhold = await _grupper.HentMedTesterAsync(Id, cancellationToken);
        if (innhold is null)
        {
            return RedirectToPage("Index");
        }

        if (string.IsNullOrWhiteSpace(Navn))
        {
            Feilmelding = "Gruppenavn er obligatorisk.";
            Opprettet = innhold.Gruppe.OpprettetUtc;
            BehandlerNavn = (await _db.Behandlere.FirstOrDefaultAsync(b => b.Id == innhold.Gruppe.BehandlerId, cancellationToken))?.Visningsnavn;
            await LastKategoriTreAsync(cancellationToken);
            InvitasjonsLenke = $"{Request.Scheme}://{Request.Host}/BliPasient/g/{innhold.Gruppe.QrToken}";
            AntallProvedata = await _grupper.TellProvedataAsync(Id, cancellationToken);
            await LastRapportgrunnlagAsync(innhold, cancellationToken);
            return Page();
        }

        await _grupper.OppdaterAsync(Id, Navn, TestIder, StartDato, SluttDato, cancellationToken);

        await _auditLogger.LogAsync(
            _currentUser.UserId, _currentUser.Role.ToString(), "OppdaterGruppe",
            nameof(Gruppe), Id.ToString(), cancellationToken: cancellationToken);

        return RedirectToPage("Index");
    }

    private async Task LastKategoriTreAsync(CancellationToken cancellationToken)
    {
        KategoriTre = await _testService.HentKategoriTreAsync(cancellationToken: cancellationToken);
        var testIder = KategoriTre.SelectMany(k => k.Tester).Select(t => t.Id).Distinct().ToList();
        var antallLedd = await _testService.HentAntallLeddPerTestAsync(testIder, cancellationToken);
        EstimertMinutterPerTestId = antallLedd.ToDictionary(kv => kv.Key, kv => Math.Max(1, (int)Math.Ceiling(kv.Value * 15.0 / 60)));
    }

    /// <summary>Grunnlaget "Generer gruppe­rapport"-popupen trenger — se Rediger.cshtml og GruppeService.HentTidligsteTildeltDatoPerTestAsync.</summary>
    private async Task LastRapportgrunnlagAsync(GruppeMedTester innhold, CancellationToken cancellationToken)
    {
        TilordnedeTester = innhold.Tester;
        TidligsteDatoPerTest = await _grupper.HentTidligsteTildeltDatoPerTestAsync(innhold.Gruppe.Id, cancellationToken);
    }
}
