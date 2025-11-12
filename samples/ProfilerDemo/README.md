# NPA Profiler Demo - Enterprise Edition

This sample demonstrates **real-world** NPA performance monitoring and profiling with:

- ✅ **1 Million Records** - Realistic dataset size for performance testing
- ✅ **Faker (Bogus)** - Realistic test data generation
- ✅ **Dependency Injection** - Enterprise-ready architecture
- ✅ **Repository Pattern** - Clean separation of concerns
- ✅ **Testcontainers** - Fully self-contained with PostgreSQL in Docker
- ✅ **Performance Monitoring** - Real-time tracking and analysis

## What This Sample Demonstrates

### 1. Data Generation
- **Faker integration** for realistic user data (names, emails, addresses, etc.)
- **Batch inserts** for efficient bulk data loading
- **1 million records** generated in ~30-60 seconds

### 2. Performance Testing Scenarios

#### Indexed vs Non-Indexed Queries
Compare performance between indexed and full table scan queries

#### N+1 Problem Detection
- Demonstrate the N+1 query anti-pattern (100 individual queries)
- Show the optimized solution (1 batch query)
- Measure the performance difference (typically 50-100x improvement)

#### Pagination Performance
- Compare early vs deep pagination
- Demonstrate offset performance degradation
- Show why keyset pagination is better for large datasets

#### Aggregate Queries
- GROUP BY operations on million-record datasets
- Index utilization in aggregate queries

#### Bulk Operations
- Bulk UPDATE performance
- Bulk DELETE with conditions

### 3. Architecture Patterns

#### Dependency Injection
```csharp
services.AddSingleton<PerformanceMonitor>();
services.AddScoped<IUserRepository, UserRepository>();
```

#### Repository Pattern
```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetByIdsAsync(int[] ids);
    Task<IEnumerable<User>> FindActiveUsersAsync();
    // ... more methods
}
```

## Prerequisites

- **.NET 8.0 SDK** or higher
- **Docker Desktop** running (required for Testcontainers)
- **At least 2GB free RAM** (for 1M records + PostgreSQL)

## Running the Sample

```bash
dotnet run --project samples/ProfilerDemo
```

### Expected Runtime

- **Data Generation**: ~30-60 seconds (1 million records)
- **Performance Tests**: ~10-20 seconds
- **Total Runtime**: ~1-2 minutes

## Sample Output

```
=== NPA Profiler Demo - Enterprise Edition ===
Demonstrating performance profiling with 1 million records

info: Starting PostgreSQL container...
info: PostgreSQL container started successfully
info: Creating database schema...
info: Database schema created successfully

=== Phase 1: Data Generation ===
info: Generating 1,000,000 realistic user records using Faker...
  Progress: 100,000 / 1,000,000 records (10.0%)
  Progress: 200,000 / 1,000,000 records (20.0%)
  ...
  Progress: 1,000,000 / 1,000,000 records (100.0%)
✓ Generated 1,000,000 records in 45.23s (22,105 records/sec)

=== Phase 2: Performance Testing ===

1. Testing Indexed Queries
   Email lookup (indexed): 0.85ms
   Username lookup (indexed): 1.23ms

2. Testing N+1 Problem (BAD)
   100 individual queries: 125.45ms (1.25ms per query)

3. Testing Optimized Batch Query (GOOD)
   Single batch query for 100 users: 2.34ms
   ✓ 54x faster than N+1!

4. Testing Full Table Scan (SLOW)
   ⚠️  Full table scan: 3,245.67ms (850,000 records)

5. Testing Aggregate Queries
   Aggregation by country: 1,856.23ms (195 countries)

6. Testing Pagination Performance
   Page 1 (first 50): 1.12ms
   Page 1000 (offset 50k): 45.67ms

7. Testing Bulk Operations
   Bulk update (45,234 records): 234.56ms
   Bulk delete (12,345 records): 156.78ms

=== Phase 3: Performance Report ===

======================================================================
PERFORMANCE ANALYSIS REPORT
======================================================================

SELECT_INDEXED:
  Operations: 2
  Avg: 1.04ms
  Min: 0.85ms
  Max: 1.23ms

SELECT_N1:
  Operations: 100
  Avg: 1.25ms
  Min: 0.89ms
  Max: 2.34ms

SELECT_BATCH:
  Operations: 1
  Avg: 2.34ms
  Min: 2.34ms
  Max: 2.34ms

SELECT_FULL_SCAN:
  Operations: 1
  Avg: 3245.67ms
  Min: 3245.67ms
  Max: 3245.67ms

...

======================================================================
KEY INSIGHTS
======================================================================

✓ Index Performance:
  Indexed queries: 1.04ms
  Full scan: 3245.67ms
  → Indexes are 3121x faster!

✓ N+1 vs Batch Queries:
  N+1 approach (100 queries): 125.45ms
  Batch approach (1 query): 2.34ms
  → Batch queries are 54x faster!

✓ Pagination:
  Early pages: 1.12ms
  Deep pagination (50k offset): 45.67ms
  → Use keyset pagination for better performance!

======================================================================
```

