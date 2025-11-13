# NamedQuery Support

## Overview

NPA supports **Named Queries** - a JPA-like feature that allows you to define reusable queries at the entity level. Named queries are **automatically matched by method name**, making your code cleaner and more maintainable without explicit attribute annotations on repository methods.

## Usage

### 1. Define Named Queries on Your Entity

Use the `[NamedQuery]` attribute on your entity class to define queries:

```csharp
using NPA.Core.Annotations;

[Entity]
[Table("users")]
[NamedQuery("User.FindByEmail", 
            "SELECT u FROM User u WHERE u.Email = :email")]
[NamedQuery("User.FindActiveUsers", 
            "SELECT u FROM User u WHERE u.IsActive = true")]
[NamedQuery("User.FindByEmailDomain",
            "SELECT u FROM User u WHERE u.Email LIKE :domain",
            Description = "Finds users by email domain")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
```

### 2. Create Repository Methods with Matching Names

**No attribute needed!** The generator automatically matches method names to named queries:

```csharp
using NPA.Core.Annotations;
using NPA.Core.Repositories;

[Repository]
public interface IUserRepository : IRepository<User, long>
{
    // Automatically matches "User.FindByEmail" or "User.FindByEmailAsync"
    Task<User?> FindByEmailAsync(string email);

    // Automatically matches "User.FindActiveUsers" or "User.FindActiveUsersAsync"
    Task<IEnumerable<User>> FindActiveUsersAsync();

    // Automatically matches "User.FindByEmailDomain" or "User.FindByEmailDomainAsync"
    Task<IEnumerable<User>> FindByEmailDomainAsync(string domain);
}
```

### 3. Named Query Matching Rules

The generator searches for named queries in this order:

1. **EntityName.MethodName** (e.g., `User.FindByEmailAsync`)
2. **MethodName only** (e.g., `FindByEmailAsync`)
3. **EntityName.MethodNameWithoutAsync** (e.g., `User.FindByEmail`)
4. **MethodNameWithoutAsync only** (e.g., `FindByEmail`)

This means all of these work:
```csharp
// Entity named query can be any of:
[NamedQuery("User.FindByEmail", "...")]           // ✅ Matches FindByEmailAsync()
[NamedQuery("User.FindByEmailAsync", "...")]      // ✅ Matches FindByEmailAsync()
[NamedQuery("FindByEmail", "...")]                // ✅ Matches FindByEmailAsync()
[NamedQuery("FindByEmailAsync", "...")]           // ✅ Matches FindByEmailAsync()
```

### 4. Priority Order

When generating repository methods, NPA follows this priority:

1. **Named Query** (auto-matched by name) - **Highest Priority** ⭐
2. `[NamedQuery("name")]` attribute on method (explicit override)
3. `[Query("sql")]` attribute
4. `[StoredProcedure]` attribute
5. Convention-based generation - Lowest Priority

This means **Named Queries take priority over everything else**, including `[Query]` attribute!

### 5. Named Query Properties

The `NamedQueryAttribute` supports the following properties:

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `name` | string | Yes | - | Query name (convention: EntityName.MethodName) |
| `query` | string | Yes | - | The CPQL or SQL query string |
| `NativeQuery` | bool | No | false | If true, executes as raw SQL without CPQL conversion |
| `CommandTimeout` | int? | No | null | Query timeout in seconds |
| `Buffered` | bool | No | true | Whether to buffer results (Dapper compatibility) |
| `Description` | string? | No | null | Description of what the query does |

## Examples

### Example 1: Simple Auto-Detected Named Query

```csharp
[Entity]
[Table("products")]
[NamedQuery("Product.FindByCategory", 
            "SELECT p FROM Product p WHERE p.Category = :category")]
public class Product
{
    [Id]
    public int Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("category")]
    public string Category { get; set; } = string.Empty;
    
    [Column("price")]
    public decimal Price { get; set; }
}

[Repository]
public interface IProductRepository : IRepository<Product, int>
{
    // No attribute needed! Auto-matches "Product.FindByCategory"
    Task<IEnumerable<Product>> FindByCategoryAsync(string category);
}
```

