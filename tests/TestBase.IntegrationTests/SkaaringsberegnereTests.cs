using TestBase.Shared.Domain.Tester;
using TestBase.Shared.Domain.Tester.Skaaring;

namespace TestBase.IntegrationTests;

/// <summary>
/// Rene enhetstester (ingen DB/HTTP) av skåringsklassene for de åtte
/// Helsebiblioteket-testene lagt til 2026-09 — se
/// docs/beslutningslogg.md. Fokuserer på de mest feilutsatte delene: reversert
/// skåring (RAADS-R, EQ40), delmengde-uttrekk (WURS, PHQ-9 sitt ekskluderte
/// funksjonsspørsmål, TRAPS I sin del 2). Speiler mønsteret fra
/// TestPrisberegnerTests.cs.
/// </summary>
public sealed class SkaaringsberegnereTests
{
    private static TestSvar Svar(string verdi) => new() { SvarVerdi = verdi };
    private static List<TestSvar> Svar(params string[] verdier) => verdier.Select(Svar).ToList();

    [Fact]
    public void Phq9_SummererKunDeForsteNiLeddIkkeFunksjonssporsmalet()
    {
        // 9 skårede ledd (sum 18) + ett 10. (funksjons-)ledd med verdi 3 som IKKE skal telle med.
        var svar = Svar("2", "2", "2", "2", "2", "2", "2", "2", "2", "3");
        var resultat = new Phq9Skaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal(18, resultat.RaaSkaar);
        Assert.Equal(27, resultat.RaaSkaarMaks);
    }

    [Fact]
    public void Ipds_TellerAntallJa()
    {
        var svar = Svar("Ja", "Nei", "Ja", "Ja", "Nei", "Nei", "Nei", "Nei", "Nei", "Nei", "Nei");
        var resultat = new IpdsSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal(3, resultat.RaaSkaar);
        Assert.Equal(11, resultat.RaaSkaarMaks);
    }

    [Fact]
    public void Ipds_Grupperer_JaBesvarteLedd_PerPersonlighetsklynge()
    {
        // Ledd 1 og 7 er begge "Emosjonelt ustabil PF" (se IpdsSkaaringsberegner.KlyngePerLedd) —
        // skal slås sammen til ÉN indikator med begge leddnumre, ikke to separate.
        var svar = Svar("Ja", "Nei", "Nei", "Nei", "Nei", "Nei", "Ja", "Nei", "Nei", "Nei", "Nei");
        var resultat = new IpdsSkaaringsberegner().BeregnSkaaring(svar);

        var klyngeIndikator = Assert.Single(resultat.Indikatorer!, i => i.Navn == "Mulig personlighetsklynge (uverifisert)");
        Assert.Equal("Indikerer: Emosjonelt ustabil PF (borderline type) – Ledd 1,7", klyngeIndikator.Verdi);
    }

    [Fact]
    public void MadrsS_SummererAlleNiLedd()
    {
        var svar = Svar("0", "2", "4", "6", "1", "3", "5", "0", "2");
        var resultat = new MadrsSSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal(23, resultat.RaaSkaar);
        Assert.Equal(54, resultat.RaaSkaarMaks);
    }

    [Fact]
    public void TrapsI_HopperOverDel1OgSummererKunDeTjueDel2Ledd()
    {
        var del1 = Enumerable.Repeat("Ja", 16).ToArray(); // 15 JaNei + 1 fritekst — skal ikke telle
        var del2 = Enumerable.Repeat("2", 20).ToArray(); // 20 x 2 = 40
        var resultat = new TrapsISkaaringsberegner().BeregnSkaaring(Svar(del1.Concat(del2).ToArray()));

        Assert.Equal(40, resultat.RaaSkaar);
        Assert.Equal(80, resultat.RaaSkaarMaks);
    }

