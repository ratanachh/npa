# Phase 3.5: Connection Pooling

## 📋 Task Overview

**Objective**: Implement efficient connection pooling that manages database connections to improve performance, reduce connection overhead, and provide better resource management.

**Priority**: Medium  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.4 (Transaction Management, Cascade Operations, Bulk Operations, Lazy Loading)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] IConnectionPool interface is complete
- [ ] Connection pooling is implemented
- [ ] Connection management works
- [ ] Performance is optimized
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. IConnectionPool Interface
- **Purpose**: Defines the contract for connection pooling
- **Methods**:
  - `Task<IDbConnection> GetConnectionAsync()` - Get connection from pool
  - `Task ReturnConnectionAsync(IDbConnection connection)` - Return connection to pool
  - `Task<IDbConnection> GetConnectionAsync(string connectionString)` - Get connection with specific connection string
  - `Task<int> GetAvailableConnectionsCountAsync()` - Get available connections count
  - `Task<int> GetTotalConnectionsCountAsync()` - Get total connections count
  - `Task ClearPoolAsync()` - Clear connection pool
  - `Task<ConnectionPoolStats> GetStatsAsync()` - Get connection pool statistics

### 2. Connection Pool Implementation
- **Pool Management**: Manage connection pool lifecycle
- **Connection Creation**: Create new connections
- **Connection Validation**: Validate connections
- **Connection Cleanup**: Clean up connections
- **Pool Configuration**: Configure pool settings

### 3. Connection Management
- **Connection Lifecycle**: Manage connection lifecycle
- **Connection State**: Track connection state
- **Connection Health**: Monitor connection health
- **Connection Recovery**: Recover from connection failures

### 4. Performance Optimization
- **Pool Sizing**: Optimize pool size
- **Connection Reuse**: Reuse connections efficiently
- **Load Balancing**: Balance connection load
- **Memory Management**: Manage memory usage

### 5. Monitoring and Statistics
- **Connection Statistics**: Track connection statistics
- **Performance Metrics**: Monitor performance metrics
- **Health Monitoring**: Monitor connection health
- **Error Tracking**: Track connection errors

## 🏗️ Implementation Plan

### Step 1: Create Connection Pool Interfaces
1. Create `IConnectionPool` interface
2. Create `IConnectionPoolManager` interface
3. Create `IConnectionValidator` interface
4. Create `IConnectionPoolStats` interface

### Step 2: Implement Connection Pool
1. Create `ConnectionPool` class
2. Create `ConnectionPoolManager` class
3. Create `ConnectionValidator` class
4. Implement pool management

### Step 3: Add Connection Management
1. Implement connection lifecycle
2. Implement connection state tracking
3. Implement connection health monitoring
4. Implement connection recovery

### Step 4: Add Performance Optimization
1. Implement pool sizing
2. Implement connection reuse
3. Implement load balancing
4. Implement memory management

### Step 5: Add Monitoring and Statistics
1. Implement connection statistics
2. Implement performance metrics
3. Implement health monitoring
4. Implement error tracking

### Step 6: Create Unit Tests
1. Test connection pool interfaces
2. Test connection pool implementation
3. Test connection management
4. Test performance optimization

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Connection pooling guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/ConnectionPooling/
├── IConnectionPool.cs
├── ConnectionPool.cs
├── IConnectionPoolManager.cs
├── ConnectionPoolManager.cs
├── IConnectionValidator.cs
├── ConnectionValidator.cs
├── IConnectionPoolStats.cs
├── ConnectionPoolStats.cs
├── ConnectionPoolOptions.cs
├── ConnectionPoolHealth.cs
└── ConnectionPoolMetrics.cs

tests/NPA.Core.Tests/ConnectionPooling/
├── ConnectionPoolTests.cs
├── ConnectionPoolManagerTests.cs
├── ConnectionValidatorTests.cs
├── ConnectionPoolStatsTests.cs
└── ConnectionPoolingIntegrationTests.cs
```

## 💻 Code Examples

### IConnectionPool Interface
```csharp
public interface IConnectionPool : IDisposable
{
    Task<IDbConnection> GetConnectionAsync();
    Task<IDbConnection> GetConnectionAsync(string connectionString);
    Task ReturnConnectionAsync(IDbConnection connection);
    Task<int> GetAvailableConnectionsCountAsync();
    Task<int> GetTotalConnectionsCountAsync();
    Task ClearPoolAsync();
    Task<ConnectionPoolStats> GetStatsAsync();
    Task<ConnectionPoolHealth> GetHealthAsync();
    bool IsDisposed { get; }
}

