using Catalog.Core.Common;
using Catalog.Core.Entities;
using FluentAssertions;

namespace Catalog.UnitTests;

public class ProductTests
{
    [Fact]
    public void Create_Product_Should_Set_Properties_Correctly()
    {
        var price = new Money(99.99m, "USD");
        
        var product = new Product("Test Product", price, 10, "Electronics", "TEST123", "Test Description");
        
        product.Name.Should().Be("Test Product");
        product.Price.Amount.Should().Be(99.99m);
        product.Price.Currency.Should().Be("USD");
        product.StockQuantity.Should().Be(10);
        product.Category.Should().Be("Electronics");
        product.Sku.Should().Be("TEST123");
        product.Description.Should().Be("Test Description");
        product.IsActive.Should().BeTrue();
        product.Id.Should().NotBe(Guid.Empty);
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void UpdateStock_WithNegativeQuantity_ShouldThrowException()
    {
        var price = new Money(99.99m);
        var product = new Product("Test", price, 10, "Electronics", "TEST123");
        
        var action = () => product.UpdateStock(-5);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Stock quantity cannot be negative");
    }
    
    [Fact]
    public void UpdateStock_WithValidQuantity_ShouldUpdateStock()
    {
        var price = new Money(99.99m);
        var product = new Product("Test", price, 10, "Electronics", "TEST123");
        var originalUpdatedAt = product.UpdatedAt;
        
        product.UpdateStock(20);
        
        product.StockQuantity.Should().Be(20);
        product.UpdatedAt.Should().NotBeNull();
        product.UpdatedAt.Should().NotBe(originalUpdatedAt);
    }
    
    [Fact]
    public void Update_Product_Should_Update_Properties()
    {
        var originalPrice = new Money(99.99m);
        var newPrice = new Money(149.99m);
        var product = new Product("Old Name", originalPrice, 10, "Old Category", "OLD123");
        
        product.Update("New Name", newPrice, "New Category", "NEW123", "New Description");
        
        product.Name.Should().Be("New Name");
        product.Price.Should().Be(newPrice);
        product.Category.Should().Be("New Category");
        product.Sku.Should().Be("NEW123");
        product.Description.Should().Be("New Description");
        product.UpdatedAt.Should().NotBeNull();
    }
}