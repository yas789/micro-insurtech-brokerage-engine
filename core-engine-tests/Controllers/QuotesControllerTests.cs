using MicroInsurTech.CoreEngine.Controllers;
using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Controllers;

public sealed class QuotesControllerTests
{
    private const string ValidFirstName = "Jane";
    private const string ValidLastName = "Broker";
    private const string ValidEmail = "jane@example.com";
    private const string ValidPostcode = "SW1A 1AA";
    private const int ValidYearBuilt = 1910;
    private const decimal ValidRebuildCost = 750000.00m;

    [Fact]
    public async Task GenerateQuoteAsync_ReturnsBadRequestWhenClientIsMissing()
    {
        var orchestrationService = new StubQuoteOrchestrationService();
        var persistenceService = new StubQuotePersistenceService();
        var controller = new QuotesController(orchestrationService, persistenceService);
        var request = new QuoteRequest(
            null,
            new PropertyEvaluationDto(ValidPostcode, ValidYearBuilt, ValidRebuildCost, false));

        var result = await controller.GenerateQuoteAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(orchestrationService.WasCalled);
        Assert.False(persistenceService.WasCalled);
    }

    [Fact]
    public async Task GenerateQuoteAsync_ReturnsBadRequestWhenPropertyIsMissing()
    {
        var orchestrationService = new StubQuoteOrchestrationService();
        var persistenceService = new StubQuotePersistenceService();
        var controller = new QuotesController(orchestrationService, persistenceService);
        var request = new QuoteRequest(
            new ClientDto(ValidFirstName, ValidLastName, ValidEmail),
            null);

        var result = await controller.GenerateQuoteAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(orchestrationService.WasCalled);
        Assert.False(persistenceService.WasCalled);
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("email")]
    [InlineData("postcode")]
    public async Task GenerateQuoteAsync_ReturnsBadRequestAndSkipsSideEffectsWhenDbStringLimitsAreExceeded(string field)
    {
        var orchestrationService = new StubQuoteOrchestrationService();
        var persistenceService = new StubQuotePersistenceService();
        var controller = new QuotesController(orchestrationService, persistenceService);
        var request = CreateRequestWithOversizedField(field);

        var result = await controller.GenerateQuoteAsync(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(orchestrationService.WasCalled);
        Assert.False(persistenceService.WasCalled);
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
            new ClientDto(ValidFirstName, ValidLastName, ValidEmail),
            new PropertyEvaluationDto(ValidPostcode, ValidYearBuilt, ValidRebuildCost, false));

        var result = await controller.GenerateQuoteAsync(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expectedResponse, okResult.Value);
        Assert.True(persistenceService.WasCalled);
    }

    private sealed class StubQuoteOrchestrationService(QuoteResponse? response = null) : IQuoteOrchestrationService
    {
        public bool WasCalled { get; private set; }

        public Task<QuoteResponse> GenerateQuotesAsync(QuoteRequest request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(response ?? new QuoteResponse(Array.Empty<QuoteResult>()));
        }
    }

    private static QuoteRequest CreateRequestWithOversizedField(string field)
    {
        return field switch
        {
            "firstName" => CreateRequest(firstName: new string('A', 101)),
            "lastName" => CreateRequest(lastName: new string('B', 101)),
            "email" => CreateRequest(email: $"{new string('c', 250)}@x.com"),
            "postcode" => CreateRequest(postcode: new string('D', 17)),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unsupported field."),
        };
    }

    private static QuoteRequest CreateRequest(
        string firstName = ValidFirstName,
        string lastName = ValidLastName,
        string email = ValidEmail,
        string postcode = ValidPostcode)
    {
        return new QuoteRequest(
            new ClientDto(firstName, lastName, email),
            new PropertyEvaluationDto(postcode, ValidYearBuilt, ValidRebuildCost, false));
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