    [Fact]
    public void RaadsR_ReverserteLeddSkaaresMotsattAvSymptomLedd()
    {
        // Ledd 0 er reversert (se RaadsRTestSeeder.ErReversertPerLedd[0] == true),
        // ledd 1 er ikke (== false). Skalaverdi 1 = "stemmer nå og da jeg var ung".
        var svar = Svar("1", "1").Concat(Enumerable.Repeat(Svar("4"), 78)).ToList();
        var resultat = new RaadsRSkaaringsberegner().BeregnSkaaring(svar);

        // Ledd 0 (reversert, skalaverdi 1) => 0 poeng. Ledd 1 (symptom, skalaverdi 1) => 3 poeng.
        // Resten (skalaverdi 4 = "aldri stemt") gir 3 poeng for reverserte, 0 for symptomledd.
        var forventet = 0 + 3 + Enumerable.Range(2, 78).Sum(i => RaadsRTestSeeder_ErReversert(i) ? 3 : 0);
        Assert.Equal(forventet, resultat.RaaSkaar);
        Assert.Equal(240, resultat.RaaSkaarMaks);

        static bool RaadsRTestSeeder_ErReversert(int index) =>
            TestBase.Shared.Domain.Tester.InnebygdeTester.RaadsRTestSeeder.ErReversertPerLedd[index];
    }

    [Fact]
    public void Wurs_SummererKunDeTjuefemWurs25LeddIkkeAlleSekstien()
    {
        // Sett ALLE 61 til 0, og bare de 25 WURS-25-posisjonene til 4 — sumSkår skal da bli 25*4=100.
        var verdier = new string[61];
        Array.Fill(verdier, "0");
        foreach (var pos in TestBase.Shared.Domain.Tester.InnebygdeTester.WursTestSeeder.Wurs25Posisjoner)
        {
            verdier[pos] = "4";
        }

        var resultat = new WursSkaaringsberegner().BeregnSkaaring(Svar(verdier));

        Assert.Equal(100, resultat.RaaSkaar);
        Assert.Equal(100, resultat.RaaSkaarMaks);
    }

    [Fact]
    public void Wurs_LeddUtenforWurs25PaavirkerIkkeSkaaren()
    {
        // Motsatt av testen over: WURS-25-posisjonene er 0, ALT ANNET er 4 (maks).
        // Skåren skal likevel bli 0, siden kun WURS-25-delmengden telles.
        var verdier = new string[61];
        Array.Fill(verdier, "4");
        foreach (var pos in TestBase.Shared.Domain.Tester.InnebygdeTester.WursTestSeeder.Wurs25Posisjoner)
        {
            verdier[pos] = "0";
        }

        var resultat = new WursSkaaringsberegner().BeregnSkaaring(Svar(verdier));

        Assert.Equal(0, resultat.RaaSkaar);
    }

    [Fact]
    public void Eq40_EnigKeyetOgUenigKeyetLeddSkaaresMotsatt()
    {
        // Ledd 0 er "enig-keyed" (ErEnigKeyed[0] == true), ledd 1 er "uenig-keyed" (== false).
        // Skala: 3=Helt enig, 2=Litt enig, 1=Litt uenig, 0=Helt uenig.
        var svar = Svar("3", "3").Concat(Enumerable.Repeat(Svar("0"), 38)).ToList();
        var resultat = new Eq40Skaaringsberegner().BeregnSkaaring(svar);

        // Ledd 0 (enig-keyed, "Helt enig") => 2 poeng. Ledd 1 (uenig-keyed, "Helt enig") => 0 poeng.
        // Resten (verdi 0 = "Helt uenig"): enig-keyed => 0 poeng, uenig-keyed => 2 poeng.
        var enigKeyed = TestBase.Shared.Domain.Tester.InnebygdeTester.Eq40TestSeeder.ErEnigKeyed;
        var forventet = 2 + 0 + Enumerable.Range(2, 38).Sum(i => enigKeyed[i] ? 0 : 2);
        Assert.Equal(forventet, resultat.RaaSkaar);
        Assert.Equal(40, resultat.RaaSkaarMaks);
    }

