using MicroInsurTech.CoreEngine.Dtos;
using MicroInsurTech.CoreEngine.Schemes;
using Xunit;

namespace MicroInsurTech.CoreEngine.Tests.Schemes;

public sealed class AvivaSchemeTests
{
    [Fact]
    public async Task CalculatePremiumAsync_ReturnsBasePremiumForModernProperty()
    {
        var scheme = new AvivaScheme();
        var property = new PropertyEvaluationDto("SW1A 1AA", 1985, 250000.00m, false, "London");

        var result = await scheme.CalculatePremiumAsync(property);

        Assert.Equal("AvivaScheme", result.UnderwriterName);
        Assert.Equal(150.00m, result.PremiumAmount);
        Assert.Equal("Low", result.RiskRating);
        Assert.Equal("London", result.Region);
    }

    [Theory]
    [InlineData(1919, 210, "Medium")]
    [InlineData(1920, 150, "Low")]
    [InlineData(1921, 150, "Low")]
    public async Task CalculatePremiumAsync_AppliesMultiplierOnlyBefore1920(int yearBuilt, decimal premium, string risk)
    {
        var scheme = new AvivaScheme();
        var property = new PropertyEvaluationDto("SW1A 1AA", yearBuilt, 250000.00m, false, "London");

        var result = await scheme.CalculatePremiumAsync(property);

        Assert.Equal(premium, result.PremiumAmount);
        Assert.Equal(risk, result.RiskRating);
    }
}
