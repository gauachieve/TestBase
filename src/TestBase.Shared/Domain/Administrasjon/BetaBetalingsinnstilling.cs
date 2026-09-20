namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>Vipps kan kjøre mot Vipps sitt EGET testmiljø (apitest.vipps.no, egen test-merchant-avtale) eller mot den EKTE produksjonskontoen (samme som live) — se docs/beslutningslogg.md "Beta-miljø".</summary>
public enum VippsDriftsmodus
{
    Mock,
    Test,
    Produksjon
}

/// <summary>Stripe har ingen tilsvarende "produksjon"-fare her — testnøklene ER trygge ekte API-kall, ingen ekte penger. Kun mock/test.</summary>
public enum StripeDriftsmodus
{
    Mock,
    Test
}

/// <summary>
/// Singleton-rad (alltid Id=1) som styrer hvilken betalingsleverandør-modus
/// beta-miljøet faktisk bruker akkurat nå — lest per kall av
/// BetaSwitchingVippsClient/BetaSwitchingStripeClient, IKKE cachet ved
/// oppstart, slik at en admin/superadmin kan endre den uten omstart. Finnes
/// (og er ufarlig) i ALLE miljøer, men UI-en for å endre den og selve
/// switching-oppførselen er kun aktiv når "Miljo:ErBeta" er satt, se
/// Program.cs. Live/lokal dev bruker fortsatt den opprinnelige,
/// ukonvensjonelle direkte DI-registreringen (uendret) og leser ALDRI denne
/// tabellen.
/// </summary>
public sealed class BetaBetalingsinnstilling
{
    public long Id { get; set; }
    public VippsDriftsmodus VippsModus { get; set; } = VippsDriftsmodus.Mock;
    public StripeDriftsmodus StripeModus { get; set; } = StripeDriftsmodus.Mock;
    public long? SistEndretAvUserId { get; set; }
    public DateTimeOffset? SistEndretUtc { get; set; }
}