public interface IConnectionPoolManager
{
    Task<IConnectionPool> CreatePoolAsync(string connectionString, ConnectionPoolOptions options);
    Task<IConnectionPool> GetPoolAsync(string connectionString);
    Task RemovePoolAsync(string connectionString);
    Task ClearAllPoolsAsync();
    Task<Dictionary<string, ConnectionPoolStats>> GetAllStatsAsync();
    Task<Dictionary<string, ConnectionPoolHealth>> GetAllHealthAsync();
}

public interface IConnectionValidator
{
    Task<bool> ValidateConnectionAsync(IDbConnection connection);
    Task<bool> ValidateConnectionAsync(IDbConnection connection, string connectionString);
    Task<ConnectionValidationResult> ValidateConnectionWithResultAsync(IDbConnection connection);
    Task<ConnectionValidationResult> ValidateConnectionWithResultAsync(IDbConnection connection, string connectionString);
}

public class ConnectionPoolStats
{
    public int TotalConnections { get; set; }
    public int AvailableConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public int CreatedConnections { get; set; }
    public int DestroyedConnections { get; set; }
    public int FailedConnections { get; set; }
    public TimeSpan AverageConnectionLifetime { get; set; }
    public TimeSpan AverageConnectionIdleTime { get; set; }
    public DateTime LastConnectionCreated { get; set; }
    public DateTime LastConnectionDestroyed { get; set; }
    public DateTime LastConnectionReturned { get; set; }
    public DateTime LastConnectionBorrowed { get; set; }
}

public class ConnectionPoolHealth
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; }
    public List<string> Issues { get; set; } = new();
    public DateTime LastHealthCheck { get; set; }
    public TimeSpan HealthCheckDuration { get; set; }
    public int ConsecutiveFailures { get; set; }
    public DateTime LastFailure { get; set; }
}

public class ConnectionPoolOptions
{
    public int MinPoolSize { get; set; } = 5;
    public int MaxPoolSize { get; set; } = 100;
    public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan ConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan ConnectionValidationTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool ValidateConnectionsOnBorrow { get; set; } = true;
    public bool ValidateConnectionsOnReturn { get; set; } = false;
    public bool ValidateConnectionsOnIdle { get; set; } = true;
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableStatistics { get; set; } = true;
    public bool EnableHealthMonitoring { get; set; } = true;
}
```

### ConnectionPool Class
```csharp
public class ConnectionPool : IConnectionPool
{
    private readonly string _connectionString;
    private readonly ConnectionPoolOptions _options;
    private readonly IConnectionValidator _validator;
    private readonly ConcurrentQueue<PooledConnection> _availableConnections = new();
    private readonly ConcurrentDictionary<IDbConnection, PooledConnection> _activeConnections = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer _healthCheckTimer;
    private readonly Timer _cleanupTimer;
    private readonly object _lock = new();
    private int _totalConnections;
    private int _createdConnections;
    private int _destroyedConnections;
    private int _failedConnections;
    private DateTime _lastConnectionCreated;
    private DateTime _lastConnectionDestroyed;
    private DateTime _lastConnectionReturned;
    private DateTime _lastConnectionBorrowed;
    private bool _disposed;
    
    public bool IsDisposed => _disposed;
    
    public ConnectionPool(string connectionString, ConnectionPoolOptions options, IConnectionValidator validator)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _semaphore = new SemaphoreSlim(options.MaxPoolSize, options.MaxPoolSize);
        
