# Phase 1.2: EntityManager with CRUD Operations

## 📋 Task Overview

**Objective**: Implement the core EntityManager class that provides JPA-like entity lifecycle management using Dapper as the underlying data access technology.

**Priority**: High  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1 (Entity Mapping Attributes)  
**Target Framework**: .NET 8.0  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [x] IEntityManager interface is complete
- [x] EntityManager class implements all CRUD operations
- [x] All methods support both async and sync patterns
- [x] Unit tests cover all functionality
- [x] Performance is optimized
- [x] Documentation is complete

## 📌 Update: Synchronous/Asynchronous Methods (2025-01-10)

**Status:** [Completed] **COMPLETED**

All EntityManager methods now support both asynchronous and synchronous patterns:
- `PersistAsync<T>()` / `Persist<T>()`
- `FindAsync<T>()` / `Find<T>()`
- `MergeAsync<T>()` / `Merge<T>()`
- `RemoveAsync<T>()` / `Remove<T>()`
- `FlushAsync()` / `Flush()`
- `ClearAsync()` / `Clear()`

**Use Cases:**
- **Async**: Web applications, APIs, high-concurrency services
- **Sync**: Console applications, CLI tools, batch processing, desktop apps

See **docs/SyncAsyncMethodsGuide.md** for comprehensive documentation.

## 📝 Detailed Requirements

### 1. IEntityManager Interface
- **Purpose**: Defines the contract for entity management
- **Methods** (Async & Sync):
  - `Task PersistAsync<T>(T entity)` / `void Persist<T>(T entity)` - Insert new entity
  - `Task<T?> FindAsync<T>(object id)` / `T? Find<T>(object id)` - Find entity by ID
  - `Task<T?> FindAsync<T>(CompositeKey key)` / `T? Find<T>(CompositeKey key)` - Find entity by composite key
  - `Task MergeAsync<T>(T entity)` / `void Merge<T>(T entity)` - Update existing entity
  - `Task RemoveAsync<T>(T entity)` / `void Remove<T>(T entity)` - Delete entity
  - `Task RemoveAsync<T>(object id)` / `void Remove<T>(object id)` - Delete entity by ID
  - `Task RemoveAsync<T>(CompositeKey key)` / `void Remove<T>(CompositeKey key)` - Delete by composite key
  - `Task FlushAsync()` / `void Flush()` - Flush pending changes
  - `Task ClearAsync()` / `void Clear()` - Clear persistence context
  - `bool Contains<T>(T entity)` - Check if entity is managed
  - `void Detach<T>(T entity)` - Detach entity from context
  - `IQuery<T> CreateQuery<T>(string cpql)` - Create CPQL query

### 2. EntityManager Class
- **Purpose**: Core implementation of entity lifecycle management
- **Dependencies**: IDbConnection, IMetadataProvider
- **Features**:
  - Entity state tracking
  - Change detection
  - Batch operations
  - Transaction support
  - Performance optimization

### 3. Entity State Management
- **States**: Detached, Managed, Removed, Added, Modified
- **Tracking**: Track entity changes for efficient updates
- **Optimization**: Only update changed properties

### 4. Dapper Integration
- **Query Generation**: Generate SQL from entity metadata
- **Parameter Binding**: Safe parameter binding
- **Result Mapping**: Map results to entities
- **Performance**: Optimize for Dapper's strengths

## 🏗️ Implementation Plan

### Step 1: Create Interfaces
1. Create `IEntityManager` interface
2. Create `IEntityState` interface
3. Create `IChangeTracker` interface
4. Create `IMetadataProvider` interface

### Step 2: Implement Core Classes
1. Create `EntityManager` class
2. Create `EntityState` enum
3. Create `ChangeTracker` class
4. Create `MetadataProvider` class

### Step 3: Implement CRUD Operations
1. Implement `PersistAsync()` / `Persist()` methods
2. Implement `FindAsync()` / `Find()` methods
3. Implement `MergeAsync()` / `Merge()` methods
4. Implement `RemoveAsync()` / `Remove()` methods
5. Implement `FlushAsync()` / `Flush()` methods

**Note:** Both async and sync implementations use Dapper's native async/sync methods (not blocking wrappers).

### Step 4: Add State Management
1. Implement entity state tracking
2. Implement change detection
3. Implement batch operations
4. Add performance optimizations

## 💡 Flush Strategy and Change Tracking

### Current Implementation (Phase 1.2)

The current `Flush()` implementation has **immediate execution** behavior:

**How it works:**
- Most operations (`Persist`, `Merge`, `Remove`) execute SQL **immediately**
- Changes are written to the database right away
- `Flush()` only matters for entities without generated IDs

