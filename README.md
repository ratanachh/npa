# NPA - JPA-like ORM for .NET

A lightweight, high-performance Object-Relational Mapping library for .NET that provides Java Persistence API (JPA) inspired features while leveraging Dapper's excellent performance as the underlying data access technology.

> **🚧 Development Status**: This project is currently in active development. Phase 1 (Core Foundation) is partially complete with basic entity mapping, EntityManager CRUD operations, and simple query support implemented. See the [Development Roadmap](#-development-roadmap) for current progress.

## 🎯 Project Goals

- **JPA-like API**: Familiar annotations and patterns for Java developers transitioning to .NET
- **High Performance**: Built on Dapper for optimal database performance
- **Lightweight**: Minimal overhead compared to full ORMs like Entity Framework
- **Type Safety**: Strong typing with compile-time safety
- **Extensibility**: Plugin architecture for custom behaviors

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│  @Entity Classes  │  Repository Interfaces  │  Services      │
├─────────────────────────────────────────────────────────────┤
│                    NPA Core                           │
├─────────────────────────────────────────────────────────────┤
│  EntityManager  │  QueryBuilder  │  Metadata  │  Validators  │
├─────────────────────────────────────────────────────────────┤
│                      Dapper Layer                           │
├─────────────────────────────────────────────────────────────┤
│                    Database Providers                       │
│    SQL Server    │    PostgreSQL    │    MySQL    │   SQLite  │
└─────────────────────────────────────────────────────────────┘
```

## 📋 Currently Implemented Features

### 1. Entity Mapping ✅
```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("username", nullable: false, length: 50)]
    public string Username { get; set; }
    
    [Column("email", nullable: false, unique: true)]
    public string Email { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; }
}
```

> **Note**: Relationship mapping (OneToMany, ManyToOne, etc.) is planned for Phase 2.

### 2. EntityManager API ✅
```csharp
public class UserService
{
    private readonly IEntityManager entityManager;
    
    public UserService(IEntityManager entityManager)
    {
        this.entityManager = entityManager;
    }
    
    public async Task<User> CreateUserAsync(string username, string email)
    {
        var user = new User 
        { 
            Username = username, 
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
        
        await entityManager.PersistAsync(user);
        await entityManager.FlushAsync();
        
        return user;
    }
    
    public async Task<User?> FindUserAsync(long id)
    {
        return await entityManager.FindAsync<User>(id);
    }
    
    public async Task UpdateUserAsync(User user)
    {
        await entityManager.MergeAsync(user);
        await entityManager.FlushAsync();
    }
    
    public async Task DeleteUserAsync(long id)
    {
        var user = await entityManager.FindAsync<User>(id);
        if (user != null)
        {
            await entityManager.RemoveAsync(user);
            await entityManager.FlushAsync();
        }
    }
}
```

> **Note**: Repository pattern implementation is planned for Phase 2.

### 3. Query Language (CPQL) ✅
```csharp
// Dynamic Queries using EntityManager
var query = entityManager
    .CreateQuery<User>("SELECT u FROM User u WHERE u.Username = :username AND u.IsActive = :active")
    .SetParameter("username", "john")
    .SetParameter("active", true);

var users = await query.GetResultListAsync();

// Single result queries
var user = await entityManager
    .CreateQuery<User>("SELECT u FROM User u WHERE u.Id = :id")
    .SetParameter("id", 1L)
    .GetSingleResultAsync();

// Update queries
var updatedCount = await entityManager
    .CreateQuery<User>("UPDATE User u SET u.IsActive = :active WHERE u.CreatedAt < :date")
    .SetParameter("active", false)
    .SetParameter("date", DateTime.UtcNow.AddYears(-1))
    .ExecuteUpdateAsync();
```

## 🚧 Planned Features (Not Yet Implemented)

### 4. Relationship Mapping
```csharp
[Entity]
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
    
    [OneToMany(mappedBy = "Order", cascade = CascadeType.All)]
    public ICollection<OrderItem> Items { get; set; }
}
```

### 5. Repository Pattern
```csharp
public interface IUserRepository : IRepository<User, long>
{
    Task<User> FindByUsernameAsync(string username);
    Task<IEnumerable<User>> FindByEmailDomainAsync(string domain);
}
```

### 6. Transaction Management
```csharp
public class OrderService
{
    private readonly IEntityManager entityManager;
    
    public OrderService(IEntityManager entityManager)
    {
        this.entityManager = entityManager;
    }
    
    public async Task<Order> CreateOrderWithItemsAsync(long userId, List<OrderItemDto> items)
    {
        using var transaction = await entityManager.BeginTransactionAsync();
        try
        {
            // Create order
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending
            };
            await entityManager.PersistAsync(order);
            
            // Create order items
            foreach (var itemDto in items)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    Price = itemDto.Price
                };
                await entityManager.PersistAsync(orderItem);
            }
            
            await entityManager.CommitAsync();
            return order;
        }
        catch
        {
            await entityManager.RollbackAsync();
            throw;
        }
    }
}
```

### 7. Source Generator Integration (Planned)
```csharp
// Define repository interface - implementation will be auto-generated
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    Task<User> FindByUsernameAsync(string username);
    Task<IEnumerable<User>> FindByEmailDomainAsync(string domain);
    Task<IEnumerable<User>> FindActiveUsersAsync();
}

// Generated implementation (created at compile time)
public partial class UserRepository : RepositoryBase<User, long>, IUserRepository
{
    public UserRepository(IDbConnection connection) : base(connection) { }
    
    public async Task<User> FindByUsernameAsync(string username)
    {
        return await Connection.QueryFirstOrDefaultAsync<User>(
            "SELECT id, username, email, created_at FROM users WHERE username = @username", 
            new { username });
    }
}
```

---

## 📋 Detailed Implementation Plans (Reference)

> **Note**: The following sections contain detailed implementation plans and examples for future development phases. These features are not yet implemented but serve as a comprehensive roadmap and reference for the project.

### 🔧 Source Generator Details (Planned)

#### Repository Generation Strategy

The NPA Source Generator will automatically generate repository implementations based on interface definitions and naming conventions:

##### 1. **Method Name Analysis**
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    // Generates: SELECT id, username, email, created_at FROM users WHERE username = @username
    Task<User> FindByUsernameAsync(string username);
    
    // Generates: SELECT id, username, email, created_at FROM users WHERE email LIKE @domain
    Task<IEnumerable<User>> FindByEmailDomainAsync(string domain);
    
    // Generates: SELECT id, username, email, created_at FROM users WHERE is_active = @active
    Task<IEnumerable<User>> FindActiveUsersAsync();
    
    // Generates: SELECT id, username, email, created_at FROM users WHERE created_at > @since
    Task<IEnumerable<User>> FindByCreatedAfterAsync(DateTime since);
}
```

