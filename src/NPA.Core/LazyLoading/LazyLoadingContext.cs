using System.Data;
using NPA.Core.Core;
using NPA.Core.Metadata;

namespace NPA.Core.LazyLoading;

/// <summary>
/// Implementation of lazy loading context.
/// Provides access to database connection, transaction, entity manager, and metadata provider.
/// </summary>
public class LazyLoadingContext : ILazyLoadingContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LazyLoadingContext"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="entityManager">The entity manager.</param>
    /// <param name="metadataProvider">The metadata provider.</param>
    /// <param name="transaction">The current transaction (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when connection, entityManager, or metadataProvider is null.</exception>
    public LazyLoadingContext(
        IDbConnection connection,
        IEntityManager entityManager,
        IMetadataProvider metadataProvider,
        IDbTransaction? transaction = null)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        EntityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        MetadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        Transaction = transaction;
    }

    /// <inheritdoc />
    public IDbConnection Connection { get; }

    /// <inheritdoc />
    public IDbTransaction? Transaction { get; }

    /// <inheritdoc />
    public IEntityManager EntityManager { get; }

    /// <inheritdoc />
    public IMetadataProvider MetadataProvider { get; }
}
