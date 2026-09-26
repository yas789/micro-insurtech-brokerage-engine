using System.Net;
using MicroInsurTech.CoreEngine.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Services;

public sealed class PostcodesIoLookupServiceTests
{
    [Fact]
    public async Task GetRegionAsync_TrimsAndEncodesPostcode()
    {
        using var handler = new StubHandler((request, _) =>
        {
            Assert.Equal("/postcodes/SW1A+1AA", request.RequestUri!.PathAndQuery);
            return Task.FromResult(JsonResponse("""{"result":{"region":"London"}}"""));
        });
        using var client = CreateClient(handler);

        Assert.Equal("London", await CreateService(client).GetRegionAsync(" SW1A 1AA ", CancellationToken.None));
    }

    [Theory]
    [InlineData("{\"result\":null}")]
    [InlineData("{\"result\":{\"region\":\" \"}}")]
    [InlineData("{}")]
    [InlineData("not-json")]
    public async Task GetRegionAsync_AllowsMissingOrMalformedRegion(string body)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse(body)));
        using var client = CreateClient(handler);

        Assert.Null(await CreateService(client).GetRegionAsync("SW1A 1AA", CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetRegionAsync_AllowsHttpFailure(HttpStatusCode status)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(status)));
        using var client = CreateClient(handler);

        Assert.Null(await CreateService(client).GetRegionAsync("SW1A 1AA", CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetRegionAsync_AllowsNetworkFailureOrTimeout(bool timeout)
    {
        using var handler = new StubHandler((_, _) => Task.FromException<HttpResponseMessage>(
            timeout ? new TaskCanceledException("HTTP timeout") : new HttpRequestException("Connection failed")));
        using var client = CreateClient(handler);

        Assert.Null(await CreateService(client).GetRegionAsync("SW1A 1AA", CancellationToken.None));
    }

    [Fact]
    public async Task GetRegionAsync_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new StubHandler((_, token) =>
        {
            cancellation.Cancel();
            return Task.FromCanceled<HttpResponseMessage>(token);
        });
        using var client = CreateClient(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(client).GetRegionAsync("SW1A 1AA", cancellation.Token));
    }

    [Fact]
    public async Task GetRegionAsync_SkipsHttpForBlankPostcode()
    {
        using var handler = new StubHandler((_, _) => throw new InvalidOperationException("Unexpected HTTP call"));
        using var client = CreateClient(handler);

        Assert.Null(await CreateService(client).GetRegionAsync(" ", CancellationToken.None));
        Assert.Equal(0, handler.CallCount);
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://postcode.test") };

    private static PostcodesIoLookupService CreateService(HttpClient client) =>
        new(client, NullLogger<PostcodesIoLookupService>.Instance);

    private static HttpResponseMessage JsonResponse(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body) };

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            CallCount++;
            return send(request, token);
        }
    }
}