##### 2. **Convention-Based Query Generation**
- `FindBy{Property}Async` → `WHERE {property} = @{property}`
- `Find{Property}ContainingAsync` → `WHERE {property} LIKE '%@{property}%'`
- `Find{Property}StartingWithAsync` → `WHERE {property} LIKE '@{property}%'`
- `Find{Property}EndingWithAsync` → `WHERE {property} LIKE '%@{property}'`
- `FindBy{Property}GreaterThanAsync` → `WHERE {property} > @{property}`
- `FindBy{Property}LessThanAsync` → `WHERE {property} < @{property}`

##### 3. **Dapper Feature Integration**
The source generator will support all Dapper capabilities:

###### **Multi-Mapping Support**
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    // Generates multi-mapping query
    Task<IEnumerable<OrderWithCustomer>> GetOrdersWithCustomersAsync();
    
    // Generates complex multi-mapping with custom mapping function
    Task<IEnumerable<OrderSummary>> GetOrderSummariesAsync();
}

// Generated implementation
public async Task<IEnumerable<OrderWithCustomer>> GetOrdersWithCustomersAsync()
{
    return await Connection.QueryAsync<Order, Customer, OrderWithCustomer>(
        @"SELECT o.*, c.* FROM orders o 
          INNER JOIN customers c ON o.customer_id = c.id",
        (order, customer) => new OrderWithCustomer 
        { 
            Order = order, 
            Customer = customer 
        });
}
```

###### **Stored Procedure Support**
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    [StoredProcedure("sp_GetUserStatistics")]
    Task<UserStatistics> GetUserStatisticsAsync(int userId);
    
    [StoredProcedure("sp_UpdateUserStatus")]
    Task<int> UpdateUserStatusAsync(int userId, bool isActive);
}

// Generated implementation
public async Task<UserStatistics> GetUserStatisticsAsync(int userId)
{
    return await Connection.QueryFirstOrDefaultAsync<UserStatistics>(
        "sp_GetUserStatistics", 
        new { userId }, 
        commandType: CommandType.StoredProcedure);
}
```

###### **Bulk Operations**
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    Task<int> BulkInsertUsersAsync(IEnumerable<User> users);
    Task<int> BulkUpdateUsersAsync(IEnumerable<User> users);
    Task<int> BulkDeleteUsersAsync(IEnumerable<long> userIds);
}

// Generated implementation
public async Task<int> BulkInsertUsersAsync(IEnumerable<User> users)
{
    return await Connection.ExecuteAsync(
        @"INSERT INTO users (username, email, created_at) 
          VALUES (@Username, @Email, @CreatedAt)", 
        users);
}
```

###### **Pagination Support**
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    Task<PagedResult<User>> GetUsersPagedAsync(int page, int pageSize);
    Task<PagedResult<User>> SearchUsersPagedAsync(string searchTerm, int page, int pageSize);
}

// Generated implementation
public async Task<PagedResult<User>> GetUsersPagedAsync(int page, int pageSize)
{
    var offset = (page - 1) * pageSize;
    
    var countQuery = "SELECT COUNT(*) FROM users";
    var dataQuery = "SELECT id, username, email, created_at FROM users ORDER BY created_at DESC LIMIT @pageSize OFFSET @offset";
    
    var totalCount = await Connection.QuerySingleAsync<int>(countQuery);
    var users = await Connection.QueryAsync<User>(dataQuery, new { pageSize, offset });
    
    return new PagedResult<User>
    {
        Data = users,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

#### Generator Pipeline

1. **Syntax Analysis**: Detects interfaces with `[Repository]` attribute
2. **Method Analysis**: Analyzes method signatures and naming patterns
3. **Return Type Analysis**: Examines return types to determine required columns
4. **Column Mapping**: Maps entity properties to database columns using conventions/attributes
5. **Dapper Feature Detection**: Identifies Dapper-specific patterns and attributes
6. **Query Generation**: Creates SQL queries with specific column selections
7. **Code Generation**: Generates implementation classes with full Dapper integration
8. **Metadata Generation**: Creates compile-time metadata
9. **Validation**: Validates generated code for correctness

### 🔧 Advanced Features Details (Planned)

#### Composite Key Support

NPA will support composite keys for entities that require multiple columns to uniquely identify records:

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
    
    [Column("price")]
    public decimal Price { get; set; }
    
    [ManyToOne]
    [JoinColumn("order_id")]
    public Order Order { get; set; }
    
    [ManyToOne]
    [JoinColumn("product_id")]
    public Product Product { get; set; }
}

// Repository operations with composite keys
public interface IOrderItemRepository : IRepository<OrderItem, CompositeKey>
{
    Task<OrderItem> FindByCompositeKeyAsync(long orderId, long productId);
    Task<IEnumerable<OrderItem>> FindByOrderIdAsync(long orderId);
}
```

#### Many-to-Many Relationships

Automatic join table management with full relationship support:

```csharp
[Entity]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("username")]
    public string Username { get; set; }
    
    [ManyToMany]
    [JoinTable("user_roles", 
        JoinColumns = new[] { "user_id" }, 
        InverseJoinColumns = new[] { "role_id" })]
    public ICollection<Role> Roles { get; set; }
}

[Entity]
public class Role
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [ManyToMany(mappedBy = "Roles")]
    public ICollection<User> Users { get; set; }
}

// Generated queries for many-to-many
// SELECT u.* FROM users u 
// INNER JOIN user_roles ur ON u.id = ur.user_id 
// WHERE ur.role_id = @roleId
```

#### Transaction Management

Comprehensive transaction support with both declarative and programmatic approaches:

```csharp
// Declarative transaction management
[Transactional]
public async Task<Order> CreateOrderWithItemsAsync(long userId, List<OrderItemDto> items)
{
    var order = new Order { UserId = userId, OrderDate = DateTime.UtcNow };
    await entityManager.PersistAsync(order);
    
    foreach (var item in items)
    {
        var orderItem = new OrderItem { OrderId = order.Id, ProductId = item.ProductId };
        await entityManager.PersistAsync(orderItem);
    }
    
    return order; // Transaction commits automatically
}

// Programmatic transaction management
public async Task TransferFundsAsync(long fromAccountId, long toAccountId, decimal amount)
{
    using var transaction = await entityManager.BeginTransactionAsync();
    try
    {
        var fromAccount = await entityManager.FindAsync<Account>(fromAccountId);
        var toAccount = await entityManager.FindAsync<Account>(toAccountId);
        
        fromAccount.Balance -= amount;
        toAccount.Balance += amount;
        
        await entityManager.MergeAsync(fromAccount);
        await entityManager.MergeAsync(toAccount);
        
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

#### Cascade Operations

Automatic handling of related entity operations:

```csharp
[Entity]
public class Order
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [OneToMany(mappedBy = "Order", cascade = CascadeType.All)]
    public ICollection<OrderItem> Items { get; set; }
}

