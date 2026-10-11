namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// En partner — et selskap som lar sine egne behandlere bruke plattformen
/// (uten egen branding/embedding/pålogging i denne fasen, se
/// docs/beslutningslogg.md "Partner System + Test Monetization"). Opprettes
/// KUN av en Superadmin (se <see cref="Administrator.ErSuperadmin"/>).
/// Hvilke tester partnerens behandlere får tilgang til styres separat via
/// <see cref="PartnerTestTilgang"/>, kuratert av samme Superadmin.
/// </summary>
public sealed class Partner
{
    public long Id { get; set; }
    public required string Navn { get; set; }
    public string? KontaktpersonNavn { get; set; }
    public string? KontaktEpost { get; set; }
    public string? KontaktMobilNr { get; set; }

    /// <summary>Informasjonsfelt for partneroppgjøret (se "Oppsett"-fanen på MinPartner/Utbetaling.cshtml) — brukes IKKE av selve Stripe Connect-utbetalingen (se UtbetalingsMottakerKonto).</summary>
    public string? Organisasjonsnummer { get; set; }
    public string? Kontonummer { get; set; }

    /// <summary>Egen kontoplan-etikett/-konto for partnerandel i periodiserte oppgjør — faller tilbake til OkonomiOppsettStandard.PartnerandelInntekt når null/tom.</summary>
    public string? PartnerandelEtikett { get; set; }
    public string? PartnerandelKonto { get; set; }

    /// <summary>
    /// Dekker partnerens behandlere sine plattformkostnader (se
    /// TestPrisberegner) — selve den tilbakevendende faktureringsmotoren er
    /// bevisst IKKE bygget i denne fasen, kun denne tilstanden.
    /// </summary>
    public bool HarAktivtAbonnement { get; set; }

    public long OpprettetAvAdministratorId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
    public bool ErArkivert { get; set; }
    public DateTimeOffset? ArkivertUtc { get; set; }

    /// <summary>Andre steg etter arkivering — se Administrator.ErSlettet for full begrunnelse.</summary>
    public bool ErSlettet { get; set; }
    public DateTimeOffset? SlettetUtc { get; set; }
}