        if (options.EnableHealthMonitoring)
        {
            _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.Zero, options.HealthCheckInterval);
        }
        
        _cleanupTimer = new Timer(CleanupIdleConnections, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        
        // Pre-populate pool with minimum connections
        _ = Task.Run(PrePopulatePoolAsync);
    }
    
    public async Task<IDbConnection> GetConnectionAsync()
    {
        return await GetConnectionAsync(_connectionString);
    }
    
    public async Task<IDbConnection> GetConnectionAsync(string connectionString)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ConnectionPool));
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        
        await _semaphore.WaitAsync();
        
        try
        {
            // Try to get an available connection
            if (_availableConnections.TryDequeue(out var pooledConnection))
            {
                // Validate connection if required
                if (_options.ValidateConnectionsOnBorrow)
                {
                    var validationResult = await _validator.ValidateConnectionWithResultAsync(pooledConnection.Connection, connectionString);
                    if (!validationResult.IsValid)
                    {
                        // Connection is invalid, create a new one
                        pooledConnection = await CreatePooledConnectionAsync(connectionString);
                    }
                }
                
                pooledConnection.LastBorrowed = DateTime.UtcNow;
                pooledConnection.IsActive = true;
                _activeConnections[pooledConnection.Connection] = pooledConnection;
                _lastConnectionBorrowed = DateTime.UtcNow;
                
                return pooledConnection.Connection;
            }
            
            // No available connections, create a new one if under limit
            if (_totalConnections < _options.MaxPoolSize)
            {
                pooledConnection = await CreatePooledConnectionAsync(connectionString);
                pooledConnection.LastBorrowed = DateTime.UtcNow;
                pooledConnection.IsActive = true;
                _activeConnections[pooledConnection.Connection] = pooledConnection;
                _lastConnectionBorrowed = DateTime.UtcNow;
                
                return pooledConnection.Connection;
            }
            
            // Pool is full, wait for a connection to become available
            throw new InvalidOperationException("Connection pool is full and no connections are available");
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task ReturnConnectionAsync(IDbConnection connection)
    {
        if (_disposed) return;
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        
        if (_activeConnections.TryRemove(connection, out var pooledConnection))
        {
            pooledConnection.IsActive = false;
            pooledConnection.LastReturned = DateTime.UtcNow;
            _lastConnectionReturned = DateTime.UtcNow;
            
            // Validate connection if required
            if (_options.ValidateConnectionsOnReturn)
            {
                var validationResult = await _validator.ValidateConnectionWithResultAsync(connection, _connectionString);
                if (!validationResult.IsValid)
                {
                    // Connection is invalid, destroy it
                    await DestroyConnectionAsync(pooledConnection);
                    return;
                }
            }
            
            // Check if connection has exceeded its lifetime
            if (DateTime.UtcNow - pooledConnection.CreatedAt > _options.ConnectionLifetime)
            {
                await DestroyConnectionAsync(pooledConnection);
                return;
            }
            
            // Return connection to pool
            _availableConnections.Enqueue(pooledConnection);
        }
    }
    
    public async Task<int> GetAvailableConnectionsCountAsync()
    {
        return await Task.FromResult(_availableConnections.Count);
    }
    
    public async Task<int> GetTotalConnectionsCountAsync()
    {
        return await Task.FromResult(_totalConnections);
    }
    
    public async Task ClearPoolAsync()
    {
        if (_disposed) return;
        
        // Clear available connections
        while (_availableConnections.TryDequeue(out var pooledConnection))
        {
            await DestroyConnectionAsync(pooledConnection);
        }
        
        // Clear active connections
        foreach (var kvp in _activeConnections)
        {
            await DestroyConnectionAsync(kvp.Value);
        }
        
        _activeConnections.Clear();
    }
    
    public async Task<ConnectionPoolStats> GetStatsAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ConnectionPool));
        
        return await Task.FromResult(new ConnectionPoolStats
        {
            TotalConnections = _totalConnections,
            AvailableConnections = _availableConnections.Count,
            ActiveConnections = _activeConnections.Count,
            IdleConnections = _availableConnections.Count,
            CreatedConnections = _createdConnections,
            DestroyedConnections = _destroyedConnections,
            FailedConnections = _failedConnections,
            AverageConnectionLifetime = CalculateAverageConnectionLifetime(),
            AverageConnectionIdleTime = CalculateAverageConnectionIdleTime(),
            LastConnectionCreated = _lastConnectionCreated,
            LastConnectionDestroyed = _lastConnectionDestroyed,
            LastConnectionReturned = _lastConnectionReturned,
            LastConnectionBorrowed = _lastConnectionBorrowed
        });
    }
    
    public async Task<ConnectionPoolHealth> GetHealthAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ConnectionPool));
        
        var health = new ConnectionPoolHealth
        {
            LastHealthCheck = DateTime.UtcNow,
            IsHealthy = true,
            Status = "Healthy"
        };
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Check if pool is healthy
            if (_totalConnections == 0)
            {
                health.IsHealthy = false;
                health.Status = "No connections available";
                health.Issues.Add("No connections in pool");
            }
            
            if (_availableConnections.Count == 0 && _activeConnections.Count >= _options.MaxPoolSize)
            {
                health.IsHealthy = false;
                health.Status = "Pool exhausted";
                health.Issues.Add("All connections are in use");
            }
            
            if (_failedConnections > _createdConnections * 0.1) // More than 10% failure rate
            {
                health.IsHealthy = false;
                health.Status = "High failure rate";
                health.Issues.Add($"High connection failure rate: {_failedConnections}/{_createdConnections}");
            }
        }
        finally
        {
            stopwatch.Stop();
            health.HealthCheckDuration = stopwatch.Elapsed;
        }
        
        return await Task.FromResult(health);
    }
    
    private async Task PrePopulatePoolAsync()
    {
        for (int i = 0; i < _options.MinPoolSize; i++)
        {
            try
            {
                var pooledConnection = await CreatePooledConnectionAsync(_connectionString);
                _availableConnections.Enqueue(pooledConnection);
            }
            catch (Exception ex)
            {
                // Log error but continue
                Console.WriteLine($"Failed to pre-populate connection pool: {ex.Message}");
            }
        }
    }
    
    private async Task<PooledConnection> CreatePooledConnectionAsync(string connectionString)
    {
        try
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var pooledConnection = new PooledConnection
            {
                Connection = connection,
                CreatedAt = DateTime.UtcNow,
                LastBorrowed = DateTime.UtcNow,
                LastReturned = DateTime.UtcNow,
                IsActive = false
            };
            
            Interlocked.Increment(ref _totalConnections);
            Interlocked.Increment(ref _createdConnections);
            _lastConnectionCreated = DateTime.UtcNow;
            
            return pooledConnection;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedConnections);
            throw new InvalidOperationException("Failed to create database connection", ex);
        }
    }
    
    private async Task DestroyConnectionAsync(PooledConnection pooledConnection)
    {
        try
        {
            if (pooledConnection.Connection.State != ConnectionState.Closed)
            {
                await pooledConnection.Connection.CloseAsync();
            }
            
            pooledConnection.Connection.Dispose();
            
            Interlocked.Decrement(ref _totalConnections);
            Interlocked.Increment(ref _destroyedConnections);
            _lastConnectionDestroyed = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            // Log error but continue
            Console.WriteLine($"Failed to destroy connection: {ex.Message}");
        }
    }
    
    private void PerformHealthCheck(object state)
    {
        if (_disposed) return;
        
        _ = Task.Run(async () =>
        {
            try
            {
                var health = await GetHealthAsync();
                if (!health.IsHealthy)
                {
                    Console.WriteLine($"Connection pool health check failed: {health.Status}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection pool health check error: {ex.Message}");
            }
        });
    }
    
    private void CleanupIdleConnections(object state)
    {
        if (_disposed) return;
        
        _ = Task.Run(async () =>
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - _options.ConnectionIdleTimeout;
                var connectionsToRemove = new List<PooledConnection>();
                
                foreach (var pooledConnection in _availableConnections)
                {
                    if (pooledConnection.LastReturned < cutoffTime)
                    {
                        connectionsToRemove.Add(pooledConnection);
                    }
                }
                
                foreach (var connection in connectionsToRemove)
                {
                    if (_availableConnections.TryDequeue(out var pooledConnection) && pooledConnection == connection)
                    {
                        await DestroyConnectionAsync(pooledConnection);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection pool cleanup error: {ex.Message}");
            }
        });
    }
    
    private TimeSpan CalculateAverageConnectionLifetime()
    {
        // Implementation to calculate average connection lifetime
        return TimeSpan.Zero;
    }
    
    private TimeSpan CalculateAverageConnectionIdleTime()
    {
        // Implementation to calculate average connection idle time
        return TimeSpan.Zero;
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        _healthCheckTimer?.Dispose();
        _cleanupTimer?.Dispose();
        
        // Clear all connections
        _ = Task.Run(ClearPoolAsync);
        
        _semaphore?.Dispose();
    }
}

