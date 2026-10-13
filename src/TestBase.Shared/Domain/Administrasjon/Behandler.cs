using TestBase.Shared.Domain.Pasienter;

namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// En behandler, jf. Del 2/3 i kravdokumentet. Opprettes av administrator ELLER en annen
/// behandler med kun kontaktinfo (status <see cref="BehandlerStatus.Invitert"/>) — nøyaktig én
/// av <see cref="InvitertAvAdministratorId"/>/<see cref="InvitertAvBehandlerId"/> er satt (se
/// BehandlerInvitasjonService). Resten av feltene fylles inn av behandleren selv via
/// invitasjonslenken. <see cref="Status"/> blir <see cref="BehandlerStatus.Aktiv"/> først når
/// egenregistrering, brukeravtale-aksept og kontaktverifisering (e-post + mobil) er fullført —
/// se BehandlerInvitasjonService.BekreftKontaktAsync.
/// </summary>
public sealed class Behandler
{
    public long Id { get; set; }
    public required string MobilNr { get; set; }
    public required string Email { get; set; }
    public string? Fornavn { get; set; }
    public string? Etternavn { get; set; }

    /// <summary>Lagres kryptert i databasen via AppDbContext — se derfor aldri ubehandlet i logger.</summary>
    public string? Personnummer { get; set; }

    public string? HprNr { get; set; }
    public string? Kontonummer { get; set; }
    public string? Arbeidsadresse { get; set; }
    public string? Tittel { get; set; }

    /// <summary>
    /// Egen kontoplan-etikett/-konto for honorarinntekt i periodiserte oppgjør
    /// (se Behandlerportal/Utbetaling.cshtml sin "Oppsett"-fane) — faller
    /// tilbake til OkonomiOppsettStandard.BehandlerHonorarinntekt når null/tom.
    /// </summary>
    public string? HonorarinntektEtikett { get; set; }
    public string? HonorarinntektKonto { get; set; }

    public bool HprGodkjent { get; set; }
    public DateTimeOffset? HprGodkjentUtc { get; set; }
    public long? HprGodkjentAvAdministratorId { get; set; }

    /// <summary>
    /// Satt av admin via "Utvid fristen med 21 dager" (kan kun brukes ÉN gang —
    /// se HprPolicy.BeregnFrist/OnPostForlengHprAsync), typisk brukt ved ferie.
    /// </summary>
    public DateTimeOffset? HprForlengetTilUtc { get; set; }

    /// <summary>Når egenregistrering (skjema + avtale + verifisering) ble fullført — starter HprPolicy sin prøveperiode.</summary>
    public DateTimeOffset? RegistrertUtc { get; set; }
    public DateTimeOffset? EpostVerifisertUtc { get; set; }
    public DateTimeOffset? MobilVerifisertUtc { get; set; }

    public int? BrukeravtaleGodkjentVersjon { get; set; }
    public DateTimeOffset? BrukeravtaleGodkjentUtc { get; set; }

    public BehandlerStatus Status { get; set; } = BehandlerStatus.Invitert;

    public long? InvitertAvAdministratorId { get; set; }
    public long? InvitertAvBehandlerId { get; set; }

    public DateTimeOffset OpprettetUtc { get; set; }
    public DateTimeOffset? ArkivertUtc { get; set; }

    /// <summary>Andre steg etter arkivering (Status == Arkivert) — se Administrator.ErSlettet for full begrunnelse.</summary>
    public bool ErSlettet { get; set; }
    public DateTimeOffset? SlettetUtc { get; set; }

    /// <summary>
    /// Null = uavhengig behandler (dagens oppførsel for alle, uendret). Satt
    /// = tilknyttet en Partner — se docs/beslutningslogg.md "Partner System +
    /// Test Monetization". Kun en Superadmin kan sette denne (Admin/Partnere).
    /// </summary>
    public long? PartnerId { get; set; }

    /// <summary>
    /// Kun meningsfylt når <see cref="PartnerId"/> er satt — gir rett til å
    /// legge til/fjerne kolleger INNENFOR samme partner (PartnerAdminOmrade),
    /// og til å sette partnerens egen andel per test (PartnerTestAndel).
    /// Invarianten (kun relevant sammen med PartnerId) håndheves i
    /// tjenestelaget, ikke i databasen — samme mønster som "nøyaktig én av
    /// InvitertAv*" over.
    /// </summary>
    public bool ErPartnerAdministrator { get; set; }

