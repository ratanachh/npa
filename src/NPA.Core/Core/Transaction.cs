using System.Data;

namespace NPA.Core.Core;

/// <summary>
/// Represents a database transaction that integrates with the EntityManager's flush mechanism.
/// </summary>
/// <remarks>
/// This transaction implementation provides:
/// - Automatic flush before commit to execute queued operations
/// - Change tracker clearing on rollback
/// - Support for both async and sync operations
/// - Proper resource cleanup via IDisposable/IAsyncDisposable
/// - Transaction state management
/// </remarks>
public class Transaction : ITransaction
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _dbTransaction;
    private readonly IEntityManager _entityManager;
    private bool _disposed = false;
    private bool _committed = false;
    private bool _rolledBack = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="entityManager">The entity manager.</param>
    /// <param name="isolationLevel">The transaction isolation level.</param>
    /// <exception cref="ArgumentNullException">Thrown when connection or entityManager is null.</exception>
    public Transaction(
        IDbConnection connection,
        IEntityManager entityManager,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        
        // Ensure connection is open
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }
        
        _dbTransaction = _connection.BeginTransaction(isolationLevel);
        IsolationLevel = isolationLevel;
    }

    /// <inheritdoc />
    public async Task CommitAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Transaction));

        if (!IsActive)
            throw new InvalidOperationException("Transaction is not active");

        if (_committed)
            throw new InvalidOperationException("Transaction has already been committed");

        if (_rolledBack)
            throw new InvalidOperationException("Transaction has already been rolled back");

        try
        {
            // IMPORTANT: Auto-flush before commit to execute queued operations
            await _entityManager.FlushAsync();

            _dbTransaction.Commit();
            _committed = true;
        }
        catch (Exception ex)
        {
            throw new TransactionException("Failed to commit transaction", ex);
        }
    }

    /// <inheritdoc />
    public void Commit()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Transaction));

        if (!IsActive)
            throw new InvalidOperationException("Transaction is not active");

        if (_committed)
            throw new InvalidOperationException("Transaction has already been committed");

        if (_rolledBack)
            throw new InvalidOperationException("Transaction has already been rolled back");

        try
        {
            // IMPORTANT: Auto-flush before commit to execute queued operations
            _entityManager.Flush();

            _dbTransaction.Commit();
            _committed = true;
        }
        catch (Exception ex)
        {
            throw new TransactionException("Failed to commit transaction", ex);
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync()
    {
        if (_disposed)
            return;

        if (_rolledBack || _committed)
            return;

        try
        {
            // Clear queued operations on rollback
            await _entityManager.ClearAsync();
            
            _dbTransaction.Rollback();
            _rolledBack = true;
        }
        catch (Exception ex)
        {
            throw new TransactionException("Failed to rollback transaction", ex);
        }
    }

    /// <inheritdoc />
    public void Rollback()
    {
        if (_disposed)
            return;

        if (_rolledBack || _committed)
            return;

        try
        {
            // Clear queued operations on rollback
            _entityManager.Clear();
            
            _dbTransaction.Rollback();
            _rolledBack = true;
        }
        catch (Exception ex)
        {
            throw new TransactionException("Failed to rollback transaction", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                // Auto-rollback if not committed
                if (IsActive && !_committed && !_rolledBack)
                {
                    await RollbackAsync();
                }
            }
            finally
            {
                _dbTransaction?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                // Auto-rollback if not committed
                if (IsActive && !_committed && !_rolledBack)
                {
                    Rollback();
                }
            }
            finally
            {
                _dbTransaction?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <inheritdoc />
    public bool IsActive => !_disposed && _dbTransaction != null && !_committed && !_rolledBack;

    /// <inheritdoc />
    public IsolationLevel IsolationLevel { get; }

    /// <inheritdoc />
    public IDbTransaction DbTransaction => _dbTransaction;
}
