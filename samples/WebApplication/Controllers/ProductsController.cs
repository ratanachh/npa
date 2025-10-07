using Microsoft.AspNetCore.Mvc;

namespace WebApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ILogger<ProductsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets all products
    /// </summary>
    /// <returns>List of products</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        _logger.LogInformation("Getting all products");
        
        // TODO: Use NPA to fetch products from database
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Sample Product 1", Price = 29.99m, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Sample Product 2", Price = 39.99m, CreatedAt = DateTime.UtcNow }
        };
        
        return Ok(products);
    }

    /// <summary>
    /// Gets a specific product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        _logger.LogInformation("Getting product with ID {ProductId}", id);
        
        // TODO: Use NPA to fetch product by ID
        var product = new Product 
        { 
            Id = id, 
            Name = $"Sample Product {id}", 
            Price = 29.99m, 
            CreatedAt = DateTime.UtcNow 
        };
        
        return Ok(product);
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="product">Product to create</param>
    /// <returns>Created product</returns>
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
    {
        _logger.LogInformation("Creating new product: {ProductName}", product.Name);
        
        // TODO: Use NPA to save product to database
        product.Id = Random.Shared.Next(1000, 9999);
        product.CreatedAt = DateTime.UtcNow;
        
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="product">Updated product data</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> UpdateProduct(int id, [FromBody] Product product)
    {
        _logger.LogInformation("Updating product with ID {ProductId}", id);
        
        // TODO: Use NPA to update product in database
        product.Id = id;
        product.UpdatedAt = DateTime.UtcNow;
        
        return Ok(product);
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        _logger.LogInformation("Deleting product with ID {ProductId}", id);
        
        // TODO: Use NPA to delete product from database
        
        return NoContent();
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}