namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Samler HPR-prøveperiodens lengde og forlengelsesregel ett sted — tidligere
/// hardkodet "7 dager" tre uavhengige steder (Ny.cshtml.cs, Gruppeimport.cshtml.cs,
/// Admin/Behandlere/Index.cshtml), se docs/beslutningslogg.md "Bugliste
/// 2026-09-13".
/// </summary>
public static class HprPolicy
{
    public const int ProveperiodeDager = 21;
    public const int ForlengelseDager = 21;

    public static readonly TimeSpan Proveperiode = TimeSpan.FromDays(ProveperiodeDager);

    /// <summary>Faktisk frist for en behandler — forlengelsen (satt kun én gang) vinner over standardfristen hvis den er senere.</summary>
    public static DateTimeOffset? BeregnFrist(Behandler behandler)
    {
        if (behandler.RegistrertUtc is null)
        {
            return null;
        }

        var standardfrist = behandler.RegistrertUtc.Value.Add(Proveperiode);
        return behandler.HprForlengetTilUtc is { } forlenget && forlenget > standardfrist
            ? forlenget
            : standardfrist;
    }

    public static bool ErUtlopt(Behandler behandler, DateTimeOffset naa) =>
        !behandler.HprGodkjent && BeregnFrist(behandler) is { } frist && naa > frist;
}
