# Changelog

All notable changes to the NPA (JPA-like ORM for .NET) project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Current Status
- **Overall Progress**: 86% complete (30/35 tasks)
- **Total Tests**: 1,220 passing
- **Phase 1-2**: ✅ 100% Complete
- **Phase 3**: ✅ 100% Complete (5/5 tasks)
- **Phase 4**: 86% Complete (6/7 tasks)
- **Phase 5**: ✅ 100% Complete
- **Phase 6**: 0% Complete (planning stage)

### Added - Connection Pooling Optimization (Phase 3.5)
- `ConnectionPoolOptions` class with unified pooling configuration API
- Leverages ADO.NET built-in connection pooling across all database providers
- **SQL Server**: Connection string generation with `SqlConnectionStringBuilder`
- **PostgreSQL**: Connection string generation with `NpgsqlConnectionStringBuilder`
- **MySQL**: Connection string generation with `MySqlConnectionStringBuilder`
- **SQLite**: Shared cache mode configuration (Enabled → Shared cache)
- Configurable pool size (MinPoolSize: 5, MaxPoolSize: 100 defaults)
- Connection timeouts and lifetime management
- Connection validation and reset on return
- External pooling support (PgBouncer for PostgreSQL, ProxySQL for MySQL)
- Performance: Eliminates connection establishment overhead
- Zero breaking changes: Backward compatible with existing connection strings
- **127 comprehensive tests** covering all scenarios (29 Core + 19 SQL Server + 28 PostgreSQL + 31 MySQL + 20 SQLite)

---

## [0.8.0] - 2025-11-11

### Added - SQL Formatting (Optional Feature)
- Optional `formatSql` parameter in `CpqlToSqlConverter.ConvertToSql()` methods
- `FormatSql()` method for readable SQL output with line breaks
- Indentation for AND/OR conditions in WHERE clauses
- Whitespace normalization before formatting
- Backward compatible (default: `formatSql = false`)
- Test coverage in CPQL tests (64 tests passing)

### Performance
- Zero performance impact when disabled (default)
- Useful for debugging and logging SQL queries

---

## [0.7.0] - 2025-11-09

### Added - Phase 5: Enterprise Features (100% Complete)

#### Multi-Tenancy Support (Phase 5.5)
- `ITenantProvider` and `AsyncLocalTenantProvider` for tenant context management
- `ITenantStore` and `InMemoryTenantStore` for tenant registration
- `TenantManager` for high-level tenant operations
- Three isolation strategies: Discriminator, Schema, Database
- `[MultiTenant]` attribute for marking multi-tenant entities
- DI extensions with `AddNpaMultiTenancy()`
- Execute queries in tenant context with automatic filtering
- **25 tests** passing (21 Extensions + 4 Core attribute tests)

#### Audit Logging (Phase 5.4)
- `IAuditStore` interface and `InMemoryAuditStore` implementation
- Comprehensive tracking: who, when, what, old/new values
- Flexible filtering: date range, user, entity type, action, category, severity
- Severity levels: Low, Normal, High, Critical
- Parameter and IP address tracking
- `[Audit]` attribute for automatic audit trail generation
- **25 tests** passing (20 store tests + 5 attribute tests)

#### Performance Monitoring (Phase 5.3)
- `IMetricCollector` interface and `InMemoryMetricCollector` implementation
- Statistical analysis: Min, max, average, p95 duration tracking
- Warning thresholds for slow queries
- Category-based filtering and organization
- Parameter tracking for detailed analysis
- `[PerformanceMonitor]` attribute for auto-instrumentation
- Thread-safe implementation
- **12 tests** passing

#### Database Migrations (Phase 5.2)
- `IMigration` interface for defining migrations
- `MigrationRunner` for executing migrations with transaction support
- Rollback capability for safe schema changes
- Version tracking in `__MigrationHistory` table
- Database-agnostic implementation (SQL Server, SQLite, extensible)
- Support for both Up and Down migrations
- **20 tests** passing

#### Caching Support (Phase 5.1)
- `ICacheProvider` interface for extensible caching
- `MemoryCacheProvider` with TTL support
- `NullCacheProvider` for testing/disabling cache
- `CacheKeyGenerator` for consistent key generation
- DI extensions with `AddNpaCaching()`
- Async and sync operations
- **31 tests** passing