    [Fact]
    public void Sovn_SummererKunDeFjortenSymptomleddOgFlaggerSoevnapneVedHoyeVerdier()
    {
        // Snorking (posisjon 5) og pustepauser (posisjon 6) satt høyt (>=3) skal utløse søvnapné-indikatoren.
        var symptomLedd = Enumerable.Repeat("0", 14).ToArray();
        symptomLedd[5] = "4";
        symptomLedd[6] = "4";
        var resten = Enumerable.Repeat("Nei", 10).ToArray(); // resten av skjemaet — skal ikke påvirke RaaSkaar
        var resultat = new SovnSkaaringsberegner().BeregnSkaaring(Svar(symptomLedd.Concat(resten).ToArray()));

        Assert.Equal(8, resultat.RaaSkaar);
        Assert.NotNull(resultat.Indikatorer);
        var soevnapne = resultat.Indikatorer!.Single(i => i.Navn == "Mulig søvnapné-mistanke");
        Assert.False(soevnapne.Positiv);
    }

    [Fact]
    public void Who5Vas_ProsentskaarErGjennomsnittAvDeFemVasSvarene()
    {
        // 80,80,80,80,80 -> gjennomsnitt 80, over velvære-grensen (50) og depresjon-grensen (28).
        var resultat = new Who5VasSkaaringsberegner().BeregnSkaaring(Svar("80", "80", "80", "80", "80"));

        Assert.Equal(400, resultat.RaaSkaar);
        Assert.Equal(500, resultat.RaaSkaarMaks);
        Assert.Equal(80, resultat.ProsentSkaar);
        Assert.True(resultat.Indikatorer!.Single(i => i.Navn == "Velvære").Positiv);
        Assert.True(resultat.Indikatorer!.Single(i => i.Navn == "Depresjon").Positiv);
    }

    [Fact]
    public void Who5Vas_KontinuerligSkaarKanSkilleVelvaereOgDepresjonsgrenseneNoeLikertIkkeKan()
    {
        // Gjennomsnitt 40: under velvære-grensen (50) men OVER depresjon-grensen (28) —
        // et utfall som er umulig for den diskrete Likert-versjonen, se
        // Who5VasSkaaringsberegner sin klassekommentar.
        var resultat = new Who5VasSkaaringsberegner().BeregnSkaaring(Svar("40", "40", "40", "40", "40"));

        Assert.Equal(40, resultat.ProsentSkaar);
        Assert.False(resultat.Indikatorer!.Single(i => i.Navn == "Velvære").Positiv);
        Assert.True(resultat.Indikatorer!.Single(i => i.Navn == "Depresjon").Positiv, "Skal IKKE indikere depresjon når skåren er over depresjonsgrensen på 28.");
    }

    [Fact]
    public void Who5Vas_LavtEnkeltsvarUtloserUtredningSelvOmTotalenErHoy()
    {
        // Fire svar på 90 (snitt ville vært 76 uten det femte) men ett enkeltsvar på 10 —
        // skal likevel flagges, samme prinsipp som Likert-versjonens "0 eller 1 av 5".
        var resultat = new Who5VasSkaaringsberegner().BeregnSkaaring(Svar("90", "90", "90", "90", "10"));

        Assert.Contains("nærmere undersøkelse", resultat.Fortolkning);
    }

    [Fact]
    public void Who5Vas_HarToNavngitteReferanselinjerFraNormeringen()
    {
        var referanselinjer = new Who5VasSkaaringsberegner().Referanselinjer;

        Assert.Equal(2, referanselinjer.Count);
        Assert.Equal(50, referanselinjer.Single(r => r.Navn == "Velvære").ProsentVerdi);
        Assert.Equal(28, referanselinjer.Single(r => r.Navn == "Depresjon").ProsentVerdi);
    }

    [Fact]
    public void SkaaringsberegnereUtenReferanselinjer_ReturnererTomListeSomStandard()
    {
        // Bekrefter at default-implementasjonen i ITestSkaaringsberegner faktisk
        // fungerer for en beregner som ALDRI har blitt endret for å ta i bruk grafen.
        // Må hentes via grensesnitt-typen — C#s default interface-medlemmer er
        // ikke synlige gjennom en konkret klassereferanse, kun via grensesnittet.
        ITestSkaaringsberegner beregner = new Who5Skaaringsberegner();
        Assert.Empty(beregner.Referanselinjer);
    }

    // --- ICD-11-tester (2026-09-20) -----------------------------------------

