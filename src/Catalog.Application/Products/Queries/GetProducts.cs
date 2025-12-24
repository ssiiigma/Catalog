using Catalog.Application.Common.Models;
using Catalog.Application.Interfaces;
using Catalog.Application.Products.Dtos;
using Dapper;
using MediatR;

namespace Catalog.Application.Products.Queries;

public record GetProductsQuery : IRequest<PaginatedResult<ProductDto>>
{
    public string? Category { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginatedResult<ProductDto>>
{
    private readonly IDapperContext _dapperContext;
    private readonly ICacheService _cache;
    
    public GetProductsQueryHandler(IDapperContext dapperContext, ICacheService cache)
    {
        _dapperContext = dapperContext;
        _cache = cache;
    }
    
    public async Task<PaginatedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"products_{request.Category}_{request.MinPrice}_{request.MaxPrice}_{request.IsActive}_{request.PageNumber}_{request.PageSize}";
        
        var cachedData = await _cache.GetAsync<PaginatedResult<ProductDto>>(cacheKey, cancellationToken);
        if (cachedData != null)
            return cachedData;
        
        var countQuery = @"
            SELECT COUNT(*)
            FROM Products
            WHERE (@Category IS NULL OR Category = @Category)
              AND (@MinPrice IS NULL OR PriceAmount >= @MinPrice)
              AND (@MaxPrice IS NULL OR PriceAmount <= @MaxPrice)
              AND (@IsActive IS NULL OR IsActive = @IsActive)";
        
        var dataQuery = @"
            SELECT 
                Id, Name, Description, 
                PriceAmount, PriceCurrency,
                StockQuantity, Category, Sku, IsActive,
                CreatedAt, UpdatedAt
            FROM Products
            WHERE (@Category IS NULL OR Category = @Category)
              AND (@MinPrice IS NULL OR PriceAmount >= @MinPrice)
              AND (@MaxPrice IS NULL OR PriceAmount <= @MaxPrice)
              AND (@IsActive IS NULL OR IsActive = @IsActive)
            ORDER BY CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset";
        
        var offset = (request.PageNumber - 1) * request.PageSize;
        
        using var connection = _dapperContext.CreateConnection();
        
        var totalCount = await connection.ExecuteScalarAsync<int>(countQuery, new
        {
            request.Category,
            request.MinPrice,
            request.MaxPrice,
            request.IsActive
        });
        
        var products = await connection.QueryAsync<ProductDto>(dataQuery, new
        {
            request.Category,
            request.MinPrice,
            request.MaxPrice,
            request.IsActive,
            request.PageSize,
            Offset = offset
        });
        
        var result = new PaginatedResult<ProductDto>(
            products.ToList(), 
            totalCount, 
            request.PageNumber, 
            request.PageSize);
        
        await _cache.SetAsync(cacheKey, result,
            new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            },
            cancellationToken);
        
        return result;
    }
}