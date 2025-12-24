namespace Catalog.Application.Interfaces;

public interface ICacheInvalidationService
{
    Task InvalidateProductCache(Guid productId, CancellationToken cancellationToken = default);
    Task InvalidateProductsListCache(CancellationToken cancellationToken = default);
}