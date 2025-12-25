using Catalog.Core.Common;

namespace Catalog.Core.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public Money Price { get; set; } = null!;
    public int StockQuantity { get; private set; }
    public string Category { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    
    private Product() { } 
    
    public Product(string name, Money price, int stockQuantity, string category, string sku, string? description = null)
    {
        Name = name;
        Price = price ?? throw new ArgumentNullException(nameof(price));
        StockQuantity = stockQuantity;
        Category = category;
        Sku = sku;
        Description = description ?? string.Empty;
    }
    
    public void Update(string name, Money price, string category, string sku, string? description = null)
    {
        Name = name;
        Price = price;
        Category = category;
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