namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Resultatet av en prisberegning for ÉN test til ÉN pasient — snapshottes
/// direkte inn i en <see cref="TestTildelingBetaling"/>, se
/// TestTildelingsService.TildelOgVarsleAsync.
/// </summary>
public sealed record PrisberegningResultat(
    decimal PasientTotalprisKr, decimal BehandlerHonorarKr, decimal PlattformAndelKr,
    decimal? PartnerAndelKr, bool DekketAvAbonnement);

/// <summary>
/// Ren, tilstandsløs prisberegner — se docs/beslutningslogg.md "Partner System +
/// Test Monetization" for hele resonnementet bak formelen. Kaller (ikke denne
/// klassen) slår opp behandler/partner/abonnement-tilstand og partnerens
/// faktiske andel FØR kallet, siden det krever databasetilgang.
///
/// Test sine Min/Max-grenser er grenser på PASIENTENS TOTALPRIS (ikke bare
/// behandlerens honorar) — MinstePrisKr=0 er hvordan en test markeres "kan
/// tilbys gratis". Overskrider ønsket sum StorstePrisKr, er det ALLTID
/// behandlerens eget honorar som reduseres — plattform- og partnerandelen er
/// garanterte gulv som aldri kuttes.
/// </summary>
public sealed class TestPrisberegner
{
    public PrisberegningResultat Beregn(
        Test test, bool dekketAvAbonnement, decimal? onsketHonorarKr, decimal? effektivPartnerAndelKr)
    {
        var plattformAndel = dekketAvAbonnement ? 0m : test.MinstePrisKr;
        var partnerAndel = effektivPartnerAndelKr ?? 0m;
        var onsketTotal = plattformAndel + partnerAndel + (onsketHonorarKr ?? test.TypiskBehandlerHonorarKr);

        var storstePris = Math.Max(test.StorstePrisKr, test.MinstePrisKr);
        var totalKr = Math.Clamp(onsketTotal, test.MinstePrisKr, storstePris);
        var behandlerHonorar = Math.Max(0m, totalKr - plattformAndel - partnerAndel);

        return new PrisberegningResultat(
            totalKr, behandlerHonorar, plattformAndel,
            effektivPartnerAndelKr.HasValue ? partnerAndel : null, dekketAvAbonnement);
    }
}
