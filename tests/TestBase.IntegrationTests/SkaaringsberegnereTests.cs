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
}