### Performance Achievements
- **Metadata Provider**: 250-500x faster than reflection (Phase 2.7)
- **Bulk Operations**: 10-100x faster than individual operations
- **Transaction Batching**: 90-95% reduction in database round trips

---

## [0.6.0] - 2025-11-08

### Added - Phase 4: Source Generator Enhancements (86% Complete)

#### Custom Generator Attributes (Phase 4.6)
- `[GeneratedMethod]` - Control code generation behavior
- `[IgnoreInGeneration]` - Exclude members from generation
- `[CustomImplementation]` - Signal custom implementation
- `[CacheResult]` - Auto-generate caching logic
- `[ValidateParameters]` - Auto-generate parameter validation
- `[RetryOnFailure]` - Auto-generate retry logic
- `[TransactionScope]` - Control transaction behavior
- **20 tests** passing

#### Incremental Generator Optimizations (Phase 4.5)
- Incremental processing for faster builds
- Caching mechanisms to avoid redundant generation
- **10 tests** passing

#### Many-to-Many Query Generation (Phase 4.4)
- Automatic detection of M2M relationships
- Join query generation for navigation
- Relationship management methods
- **Tests included** in overall generator suite

#### Composite Key Repository Generation (Phase 4.3)
- Automatic composite key detection
- Generated methods: GetByIdAsync, DeleteAsync, ExistsAsync
- Support for individual parameter queries
- **7 tests** passing

#### Query Method Generation (Phase 4.2)
- Convention-based method name analysis
- OrderBy clause parsing and generation
- Enhanced method signature parsing
- **14 tests** passing

#### Advanced Repository Patterns (Phase 4.1)
- Complex method pattern support
- Custom query generation
- Relationship query support
- **27 tests** passing (15 attribute + 12 analyzer)

---

## [0.5.0] - 2025-11-07

### Added - Phase 3: Transaction & Performance Features (80% Complete)

#### Lazy Loading (Phase 3.4)
- `ILazyLoader` interface with Load/LoadCollection methods
- `ILazyLoadingProxy` for proxy tracking
- `ILazyLoadingContext` for loading context
- `ILazyLoadingCache` for thread-safe caching with `ConcurrentDictionary`
- Support for ManyToOne, OneToMany, ManyToMany relationships
- Automatic SQL generation based on relationship metadata
- Cache-first strategy to avoid redundant queries
- Cancellation token support
- **37 tests** passing

#### Bulk Operations (Phase 3.3)
- `BulkInsertAsync/BulkInsert` methods (10-100x faster)
- `BulkUpdateAsync/BulkUpdate` methods
- `BulkDeleteAsync/BulkDelete` methods
- Provider-specific optimizations:
  - PostgreSQL: COPY command with binary format
  - SQL Server: SqlBulkCopy
  - MySQL: Multi-row INSERT
  - SQLite: Batch INSERT in transaction
- Empty collection handling
- Large dataset support (100,000+ records)
- **13 tests** passing

#### Cascade Operations (Phase 3.2)
- `ICascadeService` interface and implementation
- Cascade types: Persist, Merge, Remove, Detach, All
- Orphan removal support for automatic cleanup
- Cycle detection to prevent infinite recursion
- Depth-first removal for FK constraint safety
- Collection and single entity support
- **10 tests** passing

#### Transaction Management (Phase 3.1)
- `ITransaction` interface with full lifecycle management
- Deferred execution with operation batching
- Automatic rollback on dispose if not committed
- Operation priority ordering (INSERT → UPDATE → DELETE)
- Both async and sync patterns
- `TransactionException` for transaction-specific errors
- **22 tests** passing
- **Performance**: 90-95% reduction in database round trips

### Fixed
- **EntityMetadataGenerator**: Fixed PropertyInfo null reference bug
  - Added `PropertyInfo = typeof({entity}).GetProperty("{prop}")` to generated metadata
  - All entity operations now work correctly with generated metadata provider

---

## [0.4.0] - 2025-11-06

### Added - Phase 2: Advanced Features (100% Complete)

#### Metadata Provider Integration (Phase 2.7)
- `GeneratedMetadataProvider` static class generation
- `ServiceCollectionExtensions` with `AddNpaMetadataProvider()`
- Smart three-tier assembly scanning for metadata discovery
- Updated all provider extensions (PostgreSQL, SQL Server, MySQL, SQLite)
- Updated all 7 sample applications
- **31 tests** passing (10 ServiceCollectionExtensions + 21 generator)
- **Performance**: 250-500x faster than reflection! 🚀

