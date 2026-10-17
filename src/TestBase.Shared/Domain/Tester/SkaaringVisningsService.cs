using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Avgjør om beregnet skåre/tolkning skal vises i en rapport — MDR-gating (2026-10-17, se
/// docs/mdr-klassifiseringsvurdering-iib-begrunnelse.pdf og docs/beslutningslogg.md
/// "MDR-klassifisering"). Brukt av alle tre Rapport-sidene (Admin/Behandlerportal/
/// Pasientportal) FØR TestService.BeregnSkaaringAsync kalles — selve beregningen i
/// TestService er UENDRET og fortsatt tilgjengelig internt (f.eks. for grupperapporter),
/// det er kun VISNING av skåren til en behandler/pasient i en rapport dette gater.
/// </summary>
public sealed class SkaaringVisningsService
{
    private readonly DriftsbryterInnstillingService _driftsbrytere;
    private readonly AppDbContext _db;

    public SkaaringVisningsService(DriftsbryterInnstillingService driftsbrytere, AppDbContext db)
    {
        _driftsbrytere = driftsbrytere;
        _db = db;
    }

    /// <summary>
    /// true når skåring trygt kan vises: pasienten er en "prøvepasient" uten personnummer
    /// (se GruppeService.TellProvedataAsync — ikke en ekte, identifiserbar person, uansett
    /// miljø inkl. live), ELLER Superadmin/Utvikler har eksplisitt slått
    /// DriftsbryterInnstilling.AutomatiskSkaaringErAktiv PÅ for dette miljøet (se
    /// Admin/MinSide — tiltenkt KUN for dev/beta, der bare syntetiske testdata finnes).
    /// </summary>
    public async Task<bool> SkalViseSkaaringAsync(long pasientId, CancellationToken cancellationToken = default)
    {
        var personnummer = await _db.Pasienter
            .Where(p => p.Id == pasientId)
            .Select(p => p.Personnummer)
            .FirstOrDefaultAsync(cancellationToken);

        if (personnummer is null)
        {
            return true;
        }

        var driftsbryter = await _driftsbrytere.HentAsync(cancellationToken);
        return driftsbryter.AutomatiskSkaaringErAktiv;
    }
}
