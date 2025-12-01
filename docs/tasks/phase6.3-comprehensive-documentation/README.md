# Phase 6.4: Comprehensive Documentation

## 📋 Task Overview

**Objective**: Create comprehensive documentation for the NPA library including API reference, tutorials, guides, and examples to help developers effectively use the library.

**Priority**: Low  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.5, Phase 4.1-4.6, Phase 5.1-5.5, Phase 6.1-6.2 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] API reference documentation is complete
- [ ] Tutorial documentation is complete
- [ ] Guide documentation is complete
- [ ] Example documentation is complete
- [ ] Documentation is well-organized
- [ ] Documentation is searchable

## 📝 Detailed Requirements

### 1. API Reference Documentation
- **Purpose**: Complete reference for all public APIs
- **Content**:
  - Class documentation
  - Method documentation
  - Property documentation
  - Parameter descriptions
  - Return value descriptions
  - Exception documentation
  - Usage examples

### 2. Tutorial Documentation
- **Purpose**: Step-by-step tutorials for common scenarios
- **Content**:
  - Getting started tutorial
  - Basic CRUD operations tutorial
  - Advanced features tutorial
  - Performance optimization tutorial
  - Best practices tutorial

### 3. Guide Documentation
- **Purpose**: Comprehensive guides for specific topics
- **Content**:
  - Entity mapping guide
  - Repository pattern guide
  - Query language guide
  - Transaction management guide
  - Performance monitoring guide
  - Migration guide

### 4. Example Documentation
- **Purpose**: Real-world examples and code samples
- **Content**:
  - Basic examples
  - Advanced examples
  - Integration examples
  - Performance examples
  - Troubleshooting examples

### 5. Documentation Organization
- **Structure**: Well-organized documentation structure
- **Navigation**: Easy navigation between sections
- **Search**: Searchable documentation
- **Cross-references**: Cross-references between related topics

## 🏗️ Implementation Plan

### Step 1: Create Documentation Structure
1. Create documentation directory structure
2. Create documentation templates
3. Set up documentation generation
4. Configure documentation hosting

### Step 2: Create API Reference Documentation
1. Generate API reference from code
2. Add detailed descriptions
3. Add usage examples
4. Add cross-references

### Step 3: Create Tutorial Documentation
1. Create getting started tutorial
2. Create basic operations tutorial
3. Create advanced features tutorial
4. Create performance tutorial

### Step 4: Create Guide Documentation
1. Create entity mapping guide
2. Create repository pattern guide
3. Create query language guide
4. Create transaction management guide

### Step 5: Create Example Documentation
1. Create basic examples
2. Create advanced examples
3. Create integration examples
4. Create performance examples

### Step 6: Organize Documentation
1. Create navigation structure
2. Add cross-references
3. Implement search functionality
4. Test documentation

### Step 7: Publish Documentation
1. Generate documentation
2. Deploy documentation
3. Test documentation
4. Update documentation

## 📁 File Structure

```
docs/
├── api/
│   ├── NPA.Core/
│   │   ├── EntityManager.md
│   │   ├── IRepository.md
│   │   ├── IQueryBuilder.md
│   │   └── ...
│   ├── NPA.Generators/
│   │   ├── EntityMetadataGenerator.md
│   │   ├── RepositoryGenerator.md
│   │   └── ...
│   └── NPA.Extensions/
│       ├── ServiceCollectionExtensions.md
│       └── ...
├── tutorials/
│   ├── getting-started.md
│   ├── basic-crud-operations.md
│   ├── advanced-features.md
│   ├── performance-optimization.md
│   └── best-practices.md
├── guides/
│   ├── entity-mapping.md
│   ├── repository-pattern.md
│   ├── query-language.md
│   ├── transaction-management.md
│   ├── performance-monitoring.md
│   └── migration.md
├── examples/
│   ├── basic/
│   │   ├── simple-crud.md
│   │   ├── entity-mapping.md
│   │   └── repository-usage.md
│   ├── advanced/
│   │   ├── complex-queries.md
│   │   ├── bulk-operations.md
│   │   └── performance-optimization.md
│   ├── integration/
│   │   ├── asp-net-core.md
│   │   ├── dependency-injection.md
│   │   └── unit-testing.md
│   └── troubleshooting/
│       ├── common-issues.md
│       ├── performance-issues.md
│       └── error-handling.md
├── reference/
│   ├── configuration.md
│   ├── attributes.md
│   ├── exceptions.md
│   └── conventions.md
└── index.md
```

