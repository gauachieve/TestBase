namespace TestBase.Shared.Domain.Tilbakemeldinger;

/// <summary>
/// Status på en innsendt tilbakemelding — satt av admin (manuelt, se
/// TilbakemeldingerController/Index) eller av den daglige rapport-agenten
/// (AgentNotat + evt. status-endring når en krasjrapport er fikset og
/// deployet, se docs/beslutningslogg.md "Tilbakemeldingsverktøy").
/// </summary>
public enum TilbakemeldingStatus
{
    Ny,
    Sett,
    UnderArbeid,
    Lost,
    Avvist
}

/// <summary>
/// Én innsendt tilbakemelding fra widgeten (se wwwroot/js/tilbakemelding-widget.js).
/// Bevisst IKKE personnummer-kryptert som resten av appen (se AppDbContext) — feltene
/// her er tekniske (URL/nettleser/skjermstørrelse) og selvrapportert fritekst, ikke
/// helseopplysninger i seg selv. MERK likevel: <see cref="ScreenshotBase64"/> kan
/// INNEHOLDE helseopplysninger/PII hvis avsenderen hadde en pasients data på skjermen —
/// se derfor kommentaren på AdminOmrade-policyen på Tilbakemeldinger-siden (kun admin/
/// superadmin kan lese denne tabellen, akkurat som pasientdata ellers).
/// </summary>
public sealed class Tilbakemelding
{
    public long Id { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }

    public required string Melding { get; set; }

    // --- Auto-innsamlet teknisk kontekst (se widgetens innsamlingSkjema()) ---
    public string? Url { get; set; }
    public string? BrukerAgent { get; set; }
    public int? SkjermBredde { get; set; }
    public int? SkjermHoyde { get; set; }
    public int? VindaugBredde { get; set; }
    public int? VindaugHoyde { get; set; }

    /// <summary>"Administrator"/"Behandler"/"Pasient"/"Utvikler"/null (ikke innlogget) — se ICurrentUserContext.</summary>
    public string? InnloggetRolle { get; set; }
    public long? InnloggetBrukerId { get; set; }

    /// <summary>
    /// Klientside JS-feil (window.onerror/unhandledrejection) fanget FØR skjemaet ble
    /// åpnet, eller en serverside 500-markør fra Pages/Error.cshtml — null hvis ingen.
    /// Tilstedeværelse av denne (ELLER at avsenderen selv krysset av "det er en feil/krasj")
    /// setter <see cref="ErKrasjRapport"/>, som er det den daglige agenten prioriterer.
    /// </summary>
    public string? TekniskFeilInfo { get; set; }
    public bool ErKrasjRapport { get; set; }

    /// <summary>Data-URL (data:image/png;base64,...) fra html2canvas — best-effort, kan mangle. Se klassedoc.</summary>
    public string? ScreenshotDataUrl { get; set; }

    public TilbakemeldingStatus Status { get; set; } = TilbakemeldingStatus.Ny;

    /// <summary>Fritekst-notat agenten/admin legger til (analyse, foreslått løsning, hva som ble gjort).</summary>
    public string? Notat { get; set; }

    public DateTimeOffset? SistOppdatertUtc { get; set; }
}
