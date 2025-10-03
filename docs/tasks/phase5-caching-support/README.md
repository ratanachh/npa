# Phase 5.1: Caching Support

## 📋 Task Overview

**Objective**: Implement comprehensive caching support for the NPA library to improve performance and reduce database load.

**Priority**: Medium  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.5, Phase 4.1-4.7 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] ICacheProvider interface is complete
- [ ] Multiple cache providers are implemented
- [ ] Cache invalidation works correctly
- [ ] Cache configuration is flexible
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. ICacheProvider Interface
- **Purpose**: Defines the contract for cache operations
- **Methods**:
  - `Task<T> GetAsync<T>(string key)` - Get cached value
  - `Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)` - Set cached value
  - `Task RemoveAsync(string key)` - Remove cached value
  - `Task RemoveByPatternAsync(string pattern)` - Remove values by pattern
  - `Task ClearAsync()` - Clear all cached values
  - `Task<bool> ExistsAsync(string key)` - Check if key exists
  - `Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")` - Get keys by pattern

### 2. Cache Providers
- **MemoryCacheProvider**: In-memory caching using IMemoryCache
- **RedisCacheProvider**: Redis-based distributed caching
- **SqlServerCacheProvider**: SQL Server-based caching
- **NullCacheProvider**: No-op cache provider for testing

### 3. Cache Configuration
- **Cache Attributes**: Mark methods/classes for caching
- **Expiration Policies**: TTL, sliding expiration, absolute expiration
- **Cache Keys**: Automatic key generation and customization
- **Cache Regions**: Logical cache partitioning

### 4. Cache Invalidation
- **Automatic Invalidation**: Invalidate on entity changes
- **Manual Invalidation**: Programmatic cache clearing
- **Pattern-based Invalidation**: Clear by key patterns
- **Event-driven Invalidation**: Invalidate on external events

### 5. Integration with EntityManager
- **Query Caching**: Cache query results
- **Entity Caching**: Cache entity instances
- **Metadata Caching**: Cache entity metadata
- **Performance Monitoring**: Cache hit/miss statistics

## 🏗️ Implementation Plan

### Step 1: Create Cache Interfaces
1. Create `ICacheProvider` interface
2. Create `ICacheConfiguration` interface
3. Create `ICacheKeyGenerator` interface
4. Create `ICacheInvalidator` interface

### Step 2: Implement Cache Providers
1. Create `MemoryCacheProvider` class
2. Create `RedisCacheProvider` class
3. Create `SqlServerCacheProvider` class
4. Create `NullCacheProvider` class

### Step 3: Implement Cache Configuration
1. Create `CacheAttribute` class
2. Create `CacheConfiguration` class
3. Create `CacheKeyGenerator` class
4. Create `CacheInvalidator` class

### Step 4: Add Cache Integration
1. Update EntityManager for caching
2. Add query result caching
3. Add entity caching
4. Add metadata caching

### Step 5: Implement Cache Invalidation
1. Add automatic invalidation
2. Add manual invalidation
3. Add pattern-based invalidation
4. Add event-driven invalidation

### Step 6: Create Unit Tests
1. Test cache providers
2. Test cache configuration
3. Test cache invalidation
4. Test integration

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Cache configuration guide
4. Performance guide

## 📁 File Structure

```
src/NPA.Core/Caching/
├── ICacheProvider.cs
├── MemoryCacheProvider.cs
├── RedisCacheProvider.cs
├── SqlServerCacheProvider.cs
├── NullCacheProvider.cs
├── ICacheConfiguration.cs
├── CacheConfiguration.cs
├── ICacheKeyGenerator.cs
├── CacheKeyGenerator.cs
├── ICacheInvalidator.cs
├── CacheInvalidator.cs
├── CacheAttribute.cs
└── CacheExtensions.cs

tests/NPA.Core.Tests/Caching/
├── MemoryCacheProviderTests.cs
├── RedisCacheProviderTests.cs
├── SqlServerCacheProviderTests.cs
├── CacheConfigurationTests.cs
├── CacheKeyGeneratorTests.cs
├── CacheInvalidatorTests.cs
└── CacheIntegrationTests.cs
```