### Example 2: Native SQL Query (Auto-Detected)

```csharp
[Entity]
[Table("orders")]
[NamedQuery("Order.FindRecentOrdersAsync",
            "SELECT * FROM orders WHERE created_at > @since ORDER BY created_at DESC",
            NativeQuery = true,
            Description = "Finds orders created after a specific date")]
public class Order
{
    [Id]
    public long Id { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("total")]
    public decimal Total { get; set; }
}

[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    // Auto-matches "Order.FindRecentOrdersAsync"
    Task<IEnumerable<Order>> FindRecentOrdersAsync(DateTime since);
}
```

### Example 3: Override with Explicit Attribute (Optional)

```csharp
[Entity]
[Table("students")]
[NamedQuery("Student.TopPerformers",
            "SELECT s FROM Student s WHERE s.Grade >= 90 ORDER BY s.Grade DESC")]
[NamedQuery("Student.FindByGrade",
            "SELECT s FROM Student s WHERE s.Grade = :grade")]
public class Student
{
    [Id]
    public long Id { get; set; }
    
    [Column("grade")]
    public int Grade { get; set; }
}

[Repository]
public interface IStudentRepository : IRepository<Student, long>
{
    // Method name doesn't match, so use explicit attribute
    [NamedQuery("Student.TopPerformers")]
    Task<IEnumerable<Student>> GetTopStudentsAsync();
    
    // Auto-matches "Student.FindByGrade" - no attribute needed
    Task<IEnumerable<Student>> FindByGradeAsync(int grade);
}
```

## Benefits

1. **Zero Boilerplate**: No need for `[NamedQuery]` attribute on repository methods
2. **Centralized Query Management**: Define queries once on the entity, reuse across repositories
3. **Type Safety**: Queries are validated at compile time
4. **Maintainability**: Changes to queries only need to be made in one place
5. **JPA Compatibility**: Familiar pattern for developers coming from JPA/Hibernate
6. **CPQL Support**: Queries can use CPQL (Cross-Platform Query Language) for database independence
7. **Priority over Conventions**: Named queries take priority over `[Query]` and convention-based generation

## Naming Convention

**Recommended pattern for auto-detection:**
```
EntityName.MethodName
```

Examples:
- `User.FindByEmail` → matches `FindByEmailAsync()`
- `Product.FindByCategory` → matches `FindByCategoryAsync()`
- `Order.FindRecentOrders` → matches `FindRecentOrdersAsync()`

The `Async` suffix is optional in the named query name - it will match either way.

## Comparison with Other Approaches

### Named Query (Auto-Detected) - ⭐ Recommended
```csharp
// On Entity:
[NamedQuery("User.FindByEmail", "SELECT u FROM User u WHERE u.Email = :email")]
public class User { ... }

// In Repository - Clean and simple!
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    Task<User?> FindByEmailAsync(string email);  // Auto-matches!
}
```

### [Query] Attribute
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    [Query("SELECT u FROM User u WHERE u.Email = :email")]
    Task<User?> FindByEmailAsync(string email);
}
```

### Convention-Based
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    Task<User?> FindByEmailAsync(string email);  // Generates: WHERE Email = @email
}
```

**When to use Named Queries:**
- Complex queries that benefit from being centralized ✅
- Queries used in multiple repositories ✅
- When you want control over the exact SQL/CPQL ✅
- Following JPA conventions ✅
- **Most production scenarios** ✅

**When to use @Query:**
- Simple, repository-specific queries
- One-off queries that won't be reused
- Quick prototyping

**When to use Convention:**
- Simple CRUD operations
- Basic filtering by property names
- Rapid development with default behavior
