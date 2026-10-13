namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// De tre selvbetjente abonnementsmodellene for en uavhengig behandler (ikke
/// tilknyttet en klinikk/Partner, som i stedet betaler sete-basert — se
/// KlinikkSeterTier) — se docs/beslutningslogg.md "Abonnement/subscription-
/// system" for hele resonnementet og formlene. Kun meningsfylt når
/// <see cref="Behandler.HarKortLagret"/> er sant.
/// </summary>
public enum AbonnementModus
{
    /// <summary>
    /// a) Terapeut Gratis — ingen flat månedsavgift. Pasienten betaler alt:
    /// SMS/e-post-kostnad + evt. ekstra påminnelse-SMS + et fast betalingsgebyr
    /// + en prosentandel av behandlerens honorar — se AbonnementOppsett.
    /// </summary>
    TerapeutGratis,

    /// <summary>
    /// b) Pasienten Gratis — ingen plattformandel på selve testen (som ved
    /// gammel HarEgetAbonnement=true). Behandleren betaler en flat månedsavgift
    /// + en prosentandel av eget honorar + reell SMS-kostnad. Default-modus i
    /// prøveperioden før behandleren aktivt velger (brukerbeslutning).
    /// </summary>
    PasientenGratis,

    /// <summary>
    /// c) Forutsigbart — behandleren betaler KUN en flat månedsavgift (inkl.
    /// SMS/e-post-kostnad). Ingen plattformandel på testen, ingen prosentkutt.
    /// </summary>
    Forutsigbart
}
