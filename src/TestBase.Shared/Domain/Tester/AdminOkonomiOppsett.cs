namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Singleton-rad (alltid Id=1, opprettes lat — samme mønster som
/// EktBankIdInnstilling) for firmaets egne regnskapsopplysninger og
/// kontoplan, vist/redigert i "Oppsett"-fanen på Admin/Okonomi/Index.cshtml.
/// Rene informasjonsfelt for de periodiserte oppgjørsrapportene (se
/// OkonomiRapportService) — påvirker IKKE selve Pengebevegelse-loggen eller
/// noen faktisk pengeoverføring.
/// </summary>
public sealed class AdminOkonomiOppsett
{
    public long Id { get; set; }
    public string? Organisasjonsnummer { get; set; }
    public string? HprNummerFirma { get; set; }

    public string? SalgsinntektEtikett { get; set; }
    public string? SalgsinntektKonto { get; set; }
    public string? BetalingsgebyrEtikett { get; set; }
    public string? BetalingsgebyrKonto { get; set; }
    public string? BehandlerHonorarEtikett { get; set; }
    public string? BehandlerHonorarKonto { get; set; }
    public string? PartnerprovisjonEtikett { get; set; }
    public string? PartnerprovisjonKonto { get; set; }

    public string? UtbetalingKontonummer { get; set; }

    public long? SistEndretAvUserId { get; set; }
    public DateTimeOffset? SistEndretUtc { get; set; }
}

/// <summary>Standardverdier (fra designforslaget "Periodiserte oppgjør", 2026-10-10) brukt når et felt over/på Behandler/Partner ikke er satt — se docs/beslutningslogg.md.</summary>
public static class OkonomiOppsettStandard
{
    public const string SalgsinntektEtikett = "Salgsinntekt tjenester, unntatt mva";
    public const string SalgsinntektKonto = "3100";
    public const string BetalingsgebyrEtikett = "Betalingsgebyr (Stripe/Vipps)";
    public const string BetalingsgebyrKonto = "7770";
    public const string BehandlerHonorarEtikett = "Behandlerhonorar";
    public const string BehandlerHonorarKonto = "6560";
    public const string PartnerprovisjonEtikett = "Partnerprovisjon";
    public const string PartnerprovisjonKonto = "6960";

    public const string BehandlerHonorarinntektEtikett = "Honorarinntekt — salg av helsetjeneste, unntatt mva";
    public const string BehandlerHonorarinntektKonto = "3100";

    public const string PartnerandelInntektEtikett = "Partnerandel — salg av helsetjeneste, unntatt mva";
    public const string PartnerandelInntektKonto = "3100";
}