## Key Performance Insights

### 1. **Index Impact**
With 1 million records, indexes provide **~3000x** performance improvement:
- Indexed query: ~1ms
- Full table scan: ~3000ms

### 2. **N+1 Problem**
The N+1 anti-pattern is **50-100x slower** than batch queries:
- 100 individual queries: ~125ms
- 1 batch query: ~2ms

### 3. **Pagination Degradation**
OFFSET pagination degrades with depth:
- Page 1: ~1ms
- Page 1000 (50k offset): ~45ms
- **Solution**: Use keyset/cursor-based pagination

### 4. **Bulk Operations**
Bulk operations scale well:
- Update 45k records: ~235ms (~192 records/ms)
- Delete 12k records: ~157ms (~76 records/ms)

## Code Examples

### Using Faker for Realistic Data

```csharp
var faker = new Faker<User>()
    .RuleFor(u => u.Username, f => f.Internet.UserName())
    .RuleFor(u => u.Email, f => f.Internet.Email())
    .RuleFor(u => u.FirstName, f => f.Name.FirstName())
    .RuleFor(u => u.LastName, f => f.Name.LastName())
    .RuleFor(u => u.Age, f => f.Random.Int(18, 80))
    .RuleFor(u => u.Country, f => f.Address.Country())
    .RuleFor(u => u.City, f => f.Address.City());

var users = faker.Generate(10000);
```

### Dependency Injection Setup

```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<PerformanceMonitor>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ProfilerDemoService>();
    })
    .Build();
```

### Repository Pattern with Performance Monitoring

```csharp
public class UserRepository : IUserRepository
{
    private readonly PerformanceMonitor _monitor;
    
    public async Task<User?> GetByIdAsync(int id)
    {
        var sw = Stopwatch.StartNew();
        // Execute query...
        sw.Stop();
        _monitor.RecordMetric("SELECT_BY_ID", sw.Elapsed, 1);
        return user;
    }
}
```

## Performance Optimization Tips

Based on this demo's findings:

1. **Always use indexes** on frequently queried columns
2. **Avoid N+1 queries** - use batch queries with IN or ANY
3. **Use keyset pagination** instead of OFFSET for deep pages
4. **Batch operations** when possible (INSERT, UPDATE, DELETE)
5. **Monitor query performance** in production
6. **Set query timeout limits** to catch slow queries early

## Using NPA.Profiler Tool

For advanced analysis, export metrics and use the profiler tool:

```bash
# Generate profiling data
dotnet run --project samples/ProfilerDemo > profile-data.json

# Analyze with NPA.Profiler
dotnet run --project tools/NPA.Profiler -- analyze --data profile-data.json

# Generate HTML report
dotnet run --project tools/NPA.Profiler -- report \
  --data profile-data.json \
  --format html \
  --output performance-report.html
```

