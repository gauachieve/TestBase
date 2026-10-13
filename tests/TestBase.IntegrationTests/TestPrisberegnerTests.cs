using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Domain.Tester;

namespace TestBase.IntegrationTests;

/// <summary>
/// Ren enhetstest av TestPrisberegner (ingen DB/HTTP nødvendig) — dekker
/// de tre abonnementsmodi-formlene (se docs/beslutningslogg.md "Abonnement/
/// subscription-system"), klinikk-%, og maks-/min-klemming. Erstatter de
/// eldre testene for den nå utfasede "plattform=MinstePrisKr uansett
/// modus"-formelen fra "Partner System + Test Monetization".
/// </summary>
public sealed class TestPrisberegnerTests
{
    private static Test LagTest(decimal minstePris, decimal storstePris, decimal typiskHonorar) =>
        new()
        {
            Navn = "Test",
            MinstePrisKr = minstePris,
            StorstePrisKr = storstePris,
            TypiskBehandlerHonorarKr = typiskHonorar
        };

    private static AbonnementOppsett LagOppsett(decimal betalingsgebyrKr = 0m, decimal terapeutGratisProsent = 0m) =>
        new() { BetalingsgebyrKr = betalingsgebyrKr, TerapeutGratisProsent = terapeutGratisProsent };

    [Fact]
    public void UkonfigurertTest_GirNullpris()
    {
        var test = LagTest(0, 0, 0);
        var resultat = new TestPrisberegner().Beregn(test, AbonnementModus.TerapeutGratis, LagOppsett(), onsketHonorarKr: null, effektivPartnerAndelProsent: null);

        Assert.Equal(0m, resultat.PasientTotalprisKr);
        Assert.Equal(0m, resultat.BehandlerHonorarKr);
        Assert.Equal(0m, resultat.PlattformAndelKr);
        Assert.Null(resultat.PartnerAndelKr);
    }

    [Fact]
    public void TerapeutGratis_PlattformAndelErGebyrPlussProsentAvHonorar()
    {
        var test = LagTest(minstePris: 0, storstePris: 1000, typiskHonorar: 200);
        var oppsett = LagOppsett(betalingsgebyrKr: 10m, terapeutGratisProsent: 10m);
        var resultat = new TestPrisberegner().Beregn(test, AbonnementModus.TerapeutGratis, oppsett, onsketHonorarKr: null, effektivPartnerAndelProsent: null);

        // 10 kr flatt + 10 % av 200 kr honorar = 30 kr plattformandel.
        Assert.Equal(30m, resultat.PlattformAndelKr);
        Assert.Equal(200m, resultat.BehandlerHonorarKr);
        Assert.Equal(230m, resultat.PasientTotalprisKr);
        Assert.False(resultat.DekketAvAbonnement);
    }

    [Theory]
    [InlineData(AbonnementModus.PasientenGratis)]
    [InlineData(AbonnementModus.Forutsigbart)]
    public void DekketAvAbonnement_FjernerPlattformAndel(AbonnementModus modus)
    {
        var test = LagTest(minstePris: 100, storstePris: 500, typiskHonorar: 200);
        var oppsett = LagOppsett(betalingsgebyrKr: 10m, terapeutGratisProsent: 10m);
        var resultat = new TestPrisberegner().Beregn(test, modus, oppsett, onsketHonorarKr: null, effektivPartnerAndelProsent: null);

        Assert.Equal(0m, resultat.PlattformAndelKr);
        Assert.Equal(200m, resultat.BehandlerHonorarKr);
        Assert.Equal(200m, resultat.PasientTotalprisKr);
        Assert.True(resultat.DekketAvAbonnement);
    }

    [Fact]
    public void MedPartner_LeggerTilPartnerandelSomProsentAvSubtotal()
    {
        var test = LagTest(minstePris: 0, storstePris: 1000, typiskHonorar: 200);
        var resultat = new TestPrisberegner().Beregn(test, AbonnementModus.TerapeutGratis, LagOppsett(), onsketHonorarKr: null, effektivPartnerAndelProsent: 10m);

        // 10 % av (plattform 0 + honorar 200) = 20 kr.
        Assert.Equal(20m, resultat.PartnerAndelKr);
        Assert.Equal(200m, resultat.BehandlerHonorarKr);
        Assert.Equal(220m, resultat.PasientTotalprisKr);
    }

