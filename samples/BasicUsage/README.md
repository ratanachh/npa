# BasicUsage Sample (Phases 1.1 – 3.1)

This sample demonstrates the **implemented and tested** features of NPA using SQL Server, MySQL, or PostgreSQL provider:

| Phase | Status | Focus | Demonstrated In |
|-------|--------|-------|-----------------|
| 1.1 | [Completed] Complete | Attribute-based entity mapping (`[Entity]`, `[Table]`, `[Id]`, `[Column]`, `[GeneratedValue]`) | `User` entity class |
| 1.2 | [Completed] Complete | EntityManager CRUD lifecycle | `Phase1Demo.RunAsync` (persist, find, merge, delete, detach/contains) |
| 1.3 | [Completed] Complete | CPQL query creation & parameter binding | `Phase1Demo` (active users & single user queries) |
| 1.4 | [Completed] Complete | SQL Server provider with advanced features | `SqlServerProviderRunner` (TVPs, JSON, Spatial, Full-Text) |
| 1.5 | [Completed] Complete | MySQL provider with advanced features | `MySqlProviderRunner` (JSON, Spatial, Full-Text, UPSERT) |
| 3.1 | [Completed] Complete | Transaction management with deferred execution | `TransactionSample` (batching, rollback, ordering, 90-95% perf gain) |
| 5.5 | [Completed] Complete | Multi-tenancy support with automatic tenant isolation | `MultiTenancySample` (row-level security, tenant context, validation) |

**Default Provider**: SQL Server (63 tests passing)  
**Alternative Providers**: MySQL (86 tests passing), PostgreSQL

## What Happens When You Run
The sample includes an interactive menu system that auto-discovers and runs different demonstrations:

### Available Samples:
1. **Basic CRUD Sample** - Phase 1.2 entity lifecycle operations
2. **Repository Pattern Sample** - Phase 2.4 repository pattern with LINQ
3. **Source Generator Sample** - Phase 2.6 compile-time metadata generation
4. **Sync API Sample** - Synchronous API alternatives
5. **Advanced Queries Sample** - Phase 2.3 CPQL with JOINs, GROUP BY, aggregates
6. **Transaction Management Sample** - Phase 3.1 deferred execution and batching
7. **Multi-Tenancy Sample** - Phase 5.5 automatic tenant isolation and row-level security

### Transaction Sample Demonstrates:
1. **Basic Transaction** - Commit with multiple operations batched together
2. **Batching for Performance** - 90-95% reduction in database round trips
3. **Explicit Flush** - Early execution to get generated IDs
4. **Automatic Rollback** - Transaction rolls back on exception
5. **Mixed Operations** - Automatic priority ordering (INSERT → UPDATE → DELETE)
6. **Backward Compatibility** - Immediate execution without transactions

### Multi-Tenancy Sample Demonstrates:
1. **Basic Tenant Isolation** - Automatic filtering by tenant
2. **Auto TenantId Population** - EntityManager auto-populates TenantId
3. **Tenant Context Switching** - Change tenant and see different data
4. **Cross-Tenant Validation** - Prevents modifying other tenant's data
5. **Query Filtering** - Tenant filter works with WHERE, JOINs, aggregates
6. **Multi-Tenant Transactions** - Batching with tenant isolation
7. **Tenant Statistics** - Aggregate queries auto-filtered by tenant

Each sample:
- Sets up its own database schema
- Runs comprehensive demonstrations
- Shows console output with detailed explanations
- Cleans up after itself