#### Metadata Source Generator (Phase 2.6)
- `EntityMetadataGenerator` with IIncrementalGenerator
- Automatic entity discovery from `[Entity]` attributes
- Compile-time metadata generation (zero runtime reflection)
- Entity and property metadata with relationship detection
- **9 tests** passing

#### Additional Database Providers (Phase 2.5)
- **PostgreSQL Provider**:
  - COPY command for bulk operations
  - RETURNING clause support
  - JSONB and array types
  - UUID support
  - UPSERT (ON CONFLICT)
  - Full-text search with GIN indexes
  - **132 tests** passing
- **SQLite Provider**:
  - Type affinity system (INTEGER, REAL, TEXT, BLOB)
  - last_insert_rowid() for ID generation
  - In-memory database support
  - Pragma configuration (foreign keys, WAL)
  - FTS5 full-text search
  - **58 tests** passing
- **Total Provider Tests**: 316 (SQL Server: 63, MySQL: 63, PostgreSQL: 132, SQLite: 58)

#### Repository Pattern (Phase 2.4)
- `IRepository<T, TKey>` and `IReadOnlyRepository<T, TKey>` interfaces
- `BaseRepository<T, TKey>` and `CustomRepositoryBase<T, TKey>`
- LINQ expression support (predicates, ordering, paging)
- `IRepositoryFactory` and `RepositoryFactory` for DI
- `ExpressionTranslator` for LINQ to SQL conversion
- **14 tests** passing

#### CPQL Query Language (Phase 2.3)
- Complete CPQL parser with Lexer and AST
- JOIN support (INNER, LEFT, RIGHT, FULL with ON conditions)
- GROUP BY and HAVING clauses
- Aggregate functions: COUNT, SUM, AVG, MIN, MAX with DISTINCT
- String functions: UPPER, LOWER, LENGTH, SUBSTRING, TRIM, CONCAT
- Date functions: YEAR, MONTH, DAY, HOUR, MINUTE, SECOND, NOW
- Complex expressions with operator precedence
- DISTINCT keyword and multiple ORDER BY columns
- Named parameters (`:paramName`)
- Database dialect support for all 4 databases:
  - SQL Server: No quotes (63 tests)
  - MySQL/MariaDB: Backticks (63 tests)
  - PostgreSQL: Double quotes (132 tests)
  - SQLite: Double quotes (58 tests)
- Culture-independent number parsing
- Comment support (line `--` and block `/* */`)
- **30 tests** passing

#### Composite Key Support (Phase 2.2)
- `CompositeKey` class with equality and hashing
- `CompositeKeyMetadata` for metadata management
- `CompositeKeyBuilder` fluent API
- EntityManager Find/Remove operations with composite keys
- Async and sync support
- **25 tests** passing

#### Relationship Mapping (Phase 2.1)
- `@OneToMany`, `@ManyToOne`, `@ManyToMany` attributes
- `@JoinColumn` and `@JoinTable` configuration
- Bidirectional relationships with `mappedBy`
- Cascade types: Persist, Merge, Remove, Refresh, Detach, All
- Fetch strategies: Eager, Lazy
- Orphan removal support
- **27 tests** passing

---

## [0.3.0] - 2025-11-05

### Added - Phase 1: Core Foundation (100% Complete)

#### Repository Source Generator (Phase 1.6)
- `RepositoryGenerator` with IIncrementalGenerator
- Syntax receiver for repository interface detection
- Basic code generation for CRUD methods
- Convention-based method generation
- **Tests**: Included in generator test suite

#### MySQL/MariaDB Provider (Phase 1.5)
- `MySqlProvider`, `MySqlDialect`, `MySqlTypeConverter`
- `MySqlBulkOperationProvider` with MySqlBulkLoader
- AUTO_INCREMENT support
- JSON column support
- Spatial data types
- Full-text search
- Generated columns
- **63 tests** passing

#### SQL Server Provider (Phase 1.4)
- `SqlServerProvider`, `SqlServerDialect`, `SqlServerTypeConverter`
- `SqlServerBulkOperationProvider` with SqlBulkCopy
- SCOPE_IDENTITY() for ID generation
- Table-valued parameters
- **63 tests** passing

