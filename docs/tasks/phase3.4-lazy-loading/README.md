# Phase 3.4: Lazy Loading

## 📋 Task Overview

**Objective**: Implement lazy loading functionality that defers loading of related entities until they are actually accessed, improving performance and memory usage.

**Priority**: Medium  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.3 (Transaction Management, Cascade Operations, Bulk Operations)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] ILazyLoader interface is complete
- [ ] Lazy loading proxy generation works
- [ ] Lazy loading is integrated with EntityManager
- [ ] Repository generation includes lazy loading
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. ILazyLoader Interface
- **Purpose**: Defines the contract for lazy loading operations
- **Methods**:
  - `Task<T> LoadAsync<T>(object entity, string propertyName)` - Load related entity
  - `Task<IEnumerable<T>> LoadCollectionAsync<T>(object entity, string propertyName)` - Load related collection
  - `bool IsLoaded(object entity, string propertyName)` - Check if property is loaded
  - `void MarkAsLoaded(object entity, string propertyName)` - Mark property as loaded
  - `void MarkAsNotLoaded(object entity, string propertyName)` - Mark property as not loaded

### 2. Lazy Loading Proxy Generation
- **Proxy Creation**: Create lazy loading proxies for entities
- **Property Interception**: Intercept property access
- **Loading Logic**: Implement loading logic
- **Caching**: Cache loaded entities

### 3. EntityManager Integration
- **Lazy Loading Support**: Support lazy loading in EntityManager
- **Proxy Creation**: Create proxies for entities
- **Loading Context**: Maintain loading context
- **Performance Optimization**: Optimize lazy loading performance

### 4. Repository Generation
- **Lazy Loading Methods**: Generate lazy loading methods
- [ ] **Lazy Loading Queries**: Generate lazy loading queries
- [ ] **Lazy Loading Validation**: Generate lazy loading validation
- [ ] **Lazy Loading Error Handling**: Generate lazy loading error handling

### 5. Performance Optimization
- **Lazy Loading Caching**: Cache lazy loaded entities
- **Batch Loading**: Batch load related entities
- **Memory Management**: Manage memory usage
- **Loading Optimization**: Optimize loading performance

## 🏗️ Implementation Plan

### Step 1: Create Lazy Loading Interfaces
1. Create `ILazyLoader` interface
2. Create `ILazyLoadingProxy` interface
3. Create `ILazyLoadingContext` interface
4. Create `ILazyLoadingCache` interface

### Step 2: Implement Lazy Loading Core
1. Create `LazyLoader` class
2. Create `LazyLoadingContext` class
3. Create `LazyLoadingCache` class
4. Implement lazy loading logic

### Step 3: Implement Proxy Generation
1. Create `ILazyLoadingProxyGenerator` interface
2. Create `LazyLoadingProxyGenerator` class
3. Implement proxy creation
4. Implement property interception

### Step 4: Update EntityManager
1. Add lazy loading support
2. Add proxy creation
3. Add loading context
4. Add performance optimization

### Step 5: Update Repository Generation
1. Add lazy loading methods
2. Add lazy loading queries
3. Add lazy loading validation
4. Add lazy loading error handling

### Step 6: Create Unit Tests
1. Test lazy loading interfaces
2. Test lazy loading core
3. Test proxy generation
4. Test EntityManager integration

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Lazy loading guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/LazyLoading/
├── ILazyLoader.cs
├── LazyLoader.cs
├── ILazyLoadingProxy.cs
├── LazyLoadingProxy.cs
├── ILazyLoadingContext.cs
├── LazyLoadingContext.cs
├── ILazyLoadingCache.cs
├── LazyLoadingCache.cs
├── ILazyLoadingProxyGenerator.cs
├── LazyLoadingProxyGenerator.cs
├── LazyLoadingInterceptor.cs
└── LazyLoadingOptions.cs

tests/NPA.Core.Tests/LazyLoading/
├── LazyLoaderTests.cs
├── LazyLoadingProxyTests.cs
├── LazyLoadingContextTests.cs
├── LazyLoadingCacheTests.cs
├── LazyLoadingProxyGeneratorTests.cs
└── LazyLoadingIntegrationTests.cs
```

## 💻 Code Examples

### ILazyLoader Interface
```csharp
public interface ILazyLoader
{
    Task<T> LoadAsync<T>(object entity, string propertyName) where T : class;
    Task<IEnumerable<T>> LoadCollectionAsync<T>(object entity, string propertyName) where T : class;
    bool IsLoaded(object entity, string propertyName);
    void MarkAsLoaded(object entity, string propertyName);
    void MarkAsNotLoaded(object entity, string propertyName);
    void ClearCache();
    void ClearCache(object entity);
    void ClearCache(object entity, string propertyName);
}

