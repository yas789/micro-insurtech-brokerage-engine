namespace MicroInsurTech.CoreEngine.Data.Entities;

public sealed class PropertyEntity
{
    public int PropertyId { get; set; }

    public int ClientId { get; set; }

    public required string Postcode { get; set; }

    public string? Region { get; set; }

    public int YearBuilt { get; set; }

    public decimal RebuildCost { get; set; }

    public bool IsUnoccupied { get; set; }

    public ClientEntity? Client { get; set; }

    public ICollection<QuoteEntity> Quotes { get; } = new List<QuoteEntity>();
}
