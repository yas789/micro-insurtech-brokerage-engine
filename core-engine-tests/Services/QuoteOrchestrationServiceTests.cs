using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Services;

public sealed class QuoteOrchestrationServiceTests
{
    [Fact]
    public async Task GenerateQuotesAsync_CallsAllUnderwritersAndSortsQuotesByPremium()
    {
        var service = new QuoteOrchestrationService(
            new IUnderwriterService[]
            {
                new StubUnderwriterService("Expensive", 300.00m),
                new StubUnderwriterService("Cheap", 100.00m),
                new StubUnderwriterService("Middle", 200.00m),
            },
            new StubPostcodeLookupService("London"));
        var request = CreateRequest();

        var response = await service.GenerateQuotesAsync(request, CancellationToken.None);

        Assert.Collection(
            response.Quotes,
            quote => Assert.Equal("Cheap", quote.UnderwriterName),
            quote => Assert.Equal("Middle", quote.UnderwriterName),
            quote => Assert.Equal("Expensive", quote.UnderwriterName));
    }

    [Fact]
    public async Task GenerateQuotesAsync_AddsPostcodeRegionToUnderwriterEvaluation()
    {
        var underwriter = new CapturingUnderwriterService();
        var service = new QuoteOrchestrationService(
            new IUnderwriterService[] { underwriter },
            new StubPostcodeLookupService("South East"));
        var request = CreateRequest();

        var response = await service.GenerateQuotesAsync(request, CancellationToken.None);

        Assert.Equal("South East", underwriter.CapturedProperty?.Region);
        Assert.Equal("South East", response.Quotes.Single().Region);
    }

    [Fact]
    public async Task GenerateQuotesAsync_AllowsMissingPostcodeRegion()
    {
        var underwriter = new CapturingUnderwriterService();
        var service = new QuoteOrchestrationService(
            new IUnderwriterService[] { underwriter },
            new StubPostcodeLookupService(null));
        var request = CreateRequest();

        var response = await service.GenerateQuotesAsync(request, CancellationToken.None);

        Assert.Null(underwriter.CapturedProperty?.Region);
        Assert.Null(response.Quotes.Single().Region);
    }

    [Fact]
    public async Task GenerateQuotesAsync_SortsMatchingPremiumsByUnderwriterName()
    {
        var service = new QuoteOrchestrationService(
            new IUnderwriterService[]
            {
                new StubUnderwriterService("Zurich", 200.00m),
                new StubUnderwriterService("Aviva", 200.00m),
            },
            new StubPostcodeLookupService("London"));

        var response = await service.GenerateQuotesAsync(CreateRequest(), CancellationToken.None);

        Assert.Collection(
            response.Quotes,
            quote => Assert.Equal("Aviva", quote.UnderwriterName),
            quote => Assert.Equal("Zurich", quote.UnderwriterName));
    }

    [Fact]
    public async Task GenerateQuotesAsync_RejectsMissingProperty()
    {
        var service = new QuoteOrchestrationService(
            Array.Empty<IUnderwriterService>(),
            new StubPostcodeLookupService("London"));
        var request = new QuoteRequest(new ClientDto("Jane", "Broker", "jane@example.com"), null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GenerateQuotesAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateQuotesAsync_PassesCancellationTokenToPostcodeLookup()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var postcodeLookup = new CapturingPostcodeLookupService("London");
        var service = new QuoteOrchestrationService(
            new IUnderwriterService[] { new StubUnderwriterService("Aviva", 100.00m) },
            postcodeLookup);

        await service.GenerateQuotesAsync(CreateRequest(), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, postcodeLookup.CapturedCancellationToken);
    }

    [Fact]
    public async Task GenerateQuotesAsync_StartsEverySchemeBeforeWaitingForResults()
    {
        var first = new DeferredUnderwriterService();
        var second = new DeferredUnderwriterService();
        var service = new QuoteOrchestrationService(
            new IUnderwriterService[] { first, second }, new StubPostcodeLookupService("London"));

        var pending = service.GenerateQuotesAsync(CreateRequest(), CancellationToken.None);

        Assert.True(first.WasCalled);
        Assert.True(second.WasCalled);
        Assert.False(pending.IsCompleted);
        second.Completion.SetResult(new QuoteResult("Second", 200, "Low", "London"));
        Assert.False(pending.IsCompleted);
        first.Completion.SetResult(new QuoteResult("First", 100, "Low", "London"));

        var response = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { "First", "Second" }, response.Quotes.Select(quote => quote.UnderwriterName));
    }

    [Fact]
    public async Task GenerateQuotesAsync_DoesNotReturnPartialQuotesWhenSchemeFails()
    {
        var failed = new DeferredUnderwriterService();
        var service = new QuoteOrchestrationService(
            new IUnderwriterService[] { new StubUnderwriterService("Success", 100), failed },
            new StubPostcodeLookupService(null));
        var failure = new InvalidOperationException("Rating failed");

        var pending = service.GenerateQuotesAsync(CreateRequest(), CancellationToken.None);
        failed.Completion.SetException(failure);

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending));
    }

    private sealed class DeferredUnderwriterService : IUnderwriterService
    {
        public bool WasCalled { get; private set; }
        public TaskCompletionSource<QuoteResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<QuoteResult> CalculatePremiumAsync(PropertyEvaluationDto property)
        {
            WasCalled = true;
            return Completion.Task;
        }
    }

    private static QuoteRequest CreateRequest()
    {
        return new QuoteRequest(
            new ClientDto("Jane", "Broker", "jane@example.com"),
            new PropertyEvaluationDto("SW1A 1AA", 1910, 750000.00m, false));
    }

    private sealed class StubPostcodeLookupService(string? region) : IPostcodeLookupService
    {
        public Task<string?> GetRegionAsync(string postcode, CancellationToken cancellationToken)
        {
            return Task.FromResult(region);
        }
    }

    private sealed class CapturingPostcodeLookupService(string? region) : IPostcodeLookupService
    {
        public CancellationToken CapturedCancellationToken { get; private set; }

        public Task<string?> GetRegionAsync(string postcode, CancellationToken cancellationToken)
        {
            CapturedCancellationToken = cancellationToken;
            return Task.FromResult(region);
        }
    }

    private sealed class StubUnderwriterService(string name, decimal premium) : IUnderwriterService
    {
        public Task<QuoteResult> CalculatePremiumAsync(PropertyEvaluationDto property)
        {
            return Task.FromResult(new QuoteResult(name, premium, "Low", property.Region));
        }
    }

    private sealed class CapturingUnderwriterService : IUnderwriterService
    {
        public PropertyEvaluationDto? CapturedProperty { get; private set; }

        public Task<QuoteResult> CalculatePremiumAsync(PropertyEvaluationDto property)
        {
            CapturedProperty = property;
            return Task.FromResult(new QuoteResult("Capture", 100.00m, "Low", property.Region));
        }
    }
}