## 💻 Code Examples

### API Reference Documentation Template
```markdown
# EntityManager Class

The `EntityManager` class provides the main interface for entity lifecycle management in NPA.

## Namespace
`NPA.Core`

## Assembly
`NPA.Core.dll`

## Syntax
```csharp
public class EntityManager : IEntityManager
```

## Remarks
The `EntityManager` class is the central component of NPA that manages entity persistence, retrieval, and lifecycle operations. It provides a JPA-like API while leveraging Dapper for high-performance data access.

## Constructors

### EntityManager(IDbConnection, IMetadataProvider, IChangeTracker)
Initializes a new instance of the `EntityManager` class.

```csharp
public EntityManager(
    IDbConnection connection,
    IMetadataProvider metadataProvider,
    IChangeTracker changeTracker)
```

#### Parameters
- `connection`: The database connection to use for data access.
- `metadataProvider`: The metadata provider for entity information.
- `changeTracker`: The change tracker for entity state management.

#### Exceptions
- `ArgumentNullException`: Thrown when any parameter is null.

#### Example
```csharp
// Recommended: Use DI with provider extensions
var services = new ServiceCollection();
services.AddSqlServerProvider(connectionString);
var provider = services.BuildServiceProvider();
var entityManager = provider.GetRequiredService<IEntityManager>();

// Alternative: Manual setup (not recommended for production)
var connection = new SqlConnection(connectionString);
var services = new ServiceCollection();
services.AddNpaMetadataProvider(); // Uses generated provider if available
services.AddSingleton<IDatabaseProvider, SqlServerProvider>();
services.AddSingleton<IDbConnection>(connection);
services.AddScoped<IEntityManager, EntityManager>();
var provider = services.BuildServiceProvider();
var entityManager = provider.GetRequiredService<IEntityManager>();
```

## Methods

### FindAsync<T>(object)
Finds an entity by its primary key.

```csharp
public async Task<T?> FindAsync<T>(object id) where T : class
```

#### Parameters
- `id`: The primary key value of the entity to find.

#### Returns
A `Task<T?>` that represents the asynchronous operation. The task result contains the entity if found, or null if not found.

#### Exceptions
- `ArgumentNullException`: Thrown when `id` is null.
- `InvalidOperationException`: Thrown when the entity type is not mapped.

#### Example
```csharp
var user = await entityManager.FindAsync<User>(1);
if (user != null)
{
    Console.WriteLine($"Found user: {user.Username}");
}
```

### PersistAsync<T>(T)
Persists a new entity to the database.

```csharp
public async Task PersistAsync<T>(T entity) where T : class
```

#### Parameters
- `entity`: The entity to persist.

#### Returns
A `Task` that represents the asynchronous operation.

#### Exceptions
- `ArgumentNullException`: Thrown when `entity` is null.
- `InvalidOperationException`: Thrown when the entity type is not mapped.

#### Example
```csharp
var user = new User
{
    Username = "john_doe",
    Email = "john@example.com",
    CreatedAt = DateTime.UtcNow
};

await entityManager.PersistAsync(user);
await entityManager.FlushAsync();
```

### MergeAsync<T>(T)
Merges an existing entity with the database.

```csharp
public async Task MergeAsync<T>(T entity) where T : class
```

#### Parameters
- `entity`: The entity to merge.

#### Returns
A `Task` that represents the asynchronous operation.

#### Exceptions
- `ArgumentNullException`: Thrown when `entity` is null.
- `InvalidOperationException`: Thrown when the entity type is not mapped.

#### Example
```csharp
var user = await entityManager.FindAsync<User>(1);
if (user != null)
{
    user.Email = "newemail@example.com";
    await entityManager.MergeAsync(user);
    await entityManager.FlushAsync();
}
```

### RemoveAsync<T>(T)
Removes an entity from the database.

```csharp
public async Task RemoveAsync<T>(T entity) where T : class
```

#### Parameters
- `entity`: The entity to remove.

#### Returns
A `Task` that represents the asynchronous operation.

#### Exceptions
- `ArgumentNullException`: Thrown when `entity` is null.
- `InvalidOperationException`: Thrown when the entity type is not mapped.

#### Example
```csharp
var user = await entityManager.FindAsync<User>(1);
if (user != null)
{
    await entityManager.RemoveAsync(user);
    await entityManager.FlushAsync();
}
```

### FlushAsync()
Flushes all pending changes to the database.

```csharp
public async Task FlushAsync()
```

#### Returns
A `Task` that represents the asynchronous operation.

#### Example
```csharp
await entityManager.PersistAsync(user1);
await entityManager.PersistAsync(user2);
await entityManager.FlushAsync(); // Both users are saved in a single transaction
```

## Properties

### IsConnected
Gets a value indicating whether the entity manager is connected to the database.

```csharp
public bool IsConnected { get; }
```

#### Value
`true` if connected; otherwise, `false`.

#### Example
```csharp
if (entityManager.IsConnected)
{
    var user = await entityManager.FindAsync<User>(1);
}
```

## See Also
- [IRepository<T, TKey>](IRepository.md)
- [IQueryBuilder<T>](IQueryBuilder.md)
- [IMetadataProvider](IMetadataProvider.md)
- [Entity Mapping Guide](../guides/entity-mapping.md)
- [Repository Pattern Guide](../guides/repository-pattern.md)
```

