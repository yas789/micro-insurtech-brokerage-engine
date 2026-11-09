using MicroInsurTech.CoreEngine.Dtos;

namespace MicroInsurTech.CoreEngine.Services;

public interface IQuotePersistenceService
{
    Task<SavedComparisonMetadata> SaveQuoteRequestAsync(
        QuoteRequest request,
        QuoteResponse response,
        CancellationToken cancellationToken);
}
