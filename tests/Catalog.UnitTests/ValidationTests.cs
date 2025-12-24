using Catalog.Application.Products.Commands;
using FluentValidation.TestHelper;
namespace Catalog.UnitTests;

public class ValidationTests
{
    private readonly CreateProductCommandValidator _createValidator;
    private readonly UpdateProductCommandValidator _updateValidator;
    private readonly DeleteProductCommandValidator _deleteValidator;

    public ValidationTests()
    {
        _createValidator = new CreateProductCommandValidator();
        _updateValidator = new UpdateProductCommandValidator();
        _deleteValidator = new DeleteProductCommandValidator();
    }

    [Fact]
    public void CreateProductCommand_ValidData_ShouldPassValidation()
    {
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            PriceAmount = 99.99m,
            PriceCurrency = "USD",
            StockQuantity = 10,
            Category = "Electronics",
            Sku = "TEST123"
        };

        var result = _createValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateProductCommand_EmptyName_ShouldFailValidation()
    {
        var command = new CreateProductCommand
        {
            Name = "", 
            PriceAmount = 99.99m,
            PriceCurrency = "USD",
            StockQuantity = 10,
            Category = "Electronics",
            Sku = "TEST123"
        };

        var result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void UpdateProductCommand_ValidData_ShouldPassValidation()
    {
        var command = new UpdateProductCommand
        {
            Id = Guid.NewGuid(),
            Name = "Updated Product",
            PriceAmount = 149.99m,
            PriceCurrency = "USD",
            Category = "Electronics",
            Sku = "UPD123"
        };

        var result = _updateValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DeleteProductCommand_EmptyId_ShouldFailValidation()
    {
        var command = new DeleteProductCommand
        {
            Id = Guid.Empty 
        };

        var result = _deleteValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}