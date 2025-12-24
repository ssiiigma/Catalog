namespace Catalog.Application.Products.Dtos;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "USD";
    public int StockQuantity { get; set; }
    public string Category { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}