using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;

namespace TestBase.Shared.Domain.Tester;

/// <summary>Leser/skriver den ENE AdminOkonomiOppsett-raden (Id=1, opprettes lat) — samme mønster som EktBankIdInnstillingService.</summary>
public sealed class AdminOkonomiOppsettService
{
    private readonly AppDbContext _db;

    public AdminOkonomiOppsettService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminOkonomiOppsett> HentAsync(CancellationToken cancellationToken = default)
    {
        var rad = await _db.AdminOkonomiOppsett.FirstOrDefaultAsync(cancellationToken);
        if (rad is not null)
        {
            return rad;
        }

        rad = new AdminOkonomiOppsett();
        _db.AdminOkonomiOppsett.Add(rad);
        await _db.SaveChangesAsync(cancellationToken);
        return rad;
    }

    public async Task LagreAsync(AdminOkonomiOppsett innkommende, long endretAvUserId, CancellationToken cancellationToken = default)
    {
        var rad = await HentAsync(cancellationToken);
        rad.Organisasjonsnummer = innkommende.Organisasjonsnummer;
        rad.HprNummerFirma = innkommende.HprNummerFirma;
        rad.SalgsinntektEtikett = innkommende.SalgsinntektEtikett;
        rad.SalgsinntektKonto = innkommende.SalgsinntektKonto;
        rad.BetalingsgebyrEtikett = innkommende.BetalingsgebyrEtikett;
        rad.BetalingsgebyrKonto = innkommende.BetalingsgebyrKonto;
        rad.BehandlerHonorarEtikett = innkommende.BehandlerHonorarEtikett;
        rad.BehandlerHonorarKonto = innkommende.BehandlerHonorarKonto;
        rad.PartnerprovisjonEtikett = innkommende.PartnerprovisjonEtikett;
        rad.PartnerprovisjonKonto = innkommende.PartnerprovisjonKonto;
        rad.UtbetalingKontonummer = innkommende.UtbetalingKontonummer;
        rad.SistEndretAvUserId = endretAvUserId;
        rad.SistEndretUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
