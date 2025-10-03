# Phase 1.2: EntityManager with CRUD Operations

## 📋 Task Overview

**Objective**: Implement the core EntityManager class that provides JPA-like entity lifecycle management using Dapper as the underlying data access technology.

**Priority**: High  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1 (Entity Mapping Attributes)  
**Target Framework**: .NET 6.0  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] IEntityManager interface is complete
- [ ] EntityManager class implements all CRUD operations
- [ ] All methods are async and use Dapper
- [ ] Unit tests cover all functionality
- [ ] Performance is optimized
- [ ] Documentation is complete

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
- [ ] Insert new entity successfully
- [ ] Handle null entity (should throw)
- [ ] Handle invalid entity (should throw)
- [ ] Set generated ID correctly
- [ ] Track entity state

### FindAsync Tests
- [ ] Find existing entity
- [ ] Return null for non-existent entity
- [ ] Handle null ID (should throw)
- [ ] Handle composite key
- [ ] Performance test

### MergeAsync Tests
- [ ] Update existing entity
- [ ] Handle null entity (should throw)
- [ ] Track changes correctly
- [ ] Optimize updates (only changed properties)

### RemoveAsync Tests
- [ ] Remove existing entity
- [ ] Handle null entity (should throw)
- [ ] Handle non-existent entity
- [ ] Track removal state

### FlushAsync Tests
- [ ] Flush all pending changes
- [ ] Handle batch operations
- [ ] Handle errors during flush
- [ ] Performance test

### State Management Tests
- [ ] Track entity states correctly
- [ ] Detect changes accurately
- [ ] Handle state transitions
- [ ] Optimize state tracking

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic CRUD operations
- [ ] Entity state management
- [ ] Performance considerations
- [ ] Best practices
- [ ] Error handling

## 🔍 Code Review Checklist

- [ ] Code follows .NET naming conventions
- [ ] All public members have XML documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance is optimized
- [ ] Memory usage is efficient
- [ ] Thread safety considerations

## 🚀 Next Steps

After completing this task:
1. Move to Phase 1.3: Simple Query Support
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on entity state management
- [ ] Performance considerations for change tracking
- [ ] Integration with Dapper optimizations
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