## Architecture Details

### Entity Definition

```csharp
[Entity]
[Table("users")]
public partial class User
{
    [Id]
    [GeneratedValue(Strategy = GenerationStrategy.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    // ... 9 more properties
}
```

### Repository Interface

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetByIdsAsync(int[] ids);
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByUsernameAsync(string username);
    Task<IEnumerable<User>> FindActiveUsersAsync();
    Task<IEnumerable<UserStatistics>> GetUserStatisticsByCountryAsync();
    Task<IEnumerable<User>> GetUsersPageAsync(int page, int pageSize);
    Task<int> BulkUpdateAccountBalanceAsync(string country, decimal amount);
    Task<int> DeleteInactiveUsersOlderThanAsync(DateTime date);
}
```

### Dependency Injection Container

```csharp
services.AddSingleton<PerformanceMonitor>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PerformanceMonitor>>();
    return new PerformanceMonitor(logger);
});

services.AddScoped<IUserRepository>(sp =>
{
    var connectionString = /* from config */;
    var monitor = sp.GetRequiredService<PerformanceMonitor>();
    var logger = sp.GetRequiredService<ILogger<UserRepository>>();
    return new UserRepository(connectionString, monitor, logger);
});
```

## Database Schema

```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    age INT NOT NULL,
    country VARCHAR(100) NOT NULL,
    city VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NOT NULL,
    last_login TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT true,
    account_balance DECIMAL(18, 2) NOT NULL DEFAULT 0
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_country_city ON users(country, city);
CREATE INDEX idx_users_age ON users(age);
CREATE INDEX idx_users_created_at ON users(created_at);
```

## Customization

### Adjust Record Count

Change the constant in `ProfilerDemoService`:

```csharp
private const int TOTAL_RECORDS = 1_000_000; // Change this
```

### Modify Test Scenarios

Add your own performance tests in `ProfilerDemoService.RunPerformanceTests()`:

```csharp
private async Task RunPerformanceTests()
{
    // Your custom test
    var sw = Stopwatch.StartNew();
    // ... your query ...
    sw.Stop();
    _monitor.RecordMetric("MY_CUSTOM_TEST", sw.Elapsed, recordCount);
}
```

### Change Database Provider

Replace PostgreSQL with MySQL or SQL Server:

```csharp
// For MySQL
var mysqlContainer = new MySqlBuilder()
    .WithImage("mysql:8.0")
    .Build();

// For SQL Server
var sqlServerContainer = new MsSqlBuilder()
    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
    .Build();
```

## Troubleshooting

### Docker Not Running
```
Error: Cannot connect to Docker daemon
Solution: Start Docker Desktop
```

### Out of Memory
```
Error: OutOfMemoryException during data generation
Solution: Reduce TOTAL_RECORDS or increase Docker memory limit
```

### Slow Performance
```
Issue: Data generation takes > 2 minutes
Causes:
  - Docker resource limits
  - Disk I/O constraints
  - Low system RAM
Solution: 
  - Increase Docker CPU/RAM allocation
  - Use SSD for Docker storage
  - Reduce batch size or total records
```

## Learning Resources

- **NPA Documentation**: See `docs/` folder
- **Performance Monitoring**: `docs/performance-monitoring.md`
- **Profiler Tool**: `tools/NPA.Profiler/README.md`
- **Repository Pattern**: `docs/repository-pattern.md`
- **Testcontainers**: https://dotnet.testcontainers.org/

## Next Steps

1. **Run the demo** to see enterprise-grade profiling in action
2. **Analyze the results** - understand index impact, N+1 problems
3. **Integrate patterns** into your own projects
4. **Customize tests** for your specific use cases
5. **Monitor production** with NPA.Profiler tool

---

**Ready to run?**

```bash
# Ensure Docker is running, then:
dotnet run --project samples/ProfilerDemo

# Watch as 1 million records are generated and analyzed!
```