## 💻 Code Examples

### ICacheProvider Interface
```csharp
public interface ICacheProvider : IDisposable
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task ClearAsync();
    Task<bool> ExistsAsync(string key);
    Task<IEnumerable<string>> GetKeysAsync(string pattern = "*");
}
```

### MemoryCacheProvider Class
```csharp
public class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;
    
    public MemoryCacheProvider(IMemoryCache memoryCache, IOptions<CacheOptions> options)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }
    
    public async Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        
        return _memoryCache.Get<T>(key);
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultExpiration
        };
        
        _memoryCache.Set(key, value, options);
    }
    
    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        
        _memoryCache.Remove(key);
    }
    
    public async Task RemoveByPatternAsync(string pattern)
    {
        // Memory cache doesn't support pattern-based removal
        // This would need to be implemented with a custom wrapper
        throw new NotSupportedException("Pattern-based removal not supported in memory cache");
    }
    
    public async Task ClearAsync()
    {
        if (_memoryCache is MemoryCache mc)
        {
            mc.Compact(1.0);
        }
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        return _memoryCache.TryGetValue(key, out _);
    }
    
    public async Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")
    {
        // Memory cache doesn't support key enumeration
        throw new NotSupportedException("Key enumeration not supported in memory cache");
    }
}
```

### RedisCacheProvider Class
```csharp
public class RedisCacheProvider : ICacheProvider
{
    private readonly IDatabase _database;
    private readonly IServer _server;
    private readonly CacheOptions _options;
    
    public RedisCacheProvider(IConnectionMultiplexer redis, IOptions<CacheOptions> options)
    {
        _database = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints().First());
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }
    
    public async Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value) : default;
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        
        var serializedValue = JsonSerializer.Serialize(value);
        var expiry = expiration ?? _options.DefaultExpiration;
        
        await _database.StringSetAsync(key, serializedValue, expiry);
    }
    
    public async Task RemoveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        
        await _database.KeyDeleteAsync(key);
    }
    
    public async Task RemoveByPatternAsync(string pattern)
    {
        var keys = _server.Keys(pattern: pattern);
        await _database.KeyDeleteAsync(keys.ToArray());
    }
    
    public async Task ClearAsync()
    {
        await _database.ExecuteAsync("FLUSHDB");
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        return await _database.KeyExistsAsync(key);
    }
    
    public async Task<IEnumerable<string>> GetKeysAsync(string pattern = "*")
    {
        var keys = _server.Keys(pattern: pattern);
        return keys.Select(k => k.ToString());
    }
}
```

### CacheAttribute Class
```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class CacheAttribute : Attribute
{
    public string Key { get; set; } = string.Empty;
    public int ExpirationSeconds { get; set; } = 300; // 5 minutes
    public string Region { get; set; } = string.Empty;
    public bool SlidingExpiration { get; set; } = false;
    
    public CacheAttribute() { }
    
    public CacheAttribute(string key)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }
    
    public CacheAttribute(string key, int expirationSeconds)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        ExpirationSeconds = expirationSeconds;
    }
}
```