**Current Flush Behavior:**
```csharp
// Operation 1: Executes INSERT immediately
entityManager.Persist(user);  

// Operation 2: Executes UPDATE immediately  
entityManager.Merge(user);    

// Flush: Only processes queued operations (rare)
entityManager.Flush();
```

**When Flush is Currently Used:**
1. **Entities without generated IDs**: Queued and executed on Flush
2. **JPA Compatibility**: Maintains familiar JPA pattern
3. **Explicit Control**: Forces pending changes to database

**Current Limitations:**
- ❌ Cannot batch multiple operations
- ❌ No transaction-aware operation deferral
- ❌ Limited performance optimization opportunities
- [Completed] Simple and predictable behavior
- [Completed] Immediate consistency

### Future Enhancement (Phase 3.1)

**Phase 3.1 will introduce deferred execution with transactions:**

**Enhanced Flush Strategy:**
- Operations will be **queued** in the change tracker by default
- SQL execution will be **deferred** until `Flush()` or transaction commit
- Enables **batching** multiple operations for better performance
- Provides **transaction-aware** operation management

**Future Behavior:**
```csharp
// With transaction support (Phase 3.1)
using var tx = await entityManager.BeginTransactionAsync();

// Operation 1: Queued (not executed yet)
await entityManager.PersistAsync(user1);  

// Operation 2: Queued (not executed yet)
await entityManager.PersistAsync(user2);  

// Operation 3: Queued (not executed yet)
await entityManager.MergeAsync(user3);    

// Flush: Batches all operations and executes them
await entityManager.FlushAsync();

// Commit: Finalizes the transaction
await tx.CommitAsync();
```

**Benefits of Enhanced Strategy:**
- [Completed] Batch multiple operations
- [Completed] Reduce database round-trips
- [Completed] Better transaction support
- [Completed] Performance optimizations
- [Completed] Deferred constraint checking
- [Completed] True unit-of-work pattern

### Design Rationale

**Why keep immediate execution in Phase 1.2?**
1. **Simplicity**: Easy to understand and debug
2. **Predictability**: Know exactly when SQL executes
3. **Incremental Development**: Foundation for future enhancements
4. **JPA Compatibility**: Maintains familiar pattern

**Why enhance in Phase 3.1?**
1. **Transaction Context**: Requires transaction infrastructure
2. **Complexity Management**: Transaction support adds necessary complexity
3. **Performance**: Transaction batching provides real benefits
4. **Unit of Work**: True JPA-like unit-of-work pattern

### Current vs Future Comparison

| Aspect | Current (Phase 1.2) | Future (Phase 3.1) |
|--------|---------------------|-------------------|
| **Execution** | Immediate | Deferred |
| **Batching** | Limited | Full support |
| **Transactions** | Basic | Advanced |
| **Flush Importance** | Low | High |
| **Performance** | Good | Optimized |
| **Complexity** | Simple | Moderate |
| **Use Case** | Simple CRUD | Complex workflows |

### Migration Path

**Phase 1.2 code will continue to work in Phase 3.1:**
```csharp
// This code will work in both phases
await entityManager.PersistAsync(user);
await entityManager.FlushAsync();  // Optional in 1.2, important in 3.1
```

**Phase 3.1 will add new capabilities:**
```csharp
// New in Phase 3.1: Transaction-aware batching
using var tx = await entityManager.BeginTransactionAsync();
await entityManager.PersistAsync(user1);
await entityManager.PersistAsync(user2);
await entityManager.FlushAsync();  // Batches operations
await tx.CommitAsync();
```

### Recommendation

**Current Phase (1.2):**
- Use `Flush()` after each operation for clarity
- Don't rely on batching behavior
- Understand operations execute immediately

**Future Phase (3.1):**
- `Flush()` will become essential for performance
- Operations will batch automatically
- Transaction support will enable advanced patterns

### Step 5: Create Unit Tests
1. Test all CRUD operations
2. Test state management
3. Test error scenarios
4. Test performance

### Step 6: Add Documentation
1. XML documentation comments
2. Usage examples
3. Best practices guide

## 📁 File Structure

```
src/NPA.Core/Core/
├── IEntityManager.cs
├── EntityManager.cs
├── IEntityState.cs
├── EntityState.cs
├── IChangeTracker.cs
├── ChangeTracker.cs
├── IMetadataProvider.cs
└── MetadataProvider.cs

tests/NPA.Core.Tests/Core/
├── EntityManagerTests.cs
├── ChangeTrackerTests.cs
└── MetadataProviderTests.cs
```

