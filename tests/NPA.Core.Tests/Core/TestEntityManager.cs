using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Metadata;

namespace NPA.Core.Tests.Core;

/// <summary>
/// Test-specific EntityManager that doesn't use Dapper async operations.
/// </summary>
public class TestEntityManager : IEntityManager
{
    private readonly MockDbConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IChangeTracker _changeTracker;
    private readonly ILogger<TestEntityManager>? _logger;
    private bool _disposed;

    public TestEntityManager(MockDbConnection connection, IMetadataProvider metadataProvider, ILogger<TestEntityManager>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _logger = logger;
        _changeTracker = new ChangeTracker();
    }

    public IMetadataProvider MetadataProvider => _metadataProvider;
    public IChangeTracker ChangeTracker => _changeTracker;

    public async Task PersistAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _logger?.LogDebug("Persisting entity of type {EntityType}", typeof(T).Name);

        var metadata = _metadataProvider.GetEntityMetadata<T>();
        
        // Execute command on mock connection
        var command = _connection.CreateCommand();
        command.CommandText = "INSERT INTO " + metadata.TableName + " (id, username, email, created_at, is_active) VALUES (@id, @username, @email, @created_at, @is_active)";
        _connection.AddExecutedCommand((MockCommand)command);

        // Mock successful persistence
        _changeTracker.Track(entity, EntityState.Added);
        
        // Simulate setting generated ID
        if (metadata.PrimaryKeyProperty != null)
        {
            var propertyMetadata = metadata.Properties[metadata.PrimaryKeyProperty];
            var property = typeof(T).GetProperty(propertyMetadata.PropertyName);
            if (property != null)
            {
                var currentValue = property.GetValue(entity);
                if (currentValue == null || currentValue.Equals(Activator.CreateInstance(property.PropertyType)))
                {
                    property.SetValue(entity, 123L);
                }
            }
        }

        _logger?.LogDebug("Successfully persisted entity of type {EntityType}", typeof(T).Name);
    }

    public async Task<T?> FindAsync<T>(object id) where T : class
    {
        ThrowIfDisposed();
        if (id == null) throw new ArgumentNullException(nameof(id));

        _logger?.LogDebug("Finding entity of type {EntityType} with ID {Id}", typeof(T).Name, id);

        // Execute command on mock connection
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM " + _metadataProvider.GetEntityMetadata<T>().TableName + " WHERE id = @id";
        _connection.AddExecutedCommand((MockCommand)command);

        // Mock finding entity - return null for testing
        return null;
    }

    public async Task<T?> FindAsync<T>(CompositeKey key) where T : class
    {
        ThrowIfDisposed();
        if (key == null) throw new ArgumentNullException(nameof(key));

        _logger?.LogDebug("Finding entity of type {EntityType} with composite key", typeof(T).Name);

        // Execute command on mock connection
        var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM " + _metadataProvider.GetEntityMetadata<T>().TableName + " WHERE OrderId = @OrderId AND ProductId = @ProductId";
        _connection.AddExecutedCommand((MockCommand)command);

        // Mock finding entity - return null for testing
        return null;
    }

    public async Task MergeAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _logger?.LogDebug("Merging entity of type {EntityType}", typeof(T).Name);

        // Execute command on mock connection
        var command = _connection.CreateCommand();
        command.CommandText = "UPDATE " + _metadataProvider.GetEntityMetadata<T>().TableName + " SET username = @username, email = @email WHERE id = @id";
        _connection.AddExecutedCommand((MockCommand)command);

        _changeTracker.Track(entity, EntityState.Modified);

        _logger?.LogDebug("Successfully merged entity of type {EntityType}", typeof(T).Name);
    }

    public async Task RemoveAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _logger?.LogDebug("Removing entity of type {EntityType}", typeof(T).Name);

        // Execute command on mock connection
        var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM " + _metadataProvider.GetEntityMetadata<T>().TableName + " WHERE id = @id";
        _connection.AddExecutedCommand((MockCommand)command);

        _changeTracker.Track(entity, EntityState.Deleted);

        _logger?.LogDebug("Successfully removed entity of type {EntityType}", typeof(T).Name);
    }

    public async Task RemoveAsync<T>(object id) where T : class
    {
        ThrowIfDisposed();
        if (id == null) throw new ArgumentNullException(nameof(id));

        _logger?.LogDebug("Removing entity of type {EntityType} with ID {Id}", typeof(T).Name, id);

        // Execute command on mock connection
        var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM " + _metadataProvider.GetEntityMetadata<T>().TableName + " WHERE id = @id";
        _connection.AddExecutedCommand((MockCommand)command);

        // Mock removal - just track as deleted
        _logger?.LogDebug("Successfully removed entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
    }

    public async Task FlushAsync()
    {
        ThrowIfDisposed();

        _logger?.LogDebug("Flushing pending changes");

        // Execute commands for pending changes
        var pendingChanges = _changeTracker.GetPendingChanges();
        foreach (var (entity, state) in pendingChanges)
        {
            var command = _connection.CreateCommand();
            switch (state)
            {
                case EntityState.Added:
                    command.CommandText = "INSERT INTO users (id, username, email) VALUES (@id, @username, @email)";
                    break;
                case EntityState.Modified:
                    command.CommandText = "UPDATE users SET username = @username, email = @email WHERE id = @id";
                    break;
                case EntityState.Deleted:
                    command.CommandText = "DELETE FROM users WHERE id = @id";
                    break;
            }
            _connection.AddExecutedCommand((MockCommand)command);
        }

        // Mock flush - clear all tracked entities
        _changeTracker.Clear();

        _logger?.LogDebug("Successfully flushed pending changes");
    }

    public async Task ClearAsync()
    {
        ThrowIfDisposed();

        _logger?.LogDebug("Clearing persistence context");

        _changeTracker.Clear();

        _logger?.LogDebug("Successfully cleared persistence context");
    }

    public bool Contains<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) return false;

        var state = _changeTracker.GetState(entity);
        return state != null && state != EntityState.Detached;
    }

    public void Detach<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        if (entity == null) return;

        _changeTracker.Untrack(entity);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _changeTracker.Clear();
            _connection?.Dispose();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TestEntityManager));
        }
    }
}
