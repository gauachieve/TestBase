using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Shared.Providers;

/// <summary>
/// Leser DriftsbryterInnstilling.SmsErAktiv FOR HVERT KALL (ikke cachet) og delegerer til ekte
/// eller mock SMS-sender — se DriftsbryterInnstilling for begrunnelsen. ISmsSender er alltid
/// registrert Scoped (VonageSmsSender/MockSmsSender begge Scoped), så DriftsbryterInnstillingService
/// (også Scoped) kan injiseres direkte her — IKKE samme "scoped fra singleton"-problem som
/// DriftsbryterVippsClient må løse med en scope-factory.
/// </summary>
public sealed class DriftsbryterSmsSender : ISmsSender
{
    private readonly DriftsbryterInnstillingService _innstillinger;
    private readonly ISmsSender _mock;
    private readonly ISmsSender? _ekte;

    public DriftsbryterSmsSender(DriftsbryterInnstillingService innstillinger, ISmsSender mock, ISmsSender? ekte)
    {
        _innstillinger = innstillinger;
        _mock = mock;
        _ekte = ekte;
    }

    public async Task SendAsync(string toMobileNumber, string message, CancellationToken cancellationToken = default)
    {
        var innstilling = await _innstillinger.HentAsync(cancellationToken);
        var klient = innstilling.SmsErAktiv && _ekte is not null ? _ekte : _mock;
        await klient.SendAsync(toMobileNumber, message, cancellationToken);
    }
}