public interface ILazyLoadingProxy
{
    object Entity { get; }
    ILazyLoader LazyLoader { get; }
    bool IsLoaded(string propertyName);
    Task LoadAsync(string propertyName);
    void MarkAsLoaded(string propertyName);
    void MarkAsNotLoaded(string propertyName);
}

public interface ILazyLoadingContext
{
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }
    IEntityManager EntityManager { get; }
    IMetadataProvider MetadataProvider { get; }
    Dictionary<string, object> Parameters { get; }
}

public interface ILazyLoadingCache
{
    void Add<T>(object entity, string propertyName, T value);
    T Get<T>(object entity, string propertyName);
    bool TryGet<T>(object entity, string propertyName, out T value);
    void Remove(object entity, string propertyName);
    void Remove(object entity);
    void Clear();
    bool Contains(object entity, string propertyName);
}
```

### LazyLoader Class
```csharp
public class LazyLoader : ILazyLoader
{
    private readonly ILazyLoadingContext _context;
    private readonly ILazyLoadingCache _cache;
    private readonly ILazyLoadingProxyGenerator _proxyGenerator;
    
    public LazyLoader(
        ILazyLoadingContext context,
        ILazyLoadingCache cache,
        ILazyLoadingProxyGenerator proxyGenerator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _proxyGenerator = proxyGenerator ?? throw new ArgumentNullException(nameof(proxyGenerator));
    }
    
    public async Task<T> LoadAsync<T>(object entity, string propertyName) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        // Check if already loaded
        if (IsLoaded(entity, propertyName))
        {
            if (_cache.TryGet<T>(entity, propertyName, out var cachedValue))
            {
                return cachedValue;
            }
        }
        
        // Load the related entity
        var relatedEntity = await LoadRelatedEntityAsync<T>(entity, propertyName);
        
        // Cache the loaded entity
        if (relatedEntity != null)
        {
            _cache.Add(entity, propertyName, relatedEntity);
        }
        
        // Mark as loaded
        MarkAsLoaded(entity, propertyName);
        