public class PooledConnection
{
    public IDbConnection Connection { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastBorrowed { get; set; }
    public DateTime LastReturned { get; set; }
    public bool IsActive { get; set; }
}
```

### ConnectionValidator Class
```csharp
public class ConnectionValidator : IConnectionValidator
{
    public async Task<bool> ValidateConnectionAsync(IDbConnection connection)
    {
        var result = await ValidateConnectionWithResultAsync(connection);
        return result.IsValid;
    }
    
    public async Task<bool> ValidateConnectionAsync(IDbConnection connection, string connectionString)
    {
        var result = await ValidateConnectionWithResultAsync(connection, connectionString);
        return result.IsValid;
    }
    
    public async Task<ConnectionValidationResult> ValidateConnectionWithResultAsync(IDbConnection connection)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        
        var result = new ConnectionValidationResult
        {
            IsValid = false,
            Connection = connection,
            ValidationTime = DateTime.UtcNow
        };
        
        try
        {
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 30;
            
            var value = await command.ExecuteScalarAsync();
            result.IsValid = value != null && value.ToString() == "1";
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Error = ex.Message;
            result.Exception = ex;
        }
        finally
        {
            result.ValidationDuration = DateTime.UtcNow - result.ValidationTime;
        }
        
        return result;
    }
    
