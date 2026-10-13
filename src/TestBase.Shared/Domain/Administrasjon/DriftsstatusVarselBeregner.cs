namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Ren diff-logikk for kritisk-varsling (brukerbeslutning 2026-10-13) — regner ut hvilke sjekker
/// som NYLIG ble røde (skal varsles) og hvilke som NYLIG ble friske igjen (skal gi et "friskt
/// igjen"-varsel), basert på settet av sjekknavn som sist ble varslet om. Skilt ut som en ren,
/// stateløs funksjon (muterer kun det innsendte settet) for å kunne testes uten bakgrunnsjobbens
/// egen singleton-tilstand/DI — se DriftsstatusVarslingBakgrunnstjeneste i TestBase.Web.
/// </summary>
public static class DriftsstatusVarselBeregner
{
    public static (IReadOnlyList<string> NyeRoede, IReadOnlyList<string> NyFriske) BeregnDiff(
        HashSet<string> tidligereVarslet, IReadOnlyList<DriftsstatusSjekk> sjekker)
    {
        var roedeNaa = sjekker.Where(s => s.Lys == DriftsstatusLys.Rod).Select(s => s.Navn).ToHashSet();

        var nyeRoede = roedeNaa.Where(navn => !tidligereVarslet.Contains(navn)).ToList();
        var nyFriske = tidligereVarslet.Where(navn => !roedeNaa.Contains(navn)).ToList();

        foreach (var navn in nyeRoede)
        {
            tidligereVarslet.Add(navn);
        }

        foreach (var navn in nyFriske)
        {
            tidligereVarslet.Remove(navn);
        }

        return (nyeRoede, nyFriske);
    }
}
