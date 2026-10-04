using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Tester;

namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Én ulest/lest melding, beriket med det som trengs for å vise den i en liste uten ekstra oppslag.
/// Nøyaktig ÉN av (TestTildelingId+TestNavn+PasientId) ELLER Fritekst er satt — se
/// BehandlerMelding sin XML-doc for hvorfor (generalisert 2026-10-04 for program-hendelser).
/// </summary>
public sealed record MeldingMedDetaljer(
    BehandlerMelding Melding, long? TestTildelingId, string? TestNavn, long? PasientId, string? PasientNavn, string? Fritekst);

/// <summary>
/// Behandlers "innboks" av meldinger om at en pasient har fullført en test —
/// se BehandlerMelding. Enkelt lest/ulest-system, ingen sletting/arkivering
/// ennå (bevisst utsatt, jf. beslutningsloggen).
/// </summary>
public sealed class BehandlerMeldingService
{
    private readonly AppDbContext _db;

    public BehandlerMeldingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task OpprettAsync(long behandlerId, long testTildelingId, CancellationToken cancellationToken = default)
    {
        _db.BehandlerMeldinger.Add(new BehandlerMelding
        {
            BehandlerId = behandlerId,
            TestTildelingId = testTildelingId,
            OpprettetUtc = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Hjemmeoppgaver/programmer (2026-10-04): en hendelse UTEN tilknyttet tildeling — f.eks. "xx meldte seg ut av programmet kl. [tid]". Se BehandlerMelding.Fritekst.</summary>
    public async Task OpprettFritekstAsync(long behandlerId, string fritekst, CancellationToken cancellationToken = default)
    {
        _db.BehandlerMeldinger.Add(new BehandlerMelding
        {
            BehandlerId = behandlerId,
            Fritekst = fritekst,
            OpprettetUtc = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> TellUlesteAsync(long behandlerId, CancellationToken cancellationToken = default) =>
        _db.BehandlerMeldinger.CountAsync(m => m.BehandlerId == behandlerId && m.LestUtc == null, cancellationToken);

    public async Task<IReadOnlyList<MeldingMedDetaljer>> HentUlesteAsync(long behandlerId, CancellationToken cancellationToken = default)
    {
        var meldinger = await _db.BehandlerMeldinger
            .Where(m => m.BehandlerId == behandlerId && m.LestUtc == null)
            .OrderByDescending(m => m.OpprettetUtc)
            .ToListAsync(cancellationToken);

        return await BerikAsync(meldinger, cancellationToken);
    }

    /// <summary>Markerer alle uleste meldinger for én tildeling som lest — kalt når behandler åpner rapporten (se Rapport.cshtml.cs).</summary>
    public async Task MarkerLestForTildelingAsync(long behandlerId, long testTildelingId, CancellationToken cancellationToken = default)
    {
        var meldinger = await _db.BehandlerMeldinger
            .Where(m => m.BehandlerId == behandlerId && m.TestTildelingId == testTildelingId && m.LestUtc == null)
            .ToListAsync(cancellationToken);

        if (meldinger.Count == 0)
        {
            return;
        }

        var na = DateTimeOffset.UtcNow;
        foreach (var melding in meldinger)
        {
            melding.LestUtc = na;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<MeldingMedDetaljer>> BerikAsync(List<BehandlerMelding> meldinger, CancellationToken cancellationToken)
    {
        // Hjemmeoppgaver/programmer (2026-10-04): TestTildelingId er nå NULLABLE (se
        // BehandlerMelding sin XML-doc) — en fritekst-melding har ingen tildeling å berike med,
        // og tas med direkte i resultatet uten oppslag.
        var tildelingIder = meldinger.Where(m => m.TestTildelingId is not null).Select(m => m.TestTildelingId!.Value).ToList();
        var tildelinger = await _db.TestTildelinger.Where(t => tildelingIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);

        var testIder = tildelinger.Values.Select(t => t.TestId).Distinct().ToList();
        var testNavn = await _db.Tester.Where(t => testIder.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Navn, cancellationToken);

        var pasientIder = tildelinger.Values.Select(t => t.PasientId).Distinct().ToList();
        var pasientNavn = await _db.Pasienter.Where(p => pasientIder.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Navn, cancellationToken);

        var resultat = new List<MeldingMedDetaljer>();
        foreach (var m in meldinger)
        {
            if (m.TestTildelingId is null)
            {
                resultat.Add(new MeldingMedDetaljer(m, null, null, null, null, m.Fritekst));
                continue;
            }

            if (!tildelinger.TryGetValue(m.TestTildelingId.Value, out var tildeling))
            {
                continue;
            }

            resultat.Add(new MeldingMedDetaljer(
                m, tildeling.Id, testNavn.GetValueOrDefault(tildeling.TestId, "(ukjent test)"),
                tildeling.PasientId, pasientNavn.GetValueOrDefault(tildeling.PasientId), null));
        }
        return resultat;
    }
}
