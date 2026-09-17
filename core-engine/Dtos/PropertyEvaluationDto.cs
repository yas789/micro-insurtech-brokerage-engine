namespace MicroInsurTech.CoreEngine.Dtos;

public sealed record PropertyEvaluationDto(
    string Postcode,
    int YearBuilt,
    decimal RebuildCost,
    bool IsUnoccupied,
    string? Region = null);
