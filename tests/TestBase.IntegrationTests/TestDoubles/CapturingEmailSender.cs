using TestBase.Shared.Providers;

namespace TestBase.IntegrationTests.TestDoubles;

/// <summary>Erstatter MockEmailSender i test-verten — se CapturingSmsSender for begrunnelse.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    public sealed record SendtEpost(string Til, string Emne, string Body, DateTimeOffset SendtUtc);

    private readonly List<SendtEpost> _sendt = new();
    private readonly object _lock = new();

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _sendt.Add(new SendtEpost(toEmail, subject, body, DateTimeOffset.UtcNow));
        }
        return Task.CompletedTask;
    }

    public string? SisteMeldingTil(string epost)
    {
        lock (_lock)
        {
            return _sendt.LastOrDefault(s => s.Til == epost)?.Body;
        }
    }

    public IReadOnlyList<SendtEpost> AlleSendte()
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
