using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Services;

namespace MicroInsurTech.CoreEngine.Schemes;

public sealed class NichePropertyCover : IUnderwriterService
{
    private const decimal BasePremium = 120.00m;
    private const decimal HighRebuildCostMultiplier = 1.30m;
    private const decimal HighRebuildCostThreshold = 500000.00m;

    public Task<QuoteResult> CalculatePremiumAsync(PropertyEvaluationDto property)
    {
        var multiplier = property.RebuildCost > HighRebuildCostThreshold ? HighRebuildCostMultiplier : 1.00m;
        var premium = decimal.Round(BasePremium * multiplier, 2, MidpointRounding.AwayFromZero);

        return Task.FromResult(new QuoteResult(
            UnderwriterName: nameof(NichePropertyCover),
            PremiumAmount: premium,
            RiskRating: multiplier > 1.00m ? "Medium" : "Low",
            Region: property.Region));
    }
}
