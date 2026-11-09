namespace MicroInsurTech.CoreEngine.Dtos;

public sealed record SavedComparisonResponse(
    Guid QuoteReference, DateTime CreatedAt, PropertyEvaluationDto Property,
    IReadOnlyList<QuoteResult> Quotes);
