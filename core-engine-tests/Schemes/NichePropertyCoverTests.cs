using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Schemes;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Schemes;

public sealed class NichePropertyCoverTests
{
    [Theory]
    [InlineData(499999.99)]
    [InlineData(500000.00)]
    public async Task CalculatePremiumAsync_ReturnsBasePremiumAtOrBelowThreshold(decimal rebuildCost)
    {
        var scheme = new NichePropertyCover();
        var property = new PropertyEvaluationDto("EH1 1YZ", 1985, rebuildCost, false, "Scotland");

        var result = await scheme.CalculatePremiumAsync(property);

        Assert.Equal("NichePropertyCover", result.UnderwriterName);
        Assert.Equal(120.00m, result.PremiumAmount);
        Assert.Equal("Low", result.RiskRating);
        Assert.Equal("Scotland", result.Region);
    }

    [Fact]
    public async Task CalculatePremiumAsync_AppliesHighRebuildCostMultiplierAboveThreshold()
    {
        var scheme = new NichePropertyCover();
        var property = new PropertyEvaluationDto("EH1 1YZ", 1985, 500000.01m, false, "Scotland");

        var result = await scheme.CalculatePremiumAsync(property);

        Assert.Equal(156.00m, result.PremiumAmount);
        Assert.Equal("Medium", result.RiskRating);
    }
}
