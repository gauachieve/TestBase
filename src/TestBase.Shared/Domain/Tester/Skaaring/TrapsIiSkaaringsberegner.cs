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
/// Ledd-layout (0-indeksert i alleLedd): 0-13 Del 1 traumeeksponering (JaNei,
/// LISTES i rapporten for hvert «Ja»-svar), 14 Del 1 sitt frittekst-tilleggsspørsmål
/// (listes også hvis besvart), 15-16 «Om hendelsen» (ikke skåret), 17-22
/// PTSD-symptomer (P1-P6, to og to i klyngene Re/Av/Th), 23-25 PTSD-funksjon
/// (P7-P9), 26-31 DSO-symptomer (C1-C6, to og to i klyngene Ad/Nsc/Dr), 32-34
/// DSO-funksjon (C7-C9). 2026-09-27: utvidet fra kun ja/nei-diagnosekonklusjon
/// til å liste hver klyngeskår (Re/Av/Th/Ad/Nsc/Dr) og hvert bekreftet
/// traumeeksponeringsspørsmål — brukerens tilbakemelding: "Det er for
/// forenklet nå", se docs/beslutningslogg.md.
/// </summary>
public sealed class TrapsIiSkaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    private const int AntallDel1JaNei = 14;
    private const int Del1OgOmAntall = 15 + 2; // 17
    private const int PtsdSymptomAntall = 6;
    private const int PtsdFunksjonAntall = 3;
    private const int DsoSymptomAntall = 6;
    private const int MaksPerCluster = PtsdSymptomAntall * 4; // 24
    private const int MaksPerDelklynge = 2 * 4; // 8 (to ledd per klynge, maks 4 hver)
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

        var reSkaar = ptsdSymptomLedd[0] + ptsdSymptomLedd[1];
        var avSkaar = ptsdSymptomLedd[2] + ptsdSymptomLedd[3];
        var thSkaar = ptsdSymptomLedd[4] + ptsdSymptomLedd[5];
        var reDx = ptsdSymptomLedd[0] >= TerskelTilstede || ptsdSymptomLedd[1] >= TerskelTilstede;
        var avDx = ptsdSymptomLedd[2] >= TerskelTilstede || ptsdSymptomLedd[3] >= TerskelTilstede;
        var thDx = ptsdSymptomLedd[4] >= TerskelTilstede || ptsdSymptomLedd[5] >= TerskelTilstede;
        var ptsdFi = ptsdFunksjonLedd.Any(v => v >= TerskelTilstede);
        var ptsdKriterier = reDx && avDx && thDx && ptsdFi;

        var adSkaar = dsoSymptomLedd[0] + dsoSymptomLedd[1];
        var nscSkaar = dsoSymptomLedd[2] + dsoSymptomLedd[3];
        var drSkaar = dsoSymptomLedd[4] + dsoSymptomLedd[5];
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

        // Bugliste punkt 12, 2026-10-04: disse lå TIDLIGERE som individuelle Indikator-badger
        // (opptil 15 stykker, ÉN per bekreftet Del 1-spørsmål) — rendret i SAMME rad som de 9
        // diagnostiske badgene øverst i rapporten, og dominerte/"så wonky ut" nettopp fordi en
        // pasient med mange bekreftede traumer kunne få et dusin+ lange, røde tekst-badger
        // blandet inn blant de egentlige diagnose-indikatorene. Flyttet til en ren punktliste i
        // selve Fortolkning-teksten i stedet (samme mønster som CORE-OM/EDE-Q/SCL-25 sine
        // delskala-punktlister, bugliste punkt 6) — Indikatorer inneholder nå KUN de 9 faste
        // diagnostiske badgene, uavhengig av hvor mange traumer som ble bekreftet.
        var del1JaLedd = alleLedd.Take(AntallDel1JaNei)
            .Where(ledd => svarPerLeddId.TryGetValue(ledd.Id, out var v) && v == "Ja")
            .ToList();
        string? annenHendelseTekst = AntallDel1JaNei < alleLedd.Count &&
            svarPerLeddId.TryGetValue(alleLedd[AntallDel1JaNei].Id, out var annenTekst) &&
            !string.IsNullOrWhiteSpace(annenTekst)
            ? annenTekst
            : null;

        var fortolkning = $"{diagnose} PTSD-skåre (gjenopplevelse/unngåelse/fare): {ptsdSkaar}/{MaksPerCluster}. " +
                           $"DSO-skåre (selvorganisering): {dsoSkaar}/{MaksPerCluster}. " +
                           $"Klynger — Gjenopplevelse (Re): {reSkaar}/{MaksPerDelklynge}, Unngåelse (Av): {avSkaar}/{MaksPerDelklynge}, " +
                           $"Nåværende trusselfølelse (Th): {thSkaar}/{MaksPerDelklynge}, Affektregulering (Ad): {adSkaar}/{MaksPerDelklynge}, " +
                           $"Negativt selvbilde (Nsc): {nscSkaar}/{MaksPerDelklynge}, Relasjonsvansker (Dr): {drSkaar}/{MaksPerDelklynge}. " +
                           "Hver klynge krever minst ett ledd besvart «Moderat» eller over for å telle som diagnostisk til stede " +
                           "(kriteriet nedenfor), i tillegg til funksjonstap. Klinisk vurdering skal alltid ha forrang ved uenighet " +
                           "med den automatiske skåren.";

        if (del1JaLedd.Count > 0 || annenHendelseTekst is not null)
        {
            fortolkning += "\n\nBekreftede traumeeksponeringer:\n" +
                string.Join("\n", del1JaLedd.Select(ledd => $"• {ledd.Sporsmalstekst}")) +
                (annenHendelseTekst is not null ? $"\n• Annet: {annenHendelseTekst}" : "");
        }

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("PTSD-kriterier oppfylt", ptsdKriterier ? "Ja" : "Nei", !ptsdKriterier),
            new("Forstyrrelser i selvorganisering (DSO) oppfylt", dsoKriterier ? "Ja" : "Nei", !dsoKriterier),
            new("Diagnostisk konklusjon", ptsdKriterier && dsoKriterier ? "KPTSD" : ptsdKriterier ? "PTSD" : "Ingen", !ptsdKriterier),
            new("Gjenopplevelse (Re)", $"Re (gjenopplevelse): {reSkaar}/{MaksPerDelklynge}" + (reDx ? " — til stede" : ""), !reDx),
            new("Unngåelse (Av)", $"Av (unngåelse): {avSkaar}/{MaksPerDelklynge}" + (avDx ? " — til stede" : ""), !avDx),
            new("Nåværende trusselfølelse (Th)", $"Th (trusselfølelse): {thSkaar}/{MaksPerDelklynge}" + (thDx ? " — til stede" : ""), !thDx),
            new("Affektregulering (Ad)", $"Ad (affektregulering): {adSkaar}/{MaksPerDelklynge}" + (adDx ? " — til stede" : ""), !adDx),
            new("Negativt selvbilde (Nsc)", $"Nsc (negativt selvbilde): {nscSkaar}/{MaksPerDelklynge}" + (nscDx ? " — til stede" : ""), !nscDx),
            new("Relasjonsvansker (Dr)", $"Dr (relasjonsvansker): {drSkaar}/{MaksPerDelklynge}" + (drDx ? " — til stede" : ""), !drDx)
        };

        return new TestSkaaring(raaSkaar, maks, prosentSkaar, fortolkning, indikatorer);
    }
}