        return relatedEntity;
    }
    
    public async Task<IEnumerable<T>> LoadCollectionAsync<T>(object entity, string propertyName) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        // Check if already loaded
        if (IsLoaded(entity, propertyName))
        {
            if (_cache.TryGet<IEnumerable<T>>(entity, propertyName, out var cachedValue))
            {
                return cachedValue;
            }
        }
        
        // Load the related collection
        var relatedCollection = await LoadRelatedCollectionAsync<T>(entity, propertyName);
        
        // Cache the loaded collection
        if (relatedCollection != null)
        {
            _cache.Add(entity, propertyName, relatedCollection);
        }
        
        // Mark as loaded
        MarkAsLoaded(entity, propertyName);
        
        return relatedCollection ?? Enumerable.Empty<T>();
    }
    
    public bool IsLoaded(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        if (entity is ILazyLoadingProxy proxy)
        {
            return proxy.IsLoaded(propertyName);
        }
        
        return _cache.Contains(entity, propertyName);
    }
    
    public void MarkAsLoaded(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        if (entity is ILazyLoadingProxy proxy)
        {
            proxy.MarkAsLoaded(propertyName);
        }
    }
    
    public void MarkAsNotLoaded(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        if (entity is ILazyLoadingProxy proxy)
        {
            proxy.MarkAsNotLoaded(propertyName);
        }
        
        _cache.Remove(entity, propertyName);
    }
    
    public void ClearCache()
    {
        _cache.Clear();
    }
    
    public void ClearCache(object entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        _cache.Remove(entity);
    }
    
    public void ClearCache(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        _cache.Remove(entity, propertyName);
    }
    
    private async Task<T> LoadRelatedEntityAsync<T>(object entity, string propertyName) where T : class
    {
        var entityType = entity.GetType();
        var metadata = _context.MetadataProvider.GetEntityMetadata(entityType);
        
        if (!metadata.Relationships.TryGetValue(propertyName, out var relationship))
        {
            throw new InvalidOperationException($"Relationship '{propertyName}' not found on entity '{entityType.Name}'");
        }
        
        var sql = GenerateLoadSql(relationship, entity);
        var parameters = CreateLoadParameters(relationship, entity);
        
        return await _context.Connection.QueryFirstOrDefaultAsync<T>(sql, parameters, _context.Transaction);
    }
    
    private async Task<IEnumerable<T>> LoadRelatedCollectionAsync<T>(object entity, string propertyName) where T : class
    {
        var entityType = entity.GetType();
        var metadata = _context.MetadataProvider.GetEntityMetadata(entityType);
        
        if (!metadata.Relationships.TryGetValue(propertyName, out var relationship))
        {
            throw new InvalidOperationException($"Relationship '{propertyName}' not found on entity '{entityType.Name}'");
        }
        
        var sql = GenerateLoadCollectionSql(relationship, entity);
        var parameters = CreateLoadParameters(relationship, entity);
        
        return await _context.Connection.QueryAsync<T>(sql, parameters, _context.Transaction);
    }
    
    private string GenerateLoadSql(RelationshipMetadata relationship, object entity)
    {
        var relatedEntityType = Type.GetType(relationship.PropertyType);
        var relatedMetadata = _context.MetadataProvider.GetEntityMetadata(relatedEntityType);
        
        var columns = string.Join(", ", relatedMetadata.Properties.Select(p => p.ColumnName));
        var whereClause = GenerateWhereClause(relationship, entity);
        
        return $"SELECT {columns} FROM {relatedMetadata.TableName} WHERE {whereClause}";
    }
    
    private string GenerateLoadCollectionSql(RelationshipMetadata relationship, object entity)
    {
        var relatedEntityType = Type.GetType(relationship.PropertyType);
        var relatedMetadata = _context.MetadataProvider.GetEntityMetadata(relatedEntityType);
        
        var columns = string.Join(", ", relatedMetadata.Properties.Select(p => p.ColumnName));
        var whereClause = GenerateWhereClause(relationship, entity);
        
        return $"SELECT {columns} FROM {relatedMetadata.TableName} WHERE {whereClause}";
    }
    
    private string GenerateWhereClause(RelationshipMetadata relationship, object entity)
    {
        var entityType = entity.GetType();
        var metadata = _context.MetadataProvider.GetEntityMetadata(entityType);
        
        var primaryKey = metadata.Properties.First(p => p.IsPrimaryKey);
        var primaryKeyValue = primaryKey.GetValue(entity);
        
        return $"{relationship.JoinColumn} = @{primaryKey.Name}";
    }
    
    private Dictionary<string, object> CreateLoadParameters(RelationshipMetadata relationship, object entity)
    {
        var entityType = entity.GetType();
        var metadata = _context.MetadataProvider.GetEntityMetadata(entityType);
        
        var primaryKey = metadata.Properties.First(p => p.IsPrimaryKey);
        var primaryKeyValue = primaryKey.GetValue(entity);
        
        return new Dictionary<string, object>
        {
            [primaryKey.Name] = primaryKeyValue
        };
    }
}
```

### LazyLoadingProxy Class
```csharp
public class LazyLoadingProxy : ILazyLoadingProxy
{
    private readonly Dictionary<string, bool> _loadedProperties = new();
    private readonly Dictionary<string, object> _propertyValues = new();
    
    public object Entity { get; }
    public ILazyLoader LazyLoader { get; }
    
    public LazyLoadingProxy(object entity, ILazyLoader lazyLoader)
    {
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        LazyLoader = lazyLoader ?? throw new ArgumentNullException(nameof(lazyLoader));
    }
    
    public bool IsLoaded(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        return _loadedProperties.TryGetValue(propertyName, out var loaded) && loaded;
    }
    
    public async Task LoadAsync(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        if (IsLoaded(propertyName))
        {
            return;
        }
        
        var property = Entity.GetType().GetProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on entity '{Entity.GetType().Name}'");
        }
        
        var propertyType = property.PropertyType;
        
