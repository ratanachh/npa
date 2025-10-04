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
- [x] All methods are async and use Dapper
- [x] Unit tests cover all functionality
- [x] Performance is optimized
- [x] Documentation is complete

## 📝 Detailed Requirements

### 1. IEntityManager Interface
- **Purpose**: Defines the contract for entity management
- **Methods**:
  - `Task PersistAsync<T>(T entity)` - Insert new entity
  - `Task<T> FindAsync<T>(object id)` - Find entity by ID
  - `Task<T> FindAsync<T>(CompositeKey key)` - Find entity by composite key
  - `Task MergeAsync<T>(T entity)` - Update existing entity
  - `Task RemoveAsync<T>(T entity)` - Delete entity
  - `Task RemoveAsync<T>(object id)` - Delete entity by ID
  - `Task FlushAsync()` - Flush pending changes
  - `Task ClearAsync()` - Clear persistence context
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
1. Implement `PersistAsync()` method
2. Implement `FindAsync()` method
3. Implement `MergeAsync()` method
4. Implement `RemoveAsync()` method
5. Implement `FlushAsync()` method

### Step 4: Add State Management
1. Implement entity state tracking
2. Implement change detection
3. Implement batch operations
4. Add performance optimizations

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

## ✅ Implementation Notes

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
*Status: ✅ COMPLETED*
