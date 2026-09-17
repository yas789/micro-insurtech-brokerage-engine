using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;

namespace MicroInsurTech.CoreEngine.Schemes;

public sealed class AXAScheme : IUnderwriterService
{
    private const decimal BasePremium = 180.00m;
    private const decimal UnoccupiedMultiplier = 1.50m;

    public Task<QuoteResult> CalculatePremiumAsync(PropertyEvaluationDto property)
    {
        var multiplier = property.IsUnoccupied ? UnoccupiedMultiplier : 1.00m;
        var premium = decimal.Round(BasePremium * multiplier, 2, MidpointRounding.AwayFromZero);

        return Task.FromResult(new QuoteResult(
            UnderwriterName: nameof(AXAScheme),
            PremiumAmount: premium,
            RiskRating: multiplier > 1.00m ? "High" : "Low",
            Region: property.Region));
    }
}
