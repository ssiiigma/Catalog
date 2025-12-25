using Catalog.Core.Common;
using Catalog.Core.Entities;
using FluentAssertions;

namespace Catalog.UnitTests;

public class ProductTests
{
    private readonly Guid _testCategoryId = Guid.NewGuid();

    [Fact]
    public void Create_Product_Should_Set_Properties_Correctly()
    {
        var price = new Money(99.99m, "USD");

        var product = new Product(
            name: "Test Product",
            price: price,
            stockQuantity: 10,
            categoryId: _testCategoryId,
            sku: "TEST123",
            description: "Test Description");

        product.Name.Should().Be("Test Product");
        product.Price.Amount.Should().Be(99.99m);
        product.Price.Currency.Should().Be("USD");
        product.StockQuantity.Should().Be(10);
        product.CategoryId.Should().Be(_testCategoryId);
        product.Sku.Should().Be("TEST123");
        product.Description.Should().Be("Test Description");
        product.IsActive.Should().BeTrue();
        product.Id.Should().NotBe(Guid.Empty);
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        product.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateStock_WithNegativeQuantity_ShouldThrowException()
    {
        var product = CreateValidProduct();

        var action = () => product.UpdateStock(-5);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Stock quantity cannot be negative");
    }

    [Fact]
    public void UpdateStock_WithValidQuantity_ShouldUpdateStock()
    {
        var product = CreateValidProduct();
        var originalUpdatedAt = product.UpdatedAt;

        product.UpdateStock(20);

        product.StockQuantity.Should().Be(20);
        product.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateProduct_ShouldUpdateProperties()
    {
        var product = CreateValidProduct();

        var newPrice = new Money(150.00m, "USD");
        var newCategoryId = Guid.NewGuid();

        product.Update(
            name: "New Name",
            price: newPrice,
            categoryId: newCategoryId,
            sku: "NEW123",
            description: "New Description");

        product.Name.Should().Be("New Name");
        product.Price.Should().Be(newPrice);
        product.CategoryId.Should().Be(newCategoryId);
        product.Sku.Should().Be("NEW123");
        product.Description.Should().Be("New Description");
        product.UpdatedAt.Should().BeAfter(product.CreatedAt);
    }

    [Fact]
    public void SetActive_ShouldUpdateIsActiveStatus()
    {
        var product = CreateValidProduct();
        var originalUpdatedAt = product.UpdatedAt;

        product.SetActive(false);

        product.IsActive.Should().BeFalse();
        product.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void CreateProduct_WithNullPrice_ShouldThrowException()
    {
        var action = () => new Product(
            name: "Test Product",
            price: null!,
            stockQuantity: 10,
            categoryId: _testCategoryId,
            sku: "TEST123");

        action.Should().Throw<ArgumentNullException>()
            .WithMessage("*price*");
    }

    [Fact]
    public void UpdateProduct_WithNullPrice_ShouldThrowException()
    {
        var product = CreateValidProduct();

        var action = () => product.Update(
            name: "New Name",
            price: null!,
            categoryId: Guid.NewGuid(),
            sku: "NEW123",
            description: "New Description");

        action.Should().Throw<ArgumentNullException>()
            .WithMessage("*price*");
    }

    [Fact]
    public void CreateProduct_WithEmptyName_ShouldThrowException()
    {
        var price = new Money(10, "USD");

        var action = () => new Product(
            name: "",
            price: price,
            stockQuantity: 10,
            categoryId: _testCategoryId,
            sku: "SKU");

        action.Should().Throw<ArgumentNullException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void CreateProduct_WithEmptySku_ShouldThrowException()
    {
        var price = new Money(10, "USD");

        var action = () => new Product(
            name: "Test",
            price: price,
            stockQuantity: 10,
            categoryId: _testCategoryId,
            sku: "");

        action.Should().Throw<ArgumentNullException>()
            .WithMessage("*sku*");
    }

    // ===== Helper =====
    private Product CreateValidProduct()
    {
        return new Product(
            name: "Test Product",
            price: new Money(100, "USD"),
            stockQuantity: 10,
            categoryId: _testCategoryId,
            sku: "TEST123",
            description: "Test Description");
    }
}
