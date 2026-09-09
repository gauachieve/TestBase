using TestBase.Shared.Domain.Tester;

namespace TestBase.IntegrationTests;

/// <summary>
/// Ren enhetstest av TestPrisberegner (ingen DB/HTTP nødvendig) — dekker
/// tilfellene nevnt i implementasjonsplanen for "Partner System + Test
/// Monetization": ingen partner, partner-gulv, partner eksplisitt andel,
/// abonnement, og maks-klemming.
/// </summary>
public sealed class TestPrisberegnerTests
{
    private static Test LagTest(decimal minstePris, decimal storstePris, decimal typiskHonorar, decimal minstePartnerAndel = 0m) =>
        new()
        {
            Navn = "Test",
            MinstePrisKr = minstePris,
            StorstePrisKr = storstePris,
            TypiskBehandlerHonorarKr = typiskHonorar,
            MinstePartnerAndelKr = minstePartnerAndel
        };

    [Fact]
    public void UkonfigurertTest_GirNullpris()
    {
        var test = LagTest(0, 0, 0);
        var resultat = new TestPrisberegner().Beregn(test, dekketAvAbonnement: false, onsketHonorarKr: null, effektivPartnerAndelKr: null);

        Assert.Equal(0m, resultat.PasientTotalprisKr);
        Assert.Equal(0m, resultat.BehandlerHonorarKr);
        Assert.Equal(0m, resultat.PlattformAndelKr);
        Assert.Null(resultat.PartnerAndelKr);
    }

    [Fact]
    public void IngenPartner_PlattformPlussHonorar()
    {
        var test = LagTest(minstePris: 100, storstePris: 500, typiskHonorar: 200);
        var resultat = new TestPrisberegner().Beregn(test, dekketAvAbonnement: false, onsketHonorarKr: null, effektivPartnerAndelKr: null);

        Assert.Equal(100m, resultat.PlattformAndelKr);
        Assert.Equal(200m, resultat.BehandlerHonorarKr);
        Assert.Equal(300m, resultat.PasientTotalprisKr);
        Assert.Null(resultat.PartnerAndelKr);
    }

    [Fact]
    public void MedPartner_LeggerTilPartnerandel()
    {
        var test = LagTest(minstePris: 100, storstePris: 1000, typiskHonorar: 200, minstePartnerAndel: 50);
        var resultat = new TestPrisberegner().Beregn(test, dekketAvAbonnement: false, onsketHonorarKr: null, effektivPartnerAndelKr: 50m);

        Assert.Equal(100m, resultat.PlattformAndelKr);
        Assert.Equal(50m, resultat.PartnerAndelKr);
        Assert.Equal(200m, resultat.BehandlerHonorarKr);
        Assert.Equal(350m, resultat.PasientTotalprisKr);
    }

    [Fact]
    public void PartnerEksplisittHoyereAndelEnnGulv_BrukesDirekte()
    {
        var test = LagTest(minstePris: 0, storstePris: 1000, typiskHonorar: 200, minstePartnerAndel: 50);
        var resultat = new TestPrisberegner().Beregn(test, dekketAvAbonnement: false, onsketHonorarKr: null, effektivPartnerAndelKr: 150m);

        Assert.Equal(150m, resultat.PartnerAndelKr);
        Assert.Equal(350m, resultat.PasientTotalprisKr);
    }

    [Fact]
    public void DekketAvAbonnement_FjernerPlattformAndel()
    {
        var test = LagTest(minstePris: 100, storstePris: 500, typiskHonorar: 200);
        var resultat = new TestPrisberegner().Beregn(test, dekketAvAbonnement: true, onsketHonorarKr: null, effektivPartnerAndelKr: null);

        Assert.Equal(0m, resultat.PlattformAndelKr);
        Assert.Equal(200m, resultat.BehandlerHonorarKr);
        Assert.Equal(200m, resultat.PasientTotalprisKr);
        Assert.True(resultat.DekketAvAbonnement);
    }

    [Fact]
    public void OnsketSumOverMaks_BehandlerensAndelAbsorberesNed()
    {
        var test = LagTest(minstePris: 100, storstePris: 400, typiskHonorar: 1000, minstePartnerAndel: 50);
        var resultat = new TestPrisberegner().Beregn(test, dekketAvAbonnement: false, onsketHonorarKr: null, effektivPartnerAndelKr: 50m);

        // Total klemmes til 400 (maks); plattform (100) og partner (50) er garanterte
        // gulv og beholdes uendret — behandlerens honorar tar hele kuttet.
        Assert.Equal(400m, resultat.PasientTotalprisKr);
        Assert.Equal(100m, resultat.PlattformAndelKr);
        Assert.Equal(50m, resultat.PartnerAndelKr);
        Assert.Equal(250m, resultat.BehandlerHonorarKr);
    }

    [Fact]
    public void OnsketSumUnderMinimum_KlemmesOpp()
    {
        var test = LagTest(minstePris: 300, storstePris: 1000, typiskHonorar: 0);
        var resultat = new TestPrisberegner().Beregn(test, dekketAvAbonnement: false, onsketHonorarKr: 0m, effektivPartnerAndelKr: null);

        Assert.Equal(300m, resultat.PasientTotalprisKr);
    }

    [Fact]
    public void GratisTestMedAbonnement_GirNullpris()
    {
        var test = LagTest(minstePris: 0, storstePris: 500, typiskHonorar: 0);
        var resultat = new TestPrisberegner().Beregn(test, dekketAvAbonnement: true, onsketHonorarKr: 0m, effektivPartnerAndelKr: null);

        Assert.Equal(0m, resultat.PasientTotalprisKr);
    }

    [Fact]
    public void SmsGebyr_LeggesOvenpaKlemtTotalOgGarPlattformen()
    {
        // Selv en maks-klemt test (400) skal få SMS-gebyret lagt OVENPÅ, ikke klemt
        // bort — gebyret er en kanal-kostnad, ikke en del av testens prisgrenser.
        var test = LagTest(minstePris: 100, storstePris: 400, typiskHonorar: 1000, minstePartnerAndel: 50);
        var resultat = new TestPrisberegner().Beregn(test, dekketAvAbonnement: false, onsketHonorarKr: null, effektivPartnerAndelKr: 50m, smsGebyrKr: 5m);

        Assert.Equal(405m, resultat.PasientTotalprisKr);
        Assert.Equal(105m, resultat.PlattformAndelKr);
        Assert.Equal(50m, resultat.PartnerAndelKr);
        Assert.Equal(250m, resultat.BehandlerHonorarKr);
        Assert.Equal(5m, resultat.SmsGebyrKr);
    }
}
