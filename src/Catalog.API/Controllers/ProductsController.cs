using Catalog.Application.Services;
using Catalog.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;
    
    public ProductsController(
        IProductService productService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        _logger.LogInformation("Getting all products");
        
        try
        {
            var products = await _productService.GetProductsAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting product with ID: {Id}", id);
        
        try
        {
            var product = await _productService.GetProductAsync(id, cancellationToken);
            
            if (product == null)
            {
                _logger.LogWarning("Product {Id} not found", id);
                return NotFound();
            }
            
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(
        [FromBody] Product product,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new product");
        
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid product data received");
                return BadRequest(ModelState);
            }
            
            var createdProduct = await _productService.CreateProductAsync(product, cancellationToken);
            
            return CreatedAtAction(
                nameof(GetProduct),
                new { id = createdProduct.Id },
                createdProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] Product product,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating product {Id}", id);
        
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid product data received for update");
                return BadRequest(ModelState);
            }
            
            var updatedProduct = await _productService.UpdateProductAsync(id, product, cancellationToken);
            
            if (updatedProduct == null)
            {
                _logger.LogWarning("Product {Id} not found for update", id);
                return NotFound();
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting product {Id}", id);
        
        try
        {
            var deleted = await _productService.DeleteProductAsync(id, cancellationToken);
            
            if (!deleted)
            {
                _logger.LogWarning("Product {Id} not found for deletion", id);
                return NotFound();
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}