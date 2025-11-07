using System.Data;
using NPA.Core.Core;
using NPA.Core.Metadata;

namespace NPA.Core.LazyLoading;

/// <summary>
/// Defines the contract for lazy loading context.
/// Provides access to the database connection, transaction, and entity manager.
/// </summary>
public interface ILazyLoadingContext
{
    /// <summary>
    /// Gets the database connection.
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// Gets the current transaction, if any.
    /// </summary>
    IDbTransaction? Transaction { get; }

    /// <summary>
    /// Gets the entity manager.
    /// </summary>
    IEntityManager EntityManager { get; }

    /// <summary>
    /// Gets the metadata provider.
    /// </summary>
    IMetadataProvider MetadataProvider { get; }
}
