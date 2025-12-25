using Catalog.Application.Interfaces;
using Catalog.Application.Services;
using Catalog.Core.Entities;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductService> _logger;
    
    public ProductService(
        IProductRepository productRepository,
        ICacheService cacheService,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    public async Task<IEnumerable<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "products_all";
        
        var cachedProducts = await _cacheService.GetAsync<List<Product>>(cacheKey, cancellationToken);
        if (cachedProducts != null)
        {
            _logger.LogDebug("Products retrieved from cache");
            return cachedProducts;
        }
        
        _logger.LogDebug("Fetching products from database");
        var products = (await _productRepository.GetAllAsync(cancellationToken)).ToList();
        
        if (products.Any())
        {
            await _cacheService.SetAsync(cacheKey, products, TimeSpan.FromMinutes(2), cancellationToken);
            _logger.LogDebug("Products cached for 2 minutes");
        }
        
        return products;
    }
    
    public async Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product_{id}";
        
        var cachedProduct = await _cacheService.GetAsync<Product>(cacheKey, cancellationToken);
        if (cachedProduct != null)
        {
            _logger.LogDebug("Product {Id} retrieved from cache", id);
            return cachedProduct;
        }
        
        _logger.LogDebug("Fetching product {Id} from database", id);
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        
        if (product != null)
        {
            await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(5), cancellationToken);
            _logger.LogDebug("Product {Id} cached for 5 minutes", id);
        }
        
        return product;
    }
    
    public async Task<Product> CreateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new product: {Name}", product.Name);
        var createdProduct = await _productRepository.AddAsync(product, cancellationToken);
        
        await _cacheService.RemoveAsync("products_all", cancellationToken);
        _logger.LogDebug("Cache invalidated for all products after creation");
        
        return createdProduct;
    }
    
    public async Task<Product?> UpdateProductAsync(Guid id, Product product, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating product {Id}", id);
        var existingProduct = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (existingProduct == null)
        {
            _logger.LogWarning("Product {Id} not found for update", id);
            return null;
        }
        
        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.UpdatedAt = DateTime.UtcNow;
        
        await _productRepository.UpdateAsync(existingProduct, cancellationToken);
        
        await _cacheService.RemoveAsync($"product_{id}", cancellationToken);
        await _cacheService.RemoveAsync("products_all", cancellationToken);
        _logger.LogDebug("Cache invalidated for product {Id} and all products", id);
        
        return existingProduct;
    }
    
    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting product {Id}", id);
        var exists = await _productRepository.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            _logger.LogWarning("Product {Id} not found for deletion", id);
            return false;
        }
        
        await _productRepository.DeleteAsync(id, cancellationToken);
        
        await _cacheService.RemoveAsync($"product_{id}", cancellationToken);
        await _cacheService.RemoveAsync("products_all", cancellationToken);
        _logger.LogDebug("Cache invalidated for deleted product {Id}", id);
        
        return true;
    }
}