    [Fact]
    public void OnsketSumOverMaks_BehandlerensAndelAbsorberesNed()
    {
        var test = LagTest(minstePris: 100, storstePris: 400, typiskHonorar: 1000);
        var resultat = new TestPrisberegner().Beregn(test, AbonnementModus.TerapeutGratis, LagOppsett(), onsketHonorarKr: null, effektivPartnerAndelProsent: 10m);

        // Plattform 0, partner 10 % av (0+1000)=100 — begge er garanterte gulv.
        // Total klemmes til 400 (maks); behandlerens honorar tar hele kuttet.
        Assert.Equal(400m, resultat.PasientTotalprisKr);
        Assert.Equal(0m, resultat.PlattformAndelKr);
        Assert.Equal(100m, resultat.PartnerAndelKr);
        Assert.Equal(300m, resultat.BehandlerHonorarKr);
    }

    [Fact]
    public void OnsketSumUnderMinimum_KlemmesOpp()
    {
        var test = LagTest(minstePris: 300, storstePris: 1000, typiskHonorar: 0);
        var resultat = new TestPrisberegner().Beregn(test, AbonnementModus.TerapeutGratis, LagOppsett(), onsketHonorarKr: 0m, effektivPartnerAndelProsent: null);

        Assert.Equal(300m, resultat.PasientTotalprisKr);
    }

    [Fact]
    public void GratisTestMedAbonnement_GirNullpris()
    {
        var test = LagTest(minstePris: 0, storstePris: 500, typiskHonorar: 0);
        var resultat = new TestPrisberegner().Beregn(test, AbonnementModus.PasientenGratis, LagOppsett(), onsketHonorarKr: 0m, effektivPartnerAndelProsent: null);

        Assert.Equal(0m, resultat.PasientTotalprisKr);
    }

    [Fact]
    public void SmsGebyr_TerapeutGratis_LeggesOvenpaKlemtTotalOgGarPlattformen()
    {
        // Selv en maks-klemt test (400) skal få SMS-gebyret lagt OVENPÅ, ikke klemt
        // bort — gebyret er en kanal-kostnad, ikke en del av testens prisgrenser.
        var test = LagTest(minstePris: 100, storstePris: 400, typiskHonorar: 1000);
        var resultat = new TestPrisberegner().Beregn(
            test, AbonnementModus.TerapeutGratis, LagOppsett(), onsketHonorarKr: null, effektivPartnerAndelProsent: 10m, smsGebyrKr: 5m);

        Assert.Equal(405m, resultat.PasientTotalprisKr);
        Assert.Equal(5m, resultat.PlattformAndelKr); // plattform 0 + sms 5
        Assert.Equal(100m, resultat.PartnerAndelKr); // 10 % av (0+1000), klemt-gulv, uendret av smsen
        Assert.Equal(300m, resultat.BehandlerHonorarKr);
        Assert.Equal(5m, resultat.SmsGebyrKr);
    }

    [Fact]
    public void SmsGebyr_PasientenGratis_FaktureresIKKEPasienten()
    {
        // b)/c): behandleren faktureres reell SMS-kostnad separat (ikke bygget
        // ennå, se beslutningsloggen) — pasienten skal ALDRI se SMS-gebyret her.
        var test = LagTest(minstePris: 0, storstePris: 1000, typiskHonorar: 200);
        var resultat = new TestPrisberegner().Beregn(
            test, AbonnementModus.PasientenGratis, LagOppsett(), onsketHonorarKr: null, effektivPartnerAndelProsent: null, smsGebyrKr: 5m);

        Assert.Equal(200m, resultat.PasientTotalprisKr);
        Assert.Equal(0m, resultat.PlattformAndelKr);
        Assert.Equal(0m, resultat.SmsGebyrKr);
    }
}
