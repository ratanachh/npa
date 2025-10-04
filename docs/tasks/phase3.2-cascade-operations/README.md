# Phase 3.2: Cascade Operations

## 📋 Task Overview

**Objective**: Implement cascade operations that automatically propagate entity state changes to related entities, providing JPA-like cascade behavior with Dapper performance.

**Priority**: Medium  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1 (Transaction Management)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] CascadeType enum is complete
- [ ] Cascade operations are implemented
- [ ] EntityManager supports cascading
- [ ] Repository generation includes cascading
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. CascadeType Enum
- **Purpose**: Defines cascade operation types
- **Values**:
  - `None` - No cascade operations
  - `All` - All cascade operations
  - `Persist` - Cascade persist operations
  - `Merge` - Cascade merge operations
  - `Remove` - Cascade remove operations
  - `Refresh` - Cascade refresh operations
  - `Detach` - Cascade detach operations

### 2. Cascade Operations
- **Persist Cascade**: Automatically persist related entities
- **Merge Cascade**: Automatically merge related entities
- **Remove Cascade**: Automatically remove related entities
- **Refresh Cascade**: Automatically refresh related entities
- **Detach Cascade**: Automatically detach related entities

### 3. EntityManager Integration
- **Cascade Detection**: Detect cascade operations from metadata
- **Cascade Execution**: Execute cascade operations
- **Cascade Validation**: Validate cascade operations
- **Cascade Error Handling**: Handle cascade operation errors

### 4. Repository Generation
- **Cascade Method Generation**: Generate cascade methods
- **Cascade Query Generation**: Generate cascade queries
- **Cascade Validation**: Generate cascade validation
- **Cascade Error Handling**: Generate cascade error handling

### 5. Performance Optimization
- **Batch Operations**: Batch cascade operations
- **Lazy Loading**: Lazy load related entities
- **Caching**: Cache cascade operations
- **Optimization**: Optimize cascade performance

## 🏗️ Implementation Plan

### Step 1: Create Cascade Types
1. Create `CascadeType` enum
2. Create `CascadeOperation` class
3. Create `CascadeContext` class
4. Create `CascadeResult` class

### Step 2: Implement Cascade Operations
1. Create `ICascadeService` interface
2. Create `CascadeService` class
3. Implement persist cascade
4. Implement merge cascade
5. Implement remove cascade

### Step 3: Update EntityManager
1. Add cascade detection
2. Add cascade execution
3. Add cascade validation
4. Add cascade error handling

### Step 4: Update Repository Generation
1. Add cascade method generation
2. Add cascade query generation
3. Add cascade validation
4. Add cascade error handling

### Step 5: Add Performance Optimization
1. Implement batch operations
2. Implement lazy loading
3. Implement caching
4. Implement optimization

### Step 6: Create Unit Tests
1. Test cascade types
2. Test cascade operations
3. Test EntityManager integration
4. Test repository generation

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Cascade operations guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/Cascade/
├── CascadeType.cs
├── CascadeOperation.cs
├── CascadeContext.cs
├── CascadeResult.cs
├── ICascadeService.cs
├── CascadeService.cs
├── CascadeDetector.cs
├── CascadeExecutor.cs
└── CascadeValidator.cs

tests/NPA.Core.Tests/Cascade/
├── CascadeTypeTests.cs
├── CascadeOperationTests.cs
├── CascadeServiceTests.cs
├── CascadeDetectorTests.cs
├── CascadeExecutorTests.cs
└── CascadeValidatorTests.cs
```

## 💻 Code Examples

### CascadeType Enum
```csharp
[Flags]
public enum CascadeType
{
    None = 0,
    Persist = 1,
    Merge = 2,
    Remove = 4,
    Refresh = 8,
    Detach = 16,
    All = Persist | Merge | Remove | Refresh | Detach
}

public static class CascadeTypeExtensions
{
    public static bool HasFlag(this CascadeType cascadeType, CascadeType flag)
    {
        return (cascadeType & flag) == flag;
    }
    
