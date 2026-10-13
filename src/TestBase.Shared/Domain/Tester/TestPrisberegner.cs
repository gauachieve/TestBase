using TestBase.Shared.Domain.Administrasjon;

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
/// Ren, tilstandsløs prisberegner — se docs/beslutningslogg.md "Abonnement/
/// subscription-system" for hele resonnementet bak de tre modus-formlene
/// (tidligere versjon hadde ÉN flat plattformandel uansett modus, se
/// "Partner System + Test Monetization" for den opprinnelige, nå utfasede
/// formelen). Kaller (ikke denne klassen) slår opp behandler/partner-tilstand
/// og den EFFEKTIVE modusen FØR kallet (se TestTildelingsService), siden det
/// krever databasetilgang — klinikk-tilknyttede behandlere er alltid
/// "dekket" (ingen plattformandel på selve testen, klinikken betaler
/// sete-basert i stedet, se KlinikkPrisService), uavhengig av egen
/// AbonnementModus.
///
/// Test sine Min/Max-grenser er grenser på PASIENTENS TOTALPRIS (ikke bare
/// behandlerens honorar) — MinstePrisKr=0 er hvordan en test markeres "kan
/// tilbys gratis". Overskrider ønsket sum StorstePrisKr, er det ALLTID
/// behandlerens eget honorar som reduseres — plattform- og partnerandelen er
/// garanterte gulv som aldri kuttes.
///
/// <paramref name="smsGebyrKr"/> (satt av kalleren når varslingsmetoden for
/// batchen inkluderer SMS, se TestTildelingsService) legges KUN til pasientens
/// pris i modus a) Terapeut Gratis — i b)/c) faktureres behandleren for reell
/// SMS-kostnad separat (ikke bygget ennå, se beslutningsloggen), så pasienten
/// skal ikke dobbeltbetale for den.
/// </summary>
public sealed class TestPrisberegner
{
    public PrisberegningResultat Beregn(
        Test test, AbonnementModus effektivModus, AbonnementOppsett oppsett,
        decimal? onsketHonorarKr, decimal? effektivPartnerAndelProsent, decimal smsGebyrKr = 0m)
    {
        var dekketAvAbonnement = effektivModus != AbonnementModus.TerapeutGratis;
        var honorarBase = onsketHonorarKr ?? test.TypiskBehandlerHonorarKr;

        // a) Terapeut Gratis: pasienten betaler plattformens kutt (flatt
        // betalingsgebyr + en prosentandel av HONORARET, se AbonnementOppsett).
        // b)/c): ingen plattformandel på selve testen — behandleren betaler i
        // stedet sin flate månedsavgift (+ evt. prosent av honorar for b), se
        // StripeAbonnementService (kommer i senere fase).
        var plattformAndel = dekketAvAbonnement
            ? 0m
            : oppsett.BetalingsgebyrKr + Math.Round(honorarBase * oppsett.TerapeutGratisProsent / 100m, 2);

        // Klinikk-%-en (erstatter den gamle flate klinikk-kr-per-test) er en
        // prosentandel av det pasienten ELLERS ville betalt (plattformandel +
        // honorar) — lagt TIL OPPÅ, ikke en sirkulær andel av den endelige
        // totalsummen (som ville krevd å løse total for seg selv).
        var partnerAndelKr = effektivPartnerAndelProsent is { } pct
            ? Math.Round((plattformAndel + honorarBase) * pct / 100m, 2)
            : 0m;

        var onsketTotal = plattformAndel + partnerAndelKr + honorarBase;
        var storstePris = Math.Max(test.StorstePrisKr, test.MinstePrisKr);
        var totalKrUtenSms = Math.Clamp(onsketTotal, test.MinstePrisKr, storstePris);
        var behandlerHonorar = Math.Max(0m, totalKrUtenSms - plattformAndel - partnerAndelKr);

        var smsGebyrPatientKr = dekketAvAbonnement ? 0m : smsGebyrKr;

        return new PrisberegningResultat(
            totalKrUtenSms + smsGebyrPatientKr, behandlerHonorar, plattformAndel + smsGebyrPatientKr,
            effektivPartnerAndelProsent.HasValue ? partnerAndelKr : null, dekketAvAbonnement, smsGebyrPatientKr);
    }
}
