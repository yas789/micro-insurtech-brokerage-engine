using MicroInsurTech.CoreEngine.Data;
using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Services;

public sealed class ComparisonLookupServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FindsOriginalSnapshotWithoutTrackingOrIdentity(bool empty)
    {
        await using var context = new BrokerageDbContext(new DbContextOptionsBuilder<BrokerageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var request = new QuoteRequest(new ClientDto("PrivateName", "PrivateSurname", "private@example.com"),
            new PropertyEvaluationDto("SW1A 1AA", 1910, 750000, false));
        var quotes = empty ? Array.Empty<QuoteResult>() : new[]
        {
            new QuoteResult("Z", 123.45m, "High", "Original region"),
            new QuoteResult("A", 123.45m, "Low", "Original region"),
        };
        var saved = await new QuotePersistenceService(context).SaveQuoteRequestAsync(request, new QuoteResponse(quotes), default);
        context.ChangeTracker.Clear();
        var lookup = new ComparisonLookupService(context);

        Assert.Null(await lookup.FindAsync(Guid.NewGuid(), default));
        var found = Assert.IsType<SavedComparisonResponse>(await lookup.FindAsync(saved.QuoteReference, default));
        Assert.Equal(saved.CreatedAt, found.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, found.CreatedAt.Kind);
        Assert.Equal(request.Property!.RebuildCost, found.Property.RebuildCost);
        Assert.Equal(quotes.OrderBy(q => q.UnderwriterName), found.Quotes);
        Assert.Empty(context.ChangeTracker.Entries());
        var json = JsonSerializer.Serialize(found);
        Assert.DoesNotContain("PrivateName", json);
        Assert.DoesNotContain("private@example.com", json);
        Assert.DoesNotContain("ClientId", json);
        Assert.DoesNotContain("PropertyId", json);
        Assert.Equal(1, await context.Properties.CountAsync());
        Assert.Equal(quotes.Length, await context.Quotes.CountAsync());
    }
}