    public async Task<ConnectionValidationResult> ValidateConnectionWithResultAsync(IDbConnection connection, string connectionString)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        
        var result = await ValidateConnectionWithResultAsync(connection);
        
        // Additional validation for connection string match
        if (result.IsValid)
        {
            result.IsValid = connection.ConnectionString == connectionString;
            if (!result.IsValid)
            {
                result.Error = "Connection string mismatch";
            }
        }
        
        return result;
    }
}

public class ConnectionValidationResult
{
    public bool IsValid { get; set; }
    public IDbConnection Connection { get; set; }
    public string Error { get; set; }
    public Exception Exception { get; set; }
    public DateTime ValidationTime { get; set; }
    public TimeSpan ValidationDuration { get; set; }
}
```

### ConnectionPoolManager Class
```csharp
public class ConnectionPoolManager : IConnectionPoolManager
{
    private readonly ConcurrentDictionary<string, IConnectionPool> _pools = new();
    private readonly IConnectionValidator _validator;
    private readonly object _lock = new();
    
    public ConnectionPoolManager(IConnectionValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }
    
    public async Task<IConnectionPool> CreatePoolAsync(string connectionString, ConnectionPoolOptions options)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        var pool = new ConnectionPool(connectionString, options, _validator);
        _pools[connectionString] = pool;
        
        return await Task.FromResult(pool);
    }
    
    public async Task<IConnectionPool> GetPoolAsync(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        
        if (_pools.TryGetValue(connectionString, out var pool))
        {
            return await Task.FromResult(pool);
        }
        
        throw new InvalidOperationException($"No connection pool found for connection string: {connectionString}");
    }
    
    public async Task RemovePoolAsync(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        
        if (_pools.TryRemove(connectionString, out var pool))
        {
            pool.Dispose();
        }
        
        await Task.CompletedTask;
    }
    
    public async Task ClearAllPoolsAsync()
    {
        foreach (var kvp in _pools)
        {
            kvp.Value.Dispose();
        }
        
        _pools.Clear();
        await Task.CompletedTask;
    }
    
    public async Task<Dictionary<string, ConnectionPoolStats>> GetAllStatsAsync()
    {
        var stats = new Dictionary<string, ConnectionPoolStats>();
        
        foreach (var kvp in _pools)
        {
            var poolStats = await kvp.Value.GetStatsAsync();
            stats[kvp.Key] = poolStats;
        }
        
        return await Task.FromResult(stats);
    }
    
    public async Task<Dictionary<string, ConnectionPoolHealth>> GetAllHealthAsync()
    {
        var health = new Dictionary<string, ConnectionPoolHealth>();
        
        foreach (var kvp in _pools)
        {
            var poolHealth = await kvp.Value.GetHealthAsync();
            health[kvp.Key] = poolHealth;
        }
        
        return await Task.FromResult(health);
    }
}
```

### Usage Examples
```csharp
// Basic connection pooling
public class UserService
{
    private readonly IConnectionPool _connectionPool;
    
    public UserService(IConnectionPool connectionPool)
    {
        _connectionPool = connectionPool;
    }
    