    /// <summary>
    /// Hvilken av de tre abonnementsmodellene (se docs/beslutningslogg.md
    /// "Abonnement/subscription-system") som gjelder for denne uavhengige
    /// behandleren — kun meningsfylt når <see cref="HarKortLagret"/> er sant;
    /// uten kort er behandleren alltid i "test-modus" uansett denne verdien.
    /// Default (også for nye rader) er <see cref="AbonnementModus.PasientenGratis"/>,
    /// siden det er modusen som skal gjelde i prøveperioden før behandleren
    /// aktivt velger (brukerbeslutning).
    /// </summary>
    public AbonnementModus AbonnementModus { get; set; } = AbonnementModus.PasientenGratis;

    /// <summary>
    /// Sant KUN for behandlere opprettet via den offentlige selvregistrerings-
    /// siden (Pages/BliBehandler — se BehandlerInvitasjonService.InviterAsync,
    /// satt når BÅDE InvitertAvAdministratorId/InvitertAvBehandlerId er null).
    /// Avgjør om "test-modus"-sperren i TestTildelingsService i det hele tatt
    /// er relevant — en admin/kollega-INVITERT behandler var alltid fullt
    /// funksjonell FØR denne funksjonen fantes, og skal IKKE retroaktivt
    /// låses til test-modus bare fordi HarKortLagret defaulter til false.
    /// Default false for ALLE eksisterende rader ved migrering.
    /// </summary>
    public bool OpprettetViaSelvregistrering { get; set; }

    /// <summary>
    /// Sant når behandleren har lagret et betalingskort hos Stripe (via
    /// StripeAbonnementService) — IKKE belastet ved selve lagringen (Stripe
    /// SetupIntent). Uten kort kan behandleren bruke alt i systemet, men kun
    /// sende tester til seg selv ("test-modus") — se
    /// TestTildelingsService.TildelOgVarsleAsync.
    /// </summary>
    public bool HarKortLagret { get; set; }

    /// <summary>Stripe-kunde-ID, satt når kortet lagres. Null før kort er lagret.</summary>
    public string? StripeKundeId { get; set; }

    /// <summary>
    /// Stripe-abonnement-ID for den månedlige flate avgiften — kun satt for
    /// <see cref="AbonnementModus.PasientenGratis"/>/<see cref="AbonnementModus.Forutsigbart"/>
    /// (Terapeut Gratis har ingen flat månedsavgift, se TestPrisberegner).
    /// </summary>
    public string? StripeAbonnementId { get; set; }

    /// <summary>
    /// Satt til NÅ+1 måned når kortet lagres første gang — den flate
    /// månedsavgiften (modus b/c) belastes ikke før denne er passert
    /// ("gratis måned", håndtert av Stripe sin egen trial_end).
    /// </summary>
    public DateTimeOffset? ProveperiodeSluttUtc { get; set; }

    /// <summary>Daglig påminnelse om ugodkjente fullførte rapporter — se PaaminnelseService, satt under Behandlerportal/Innstillinger.</summary>
    public bool OnskerDagligPaaminnelse { get; set; }
    public Varslingspreferanse PaaminnelseKanal { get; set; } = Varslingspreferanse.Begge;
    public DateTimeOffset? SistPaaminnetUtc { get; set; }

    public string? Visningsnavn => Fornavn is null && Etternavn is null ? null : $"{Fornavn} {Etternavn}".Trim();

    /// <summary>
    /// Token bak behandlerens generelle (ikke gruppespesifikke) QR-invitasjon
    /// — se GruppeService/PasientInvitasjonService og
    /// docs/beslutningslogg.md "Invitasjons- og gruppesystem, fase 2". Null
    /// inntil behandler første gang åpner "Min side" og en genereres
    /// automatisk. Kan regenereres (invaliderer forrige QR-kode/lenke).
    /// </summary>
    public string? PasientInviteQrToken { get; set; }

    /// <summary>Se Administrator.BankIdSubjekt — identisk begrunnelse/mønster, samme kobling-flyt.</summary>
    public string? BankIdSubjekt { get; set; }
}
