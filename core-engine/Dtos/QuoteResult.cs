namespace MicroInsurTech.CoreEngine.Dtos;

public sealed record QuoteResult(
    string UnderwriterName,
    decimal PremiumAmount,
    string RiskRating,
    string? Region);
