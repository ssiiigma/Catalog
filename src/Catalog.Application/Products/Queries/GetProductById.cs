using Catalog.Application.Products.Dtos;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Catalog.Application.Interfaces;
using Dapper;

namespace Catalog.Application.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IDapperContext _dapperContext;
    private readonly ICacheService _cache;
    
    public GetProductByIdQueryHandler(IDapperContext dapperContext, ICacheService cache)
    {
        _dapperContext = dapperContext;
        _cache = cache;
    }
    
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"product_{request.Id}";
        
        var cachedProduct = await _cache.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cachedProduct != null)
            return cachedProduct;
        
        var query = @"
            SELECT 
                Id, Name, Description, 
                PriceAmount, PriceCurrency,
                StockQuantity, Category, Sku, IsActive,
                CreatedAt, UpdatedAt
            FROM Products 
            WHERE Id = @Id";
        
        using var connection = _dapperContext.CreateConnection();
        var product = await connection.QuerySingleOrDefaultAsync<ProductDto>(query, new { request.Id });
        
        if (product != null)
        {
            await _cache.SetAsync(cacheKey, product,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                },
                cancellationToken);
        }
        
        return product;
    }
}