### Cache Integration with EntityManager
```csharp
public class EntityManager : IEntityManager
{
    private readonly IDbConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IChangeTracker _changeTracker;
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheKeyGenerator _cacheKeyGenerator;
    
    public EntityManager(IDbConnection connection, IMetadataProvider metadataProvider, ICacheProvider cacheProvider)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
        _changeTracker = new ChangeTracker();
        _cacheKeyGenerator = new CacheKeyGenerator();
    }
    
    public async Task<T?> FindAsync<T>(object id) where T : class
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        var cacheKey = _cacheKeyGenerator.GenerateEntityKey<T>(id);
        var cachedEntity = await _cacheProvider.GetAsync<T>(cacheKey);
        
        if (cachedEntity != null)
            return cachedEntity;
        
        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var sql = GenerateSelectByIdSql(metadata);
        var parameters = new { id };
        
        var entity = await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
        
        if (entity != null)
        {
            await _cacheProvider.SetAsync(cacheKey, entity);
        }
        
        return entity;
    }
    
    public async Task MergeAsync<T>(T entity) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var id = GetEntityId(entity, metadata);
        
        // Update database
        var sql = GenerateUpdateSql(metadata);
        var parameters = ExtractParameters(entity, metadata);
        await _connection.ExecuteAsync(sql, parameters);
        
        // Invalidate cache
        var cacheKey = _cacheKeyGenerator.GenerateEntityKey<T>(id);
        await _cacheProvider.RemoveAsync(cacheKey);
        
        // Invalidate related caches
        await InvalidateRelatedCaches<T>(entity);
    }
    
    private async Task InvalidateRelatedCaches<T>(T entity)
    {
        var metadata = _metadataProvider.GetEntityMetadata<T>();
        var entityType = typeof(T).Name;
        
        // Invalidate query caches for this entity type
        var queryPattern = $"query:{entityType}:*";
        await _cacheProvider.RemoveByPatternAsync(queryPattern);
    }
}
```

### Usage Examples
```csharp
// Cache configuration
services.AddNPA(config =>
{
    config.ConnectionString = "Server=localhost;Database=MyApp;";
    config.CacheProvider = CacheProvider.Redis;
    config.CacheOptions = new CacheOptions
    {
        DefaultExpiration = TimeSpan.FromMinutes(30),
        RedisConnectionString = "localhost:6379"
    };
});

// Cached repository methods
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    [Cache("user:by-username:{username}", 600)] // 10 minutes
    Task<User> FindByUsernameAsync(string username);
    
    [Cache("users:active", 300)] // 5 minutes
    Task<IEnumerable<User>> FindActiveUsersAsync();
}

// Manual cache operations
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheProvider _cacheProvider;
    
    public async Task<User> GetUserWithCacheAsync(long id)
    {
        var cacheKey = $"user:{id}";
        var cachedUser = await _cacheProvider.GetAsync<User>(cacheKey);
        
        if (cachedUser != null)
            return cachedUser;
        
        var user = await _userRepository.GetByIdAsync(id);
        if (user != null)
        {
            await _cacheProvider.SetAsync(cacheKey, user, TimeSpan.FromMinutes(30));
        }
        
        return user;
    }
    
    public async Task InvalidateUserCacheAsync(long userId)
    {
        var cacheKey = $"user:{userId}";
        await _cacheProvider.RemoveAsync(cacheKey);
        
        // Also invalidate related caches
        await _cacheProvider.RemoveByPatternAsync("users:*");
    }
}
```

## 🧪 Test Cases

### Cache Provider Tests
- [ ] Memory cache provider functionality
- [ ] Redis cache provider functionality
- [ ] SQL Server cache provider functionality
- [ ] Null cache provider functionality
- [ ] Error handling for each provider

### Cache Configuration Tests
- [ ] Cache attribute processing
- [ ] Cache key generation
- [ ] Expiration handling
- [ ] Region support
- [ ] Configuration validation

### Cache Integration Tests
- [ ] EntityManager caching
- [ ] Query result caching
- [ ] Metadata caching
- [ ] Cache invalidation
- [ ] Performance testing

### Cache Invalidation Tests
- [ ] Automatic invalidation
- [ ] Manual invalidation
- [ ] Pattern-based invalidation
- [ ] Event-driven invalidation
- [ ] Related cache invalidation

### Performance Tests
- [ ] Cache hit/miss ratios
- [ ] Memory usage
- [ ] Response times
- [ ] Throughput testing
- [ ] Load testing

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic caching operations
- [ ] Cache configuration
- [ ] Cache invalidation
- [ ] Performance optimization
- [ ] Best practices

### Cache Configuration Guide
- [ ] Cache providers
- [ ] Configuration options
- [ ] Expiration policies
- [ ] Cache regions
- [ ] Performance tuning

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
1. Move to Phase 5.2: Database Migrations
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on cache providers
- [ ] Performance considerations for caching
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
