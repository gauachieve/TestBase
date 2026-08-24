using TestBase.Shared.Providers;

namespace TestBase.IntegrationTests.TestDoubles;

/// <summary>
/// Erstatter MockSmsSender i test-verten: fanger opp "sendte" meldinger i minnet
/// slik at tester kan hente ut 2FA-koder/invitasjonslenker direkte, i stedet for
/// å skrape logg-tekst (som den manuelle curl-verifiseringen under utvikling gjorde).
/// Registrert som singleton — se TestBaseWebApplicationFactory.
/// </summary>
public sealed class CapturingSmsSender : ISmsSender
{
    public sealed record SendtSms(string Til, string Melding, DateTimeOffset SendtUtc);

    private readonly List<SendtSms> _sendt = new();
    private readonly object _lock = new();

    public Task SendAsync(string toMobileNumber, string message, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _sendt.Add(new SendtSms(toMobileNumber, message, DateTimeOffset.UtcNow));
        }
        return Task.CompletedTask;
    }

    /// <summary>Siste melding sendt til et gitt mobilnummer, eller null hvis ingen.</summary>
    public string? SisteMeldingTil(string mobilNr)
    {
        lock (_lock)
        {
            return _sendt.LastOrDefault(s => s.Til == mobilNr)?.Melding;
        }
    }

    public IReadOnlyList<SendtSms> AlleSendte()
    {
        lock (_lock)
        {
            return _sendt.ToList();
        }
    }

    public void Nullstill()
    {
        lock (_lock)
        {
            _sendt.Clear();
        }
    }
}
