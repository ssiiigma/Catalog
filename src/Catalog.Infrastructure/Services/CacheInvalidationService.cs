using Catalog.Application.Interfaces;

namespace Catalog.Infrastructure.Services;

public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cache;
    
    public CacheInvalidationService(ICacheService cache)
    {
        _cache = cache;
    }
    
    public async Task InvalidateProductCache(Guid productId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync($"product_{productId}", cancellationToken);
    }
    
    public async Task InvalidateProductsListCache(CancellationToken cancellationToken = default)
    {
        var cacheKeys = new[] 
        { 
            "products_",
            "products_page_",
            "products_list_"
        };
        
        foreach (var pattern in cacheKeys)
        {
            await _cache.RemoveAsync(pattern, cancellationToken);
        }
    }
}