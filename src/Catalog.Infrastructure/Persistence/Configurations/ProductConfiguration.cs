using Catalog.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.Property(p => p.Description)
            .HasMaxLength(1000);
        
        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(18, 2)
                .IsRequired();
                
            price.Property(m => m.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsRequired()
                .HasDefaultValue("USD");
        });
        
        builder.Property(p => p.StockQuantity)
            .IsRequired();
            
        builder.Property(p => p.Category)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(p => p.Sku)
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(p => p.CreatedAt)
            .IsRequired();
            
        builder.Property(p => p.UpdatedAt);
            
        builder.HasIndex(p => p.Sku)
            .IsUnique();
            
        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.CreatedAt);
    }
}