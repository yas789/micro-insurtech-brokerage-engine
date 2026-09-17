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