        if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
        {
            var elementType = propertyType.GetGenericArguments()[0];
            var loadMethod = typeof(ILazyLoader).GetMethod(nameof(ILazyLoader.LoadCollectionAsync));
            var genericLoadMethod = loadMethod.MakeGenericMethod(elementType);
            var task = (Task)genericLoadMethod.Invoke(LazyLoader, new[] { Entity, propertyName });
            await task;
        }
        else
        {
            var loadMethod = typeof(ILazyLoader).GetMethod(nameof(ILazyLoader.LoadAsync));
            var genericLoadMethod = loadMethod.MakeGenericMethod(propertyType);
            var task = (Task)genericLoadMethod.Invoke(LazyLoader, new[] { Entity, propertyName });
            await task;
        }
    }
    
    public void MarkAsLoaded(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        _loadedProperties[propertyName] = true;
    }
    
    public void MarkAsNotLoaded(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        _loadedProperties[propertyName] = false;
        _propertyValues.Remove(propertyName);
    }
    
    public T GetPropertyValue<T>(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        if (_propertyValues.TryGetValue(propertyName, out var value))
        {
            return (T)value;
        }
        
        var property = Entity.GetType().GetProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on entity '{Entity.GetType().Name}'");
        }
        
        var propertyValue = property.GetValue(Entity);
        _propertyValues[propertyName] = propertyValue;
        
        return (T)propertyValue;
    }
    
    public void SetPropertyValue<T>(string propertyName, T value)
    {
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        var property = Entity.GetType().GetProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on entity '{Entity.GetType().Name}'");
        }
        
        property.SetValue(Entity, value);
        _propertyValues[propertyName] = value;
        MarkAsLoaded(propertyName);
    }
}
```

### LazyLoadingCache Class
```csharp
public class LazyLoadingCache : ILazyLoadingCache
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private readonly ReaderWriterLockSlim _lock = new();
    
    public void Add<T>(object entity, string propertyName, T value)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        var key = CreateKey(entity, propertyName);
        _cache[key] = value;
    }
    
    public T Get<T>(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        var key = CreateKey(entity, propertyName);
        return _cache.TryGetValue(key, out var value) ? (T)value : default;
    }
    
    public bool TryGet<T>(object entity, string propertyName, out T value)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        var key = CreateKey(entity, propertyName);
        if (_cache.TryGetValue(key, out var cachedValue))
        {
            value = (T)cachedValue;
            return true;
        }
        
        value = default;
        return false;
    }
    
    public void Remove(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        var key = CreateKey(entity, propertyName);
        _cache.TryRemove(key, out _);
    }
    
    public void Remove(object entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        var entityKey = CreateEntityKey(entity);
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(entityKey)).ToList();
        
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }
    
    public void Clear()
    {
        _cache.Clear();
    }
    
    public bool Contains(object entity, string propertyName)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        var key = CreateKey(entity, propertyName);
        return _cache.ContainsKey(key);
    }
    
    private string CreateKey(object entity, string propertyName)
    {
        var entityKey = CreateEntityKey(entity);
        return $"{entityKey}:{propertyName}";
    }
    
    private string CreateEntityKey(object entity)
    {
        var entityType = entity.GetType();
        var hashCode = entity.GetHashCode();
        return $"{entityType.FullName}:{hashCode}";
    }
}
```

### EntityManager Integration
```csharp
public class EntityManager : IEntityManager
{
    // ... existing code ...
    
    private readonly ILazyLoader _lazyLoader;
    private readonly ILazyLoadingProxyGenerator _proxyGenerator;
    
    public EntityManager(
        IDbConnection connection,
        IMetadataProvider metadataProvider,
        IChangeTracker changeTracker,
        ILazyLoader lazyLoader,
        ILazyLoadingProxyGenerator proxyGenerator)
    {
        // ... existing constructor code ...
        _lazyLoader = lazyLoader ?? throw new ArgumentNullException(nameof(lazyLoader));
        _proxyGenerator = proxyGenerator ?? throw new ArgumentNullException(nameof(proxyGenerator));
    }
    
    public async Task<T?> FindAsync<T>(object id) where T : class
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateSelectByIdSql(metadata);
        var parameters = new { id };
        
        var entity = await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
        
        if (entity != null)
        {
            // Create lazy loading proxy
            entity = _proxyGenerator.CreateProxy(entity, _lazyLoader);
        }
        
