namespace MicroInsurTech.CoreEngine.Dtos;

public sealed record QuoteRequest(
    ClientDto Client,
    PropertyEvaluationDto Property);
