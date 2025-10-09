# BasicUsage Sample (Phases 1.1 – 1.4)

This sample demonstrates the **implemented and tested** features of NPA (Phases 1.1-1.4) using SQL Server or PostgreSQL provider:

| Phase | Status | Focus | Demonstrated In |
|-------|--------|-------|-----------------|
| 1.1 | ✅ Complete | Attribute-based entity mapping (`[Entity]`, `[Table]`, `[Id]`, `[Column]`, `[GeneratedValue]`) | `User` entity class |
| 1.2 | ✅ Complete | EntityManager CRUD lifecycle | `Phase1Demo.RunAsync` (persist, find, merge, delete, detach/contains) |
| 1.3 | ✅ Complete | CPQL query creation & parameter binding | `Phase1Demo` (active users & single user queries) |
| 1.4 | ✅ Complete | SQL Server provider with advanced features | `SqlServerProviderRunner` (TVPs, JSON, Spatial, Full-Text) |

**Default Provider**: SQL Server (63 tests passing, fully tested)

## What Happens When You Run
1. A Testcontainers-based **SQL Server** instance is started (default).
2. A `users` table is created if it does not exist.
3. Dependency Injection is configured and the SQL Server provider registered.
4. `Phase1Demo` runs through:
   - **Persist** a `User` (Phase 1.2)
   - **Find** it by ID (Phase 1.2)
   - **Update** (merge) it (Phase 1.2)
   - **Detach** and refetch (Phase 1.2)
   - Run two **parameterized CPQL queries** (Phase 1.3)
   - **Remove** the user and verify deletion (Phase 1.2)
5. Container shuts down automatically.

## Files Overview
- `Program.cs` – Entry point, selects provider (default: SQL Server ✅, optional: PostgreSQL ✅).
- `Features/SqlServerProviderRunner.cs` – Orchestrates SQL Server container, DI, schema creation (✅ Phase 1.4 complete).
- `Features/PostgreSqlProviderRunner.cs` – Orchestrates PostgreSQL container, DI, schema creation (✅ alternative provider).
- `Features/Phase1Demo.cs` – Consolidated Phase 1.1-1.4 lifecycle + CPQL query walkthrough.
- `User.cs` – Sample entity with JPA-like attribute mappings (Phase 1.1).
  

## Running the Sample
From the repository root:

```powershell
# Build just the sample (ensures core + provider too)
dotnet build samples/BasicUsage/BasicUsage.csproj

# Run (starts a container, requires Docker running)
dotnet run --project samples/BasicUsage/BasicUsage.csproj
```

Expected console output includes lines similar to:
```
Starting SQL Server container...
Database schema created successfully
--- Phase1 Demo: Lifecycle & Query ---
Persisted user id=1
Found user username=sqlserver_phase1_user
Merged user newEmail=updated.phase1@sqlserver.example.com
EntityManager tracking user; detaching...
Refetched after detach => ok
Query active count=1
Query single=sqlserver_phase1_user
User removed successfully
--- End Phase1 Demo ---
NPA Demo Completed Successfully!
```
(IDs may differ depending on identity seed.)

## Best Practices Illustrated
- Explicit schema creation separate from lifecycle logic.
- Scoped `EntityManager` usage via `IServiceScope` for isolation.
- Parameterized queries using `.SetParameter(name, value)`.
- Defensive try/catch around query block to future-proof parser evolution.
- Clean resource disposal (container stop in `finally`).

## Next Steps (Beyond Phase 1.4)
- Add composite key entity (Phase 2.2).
- Extend query language coverage (joins, ordering, updates, deletes).
- Introduce provider-neutral testing and migration scaffolding.
- Implement bulk operations & transaction samples.

---
Generated automatically as part of aligning the sample with implemented Phases 1.1–1.4. Legacy sample files were removed in favor of the unified `Phase1Demo`.
