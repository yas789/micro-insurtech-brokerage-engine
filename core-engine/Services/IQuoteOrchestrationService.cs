using MicroInsurTech.CoreEngine.Dtos;

namespace MicroInsurTech.CoreEngine.Services;

public interface IQuoteOrchestrationService
{
    Task<QuoteResponse> GenerateQuotesAsync(QuoteRequest request, CancellationToken cancellationToken);
}
