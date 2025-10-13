# Phase 1.2: CRUD Operations Sample

## 📋 Task Overview

**Objective**: Create a console application demonstrating EntityManager CRUD operations with a real database.

**Priority**: High  
**Estimated Time**: 4-5 hours  
**Dependencies**: Phase 1.1, Phase 1.2 (EntityManager with CRUD Operations)  
**Target Framework**: .NET 8.0  
**Sample Name**: CrudOperationsSample

## 🎯 Success Criteria

- [ ] Sample demonstrates all CRUD operations (Create, Read, Update, Delete)
- [ ] Uses EntityManager with real PostgreSQL database
- [ ] Shows change tracking functionality
- [ ] Demonstrates batch operations with FlushAsync
- [ ] Includes proper error handling
- [ ] Uses Testcontainers for database setup
- [ ] README explains all operations
- [ ] Code is production-ready

## 📝 Detailed Requirements

### 1. Database Setup
- Use Testcontainers for PostgreSQL
- Automatic database initialization
- Schema creation on startup
- Cleanup on shutdown

### 2. CRUD Operations
- **Create**: PersistAsync for new entities
- **Read**: FindAsync by ID
- **Update**: MergeAsync for modifications
- **Delete**: RemoveAsync by entity or ID
- **Batch**: FlushAsync for pending changes

### 3. Change Tracking
- Demonstrate entity state changes
- Show tracked vs untracked entities
- Display change detection

## 🏗️ Implementation Plan

### Step 1: Project Setup
```bash
dotnet new console -n CrudOperationsSample
cd CrudOperationsSample
dotnet add reference ../../src/NPA.Core/NPA.Core.csproj
dotnet add reference ../../src/NPA.Providers.PostgreSql/NPA.Providers.PostgreSql.csproj
dotnet add package Npgsql --version 9.0.3
dotnet add package Testcontainers --version 3.6.0
dotnet add package Testcontainers.PostgreSql --version 3.6.0
dotnet add package Microsoft.Extensions.Logging.Console --version 7.0.0
```

### Step 2: Create Entity Classes

**Customer.cs**
```csharp
using NPA.Core.Annotations;

namespace CrudOperationsSample.Entities;

[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("first_name", Length = 50, IsNullable = false)]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name", Length = 50, IsNullable = false)]
    public string LastName { get; set; } = string.Empty;

    [Column("email", Length = 255, IsNullable = false, IsUnique = true)]
    public string Email { get; set; } = string.Empty;

    [Column("phone", Length = 20)]
    public string? Phone { get; set; }

    [Column("created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("is_active", IsNullable = false)]
    public bool IsActive { get; set; } = true;

    public override string ToString()
    {
        return $"Customer[Id={Id}, Name={FirstName} {LastName}, Email={Email}, Active={IsActive}]";
    }
}
```

### Step 3: Create Database Manager

**DatabaseManager.cs**
```csharp
using System.Data;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CrudOperationsSample;

public class DatabaseManager : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;
    private NpgsqlConnection? _connection;

    public DatabaseManager()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("sampledb")
            .WithUsername("sample_user")
            .WithPassword("sample_password")
            .Build();
    }

    public async Task<IDbConnection> StartAsync()
    {
        Console.WriteLine("Starting PostgreSQL container...");
        await _container.StartAsync();
        
        var connectionString = _container.GetConnectionString();
        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync();
        
        await InitializeDatabaseAsync();
        
        Console.WriteLine("Database ready!\n");
        return _connection;
    }

    private async Task InitializeDatabaseAsync()
    {
        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS customers (
                id BIGSERIAL PRIMARY KEY,
                first_name VARCHAR(50) NOT NULL,
                last_name VARCHAR(50) NOT NULL,
                email VARCHAR(255) NOT NULL UNIQUE,
                phone VARCHAR(20),
                created_at TIMESTAMP NOT NULL,
                updated_at TIMESTAMP,
                is_active BOOLEAN NOT NULL DEFAULT true
            );";

        await using var command = new NpgsqlCommand(createTableSql, _connection);
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
        
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}
```

### Step 4: Implement CRUD Operations

