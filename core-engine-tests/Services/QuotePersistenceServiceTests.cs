using MicroInsurTech.CoreEngine.Data;
using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Services;

public sealed class QuotePersistenceServiceTests
{
    [Fact]
    public async Task SaveQuoteRequestAsync_SavesClientPropertyAndQuotes()
    {
        await using var dbContext = CreateDbContext();
        var service = new QuotePersistenceService(dbContext);
        var request = new QuoteRequest(
            new ClientDto(" Jane ", " Broker ", " jane@example.com "),
            new PropertyEvaluationDto(" SW1A 1AA ", 1910, 750000.00m, false));
        var response = new QuoteResponse(new[]
        {
            new QuoteResult("Aviva", 950.00m, "Low", "London"),
            new QuoteResult("AXA", 1000.00m, "Medium", "London"),
        });

        await service.SaveQuoteRequestAsync(request, response, CancellationToken.None);

        var client = await dbContext.Clients.SingleAsync();
        var property = await dbContext.Properties.SingleAsync();
        var quotes = await dbContext.Quotes.OrderBy(quote => quote.PremiumAmount).ToArrayAsync();

        Assert.Equal("Jane", client.FirstName);
        Assert.Equal("Broker", client.LastName);
        Assert.Equal("jane@example.com", client.Email);
        Assert.Equal(client.ClientId, property.ClientId);
        Assert.Equal("SW1A 1AA", property.Postcode);
        Assert.Equal("London", property.Region);
        Assert.Equal(1910, property.YearBuilt);
        Assert.Equal(750000.00m, property.RebuildCost);
        Assert.Collection(
            quotes,
            quote =>
            {
                Assert.Equal(property.PropertyId, quote.PropertyId);
                Assert.Equal("Aviva", quote.UnderwriterName);
                Assert.Equal(950.00m, quote.PremiumAmount);
                Assert.Equal("Low", quote.RiskRating);
                Assert.Equal("London", quote.Region);
            },
            quote =>
            {
                Assert.Equal(property.PropertyId, quote.PropertyId);
                Assert.Equal("AXA", quote.UnderwriterName);
                Assert.Equal(1000.00m, quote.PremiumAmount);
                Assert.Equal("Medium", quote.RiskRating);
                Assert.Equal("London", quote.Region);
            });
    }

    [Fact]
    public async Task SaveQuoteRequestAsync_RejectsMissingClient()
    {
        await using var dbContext = CreateDbContext();
        var service = new QuotePersistenceService(dbContext);
        var request = new QuoteRequest(
            null,
            new PropertyEvaluationDto("SW1A 1AA", 1910, 750000.00m, false));
        var response = new QuoteResponse(Array.Empty<QuoteResult>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveQuoteRequestAsync(request, response, CancellationToken.None));
    }

    [Fact]
    public async Task SaveQuoteRequestAsync_RejectsMissingProperty()
    {
        await using var dbContext = CreateDbContext();
        var service = new QuotePersistenceService(dbContext);
        var request = new QuoteRequest(
            new ClientDto("Jane", "Broker", "jane@example.com"),
            null);
        var response = new QuoteResponse(Array.Empty<QuoteResult>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveQuoteRequestAsync(request, response, CancellationToken.None));
    }

    private static BrokerageDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BrokerageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new BrokerageDbContext(options);
    }
}
