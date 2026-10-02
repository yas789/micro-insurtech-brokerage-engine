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
    private const string ValidRegion = "London";
    private const string ValidRiskRating = "Low";
    private const decimal ValidPremiumAmount = 100.00m;
    private const string ValidUnderwriterName = "Test";

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

    [Fact]
    public async Task GenerateQuoteAsync_ReturnsBadRequestWhenRequestIsMissing()
    {
        var orchestrationService = new StubQuoteOrchestrationService();
        var persistenceService = new StubQuotePersistenceService();
        var controller = new QuotesController(orchestrationService, persistenceService);

        var result = await controller.GenerateQuoteAsync(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(orchestrationService.WasCalled);
        Assert.False(persistenceService.WasCalled);
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("email")]
    [InlineData("postcode")]
    [InlineData("yearBuiltTooOld")]
    [InlineData("yearBuiltTooNew")]
    [InlineData("rebuildCost")]
    public async Task GenerateQuoteAsync_ReturnsBadRequestAndSkipsSideEffectsForInvalidFields(string field)
    {
        var orchestrationService = new StubQuoteOrchestrationService();
        var persistenceService = new StubQuotePersistenceService();
        var controller = new QuotesController(orchestrationService, persistenceService);
        var request = CreateRequestWithInvalidField(field);

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

    [Theory]
    [InlineData("0.01")]
    [InlineData("750000.00")]
    [InlineData("99999999.99")]
    public async Task GenerateQuoteAsync_ReturnsQuoteResponseForValidRequest(string rebuildCost)
    {
        var expectedResponse = new QuoteResponse(new[]
        {
            new QuoteResult(ValidUnderwriterName, ValidPremiumAmount, ValidRiskRating, ValidRegion),
        });
        var persistenceService = new StubQuotePersistenceService();
        var controller = new QuotesController(new StubQuoteOrchestrationService(expectedResponse), persistenceService);
        var request = new QuoteRequest(
            new ClientDto(ValidFirstName, ValidLastName, ValidEmail),
            new PropertyEvaluationDto(ValidPostcode, ValidYearBuilt,
                decimal.Parse(rebuildCost, System.Globalization.CultureInfo.InvariantCulture), false));

        var result = await controller.GenerateQuoteAsync(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expectedResponse, okResult.Value);
        Assert.True(persistenceService.WasCalled);
    }

    [Theory]
    [InlineData("100000000", "Property rebuild cost must be 99999999.99 or less.")]
    [InlineData("99999999.995", "Property rebuild cost must be 99999999.99 or less.")]
    [InlineData("750000.001", "Property rebuild cost must have at most two decimal places.")]
    [InlineData("0.001", "Property rebuild cost must have at most two decimal places.")]
    public async Task GenerateQuoteAsync_RejectsUnstorableRebuildCostsWithoutSideEffects(string value, string expectedError)
    {
        var orchestration = new StubQuoteOrchestrationService();
        var persistence = new StubQuotePersistenceService();
        var controller = new QuotesController(orchestration, persistence);
        var request = CreateRequest(rebuildCost: decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

        var result = await controller.GenerateQuoteAsync(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var json = System.Text.Json.JsonSerializer.SerializeToElement(badRequest.Value);
        Assert.Equal(expectedError, Assert.Single(json.GetProperty("errors").EnumerateArray()).GetString());
        Assert.False(orchestration.WasCalled);
        Assert.False(persistence.WasCalled);
    }

    [Fact]
    public async Task GenerateQuoteAsync_WaitsForPersistenceBeforeReturningSuccess()
    {
        using var cancellation = new CancellationTokenSource();
        var persistence = new DeferredPersistenceService();
        var response = new QuoteResponse(new[] { new QuoteResult("Test", 100, "Low", "London") });
        var controller = new QuotesController(new StubQuoteOrchestrationService(response), persistence);
        var request = CreateRequest();

        var pending = controller.GenerateQuoteAsync(request, cancellation.Token);

        Assert.False(pending.IsCompleted);
        Assert.Same(request, persistence.Request);
        Assert.Same(response, persistence.Response);
        Assert.Equal(cancellation.Token, persistence.Token);
        persistence.Completion.SetResult();
        var result = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Same(response, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task GenerateQuoteAsync_DoesNotReturnSuccessWhenPersistenceFails()
    {
        var persistence = new DeferredPersistenceService();
        var controller = new QuotesController(new StubQuoteOrchestrationService(), persistence);
        var failure = new InvalidOperationException("Database unavailable");

        var pending = controller.GenerateQuoteAsync(CreateRequest(), CancellationToken.None);
        persistence.Completion.SetException(failure);

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending));
    }

    [Fact]
    public async Task GenerateQuoteAsync_SkipsPersistenceWhenRatingFails()
    {
        var persistence = new StubQuotePersistenceService();
        var controller = new QuotesController(new FailingOrchestrationService(), persistence);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.GenerateQuoteAsync(CreateRequest(), CancellationToken.None));

        Assert.False(persistence.WasCalled);
    }

    private sealed class FailingOrchestrationService : IQuoteOrchestrationService
    {
        public Task<QuoteResponse> GenerateQuotesAsync(QuoteRequest request, CancellationToken cancellationToken) =>
            Task.FromException<QuoteResponse>(new InvalidOperationException("Rating failed"));
    }

    private sealed class DeferredPersistenceService : IQuotePersistenceService
    {
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public QuoteRequest? Request { get; private set; }
        public QuoteResponse? Response { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task SaveQuoteRequestAsync(QuoteRequest request, QuoteResponse response, CancellationToken cancellationToken)
        {
            Request = request;
            Response = response;
            Token = cancellationToken;
            return Completion.Task;
        }
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

    private static QuoteRequest CreateRequestWithInvalidField(string field)
    {
        return field switch
        {
            "firstName" => CreateRequest(firstName: " "),
            "lastName" => CreateRequest(lastName: " "),
            "email" => CreateRequest(email: "not-an-email"),
            "postcode" => CreateRequest(postcode: " "),
            "yearBuiltTooOld" => CreateRequest(yearBuilt: 1499),
            "yearBuiltTooNew" => CreateRequest(yearBuilt: 2101),
            "rebuildCost" => CreateRequest(rebuildCost: 0),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unsupported field."),
        };
    }

    private static QuoteRequest CreateRequest(
        string firstName = ValidFirstName,
        string lastName = ValidLastName,
        string email = ValidEmail,
        string postcode = ValidPostcode,
        int yearBuilt = ValidYearBuilt,
        decimal rebuildCost = ValidRebuildCost)
    {
        return new QuoteRequest(
            new ClientDto(firstName, lastName, email),
            new PropertyEvaluationDto(postcode, yearBuilt, rebuildCost, false));
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
