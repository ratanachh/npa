# NPA Framework Samples

This directory contains sample projects demonstrating the features of the NPA (Nasal Persistence Architecture) framework.

## Consolidated Sample Runner

To simplify the experience, all individual console-based samples have been consolidated into a single, menu-driven application located in the `BasicUsage/` directory (soon to be renamed `NPA.Samples`).

This application acts as a host for all feature demonstrations, from basic CRUD to advanced queries and the repository pattern. Each sample is self-contained and uses Testcontainers to spin up a necessary database in Docker, so there are no external dependencies to configure.

### How to Run the Samples

1.  **Navigate to the project directory:**
    ```sh
    cd samples/BasicUsage
    ```

2.  **Run the application:**
    ```sh
    dotnet run
    ```

3.  **Choose a sample:**
    You will be presented with an interactive menu. Simply enter the number of the sample you wish to run, or select 'A' to run all samples sequentially.

    ```
    === NPA Framework Samples ===
    Please choose a sample to run:
      1. Advanced CPQL Queries
         Demonstrates advanced CPQL features like JOINs, GROUP BY, aggregates, and functions.
      2. Basic CRUD Operations
         Demonstrates basic entity mapping, EntityManager CRUD operations, and simple CPQL queries using the PostgreSQL provider.
      3. Multi-Tenancy Support
         Demonstrates the Discriminator Column strategy with automatic tenant filtering, isolation validation, and transaction management.
      4. Repository Pattern
         Demonstrates base repositories, custom repositories, and the repository factory.
      5. Source Generators
         Explains the benefits of the Repository and Metadata source generators.
      6. Synchronous API Usage
         Demonstrates the use of synchronous (blocking) API methods, ideal for console applications.

      A. Run All Samples
      Q. Quit

    Enter your choice:
    ```

## Individual Sample Projects

### Phase7Demo

**NEWLY CONSOLIDATED** - Comprehensive demonstration of Phase 7: Advanced Relationship Management features.

**Location**: `samples/Phase7Demo/`

**Features**:
- **Phase 7.1**: Relationship-Aware Repository Generation
  - Automatic GetByIdWith{Property}Async() methods
  - Load{Property}Async() for lazy loading
  - SQL JOIN generation with Dapper multi-mapping
- **Phase 7.2**: Eager Loading Support
  - FetchType.Eager automatic loading
  - GetByIdsAsync() batch loading (N+1 prevention)
- **Phase 7.3**: Cascade Operations Enhancement
  - AddWithCascadeAsync() - parent-first/child-after strategy
  - UpdateWithCascadeAsync() - with orphan removal
  - DeleteWithCascadeAsync() - children-first deletion
- **Phase 7.4**: Bidirectional Relationship Management (70% complete)
  - Static helper classes for synchronization
  - OneToMany/ManyToOne bidirectional sync
  - OneToOne bidirectional sync

**Run**:
```sh
cd samples/Phase7Demo
dotnet run
```

Check `samples/Phase7Demo/obj/generated/` folder after running to see all generated code!

### ProfilerDemo

A standalone sample demonstrating NPA's performance monitoring and profiling capabilities.

**Location**: `samples/ProfilerDemo/`

**Features**:
- Real-time performance monitoring
- Slow query detection
- Cache hit rate analysis
- Performance metrics (P50, P95, P99)
- Automated recommendations
- Integration with NPA.Profiler tool

**Run**:
```sh
cd samples/ProfilerDemo
dotnet run
```

See `samples/ProfilerDemo/README.md` for detailed usage and profiler tool commands.

---

## Project Structure

```
samples/
├── BasicUsage/              # Consolidated sample runner (all core features)
├── Phase7Demo/              # Phase 7: Advanced Relationship Management (ALL features)
├── ProfilerDemo/            # Performance profiling sample
├── UdemyCloneSaaS/          # Real-world SaaS application example
└── UdemyCloneSaaS.Api/      # API layer for SaaS example
```
