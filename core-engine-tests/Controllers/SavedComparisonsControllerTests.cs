using MicroInsurTech.CoreEngine.Controllers;
using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Controllers;

public sealed class SavedComparisonsControllerTests
{
    [Theory]
    [InlineData("1")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("3fc8045c0a9a4df496d6f530bddb899a")]
    [InlineData("{3fc8045c-0a9a-4df4-96d6-f530bddb899a}")]
    public async Task RejectsMalformedReferencesWithoutLookup(string reference)
    {
        var lookup = new Lookup();
        var result = await new SavedComparisonsController(lookup).GetAsync(reference, default);
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(lookup.Called);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReturnsSavedComparisonOrNotFound(bool exists)
    {
        var reference = Guid.NewGuid();
        var lookup = new Lookup { Value = exists ? new SavedComparisonResponse(reference, DateTime.UtcNow,
            new PropertyEvaluationDto("SW1A 1AA", 1910, 750000, false), Array.Empty<QuoteResult>()) : null };
        using var cancellation = new CancellationTokenSource();
        var result = await new SavedComparisonsController(lookup).GetAsync(reference.ToString().ToUpperInvariant(), cancellation.Token);
        Assert.Equal(reference, lookup.Reference);
        Assert.Equal(cancellation.Token, lookup.Token);
        if (exists) Assert.Same(lookup.Value, Assert.IsType<OkObjectResult>(result.Result).Value);
        else Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    private sealed class Lookup : IComparisonLookupService
    {
        public bool Called { get; private set; }
        public Guid Reference { get; private set; }
        public CancellationToken Token { get; private set; }
        public SavedComparisonResponse? Value { get; init; }
        public Task<SavedComparisonResponse?> FindAsync(Guid reference, CancellationToken cancellationToken)
        {
            Called = true;
            Reference = reference;
            Token = cancellationToken;
            return Task.FromResult(Value);
        }
    }
}