// When deleting an order, all order items are automatically deleted
await entityManager.RemoveAsync(order); // Cascades to OrderItems
```

#### Bulk Operations

Efficient batch processing for large datasets:

```csharp
// Bulk insert
var users = new List<User> { /* ... */ };
await entityManager.BulkInsertAsync(users);

// Bulk update
await entityManager.BulkUpdateAsync<User>(
    "UPDATE users SET is_active = @active WHERE created_at < @date",
    new { active = false, date = DateTime.UtcNow.AddYears(-1) });

// Bulk delete
await entityManager.BulkDeleteAsync<User>(
    "DELETE FROM users WHERE is_active = @active AND last_login < @date",
    new { active = false, date = DateTime.UtcNow.AddMonths(-6) });
```

### 7. Source Generator Integration
```csharp
// Define repository interface - implementation will be auto-generated
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    Task<User> FindByUsernameAsync(string username);
    Task<IEnumerable<User>> FindByEmailDomainAsync(string domain);
    Task<IEnumerable<User>> FindActiveUsersAsync();
}

// Generated implementation (created at compile time)
public partial class UserRepository : RepositoryBase<User, long>, IUserRepository
{
    public UserRepository(IDbConnection connection) : base(connection) { }
    
    public async Task<User> FindByUsernameAsync(string username)
    {
        return await Connection.QueryFirstOrDefaultAsync<User>(
            "SELECT id, username, email, created_at FROM users WHERE username = @username", 
            new { username });
    }
    
    public async Task<IEnumerable<User>> FindByEmailDomainAsync(string domain)
    {
        return await Connection.QueryAsync<User>(
            "SELECT id, username, email, created_at FROM users WHERE email LIKE @domain", 
            new { domain = $"%@{domain}" });
    }
    
    public async Task<IEnumerable<User>> FindActiveUsersAsync()
    {
        return await Connection.QueryAsync<User>(
            "SELECT id, username, email, created_at FROM users WHERE is_active = @active", 
            new { active = true });
    }
}
```

### 🔧 Additional Source Generator Features (Planned)

#### Dynamic Parameters Support
```csharp
[Repository]
public interface IProductRepository : IRepository<Product, long>
{
    Task<IEnumerable<Product>> SearchProductsAsync(DynamicParameters parameters);
}

// Generated implementation
public async Task<IEnumerable<Product>> SearchProductsAsync(DynamicParameters parameters)
{
    return await Connection.QueryAsync<Product>(
        "SELECT * FROM products WHERE name LIKE @name AND category_id = @categoryId", 
        parameters);
}
```

#### Grid Reader Support
```csharp
[Repository]
public interface IReportRepository
{
    Task<ReportData> GetDashboardDataAsync();
}

// Generated implementation
public async Task<ReportData> GetDashboardDataAsync()
{
    using var gridReader = await Connection.QueryMultipleAsync(@"
        SELECT COUNT(*) FROM users;
        SELECT COUNT(*) FROM orders;
        SELECT * FROM recent_activities ORDER BY created_at DESC LIMIT 10");
    
    return new ReportData
    {
        UserCount = await gridReader.ReadSingleAsync<int>(),
        OrderCount = await gridReader.ReadSingleAsync<int>(),
        RecentActivities = await gridReader.ReadAsync<Activity>()
    };
}
```

#### Custom SQL with Parameters
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    [Query("SELECT o.*, c.name as customer_name FROM orders o JOIN customers c ON o.customer_id = c.id WHERE o.status = @status")]
    Task<IEnumerable<OrderWithCustomer>> GetOrdersByStatusAsync(OrderStatus status);
    
    [Query("SELECT * FROM orders WHERE created_at BETWEEN @startDate AND @endDate")]
    Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
}
```

#### Async/Await Patterns
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    // Async single operations
    Task<User> GetUserByIdAsync(long id);
    Task<User> GetUserByEmailAsync(string email);
    
    // Async enumerable operations
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
    
    // Async scalar operations
    Task<int> GetUserCountAsync();
    Task<bool> UserExistsAsync(string email);
    Task<DateTime?> GetLastLoginAsync(long userId);
}
```

#### Transaction Support
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    [Transactional]
    Task<Order> CreateOrderWithItemsAsync(Order order, IEnumerable<OrderItem> items);
    
    Task<Order> CreateOrderInTransactionAsync(Order order, IDbTransaction transaction);
}
```

#### Connection String Management
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    [ConnectionString("ReadOnlyConnection")]
    Task<IEnumerable<User>> GetReadOnlyUsersAsync();
    
    [ConnectionString("AnalyticsConnection")]
    Task<IEnumerable<UserAnalytics>> GetUserAnalyticsAsync();
}
```

#### Command Timeout Configuration
```csharp
[Repository]
public interface IReportRepository
{
    [CommandTimeout(300)] // 5 minutes
    Task<ComplexReport> GenerateComplexReportAsync();
    
    [CommandTimeout(30)]
    Task<SimpleReport> GenerateSimpleReportAsync();
}
```

#### Result Set Mapping
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    [MultiMapping(typeof(Order), typeof(Customer), typeof(Product))]
    Task<IEnumerable<OrderWithDetails>> GetOrdersWithDetailsAsync();
}

// Generated implementation
public async Task<IEnumerable<OrderWithDetails>> GetOrdersWithDetailsAsync()
{
    return await Connection.QueryAsync<Order, Customer, Product, OrderWithDetails>(
        @"SELECT o.*, c.*, p.* FROM orders o 
          JOIN customers c ON o.customer_id = c.id
          JOIN order_items oi ON o.id = oi.order_id
          JOIN products p ON oi.product_id = p.id",
        (order, customer, product) => new OrderWithDetails
        {
            Order = order,
            Customer = customer,
            Product = product
        },
        splitOn: "id,id");
}
```

#### Pagination Support
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    Task<PagedResult<User>> GetUsersPagedAsync(int page, int pageSize);
    Task<PagedResult<User>> SearchUsersPagedAsync(string searchTerm, int page, int pageSize);
    Task<PagedResult<User>> GetUsersByRolePagedAsync(string role, int page, int pageSize);
}

// Generated implementation
public async Task<PagedResult<User>> GetUsersPagedAsync(int page, int pageSize)
{
    var offset = (page - 1) * pageSize;
    
    var countQuery = "SELECT COUNT(*) FROM users";
    var dataQuery = "SELECT id, username, email, created_at, is_active FROM users ORDER BY created_at DESC LIMIT @pageSize OFFSET @offset";
    
    var totalCount = await Connection.QuerySingleAsync<int>(countQuery);
    var users = await Connection.QueryAsync<User>(dataQuery, new { pageSize, offset });
    
    return new PagedResult<User>
    {
        Data = users,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
        HasPreviousPage = page > 1
    };
}

