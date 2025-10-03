# NPA - JPA-like ORM for .NET

A lightweight, high-performance Object-Relational Mapping library for .NET that provides Java Persistence API (JPA) inspired features while leveraging Dapper's excellent performance as the underlying data access technology.

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

## 📋 Core Features

### 1. Entity Mapping
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
    
    [OneToMany(mappedBy = "User")]
    public ICollection<Order> Orders { get; set; }
}
```

### 2. Repository Pattern
```csharp
public interface IUserRepository : IRepository<User, long>
{
    Task<User> FindByUsernameAsync(string username);
    Task<IEnumerable<User>> FindByEmailDomainAsync(string domain);
}

[Repository]
public class UserRepository : BaseRepository<User, long>, IUserRepository
{
    public async Task<User> FindByUsernameAsync(string username)
    {
        return await EntityManager
            .CreateQuery<User>("SELECT * FROM users WHERE username = @username")
            .SetParameter("username", username)
            .GetSingleResultAsync();
    }
}
```

### 3. EntityManager API
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
    
    public async Task<User> FindUserAsync(long id)
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

### 4. Query Language (JPQL-like)
```csharp
// Named Queries
[Entity]
[NamedQuery("User.findByEmail", "SELECT u FROM User u WHERE u.Email = :email")]
[NamedQuery("User.findActiveUsers", "SELECT u FROM User u WHERE u.IsActive = true")]
public class User { /* ... */ }

// Dynamic Queries
var query = entityManager
    .CreateQuery<User>("SELECT u FROM User u WHERE u.Username = :username AND u.IsActive = :active")
    .SetParameter("username", "john")
    .SetParameter("active", true);

