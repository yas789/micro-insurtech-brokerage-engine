using MicroInsurTech.CoreEngine.Dtos;

namespace MicroInsurTech.CoreEngine.Services;

public interface IUnderwriterService
{
    Task<QuoteResult> CalculatePremiumAsync(PropertyEvaluationDto property);
}
