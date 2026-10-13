using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TestBase.Shared.Domain.Administrasjon;
using TestBase.Shared.Providers;
using Xunit;

namespace TestBase.IntegrationTests;

public class DriftsstatusVarslingTests
{
    [Fact]
    public void BeregnDiff_NyRoedSjekk_GirDenSomNyRoedOgHuskerDen()
    {
        var sistVarslet = new HashSet<string>();
        var sjekker = new[] { new DriftsstatusSjekk("Vipps", DriftsstatusLys.Rod, "Feil", 10) };

        var (nyeRoede, nyFriske) = DriftsstatusVarselBeregner.BeregnDiff(sistVarslet, sjekker);

        Assert.Equal(new[] { "Vipps" }, nyeRoede);
        Assert.Empty(nyFriske);
        Assert.Contains("Vipps", sistVarslet);
    }

    [Fact]
    public void BeregnDiff_SammeRoedSjekkToGangerPaaRad_VarslerIkkePaaNytt()
    {
        var sistVarslet = new HashSet<string> { "Vipps" };
        var sjekker = new[] { new DriftsstatusSjekk("Vipps", DriftsstatusLys.Rod, "Fortsatt feil", 10) };

        var (nyeRoede, nyFriske) = DriftsstatusVarselBeregner.BeregnDiff(sistVarslet, sjekker);

        Assert.Empty(nyeRoede);
        Assert.Empty(nyFriske);
    }

    [Fact]
    public void BeregnDiff_SjekkBlirFriskIgjen_GirDenSomNyFriskOgGlemmerDen()
    {
        var sistVarslet = new HashSet<string> { "Vipps" };
        var sjekker = new[] { new DriftsstatusSjekk("Vipps", DriftsstatusLys.Gronn, "OK", 10) };

        var (nyeRoede, nyFriske) = DriftsstatusVarselBeregner.BeregnDiff(sistVarslet, sjekker);

        Assert.Empty(nyeRoede);
        Assert.Equal(new[] { "Vipps" }, nyFriske);
        Assert.DoesNotContain("Vipps", sistVarslet);
    }

    [Fact]
    public void BeregnDiff_GraattLysTellerIkkeSomRoedt()
    {
        var sistVarslet = new HashSet<string>();
        var sjekker = new[] { new DriftsstatusSjekk("E-post", DriftsstatusLys.Graa, "Ikke konfigurert", null) };

        var (nyeRoede, nyFriske) = DriftsstatusVarselBeregner.BeregnDiff(sistVarslet, sjekker);

        Assert.Empty(nyeRoede);
        Assert.Empty(nyFriske);
        Assert.Empty(sistVarslet);
    }

    [Fact]
    public async Task PushoverKritiskVarselSender_SendAsync_PosterTokenBrukerOgMelding()
    {
        HttpRequestMessage? fangetForespørsel = null;
        string? fangetBody = null;
        var handler = new FakeHttpMessageHandler(async request =>
        {
            fangetForespørsel = request;
            fangetBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var http = new HttpClient(handler);
        var sender = new PushoverKritiskVarselSender(http, "app-token-123", NullLogger<PushoverKritiskVarselSender>.Instance);

        await sender.SendAsync("bruker-key-456", "Tittel", "Melding");

        Assert.NotNull(fangetForespørsel);
        Assert.Equal("https://api.pushover.net/1/messages.json", fangetForespørsel!.RequestUri!.ToString());
        Assert.Contains("token=app-token-123", fangetBody);
        Assert.Contains("user=bruker-key-456", fangetBody);
        Assert.Contains("title=Tittel", fangetBody);
        Assert.Contains("message=Melding", fangetBody);
    }

    [Fact]
    public async Task PushoverKritiskVarselSender_SendAsync_FeilStatusKasterException()
    {
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"errors\":[\"invalid user\"]}")
        }));
        var http = new HttpClient(handler);
        var sender = new PushoverKritiskVarselSender(http, "app-token-123", NullLogger<PushoverKritiskVarselSender>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => sender.SendAsync("ugyldig-key", "Tittel", "Melding"));
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _respond;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond)
        {
            _respond = respond;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _respond(request);
    }
}
