namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Singleton-rad (alltid Id=1) med tre uavhengige driftsbrytere Superadmin/Utvikler kan slå av/på
/// fra Admin/MinSide UTEN redeploy — samme mønster som <see cref="EktBankIdInnstilling"/>, se
/// docs/beslutningslogg.md "Superadmin-driftsbrytere for SMS/betaling/utbetaling". Hvert miljø
/// (lokal dev, beta, live) har sin EGEN rad i sin EGEN database — det finnes ingen tverr-miljø-
/// styring herfra, se CLAUDE.md "Arbeidsøkonomi"/beslutningsloggen for den vurderingen.
///
/// Alle tre defaulter til `true` (ekte leverandør brukes hvis konfigurert, ellers faller koden
/// naturlig tilbake til mock akkurat som i dag) — SAMME prinsipp som <see cref="EktBankIdInnstilling"/>:
/// en default på `false` ville gitt en overraskende regresjon til mock på et miljø som allerede
/// har ekte leverandører konfigurert og i bruk (f.eks. SMS på live). En Superadmin som faktisk vil
/// TESTE mock-fallback må slå bryteren av selv.
///
/// `BetalingErAktiv` gjelder kun selve PASIENTBETALINGEN (Vipps + Stripe betalingsintensjon).
/// `UtbetalingErAktiv` gjelder KUN Stripe Connect-utbetalingsveien (kontoopprettelse/onboarding/
/// faktisk overføring) — bevisst EGEN bryter, ikke samme som betaling, siden brukeren ba om
/// uavhengig kontroll over disse to (se bugs_features.txt-punkt 1c/1d).
/// </summary>
public sealed class DriftsbryterInnstilling
{
    public long Id { get; set; }
    public bool SmsErAktiv { get; set; } = true;
    public bool BetalingErAktiv { get; set; } = true;
    public bool UtbetalingErAktiv { get; set; } = true;

    /// <summary>
    /// Kritisk-varsling via Pushover (brukerbeslutning 2026-10-13, Tier 1-valg — se
    /// DriftsstatusVarslingBakgrunnstjeneste i TestBase.Web). <see cref="KritiskVarslingPushoverBrukerKey"/>
    /// er mottakerens personlige Pushover "user key" (fra pushover.net, ETT felt per miljø — ikke
    /// et mobilnummer), appens eget "app token" er delt Key Vault/appsettings-konfigurasjon
    /// ("Pushover:AppToken"), se docs/beslutningslogg.md "Driftsstatus-dashboard".
    /// </summary>
    public bool KritiskVarslingErAktiv { get; set; }
    public string? KritiskVarslingPushoverBrukerKey { get; set; }

    /// <summary>
    /// MDR-gating (2026-10-17, se docs/mdr-klassifiseringsvurdering-iib-begrunnelse.pdf og
    /// docs/beslutningslogg.md "MDR-klassifisering"): TestBase sin automatiske skårings-/
    /// tolkningsfunksjon er vurdert til å kvalifisere som medisinsk utstyr klasse IIb under
    /// MDR, og er IKKE CE-merket. Den kan derfor IKKE vises til en EKTE, identifiserbar
    /// pasient (Pasient.Personnummer != null) før CE-merking eller en godkjent klinisk
    /// utprøving — se SkaaringVisningsService. I MOTSETNING TIL de tre bryterne over
    /// defaulter denne til `false`, ikke `true`: standarden for noe regulatorisk uavklart
    /// skal være AV til noen bevisst slår det PÅ, ikke omvendt (de andre bryterne er `true`
    /// fordi allerede-konfigurerte, lovlige leverandører ikke skal regressere til mock
    /// overraskende). Prøvepasienter (Personnummer == null, se
    /// GruppeService.TellProvedataAsync) ser ALLTID skåring uansett denne bryterens stilling,
    /// siden de ikke er en ekte, identifiserbar person.
    /// </summary>
    public bool AutomatiskSkaaringErAktiv { get; set; }

    public long? SistEndretAvUserId { get; set; }
    public DateTimeOffset? SistEndretUtc { get; set; }
}