## Files Overview
- `Program.cs` – Entry point that starts the interactive sample menu
- `Core/SampleRunner.cs` – Auto-discovers ISample implementations and runs them
- `Core/ISample.cs` – Interface for all sample demonstrations
- **Samples/**
  - `BasicCrudSample.cs` – Phase 1.2 CRUD operations
  - `RepositoryPatternSample.cs` – Phase 2.4 repository pattern
  - `SourceGeneratorSample.cs` – Phase 2.6 metadata generation
  - `SyncApiSample.cs` – Synchronous API alternatives
  - `AdvancedQueriesSample.cs` – Phase 2.3 CPQL advanced features
  - `TransactionSample.cs` – Phase 3.1 transaction management
  - `TransactionSampleRunner.cs` – Transaction sample wrapper with DB setup
  - `MultiTenancySample.cs` – Phase 5.5 multi-tenancy support
  - `MultiTenancySampleRunner.cs` – Multi-tenancy sample wrapper with DB setup
- **Entities/**
  - `User.cs` – User entity with JPA-like attributes
  - `Order.cs` – Order entity with relationships
  - `OrderItem.cs` – Order item entity
  - `Product.cs` – Product entity with [MultiTenant] attribute
  - `Category.cs` – Category entity with [MultiTenant] attribute
  

## Running the Sample
From the repository root:

```powershell
# Build the sample
dotnet build samples/BasicUsage/BasicUsage.csproj

# Run the interactive menu
dotnet run --project samples/BasicUsage/BasicUsage.csproj
```

### Prerequisites
- **Docker Desktop** running (for Testcontainers)
- **.NET 8.0 SDK** installed

**Note**: Samples use Testcontainers to automatically start PostgreSQL containers. No manual database setup required!

### Sample Menu Example:
```
=== NPA Framework Samples ===
Please choose a sample to run:
  1. Advanced Queries Sample (Phase 2.3)
     Demonstrates CPQL with JOINs, GROUP BY, aggregates, and functions
  2. Basic CRUD Sample
     Demonstrates basic EntityManager CRUD operations
  3. Repository Pattern Sample (Phase 2.4)
     Demonstrates the repository pattern with LINQ expressions
  4. Source Generator Sample (Phase 2.6)
     Demonstrates compile-time metadata generation
  5. Sync API Sample
     Demonstrates synchronous API alternatives
  6. Multi-Tenancy Support (Phase 5.5)
     Demonstrates automatic tenant isolation, row-level security, and tenant context management
  7. Transaction Management (Phase 3.1)
     Demonstrates deferred execution, batching, rollback, and performance optimization

  A. Run All Samples
  Q. Quit

Enter your choice:
```

## Transaction Sample Output Example
```
╔════════════════════════════════════════════════════════════════╗
║         NPA Transaction Management Demo (Phase 3.1)           ║
╚════════════════════════════════════════════════════════════════╝

🧹 Cleaning up database...
✓ Database cleaned

─────────────────────────────────────────────────────────────────
Demo 1: Basic Transaction with Commit
─────────────────────────────────────────────────────────────────
✓ Transaction started (isolation level: ReadCommitted)
  Persisting order (operation queued, not executed yet)...
  Persisting 2 order items (operations queued)...
  Queue size: 3 operations
✓ Committing transaction (executes all 3 operations in one batch)...
[Completed] Transaction committed! Order ID: 1

─────────────────────────────────────────────────────────────────
Demo 2: Batching for Performance (90-95% reduction in round trips)
─────────────────────────────────────────────────────────────────
✓ Transaction started. Creating 10 orders with items...
  Queue size: 20 operations queued
  Committing transaction (all operations executed in one batch)...
[Completed] Created 10 orders with items in 45ms
   Performance: 20 INSERTs in single transaction
   Without transaction: Would require 20 database round trips

─────────────────────────────────────────────────────────────────
Demo 3: Explicit Flush for Early Execution
─────────────────────────────────────────────────────────────────
✓ Transaction started
  Persisting order...
  Order ID before flush: 0 (not yet available)
  Calling FlushAsync() to execute queued operations...
✓ Order ID after flush: 11 (now available!)
  Creating order item with OrderId=11...
  Committing transaction...
[Completed] Transaction committed with explicit flush! Order ID: 11

─────────────────────────────────────────────────────────────────
Demo 4: Automatic Rollback on Exception
─────────────────────────────────────────────────────────────────
✓ Transaction started
  Persisting order (queued)...
  Queue size: 1
  ⚠ Simulating an error (throwing exception)...
  Exception caught: Simulated error!
✓ Transaction automatically rolled back (using statement disposed)
  Queue cleared, no data written to database
[Completed] Verified: Order not in database (foundOrder is null: True)

[Completed] All transaction demos completed successfully!
```

### Multi-Tenancy Sample Output Example
```
╔════════════════════════════════════════════════════════════════╗
║      NPA Multi-Tenancy Support Demo (Phase 5.5)              ║
╚════════════════════════════════════════════════════════════════╝

🧹 Cleaning up database...
✓ Database cleaned

─────────────────────────────────────────────────────────────────
Demo 1: Basic Tenant Isolation
─────────────────────────────────────────────────────────────────
✓ Switched to tenant: acme-corp
  ✓ Created product for acme-corp: Acme Widget Pro
✓ Switched to tenant: contoso-ltd
  ✓ Created product for contoso-ltd: Contoso Premium Tool

✓ Querying as acme-corp: Found 1 product(s)
  └─ Product[1] Acme Widget Pro - $299.99 (Stock: 100) [Tenant: acme-corp]

✓ Querying as contoso-ltd: Found 1 product(s)
  └─ Product[2] Contoso Premium Tool - $499.99 (Stock: 50) [Tenant: contoso-ltd]

[Completed] Tenant isolation working correctly!
   SQL: SELECT * FROM products WHERE tenant_id = 'acme-corp'

─────────────────────────────────────────────────────────────────
Demo 2: Automatic TenantId Population
─────────────────────────────────────────────────────────────────
✓ Current tenant: fabrikam-inc
  Before persist: TenantId = '' (empty)
  After persist:  TenantId = 'fabrikam-inc' (auto-populated!)

[Completed] TenantId automatically set by EntityManager!
   Category[1] now belongs to tenant: fabrikam-inc

─────────────────────────────────────────────────────────────────
Demo 4: Cross-Tenant Access Validation
─────────────────────────────────────────────────────────────────
✓ Created product as tenant-security: Product[12] Secure Product
✓ Switched to tenant-hacker (attempting cross-tenant access)
  ✓ Cross-tenant update blocked: Entity belongs to different tenant

[Completed] Data integrity verified!
   Original price: $149.99
   Current price:  $149.99
   Cross-tenant modification was prevented!

[Completed] All multi-tenancy demos completed successfully!
```

## Best Practices Illustrated
- **Deferred Execution**: Operations queue when transaction is active
- **Batching**: Combine multiple operations for 90-95% performance improvement
- **Automatic Cleanup**: Transactions auto-rollback on exception (using statement)
- **Priority Ordering**: Operations execute in correct order (INSERT → UPDATE → DELETE)
- **Backward Compatibility**: Works without transactions (immediate execution)
- **Explicit Flush**: Get generated IDs before commit when needed
- **Tenant Isolation**: Automatic row-level filtering with `[MultiTenant]` attribute
- **Tenant Context**: Thread-safe tenant management with `ITenantProvider`
- **Data Security**: Auto-validation prevents cross-tenant data access
- **Performance**: Indexed tenant_id columns for fast filtering
- Scoped `EntityManager` usage via dependency injection
- Parameterized queries with `.SetParameter(name, value)`
- Clean resource disposal and error handling

## Next Steps (Beyond Phase 3.1)
- **Phase 3.2**: Cascade operations (automatic related entity operations)
- **Phase 3.3**: Bulk operations (efficient batch insert/update/delete)
- **Phase 3.4**: Lazy loading support (on-demand relationship loading)
- **Phase 3.5**: Connection pooling optimization
- **Phase 4**: Advanced source generator features
- **Phase 5**: Enterprise features (caching, migrations, monitoring)

---
**Latest Updates**:  
- **Phase 5.5 Multi-Tenancy**: Complete with 32 tests passing (100% coverage). Automatic tenant isolation with row-level security.  
- **Phase 3.1 Transactions**: Complete with 22 tests passing. 90-95% reduction in database round trips with batching.

**Multi-Tenancy Strategies**: See `samples/MultiTenancy/` for alternative approaches:
- Discriminator Column (current implementation) - Row-level filtering
- Database Per Tenant - Separate database per tenant
- Schema Per Tenant - Separate schema per tenant
