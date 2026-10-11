namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// Visningstekst for <see cref="PasientStatus"/> — enumet selv (og alt internt som
/// Status/ArkivertUtc/audit-handlingsnavnene "ArkiverPasient" osv.) er BEVISST uendret
/// (bugliste 2026-10-10: "Archive on patients should be called 'Avslutt'"). Kun selve
/// den viste teksten for <see cref="PasientStatus.Arkivert"/> er endret til "Avsluttet"
/// for pasienter — samme mønster som testnavn-visningsendringer andre steder i appen
/// (f.eks. GADIT → "Dataspillavhengighet"): ingen kodenavn endres, kun det pasienten/
/// behandleren faktisk ser.
/// </summary>
public static class PasientStatusTekst
{
    public static string TilVisningstekst(this PasientStatus status) => status switch
    {
        PasientStatus.Arkivert => "Avsluttet",
        _ => status.ToString()
    };
}