    public static bool SupportsPersist(this CascadeType cascadeType)
    {
        return cascadeType.HasFlag(CascadeType.Persist);
    }
    
    public static bool SupportsMerge(this CascadeType cascadeType)
    {
        return cascadeType.HasFlag(CascadeType.Merge);
    }
    
    public static bool SupportsRemove(this CascadeType cascadeType)
    {
        return cascadeType.HasFlag(CascadeType.Remove);
    }
    
    public static bool SupportsRefresh(this CascadeType cascadeType)
    {
        return cascadeType.HasFlag(CascadeType.Refresh);
    }
    
    public static bool SupportsDetach(this CascadeType cascadeType)
    {
        return cascadeType.HasFlag(CascadeType.Detach);
    }
}
```

### CascadeOperation Class
```csharp
public class CascadeOperation
{
    public CascadeType Type { get; set; }
    public object Entity { get; set; }
    public string PropertyName { get; set; }
    public object RelatedEntity { get; set; }
    public CascadeContext Context { get; set; }
    
    public CascadeOperation(CascadeType type, object entity, string propertyName, object relatedEntity, CascadeContext context)
    {
        Type = type;
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        RelatedEntity = relatedEntity ?? throw new ArgumentNullException(nameof(relatedEntity));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
}

public class CascadeContext
{
    public IDbConnection Connection { get; set; }
    public IDbTransaction Transaction { get; set; }
    public IEntityManager EntityManager { get; set; }
    public IMetadataProvider MetadataProvider { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    public CascadeContext(IDbConnection connection, IDbTransaction transaction, IEntityManager entityManager, IMetadataProvider metadataProvider)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Transaction = transaction;
        EntityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        MetadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
    }
}

public class CascadeResult
{
    public bool Success { get; set; }
    public List<CascadeOperation> ExecutedOperations { get; set; } = new();
    public List<Exception> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
    
    public bool HasErrors => Errors.Any();
    public int ExecutedCount => ExecutedOperations.Count;
    public int ErrorCount => Errors.Count;
}
```

### CascadeService Class
```csharp
public class CascadeService : ICascadeService
{
    private readonly ICascadeDetector _cascadeDetector;
    private readonly ICascadeExecutor _cascadeExecutor;
    private readonly ICascadeValidator _cascadeValidator;
    
    public CascadeService(
        ICascadeDetector cascadeDetector,
        ICascadeExecutor cascadeExecutor,
        ICascadeValidator cascadeValidator)
    {
        _cascadeDetector = cascadeDetector ?? throw new ArgumentNullException(nameof(cascadeDetector));
        _cascadeExecutor = cascadeExecutor ?? throw new ArgumentNullException(nameof(cascadeExecutor));
        _cascadeValidator = cascadeValidator ?? throw new ArgumentNullException(nameof(cascadeValidator));
    }
    
    public async Task<CascadeResult> ExecuteCascadeAsync<T>(T entity, CascadeType cascadeType, CascadeContext context)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (context == null) throw new ArgumentNullException(nameof(context));
        
        var result = new CascadeResult();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Detect cascade operations
            var operations = _cascadeDetector.DetectCascadeOperations(entity, cascadeType, context);
            
            // Validate cascade operations
            var validationResult = await _cascadeValidator.ValidateCascadeOperationsAsync(operations, context);
            if (!validationResult.IsValid)
            {
                result.Errors.AddRange(validationResult.Errors);
                return result;
            }
            
            // Execute cascade operations
            var executionResult = await _cascadeExecutor.ExecuteCascadeOperationsAsync(operations, context);
            result.ExecutedOperations.AddRange(executionResult.ExecutedOperations);
            result.Errors.AddRange(executionResult.Errors);
            
            result.Success = !result.HasErrors;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex);
            result.Success = false;
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    public async Task<CascadeResult> ExecuteCascadeAsync<T>(T entity, string propertyName, CascadeType cascadeType, CascadeContext context)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        if (context == null) throw new ArgumentNullException(nameof(context));
        
