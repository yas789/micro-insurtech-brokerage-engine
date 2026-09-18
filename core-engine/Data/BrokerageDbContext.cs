using MicroInsurTech.CoreEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroInsurTech.CoreEngine.Data;

public sealed class BrokerageDbContext(DbContextOptions<BrokerageDbContext> options) : DbContext(options)
{
    public DbSet<ClientEntity> Clients => Set<ClientEntity>();

    public DbSet<PropertyEntity> Properties => Set<PropertyEntity>();

    public DbSet<QuoteEntity> Quotes => Set<QuoteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientEntity>(entity =>
        {
            entity.ToTable("Clients", "dbo");
            entity.HasKey(client => client.ClientId);
            entity.Property(client => client.ClientId).HasColumnName("ClientID");
            entity.Property(client => client.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(client => client.LastName).HasMaxLength(100).IsRequired();
            entity.Property(client => client.Email).HasMaxLength(254).IsRequired();
            entity.Property(client => client.CreatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<PropertyEntity>(entity =>
        {
            entity.ToTable("Properties", "dbo");
            entity.HasKey(property => property.PropertyId);
            entity.Property(property => property.PropertyId).HasColumnName("PropertyID");
            entity.Property(property => property.ClientId).HasColumnName("ClientID");
            entity.Property(property => property.Postcode).HasMaxLength(16).IsRequired();
            entity.Property(property => property.Region).HasMaxLength(100);
            entity.Property(property => property.RebuildCost).HasPrecision(10, 2);
            entity.HasOne(property => property.Client)
                .WithMany(client => client.Properties)
                .HasForeignKey(property => property.ClientId);
        });

        modelBuilder.Entity<QuoteEntity>(entity =>
        {
            entity.ToTable("Quotes", "dbo");
            entity.HasKey(quote => quote.QuoteId);
            entity.Property(quote => quote.QuoteId).HasColumnName("QuoteID");
            entity.Property(quote => quote.PropertyId).HasColumnName("PropertyID");
            entity.Property(quote => quote.UnderwriterName).HasMaxLength(100).IsRequired();
            entity.Property(quote => quote.PremiumAmount).HasPrecision(10, 2);
            entity.Property(quote => quote.RiskRating).HasMaxLength(20).IsRequired();
            entity.Property(quote => quote.Region).HasMaxLength(100);
            entity.Property(quote => quote.GeneratedAt).HasPrecision(0);
            entity.HasOne(quote => quote.Property)
                .WithMany(property => property.Quotes)
                .HasForeignKey(quote => quote.PropertyId);
        });
    }
}
