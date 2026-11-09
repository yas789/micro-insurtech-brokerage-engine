using MicroInsurTech.CoreEngine.Data;
using MicroInsurTech.CoreEngine.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MicroInsurTech.CoreEngine.Services;

public sealed class ComparisonLookupService(BrokerageDbContext dbContext) : IComparisonLookupService
{
    public async Task<SavedComparisonResponse?> FindAsync(Guid reference, CancellationToken cancellationToken)
    {
        var saved = await dbContext.Properties.AsNoTracking()
            .Where(property => property.QuoteReference == reference)
            .Select(property => new SavedComparisonResponse(
                property.QuoteReference, property.CreatedAt,
                new PropertyEvaluationDto(property.Postcode, property.YearBuilt, property.RebuildCost,
                    property.IsUnoccupied, property.Region),
                property.Quotes.Select(quote => new QuoteResult(quote.UnderwriterName,
                    quote.PremiumAmount, quote.RiskRating, quote.Region)).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);

        return saved is null ? null : saved with
        {
            CreatedAt = DateTime.SpecifyKind(saved.CreatedAt, DateTimeKind.Utc),
            Quotes = saved.Quotes.OrderBy(quote => quote.PremiumAmount)
                .ThenBy(quote => quote.UnderwriterName, StringComparer.Ordinal).ToArray(),
        };
    }
}