    public async Task<User> GetUserAsync(long id)
    {
        IDbConnection connection = null;
        try
        {
            connection = await _connectionPool.GetConnectionAsync();
            
            var sql = "SELECT * FROM users WHERE id = @id";
            var user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { id });
            
            return user;
        }
        finally
        {
            if (connection != null)
            {
                await _connectionPool.ReturnConnectionAsync(connection);
            }
        }
    }
    
    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        IDbConnection connection = null;
        try
        {
            connection = await _connectionPool.GetConnectionAsync();
            
            var sql = "SELECT * FROM users";
            var users = await connection.QueryAsync<User>(sql);
            
            return users;
        }
        finally
        {
            if (connection != null)
            {
                await _connectionPool.ReturnConnectionAsync(connection);
            }
        }
    }
}

// Advanced connection pooling with monitoring
public class AdvancedUserService
{
    private readonly IConnectionPool _connectionPool;
    private readonly IConnectionPoolManager _poolManager;
    
    public AdvancedUserService(IConnectionPool connectionPool, IConnectionPoolManager poolManager)
    {
        _connectionPool = connectionPool;
        _poolManager = poolManager;
    }
    
    public async Task<User> GetUserWithMonitoringAsync(long id)
    {
        IDbConnection connection = null;
        try
        {
            connection = await _connectionPool.GetConnectionAsync();
            
            var sql = "SELECT * FROM users WHERE id = @id";
            var user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { id });
            
            return user;
        }
        finally
        {
            if (connection != null)
            {
                await _connectionPool.ReturnConnectionAsync(connection);
            }
        }
    }
    
    public async Task<ConnectionPoolStats> GetPoolStatsAsync()
    {
        return await _connectionPool.GetStatsAsync();
    }
    
    public async Task<ConnectionPoolHealth> GetPoolHealthAsync()
    {
        return await _connectionPool.GetHealthAsync();
    }
    
    public async Task<Dictionary<string, ConnectionPoolStats>> GetAllPoolStatsAsync()
    {
        return await _poolManager.GetAllStatsAsync();
    }
}

// Connection pool configuration
public class ConnectionPoolService
{
    private readonly IConnectionPoolManager _poolManager;
    private readonly IConnectionValidator _validator;
    
    public ConnectionPoolService(IConnectionPoolManager poolManager, IConnectionValidator validator)
    {
        _poolManager = poolManager;
        _validator = validator;
    }
    
    public async Task<IConnectionPool> CreatePoolAsync(string connectionString)
    {
        var options = new ConnectionPoolOptions
        {
            MinPoolSize = 10,
            MaxPoolSize = 100,
            ConnectionLifetime = TimeSpan.FromMinutes(30),
            ConnectionIdleTimeout = TimeSpan.FromMinutes(10),
            ConnectionValidationTimeout = TimeSpan.FromSeconds(30),
            ValidateConnectionsOnBorrow = true,
            ValidateConnectionsOnReturn = false,
            ValidateConnectionsOnIdle = true,
            HealthCheckInterval = TimeSpan.FromMinutes(1),
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromSeconds(1),
            EnableStatistics = true,
            EnableHealthMonitoring = true
        };
        
        return await _poolManager.CreatePoolAsync(connectionString, options);
    }
}
```

## 🧪 Test Cases

### Connection Pool Tests
- [ ] Connection pool creation
- [ ] Connection borrowing
- [ ] Connection returning
- [ ] Pool statistics
- [ ] Pool health monitoring

### Connection Management Tests
- [ ] Connection lifecycle
- [ ] Connection state tracking
- [ ] Connection health monitoring
- [ ] Connection recovery

### Performance Tests
- [ ] Pool sizing
- [ ] Connection reuse
- [ ] Load balancing
- [ ] Memory management

### Integration Tests
- [ ] End-to-end connection pooling
- [ ] Multi-threaded access
- [ ] Error handling
- [ ] Performance testing

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic connection pooling
- [ ] Advanced connection pooling
- [ ] Performance optimization
- [ ] Best practices

### Connection Pooling Guide
- [ ] Connection pooling concepts
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
1. Move to Phase 4.1: Advanced Repository Generation Patterns
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on connection pooling design
- [ ] Performance considerations for connection pooling
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
