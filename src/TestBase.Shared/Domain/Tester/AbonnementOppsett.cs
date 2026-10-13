namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Singleton-rad (alltid Id=1, opprettes lat — samme mønster som
/// AdminOkonomiOppsett/EktBankIdInnstilling) for alle tall- og
/// prosentkonstanter i de tre behandler-abonnementsmodellene (se
/// AbonnementModus/TestPrisberegner og docs/beslutningslogg.md "Abonnement/
/// subscription-system"). Superadmin-redigerbar — ALDRI hardkodet i
/// kalkulasjonskoden.
/// </summary>
public sealed class AbonnementOppsett
{
    public long Id { get; set; }

    /// <summary>Reell kostnad per SMS (lagt til pasientens pris i modus a, fakturert behandler i modus b).</summary>
    public decimal SmsKostnadKr { get; set; } = 1.5m;

    /// <summary>Reell kostnad per e-post (normalt neglisjerbar, men eksplisitt satt per brukerens spesifikasjon).</summary>
    public decimal EpostKostnadKr { get; set; } = 0.1m;

    /// <summary>Ekstra kostnad når automatisk påminnelse-SMS er påslått for utsendelsen.</summary>
    public decimal EkstraPaaminnelseSmsKostnadKr { get; set; } = 1.5m;

    /// <summary>Flatt betalingsgebyr (kort/Vipps-håndtering) lagt til pasientens pris i modus a — "2 kr" i brukerens spesifikasjon.</summary>
    public decimal BetalingsgebyrKr { get; set; } = 2m;

    /// <summary>a) Terapeut Gratis — prosentandel av behandlerens (+ evt. klinikkens) andel som trekkes til plattformen.</summary>
    public decimal TerapeutGratisProsent { get; set; } = 4.99m;

    /// <summary>b) Pasienten Gratis — flat månedsavgift for behandleren.</summary>
    public decimal PasientenGratisFlatKr { get; set; } = 149m;

    /// <summary>b) Pasienten Gratis — prosentandel av behandlerens eget honorar som trekkes til plattformen, i tillegg til flatbeløpet.</summary>
    public decimal PasientenGratisProsent { get; set; } = 3.99m;

    /// <summary>c) Forutsigbart — flat månedsavgift, inkluderer SMS/e-post-kostnad, ingen prosentkutt.</summary>
    public decimal ForutsigbartFlatKr { get; set; } = 299m;

    public long? SistEndretAvUserId { get; set; }
    public DateTimeOffset? SistEndretUtc { get; set; }
}
