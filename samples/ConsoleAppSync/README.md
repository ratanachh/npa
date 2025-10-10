# Console App Sync - NPA Synchronous Methods Demo

This sample demonstrates how to use **synchronous methods** in NPA for console applications, CLI tools, scripts, and batch processing.

## 🎯 Purpose

Console applications don't benefit from async/await the same way web applications do. This sample shows:
- ✅ Using synchronous CRUD operations
- ✅ Synchronous query execution
- ✅ Batch operations without async
- ✅ Simple, straightforward code flow
- ✅ **Self-contained with Testcontainers** - No database installation required!

## 🐳 Testcontainers Integration

This sample uses **Testcontainers** to run SQL Server in a Docker container:
- ✅ **No database installation needed** - Runs SQL Server automatically
- ✅ **Fully isolated** - Clean environment every run
- ✅ **Self-contained** - Everything you need in one project
- ✅ **Production-like** - Real SQL Server, not mocks

**Requirements:**
- Docker Desktop installed and running
- .NET 8.0 SDK

## 🚀 Running the Sample

```bash
# Make sure Docker Desktop is running first!

# From the project root
cd samples/ConsoleAppSync
dotnet run
```

**What happens:**
1. 🐳 Starts SQL Server 2022 container in Docker
2. 🔌 Connects to the containerized database
3. 📋 Creates test table
4. ▶️ Runs synchronous CRUD, query, and batch operations
5. 🧹 Cleanup: Stops and removes container automatically

## 📝 Features Demonstrated

### 1. CRUD Operations (Sync)
```csharp
// CREATE - Executes immediately
entityManager.Persist(customer);

// READ
var customer = entityManager.Find<Customer>(id);

// UPDATE - Executes immediately
customer.Email = "new@example.com";
entityManager.Merge(customer);

// DELETE - Executes immediately
entityManager.Remove(customer);

// Note: Flush() is optional in current implementation
// Operations execute immediately (no batching until Phase 3.1)
```

### 2. Query Operations (Sync)
```csharp
// List query
var customers = entityManager
    .CreateQuery<Customer>("SELECT c FROM Customer c WHERE c.IsActive = :active")
    .SetParameter("active", true)
    .GetResultList();

// Single result
var customer = entityManager
    .CreateQuery<Customer>("SELECT c FROM Customer c WHERE c.Email = :email")
    .SetParameter("email", "john@example.com")
    .GetSingleResult();

// Scalar query
var count = entityManager
    .CreateQuery<Customer>("SELECT COUNT(c) FROM Customer c")
    .ExecuteScalar();

// Bulk update
var deleted = entityManager
    .CreateQuery<Customer>("DELETE Customer c WHERE c.IsActive = :active")
    .SetParameter("active", false)
    .ExecuteUpdate();
```

### 3. Batch Operations (Sync)
```csharp
// Create multiple entities (each executes immediately in current version)
for (int i = 0; i < 100; i++)
{
    entityManager.Persist(new Customer { ... });
}

// Bulk update/delete using queries
var updated = entityManager
    .CreateQuery<Customer>("UPDATE Customer c SET c.IsActive = :active WHERE ...")
    .SetParameter("active", false)
    .ExecuteUpdate();

// Note: True batching will be available in Phase 3.1 with transactions
```

## ⚖️ Sync vs Async - When to Use What

### Use Synchronous Methods ✅
- Console applications
- CLI tools and scripts
- Batch processing jobs
- Desktop applications (WPF, WinForms)
- Simple CRUD utilities
- Database migration scripts
- Test data generators

### Use Asynchronous Methods ✅
- ASP.NET Core web applications
- Web APIs
- High-concurrency services
- Azure Functions
- Real-time applications
- Any I/O-bound operations in web context

## 🔧 Requirements

- ✅ .NET 8.0 or later
- ✅ Docker Desktop installed and running
- ❌ **No SQL Server installation needed!** (Runs in container)

## 🐳 Why Testcontainers?

**Benefits:**
- **Easy Setup**: No database configuration required
- **Isolation**: Clean environment every run
- **CI/CD Friendly**: Perfect for automated builds
- **Realistic**: Uses real SQL Server, not mocks
- **Portable**: Works on Windows, Mac, Linux

**How It Works:**
```csharp
// Start SQL Server container (uses default image)
var container = new MsSqlBuilder()
    .WithPassword("YourStrong@Passw0rd")
    .WithCleanUp(true)  // Auto-removes container
    .Build();

await container.StartAsync();

// Get connection string to container
var connectionString = container.GetConnectionString();
var connection = new SqlConnection(connectionString);

// Use NPA with container database
var entityManager = new EntityManager(connection, ...);

// Cleanup automatically on dispose
await container.StopAsync();
```

## 🛠️ Troubleshooting

**Error: "Docker is not running"**
- Solution: Start Docker Desktop and wait for it to be ready

**Error: "Unable to pull image"**
- Solution: Check internet connection, Docker Desktop is properly configured

**Slow first run?**
- Docker needs to pull SQL Server image (~1.5 GB) on first run
- Subsequent runs are much faster (image is cached)

## 📁 Project Structure

```
ConsoleAppSync/
├── Program.cs                    # Main entry point
├── Features/
│   ├── SyncMethodsRunner.cs      # Container setup & orchestration
│   └── SyncMethodsDemo.cs        # Synchronous method demonstrations
├── Entities/
│   └── Customer.cs               # Sample entity with annotations
├── ConsoleAppSync.csproj         # Project file with dependencies
└── README.md                     # This file
```

**Design Pattern:**
- Follows the same structure as `BasicUsage/Features/SqlServerProviderRunner.cs`
- Uses Dependency Injection
- Proper container lifecycle management
- Clean separation of concerns

## 📚 Related Samples

- **BasicUsage**: Shows both async and sync patterns
- **SyncAsyncComparisonDemo**: Side-by-side comparison (also can use Testcontainers)
- **WebApplication**: Async methods for web apps

