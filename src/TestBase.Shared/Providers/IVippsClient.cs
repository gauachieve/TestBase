namespace TestBase.Shared.Providers;

public enum VippsBetalingsstatus
{
    Opprettet,
    Autorisert,
    Fanget,
    Kansellert,
    Utlopt,
    Avbrutt
}

public sealed record VippsOpprettetBetaling(bool Success, string Referanse, string? RedirectUrl, string? ErrorMessage);

public sealed record VippsStatusResultat(bool Success, VippsBetalingsstatus? Status, string? ErrorMessage);

/// <summary>
/// Grensesnitt mot Vipps ePayment API. Selve korttall/betalingsdetaljer skal
/// ALDRI lagres i egen database — kun Vipps sin transaksjonsreferanse (se
/// docs/compliance-dpia-utkast.md). Betaling er iboende ASYNKRON: en
/// betaling opprettes og brukeren sendes til <see cref="VippsOpprettetBetaling.RedirectUrl"/>
/// (Vipps-appen), og selve utfallet bekreftes enten via webhook (se
/// Security/PaymentWebhooks.cs) eller ved å spørre <see cref="HentStatusAsync"/>
/// når brukeren kommer tilbake på returUrl — stol ALDRI på klientsiden/
/// returUrl alene som bevis for at betaling faktisk skjedde.
/// Ekte implementasjon: VippsPaymentClient. I dev/test uten ekte avtale: MockVippsClient.
/// </summary>
public interface IVippsClient
{
    Task<VippsOpprettetBetaling> OpprettBetalingAsync(
        string referanse, decimal belopNok, string beskrivelse, string returUrl, CancellationToken cancellationToken = default);

    Task<VippsStatusResultat> HentStatusAsync(string referanse, CancellationToken cancellationToken = default);
}
