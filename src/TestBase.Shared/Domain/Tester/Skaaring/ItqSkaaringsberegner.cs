namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// ITQ-skåring: diagnostisk algoritme (IKKE en enkel terskelsum) hentet
/// ordrett fra den offisielle norske oversettelsens skåringsveiledning — se
/// ItqTestSeeder for kildehenvisning. Ledd-rekkefølge er fast (se
/// ItqTestSeeder.SeedAsync): 0=hendelsesbeskrivelse (fritekst, ikke skåret),
/// 1=hendelsestidspunkt (ikke skåret), 2-7=P1-P6, 8-10=P7-P9, 11-16=C1-C6,
/// 17-19=C7-C9. Terskel for "tilstede" er skåre ≥2 på minst ett ledd i hvert
/// symptompar/-trippel, jf. skåringsveiledningen.
/// </summary>
public sealed class ItqSkaaringsberegner : ITestSkaaringsberegner
{
    private const int MaksPerCluster = 24; // 6 ledd × 4 poeng
    private const int TerskelTilstede = 2;

    public string TestKode => "itq";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        int Poeng(int index) => int.Parse(svar[index].SvarVerdi);

        var p1 = Poeng(2); var p2 = Poeng(3); var p3 = Poeng(4);
        var p4 = Poeng(5); var p5 = Poeng(6); var p6 = Poeng(7);
        var p7 = Poeng(8); var p8 = Poeng(9); var p9 = Poeng(10);

        var c1 = Poeng(11); var c2 = Poeng(12); var c3 = Poeng(13);
        var c4 = Poeng(14); var c5 = Poeng(15); var c6 = Poeng(16);
        var c7 = Poeng(17); var c8 = Poeng(18); var c9 = Poeng(19);

        var reDx = p1 >= TerskelTilstede || p2 >= TerskelTilstede;
        var avDx = p3 >= TerskelTilstede || p4 >= TerskelTilstede;
        var thDx = p5 >= TerskelTilstede || p6 >= TerskelTilstede;
        var ptsdFi = p7 >= TerskelTilstede || p8 >= TerskelTilstede || p9 >= TerskelTilstede;
        var ptsdKriterier = reDx && avDx && thDx && ptsdFi;

        var adDx = c1 >= TerskelTilstede || c2 >= TerskelTilstede;
        var nscDx = c3 >= TerskelTilstede || c4 >= TerskelTilstede;
        var drDx = c5 >= TerskelTilstede || c6 >= TerskelTilstede;
        var dsoFi = c7 >= TerskelTilstede || c8 >= TerskelTilstede || c9 >= TerskelTilstede;
        var dsoKriterier = adDx && nscDx && drDx && dsoFi;

        var ptsdSkaar = p1 + p2 + p3 + p4 + p5 + p6;
        var dsoSkaar = c1 + c2 + c3 + c4 + c5 + c6;
        var raaSkaar = ptsdSkaar + dsoSkaar;
        var maks = MaksPerCluster * 2;
        var prosentSkaar = (int)Math.Round(raaSkaar * 100m / maks);

        string diagnose;
        if (ptsdKriterier && dsoKriterier)
        {
            diagnose = "Kriteriene for kompleks PTSD (KPTSD) er oppfylt.";
        }
        else if (ptsdKriterier)
        {
            diagnose = "Kriteriene for PTSD er oppfylt (uten kompleks PTSD).";
        }
        else
        {
            diagnose = "Kriteriene for verken PTSD eller kompleks PTSD er oppfylt ut fra denne besvarelsen.";
        }

        var fortolkning = $"{diagnose} PTSD-skåre (gjenopplevelse/unngåelse/fare): {ptsdSkaar}/{MaksPerCluster}. " +
                           $"DSO-skåre (selvorganisering): {dsoSkaar}/{MaksPerCluster}. " +
                           "Klinisk vurdering skal alltid ha forrang ved uenighet med den automatiske skåren.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("PTSD-kriterier oppfylt", ptsdKriterier ? "Ja" : "Nei", !ptsdKriterier),
            new("Forstyrrelser i selvorganisering (DSO) oppfylt", dsoKriterier ? "Ja" : "Nei", !dsoKriterier),
            new("Diagnostisk konklusjon", ptsdKriterier && dsoKriterier ? "KPTSD" : ptsdKriterier ? "PTSD" : "Ingen", !ptsdKriterier)
        };

        return new TestSkaaring(raaSkaar, maks, prosentSkaar, fortolkning, indikatorer);
    }
}