        var result = new CascadeResult();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Detect cascade operations for specific property
            var operations = _cascadeDetector.DetectCascadeOperations(entity, propertyName, cascadeType, context);
            
            // Validate cascade operations
            var validationResult = await _cascadeValidator.ValidateCascadeOperationsAsync(operations, context);
            if (!validationResult.IsValid)
            {
                result.Errors.AddRange(validationResult.Errors);
                return result;
            }
            
            // Execute cascade operations
            var executionResult = await _cascadeExecutor.ExecuteCascadeOperationsAsync(operations, context);
            result.ExecutedOperations.AddRange(executionResult.ExecutedOperations);
            result.Errors.AddRange(executionResult.Errors);
            
            result.Success = !result.HasErrors;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex);
            result.Success = false;
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }
        
        return result;
    }
}
```

### CascadeDetector Class
```csharp
public class CascadeDetector : ICascadeDetector
{
    private readonly IMetadataProvider _metadataProvider;
    
    public CascadeDetector(IMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
    }
    
    public List<CascadeOperation> DetectCascadeOperations<T>(T entity, CascadeType cascadeType, CascadeContext context)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (context == null) throw new ArgumentNullException(nameof(context));
        
        var operations = new List<CascadeOperation>();
        var entityType = typeof(T);
        var metadata = _metadataProvider.GetEntityMetadata(entityType);
        
        foreach (var relationship in metadata.Relationships.Values)
        {
            if (relationship.CascadeType.HasFlag(cascadeType))
            {
                var relatedEntities = GetRelatedEntities(entity, relationship);
                
                foreach (var relatedEntity in relatedEntities)
                {
                    var operation = new CascadeOperation(
                        cascadeType,
                        entity,
                        relationship.PropertyName,
                        relatedEntity,
                        context);
                    
                    operations.Add(operation);
                }
            }
        }
        