#### Simple Query Support (Phase 1.3)
- `IQuery`, `IQueryParser`, `ISqlGenerator`, `IParameterBinder` interfaces
- `Query`, `QueryParser`, `SqlGenerator`, `ParameterBinder` implementations
- CPQL parsing with regex patterns (SELECT, UPDATE, DELETE)
- SQL injection prevention
- Parameter binding
- Async operations: GetResultListAsync, GetSingleResultAsync, ExecuteUpdateAsync, ExecuteScalarAsync
- Sync operations: GetResultList, GetSingleResult, ExecuteUpdate, ExecuteScalar
- Full test coverage

#### EntityManager with CRUD (Phase 1.2)
- `IEntityManager` and `IChangeTracker` interfaces
- `EntityManager` implementation with change tracking
- CRUD operations: PersistAsync, FindAsync, MergeAsync, RemoveAsync, FlushAsync
- Sync operations: Persist, Find, Merge, Remove, Flush
- Entity state tracking: Detached, Unchanged, Added, Modified, Deleted
- Composite key support
- `IMetadataProvider` with automatic metadata generation
- 100% test coverage

#### Basic Entity Mapping (Phase 1.1)
- `[Entity]`, `[Table]`, `[Id]`, `[Column]` attributes
- `[GeneratedValue]` with GenerationType enum (Identity, Sequence, UUID, Auto, Table)
- Support for nullable columns, unique constraints
- Length, precision, scale specifications
- Database-specific type names
- Complete XML documentation
- Comprehensive unit tests with edge cases

---

## [0.1.0] - 2025-11-04

### Added
- Initial project structure
- Solution file (NPA.sln)
- Core library (NPA.Core)
- Test project (NPA.Core.Tests)
- Sample project (BasicUsage)
- Documentation framework
- Development roadmap

### Dependencies
- Dapper 2.1.35
- Microsoft.Data.SqlClient 5.1.5
- Microsoft.Extensions.DependencyInjection.Abstractions 7.0.0
- Microsoft.Extensions.Logging.Abstractions 7.0.1
- xUnit 2.4.2
- Moq 4.20.69
- FluentAssertions 6.12.0
- Testcontainers 3.6.0

---

## Performance Benchmarks

### Metadata Access (Phase 2.7)
- **Reflection-based**: ~500-1000 µs per access
- **Generated Metadata Provider**: ~2 µs per access
- **Speedup**: **250-500x faster** 🚀

### Bulk Operations (Phase 3.3)
- **Individual Inserts**: 100 records = ~1000ms
- **Bulk Insert**: 100 records = ~10ms
- **Individual Inserts**: 10,000 records = ~100,000ms
- **Bulk Insert**: 10,000 records = ~100ms
- **Speedup**: **10-100x faster** depending on dataset size

### Transaction Batching (Phase 3.1)
- **Without Transaction**: 100 operations = 100 DB calls
- **With Transaction**: 100 operations = 1 DB call
- **Reduction**: **90-95% fewer round trips**

---

## Breaking Changes

### [None yet]
The library is still in development (version 0.x.x), so the API may change between versions.

---

## Migration Guide

### Upcoming 1.0.0 Release
When NPA reaches 1.0.0, the API will be stable and follow semantic versioning strictly.

---

## Roadmap

### Phase 6: Tooling & Ecosystem (Planned)
- [ ] VS Code Extension (6.1)
- [ ] Code Generation Tools (6.2)
- [ ] Performance Profiling (6.3)
- [ ] Comprehensive Documentation (6.4)

### Future Considerations
- Native AOT support
- Additional database providers (Oracle, MongoDB, Cassandra)
- GraphQL integration
- Event sourcing support
- CQRS patterns

---

## Credits

### Contributors
- **Primary Developer**: Ratana CHH
- **Inspiration**: Java Persistence API (JPA)
- **Foundation**: Dapper (by StackExchange)

### Community
Special thanks to all contributors, testers, and users who have provided feedback and support.

---

## License

[MIT License](LICENSE) - See LICENSE file for details

---

**Note**: This project is currently in active development. Version 1.0.0 will be released when all Phase 1-5 features are complete and battle-tested.
