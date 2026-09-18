using MicroInsurTech.CoreEngine.Dtos;

namespace MicroInsurTech.CoreEngine.Services;

public interface IQuotePersistenceService
{
    Task SaveQuoteRequestAsync(
        QuoteRequest request,
        QuoteResponse response,
        CancellationToken cancellationToken);
}
