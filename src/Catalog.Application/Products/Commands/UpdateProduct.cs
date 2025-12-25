using Catalog.Application.Interfaces;
using Catalog.Core.Common;
using Catalog.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products.Commands;

public record UpdateProductCommand : IRequest
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; } = "USD";
    public string Category { get; init; } = null!;
    public string Sku { get; init; } = null!;
    public bool IsActive { get; init; } = true;
    public int? StockQuantity { get; init; } 
}

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
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

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU must not exceed 50 characters");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StockQuantity.HasValue)
            .WithMessage("Stock quantity must be 0 or greater");
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache; 
    
    public UpdateProductCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }
    
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
            
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {request.Id} not found");
            
        if (product.Sku != request.Sku)
        {
            var skuExists = await _context.Products
                .AnyAsync(p => p.Sku == request.Sku && p.Id != request.Id, cancellationToken);
                
            if (skuExists)
                throw new InvalidOperationException($"Product with SKU '{request.Sku}' already exists.");
        }
        
        var price = new Money(request.PriceAmount, request.PriceCurrency);

        var categoryId = await GetOrCreateCategoryIdAsync(
            request.Category,
            cancellationToken);

        product.Update(
            request.Name,
            price,
            categoryId,
            request.Sku,
            request.Description);


            
        product.SetActive(request.IsActive);
        
        if (request.StockQuantity.HasValue)
        {
            product.UpdateStock(request.StockQuantity.Value);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        
        await _cache.RemoveAsync($"product_{request.Id}", cancellationToken);
        await _cache.RemoveAsync("products_", cancellationToken);
    }
    
    private async Task<Guid> GetOrCreateCategoryIdAsync(
        string categoryName,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken);

        if (category != null)
            return category.Id;

        var newCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = categoryName
        };

        _context.Categories.Add(newCategory);

        return newCategory.Id;
    }

}