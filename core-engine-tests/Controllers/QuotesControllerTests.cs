using MicroInsurTech.CoreEngine.Controllers;
using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Controllers;

public sealed class QuotesControllerTests
{
    [Fact]
    public async Task GenerateQuoteAsync_ReturnsBadRequestWhenClientIsMissing()
    {
        var controller = new QuotesController(new StubQuoteOrchestrationService(), new StubQuotePersistenceService());
        var request = new QuoteRequest(
            null,
            new PropertyEvaluationDto("SW1A 1AA", 1910, 750000.00m, false));

        var result = await controller.GenerateQuoteAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GenerateQuoteAsync_ReturnsBadRequestWhenPropertyIsMissing()
    {
        var controller = new QuotesController(new StubQuoteOrchestrationService(), new StubQuotePersistenceService());
        var request = new QuoteRequest(
            new ClientDto("Jane", "Broker", "jane@example.com"),
            null);

        var result = await controller.GenerateQuoteAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GenerateQuoteAsync_ReturnsQuoteResponseForValidRequest()
    {
        var expectedResponse = new QuoteResponse(new[]
        {
            new QuoteResult("Test", 100.00m, "Low", "London"),
        });
        var persistenceService = new StubQuotePersistenceService();
        var controller = new QuotesController(new StubQuoteOrchestrationService(expectedResponse), persistenceService);
        var request = new QuoteRequest(
            new ClientDto("Jane", "Broker", "jane@example.com"),
            new PropertyEvaluationDto("SW1A 1AA", 1910, 750000.00m, false));

        var result = await controller.GenerateQuoteAsync(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expectedResponse, okResult.Value);
        Assert.True(persistenceService.WasCalled);
    }

    private sealed class StubQuoteOrchestrationService(QuoteResponse? response = null) : IQuoteOrchestrationService
    {
        public Task<QuoteResponse> GenerateQuotesAsync(QuoteRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(response ?? new QuoteResponse(Array.Empty<QuoteResult>()));
        }
    }

    private sealed class StubQuotePersistenceService : IQuotePersistenceService
    {
        public bool WasCalled { get; private set; }

        public Task SaveQuoteRequestAsync(
            QuoteRequest request,
            QuoteResponse response,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }
}
