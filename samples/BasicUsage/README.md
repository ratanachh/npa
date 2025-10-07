# BasicUsage Sample (Phases 1.1 – 1.4)

This sample demonstrates the implemented features of NPA through Phase 1.4 using a single cohesive run:

| Phase | Focus | Demonstrated In |
|-------|-------|-----------------|
| 1.1 | Attribute-based entity mapping (`[Entity]`, `[Table]`, `[Id]`, `[Column]`, `[GeneratedValue]`) | `User` entity class |
| 1.2 | EntityManager CRUD lifecycle | `Phase1Demo.RunAsync` (persist, find, merge, delete, detach/contains) |
| 1.3 | Simple query creation & parameter binding | `Phase1Demo` (active users & single user queries) |
| 1.4 | SQL Server provider integration | `SqlServerProviderRunner` (container-backed) |

## What Happens When You Run
1. A Testcontainers-based SQL Server instance is started.
2. A `users` table is created if it does not exist.
3. Dependency Injection is configured and the SQL Server provider registered.
4. `Phase1Demo` runs through:
   - Persist a `User`
   - Find it by ID
   - Update (merge) it
   - Detach and refetch
   - Run two parameterized queries
   - Remove the user and verify deletion
5. Container shuts down.

## Files Overview
- `Program.cs` – Entry point, selects provider (currently only SQL Server path active).
- `Features/SqlServerProviderRunner.cs` – Orchestrates container, DI, schema creation, and demo execution.
- `Features/Phase1Demo.cs` – Consolidated lifecycle + query walkthrough.
- `User.cs` – Sample entity with attribute mappings.
  

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
