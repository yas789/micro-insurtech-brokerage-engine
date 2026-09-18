namespace MicroInsurTech.CoreEngine.Data.Entities;

public sealed class ClientEntity
{
    public int ClientId { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PropertyEntity> Properties { get; } = new List<PropertyEntity>();
}
