namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// TRAPS II-skåring: samme diagnostiske algoritme som ItqSkaaringsberegner
/// (se der for kilde/begrunnelse), men her via ekte TestLeddId-oppslag
/// (ITestSkaaringsberegnerMedLedd) i stedet for ItqSkaaringsberegner sin
/// rene listeposisjon (`svar[index]`) — SISTNEVNTE ER EN KJENT, IKKE FIKSET
/// SÅRBARHET (samme klasse som GADIT-krasjen, se docs/beslutningslogg.md):
/// et hoppet-over ledd i den frittstående ITQ-testen ville forskyve alle
/// påfølgende indekser og gi feil PTSD/DSO-klassifisering, mulig krasj på
/// int.Parse. Ikke rettet i ITQ selv denne runden (utenfor scope), kun
/// unngått her ved å bruke det robuste mønsteret fra første seeder-runde.
/// Ledd-layout (0-indeksert i alleLedd): 0-14 Del 1 (traumeeksponering, ikke
/// skåret her), 15-16 «Om hendelsen» (ikke skåret), 17-22 PTSD-symptomer
/// (P1-P6), 23-25 PTSD-funksjon (P7-P9), 26-31 DSO-symptomer (C1-C6), 32-34
/// DSO-funksjon (C7-C9).
/// </summary>
public sealed class TrapsIiSkaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int Del1OgOmAntall = 15 + 2; // 17
    private const int PtsdSymptomAntall = 6;
    private const int PtsdFunksjonAntall = 3;
    private const int DsoSymptomAntall = 6;
    private const int MaksPerCluster = PtsdSymptomAntall * 4; // 24
    private const int TerskelTilstede = 2;

    public string TestKode => "traps_ii";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("TRAPS II krever ledd-informasjon for PTSD/DSO-klassifisering — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        // Del 1 (traumeeksponering) sine ledd er JaNei/Fritekst, IKKE tall — kan derfor ikke
        // parses eagerly for hele svarlisten (ville kastet FormatException på "Ja"/"Nei").
        // Parses derfor kun ETT ledd av gangen, kun for ledd vi faktisk trenger (PTSD/DSO).
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);

        int VerdiForLedd(TestLedd ledd) =>
            svarPerLeddId.TryGetValue(ledd.Id, out var verdi) && int.TryParse(verdi, out var tall) ? tall : 0;

        var ptsdSymptomLedd = alleLedd.Skip(Del1OgOmAntall).Take(PtsdSymptomAntall).Select(VerdiForLedd).ToList();
        var ptsdFunksjonLedd = alleLedd.Skip(Del1OgOmAntall + PtsdSymptomAntall).Take(PtsdFunksjonAntall).Select(VerdiForLedd).ToList();
        var dsoSymptomLedd = alleLedd.Skip(Del1OgOmAntall + PtsdSymptomAntall + PtsdFunksjonAntall).Take(DsoSymptomAntall).Select(VerdiForLedd).ToList();
        var dsoFunksjonLedd = alleLedd.Skip(Del1OgOmAntall + PtsdSymptomAntall + PtsdFunksjonAntall + DsoSymptomAntall).Select(VerdiForLedd).ToList();

        var reDx = ptsdSymptomLedd[0] >= TerskelTilstede || ptsdSymptomLedd[1] >= TerskelTilstede;
        var avDx = ptsdSymptomLedd[2] >= TerskelTilstede || ptsdSymptomLedd[3] >= TerskelTilstede;
        var thDx = ptsdSymptomLedd[4] >= TerskelTilstede || ptsdSymptomLedd[5] >= TerskelTilstede;
        var ptsdFi = ptsdFunksjonLedd.Any(v => v >= TerskelTilstede);
        var ptsdKriterier = reDx && avDx && thDx && ptsdFi;

        var adDx = dsoSymptomLedd[0] >= TerskelTilstede || dsoSymptomLedd[1] >= TerskelTilstede;
        var nscDx = dsoSymptomLedd[2] >= TerskelTilstede || dsoSymptomLedd[3] >= TerskelTilstede;
        var drDx = dsoSymptomLedd[4] >= TerskelTilstede || dsoSymptomLedd[5] >= TerskelTilstede;
        var dsoFi = dsoFunksjonLedd.Any(v => v >= TerskelTilstede);
        var dsoKriterier = adDx && nscDx && drDx && dsoFi;

        var ptsdSkaar = ptsdSymptomLedd.Sum();
        var dsoSkaar = dsoSymptomLedd.Sum();
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