public async Task<PagedResult<User>> SearchUsersPagedAsync(string searchTerm, int page, int pageSize)
{
    var offset = (page - 1) * pageSize;
    var searchPattern = $"%{searchTerm}%";
    
    var countQuery = "SELECT COUNT(*) FROM users WHERE username LIKE @searchPattern OR email LIKE @searchPattern";
    var dataQuery = @"SELECT id, username, email, created_at, is_active 
                      FROM users 
                      WHERE username LIKE @searchPattern OR email LIKE @searchPattern 
                      ORDER BY created_at DESC 
                      LIMIT @pageSize OFFSET @offset";
    
    var totalCount = await Connection.QuerySingleAsync<int>(countQuery, new { searchPattern });
    var users = await Connection.QueryAsync<User>(dataQuery, new { searchPattern, pageSize, offset });
    
    return new PagedResult<User>
    {
        Data = users,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
        HasPreviousPage = page > 1
    };
}

// PagedResult helper class
public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
```

#### Advanced Pagination with Sorting
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    Task<PagedResult<User>> GetUsersPagedAsync(int page, int pageSize, string sortBy, bool ascending = true);
    Task<PagedResult<User>> GetUsersPagedWithFiltersAsync(UserFilter filter, int page, int pageSize);
}

// Generated implementation with sorting
public async Task<PagedResult<User>> GetUsersPagedAsync(int page, int pageSize, string sortBy, bool ascending = true)
{
    var offset = (page - 1) * pageSize;
    var direction = ascending ? "ASC" : "DESC";
    
    // Validate sort column to prevent SQL injection
    var validSortColumns = new[] { "username", "email", "created_at", "is_active" };
    if (!validSortColumns.Contains(sortBy.ToLower()))
        sortBy = "created_at";
    
    var countQuery = "SELECT COUNT(*) FROM users";
    var dataQuery = $"SELECT id, username, email, created_at, is_active FROM users ORDER BY {sortBy} {direction} LIMIT @pageSize OFFSET @offset";
    
    var totalCount = await Connection.QuerySingleAsync<int>(countQuery);
    var users = await Connection.QueryAsync<User>(dataQuery, new { pageSize, offset });
    
    return new PagedResult<User>
    {
        Data = users,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
        HasPreviousPage = page > 1
    };
}

// Filter-based pagination
public async Task<PagedResult<User>> GetUsersPagedWithFiltersAsync(UserFilter filter, int page, int pageSize)
{
    var offset = (page - 1) * pageSize;
    var conditions = new List<string>();
    var parameters = new Dictionary<string, object>();
    
    if (filter.IsActive.HasValue)
    {
        conditions.Add("is_active = @isActive");
        parameters["isActive"] = filter.IsActive.Value;
    }
    
    if (!string.IsNullOrEmpty(filter.EmailDomain))
    {
        conditions.Add("email LIKE @emailDomain");
        parameters["emailDomain"] = $"%@{filter.EmailDomain}";
    }
    
    if (filter.CreatedAfter.HasValue)
    {
        conditions.Add("created_at > @createdAfter");
        parameters["createdAfter"] = filter.CreatedAfter.Value;
    }
    
    var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";
    
    var countQuery = $"SELECT COUNT(*) FROM users {whereClause}";
    var dataQuery = $"SELECT id, username, email, created_at, is_active FROM users {whereClause} ORDER BY created_at DESC LIMIT @pageSize OFFSET @offset";
    
    parameters["pageSize"] = pageSize;
    parameters["offset"] = offset;
    
    var totalCount = await Connection.QuerySingleAsync<int>(countQuery, parameters);
    var users = await Connection.QueryAsync<User>(dataQuery, parameters);
    
    return new PagedResult<User>
    {
        Data = users,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize),
        HasPreviousPage = page > 1
    };
}

// Filter helper class
public class UserFilter
{
    public bool? IsActive { get; set; }
    public string? EmailDomain { get; set; }
    public DateTime? CreatedAfter { get; set; }
}
```

#### Cursor-Based Pagination (For Large Datasets)
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    Task<CursorPagedResult<User>> GetUsersCursorPagedAsync(int pageSize, long? cursor = null);
}

// Generated implementation for cursor-based pagination
public async Task<CursorPagedResult<User>> GetUsersCursorPagedAsync(int pageSize, long? cursor = null)
{
    var whereClause = cursor.HasValue ? "WHERE id > @cursor" : "";
    var parameters = cursor.HasValue ? new { pageSize, cursor } : new { pageSize };
    
    var dataQuery = $"SELECT id, username, email, created_at, is_active FROM users {whereClause} ORDER BY id ASC LIMIT @pageSize";
    
    var users = await Connection.QueryAsync<User>(dataQuery, parameters);
    var usersList = users.ToList();
    
    var nextCursor = usersList.Any() ? usersList.Last().Id : (long?)null;
    var hasNextPage = usersList.Count == pageSize;
    
    return new CursorPagedResult<User>
    {
        Data = usersList,
        NextCursor = nextCursor,
        HasNextPage = hasNextPage,
        PageSize = pageSize
    };
}

