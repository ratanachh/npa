# Getting Started with NPA

This guide will help you get started with NPA (JPA-like ORM for .NET) in your application.

## Installation

Add the NPA.Core package to your project:

```bash
dotnet add package NPA.Core
```

## Quick Start

### 1. Define Your Entity

Create a class and mark it with the `[Entity]` attribute:

```csharp
using NPA.Core.Annotations;

[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("username", nullable: false, length: 50)]
    public string Username { get; set; } = string.Empty;

    [Column("email", nullable: false, unique: true)]
    public string Email { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
```

### 2. Configure Services

Register NPA services in your `Program.cs` or `Startup.cs`:

```csharp
using Microsoft.Data.SqlClient;
using NPA.Core.Core;
using NPA.Core.Metadata;

var builder = WebApplication.CreateBuilder(args);

// Register NPA services
builder.Services.AddSingleton<IMetadataProvider, MetadataProvider>();
builder.Services.AddScoped<IDbConnection>(provider =>
{
    var connectionString = "Server=localhost;Database=MyApp;Trusted_Connection=true;";
    return new SqlConnection(connectionString);
});
builder.Services.AddScoped<IEntityManager, EntityManager>();

var app = builder.Build();
```

### 3. Use EntityManager

Inject `IEntityManager` into your services and use it for database operations:

```csharp
public class UserService
{
    private readonly IEntityManager _entityManager;

    public UserService(IEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    public async Task<User> CreateUserAsync(string username, string email)
    {
        var user = new User
        {
            Username = username,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _entityManager.PersistAsync(user);
        await _entityManager.FlushAsync();

        return user;
    }

    public async Task<User?> GetUserAsync(long id)
    {
        return await _entityManager.FindAsync<User>(id);
    }

    public async Task UpdateUserAsync(User user)
    {
        await _entityManager.MergeAsync(user);
        await _entityManager.FlushAsync();
    }

    public async Task DeleteUserAsync(long id)
    {
        await _entityManager.RemoveAsync<User>(id);
        await _entityManager.FlushAsync();
    }
}
```

## Basic Operations

### Persisting Entities

```csharp
var user = new User { Username = "john", Email = "john@example.com" };
await entityManager.PersistAsync(user);
await entityManager.FlushAsync(); // Persist changes to database
```

### Finding Entities

```csharp
// Find by primary key
var user = await entityManager.FindAsync<User>(1L);

// Find by composite key
var compositeKey = new CompositeKey();
compositeKey.SetValue("OrderId", 1L);
compositeKey.SetValue("ProductId", 2L);
var orderItem = await entityManager.FindAsync<OrderItem>(compositeKey);
```

### Updating Entities

```csharp
var user = await entityManager.FindAsync<User>(1L);
if (user != null)
{
    user.Email = "newemail@example.com";
    await entityManager.MergeAsync(user);
    await entityManager.FlushAsync();
}
```

### Deleting Entities

```csharp
// Delete by entity instance
var user = await entityManager.FindAsync<User>(1L);
if (user != null)
{
    await entityManager.RemoveAsync(user);
    await entityManager.FlushAsync();
}

// Delete by ID
await entityManager.RemoveAsync<User>(1L);
await entityManager.FlushAsync();
```

## Entity State Management

NPA tracks entity states automatically:

```csharp
// Check if entity is managed
bool isManaged = entityManager.Contains(user);

// Get entity state
EntityState? state = entityManager.ChangeTracker.GetState(user);

// Detach entity from context
entityManager.Detach(user);

// Clear all tracked entities
await entityManager.ClearAsync();
```

## Connection String Configuration

Configure your connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Trusted_Connection=true;"
  }
}
```

Then use it in your service registration:

```csharp
builder.Services.AddScoped<IDbConnection>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new SqlConnection(connectionString);
});
```

## Next Steps

- Learn about [Entity Mapping](EntityMapping.md) for advanced configuration
- Explore [Querying](Querying.md) for complex queries
- Check out [Relationships](Relationships.md) for entity relationships
- Review [Configuration](Configuration.md) for advanced settings

## Examples

See the `samples/BasicUsage` directory for a complete working example.