        return entity;
    }
    
    public async Task<IEnumerable<T>> FindAllAsync<T>() where T : class
    {
        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateSelectAllSql(metadata);
        
        var entities = await _connection.QueryAsync<T>(sql);
        
        // Create lazy loading proxies
        return entities.Select(entity => _proxyGenerator.CreateProxy(entity, _lazyLoader));
    }
    
    public async Task<IEnumerable<T>> FindByAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateSelectByPredicateSql(metadata, predicate);
        var parameters = CreatePredicateParameters(predicate);
        
        var entities = await _connection.QueryAsync<T>(sql, parameters);
        
        // Create lazy loading proxies
        return entities.Select(entity => _proxyGenerator.CreateProxy(entity, _lazyLoader));
    }
}
```

### Usage Examples
```csharp
// Entity with lazy loading
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
    
    [ManyToOne(FetchType = FetchType.Lazy)]
    [JoinColumn("customer_id")]
    public Customer Customer { get; set; }
    
    [OneToMany(MappedBy = "Order", FetchType = FetchType.Lazy)]
    public ICollection<OrderItem> Items { get; set; }
}

[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("email")]
    public string Email { get; set; }
    
    [OneToMany(MappedBy = "Customer", FetchType = FetchType.Lazy)]
    public ICollection<Order> Orders { get; set; }
}

// Using lazy loading
public class OrderService
{
    private readonly IEntityManager _entityManager;
    
    public OrderService(IEntityManager entityManager)
    {
        _entityManager = entityManager;
    }
    
    public async Task<Order> GetOrderAsync(long id)
    {
        var order = await _entityManager.FindAsync<Order>(id);
        
        if (order != null)
        {
            // Customer will be loaded lazily when accessed
            Console.WriteLine($"Order {order.Id} found");
            
            // This will trigger lazy loading
            var customer = order.Customer;
            Console.WriteLine($"Customer: {customer.Name}");
            
            // This will trigger lazy loading
            var items = order.Items;
            Console.WriteLine($"Items count: {items.Count}");
        }
        
        return order;
    }
    
    public async Task<Customer> GetCustomerAsync(long id)
    {
        var customer = await _entityManager.FindAsync<Customer>(id);
        
        if (customer != null)
        {
            // Orders will be loaded lazily when accessed
            Console.WriteLine($"Customer {customer.Name} found");
            
            // This will trigger lazy loading
            var orders = customer.Orders;
            Console.WriteLine($"Orders count: {orders.Count}");
        }
        
        return customer;
    }
}

// Manual lazy loading control
public class LazyLoadingService
{
    private readonly IEntityManager _entityManager;
    private readonly ILazyLoader _lazyLoader;
    
    public LazyLoadingService(IEntityManager entityManager, ILazyLoader lazyLoader)
    {
        _entityManager = entityManager;
        _lazyLoader = lazyLoader;
    }
    
    public async Task<Order> GetOrderWithManualLoadingAsync(long id)
    {
        var order = await _entityManager.FindAsync<Order>(id);
        
        if (order != null)
        {
            // Check if customer is loaded
            if (!_lazyLoader.IsLoaded(order, "Customer"))
            {
                Console.WriteLine("Customer not loaded, loading now...");
                await _lazyLoader.LoadAsync<Customer>(order, "Customer");
            }
            
            // Check if items are loaded
            if (!_lazyLoader.IsLoaded(order, "Items"))
            {
                Console.WriteLine("Items not loaded, loading now...");
                await _lazyLoader.LoadCollectionAsync<OrderItem>(order, "Items");
            }
        }
        
        return order;
    }
    
    public void ClearLazyLoadingCache(Order order)
    {
        // Clear cache for specific entity
        _lazyLoader.ClearCache(order);
        
        // Or clear specific property
        _lazyLoader.ClearCache(order, "Customer");
    }
}
```

## 🧪 Test Cases

### Lazy Loading Tests
- [ ] Lazy loading interfaces
- [ ] Lazy loading core functionality
- [ ] Lazy loading cache
- [ ] Lazy loading context

### Proxy Generation Tests
- [ ] Proxy creation
- [ ] Property interception
- [ ] Loading logic
- [ ] Error handling

### EntityManager Integration Tests
- [ ] Lazy loading support
- [ ] Proxy creation
- [ ] Loading context
- [ ] Performance optimization

### Integration Tests
- [ ] End-to-end lazy loading
- [ ] Performance testing
- [ ] Memory management
- [ ] Error handling

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic lazy loading
- [ ] Advanced lazy loading
- [ ] Performance optimization
- [ ] Best practices

### Lazy Loading Guide
- [ ] Lazy loading concepts
- [ ] Configuration options
- [ ] Performance considerations
- [ ] Common scenarios
- [ ] Troubleshooting

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
1. Move to Phase 3.5: Connection Pooling
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on lazy loading design
- [ ] Performance considerations for lazy loading
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