var users = await query.GetResultListAsync();
```

### 5. Advanced Relationship Mapping
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

// Composite Key Example
[Entity]
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

// Many-to-Many Example
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
            
            // Update inventory
            await UpdateInventoryAsync(items);
            
            await entityManager.CommitAsync();
            return order;
        }
        catch
        {
            await entityManager.RollbackAsync();
            throw;
        }
    }
    
    // Programmatic transaction management
    public async Task TransferFundsAsync(long fromAccountId, long toAccountId, decimal amount)
    {
        await entityManager.ExecuteInTransactionAsync(async () =>
        {
            var fromAccount = await entityManager.FindAsync<Account>(fromAccountId);
            var toAccount = await entityManager.FindAsync<Account>(toAccountId);
            
            fromAccount.Balance -= amount;
            toAccount.Balance += amount;
            
            await entityManager.MergeAsync(fromAccount);
            await entityManager.MergeAsync(toAccount);
        });
    }
}
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

## 🏛️ Project Structure

```
NPA/
├── src/
│   ├── NPA.Core/                 # Core library
│   │   ├── Annotations/                # JPA-like attributes
│   │   │   ├── EntityAttribute.cs
│   │   │   ├── TableAttribute.cs
│   │   │   ├── IdAttribute.cs
│   │   │   ├── ColumnAttribute.cs
│   │   │   ├── GeneratedValueAttribute.cs
│   │   │   ├── OneToManyAttribute.cs
│   │   │   ├── ManyToOneAttribute.cs
│   │   │   ├── ManyToManyAttribute.cs
│   │   │   ├── JoinColumnAttribute.cs
│   │   │   ├── JoinTableAttribute.cs
│   │   │   ├── NamedQueryAttribute.cs
│   │   │   ├── TransactionalAttribute.cs
│   │   │   ├── StoredProcedureAttribute.cs
│   │   │   ├── QueryAttribute.cs
│   │   │   ├── BulkOperationAttribute.cs
│   │   │   ├── MultiMappingAttribute.cs
│   │   │   ├── ConnectionStringAttribute.cs
│   │   │   ├── CommandTimeoutAttribute.cs
│   │   │   ├── PaginationAttribute.cs
│   │   │   ├── CascadeType.cs
│   │   │   └── GenerationType.cs
│   │   ├── Core/                       # Core interfaces and classes
│   │   │   ├── IEntityManager.cs
│   │   │   ├── EntityManager.cs
│   │   │   ├── IRepository.cs
│   │   │   ├── BaseRepository.cs
│   │   │   ├── IQuery.cs
│   │   │   ├── Query.cs
│   │   │   ├── ITransaction.cs
│   │   │   ├── Transaction.cs
│   │   │   └── CompositeKey.cs
│   │   ├── Metadata/                   # Entity metadata management
│   │   │   ├── EntityMetadata.cs
│   │   │   ├── PropertyMetadata.cs
│   │   │   ├── RelationshipMetadata.cs
│   │   │   ├── CompositeKeyMetadata.cs
│   │   │   ├── JoinTableMetadata.cs
│   │   │   └── MetadataBuilder.cs
│   │   ├── Query/                      # Query building and execution
│   │   │   ├── QueryBuilder.cs
│   │   │   ├── JPQLParser.cs
│   │   │   ├── SqlGenerator.cs
│   │   │   └── ParameterBinder.cs
│   │   ├── Validation/                 # Entity validation
│   │   │   ├── IEntityValidator.cs
│   │   │   ├── EntityValidator.cs
│   │   │   └── ValidationResult.cs
│   │   └── Configuration/              # Configuration management
│   │       ├── NPAConfiguration.cs
│   │       ├── DatabaseProvider.cs
│   │       └── ConnectionManager.cs
│   ├── NPA.Extensions/           # Extensions and utilities
│   │   ├── DependencyInjection/
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   └── ServiceCollectionExtensions.Configuration.cs
│   │   ├── Logging/
│   │   │   └── EntityManagerLogger.cs
│   │   └── Utilities/
│   │       ├── ReflectionHelper.cs
│   │       ├── TypeHelper.cs
│   │       └── StringHelper.cs
│   ├── NPA.Generators/           # Source Generators
│   │   ├── RepositoryGenerator/
│   │   │   ├── RepositoryGenerator.cs
│   │   │   ├── RepositorySyntaxReceiver.cs
│   │   │   ├── RepositoryCodeGenerator.cs
│   │   │   └── RepositoryTemplate.cs
│   │   ├── MetadataGenerator/
│   │   │   ├── MetadataGenerator.cs
│   │   │   ├── EntitySyntaxReceiver.cs
│   │   │   ├── MetadataCodeGenerator.cs
│   │   │   └── MetadataTemplate.cs
│   │   └── QueryGenerator/
│   │       ├── QueryGenerator.cs
│   │       ├── QuerySyntaxReceiver.cs
│   │       ├── QueryCodeGenerator.cs
│   │       └── QueryTemplate.cs
│   ├── NPA.Providers/            # Database provider implementations
│   │   ├── SqlServer/
│   │   │   ├── SqlServerProvider.cs
│   │   │   └── SqlServerDialect.cs
│   │   ├── PostgreSql/
│   │   │   ├── PostgreSqlProvider.cs
│   │   │   └── PostgreSqlDialect.cs
│   │   ├── MySql/
│   │   │   ├── MySqlProvider.cs
│   │   │   └── MySqlDialect.cs
│   │   └── Sqlite/
│   │       ├── SqliteProvider.cs
│   │       └── SqliteDialect.cs
│   └── NPA/                      # Main library assembly
├── tests/
│   ├── NPA.Core.Tests/
│   ├── NPA.Extensions.Tests/
│   ├── NPA.Providers.Tests/
│   └── NPA.Integration.Tests/
├── samples/
│   ├── BasicUsage/
│   ├── AdvancedQueries/
│   └── WebApplication/
├── docs/
│   ├── GettingStarted.md
│   ├── EntityMapping.md
│   ├── Querying.md
│   ├── Relationships.md
│   └── Configuration.md
├── NPA.sln
└── README.md
```

## 🔧 Core Components

### 1. Entity Manager
- **IEntityManager**: Main interface for entity operations
- **EntityManager**: Core implementation with Dapper integration
- **Persistence Context**: Manages entity state and change tracking

### 2. Metadata System
- **EntityMetadata**: Stores entity mapping information
- **PropertyMetadata**: Property-level mapping details
- **RelationshipMetadata**: Relationship mapping configuration
- **MetadataBuilder**: Builds metadata from attributes and conventions

### 3. Query Engine
- **JPQLParser**: Parses JPQL-like queries
- **SqlGenerator**: Converts JPQL to native SQL
- **QueryBuilder**: Fluent API for building queries
- **ParameterBinder**: Safe parameter binding

### 4. Repository System
- **IRepository**: Base repository interface
- **BaseRepository**: Default implementation
- **Custom Repositories**: User-defined repository methods

### 5. Source Generators
- **RepositoryGenerator**: Generates repository implementations from interfaces
- **MetadataGenerator**: Generates compile-time metadata for entities
- **QueryGenerator**: Generates optimized query methods
- **Incremental Processing**: Only processes changed code for performance

### 6. Advanced Features
- **Composite Keys**: Support for multi-column primary keys
- **Many-to-Many Relationships**: Automatic join table management
- **Transaction Management**: Declarative and programmatic transactions
- **Cascade Operations**: Automatic related entity operations
- **Lazy Loading**: On-demand relationship loading
- **Bulk Operations**: Efficient batch processing

## 🚀 Getting Started

### 1. Installation
```bash
dotnet add package NPA
```

### 2. Configuration
```csharp
// Program.cs or Startup.cs
services.AddNPA(config =>
{
    config.ConnectionString = "Server=localhost;Database=MyApp;Trusted_Connection=true;";
    config.DatabaseProvider = DatabaseProvider.SqlServer;
    config.AutoCreateSchema = true;
    config.LogSql = true;
});
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

