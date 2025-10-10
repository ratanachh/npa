# Repository Pattern Sample

This sample demonstrates the **Repository Pattern** implementation in NPA (Phase 2.4) with **PostgreSQL Testcontainers** for real database operations.

## Features Demonstrated

### 1. Basic Repository Operations
- Generic `IRepository<T, TKey>` interface
- CRUD operations through repositories
- Dependency injection integration

### 2. Custom Repository Methods
- Creating custom repository interfaces
- Implementing domain-specific queries
- Extending `CustomRepositoryBase<T, TKey>`

### 3. LINQ Expression Support
- Using predicates: `FindAsync(u => u.IsActive)`
- String methods: `Contains`, `StartsWith`, `EndsWith`
- Comparison operators: `>, <, >=, <=, ==, !=`

### 4. Ordering and Paging
- Order by with ASC/DESC
- Skip and Take for pagination
- Combined ordering and paging

### 5. Repository Factory
- Creating repositories dynamically
- Custom repository registration
- Fallback to base repository

## Code Structure

```
Program.cs
├── IUserRepository        # Custom repository interface
├── UserRepository         # Custom repository implementation
├── User                   # Entity with NPA annotations
└── Program                # Demo entry point with DI setup
```

## Prerequisites

- Docker Desktop or Docker Engine running
- .NET 8.0 SDK

## Running the Sample

```bash
cd samples/RepositoryPattern
dotnet run
```

The sample will:
1. Start a PostgreSQL 17 Alpine container automatically
2. Create the necessary database tables
3. Demonstrate all repository operations with a real database
4. Clean up and stop the container when complete

**Note:** The first run may take longer as it downloads the PostgreSQL Docker image.

## Key Concepts

### Basic Repository Usage

```csharp
public class UserService
{
    private readonly IRepository<User, long> _userRepository;
    
    public UserService(IRepository<User, long> userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<User?> GetUserAsync(long id)
    {
        return await _userRepository.GetByIdAsync(id);
    }
    
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _userRepository.FindAsync(u => u.IsActive);
    }
}
```

### Custom Repository

```csharp
public interface IUserRepository : IRepository<User, long>
{
    Task<IEnumerable<User>> FindByEmailDomainAsync(string domain);
}

public class UserRepository : CustomRepositoryBase<User, long>, IUserRepository
{
    public UserRepository(IDbConnection connection, IEntityManager entityManager, IMetadataProvider metadataProvider)
        : base(connection, entityManager, metadataProvider)
    {
    }
    
    public async Task<IEnumerable<User>> FindByEmailDomainAsync(string domain)
    {
        return await FindAsync(u => u.Email.Contains($"@{domain}"));
    }
}
```

### Dependency Injection Setup

```csharp
// Using PostgreSQL with Testcontainers
var postgresContainer = new PostgreSqlBuilder()
    .WithImage("postgres:17-alpine")
    .WithDatabase("npa_repo_demo")
    .WithUsername("npa_user")
    .WithPassword("npa_password")
    .Build();

await postgresContainer.StartAsync();
var connectionString = postgresContainer.GetConnectionString();

// Configure services
services.AddSingleton<IDbConnection>(sp =>
{
    var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    return conn;
});
services.AddScoped<IEntityManager, EntityManager>();
services.AddSingleton<IMetadataProvider, MetadataProvider>();

// Register generic repository
services.AddScoped(typeof(IRepository<,>), typeof(BaseRepository<,>));

// Register custom repositories
services.AddScoped<IUserRepository, UserRepository>();

// Register factory
services.AddScoped<IRepositoryFactory, RepositoryFactory>();
```

## Benefits

1. **Clean Abstraction** - Separates business logic from data access
2. **Testability** - Easy to mock repository interfaces
3. **Reusability** - Common CRUD operations in base class
4. **Extensibility** - Custom methods for domain-specific queries
5. **Type Safety** - Strong typing with compile-time checks
6. **Performance** - Built on Dapper and EntityManager
7. **Isolated Testing** - Uses Testcontainers for reproducible database tests
8. **No Setup Required** - Database is automatically provisioned and cleaned up

## Related Documentation

- [Phase 2.4 Task Documentation](../../docs/tasks/phase2.4-repository-pattern/README.md)
- [Phase 2.4 Implementation Summary](../../docs/tasks/phase2.4-repository-pattern/IMPLEMENTATION_SUMMARY.md)
- [NPA Core Documentation](../../README.md)

## Notes

- This sample uses **PostgreSQL Testcontainers** for a real database experience
- The database is automatically started, configured, and cleaned up
- Real CRUD operations are performed against the containerized PostgreSQL database
- Custom queries can be added by extending `CustomRepositoryBase`
- For complex queries, use the `ExecuteQueryAsync` helper methods
- The sample demonstrates PostgreSQL-specific features like `SERIAL` primary keys and `NOW()` function

