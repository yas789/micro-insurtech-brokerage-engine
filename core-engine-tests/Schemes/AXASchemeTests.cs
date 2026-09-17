using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Schemes;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Schemes;

public sealed class AXASchemeTests
{
    [Fact]
    public async Task CalculatePremiumAsync_ReturnsBasePremiumForOccupiedProperty()
    {
        var scheme = new AXAScheme();
        var property = new PropertyEvaluationDto("M1 1AE", 1985, 250000.00m, false, "North West");

        var result = await scheme.CalculatePremiumAsync(property);

        Assert.Equal("AXAScheme", result.UnderwriterName);
        Assert.Equal(180.00m, result.PremiumAmount);
        Assert.Equal("Low", result.RiskRating);
        Assert.Equal("North West", result.Region);
    }

    [Fact]
    public async Task CalculatePremiumAsync_AppliesUnoccupiedMultiplier()
    {
        var scheme = new AXAScheme();
        var property = new PropertyEvaluationDto("M1 1AE", 1985, 250000.00m, true, "North West");

        var result = await scheme.CalculatePremiumAsync(property);

        Assert.Equal(270.00m, result.PremiumAmount);
        Assert.Equal("High", result.RiskRating);
    }
}
