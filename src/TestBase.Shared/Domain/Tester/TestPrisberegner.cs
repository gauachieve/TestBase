namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Resultatet av en prisberegning for ÉN test til ÉN pasient — snapshottes
/// direkte inn i en <see cref="TestTildelingBetaling"/>, se
/// TestTildelingsService.TildelOgVarsleAsync.
/// </summary>
public sealed record PrisberegningResultat(
    decimal PasientTotalprisKr, decimal BehandlerHonorarKr, decimal PlattformAndelKr,
    decimal? PartnerAndelKr, bool DekketAvAbonnement, decimal SmsGebyrKr = 0m);

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
///
/// <paramref name="smsGebyrKr"/> (satt av kalleren når varslingsmetoden for
/// batchen inkluderer SMS, se TestTildelingsService) legges til OVENPÅ den
/// klemte totalprisen — det er et rent gebyr for selve varslingskanalen, ikke
/// en del av testens Min/Max-grenser, og går i sin helhet til plattformandelen.
/// </summary>
public sealed class TestPrisberegner
{
    public PrisberegningResultat Beregn(
        Test test, bool dekketAvAbonnement, decimal? onsketHonorarKr, decimal? effektivPartnerAndelKr, decimal smsGebyrKr = 0m)
    {
        var plattformAndel = dekketAvAbonnement ? 0m : test.MinstePrisKr;
        var partnerAndel = effektivPartnerAndelKr ?? 0m;
        var onsketTotal = plattformAndel + partnerAndel + (onsketHonorarKr ?? test.TypiskBehandlerHonorarKr);

        var storstePris = Math.Max(test.StorstePrisKr, test.MinstePrisKr);
        var totalKrUtenSms = Math.Clamp(onsketTotal, test.MinstePrisKr, storstePris);
        var behandlerHonorar = Math.Max(0m, totalKrUtenSms - plattformAndel - partnerAndel);

        return new PrisberegningResultat(
            totalKrUtenSms + smsGebyrKr, behandlerHonorar, plattformAndel + smsGebyrKr,
            effektivPartnerAndelKr.HasValue ? partnerAndel : null, dekketAvAbonnement, smsGebyrKr);
    }
}
