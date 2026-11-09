namespace MicroInsurTech.CoreEngine.Dtos;

public sealed record CreatedComparisonResponse(
    Guid QuoteReference, DateTime CreatedAt, IReadOnlyList<QuoteResult> Quotes);
