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

    [Fact]
    public async Task CalculatePremiumAsync_AppliesPre1920Multiplier()
    {
        var scheme = new AvivaScheme();
        var property = new PropertyEvaluationDto("SW1A 1AA", 1910, 250000.00m, false, "London");

        var result = await scheme.CalculatePremiumAsync(property);

        Assert.Equal(210.00m, result.PremiumAmount);
        Assert.Equal("Medium", result.RiskRating);
    }
}