// Cursor-based pagination result
public class CursorPagedResult<T> where T : class
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public long? NextCursor { get; set; }
    public bool HasNextPage { get; set; }
    public int PageSize { get; set; }
}
```

#### Smart Column Selection
The generator analyzes the return type and generates specific column selections:

```csharp
// For User entity with properties: Id, Username, Email, CreatedAt, IsActive
public class User
{
    public long Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

// Generated queries will select only the mapped columns:
// SELECT id, username, email, created_at, is_active FROM users WHERE username = @username

// For DTOs with specific properties:
public class UserSummary
{
    public long Id { get; set; }
    public string Username { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Generated query for UserSummary return type:
// SELECT id, username, created_at FROM users WHERE username = @username
```

#### Custom Query Attributes
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    [Query("SELECT u.* FROM users u JOIN profiles p ON u.id = p.user_id WHERE p.verified = @verified")]
    Task<IEnumerable<User>> FindVerifiedUsersAsync(bool verified);
    
    [Query("SELECT COUNT(*) FROM users WHERE created_at > @since")]
    Task<int> CountUsersCreatedAfterAsync(DateTime since);
}
```

#### Metadata Generation
```csharp
// Auto-generated metadata for compile-time optimization
public static partial class UserMetadata
{
    public static readonly EntityMetadata Metadata = new()
    {
        EntityType = typeof(User),
        TableName = "users",
        PrimaryKey = "Id",
        Properties = new Dictionary<string, PropertyMetadata>
        {
            ["Id"] = new() { ColumnName = "id", IsPrimaryKey = true },
            ["Username"] = new() { ColumnName = "username", IsNullable = false },
            ["Email"] = new() { ColumnName = "email", IsNullable = false },
            ["CreatedAt"] = new() { ColumnName = "created_at", IsNullable = false }
        }
    };
}
```

### 🔧 Complete Dapper Feature Support (Planned)

#### Core Dapper Methods
- `QueryAsync<T>()` - Async query with mapping
- `QueryFirstOrDefaultAsync<T>()` - Single result with default
- `QuerySingleAsync<T>()` - Single result (throws if none/multiple)
- `QueryMultipleAsync()` - Multiple result sets
- `ExecuteAsync()` - Execute commands
- `ExecuteScalarAsync<T>()` - Single scalar value

#### Advanced Dapper Features
- **Multi-Mapping**: Complex object relationships
- **Stored Procedures**: Full stored procedure support
- **Dynamic Parameters**: Flexible parameter handling
- **Bulk Operations**: Efficient batch processing
- **Grid Reader**: Multiple result set handling
- **Custom Type Handlers**: Specialized type conversion
- **Connection Management**: Multiple connection strings
- **Command Configuration**: Timeouts, command types
- **Transaction Support**: Full transaction integration
- **Pagination**: Built-in pagination support

#### Generated Code Quality
- **Type Safety**: Full compile-time validation
- **Performance**: Optimized Dapper usage
- **IntelliSense**: Complete IDE support
- **Error Handling**: Comprehensive exception management
- **Logging**: Built-in query logging
- **Testing**: Easy unit testing support

### 🔧 Column Selection Strategy (Planned)

#### Entity Property Analysis
```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("username")]
    public string Username { get; set; }
    
    [Column("email")]
    public string Email { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; }
    
    // Navigation property - not mapped to column
    public ICollection<Order> Orders { get; set; }
}

// Generated query for User return type:
// SELECT id, username, email, created_at, is_active FROM users WHERE username = @username
```

#### DTO Support
```csharp
public class UserSummary
{
    public long Id { get; set; }
    public string Username { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Generated query for UserSummary return type:
// SELECT id, username, created_at FROM users WHERE username = @username
```

#### Convention-Based Column Mapping
- Property name → Column name (snake_case conversion)
- `Id` property → Primary key column
- Navigation properties → Excluded from SELECT
- Complex types → Analyzed recursively


## 🏗️ Complete Planned Project Structure (Reference)

```
NPA/
├── src/
│   ├── NPA.Core/                 # Core library ✅ (Phase 1)
│   │   ├── Annotations/                # Entity mapping attributes ✅
│   │   │   ├── EntityAttribute.cs
│   │   │   ├── TableAttribute.cs
│   │   │   ├── IdAttribute.cs
│   │   │   ├── ColumnAttribute.cs
│   │   │   ├── GeneratedValueAttribute.cs
│   │   │   ├── GenerationType.cs
│   │   │   ├── OneToManyAttribute.cs          # 🚧 Planned (Phase 2.1)
│   │   │   ├── ManyToOneAttribute.cs          # 🚧 Planned (Phase 2.1)
│   │   │   ├── ManyToManyAttribute.cs         # 🚧 Planned (Phase 2.1)
│   │   │   ├── JoinColumnAttribute.cs         # 🚧 Planned (Phase 2.1)
│   │   │   ├── JoinTableAttribute.cs          # 🚧 Planned (Phase 2.1)
│   │   │   ├── NamedQueryAttribute.cs         # 🚧 Planned (Phase 2.3)
│   │   │   ├── TransactionalAttribute.cs      # 🚧 Planned (Phase 3.1)
│   │   │   ├── StoredProcedureAttribute.cs    # 🚧 Planned (Phase 4.1)
│   │   │   ├── QueryAttribute.cs              # 🚧 Planned (Phase 4.1)
│   │   │   ├── BulkOperationAttribute.cs      # 🚧 Planned (Phase 3.3)
│   │   │   ├── MultiMappingAttribute.cs       # 🚧 Planned (Phase 4.1)
│   │   │   ├── ConnectionStringAttribute.cs   # 🚧 Planned (Phase 4.1)
│   │   │   ├── CommandTimeoutAttribute.cs     # 🚧 Planned (Phase 4.1)
│   │   │   ├── PaginationAttribute.cs         # 🚧 Planned (Phase 4.1)
│   │   │   └── CascadeType.cs                 # 🚧 Planned (Phase 3.2)
│   │   ├── Core/                       # Entity management ✅
│   │   │   ├── IEntityManager.cs
│   │   │   ├── EntityManager.cs
│   │   │   ├── IChangeTracker.cs
│   │   │   ├── ChangeTracker.cs
│   │   │   ├── EntityState.cs
│   │   │   ├── CompositeKey.cs
│   │   │   ├── IRepository.cs                  # 🚧 Planned (Phase 2.4)
│   │   │   ├── BaseRepository.cs               # 🚧 Planned (Phase 2.4)
│   │   │   ├── ITransaction.cs                 # 🚧 Planned (Phase 3.1)
│   │   │   ├── Transaction.cs                  # 🚧 Planned (Phase 3.1)
│   │   │   ├── IBulkOperations.cs              # 🚧 Planned (Phase 3.3)
│   │   │   ├── BulkOperations.cs               # 🚧 Planned (Phase 3.3)
│   │   │   ├── ILazyLoader.cs                  # 🚧 Planned (Phase 3.4)
│   │   │   └── LazyLoader.cs                   # 🚧 Planned (Phase 3.4)
│   │   ├── Metadata/                   # Entity metadata ✅
│   │   │   ├── EntityMetadata.cs
│   │   │   ├── PropertyMetadata.cs
│   │   │   ├── IMetadataProvider.cs
│   │   │   ├── MetadataProvider.cs
│   │   │   ├── RelationshipMetadata.cs         # 🚧 Planned (Phase 2.1)
│   │   │   ├── CompositeKeyMetadata.cs         # 🚧 Planned (Phase 2.2)
│   │   │   ├── JoinTableMetadata.cs            # 🚧 Planned (Phase 2.1)
│   │   │   └── MetadataBuilder.cs              # 🚧 Planned (Phase 2.6)
│   │   ├── Query/                      # Query system ✅
│   │   │   ├── IQuery.cs
│   │   │   ├── Query.cs
│   │   │   ├── IQueryParser.cs
│   │   │   ├── QueryParser.cs
│   │   │   ├── ISqlGenerator.cs
│   │   │   ├── SqlGenerator.cs
│   │   │   ├── IParameterBinder.cs
│   │   │   ├── ParameterBinder.cs
│   │   │   ├── QueryBuilder.cs                 # 🚧 Planned (Phase 2.3)
│   │   │   ├── JPQLParser.cs                   # 🚧 Planned (Phase 2.3)
│   │   │   ├── NamedQueryRegistry.cs           # 🚧 Planned (Phase 2.3)
│   │   │   └── QueryCache.cs                   # 🚧 Planned (Phase 5.1)
│   │   ├── Providers/                  # Database provider interfaces ✅
│   │   │   ├── IDatabaseProvider.cs
│   │   │   ├── ISqlDialect.cs
│   │   │   ├── ITypeConverter.cs
│   │   │   └── IBulkOperationProvider.cs
│   │   ├── Validation/                 # Entity validation 🚧 Planned (Phase 2.4)
│   │   │   ├── IEntityValidator.cs
│   │   │   ├── EntityValidator.cs
│   │   │   ├── ValidationResult.cs
│   │   │   ├── ValidationAttribute.cs
│   │   │   └── ValidationException.cs
│   │   ├── Configuration/              # Configuration management 🚧 Planned (Phase 1.4)
│   │   │   ├── NPAConfiguration.cs
│   │   │   ├── DatabaseProvider.cs
│   │   │   ├── ConnectionManager.cs
│   │   │   ├── ConnectionStringProvider.cs
│   │   │   └── ConfigurationBuilder.cs
│   │   └── Caching/                    # Caching support 🚧 Planned (Phase 5.1)
│   │       ├── ICacheProvider.cs
│   │       ├── MemoryCacheProvider.cs
│   │       ├── RedisCacheProvider.cs
│   │       ├── CacheConfiguration.cs
│   │       └── CacheInvalidationStrategy.cs
│   ├── NPA.Extensions/           # Extensions and utilities 🚧 Planned (Phase 2.4)
│   │   ├── DependencyInjection/
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   ├── ServiceCollectionExtensions.Configuration.cs
│   │   │   └── ServiceCollectionExtensions.Logging.cs
│   │   ├── Logging/
│   │   │   ├── EntityManagerLogger.cs
│   │   │   ├── QueryLogger.cs
│   │   │   └── PerformanceLogger.cs
│   │   ├── Utilities/
│   │   │   ├── ReflectionHelper.cs
│   │   │   ├── TypeHelper.cs
│   │   │   ├── StringHelper.cs
│   │   │   ├── ExpressionHelper.cs
│   │   │   └── PropertyAccessor.cs
│   │   └── Diagnostics/
│   │       ├── PerformanceCounter.cs
│   │       ├── MetricsCollector.cs
│   │       └── HealthChecker.cs
│   ├── NPA.Generators/           # Source Generators ✅ Basic (Phase 1.6)
│   │   ├── RepositoryGenerator/
│   │   │   ├── RepositoryGenerator.cs
│   │   │   ├── RepositorySyntaxReceiver.cs
│   │   │   ├── RepositoryCodeGenerator.cs
│   │   │   ├── RepositoryTemplate.cs
│   │   │   └── RepositoryAnalyzer.cs
│   │   ├── MetadataGenerator/
│   │   │   ├── MetadataGenerator.cs
│   │   │   ├── EntitySyntaxReceiver.cs
│   │   │   ├── MetadataCodeGenerator.cs
│   │   │   ├── MetadataTemplate.cs
│   │   │   └── MetadataAnalyzer.cs
│   │   ├── QueryGenerator/
│   │   │   ├── QueryGenerator.cs
│   │   │   ├── QuerySyntaxReceiver.cs
│   │   │   ├── QueryCodeGenerator.cs
│   │   │   ├── QueryTemplate.cs
│   │   │   └── QueryAnalyzer.cs
│   │   └── Common/
│   │       ├── GeneratorBase.cs
│   │       ├── SyntaxHelper.cs
│   │       ├── CodeBuilder.cs
│   │       └── TemplateEngine.cs
│   ├── NPA.Providers.SqlServer/  # SQL Server provider ✅ (Phase 1.4)
│   │   ├── SqlServerProvider.cs
│   │   ├── SqlServerDialect.cs
│   │   ├── SqlServerTypeConverter.cs
│   │   ├── SqlServerBulkOperationProvider.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   ├── NPA.Providers.MySql/      # MySQL provider ✅ (Phase 1.5)
│   │   ├── MySqlProvider.cs
│   │   ├── MySqlDialect.cs
│   │   ├── MySqlTypeConverter.cs
│   │   ├── MySqlBulkOperationProvider.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   ├── NPA.Providers.PostgreSql/ # PostgreSQL provider 🚧 Skeleton Only (Phase 2.5)
│   │   ├── PostgreSqlProvider.cs
│   │   ├── PostgreSqlDialect.cs
│   │   ├── PostgreSqlTypeConverter.cs
│   │   └── PostgreSqlBulkOperationProvider.cs
│   ├── NPA.Providers.Sqlite/     # SQLite provider 🚧 (Phase 2.5)
│   │   ├── SqliteProvider.cs
│   │   ├── SqliteDialect.cs
│   │   ├── SqliteTypeConverter.cs
│   │   └── SqliteBulkOperationProvider.cs
│   ├── NPA.Migrations/           # Database migrations 🚧 Skeleton Only (Phase 5.2)
│   │   ├── IMigration.cs
│   │   ├── MigrationBase.cs
│   │   ├── MigrationRunner.cs
│   │   ├── MigrationGenerator.cs
│   │   ├── SchemaComparer.cs
│   │   └── MigrationHistory.cs
│   ├── NPA.Monitoring/           # Performance monitoring 🚧 Planned (Phase 5.3)
│   │   ├── IPerformanceMonitor.cs
│   │   ├── PerformanceMonitor.cs
│   │   ├── MetricsCollector.cs
│   │   ├── QueryProfiler.cs
│   │   ├── ConnectionPoolMonitor.cs
│   │   └── PerformanceDashboard.cs
│   └── NPA/                      # Main library assembly 🚧 Planned (Phase 6.4)
├── tests/
│   ├── NPA.Core.Tests/                     # Unit tests ✅
│   │   ├── Annotations/
│   │   ├── Core/
│   │   ├── Metadata/
│   │   ├── Query/
│   │   ├── Integration/
│   │   └── TestEntities/
│   ├── NPA.Extensions.Tests/               # 🚧 Skeleton Only
│   ├── NPA.Generators.Tests/               # ✅ Implemented (Phase 1.6)
│   ├── NPA.Providers.SqlServer.Tests/      # ✅ Implemented (Phase 1.4)
│   │   ├── SqlServerProviderTests.cs
│   │   ├── SqlServerDialectTests.cs
│   │   └── SqlServerTypeConverterTests.cs
│   ├── NPA.Providers.MySql.Tests/          # ✅ Implemented (Phase 1.5)
│   │   ├── MySqlProviderTests.cs
│   │   ├── MySqlDialectTests.cs
│   │   └── MySqlTypeConverterTests.cs
│   ├── NPA.Providers.PostgreSql.Tests/     # ✅ Implemented
│   │   └── PostgreSqlProviderTests.cs
│   ├── NPA.Providers.Sqlite.Tests/         # 🚧 Planned (Phase 2.5)
│   ├── NPA.Migrations.Tests/               # 🚧 Skeleton Only
│   ├── NPA.Monitoring.Tests/               # 🚧 Skeleton Only
│   └── NPA.Integration.Tests/              # 🚧 Skeleton Only
├── samples/
│   ├── BasicUsage/               # Sample application ✅
│   │   ├── Program.cs
│   │   ├── User.cs
│   │   └── BasicUsage.csproj
│   ├── AdvancedQueries/          # 🚧 Planned (Phase 2.3)
│   │   ├── Program.cs
│   │   ├── ComplexQueries.cs
│   │   └── AdvancedQueries.csproj
│   ├── WebApplication/           # 🚧 Planned (Phase 2.4)
│   │   ├── Controllers/
│   │   ├── Models/
│   │   ├── Services/
│   │   ├── Program.cs
│   │   └── WebApplication.csproj
│   ├── RepositoryPattern/        # 🚧 Planned (Phase 2.4)
│   │   ├── Repositories/
│   │   ├── Services/
│   │   ├── Program.cs
│   │   └── RepositoryPattern.csproj
│   └── SourceGeneratorDemo/      # 🚧 Planned (Phase 4.1)
│       ├── Generated/
│       ├── Interfaces/
│       ├── Program.cs
│       └── SourceGeneratorDemo.csproj
├── tools/
│   ├── NPA.CLI/                  # Command line tools 🚧 Planned (Phase 6.2)
│   │   ├── Program.cs
│   │   ├── Commands/
│   │   ├── Generators/
│   │   └── NPA.CLI.csproj
│   ├── NPA.Migrate/              # Migration tool 🚧 Planned (Phase 5.2)
│   │   ├── Program.cs
│   │   ├── Migrations/
│   │   └── NPA.Migrate.csproj
│   └── NPA.Profiler/             # Profiling tool 🚧 Planned (Phase 6.3)
│       ├── Program.cs
│       ├── Analyzers/
│       └── NPA.Profiler.csproj
├── extensions/
│   └── NPA.VSCodeExtension/      # VS Code extension 🚧 Planned (Phase 6.1)
│       ├── Commands/
│       ├── Snippets/
│       ├── IntelliSense/
│       └── NPA.VSCodeExtension.csproj
├── docs/                         # Documentation ✅
│   ├── GettingStarted.md
│   ├── checklist.md
│   ├── EntityMapping.md          # 🚧 Planned (Phase 6.4)
│   ├── Querying.md               # 🚧 Planned (Phase 6.4)
│   ├── Relationships.md          # 🚧 Planned (Phase 6.4)
│   ├── Configuration.md          # 🚧 Planned (Phase 6.4)
│   ├── RepositoryPattern.md      # 🚧 Planned (Phase 6.4)
│   ├── SourceGenerators.md       # 🚧 Planned (Phase 6.4)
│   ├── Performance.md            # 🚧 Planned (Phase 6.4)
│   ├── Migrations.md             # 🚧 Planned (Phase 6.4)
│   ├── Monitoring.md             # 🚧 Planned (Phase 6.4)
│   ├── BestPractices.md          # 🚧 Planned (Phase 6.4)
│   ├── Troubleshooting.md        # 🚧 Planned (Phase 6.4)
│   ├── API/
│   │   ├── NPA.Core/
│   │   ├── NPA.Extensions/
│   │   ├── NPA.Generators/
│   │   └── NPA.Providers/
│   └── tasks/
│       ├── phase1.1-basic-entity-mapping-with-attributes/
│       ├── phase1.2-entity-manager-with-crud-operations/
│       ├── phase1.3-simple-query-support/
│       ├── phase1.4-sql-server-provider/
│       ├── phase1.5-mysql-mariadb-provider/
│       ├── phase1.6-repository-source-generator-basic/
│       ├── phase2.1-relationship-mapping/
│       ├── phase2.2-composite-key-support/
│       ├── phase2.3-jpql-query-language/
│       ├── phase2.4-repository-pattern/
│       ├── phase2.5-additional-database-providers/
│       ├── phase2.6-metadata-source-generator/
│       ├── phase3.1-transaction-management/
│       ├── phase3.2-cascade-operations/
│       ├── phase3.3-bulk-operations/
│       ├── phase3.4-lazy-loading/
│       ├── phase3.5-connection-pooling/
│       ├── phase4.1-advanced-generator/
│       ├── phase4.2-query-method-generation/
│       ├── phase5.1-caching-support/
│       ├── phase5.2-database-migrations/
│       ├── phase5.3-performance-monitoring/
│       ├── phase6.1-vscode-extension/
│       ├── phase6.2-code-generation-tools/
│       ├── phase6.3-performance-profiling/
│       └── phase6.4-comprehensive-documentation/
├── scripts/
│   ├── build.ps1                 # Build script 🚧 Planned (Phase 6.4)
│   ├── test.ps1                  # Test script 🚧 Planned (Phase 6.4)
│   ├── publish.ps1               # Publish script 🚧 Planned (Phase 6.4)
│   └── setup.ps1                 # Setup script 🚧 Planned (Phase 6.4)
├── templates/
│   ├── ProjectTemplates/         # Project templates 🚧 Planned (Phase 6.1)
│   │   ├── NPA.WebAPI/
│   │   ├── NPA.Console/
│   │   └── NPA.ClassLibrary/
│   └── ItemTemplates/            # Item templates 🚧 Planned (Phase 6.1)
│       ├── Entity.cs
│       ├── Repository.cs
│       └── Service.cs
├── benchmarks/                   # Performance benchmarks 🚧 Planned (Phase 5.3)
│   ├── EntityManagerBenchmarks.cs
│   ├── QueryBenchmarks.cs
│   ├── RepositoryBenchmarks.cs
│   └── Benchmarks.csproj
├── NPA.sln                       # Solution file ✅
├── NPA.sln.DotSettings.user      # IDE settings ✅
├── Directory.Build.props         # Build properties 🚧 Planned (Phase 6.4)
├── Directory.Packages.props      # Package management 🚧 Planned (Phase 6.4)
├── global.json                   # .NET version 🚧 Planned (Phase 6.4)
├── .gitignore                    # Git ignore rules 🚧 Planned (Phase 6.4)
├── .editorconfig                 # Editor configuration 🚧 Planned (Phase 6.4)
├── LICENSE                       # License file 🚧 Planned (Phase 6.4)
├── CONTRIBUTING.md               # Contributing guide 🚧 Planned (Phase 6.4)
├── CHANGELOG.md                  # Change log 🚧 Planned (Phase 6.4)
└── README.md                     # This file ✅
```

**Legend:**
- ✅ **Implemented** - Fully implemented, tested, and working
- 🚧 **Skeleton Only** - Project structure exists but contains placeholder/TODO implementations
- 🚧 **Planned** - Scheduled for future implementation
- 📋 **Design Phase** - Under design/planning

---

## 🔧 Currently Implemented Components

### 1. Entity Manager ✅
- **IEntityManager**: Main interface for entity operations
- **EntityManager**: Core implementation with Dapper integration
- **IChangeTracker**: Manages entity state and change tracking
- **EntityState**: Entity lifecycle states (Detached, Added, Modified, Deleted)

### 2. Metadata System ✅
- **EntityMetadata**: Stores entity mapping information
- **PropertyMetadata**: Property-level mapping details
- **IMetadataProvider**: Provides entity metadata
- **MetadataProvider**: Builds metadata from attributes

### 3. Query Engine ✅
- **IQuery**: Fluent API for building and executing queries
- **QueryParser**: Parses CPQL-like queries
- **SqlGenerator**: Converts CPQL to native SQL
- **ParameterBinder**: Safe parameter binding with SQL injection prevention

### 4. Entity Mapping Attributes ✅
- **EntityAttribute**: Marks classes as entities
- **TableAttribute**: Maps entities to database tables
- **IdAttribute**: Marks primary key properties
- **ColumnAttribute**: Maps properties to database columns
- **GeneratedValueAttribute**: Specifies primary key generation strategy
- **GenerationType**: Primary key generation strategies

## 🚧 Planned Components (Not Yet Implemented)

### 5. Repository System
- **IRepository**: Base repository interface
- **BaseRepository**: Default implementation
- **Custom Repositories**: User-defined repository methods

### 6. Source Generators
- **RepositoryGenerator**: Generates repository implementations from interfaces
- **MetadataGenerator**: Generates compile-time metadata for entities
- **QueryGenerator**: Generates optimized query methods

### 7. Advanced Features
- **Relationship Mapping**: OneToMany, ManyToOne, ManyToMany
- **Transaction Management**: Declarative and programmatic transactions
- **Cascade Operations**: Automatic related entity operations
- **Lazy Loading**: On-demand relationship loading
- **Bulk Operations**: Efficient batch processing
- **Database Providers**: SQL Server, PostgreSQL, MySQL, SQLite specific features

## 🚀 Getting Started

### 1. Installation
Since NPA is currently in development, you need to build it from source:

```bash
git clone https://github.com/your-org/npa.git
cd npa
dotnet build
```

### 2. Configuration
```csharp
// Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Register NPA services
builder.Services.AddSingleton<IMetadataProvider, MetadataProvider>();
builder.Services.AddScoped<IDbConnection>(provider =>
{
    var connectionString = "Server=localhost;Database=MyApp;Trusted_Connection=true;";
    return new SqlConnection(connectionString);
});
builder.Services.AddScoped<IEntityManager, EntityManager>();
```

### 3. Define Entities
```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("username", nullable: false)]
    public string Username { get; set; }
    
    [Column("email")]
    public string Email { get; set; }
}
```

### 4. Use EntityManager
```csharp
public class UserService
{
    private readonly IEntityManager entityManager;
    
    public UserService(IEntityManager entityManager)
    {
        this.entityManager = entityManager;
    }
    
    public async Task<User> CreateUserAsync(string username, string email)
    {
        var user = new User { Username = username, Email = email };
        await entityManager.PersistAsync(user);
        await entityManager.FlushAsync();
        return user;
    }
}
```

## 🎯 Key Design Principles

### 1. **Performance First**
- Leverage Dapper's excellent performance
- Minimal overhead over raw SQL
- Efficient metadata caching
- Optimized query generation

### 2. **Developer Experience**
- Familiar JPA-like API
- Strong typing and IntelliSense support
- Comprehensive error messages
- Extensive logging and debugging support

### 3. **Flexibility**
- Support multiple database providers
- Extensible query language
- Custom repository implementations
- Plugin architecture for extensions

### 4. **Standards Compliance**
- Follow .NET conventions
- Implement JPA patterns where applicable
- Consistent with existing .NET ecosystem

## 🔄 Development Roadmap

### Phase 1: Core Foundation
- [x] **1.1 Basic entity mapping with attributes** ✅ COMPLETED
- [x] **1.2 EntityManager with CRUD operations** ✅ COMPLETED  
- [x] **1.3 Simple query support** ✅ COMPLETED
- [ ] **1.4 SQL Server provider** 🚧 IN PROGRESS
- [ ] **1.5 MySQL/MariaDB provider** 📋 PLANNED
- [ ] **1.6 Repository Source Generator (basic)** 📋 PLANNED

### Phase 2: Advanced Features
- [ ] **2.1 Relationship mapping** (OneToMany, ManyToOne, ManyToMany) 📋 PLANNED
- [ ] **2.2 Composite key support** 📋 PLANNED
- [ ] **2.3 JPQL-like query language** 📋 PLANNED
- [ ] **2.4 Repository pattern implementation** 📋 PLANNED
- [ ] **2.5 Additional database providers** (PostgreSQL, MySQL, SQLite) 📋 PLANNED
- [ ] **2.6 Metadata Source Generator** 📋 PLANNED

### Phase 3: Transaction & Performance
- [ ] **3.1 Transaction management** (declarative and programmatic) 📋 PLANNED
- [ ] **3.2 Cascade operations** 📋 PLANNED
- [ ] **3.3 Bulk operations** (insert, update, delete) 📋 PLANNED
- [ ] **3.4 Lazy loading support** 📋 PLANNED
- [ ] **3.5 Connection pooling optimization** 📋 PLANNED

### Phase 4: Source Generator Enhancement
- [ ] **4.1 Advanced repository generation patterns** 📋 PLANNED
- [ ] **4.2 Query method generation from naming conventions** 📋 PLANNED
- [ ] **4.3 Composite key repository generation** 📋 PLANNED
- [ ] **4.4 Many-to-many relationship query generation** 📋 PLANNED
- [ ] **4.5 Incremental generator optimizations** 📋 PLANNED
- [ ] **4.6 Custom generator attributes** 📋 PLANNED
- [ ] **4.7 IntelliSense support for generated code** 📋 PLANNED

### Phase 5: Enterprise Features
- [ ] **5.1 Caching support** 📋 PLANNED
- [ ] **5.2 Database migrations** 📋 PLANNED
- [ ] **5.3 Performance monitoring** 📋 PLANNED
- [ ] **5.4 Audit logging** 📋 PLANNED
- [ ] **5.5 Multi-tenant support** 📋 PLANNED

### Phase 6: Tooling & Ecosystem
- [ ] **6.1 VS Code extension** 📋 PLANNED
- [ ] **6.2 Code generation tools** 📋 PLANNED
- [ ] **6.3 Performance profiling** 📋 PLANNED
- [ ] **6.4 Comprehensive documentation** 📋 PLANNED

**Current Progress: 3/33 tasks completed (9%)**

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Dapper**: For providing the excellent underlying data access layer
- **Java JPA**: For the inspiration and API design patterns
- **.NET Community**: For the vibrant ecosystem and support

---

## 📝 Documentation Status

**Current State**: This README serves as both a current implementation guide and a comprehensive roadmap for future development. The document clearly distinguishes between:

- ✅ **Implemented Features**: Currently available and working (Phase 1.1-1.3)
- 🚧 **Planned Features**: Detailed implementation plans for future phases
- 📋 **Design Phase**: Features under design/planning

**Purpose**: The detailed implementation plans are kept as a reference to guide future development and provide a complete vision of the project's intended capabilities.

**Note**: This is an architectural plan document. The actual implementation will be developed incrementally following this roadmap.