        return operations;
    }
    
    public List<CascadeOperation> DetectCascadeOperations<T>(T entity, string propertyName, CascadeType cascadeType, CascadeContext context)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        if (context == null) throw new ArgumentNullException(nameof(context));
        
        var operations = new List<CascadeOperation>();
        var entityType = typeof(T);
        var metadata = _metadataProvider.GetEntityMetadata(entityType);
        
        if (metadata.Relationships.TryGetValue(propertyName, out var relationship))
        {
            if (relationship.CascadeType.HasFlag(cascadeType))
            {
                var relatedEntities = GetRelatedEntities(entity, relationship);
                
                foreach (var relatedEntity in relatedEntities)
                {
                    var operation = new CascadeOperation(
                        cascadeType,
                        entity,
                        relationship.PropertyName,
                        relatedEntity,
                        context);
                    
                    operations.Add(operation);
                }
            }
        }
        
        return operations;
    }
    
    private List<object> GetRelatedEntities<T>(T entity, RelationshipMetadata relationship)
    {
        var relatedEntities = new List<object>();
        var property = typeof(T).GetProperty(relationship.PropertyName);
        
        if (property == null) return relatedEntities;
        
        var value = property.GetValue(entity);
        
        if (value == null) return relatedEntities;
        
        if (relationship.RelationshipType == RelationshipType.OneToOne || relationship.RelationshipType == RelationshipType.ManyToOne)
        {
            relatedEntities.Add(value);
        }
        else if (relationship.RelationshipType == RelationshipType.OneToMany || relationship.RelationshipType == RelationshipType.ManyToMany)
        {
            if (value is IEnumerable<object> collection)
            {
                relatedEntities.AddRange(collection);
            }
        }
        
        return relatedEntities;
    }
}
```

### CascadeExecutor Class
```csharp
public class CascadeExecutor : ICascadeExecutor
{
    public async Task<CascadeResult> ExecuteCascadeOperationsAsync(List<CascadeOperation> operations, CascadeContext context)
    {
        if (operations == null) throw new ArgumentNullException(nameof(operations));
        if (context == null) throw new ArgumentNullException(nameof(context));
        
        var result = new CascadeResult();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            foreach (var operation in operations)
            {
                try
                {
                    await ExecuteCascadeOperationAsync(operation, context);
                    result.ExecutedOperations.Add(operation);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(ex);
                }
            }
            
            result.Success = !result.HasErrors;
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    private async Task ExecuteCascadeOperationAsync(CascadeOperation operation, CascadeContext context)
    {
        switch (operation.Type)
        {
            case CascadeType.Persist:
                await ExecutePersistCascadeAsync(operation, context);
                break;
            case CascadeType.Merge:
                await ExecuteMergeCascadeAsync(operation, context);
                break;
            case CascadeType.Remove:
                await ExecuteRemoveCascadeAsync(operation, context);
                break;
            case CascadeType.Refresh:
                await ExecuteRefreshCascadeAsync(operation, context);
                break;
            case CascadeType.Detach:
                await ExecuteDetachCascadeAsync(operation, context);
                break;
            default:
                throw new NotSupportedException($"Cascade type {operation.Type} is not supported");
        }
    }
    
    private async Task ExecutePersistCascadeAsync(CascadeOperation operation, CascadeContext context)
    {
        await context.EntityManager.PersistAsync(operation.RelatedEntity);
    }
    
    private async Task ExecuteMergeCascadeAsync(CascadeOperation operation, CascadeContext context)
    {
        await context.EntityManager.MergeAsync(operation.RelatedEntity);
    }
    
    private async Task ExecuteRemoveCascadeAsync(CascadeOperation operation, CascadeContext context)
    {
        await context.EntityManager.RemoveAsync(operation.RelatedEntity);
    }
    
    private async Task ExecuteRefreshCascadeAsync(CascadeOperation operation, CascadeContext context)
    {
        await context.EntityManager.RefreshAsync(operation.RelatedEntity);
    }
    
    private async Task ExecuteDetachCascadeAsync(CascadeOperation operation, CascadeContext context)
    {
        await context.EntityManager.DetachAsync(operation.RelatedEntity);
    }
}
```

### EntityManager Integration
```csharp
public class EntityManager : IEntityManager
{
    // ... existing code ...
    
    private readonly ICascadeService _cascadeService;
    
    public EntityManager(
        IDbConnection connection,
        IMetadataProvider metadataProvider,
        IChangeTracker changeTracker,
        ICascadeService cascadeService)
    {
        // ... existing constructor code ...
        _cascadeService = cascadeService ?? throw new ArgumentNullException(nameof(cascadeService));
    }
    
    public async Task PersistAsync<T>(T entity) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        var context = new CascadeContext(_connection, _currentTransaction, this, _metadataProvider);
        var cascadeResult = await _cascadeService.ExecuteCascadeAsync(entity, CascadeType.Persist, context);
        
        if (!cascadeResult.Success)
        {
            throw new CascadeException("Cascade persist operation failed", cascadeResult.Errors);
        }
        
        // ... existing persist logic ...
    }
    
    public async Task MergeAsync<T>(T entity) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        var context = new CascadeContext(_connection, _currentTransaction, this, _metadataProvider);
        var cascadeResult = await _cascadeService.ExecuteCascadeAsync(entity, CascadeType.Merge, context);
        
        if (!cascadeResult.Success)
        {
            throw new CascadeException("Cascade merge operation failed", cascadeResult.Errors);
        }
        
