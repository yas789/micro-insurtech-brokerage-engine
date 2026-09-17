namespace MicroInsurTech.CoreEngine.Dtos;

public sealed record QuoteResponse(IReadOnlyList<QuoteResult> Quotes);