## 🔧 Source Generator Details

### Repository Generation Strategy

The NPA Source Generator will automatically generate repository implementations based on interface definitions and naming conventions:

#### 1. **Method Name Analysis**
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

#### 2. **Convention-Based Query Generation**
- `FindBy{Property}Async` → `WHERE {property} = @{property}`
- `Find{Property}ContainingAsync` → `WHERE {property} LIKE '%@{property}%'`
- `Find{Property}StartingWithAsync` → `WHERE {property} LIKE '@{property}%'`
- `Find{Property}EndingWithAsync` → `WHERE {property} LIKE '%@{property}'`
- `FindBy{Property}GreaterThanAsync` → `WHERE {property} > @{property}`
- `FindBy{Property}LessThanAsync` → `WHERE {property} < @{property}`

#### 3. **Dapper Feature Integration**
The source generator supports all Dapper capabilities:

##### **Multi-Mapping Support**
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

##### **Stored Procedure Support**
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

##### **Dynamic Parameters**
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

##### **Bulk Operations**
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

##### **Grid Reader Support**
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

##### **Custom SQL with Parameters**
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

##### **Async/Await Patterns**
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

##### **Transaction Support**
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    [Transactional]
    Task<Order> CreateOrderWithItemsAsync(Order order, IEnumerable<OrderItem> items);
    
    Task<Order> CreateOrderInTransactionAsync(Order order, IDbTransaction transaction);
}
```

##### **Pagination Support**
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

##### **Custom Type Handlers**
```csharp
[Repository]
public interface IProductRepository : IRepository<Product, long>
{
    Task<IEnumerable<Product>> GetProductsByTagsAsync(string[] tags);
}

