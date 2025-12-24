using Catalog.Application.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products.Commands;

public record DeleteProductCommand : IRequest
{
    public Guid Id { get; init; }
}

public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required");
    }
}

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;
    
    public DeleteProductCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }
    
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
            
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {request.Id} not found");
        
        product.SetActive(false);
        await _context.SaveChangesAsync(cancellationToken);
        
        await _cache.RemoveAsync($"product_{request.Id}", cancellationToken);
        
        var cacheKeys = new[] { "products_", $"products_page_" };
        foreach (var pattern in cacheKeys)
        {
            await _cache.RemoveAsync(pattern, cancellationToken);
        }
    }
}