**CrudOperations.cs**
```csharp
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Providers.PostgreSql;
using CrudOperationsSample.Entities;
using System.Data;

namespace CrudOperationsSample;

public class CrudOperations
{
    private readonly IEntityManager _entityManager;

    // Recommended: Use DI with provider extensions
    public CrudOperations(IEntityManager entityManager)
    {
        _entityManager = entityManager;
    }
    
    // Alternative: Manual setup (not recommended for production)
    public CrudOperations(IDbConnection connection, ILogger<EntityManager> logger)
    {
        // Use smart registration instead of direct instantiation
        var services = new ServiceCollection();
        services.AddNpaMetadataProvider(); // Uses generated provider if available
        services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();
        services.AddSingleton(connection);
        services.AddSingleton(logger);
        services.AddScoped<IEntityManager, EntityManager>();
        
        var provider = services.BuildServiceProvider();
        _entityManager = provider.GetRequiredService<IEntityManager>();
    }

    public async Task RunAllOperations()
    {
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine("CRUD Operations Demo");
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine();

        // CREATE
        var customer = await CreateCustomerAsync();

        // READ
        await ReadCustomerAsync(customer.Id);

        // UPDATE
        await UpdateCustomerAsync(customer);

        // READ AGAIN
        await ReadCustomerAsync(customer.Id);

        // DELETE
        await DeleteCustomerAsync(customer.Id);

        // VERIFY DELETION
        await VerifyDeletionAsync(customer.Id);

        // BATCH OPERATIONS
        await BatchOperationsAsync();
    }

    private async Task<Customer> CreateCustomerAsync()
    {
        Console.WriteLine("1. CREATE Operation");
        Console.WriteLine("-".PadRight(70, '-'));

        var customer = new Customer
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "+1-555-0123",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        Console.WriteLine($"Creating customer: {customer.FirstName} {customer.LastName}");
        await _entityManager.PersistAsync(customer);
        Console.WriteLine($"✓ Customer created with ID: {customer.Id}");
        Console.WriteLine($"  State: {_entityManager.ChangeTracker.GetState(customer)}");
        Console.WriteLine();

        return customer;
    }

    private async Task ReadCustomerAsync(long customerId)
    {
        Console.WriteLine("2. READ Operation");
        Console.WriteLine("-".PadRight(70, '-'));

        var customer = await _entityManager.FindAsync<Customer>(customerId);
        
        if (customer != null)
        {
            Console.WriteLine($"✓ Customer found:");
            Console.WriteLine($"  {customer}");
            Console.WriteLine($"  State: {_entityManager.ChangeTracker.GetState(customer)}");
        }
        else
        {
            Console.WriteLine($"✗ Customer with ID {customerId} not found");
        }
        Console.WriteLine();
    }

    private async Task UpdateCustomerAsync(Customer customer)
    {
        Console.WriteLine("3. UPDATE Operation");
        Console.WriteLine("-".PadRight(70, '-'));

        Console.WriteLine($"Updating customer {customer.Id}...");
        customer.Phone = "+1-555-9999";
        customer.UpdatedAt = DateTime.UtcNow;

        await _entityManager.MergeAsync(customer);
        Console.WriteLine($"✓ Customer updated");
        Console.WriteLine($"  New phone: {customer.Phone}");
        Console.WriteLine($"  State: {_entityManager.ChangeTracker.GetState(customer)}");
        Console.WriteLine();
    }

    private async Task DeleteCustomerAsync(long customerId)
    {
        Console.WriteLine("4. DELETE Operation");
        Console.WriteLine("-".PadRight(70, '-'));

        Console.WriteLine($"Deleting customer {customerId}...");
        await _entityManager.RemoveAsync<Customer>(customerId);
        Console.WriteLine($"✓ Customer deleted");
        Console.WriteLine();
    }

    private async Task VerifyDeletionAsync(long customerId)
    {
        Console.WriteLine("5. VERIFY DELETION");
        Console.WriteLine("-".PadRight(70, '-'));

        var customer = await _entityManager.FindAsync<Customer>(customerId);
        
        if (customer == null)
        {
            Console.WriteLine($"✓ Customer {customerId} successfully deleted (not found)");
        }
        else
        {
            Console.WriteLine($"✗ Customer {customerId} still exists!");
        }
        Console.WriteLine();
    }

    private async Task BatchOperationsAsync()
    {
        Console.WriteLine("6. BATCH Operations");
        Console.WriteLine("-".PadRight(70, '-'));

        var customers = new List<Customer>
        {
            new() { FirstName = "Alice", LastName = "Smith", Email = "alice.smith@example.com", IsActive = true },
            new() { FirstName = "Bob", LastName = "Johnson", Email = "bob.johnson@example.com", IsActive = true },
            new() { FirstName = "Charlie", LastName = "Brown", Email = "charlie.brown@example.com", IsActive = true }
        };

        Console.WriteLine($"Creating {customers.Count} customers in batch...");
        foreach (var customer in customers)
        {
            await _entityManager.PersistAsync(customer);
        }

        Console.WriteLine($"✓ {customers.Count} customers created");
        foreach (var customer in customers)
        {
            Console.WriteLine($"  - {customer}");
        }
        Console.WriteLine();
    }
}
```

### Step 5: Implement Main Program

**Program.cs**
```csharp
using Microsoft.Extensions.Logging;
using CrudOperationsSample;

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Warning);
});

var logger = loggerFactory.CreateLogger<NPA.Core.Core.EntityManager>();

// Setup database
await using var dbManager = new DatabaseManager();
var connection = await dbManager.StartAsync();

// Run CRUD operations
var crudOps = new CrudOperations(connection, logger);
await crudOps.RunAllOperations();

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
```

## 📁 Project Structure

```
samples/CrudOperationsSample/
├── CrudOperationsSample.csproj
├── Program.cs
├── DatabaseManager.cs
├── CrudOperations.cs
├── README.md
└── Entities/
    └── Customer.cs
```

## 🧪 Test Cases

- [ ] Customer creation succeeds
- [ ] Customer can be found by ID
- [ ] Customer update modifies database
- [ ] Customer deletion removes from database
- [ ] Batch operations work correctly
- [ ] Change tracking shows correct states
- [ ] No memory leaks on cleanup

## 📚 Learning Outcomes

- EntityManager CRUD operations
- Change tracking and entity states
- Database integration patterns
- Container-based testing
- Async/await patterns
- Resource management

---

*Created: October 8, 2025*  
*Status: ⏳ Pending*
