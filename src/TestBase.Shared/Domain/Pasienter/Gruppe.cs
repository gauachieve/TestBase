namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// En behandlers pasientgruppe — FØR 2026-09-20 var dette bare en fritekst-
/// streng (<c>Pasient.Gruppenavn</c>), utvidet til en egen entitet for å
/// støtte gruppe-spesifikke QR-invitasjoner (fase 2), automatisk test-
/// tildeling ved gruppeinnmelding (<see cref="GruppeTestTilordning"/>), og
/// aggregert rapportering på tvers av gruppens pasienter (fase 4). Se
/// docs/beslutningslogg.md "Invitasjons- og gruppesystem".
/// </summary>
public sealed class Gruppe
{
    public long Id { get; set; }
    public required string Navn { get; set; }
    public long BehandlerId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
    public bool ErArkivert { get; set; }
    public DateTimeOffset? ArkivertUtc { get; set; }

    /// <summary>Rent informasjonsfelt (ingen automatikk knyttet til dem ennå) — satt valgfritt ved opprettelse, redigerbare senere.</summary>
    public DateOnly? StartDato { get; set; }
    public DateOnly? SluttDato { get; set; }

    /// <summary>
    /// Unik, ugjettbar token brukt i QR-kodens URL (se GruppeService.RegenererQrTokenAsync).
    /// Kan regenereres av behandler hvis den anses kompromittert — invaliderer
    /// da automatisk den gamle QR-koden/lenken.
    /// </summary>
    public required string QrToken { get; set; }
}
