using System.Data;
using Dapper;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.MultiTenancy;

namespace NPA.Core.Repositories;

/// <summary>
/// Base class for custom repository implementations with helper methods for building custom queries.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class CustomRepositoryBase<T, TKey> : BaseRepository<T, TKey> where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomRepositoryBase{T, TKey}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="entityManager">The entity manager.</param>
    /// <param name="metadataProvider">The metadata provider.</param>
    /// <param name="tenantProvider">The tenant provider (optional).</param>
    protected CustomRepositoryBase(
        IDbConnection connection, 
        IEntityManager entityManager, 
        IMetadataProvider metadataProvider,
        ITenantProvider? tenantProvider = null)
        : base(connection, entityManager, metadataProvider, tenantProvider)
    {
    }
    
    /// <summary>
    /// Executes a custom SQL query and returns entities.
    /// </summary>
    /// <param name="sql">The SQL query.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <returns>A collection of entities.</returns>
    protected async Task<IEnumerable<T>> ExecuteQueryAsync(string sql, object? parameters = null)
    {
        return await _connection.QueryAsync<T>(sql, parameters);
    }
    
    /// <summary>
    /// Executes a custom SQL query and returns a single entity.
    /// </summary>
    /// <param name="sql">The SQL query.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <returns>A single entity if found; otherwise, null.</returns>
    protected async Task<T?> ExecuteQuerySingleAsync(string sql, object? parameters = null)
    {
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }
    
    /// <summary>
    /// Executes a custom SQL command and returns the number of affected rows.
    /// </summary>
    /// <param name="sql">The SQL command.</param>
    /// <param name="parameters">The command parameters.</param>
    /// <returns>The number of affected rows.</returns>
    protected async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        return await _connection.ExecuteAsync(sql, parameters);
    }
    
    /// <summary>
    /// Executes a custom SQL scalar query.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="sql">The SQL query.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <returns>The scalar result.</returns>
    protected async Task<TResult> ExecuteScalarAsync<TResult>(string sql, object? parameters = null)
    {
        return await _connection.QuerySingleAsync<TResult>(sql, parameters);
    }
}

/// <summary>
/// Base class for custom repository implementations with helper methods for building custom queries.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class CustomRepositoryBase<T> : CustomRepositoryBase<T, object> where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomRepositoryBase{T}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="entityManager">The entity manager.</param>
    /// <param name="metadataProvider">The metadata provider.</param>
    /// <param name="tenantProvider">The tenant provider (optional).</param>
    protected CustomRepositoryBase(
        IDbConnection connection, 
        IEntityManager entityManager, 
        IMetadataProvider metadataProvider,
        ITenantProvider? tenantProvider = null)
        : base(connection, entityManager, metadataProvider, tenantProvider)
    {
    }
}