## 💻 Code Examples

### IEntityManager Interface
```csharp
public interface IEntityManager : IDisposable
{
    Task PersistAsync<T>(T entity) where T : class;
    Task<T?> FindAsync<T>(object id) where T : class;
    Task<T?> FindAsync<T>(CompositeKey key) where T : class;
    Task MergeAsync<T>(T entity) where T : class;
    Task RemoveAsync<T>(T entity) where T : class;
    Task RemoveAsync<T>(object id) where T : class;
    Task FlushAsync();
    Task ClearAsync();
    bool Contains<T>(T entity) where T : class;
    void Detach<T>(T entity) where T : class;
}
```

### EntityManager Class
```csharp
public class EntityManager : IEntityManager
{
    private readonly IDbConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IChangeTracker _changeTracker;
    
    public EntityManager(IDbConnection connection, IMetadataProvider metadataProvider)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _changeTracker = new ChangeTracker();
    }
    
    public async Task PersistAsync<T>(T entity) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateInsertSql(metadata);
        var parameters = ExtractParameters(entity, metadata);
        
        var id = await _connection.QuerySingleAsync<object>(sql, parameters);
        SetEntityId(entity, id, metadata);
        
        _changeTracker.Track(entity, EntityState.Added);
    }
    
    public async Task<T?> FindAsync<T>(object id) where T : class
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateSelectSql(metadata);
        var parameters = new { id };
        
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }
    
    // ... other methods
}
```

## 🧪 Test Cases

### PersistAsync Tests
- [x] Insert new entity successfully
- [x] Handle null entity (should throw)
- [x] Handle invalid entity (should throw)
- [x] Set generated ID correctly
- [x] Track entity state

### FindAsync Tests
- [x] Find existing entity
- [x] Return null for non-existent entity
- [x] Handle null ID (should throw)
- [x] Handle composite key
- [x] Performance test

### MergeAsync Tests
- [x] Update existing entity
- [x] Handle null entity (should throw)
- [x] Track changes correctly
- [x] Optimize updates (only changed properties)

### RemoveAsync Tests
- [x] Remove existing entity
- [x] Handle null entity (should throw)
- [x] Handle non-existent entity
- [x] Track removal state

### FlushAsync Tests
- [x] Flush all pending changes
- [x] Handle batch operations
- [x] Handle errors during flush
- [x] Performance test

### State Management Tests
- [x] Track entity states correctly
- [x] Detect changes accurately
- [x] Handle state transitions
- [x] Optimize state tracking

### Query Support Tests
- [x] CreateQuery method functionality
- [x] CPQL query creation
- [x] Query parameter binding
- [x] Integration with query system

## 📚 Documentation Requirements

### XML Documentation
- [x] All public members documented
- [x] Parameter descriptions
- [x] Return value descriptions
- [x] Exception documentation
- [x] Usage examples

### Usage Guide
- [x] Basic CRUD operations
- [x] Entity state management
- [x] Performance considerations
- [x] Best practices
- [x] Error handling

## 🔍 Code Review Checklist

- [x] Code follows .NET naming conventions
- [x] All public members have XML documentation
- [x] Error handling is appropriate
- [x] Unit tests cover all scenarios
- [x] Code is readable and maintainable
- [x] Performance is optimized
- [x] Memory usage is efficient
- [x] Thread safety considerations

## 🚀 Next Steps

After completing this task:
1. Move to Phase 1.3: Simple Query Support
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [x] Clarification needed on entity state management - **RESOLVED**: Implemented comprehensive state tracking with ChangeTracker
- [x] Performance considerations for change tracking - **RESOLVED**: Optimized state tracking with efficient change detection
- [x] Integration with Dapper optimizations - **RESOLVED**: Full Dapper integration with async operations
- [x] Error message localization - **RESOLVED**: Using standard .NET exception messages

## [Completed] Implementation Notes

### Completed Features
- Full IEntityManager interface implementation with all CRUD operations
- EntityManager class with comprehensive entity lifecycle management
- ChangeTracker for efficient entity state management
- MetadataProvider for entity metadata handling
- Complete async/await pattern with Dapper integration
- CreateQuery method for CPQL query support
- Comprehensive unit test coverage
- Full XML documentation

### Test Coverage
- **EntityManagerTests.cs**: Tests for all CRUD operations and entity lifecycle
- **ChangeTrackerTests.cs**: Tests for entity state tracking and change detection
- **MetadataProviderTests.cs**: Tests for entity metadata handling
- **Integration tests**: Full integration testing with mock database

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: [Completed] COMPLETED*