    [Fact]
    public void Itq_PtsdKriterierOppfyltUtenDsoGirRentPtsd()
    {
        // Ledd-rekkefølge (se ItqTestSeeder): 0=fritekst, 1=hendelsestid, 2-7=P1-P6,
        // 8-10=P7-P9, 11-16=C1-C6, 17-19=C7-C9. Alle tre PTSD-symptomgrupper +
        // funksjonstap oppfylt (skår 2 i ett ledd per par), alle DSO-grupper på 0.
        var svar = Svar("hendelse", "1", "2", "0", "2", "0", "2", "0", "2", "0", "0",
                         "0", "0", "0", "0", "0", "0", "0", "0", "0");
        var resultat = new ItqSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal(6, resultat.RaaSkaar); // PTSD-skåre 2+0+2+0+2+0, DSO-skåre 0
        Assert.Equal(48, resultat.RaaSkaarMaks);
        Assert.Equal("PTSD", resultat.Indikatorer!.Single(i => i.Navn == "Diagnostisk konklusjon").Verdi);
    }

    [Fact]
    public void Itq_PtsdOgDsoKriterierBeggeOppfyltGirKompleksPtsd()
    {
        var svar = Svar("hendelse", "1", "2", "0", "2", "0", "2", "0", "2", "0", "0",
                         "2", "0", "2", "0", "2", "0", "2", "0", "0");
        var resultat = new ItqSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal("KPTSD", resultat.Indikatorer!.Single(i => i.Navn == "Diagnostisk konklusjon").Verdi);
    }

    [Fact]
    public void Itq_IngenSymptomerGirIngenDiagnose()
    {
        var svar = Svar(Enumerable.Repeat("0", 20).ToArray());
        // Ledd 0 og 1 parses aldri som tall av beregneren, men Svar() krever en verdi — "0" er ufarlig her.
        var resultat = new ItqSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal("Ingen", resultat.Indikatorer!.Single(i => i.Navn == "Diagnostisk konklusjon").Verdi);
        Assert.Equal(0, resultat.RaaSkaar);
    }

    [Fact]
    public void PdsIcd11_BipolareLeddOversettesFraVisningsindeksTilRiktigPoeng()
    {
        // Ledd 1-10: indeks 2 (midterste, "sunt") = 0 poeng hver -> sum 0.
        // Ledd 11-14: indeks lagres direkte som poeng (0..3) -> her 3 hver -> sum 12.
        var svar = Svar(Enumerable.Repeat("2", 10).Concat(Enumerable.Repeat("3", 4)).ToArray());
        var resultat = new PdsIcd11Skaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal(12, resultat.RaaSkaar);
        Assert.Equal(32, resultat.RaaSkaarMaks);
    }

    [Fact]
    public void PdsIcd11_YtterpunkterPaaBeggePolerGirSammePoengSomVerdi2()
    {
        // Indeks 0 og indeks 4 er de to motsatte ytterpunktene for et bipolart ledd 1-10 —
        // begge skal gi 2 poeng (se PoengForBipolarIndeks), ikke indeks-verdien selv.
        var venstrePol = Svar(Enumerable.Repeat("0", 10).Concat(Enumerable.Repeat("0", 4)).ToArray());
        var hoyrePol = Svar(Enumerable.Repeat("4", 10).Concat(Enumerable.Repeat("0", 4)).ToArray());

        var beregner = new PdsIcd11Skaaringsberegner();
        Assert.Equal(20, beregner.BeregnSkaaring(venstrePol).RaaSkaar); // 10 ledd × 2 poeng
        Assert.Equal(20, beregner.BeregnSkaaring(hoyrePol).RaaSkaar);
    }

    [Fact]
    public void Paq11R_ReverserteLeddSnusOgTellerRiktigIRiktigDomene()
    {
        // Alle 17 ledd = 0 ("Aldri"). Reverserte ledd (1,2,8,9) blir da 4 i domenesummene.
        var svar = Svar(Enumerable.Repeat("0", 17).ToArray());
        var resultat = new Paq11RSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal(0, resultat.RaaSkaar); // rå totalsum upåvirket av reversering
        Assert.Equal("12", resultat.Indikatorer!.Single(i => i.Navn.StartsWith("Tilbaketrekning")).Verdi); // 4+4+4+0
        Assert.Equal("4", resultat.Indikatorer!.Single(i => i.Navn.StartsWith("Dyssosialitet")).Verdi); // 4+0+0
    }

