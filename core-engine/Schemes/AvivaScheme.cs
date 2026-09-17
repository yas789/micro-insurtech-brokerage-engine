using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;

namespace MicroInsurTech.CoreEngine.Schemes;

public sealed class AvivaScheme : IUnderwriterService
{
    private const decimal BasePremium = 150.00m;
    private const decimal Pre1920Multiplier = 1.40m;

    public Task<QuoteResult> CalculatePremiumAsync(PropertyEvaluationDto property)
    {
        var multiplier = property.YearBuilt < 1920 ? Pre1920Multiplier : 1.00m;
        var premium = decimal.Round(BasePremium * multiplier, 2, MidpointRounding.AwayFromZero);

        return Task.FromResult(new QuoteResult(
            UnderwriterName: nameof(AvivaScheme),
            PremiumAmount: premium,
            RiskRating: multiplier > 1.00m ? "Medium" : "Low",
            Region: property.Region));
    }
}
