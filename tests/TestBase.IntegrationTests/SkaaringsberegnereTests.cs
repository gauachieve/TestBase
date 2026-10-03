using TestBase.Shared.Domain.Pasienter;
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
    public void Gadit_UbesvartMidtstiltFrekvensledd_KraskerIkkeOgTellerResterendeSvarRiktig()
    {
        // Regresjonstest for reell 500-feil 2026-09-23: TestService.LagreSvarAsync hopper stille
        // over tomme felt (ingen TestSvar-rad for et ubesvart ledd), så en pasient som markerer
        // testen "Fullfort" uten å svare på ETT frekvensspørsmål ender opp med bare 7 TestSvar,
        // ikke 8 — det 3. frekvensleddet mangler her. En posisjonsbasert antagelse ville da tolket
        // det 6. elementet ("Ja") som et frekvenssvar og kastet en FormatException ved int.Parse.
        var svar = Svar("4", "3", /* ledd 3 aldri besvart */ "0", "0", "0", "Ja", "Nei");
        var resultat = new GaditSkaaringsberegner().BeregnSkaaring(svar);

        Assert.Equal(3, resultat.RaaSkaar); // 1(4)+1(3)+0+0+0 + Ja(1) + Nei(0) = 3
        Assert.Equal(8, resultat.RaaSkaarMaks); // maks er fortsatt 8, uavhengig av hvor mange som faktisk svarte
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

    /// <summary>Ledd med fortløpende Id-er (1..N) og en tilhørende TestSvar for hver — til å teste ITestSkaaringsberegnerMedLedd-implementasjoner.</summary>
    private static (List<TestLedd> AlleLedd, List<TestSvar> Svar) LeddOgSvar(params (int LeddNr, string? Verdi)[] par)
    {
        var alleLedd = par.Select(p => new TestLedd { Id = p.LeddNr, TestSideId = 1, Sporsmalstekst = "x", Svartype = TestSvartype.LikertSkala }).ToList();
        var svar = par.Where(p => p.Verdi is not null)
            .Select(p => new TestSvar { TestLeddId = p.LeddNr, SvarVerdi = p.Verdi! })
            .ToList();
        return (alleLedd, svar);
    }

    [Fact]
    public void Asrs_HoppetOverDelASporsmaalForskyverIkkeHvilkeSvarSomTelles()
    {
        // 18 ledd totalt (Del A = 1-6, Del B = 7-18). Ledd 3 (Del A) er UBESVART — skal
        // IKKE gjøre at ledd 7 sin (Del B) verdi feilaktig telles som Del A sitt tredje svar
        // (samme rotårsak-klasse som GADIT-krasjen, se ITestSkaaringsberegnerMedLedd).
        var par = new List<(int, string?)>
        {
            (1, "2"), (2, "3"), (3, null), (4, "3"), (5, "4"), (6, "0")
        };
        for (var i = 7; i <= 18; i++)
        {
            par.Add((i, "4")); // Del B: høye verdier som IKKE skal påvirke Del A-skåren
        }
        var (alleLedd, svar) = LeddOgSvar(par.ToArray());

        var resultat = new AsrsSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        // Ledd 1 (terskel 2): 2>=2 ja. Ledd 2 (terskel 2): 3>=2 ja. Ledd 3: ubesvart, telles ikke.
        // Ledd 4 (terskel 3): 3>=3 ja. Ledd 5 (terskel 3): 4>=3 ja. Ledd 6 (terskel 3): 0>=3 nei.
        // -> 4 av 6 over terskel, uavhengig av Del B sine (mye høyere) verdier.
        Assert.Equal(4, resultat.RaaSkaar);
        Assert.Equal(6, resultat.RaaSkaarMaks);
        Assert.Contains("Positiv", resultat.Indikatorer!.Single().Verdi);
    }

    [Fact]
    public void Scl25_SelvmordsleddFlaggesKunNaarBesvartOverLaveste()
    {
        var par = Enumerable.Range(1, 25).Select(i => (i, (string?)"1")).ToList();
        par[23] = (24, "3"); // Ledd 24 = "Tanker om å avslutte livet", besvart 3 (over 1)
        var (alleLedd, svar) = LeddOgSvar(par.ToArray());

        var resultat = new Scl25Skaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Contains(resultat.Indikatorer!, i => i.Navn.Contains("Selvmordsscreening") && !i.Positiv);
    }

    [Fact]
    public void Edeq_GlobalskaarErSnittAvFireDelskalaerIkkeVektetSnittAvEnkeltledd()
    {
        // Restriksjon (5 ledd) og Spisebekymring (5 ledd) = 0, Figurbekymring (8 ledd) og
        // Vektbekymring (5 ledd) = 6 (maks) -> delskala-snitt 0, 0, 6, 6 -> global (0+0+6+6)/4 = 3.
        var par = new List<(int, string?)>();
        for (var i = 1; i <= 10; i++) par.Add((i, "0"));
        for (var i = 11; i <= 23; i++) par.Add((i, "6"));
        for (var i = 24; i <= 28; i++) par.Add((i, "tekst")); // ikke-skårede fritekstledd
        var (alleLedd, svar) = LeddOgSvar(par.ToArray());

        var resultat = new EdeqSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Contains("3,00", resultat.Fortolkning.Replace(".", ","));
        Assert.Equal(138, resultat.RaaSkaarMaks);
    }

    [Fact]
    public void TrapsIi_HoppetOverEksponeringsleddPaavirkerIkkePtsdDsoKlassifisering()
    {
        // Del 1 (15 ledd) + "Om hendelsen" (2 ledd) hoppes helt over/delvis over -- skal IKKE
        // forskyve hvilke ledd som telles som PTSD/DSO-symptomer (samme rotårsak som GADIT).
        var par = new List<(int, string?)>();
        for (var i = 1; i <= 15; i++) par.Add((i, i % 3 == 0 ? null : "Ja")); // noen Del 1-ledd ubesvart
        par.Add((16, null)); // "Om hendelsen" beskrivelse, ubesvart
        par.Add((17, "3"));  // hendelsestidspunkt
        // PTSD-symptomer (18-23): oppfyller alle tre par -> reDx/avDx/thDx sanne
        par.Add((18, "3")); par.Add((19, "0")); par.Add((20, "3")); par.Add((21, "0")); par.Add((22, "3")); par.Add((23, "0"));
        // PTSD-funksjon (24-26): minst én over terskel
        par.Add((24, "2")); par.Add((25, "0")); par.Add((26, "0"));
        // DSO-symptomer (27-32): INGEN par oppfyller terskel -> dsoKriterier skal bli falsk
        for (var i = 27; i <= 32; i++) par.Add((i, "0"));
        // DSO-funksjon (33-35)
        par.Add((33, "0")); par.Add((34, "0")); par.Add((35, "0"));

        var (alleLedd, svar) = LeddOgSvar(par.ToArray());

        var resultat = new TrapsIiSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Contains("PTSD er oppfylt (uten kompleks PTSD)", resultat.Fortolkning);
    }

    [Fact]
    public void TrapsIi_ListerKlyngeskaarOgBekreftedeTraumeeksponeringerSomIndikatorer()
    {
        // Brukerens tilbakemelding 2026-09-27: rapporten var "for forenklet" — ba om
        // klyngeskår (Re/Av/Th/Ad/Nsc/Dr) og en liste over hvilke traumeeksponeringer
        // som faktisk ble besvart "Ja" (ikke bare den endelige PTSD/KPTSD-konklusjonen).
        var alleLedd = new List<TestLedd>();
        var svar = new List<TestSvar>();
        void Legg(long id, string tekst, TestSvartype type, string? verdi)
        {
            alleLedd.Add(new TestLedd { Id = id, TestSideId = 1, Sporsmalstekst = tekst, Svartype = type });
            if (verdi is not null) svar.Add(new TestSvar { TestLeddId = id, SvarVerdi = verdi });
        }

        // Del 1: 14 JaNei-ledd, kun ledd 1 og 3 besvart "Ja", resten "Nei".
        for (var i = 1; i <= 14; i++)
        {
            Legg(i, $"Traume-spørsmål {i}", TestSvartype.JaNei, i is 1 or 3 ? "Ja" : "Nei");
        }
        Legg(15, "Annen hendelse, beskrevet", TestSvartype.Fritekst, "Ble utsatt for noe annet");
        Legg(16, "Om hendelsen: beskrivelse", TestSvartype.Fritekst, null);
        Legg(17, "Om hendelsen: tidspunkt", TestSvartype.LikertSkala, "3");
        // PTSD-symptomer (Re/Av/Th alle til stede, skår 3/8 hver)
        Legg(18, "P1", TestSvartype.LikertSkala, "3"); Legg(19, "P2", TestSvartype.LikertSkala, "0");
        Legg(20, "P3", TestSvartype.LikertSkala, "3"); Legg(21, "P4", TestSvartype.LikertSkala, "0");
        Legg(22, "P5", TestSvartype.LikertSkala, "3"); Legg(23, "P6", TestSvartype.LikertSkala, "0");
        Legg(24, "F1", TestSvartype.LikertSkala, "2"); Legg(25, "F2", TestSvartype.LikertSkala, "0"); Legg(26, "F3", TestSvartype.LikertSkala, "0");
        // DSO-symptomer: alle 0 -> Ad/Nsc/Dr skal vise 0/8
        for (var i = 27; i <= 32; i++) Legg(i, $"C{i - 26}", TestSvartype.LikertSkala, "0");
        Legg(33, "Cf1", TestSvartype.LikertSkala, "0"); Legg(34, "Cf2", TestSvartype.LikertSkala, "0"); Legg(35, "Cf3", TestSvartype.LikertSkala, "0");

        var resultat = new TrapsIiSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Contains("Gjenopplevelse (Re): 3/8", resultat.Fortolkning);
        Assert.Contains("Unngåelse (Av): 3/8", resultat.Fortolkning);
        Assert.Contains("Nåværende trusselfølelse (Th): 3/8", resultat.Fortolkning);
        Assert.Contains("Affektregulering (Ad): 0/8", resultat.Fortolkning);

        Assert.Contains(resultat.Indikatorer!, i => i.Verdi == "Bekreftet: Traume-spørsmål 1");
        Assert.Contains(resultat.Indikatorer!, i => i.Verdi == "Bekreftet: Traume-spørsmål 3");
        Assert.Contains(resultat.Indikatorer!, i => i.Verdi == "Bekreftet (annet): Ble utsatt for noe annet");
        Assert.DoesNotContain(resultat.Indikatorer!, i => i.Verdi.Contains("Traume-spørsmål 2"));
    }

    [Fact]
    public void Core10_ReverseSkaarerLeddEnOgFemUavhengigAvHoppetOverLedd()
    {
        // Ledd 2 (indeks 1) hoppes over. Ledd 1 og 5 (positivt formulert) = 0 (verst) -> reverse-skåres til 4 hver.
        // Ledd 3,4,6,7,8,9,10 = 0. Forventet sum: 4 (ledd1) + 0(ledd2 mangler) + 0+0 + 4(ledd5) + 0+0+0+0+0 = 8.
        var par = new List<(int, string?)> { (1, "0"), (2, null), (3, "0"), (4, "0"), (5, "0"), (6, "0"), (7, "0"), (8, "0"), (9, "0"), (10, "0") };
        var (alleLedd, svar) = LeddOgSvar(par.ToArray());

        var resultat = new Core10Skaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Equal(8, resultat.RaaSkaar);
    }

    [Fact]
    public void Core10_LivetIkkeVerdtAaLeveFlaggesSeparatUavhengigAvTotalskaar()
    {
        var par = Enumerable.Range(1, 10).Select(i => (i, (string?)"0")).ToList();
        par[9] = (10, "2"); // Ledd 10 = "livet ikke verdt å leve", besvart 2 (over 0)
        var (alleLedd, svar) = LeddOgSvar(par.ToArray());

        var resultat = new Core10Skaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Contains(resultat.Indikatorer!, i => i.Navn.Contains("Livet ikke verdt") && !i.Positiv);
    }

    [Fact]
    public void CoreOm_ReverseSkaarerRiktigeLeddOgFlaggerRisikoUavhengigAvHoppetOverLedd()
    {
        // 34 ledd: Velvære(1-4) alle "0" -> reversert (posisjon 0,1,3) gir 4+4+0+4=12, ledd 2 (indeks 1) hoppes over.
        var par = new List<(int, string?)>
        {
            (1, "0"), (2, null), (3, "0"), (4, "0")
        };
        for (var i = 5; i <= 16; i++) par.Add((i, "0"));   // Problemer/symptomer
        for (var i = 17; i <= 28; i++) par.Add((i, "0"));  // Livsfunksjon
        for (var i = 29; i <= 34; i++) par.Add((i, "0"));  // Risiko, alt 0 bortsett fra ett ledd under
        par[par.Count - 1] = (34, "3"); // Ledd 34 = "truet med å skade andre" -> risiko for andre flagges

        var (alleLedd, svar) = LeddOgSvar(par.ToArray());
        var resultat = new CoreOmSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        // Velvære: ledd1(revers 4-0=4) + ledd2(revers, mangler->0, 4-0=4) + ledd3(IKKE revers, 0) + ledd4(revers 4) = 12.
        Assert.Contains("Velvære 12/16", resultat.Fortolkning);
        Assert.Contains(resultat.Indikatorer!, i => i.Navn.Contains("Risiko for andre") && !i.Positiv);
    }

    [Fact]
    public void CoreA_ErIkkeEtSumskaarVerktoy_FlaggerEnkeltleddSelvOmTotalenErLav()
    {
        var par = Enumerable.Range(1, 8).Select(i => (i, (string?)"0")).ToList();
        par[2] = (3, "1"); // Ledd 3 = konkret selvmordsplan, besvart 1 (lavt, men IKKE null)
        var (alleLedd, svar) = LeddOgSvar(par.ToArray());

        var resultat = new CoreARisikoSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Contains("MINST ETT RISIKOLEDD", resultat.Fortolkning);
        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Risiko for seg selv" && !i.Positiv);
        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Risiko for andre" && i.Positiv);
    }

    [Fact]
    public void Sipp118_ReverseSkaarerMaladaptiveLeddOgManglendeLeddTellerSomZeroIkkeFalskMaks()
    {
        // Selvkontroll (ledd 1-6): ledd 1 (maladaptiv, reversert) besvart "1" -> reverse til 5-1=4 (best mulig).
        // Ledd 3 (maladaptiv, reversert) HOPPES OVER -> skal telle 0, IKKE bli reversert til en falsk "5".
        var par = new List<(int, string?)> { (1, "1"), (2, "4"), (3, null), (4, "4"), (5, "4"), (6, "4") };
        for (var i = 7; i <= 30; i++) par.Add((i, "4")); // Resten: alle adaptive svar er "4" (best)
        var (alleLedd, svar) = LeddOgSvar(par.ToArray());

        var resultat = new Sipp118Skaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        // Selvkontroll: ledd1(revers 5-1=4) + ledd2(4) + ledd3(mangler->0, IKKE reversert til 5) + ledd4(revers 5-4=1) + ledd5(revers 5-4=1) + ledd6(4) = 14.
        Assert.Contains("Selvkontroll 14/24", resultat.Fortolkning);
    }

    [Fact]
    public void YgtssR_SjekklisteMedUliktAntallAvkrysningerForskyverIkkeRangeringsleddene()
    {
        // Motorisk sjekkliste (10 ledd) har KUN 3 av 10 besvart "Ja" (resten hoppet over) -- skal
        // IKKE forskyve hvilke 5 påfølgende ledd som telles som rangeringsdimensjoner.
        var par = new List<(int, string?)>();
        for (var i = 1; i <= 10; i++) par.Add((i, i <= 3 ? "Ja" : null));
        // Motorisk rangering (ledd 11-15): sum skal bli 5+5+5+5+5=25 (maks).
        for (var i = 11; i <= 15; i++) par.Add((i, "5"));
        for (var i = 16; i <= 23; i++) par.Add((i, "Nei")); // Fonatorisk sjekkliste, alt "Nei"
        for (var i = 24; i <= 28; i++) par.Add((i, "0"));   // Fonatorisk rangering: sum 0
        par.Add((29, "20"));                                 // Funksjonsnedsettelse

        var (alleLedd, svar) = LeddOgSvar(par.ToArray());
        var resultat = new YgtssRSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Contains("Motorisk delskår 25/25", resultat.Fortolkning);
        Assert.Contains("fonatorisk delskår 0/25", resultat.Fortolkning);
        Assert.Equal(45, resultat.RaaSkaar); // 25 (motorisk) + 0 (fonatorisk) + 20 (funksjon)
    }

    [Fact]
    public void MadrsKlinikk_SelvmordsleddFlaggesSeparatUavhengigAvLavTotalskaar()
    {
        // Ledd 1-9 = 0 (ingen depresjon), ledd 10 (selvmordstanker, siste ledd-ID) = 2 -> lav
        // totalskår (2/60) men selvmordsleddet skal FORTSATT flagges eksplisitt.
        var par = Enumerable.Range(1, 10).Select(i => (i, (string?)"0")).ToList();
        par[9] = (10, "2");
        var (alleLedd, svar) = LeddOgSvar(par.ToArray());

        var resultat = new MadrsKlinikkSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Equal(2, resultat.RaaSkaar);
        Assert.Equal(60, resultat.RaaSkaarMaks);
        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Selvmordstanker" && !i.Positiv);
    }

    [Fact]
    public void MadrsKlinikk_HoppetOverLeddForskyverIkkeHvilketLeddSomErSelvmordsleddet()
    {
        // Ledd 5 (redusert appetitt) hoppes over. Selvmordsleddet identifiseres via ekte
        // TestLeddId (10, siste), IKKE listeposisjon i svar-listen (som her ville vært indeks 8).
        var par = new List<(int, string?)> { (1, "6"), (2, "6"), (3, "6"), (4, "6"), (5, null), (6, "6"), (7, "6"), (8, "6"), (9, "6"), (10, "6") };
        var (alleLedd, svar) = LeddOgSvar(par.ToArray());

        var resultat = new MadrsKlinikkSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Equal(54, resultat.RaaSkaar); // 9 besvarte ledd * 6
        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Selvmordstanker" && !i.Positiv);
        Assert.Contains("alvorlig deprimert", resultat.Fortolkning);
    }

    /// <summary>
    /// Bygger SCID-5-PF-strukturen (10 "sider", én per forstyrrelse, i samme rekkefølge og med
    /// samme kriterieantall som Scid5PfTestSeeder — Unnvikende, Avhengig, Tvangspreget, Paranoid,
    /// Schizotyp, Schizoid, Histrionisk, Narsissistisk, Borderline, Antisosial SIST (rettet
    /// 2026-09-27 til SCID-5-PD sin faktiske modulrekkefølge). Antisosial (siste side) har i
    /// tillegg 2 JaNei-portvaktledd FØR sine 7 kriterier. kriterieSvar[i] gir svarverdi
    /// ("0"/"1"/"2") for hvert kriterium i forstyrrelse nr. i — for kort array fylles resten
    /// ubesvart (null).
    /// </summary>
    private static (List<TestLedd> AlleLedd, List<TestSvar> Svar) Scid5PfBygg(
        string?[][] kriterieSvar, string? antisosialGate1 = "Nei", string? antisosialGate2 = "Nei")
    {
        var antallKriterier = new[] { 7, 8, 8, 7, 9, 7, 8, 9, 9, 7 };
        var alleLedd = new List<TestLedd>();
        var svar = new List<TestSvar>();
        long nesteId = 1;

        for (var side = 0; side < antallKriterier.Length; side++)
        {
            var sideId = side + 1;
            if (side == 9) // Antisosial (siste side): 2 portvaktledd FØR kriteriene
            {
                var gate1 = new TestLedd { Id = nesteId++, TestSideId = sideId, Sporsmalstekst = "gate1", Svartype = TestSvartype.JaNei };
                var gate2 = new TestLedd { Id = nesteId++, TestSideId = sideId, Sporsmalstekst = "gate2", Svartype = TestSvartype.JaNei };
                alleLedd.Add(gate1);
                alleLedd.Add(gate2);
                if (antisosialGate1 is not null) svar.Add(new TestSvar { TestLeddId = gate1.Id, SvarVerdi = antisosialGate1 });
                if (antisosialGate2 is not null) svar.Add(new TestSvar { TestLeddId = gate2.Id, SvarVerdi = antisosialGate2 });
            }

            for (var k = 0; k < antallKriterier[side]; k++)
            {
                var ledd = new TestLedd { Id = nesteId++, TestSideId = sideId, Sporsmalstekst = "k", Svartype = TestSvartype.LikertSkala };
                alleLedd.Add(ledd);
                var verdi = side < kriterieSvar.Length && k < kriterieSvar[side].Length ? kriterieSvar[side][k] : null;
                if (verdi is not null)
                {
                    svar.Add(new TestSvar { TestLeddId = ledd.Id, SvarVerdi = verdi });
                }
            }
        }

        return (alleLedd, svar);
    }

    [Fact]
    public void Scid5Pf_ForstyrrelseFlaggesNaarAntallOppfylteKriterierNaarEgenTerskel()
    {
        // Paranoid (nå 4. side, terskel 4 av 7): 4 kriterier "Tydelig oppfylt" (verdi 2), 3 "Fraværende" (0).
        var paranoid = new[] { "2", "2", "2", "2", "0", "0", "0" };
        var kriterieSvar = new string?[10][];
        for (var i = 0; i < 10; i++) { kriterieSvar[i] = Array.Empty<string?>(); }
        kriterieSvar[3] = paranoid;
        var (alleLedd, svar) = Scid5PfBygg(kriterieSvar!);

        var resultat = new Scid5PfSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Paranoid personlighetsforstyrrelse" && !i.Positiv);
        Assert.Contains("Paranoid personlighetsforstyrrelse", resultat.Fortolkning);
    }

    [Fact]
    public void Scid5Pf_AntisosialKreverBeggePortvakterJaIkkeBareKriterieterskel()
    {
        // Alle 7 antisosial-kriterier "Tydelig oppfylt" (langt over terskel 3), men portvakt 2
        // ("18 år eller eldre?") er "Nei" -> forstyrrelsen skal IKKE flagges likevel. Antisosial
        // er nå SISTE side (indeks 9), i tråd med SCID-5-PD sin faktiske modulrekkefølge.
        var antisosialKriterier = new[] { "2", "2", "2", "2", "2", "2", "2" };
        var kriterieSvar = new string?[10][];
        kriterieSvar[9] = antisosialKriterier;
        for (var i = 0; i < 10; i++)
        {
            if (i != 9) kriterieSvar[i] = Array.Empty<string?>();
        }
        var (alleLedd, svar) = Scid5PfBygg(kriterieSvar!, antisosialGate1: "Ja", antisosialGate2: "Nei");

        var resultat = new Scid5PfSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        var antisosialIndikator = resultat.Indikatorer!.Single(i => i.Navn == "Antisosial personlighetsforstyrrelse");
        Assert.True(antisosialIndikator.Positiv); // Positiv = terskel IKKE nådd, til tross for 7/7 kriterier
        Assert.DoesNotContain("Antisosial", resultat.Fortolkning);
    }

    /// <summary>
    /// Bygger HCR-20 V3-strukturen: 20 faktorer (ÉN kombinert Tilstede+Relevans-ledd hver, se
    /// RETTET 2026-10-02 i Hcr20V3TestSeeder) fordelt på 3 sider (Historisk 10, Klinisk 5,
    /// Risikohåndtering 5), en tom Formulering-side (3 fritekstledd, ubesvart), og en
    /// Konklusjon-side med 4 Trinn 7-ledd i fast rekkefølge (Fremtidig vold, Alvorlig skade,
    /// Umiddelbar vold, Annen risiko).
    /// </summary>
    private static (List<TestLedd> AlleLedd, List<TestSvar> Svar) Hcr20V3Bygg(
        int?[] historiskRelevans, int fremtidigVold, int alvorligSkade, int umiddelbarVold, int annenRisiko)
    {
        var alleLedd = new List<TestLedd>();
        var svar = new List<TestSvar>();
        long nesteId = 1;

        void LeggTilFaktorSide(int sideId, int antallFaktorer, int?[]? relevansOverstyring = null)
        {
            for (var i = 0; i < antallFaktorer; i++)
            {
                var faktorLedd = new TestLedd { Id = nesteId++, TestSideId = sideId, Sporsmalstekst = "tilstede og relevans", Svartype = TestSvartype.LikertSkala };
                alleLedd.Add(faktorLedd);
                var relevans = relevansOverstyring is not null && i < relevansOverstyring.Length ? relevansOverstyring[i] : 0;
                if (relevans is not null)
                {
                    svar.Add(new TestSvar { TestLeddId = faktorLedd.Id, SvarVerdi = relevans.Value.ToString() });
                }
            }
        }

        LeggTilFaktorSide(1, 10, historiskRelevans); // Historisk
        LeggTilFaktorSide(2, 5);                     // Klinisk
        LeggTilFaktorSide(3, 5);                     // Risikohåndtering

        // Formulering-side: 3 fritekstledd, alle ubesvart
        for (var i = 0; i < 3; i++)
        {
            alleLedd.Add(new TestLedd { Id = nesteId++, TestSideId = 4, Sporsmalstekst = "fritekst", Svartype = TestSvartype.Fritekst });
        }

        // Konklusjon-side: 4 ledd i fast rekkefølge
        var konklusjonLedd = new List<TestLedd>();
        foreach (var _ in Enumerable.Range(0, 4))
        {
            var ledd = new TestLedd { Id = nesteId++, TestSideId = 5, Sporsmalstekst = "konklusjon", Svartype = TestSvartype.LikertSkala };
            konklusjonLedd.Add(ledd);
            alleLedd.Add(ledd);
        }
        svar.Add(new TestSvar { TestLeddId = konklusjonLedd[0].Id, SvarVerdi = fremtidigVold.ToString() });
        svar.Add(new TestSvar { TestLeddId = konklusjonLedd[1].Id, SvarVerdi = alvorligSkade.ToString() });
        svar.Add(new TestSvar { TestLeddId = konklusjonLedd[2].Id, SvarVerdi = umiddelbarVold.ToString() });
        svar.Add(new TestSvar { TestLeddId = konklusjonLedd[3].Id, SvarVerdi = annenRisiko.ToString() });

        return (alleLedd, svar);
    }

    [Fact]
    public void Hcr20V3_KonklusjonenErKlinikerensEgenVurderingIkkeEnUtledetSum()
    {
        // Historiske faktorer: ALLE 10 vurdert Høy relevans (verdi 3) -- ville gitt "høy risiko" i et
        // sumskår-verktøy -- men kliniker konkluderer likevel Lav/Lav/Lav/Nei i Trinn 7. Rapporten
        // skal vise klinikerens EGEN konklusjon, ikke noe utledet fra de 10 høy-relevans-faktorene.
        var alleHoye = Enumerable.Repeat(3, 10).Select(v => (int?)v).ToArray();
        var (alleLedd, svar) = Hcr20V3Bygg(alleHoye, fremtidigVold: 0, alvorligSkade: 0, umiddelbarVold: 0, annenRisiko: 0);

        var resultat = new Hcr20V3Skaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Fremtidig vold/prioritering" && i.Verdi == "Lav" && i.Positiv);
        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Annen risiko" && i.Verdi == "Nei" && i.Positiv);
        Assert.Equal(10, resultat.RaaSkaar); // deskriptivt: 10 historiske faktorer med Høy relevans
        Assert.Contains("IKKE grunnlaget for konklusjonen", resultat.Fortolkning);
    }

    [Fact]
    public void Hcr20V3_HoyKonklusjonVisesKorrekt()
    {
        var ingenRelevante = Enumerable.Repeat(0, 10).Select(v => (int?)v).ToArray();
        var (alleLedd, svar) = Hcr20V3Bygg(ingenRelevante, fremtidigVold: 2, alvorligSkade: 2, umiddelbarVold: 1, annenRisiko: 2);

        var resultat = new Hcr20V3Skaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Fremtidig vold/prioritering" && i.Verdi == "Høy" && !i.Positiv);
        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Umiddelbar vold" && i.Verdi == "Moderat" && !i.Positiv);
        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Annen risiko" && i.Verdi == "Ja" && !i.Positiv);
        Assert.Equal(0, resultat.RaaSkaar);
    }

    [Fact]
    public void MiniStrukturdemo_TellerJaPerModulOgFlaggerKunModulerMedMinstEttJa()
    {
        // Modul 1 (2 ledd): ett "Ja" -> flagget. Modul 2 (2 ledd): begge "Nei" -> ikke flagget.
        var alleLedd = new List<TestLedd>
        {
            new() { Id = 1, TestSideId = 10, Sporsmalstekst = "s1", Svartype = TestSvartype.JaNei },
            new() { Id = 2, TestSideId = 10, Sporsmalstekst = "s2", Svartype = TestSvartype.JaNei },
            new() { Id = 3, TestSideId = 20, Sporsmalstekst = "s3", Svartype = TestSvartype.JaNei },
            new() { Id = 4, TestSideId = 20, Sporsmalstekst = "s4", Svartype = TestSvartype.JaNei }
        };
        var svar = new List<TestSvar>
        {
            new() { TestLeddId = 1, SvarVerdi = "Ja" },
            new() { TestLeddId = 2, SvarVerdi = "Nei" },
            new() { TestLeddId = 3, SvarVerdi = "Nei" },
            new() { TestLeddId = 4, SvarVerdi = "Nei" }
        };

        var resultat = new MiniStrukturdemoSkaaringsberegner().BeregnSkaaringMedLedd(svar, alleLedd);

        // Kun FLAGGEDE moduler blir egne indikatorer (2026-09-27) — "Suicidalitet" (0/2 Ja) skal
        // IKKE lenger dukke opp som en egen indikator i det hele tatt.
        Assert.Single(resultat.Indikatorer!);
        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Depressivt episode" && i.Verdi == "Depressivt episode (1/2)" && !i.Positiv);
        Assert.DoesNotContain(resultat.Indikatorer!, i => i.Navn == "Suicidalitet");
        Assert.True(resultat.SkjulProsent);
        Assert.Equal(1, resultat.RaaSkaar);
        Assert.Equal(4, resultat.RaaSkaarMaks);
    }

    [Fact]
    public void Scid5Pf_GrupperingErRobustMotUsortertLeddrekkefolge()
    {
        // Samme Paranoid-scenario som over, men alleLedd-listen stokkes om FØR den sendes inn —
        // grupperingen skjer via TestSideId + minste ekte TestLeddId, ikke listens rekkefølge.
        var paranoid = new[] { "2", "2", "2", "2", "0", "0", "0" };
        var kriterieSvar = new string?[10][];
        for (var i = 0; i < 10; i++) { kriterieSvar[i] = Array.Empty<string?>(); }
        kriterieSvar[3] = paranoid;
        var (alleLedd, svar) = Scid5PfBygg(kriterieSvar!);
        var stokketLedd = alleLedd.OrderByDescending(l => l.Id).ToList();

        var resultat = new Scid5PfSkaaringsberegner().BeregnSkaaringMedLedd(svar, stokketLedd);

        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Paranoid personlighetsforstyrrelse" && !i.Positiv);
    }

    [Fact]
    public void Sipp118Radar_ForsteAksenPlassertRettOverSenterVedMaksVerdi()
    {
        // Indikator 0 er "Samlet..." (hoppes over). Domene 0 (Selvkontroll) = 24/24 (maks) ->
        // skal plasseres RETT OVER senteret (start på -90°/toppen), med full radius.
        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Samlet personlighetsfunksjon", "Innenfor forventet område", true),
            new("Selvkontroll", "24/24", true),
            new("Identitetsintegrasjon", "12/24", true),
            new("Relasjonell kapasitet", "12/24", true),
            new("Ansvarlighet", "12/24", true),
            new("Sosial harmoni", "12/24", true)
        };

        var radar = Sipp118RadarBeregner.Beregn(indikatorer);

        Assert.NotNull(radar);
        var selvkontroll = radar!.Punkter[0];
        Assert.Equal(radar.Senter, selvkontroll.X, precision: 1);
        Assert.Equal(radar.Senter - radar.MaksRadius, selvkontroll.Y, precision: 1);
    }

    [Fact]
    public void Sipp118Radar_ReturnererNullNaarIkkeFemDomeneIndikatorer()
    {
        var radar = Sipp118RadarBeregner.Beregn(new[] { new TestSkaaringIndikator("Bare én", "1/2", true) });
        Assert.Null(radar);
    }

    [Fact]
    public void Scid5PfBar_ParanoidOverTerskelFaarKorrektSegmentbreddeOgTerskelNaadd()
    {
        // Paranoid (side-indeks 3, terskel 4 av 7): 4 "Tydelig oppfylt" (2), 2 "Delvis" (1), 1 "Fraværende" (0).
        var paranoid = new[] { "2", "2", "2", "2", "1", "1", "0" };
        var kriterieSvar = new string?[10][];
        for (var i = 0; i < 10; i++) { kriterieSvar[i] = Array.Empty<string?>(); }
        kriterieSvar[3] = paranoid;
        var (alleLedd, svar) = Scid5PfBygg(kriterieSvar!);
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);

        var data = Scid5PfBarBeregner.Beregn(alleLedd, svarPerLeddId);

        Assert.NotNull(data);
        var paranoidStolpe = data!.Stolper.Single(s => s.Navn == "Paranoid personlighetsforstyrrelse");
        Assert.True(paranoidStolpe.TerskelNaadd);
        Assert.Equal(4, paranoidStolpe.Antall2);
        Assert.Equal(2, paranoidStolpe.Antall1);
        // 4 kriterier * pikselbredde per kriterium for segment2 — stolpen er nå PROPORSJONAL med
        // ekte antall ledd (7), ikke en fast bredde uansett antall kriterier.
        Assert.Equal(4 * Scid5PfBarBeregner.PikslerPerKriterium, paranoidStolpe.Segment2Bredde, precision: 1);
        Assert.Equal(7 * Scid5PfBarBeregner.PikslerPerKriterium, paranoidStolpe.BarBredde, precision: 1);
        Assert.False(data.VurderBlandetPf); // én forstyrrelse NÅDDE sin terskel -> ikke "blandet"
    }

    [Fact]
    public void Scid5PfBar_VurderBlandetPfNaarIngenEnkeltForstyrrelseNaarTerskelMenSamletHoyt()
    {
        // Tre ulike forstyrrelser med 4 "Tydelig oppfylt" hver, INGEN av dem nok til å nå SIN egen
        // terskel alene (Unnvikende trenger 4 av 7 -> akkurat 4 NÅR den... juster til 3 hver i stedet
        // for å garantere at INGEN når egen terskel, men summen (9) er under vår 10-grense — bruk 4
        // forstyrrelser à 3 for å nå totalt 12 uten at noen treffer sin egen terskel).
        var kriterieSvar = new string?[10][];
        for (var i = 0; i < 10; i++) { kriterieSvar[i] = Array.Empty<string?>(); }
        kriterieSvar[0] = new[] { "2", "2", "2", "0", "0", "0", "0" };           // Unnvikende: 3/7, terskel 4 -> ikke nådd
        kriterieSvar[1] = new[] { "2", "2", "2", "0", "0", "0", "0", "0" };      // Avhengig: 3/8, terskel 5 -> ikke nådd
        kriterieSvar[2] = new[] { "2", "2", "2", "0", "0", "0", "0", "0" };      // Tvangspreget: 3/8, terskel 4 -> ikke nådd
        kriterieSvar[3] = new[] { "2", "2", "2", "0", "0", "0", "0" };           // Paranoid: 3/7, terskel 4 -> ikke nådd
        var (alleLedd, svar) = Scid5PfBygg(kriterieSvar!);
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);

        var data = Scid5PfBarBeregner.Beregn(alleLedd, svarPerLeddId);

        Assert.NotNull(data);
        Assert.DoesNotContain(data!.Stolper, s => s.TerskelNaadd);
        Assert.Equal(12, data.TotaltAntall2);
        Assert.True(data.VurderBlandetPf);
    }

    /// <summary>
    /// Bygger MPFI-24 sine 24 ledd (ingen sider/sidestruktur å teste her, se
    /// Mpfi24TestSeeder) med oppgitte svarverdier (1-6). Sorteringen i
    /// Mpfi24Skaaringsberegner er på TestLedd.Id — IDene settes derfor i stigende
    /// rekkefølge 1..24, akkurat som EF Core ville generert dem i praksis.
    /// </summary>
    private static (List<TestLedd> AlleLedd, List<TestSvar> Svar) Mpfi24Bygg(int[] svarverdier)
    {
        Assert.Equal(24, svarverdier.Length);
        var alleLedd = new List<TestLedd>();
        var svar = new List<TestSvar>();
        for (var i = 0; i < 24; i++)
        {
            var ledd = new TestLedd { Id = i + 1, TestSideId = 1, Sporsmalstekst = $"ledd{i + 1}", Svartype = TestSvartype.LikertSkala };
            alleLedd.Add(ledd);
            svar.Add(new TestSvar { TestLeddId = ledd.Id, SvarVerdi = svarverdier[i].ToString() });
        }
        return (alleLedd, svar);
    }

    [Fact]
    public void Mpfi24_UtenBiologiskKjonnGirGyldighetsAdvarselIkkeException()
    {
        var (alleLedd, svar) = Mpfi24Bygg(Enumerable.Repeat(3, 24).ToArray());

        var resultat = new Mpfi24Skaaringsberegner().BeregnSkaaringMedBiologiskKjonn(svar, alleLedd, null);

        Assert.NotNull(resultat.GyldighetsAdvarsel);
        Assert.Contains("biologisk kjønn", resultat.GyldighetsAdvarsel);
        Assert.Null(resultat.Indikatorer);
    }

    [Fact]
    public void Mpfi24_BeregnerDelskalaerOgGlobalskaarerKorrektForMann()
    {
        // Ledd 1-2 (Aksept) = 6,6 -> snitt 6. Ledd 19-20 (Fusjon) = 1,1 -> snitt 1 — BEVISST
        // IKKE påvirket av ledd 21 (satt til 6 under), som beviser at Fusjon-delskalaen bruker
        // KUN ledd 19-20 og ikke regnearkets feilaktige 19-21 (se Mpfi24TestSeeder).
        var svarverdier = new[]
        {
            6, 6, // Aksept (1,2)
            3, 3, // Nærvær (3,4)
            3, 3, // Selvet (5,6)
            3, 3, // Defusjon (7,8)
            3, 3, // Verdier (9,10)
            3, 3, // Handling (11,12)
            3, 3, // Opplevelsesunngåelse (13,14)
            3, 3, // Ikke til stede (15,16)
            3, 3, // Begrepsselv (17,18)
            1, 1, // Fusjon (19,20)
            6, 3, // Manglende kontakt med verdier (21,22) — ledd 21=6 skal IKKE smitte over i Fusjon
            3, 3  // Passivitet (23,24)
        };
        var (alleLedd, svar) = Mpfi24Bygg(svarverdier);

        var resultat = new Mpfi24Skaaringsberegner().BeregnSkaaringMedBiologiskKjonn(svar, alleLedd, BiologiskKjonn.Mann);

        Assert.NotNull(resultat.Indikatorer);
        var aksept = resultat.Indikatorer!.Single(i => i.Navn == "Fleksibilitet — Aksept");
        Assert.Equal("6.00/3.50", aksept.Verdi); // mannsnorm 3.5
        Assert.True(aksept.Positiv);

        var fusjon = resultat.Indikatorer!.Single(i => i.Navn == "Rigiditet — Fusjon");
        Assert.Equal("1.00/2.80", fusjon.Verdi); // mannsnorm 2.8 — IKKE påvirket av ledd 21=6
        Assert.True(fusjon.Positiv); // lavere enn norm er BRA for en rigiditets-delskala

        var manglerKontaktVerdier = resultat.Indikatorer!.Single(i => i.Navn == "Rigiditet — Manglende kontakt med verdier");
        Assert.Equal("4.50/2.60", manglerKontaktVerdier.Verdi); // (6+3)/2 = 4.5, mannsnorm 2.6
        Assert.False(manglerKontaktVerdier.Positiv); // høyere enn norm er DÅRLIG for en rigiditets-delskala

        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Fleksibilitet — global");
        Assert.Contains(resultat.Indikatorer!, i => i.Navn == "Rigiditet — global");
        Assert.Null(resultat.GyldighetsAdvarsel);
        Assert.True(resultat.SkjulProsent);
    }

    [Fact]
    public void Mpfi24_BrukerKvinnenormNaarBiologiskKjonnErKvinne()
    {
        var (alleLedd, svar) = Mpfi24Bygg(Enumerable.Repeat(4, 24).ToArray());

        var resultat = new Mpfi24Skaaringsberegner().BeregnSkaaringMedBiologiskKjonn(svar, alleLedd, BiologiskKjonn.Kvinne);

        var aksept = resultat.Indikatorer!.Single(i => i.Navn == "Fleksibilitet — Aksept");
        Assert.Equal("4.00/3.40", aksept.Verdi); // kvinnenorm 3.4, IKKE mannsnormen 3.5
    }
}
