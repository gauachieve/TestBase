using Microsoft.Extensions.DependencyInjection;
using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Shared.Providers;

/// <summary>
/// Kun brukt når "Miljo:ErBeta" er satt (se Program.cs) — leser gjeldende
/// VippsDriftsmodus FOR HVERT KALL (ikke cachet) fra BetaInnstillingService,
/// og delegerer til riktig underliggende klient. Faller stille tilbake til
/// mock hvis ønsket modus sin klient ikke er konfigurert (f.eks. "Produksjon"
/// valgt før noen ekte produksjonsnøkler er satt på beta) — se
/// docs/beslutningslogg.md "Beta-miljø" for begrunnelsen bak selve
/// tre-modus-designet (Mock/Test/Produksjon, IKKE bare mock/ekte).
///
/// Registrert som Singleton (som IVippsClient alltid har vært, se Program.cs —
/// VippsPaymentClient cacher tilgangstoken i minnet), men BetaInnstillingService
/// er Scoped (bruker AppDbContext) — kan derfor IKKE injiseres direkte i
/// konstruktøren (ville kastet "Cannot resolve scoped service from root
/// provider" ved første ekte kall, oppdaget under verifisering av denne
/// runden). Løses ved å opprette en kortlevd DI-scope PER kall kun for å lese
/// databaseraden, uten å gjøre selve klienten scoped (som ville mistet
/// tokens-cachen på Test-/Produksjon-instansene).
/// </summary>
public sealed class BetaSwitchingVippsClient : IVippsClient
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IVippsClient _mock;
    private readonly IVippsClient? _test;
    private readonly IVippsClient? _produksjon;

    public BetaSwitchingVippsClient(IServiceScopeFactory scopeFactory, IVippsClient mock, IVippsClient? test, IVippsClient? produksjon)
    {
        _scopeFactory = scopeFactory;
        _mock = mock;
        _test = test;
        _produksjon = produksjon;
    }

    public async Task<VippsOpprettetBetaling> OpprettBetalingAsync(
        string referanse, decimal belopNok, string beskrivelse, string returUrl, CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).OpprettBetalingAsync(referanse, belopNok, beskrivelse, returUrl, cancellationToken);

    public async Task<VippsStatusResultat> HentStatusAsync(string referanse, CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).HentStatusAsync(referanse, cancellationToken);

    public async Task<VippsFangetResultat> FangBetalingAsync(string referanse, decimal belopNok, CancellationToken cancellationToken = default) =>
        await (await AktivKlientAsync(cancellationToken)).FangBetalingAsync(referanse, belopNok, cancellationToken);

    private async Task<IVippsClient> AktivKlientAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var innstillinger = scope.ServiceProvider.GetRequiredService<BetaInnstillingService>();
        var innstilling = await innstillinger.HentAsync(cancellationToken);
        return innstilling.VippsModus switch
        {
            VippsDriftsmodus.Test when _test is not null => _test,
            VippsDriftsmodus.Produksjon when _produksjon is not null => _produksjon,
            _ => _mock
        };
    }
}