// Generated implementation with custom type handling
public async Task<IEnumerable<Product>> GetProductsByTagsAsync(string[] tags)
{
    return await Connection.QueryAsync<Product>(
        "SELECT * FROM products WHERE tags && @tags", 
        new { tags });
}
```

##### **Connection String Management**
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

##### **Command Timeout Configuration**
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

##### **Result Set Mapping**
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

#### 3. **Smart Column Selection**
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

#### 4. **Custom Query Attributes**
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

#### 5. **Metadata Generation**
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

### Generator Pipeline

1. **Syntax Analysis**: Detects interfaces with `[Repository]` attribute
2. **Method Analysis**: Analyzes method signatures and naming patterns
3. **Return Type Analysis**: Examines return types to determine required columns
4. **Column Mapping**: Maps entity properties to database columns using conventions/attributes
5. **Dapper Feature Detection**: Identifies Dapper-specific patterns and attributes
6. **Query Generation**: Creates SQL queries with specific column selections
7. **Code Generation**: Generates implementation classes with full Dapper integration
8. **Metadata Generation**: Creates compile-time metadata
9. **Validation**: Validates generated code for correctness

### Complete Dapper Feature Support

The NPA Source Generator supports **ALL** Dapper capabilities:

#### **Core Dapper Methods**
- `QueryAsync<T>()` - Async query with mapping
- `QueryFirstOrDefaultAsync<T>()` - Single result with default
- `QuerySingleAsync<T>()` - Single result (throws if none/multiple)
- `QueryMultipleAsync()` - Multiple result sets
- `ExecuteAsync()` - Execute commands
- `ExecuteScalarAsync<T>()` - Single scalar value

#### **Advanced Dapper Features**
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

#### **Generated Code Quality**
- **Type Safety**: Full compile-time validation
- **Performance**: Optimized Dapper usage
- **IntelliSense**: Complete IDE support
- **Error Handling**: Comprehensive exception management
- **Logging**: Built-in query logging
- **Testing**: Easy unit testing support

### Column Selection Strategy

The generator uses a sophisticated approach to determine which columns to select:

#### **Entity Property Analysis**
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

#### **DTO Support**
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

#### **Convention-Based Column Mapping**
- Property name → Column name (snake_case conversion)
- `Id` property → Primary key column
- Navigation properties → Excluded from SELECT
- Complex types → Analyzed recursively

## 🔧 Advanced Features Details

### Composite Key Support

NPA supports composite keys for entities that require multiple columns to uniquely identify records:

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

### Many-to-Many Relationships

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

### Transaction Management

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

### Cascade Operations

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

### Bulk Operations

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

## 🔄 Development Roadmap

### Phase 1: Core Foundation
- [ ] Basic entity mapping with attributes
- [ ] EntityManager with CRUD operations
- [ ] Simple query support
- [ ] SQL Server provider
- [ ] Repository Source Generator (basic)

### Phase 2: Advanced Features
- [ ] Relationship mapping (OneToMany, ManyToOne, ManyToMany)
- [ ] Composite key support
- [ ] JPQL-like query language
- [ ] Repository pattern implementation
- [ ] Additional database providers (PostgreSQL, MySQL, SQLite)
- [ ] Metadata Source Generator

### Phase 3: Transaction & Performance
- [ ] Transaction management (declarative and programmatic)
- [ ] Cascade operations
- [ ] Bulk operations (insert, update, delete)
- [ ] Lazy loading support
- [ ] Connection pooling optimization

### Phase 4: Source Generator Enhancement
- [ ] Advanced repository generation patterns
- [ ] Query method generation from naming conventions
- [ ] Composite key repository generation
- [ ] Many-to-many relationship query generation
- [ ] Incremental generator optimizations
- [ ] Custom generator attributes
- [ ] IntelliSense support for generated code

### Phase 5: Enterprise Features
- [ ] Caching support
- [ ] Database migrations
- [ ] Performance monitoring
- [ ] Audit logging
- [ ] Multi-tenant support

### Phase 6: Tooling & Ecosystem
- [ ] Visual Studio extensions
- [ ] Code generation tools
- [ ] Performance profiling
- [ ] Comprehensive documentation

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Dapper**: For providing the excellent underlying data access layer
- **Java JPA**: For the inspiration and API design patterns
- **.NET Community**: For the vibrant ecosystem and support

---

**Note**: This is an architectural plan document. The actual implementation will be developed incrementally following this roadmap.