### Tutorial Documentation Template
```markdown
# Getting Started with NPA

This tutorial will guide you through setting up NPA in your .NET application and performing basic operations.

## Prerequisites

- .NET 6.0 or later
- Visual Studio 2022 or later (or your preferred IDE)
- SQL Server (or another supported database)

## Step 1: Install NPA

First, install the NPA NuGet package in your project:

```bash
dotnet add package NPA
```

Or using Package Manager Console:

```powershell
Install-Package NPA
```

## Step 2: Configure NPA

Add NPA to your application's service collection:

```csharp
using NPA.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNPA(config =>
{
    config.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    config.DatabaseProvider = DatabaseProvider.SqlServer;
    config.EnableMigrations = true;
    config.EnableCaching = true;
});

var app = builder.Build();
```

## Step 3: Create Your First Entity

Create a simple entity class:

```csharp
using NPA.Core;

[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("username", Length = 50, IsUnique = true)]
    public string Username { get; set; }
    
    [Column("email", Length = 100, IsUnique = true)]
    public string Email { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
```

## Step 4: Create a Repository

Create a repository interface:

```csharp
using NPA.Core;

public interface IUserRepository : IRepository<User, long>
{
    Task<User> FindByUsernameAsync(string username);
    Task<IEnumerable<User>> FindActiveUsersAsync();
    Task<int> CountByStatusAsync(bool isActive);
}
```

The repository implementation will be automatically generated by NPA's source generator.

## Step 5: Use the Repository

Inject and use the repository in your service:

```csharp
public class UserService
{
    private readonly IUserRepository _userRepository;
    
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<User> CreateUserAsync(string username, string email)
    {
        var user = new User
        {
            Username = username,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _userRepository.AddAsync(user);
    }
    
    public async Task<User> GetUserAsync(long id)
    {
        return await _userRepository.GetByIdAsync(id);
    }
    
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _userRepository.FindActiveUsersAsync();
    }
}
```

## Step 6: Register Services

Register your services in the DI container:

```csharp
builder.Services.AddScoped<UserService>();
```

## Step 7: Use in Controller

Create a controller that uses your service:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    
    public UsersController(UserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(long id)
    {
        var user = await _userService.GetUserAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        
        return Ok(user);
    }
    
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request.Username, request.Email);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
    
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<User>>> GetActiveUsers()
    {
        var users = await _userService.GetActiveUsersAsync();
        return Ok(users);
    }
}
```

## Step 8: Run the Application

Start your application:

```bash
dotnet run
```

## Next Steps

- Learn about [Entity Mapping](../guides/entity-mapping.md)
- Explore [Repository Pattern](../guides/repository-pattern.md)
- Discover [Query Language](../guides/query-language.md)
- Check out [Advanced Features](../tutorials/advanced-features.md)

## Troubleshooting

If you encounter issues, check the [Troubleshooting Guide](../examples/troubleshooting/common-issues.md).
```

### Guide Documentation Template
```markdown
# Entity Mapping Guide

This guide explains how to map your C# classes to database tables using NPA's entity mapping features.

## Table of Contents

1. [Basic Entity Mapping](#basic-entity-mapping)
2. [Property Mapping](#property-mapping)
3. [Primary Keys](#primary-keys)
4. [Relationships](#relationships)
5. [Indexes](#indexes)
6. [Constraints](#constraints)
7. [Advanced Mapping](#advanced-mapping)

## Basic Entity Mapping

### Entity Attribute

The `[Entity]` attribute marks a class as an entity:

```csharp
[Entity]
public class User
{
    // Properties
}
```

### Table Attribute

The `[Table]` attribute specifies the database table name:

```csharp
[Entity]
[Table("users")]
public class User
{
    // Properties
}
```

You can also specify the schema:

```csharp
[Entity]
[Table("users", Schema = "dbo")]
public class User
{
    // Properties
}
```

## Property Mapping

### Column Attribute

The `[Column]` attribute maps a property to a database column:

```csharp
[Entity]
[Table("users")]
public class User
{
    [Column("user_id")]
    public long Id { get; set; }
    
    [Column("username", Length = 50)]
    public string Username { get; set; }
    
    [Column("email", Length = 100, IsUnique = true)]
    public string Email { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
```

### Column Properties

- `Name`: The column name in the database
- `Length`: Maximum length for string columns
- `Precision`: Precision for decimal columns
- `Scale`: Scale for decimal columns
- `IsNullable`: Whether the column allows null values
- `IsUnique`: Whether the column has a unique constraint
- `DefaultValue`: Default value for the column

## Primary Keys

### Id Attribute

The `[Id]` attribute marks a property as the primary key:

```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    [Column("user_id")]
    public long Id { get; set; }
}
```

### GeneratedValue Attribute

The `[GeneratedValue]` attribute specifies how the primary key is generated:

```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("user_id")]
    public long Id { get; set; }
}
```

### Generation Types

- `Identity`: Auto-incrementing integer
- `Sequence`: Database sequence
- `Table`: Table-based generation
- `None`: No automatic generation

### Composite Keys

For composite keys, mark multiple properties with `[Id]`:

```csharp
[Entity]
[Table("order_items")]
public class OrderItem
{
    [Id]
    [Column("order_id")]
    public long OrderId { get; set; }
    
    [Id]
    [Column("product_id")]
    public long ProductId { get; set; }
    
    [Column("quantity")]
    public int Quantity { get; set; }
}
```

## Relationships

### One-to-One

Use `[OneToOne]` for one-to-one relationships:

```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [OneToOne]
    [JoinColumn("profile_id")]
    public UserProfile Profile { get; set; }
}

[Entity]
[Table("user_profiles")]
public class UserProfile
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("first_name")]
    public string FirstName { get; set; }
    
    [Column("last_name")]
    public string LastName { get; set; }
    
    [OneToOne(MappedBy = "Profile")]
    public User User { get; set; }
}
```

### One-to-Many

Use `[OneToMany]` for one-to-many relationships:

```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [OneToMany(MappedBy = "User")]
    public ICollection<Order> Orders { get; set; }
}

[Entity]
[Table("orders")]
public class Order
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("order_date")]
    public DateTime OrderDate { get; set; }
    
    [ManyToOne]
    [JoinColumn("user_id")]
    public User User { get; set; }
}
```

### Many-to-Many

Use `[ManyToMany]` for many-to-many relationships:

```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [ManyToMany]
    [JoinTable("user_roles", 
        JoinColumns = new[] { "user_id" }, 
        InverseJoinColumns = new[] { "role_id" })]
    public ICollection<Role> Roles { get; set; }
}

[Entity]
[Table("roles")]
public class Role
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [ManyToMany(MappedBy = "Roles")]
    public ICollection<User> Users { get; set; }
}
```

## Indexes

### Index Attribute

Use `[Index]` to create database indexes:

```csharp
[Entity]
[Table("users")]
[Index("IX_Users_Username", new[] { "Username" }, IsUnique = true)]
[Index("IX_Users_Email", new[] { "Email" }, IsUnique = true)]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("username")]
    public string Username { get; set; }
    
    [Column("email")]
    public string Email { get; set; }
}
```

### Index Properties

- `Name`: The index name
- `Columns`: Array of column names to include in the index
- `IsUnique`: Whether the index is unique
- `IsClustered`: Whether the index is clustered

## Constraints

### CheckConstraint Attribute

Use `[CheckConstraint]` to create check constraints:

```csharp
[Entity]
[Table("users")]
[CheckConstraint("CK_Users_Email_Format", "email LIKE '%@%'")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("email")]
    public string Email { get; set; }
}
```

## Advanced Mapping

### Custom Column Types

You can specify custom column types:

```csharp
[Entity]
[Table("products")]
public class Product
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("price", TypeName = "DECIMAL(10,2)")]
    public decimal Price { get; set; }
    
    [Column("description", TypeName = "TEXT")]
    public string Description { get; set; }
}
```

### Computed Columns

For computed columns, use the `[Computed]` attribute:

```csharp
[Entity]
[Table("orders")]
public class Order
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("subtotal")]
    public decimal Subtotal { get; set; }
    
    [Column("tax_rate")]
    public decimal TaxRate { get; set; }
    
    [Computed]
    public decimal Tax => Subtotal * TaxRate;
    
    [Computed]
    public decimal Total => Subtotal + Tax;
}
```

## Best Practices

1. **Use meaningful table and column names**
2. **Always specify column lengths for string properties**
3. **Use appropriate data types**
4. **Create indexes for frequently queried columns**
5. **Use constraints to ensure data integrity**
6. **Keep relationships simple and clear**
7. **Use lazy loading for large collections**
8. **Consider performance implications of relationships**

## See Also

- [Repository Pattern Guide](repository-pattern.md)
- [Query Language Guide](query-language.md)
- [Transaction Management Guide](transaction-management.md)
- [Entity Mapping Examples](../examples/basic/entity-mapping.md)
```

### Example Documentation Template
```markdown
# Basic CRUD Operations Example

This example demonstrates how to perform basic CRUD (Create, Read, Update, Delete) operations using NPA.

## Prerequisites

- .NET 6.0 or later
- NPA library installed
- SQL Server database

## Entity Definition

First, let's define a simple `Product` entity:

```csharp
using NPA.Core;

[Entity]
[Table("products")]
public class Product
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("name", Length = 100)]
    public string Name { get; set; }
    
    [Column("description", Length = 500)]
    public string Description { get; set; }
    
    [Column("price", Precision = 10, Scale = 2)]
    public decimal Price { get; set; }
    
    [Column("category_id")]
    public long CategoryId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
```

## Repository Interface

Create a repository interface for the `Product` entity:

```csharp
using NPA.Core;

public interface IProductRepository : IRepository<Product, long>
{
    Task<IEnumerable<Product>> FindByCategoryAsync(long categoryId);
    Task<IEnumerable<Product>> FindByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    Task<IEnumerable<Product>> FindByNameAsync(string name);
    Task<int> CountByCategoryAsync(long categoryId);
}
```

## Service Implementation

Implement a service that uses the repository:

```csharp
public class ProductService
{
    private readonly IProductRepository _productRepository;
    
    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    
    // Create
    public async Task<Product> CreateProductAsync(string name, string description, decimal price, long categoryId)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _productRepository.AddAsync(product);
    }
    
    // Read
    public async Task<Product> GetProductAsync(long id)
    {
        return await _productRepository.GetByIdAsync(id);
    }
    
    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        return await _productRepository.GetAllAsync();
    }
    
    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(long categoryId)
    {
        return await _productRepository.FindByCategoryAsync(categoryId);
    }
    
    public async Task<IEnumerable<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        return await _productRepository.FindByPriceRangeAsync(minPrice, maxPrice);
    }
    
    public async Task<IEnumerable<Product>> SearchProductsByNameAsync(string name)
    {
        return await _productRepository.FindByNameAsync(name);
    }
    
    // Update
    public async Task<Product> UpdateProductAsync(long id, string name, string description, decimal price)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {id} not found");
        }
        
        product.Name = name;
        product.Description = description;
        product.Price = price;
        product.UpdatedAt = DateTime.UtcNow;
        
        await _productRepository.UpdateAsync(product);
        return product;
    }
    
    // Delete
    public async Task<bool> DeleteProductAsync(long id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            return false;
        }
        
        await _productRepository.DeleteAsync(product);
        return true;
    }
    
    // Count
    public async Task<int> GetProductCountByCategoryAsync(long categoryId)
    {
        return await _productRepository.CountByCategoryAsync(categoryId);
    }
}
```

## Controller Implementation

Create a Web API controller:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    
    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(long id)
    {
        var product = await _productService.GetProductAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        
        return Ok(product);
    }
    
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(long categoryId)
    {
        var products = await _productService.GetProductsByCategoryAsync(categoryId);
        return Ok(products);
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] string name)
    {
        var products = await _productService.SearchProductsByNameAsync(name);
        return Ok(products);
    }
    
    [HttpGet("price-range")]
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsByPriceRange(
        [FromQuery] decimal minPrice, 
        [FromQuery] decimal maxPrice)
    {
        var products = await _productService.GetProductsByPriceRangeAsync(minPrice, maxPrice);
        return Ok(products);
    }
    
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = await _productService.CreateProductAsync(
            request.Name, 
            request.Description, 
            request.Price, 
            request.CategoryId);
        
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(long id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            var product = await _productService.UpdateProductAsync(
                id, 
                request.Name, 
                request.Description, 
                request.Price);
            
            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(long id)
    {
        var deleted = await _productService.DeleteProductAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        
        return NoContent();
    }
    
    [HttpGet("category/{categoryId}/count")]
    public async Task<ActionResult<int>> GetProductCountByCategory(long categoryId)
    {
        var count = await _productService.GetProductCountByCategoryAsync(categoryId);
        return Ok(count);
    }
}
```

## Request/Response Models

Define the request and response models:

```csharp
public class CreateProductRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public long CategoryId { get; set; }
}

public class UpdateProductRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
}
```

## Configuration

Configure NPA in your `Program.cs`:

```csharp
using NPA.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddScoped<ProductService>();

// Configure NPA
builder.Services.AddNPA(config =>
{
    config.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    config.DatabaseProvider = DatabaseProvider.SqlServer;
    config.EnableMigrations = true;
    config.EnableCaching = true;
});

var app = builder.Build();

// Configure pipeline
app.UseRouting();
app.MapControllers();

app.Run();
```

## Database Migration

Create a migration to set up the database:

```csharp
[Entity]
[Table("products")]
public class Product
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("name", Length = 100)]
    public string Name { get; set; }
    
    [Column("description", Length = 500)]
    public string Description { get; set; }
    
    [Column("price", Precision = 10, Scale = 2)]
    public decimal Price { get; set; }
    
    [Column("category_id")]
    public long CategoryId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
```

## Testing

Create unit tests for your service:

```csharp
[Test]
public async Task CreateProduct_ShouldReturnProduct()
{
    // Arrange
    var productRepository = new Mock<IProductRepository>();
    var productService = new ProductService(productRepository.Object);
    
    // Act
    var product = await productService.CreateProductAsync("Test Product", "Test Description", 99.99m, 1);
    
    // Assert
    Assert.NotNull(product);
    Assert.Equal("Test Product", product.Name);
    Assert.Equal("Test Description", product.Description);
    Assert.Equal(99.99m, product.Price);
    Assert.Equal(1, product.CategoryId);
}
```

## Running the Example

1. Set up your database connection string in `appsettings.json`
2. Run the application: `dotnet run`
3. Test the API endpoints using a tool like Postman or curl

## Next Steps

- Learn about [Advanced Features](../tutorials/advanced-features.md)
- Explore [Repository Pattern](../guides/repository-pattern.md)
- Check out [Query Language](../guides/query-language.md)
```

## 🧪 Test Cases

### Documentation Structure Tests
- [ ] Documentation directory structure
- [ ] Navigation between sections
- [ ] Cross-references
- [ ] Search functionality

### API Reference Tests
- [ ] API reference generation
- [ ] Method documentation
- [ ] Parameter documentation
- [ ] Example code

### Tutorial Tests
- [ ] Tutorial completeness
- [ ] Step-by-step instructions
- [ ] Code examples
- [ ] Troubleshooting

### Guide Tests
- [ ] Guide completeness
- [ ] Topic coverage
- [ ] Code examples
- [ ] Best practices

### Example Tests
- [ ] Example completeness
- [ ] Code functionality
- [ ] Integration examples
- [ ] Performance examples

## 📚 Documentation Requirements

### Content Requirements
- [ ] Complete API reference
- [ ] Comprehensive tutorials
- [ ] Detailed guides
- [ ] Working examples
- [ ] Best practices

### Quality Requirements
- [ ] Clear and concise writing
- [ ] Accurate code examples
- [ ] Proper formatting
- [ ] Consistent style
- [ ] Up-to-date information

### Organization Requirements
- [ ] Logical structure
- [ ] Easy navigation
- [ ] Search functionality
- [ ] Cross-references
- [ ] Index

## 🔍 Code Review Checklist

- [ ] Documentation is complete
- [ ] Code examples are accurate
- [ ] Writing is clear and concise
- [ ] Structure is logical
- [ ] Navigation is intuitive
- [ ] Search functionality works
- [ ] Cross-references are correct
- [ ] Examples are functional

## 🚀 Next Steps

After completing this task:
1. All phases are complete
2. Update checklist with completion status
3. Create final pull request for review
4. Publish documentation

## 📞 Questions/Issues

- [ ] Clarification needed on documentation structure
- [ ] Content requirements
- [ ] Organization preferences
- [ ] Publishing requirements

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
