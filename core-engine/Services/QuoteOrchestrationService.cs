using MicroInsurTech.CoreEngine.Dtos;

namespace MicroInsurTech.CoreEngine.Services;

public sealed class QuoteOrchestrationService(
    IEnumerable<IUnderwriterService> underwriterServices,
    IPostcodeLookupService postcodeLookupService) : IQuoteOrchestrationService
{
    public async Task<QuoteResponse> GenerateQuotesAsync(QuoteRequest request, CancellationToken cancellationToken)
    {
        if (request.Property is null)
        {
            throw new ArgumentException("Property details are required.", nameof(request));
        }

        var region = await postcodeLookupService.GetRegionAsync(request.Property.Postcode, cancellationToken);
        var enrichedProperty = request.Property with { Region = region };

        var quoteTasks = underwriterServices.Select(service => service.CalculatePremiumAsync(enrichedProperty));
        var quotes = await Task.WhenAll(quoteTasks);

        return new QuoteResponse(
            quotes
                .OrderBy(quote => quote.PremiumAmount)
                .ThenBy(quote => quote.UnderwriterName, StringComparer.Ordinal)
                .ToArray());
    }
}