    [Fact]
    public void Gadit_HverDagOgDeFlesteDagerSkaarerEttPoengAndreSvarSkaarerNull()
    {
        // Ledd 0-5: verdi 4=Hver dag, 3=De fleste dager (begge 1 poeng), 2/1/0 = 0 poeng.
        var svar = Svar("4", "3", "2", "1", "0", "0", "Ja", "Nei");
        var resultat = new GaditSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal(3, resultat.RaaSkaar); // 1+1+0+0+0+0 + Ja(1) + Nei(0)
        Assert.Equal(8, resultat.RaaSkaarMaks);
        Assert.True(resultat.Indikatorer!.Single().Positiv); // under cutoff (5) -> positiv/grønn
    }

    [Fact]
    public void Gadit_SkaarFemEllerMerUtloserGamingDisorderIndikator()
    {
        var svar = Svar("4", "4", "4", "4", "0", "0", "Ja", "Nei"); // 4×1 (frekvens) + 1 (Ja) = 5
        var resultat = new GaditSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal(5, resultat.RaaSkaar);
        Assert.False(resultat.Indikatorer!.Single().Positiv);
    }

    [Fact]
    public void Idq_KriterierKreverBaadeNokEndosserteLeddOgBekreftetFunksjonstap()
    {
        // 5 ledd (inkl. ett kjerneledd) skåret "De fleste dager" (3), funksjon = "Nei" -> IKKE oppfylt.
        var svarUtenFunksjonstap = Svar("3", "3", "3", "3", "3", "0", "0", "0", "0", "Nei");
        var resultatUten = new IdqSkaaringsberegner().BeregnSkaaring(svarUtenFunksjonstap);
        Assert.Contains("IKKE oppfylt", resultatUten.Fortolkning);

        var svarMedFunksjonstap = Svar("3", "3", "3", "3", "3", "0", "0", "0", "0", "Ja");
        var resultatMed = new IdqSkaaringsberegner().BeregnSkaaring(svarMedFunksjonstap);
        Assert.Contains("er oppfylt", resultatMed.Fortolkning);
        Assert.Equal(15, resultatMed.RaaSkaar);
    }

    [Fact]
    public void Idq_KreverMinstEttAvDeToKjerneleddene()
    {
        // 5 ledd endossert, men INGEN av de to første (kjerne-)leddene -> skal IKKE oppfylle kriteriene.
        var svar = Svar("0", "0", "3", "3", "3", "3", "3", "0", "0", "Ja");
        var resultat = new IdqSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Contains("IKKE oppfylt", resultat.Fortolkning);
    }

    [Fact]
    public void Iaq_KriterierKreverBaadeNokEndosserteLeddOgBekreftetFunksjonstap()
    {
        var svar = Svar("3", "3", "3", "3", "0", "0", "0", "0", "Ja");
        var resultat = new IaqSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Contains("er oppfylt", resultat.Fortolkning);
        Assert.Equal(12, resultat.RaaSkaar);
    }

    [Fact]
    public void Picd_DomenesumOgSnittBeregnesForRiktigeLeddnumre()
    {
        // Sett ledd 5 (1-indeksert, del av Anankasme-domenet) til 5, alt annet til 1.
        var verdier = Enumerable.Repeat("1", 60).ToArray();
        verdier[4] = "5"; // ledd nr. 5 (0-indeksert posisjon 4)
        var resultat = new PicdSkaaringsberegner().BeregnSkaaring(Svar(verdier));

        // Anankasme = ledd 5,10,15,20,25,30,35,40,45,50,55,60 -> ett ledd er 5, resten (11 stk) er 1 -> sum 16.
        var anankasme = resultat.Indikatorer!.Single(i => i.Navn == "Anankasme");
        Assert.Contains("sum 16/60", anankasme.Verdi);
    }
}
