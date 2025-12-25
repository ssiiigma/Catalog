using System.ComponentModel.DataAnnotations.Schema;
using Catalog.Core.Common;

namespace Catalog.Core.Entities;

public sealed class Product : BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [NotMapped]
    public Money Price { get; set; } = null!;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public string Sku { get; set; } = string.Empty;
    
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    private Product() { } 
    
    public Product(
        string name,
        Money price,
        int stockQuantity,
        Guid categoryId,
        string sku,
        string? description = null)
    {
        Name = name;
        Price = price ?? throw new ArgumentNullException(nameof(price));
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
        Sku = sku;
        Description = description ?? string.Empty;
    }

    
    public void Update(
        string name,
        Money price,
        Guid categoryId,
        string sku,
        string? description = null)
    {
        Name = name;
        Price = price;
        CategoryId = categoryId;
        Sku = sku;
        Description = description ?? string.Empty;
        UpdateTimestamps();
    }

    
    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative");
            
        StockQuantity = quantity;
        UpdateTimestamps();
    }
    
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamps();
    }
}