        // ... existing merge logic ...
    }
    
    public async Task RemoveAsync<T>(T entity) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        var context = new CascadeContext(_connection, _currentTransaction, this, _metadataProvider);
        var cascadeResult = await _cascadeService.ExecuteCascadeAsync(entity, CascadeType.Remove, context);
        
        if (!cascadeResult.Success)
        {
            throw new CascadeException("Cascade remove operation failed", cascadeResult.Errors);
        }
        
        // ... existing remove logic ...
    }
}
```

### Usage Examples
```csharp
// Entity with cascade operations
[Entity]
[Table("orders")]
public class Order
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("order_date")]
    public DateTime OrderDate { get; set; }
    
    [Column("total_amount")]
    public decimal TotalAmount { get; set; }
    
    [ManyToOne]
    [JoinColumn("customer_id")]
    public Customer Customer { get; set; }
    
    [OneToMany(MappedBy = "Order", CascadeType = CascadeType.All)]
    public ICollection<OrderItem> Items { get; set; }
}

[Entity]
[Table("order_items")]
public class OrderItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("quantity")]
    public int Quantity { get; set; }
    
    [Column("price")]
    public decimal Price { get; set; }
    
    [ManyToOne]
    [JoinColumn("order_id")]
    public Order Order { get; set; }
    
    [ManyToOne]
    [JoinColumn("product_id")]
    public Product Product { get; set; }
}

// Using cascade operations
public class OrderService
{
    private readonly IEntityManager _entityManager;
    
    public OrderService(IEntityManager entityManager)
    {
        _entityManager = entityManager;
    }
    
    public async Task<Order> CreateOrderWithItemsAsync(Customer customer, List<OrderItemDto> itemDtos)
    {
        var order = new Order
        {
            OrderDate = DateTime.UtcNow,
            Customer = customer,
            Items = new List<OrderItem>()
        };
        
        foreach (var itemDto in itemDtos)
        {
            var orderItem = new OrderItem
            {
                Quantity = itemDto.Quantity,
                Price = itemDto.Price,
                Product = itemDto.Product,
                Order = order
            };
            
            order.Items.Add(orderItem);
        }
        
        // Persist order - this will automatically persist all order items due to cascade
        await _entityManager.PersistAsync(order);
        await _entityManager.FlushAsync();
        
        return order;
    }
    
    public async Task UpdateOrderWithItemsAsync(Order order, List<OrderItemDto> itemDtos)
    {
        // Clear existing items
        order.Items.Clear();
        
        // Add new items
        foreach (var itemDto in itemDtos)
        {
            var orderItem = new OrderItem
            {
                Quantity = itemDto.Quantity,
                Price = itemDto.Price,
                Product = itemDto.Product,
                Order = order
            };
            
            order.Items.Add(orderItem);
        }
        
        // Merge order - this will automatically merge all order items due to cascade
        await _entityManager.MergeAsync(order);
        await _entityManager.FlushAsync();
    }
    
    public async Task DeleteOrderWithItemsAsync(Order order)
    {
        // Remove order - this will automatically remove all order items due to cascade
        await _entityManager.RemoveAsync(order);
        await _entityManager.FlushAsync();
    }
}
```

## 🧪 Test Cases

### Cascade Type Tests
- [ ] Cascade type flags
- [ ] Cascade type extensions
- [ ] Cascade type validation
- [ ] Cascade type combinations

### Cascade Operation Tests
- [ ] Cascade operation creation
- [ ] Cascade operation execution
- [ ] Cascade operation validation
- [ ] Cascade operation error handling

### Cascade Service Tests
- [ ] Cascade service execution
- [ ] Cascade service validation
- [ ] Cascade service error handling
- [ ] Cascade service performance

### EntityManager Integration Tests
- [ ] Persist cascade
- [ ] Merge cascade
- [ ] Remove cascade
- [ ] Refresh cascade
- [ ] Detach cascade

### Integration Tests
- [ ] End-to-end cascade operations
- [ ] Complex cascade scenarios
- [ ] Performance testing
- [ ] Error handling

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic cascade operations
- [ ] Cascade configuration
- [ ] Cascade performance
- [ ] Best practices

### Cascade Operations Guide
- [ ] Cascade types
- [ ] Cascade configuration
- [ ] Cascade patterns
- [ ] Performance optimization
- [ ] Common scenarios

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
1. Move to Phase 3.3: Bulk Operations
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on cascade design
- [ ] Performance considerations for cascading
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
