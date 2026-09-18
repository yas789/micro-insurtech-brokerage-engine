namespace MicroInsurTech.CoreEngine.Data.Entities;

public sealed class QuoteEntity
{
    public int QuoteId { get; set; }

    public int PropertyId { get; set; }

    public required string UnderwriterName { get; set; }

    public decimal PremiumAmount { get; set; }

    public required string RiskRating { get; set; }

    public string? Region { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public PropertyEntity? Property { get; set; }
}
