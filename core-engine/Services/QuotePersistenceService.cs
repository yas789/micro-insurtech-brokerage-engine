using MicroInsurTech.CoreEngine.Data;
using MicroInsurTech.CoreEngine.Data.Entities;
using MicroInsurTech.CoreEngine.Dtos;
using Microsoft.EntityFrameworkCore.Storage;

namespace MicroInsurTech.CoreEngine.Services;

public sealed class QuotePersistenceService(BrokerageDbContext dbContext) : IQuotePersistenceService
{
    public async Task SaveQuoteRequestAsync(
        QuoteRequest request,
        QuoteResponse response,
        CancellationToken cancellationToken)
    {
        if (request.Client is null)
        {
            throw new ArgumentException("Client details are required.", nameof(request));
        }

        if (request.Property is null)
        {
            throw new ArgumentException("Property details are required.", nameof(request));
        }

        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);

        var client = new ClientEntity
        {
            FirstName = request.Client.FirstName.Trim(),
            LastName = request.Client.LastName.Trim(),
            Email = request.Client.Email.Trim(),
        };

        var property = new PropertyEntity
        {
            Client = client,
            Postcode = request.Property.Postcode.Trim(),
            Region = response.Quotes.FirstOrDefault(quote => !string.IsNullOrWhiteSpace(quote.Region))?.Region,
            YearBuilt = request.Property.YearBuilt,
            RebuildCost = request.Property.RebuildCost,
            IsUnoccupied = request.Property.IsUnoccupied,
        };

        foreach (var quote in response.Quotes)
        {
            property.Quotes.Add(new QuoteEntity
            {
                UnderwriterName = quote.UnderwriterName,
                PremiumAmount = quote.PremiumAmount,
                RiskRating = quote.RiskRating,
                Region = quote.Region,
            });
        }

        dbContext.Clients.Add(client);
        dbContext.Properties.Add(property);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return null;
        }

        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }
}
