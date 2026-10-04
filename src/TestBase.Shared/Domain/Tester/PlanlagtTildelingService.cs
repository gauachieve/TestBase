using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;

namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Utsatt/gjentagende variant av TestTildelingsService.TildelOgVarsleAsync —
/// se PlanlagtTildeling. Selve utsendingstidspunktet tolkes i norsk lokaltid
/// ("kl. 10" skal bety 10 norsk tid, ikke UTC) via Europe/Oslo, som .NET 6+
/// slår opp via ICU på både Windows og Linux uten noe ekstra OS-avhengig
/// tidssone-navn.
/// </summary>
public sealed class PlanlagtTildelingService
{
    private static readonly TimeZoneInfo NorskTid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    private readonly AppDbContext _db;
    private readonly TestTildelingsService _tildelingsService;

    public PlanlagtTildelingService(AppDbContext db, TestTildelingsService tildelingsService)
    {
        _db = db;
        _tildelingsService = tildelingsService;
    }

    /// <summary>"I morgen i arbeidstiden" — i dag valgt som kl. 09:00 norsk tid neste dag, se docs/beslutningslogg.md.</summary>
    public static DateTimeOffset BeregnImorgenArbeidstidUtc(DateTimeOffset naaUtc)
    {
        var naaLokal = TimeZoneInfo.ConvertTime(naaUtc, NorskTid);
        var imorgen = naaLokal.Date.AddDays(1).Add(new TimeSpan(9, 0, 0));
        return new DateTimeOffset(imorgen, NorskTid.GetUtcOffset(imorgen));
    }

    /// <summary>Neste forekomst av en gitt ukedag+klokkeslett i norsk tid, STRENGT etter <paramref name="etterUtc"/>.</summary>
    public static DateTimeOffset BeregnNesteForekomstUtc(DateTimeOffset etterUtc, DayOfWeek ukedag, TimeSpan klokkeslett)
    {
        var etterLokal = TimeZoneInfo.ConvertTime(etterUtc, NorskTid);
        var dato = etterLokal.Date;
        for (var i = 0; i < 8; i++)
        {
            var kandidatDato = dato.AddDays(i);
            if (kandidatDato.DayOfWeek != ukedag)
            {
                continue;
            }

            var kandidatLokal = kandidatDato.Add(klokkeslett);
            var kandidatUtc = new DateTimeOffset(kandidatLokal, NorskTid.GetUtcOffset(kandidatLokal));
            if (kandidatUtc > etterUtc)
            {
                return kandidatUtc;
            }
        }

        throw new InvalidOperationException("Fant ikke neste forekomst innen 8 dager — bør være umulig.");
    }

    public async Task OpprettAsync(
        IReadOnlyList<long> pasientIder, IReadOnlyList<long> testIder,
        long? behandlerId, long? administratorId, IReadOnlyDictionary<long, decimal?> onsketHonorarKrPerTestId,
        Varslingspreferanse varslingsmetode, DateTimeOffset planlagtUtc,
        DayOfWeek? gjentaUkedag, TimeSpan? gjentaKlokkeslett, int? gjentaAntall,
        long opprettetAvUserId, CancellationToken cancellationToken = default)
    {
        _db.PlanlagteTildelinger.Add(new PlanlagtTildeling
        {
            PasientIderCsv = string.Join(',', pasientIder),
            TestIderCsv = string.Join(',', testIder),
            HonorarKrJson = onsketHonorarKrPerTestId.Count > 0 ? JsonSerializer.Serialize(onsketHonorarKrPerTestId) : null,
            Varslingsmetode = varslingsmetode,
            BehandlerId = behandlerId,
            AdministratorId = administratorId,
            PlanlagtUtc = planlagtUtc,
            GjentaUkedag = gjentaUkedag,
            GjentaKlokkeslett = gjentaKlokkeslett,
            GjentaAntallGjenstaende = gjentaAntall,
            OpprettetAvUserId = opprettetAvUserId,
            OpprettetUtc = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<List<PlanlagtTildeling>> HentDueAsync(DateTimeOffset naaUtc, CancellationToken cancellationToken = default) =>
        _db.PlanlagteTildelinger
            .Where(p => p.Status == PlanlagtTildelingStatus.Venter && p.PlanlagtUtc <= naaUtc)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Utfører én planlagt utsending (gjenbruker TildelOgVarsleAsync 100%), og
    /// enten avslutter raden eller reskjedulerer den til neste forekomst.
    /// Feil under selve utsendingen fanges OG lagres på raden i stedet for å
    /// forsvinne — men raden reskjeduleres/avsluttes uansett, en varig feilet
    /// utsending skal ikke blokkere resten av en gjentagende serie for alltid.
    /// </summary>
    public async Task UtforAsync(PlanlagtTildeling rad, string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var pasientIder = ParseIder(rad.PasientIderCsv);
            var testIder = ParseIder(rad.TestIderCsv);
            var honorar = string.IsNullOrWhiteSpace(rad.HonorarKrJson)
                ? new Dictionary<long, decimal?>()
                : JsonSerializer.Deserialize<Dictionary<long, decimal?>>(rad.HonorarKrJson) ?? new Dictionary<long, decimal?>();

            await _tildelingsService.TildelOgVarsleAsync(
                pasientIder, testIder, rad.BehandlerId, rad.AdministratorId, honorar,
                baseUrl, rad.Varslingsmetode, cancellationToken: cancellationToken);
            rad.Feilmelding = null;
        }
        catch (Exception ex)
        {
            rad.Feilmelding = ex.Message;
        }

        if (rad.GjentaUkedag is not null && rad.GjentaKlokkeslett is not null && rad.GjentaAntallGjenstaende is > 1)
        {
            rad.GjentaAntallGjenstaende--;
            rad.PlanlagtUtc = BeregnNesteForekomstUtc(rad.PlanlagtUtc, rad.GjentaUkedag.Value, rad.GjentaKlokkeslett.Value);
            // Status forblir Venter — samme rad brukes til neste forekomst.
        }
        else
        {
            rad.Status = rad.Feilmelding is null ? PlanlagtTildelingStatus.Sendt : PlanlagtTildelingStatus.Feilet;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static List<long> ParseIder(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(long.Parse).ToList();
}
