using System.Data;

namespace NPA.Core.Core;

/// <summary>
/// Represents a database transaction with support for both synchronous and asynchronous operations.
/// </summary>
/// <remarks>
/// ITransaction provides a unified interface for managing database transactions.
/// It supports:
/// - Commit and rollback operations (async and sync)
/// - Transaction lifecycle management
/// - Isolation level configuration
/// - Automatic resource cleanup via IDisposable/IAsyncDisposable
/// - Integration with EntityManager's flush mechanism
/// </remarks>
/// <example>
/// <code>
/// // Asynchronous usage
/// await using var transaction = await entityManager.BeginTransactionAsync();
/// try
/// {
///     await entityManager.PersistAsync(entity1);
///     await entityManager.PersistAsync(entity2);
///     await transaction.CommitAsync(); // Auto-flushes before commit
/// }
/// catch
/// {
///     await transaction.RollbackAsync();
///     throw;
/// }
/// 
/// // Synchronous usage
/// using var transaction = entityManager.BeginTransaction();
/// try
/// {
///     entityManager.Persist(entity1);
///     entityManager.Persist(entity2);
///     transaction.Commit(); // Auto-flushes before commit
/// }
/// catch
/// {
///     transaction.Rollback();
///     throw;
/// }
/// </code>
/// </example>
public interface ITransaction : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Commits the transaction asynchronously.
    /// </summary>
    /// <remarks>
    /// This method automatically calls Flush on the associated EntityManager
    /// to execute any queued operations before committing the transaction.
    /// All changes made within the transaction will be persisted to the database.
    /// </remarks>
    /// <returns>A task that represents the asynchronous commit operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the transaction has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the transaction is not active.</exception>
    /// <exception cref="TransactionException">Thrown if the commit operation fails.</exception>
    Task CommitAsync();

    /// <summary>
    /// Commits the transaction synchronously.
    /// </summary>
    /// <remarks>
    /// This method automatically calls Flush on the associated EntityManager
    /// to execute any queued operations before committing the transaction.
    /// All changes made within the transaction will be persisted to the database.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the transaction has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the transaction is not active.</exception>
    /// <exception cref="TransactionException">Thrown if the commit operation fails.</exception>
    void Commit();

    /// <summary>
    /// Rolls back the transaction asynchronously.
    /// </summary>
    /// <remarks>
    /// This method discards all changes made within the transaction and clears
    /// any queued operations in the EntityManager's change tracker.
    /// The database state will remain unchanged.
    /// </remarks>
    /// <returns>A task that represents the asynchronous rollback operation.</returns>
    /// <exception cref="TransactionException">Thrown if the rollback operation fails.</exception>
    Task RollbackAsync();

    /// <summary>
    /// Rolls back the transaction synchronously.
    /// </summary>
    /// <remarks>
    /// This method discards all changes made within the transaction and clears
    /// any queued operations in the EntityManager's change tracker.
    /// The database state will remain unchanged.
    /// </remarks>
    /// <exception cref="TransactionException">Thrown if the rollback operation fails.</exception>
    void Rollback();

    /// <summary>
    /// Gets a value indicating whether the transaction is active.
    /// </summary>
    /// <value>
    /// <c>true</c> if the transaction is active and can be committed or rolled back;
    /// otherwise, <c>false</c>.
    /// </value>
    bool IsActive { get; }

    /// <summary>
    /// Gets the isolation level of the transaction.
    /// </summary>
    /// <value>
    /// The <see cref="System.Data.IsolationLevel"/> that was specified when the transaction was created.
    /// </value>
    IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Gets the underlying database transaction.
    /// </summary>
    /// <value>
    /// The <see cref="System.Data.IDbTransaction"/> instance managed by this transaction.
    /// </value>
    /// <remarks>
    /// This property provides access to the underlying ADO.NET transaction for advanced scenarios.
    /// Use with caution as direct manipulation may interfere with the transaction's lifecycle.
    /// </remarks>
    IDbTransaction DbTransaction { get; }
}
