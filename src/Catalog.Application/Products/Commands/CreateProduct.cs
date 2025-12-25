using Catalog.Application.Interfaces;
using Catalog.Core.Common;
using Catalog.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products.Commands;

public record CreateProductCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; } = "USD";
    public int StockQuantity { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
}

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.PriceAmount)
            .GreaterThan(0).WithMessage("Price must be greater than 0");

        RuleFor(x => x.PriceCurrency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be 3 characters");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be 0 or greater");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU must not exceed 50 characters");
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheInvalidationService _cacheInvalidation;
    
    public CreateProductCommandHandler(
        IApplicationDbContext context,
        ICacheInvalidationService cacheInvalidation)
    {
        _context = context;
        _cacheInvalidation = cacheInvalidation;
    }
    
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var skuExists = await _context.Products
            .AnyAsync(p => p.Sku == request.Sku, cancellationToken);
        
        if (skuExists)
        {
            throw new InvalidOperationException($"Product with SKU '{request.Sku}' already exists.");
        }
    
        var categoryId = await GetOrCreateCategoryIdAsync(request.Category, cancellationToken);
    
        var price = new Money(request.PriceAmount, request.PriceCurrency);
    
        var product = new Product(
            name: request.Name,
            price: price,
            stockQuantity: request.StockQuantity,
            categoryId: categoryId,
            sku: request.Sku,
            description: request.Description);

    
        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    
        await _cacheInvalidation.InvalidateProductsListCache(cancellationToken);
    
        return product.Id;
    }
    
    private async Task<Guid> GetOrCreateCategoryIdAsync(string categoryName, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken);
    
        if (category != null)
        {
            return category.Id;
        }
    
        return Guid.NewGuid();
    }
}