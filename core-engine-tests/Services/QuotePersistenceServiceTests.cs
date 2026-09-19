using MicroInsurTech.CoreEngine.Data;
using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Services;

public sealed class QuotePersistenceServiceTests
{
    private const string ValidFirstName = "Jane";
    private const string ValidLastName = "Broker";
    private const string ValidEmail = "jane@example.com";
    private const string ValidPostcode = "SW1A 1AA";
    private const int ValidYearBuilt = 1910;
    private const decimal ValidRebuildCost = 750000.00m;

    [Fact]
    public async Task SaveQuoteRequestAsync_SavesClientPropertyAndQuotes()
    {
        await using var dbContext = CreateDbContext();
        var service = new QuotePersistenceService(dbContext);
        var request = new QuoteRequest(
            new ClientDto($" {ValidFirstName} ", $" {ValidLastName} ", $" {ValidEmail} "),
            new PropertyEvaluationDto($" {ValidPostcode} ", ValidYearBuilt, ValidRebuildCost, false));
        var response = new QuoteResponse(new[]
        {
            new QuoteResult("Aviva", 950.00m, "Low", "London"),
            new QuoteResult("AXA", 1000.00m, "Medium", "London"),
        });

        await service.SaveQuoteRequestAsync(request, response, CancellationToken.None);

        var client = await dbContext.Clients.SingleAsync();
        var property = await dbContext.Properties.SingleAsync();
        var quotes = await dbContext.Quotes.OrderBy(quote => quote.PremiumAmount).ToArrayAsync();

        Assert.Equal(ValidFirstName, client.FirstName);
        Assert.Equal(ValidLastName, client.LastName);
        Assert.Equal(ValidEmail, client.Email);
        Assert.Equal(client.ClientId, property.ClientId);
        Assert.Equal(ValidPostcode, property.Postcode);
        Assert.Equal("London", property.Region);
        Assert.Equal(ValidYearBuilt, property.YearBuilt);
        Assert.Equal(ValidRebuildCost, property.RebuildCost);
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
            new PropertyEvaluationDto(ValidPostcode, ValidYearBuilt, ValidRebuildCost, false));
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
            new ClientDto(ValidFirstName, ValidLastName, ValidEmail),
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
