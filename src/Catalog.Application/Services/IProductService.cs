using Catalog.Core.Entities;

namespace Catalog.Application.Services;

public interface IProductService
{
    Task<IEnumerable<Product>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product> CreateProductAsync(Product product, CancellationToken cancellationToken = default);
    Task<Product?> UpdateProductAsync(Guid id, Product product, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}