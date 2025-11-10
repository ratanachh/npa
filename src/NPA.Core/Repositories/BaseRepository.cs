using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Dapper;
using NPA.Core.Annotations;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.MultiTenancy;

namespace NPA.Core.Repositories;

/// <summary>
/// Base implementation of repository pattern for entity operations.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public class BaseRepository<T, TKey> : IRepository<T, TKey> where T : class
{
    /// <summary>
    /// The database connection.
    /// </summary>
    protected readonly IDbConnection _connection;
    
    /// <summary>
    /// The entity manager.
    /// </summary>
    protected readonly IEntityManager _entityManager;
    
    /// <summary>
    /// The metadata provider.
    /// </summary>
    protected readonly IMetadataProvider _metadataProvider;
    
    /// <summary>
    /// The tenant provider (optional, for multi-tenant support).
    /// </summary>
    protected readonly ITenantProvider? _tenantProvider;
    
    /// <summary>
    /// The entity metadata.
    /// </summary>
    protected readonly EntityMetadata _metadata;
    
    /// <summary>
    /// The multi-tenant attribute if entity is multi-tenant.
    /// </summary>
    protected readonly MultiTenantAttribute? _multiTenantAttribute;
    
    /// <summary>
    /// Flag to temporarily bypass tenant filtering.
    /// </summary>
    private bool _bypassTenantFilter = false;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRepository{T, TKey}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="entityManager">The entity manager.</param>
    /// <param name="metadataProvider">The metadata provider.</param>
    /// <param name="tenantProvider">The tenant provider (optional).</param>
    public BaseRepository(
        IDbConnection connection, 
        IEntityManager entityManager, 
        IMetadataProvider metadataProvider,
        ITenantProvider? tenantProvider = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _tenantProvider = tenantProvider;
        _metadata = _metadataProvider.GetEntityMetadata<T>();
        _multiTenantAttribute = typeof(T).GetCustomAttribute<MultiTenantAttribute>();
    }
    
    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(TKey id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        return await _entityManager.FindAsync<T>(id);
    }
    
    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        var sql = GenerateSelectAllSql();
        sql = ApplyTenantFilter(sql);
        
        var tenantParams = GetTenantFilterParameters();
        return await _connection.QueryAsync<T>(sql, tenantParams);
    }
    
    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        await _entityManager.PersistAsync(entity);
        return entity;
    }
    
    /// <inheritdoc />
    public virtual async Task UpdateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        await _entityManager.MergeAsync(entity);
    }
    
    /// <inheritdoc />
    public virtual async Task DeleteAsync(TKey id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        await _entityManager.RemoveAsync<T>(id);
    }
    
    /// <inheritdoc />
    public virtual async Task DeleteAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        await _entityManager.RemoveAsync(entity);
    }
    
    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(TKey id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        var entity = await GetByIdAsync(id);
        return entity != null;
    }
    
    /// <inheritdoc />
    public virtual async Task<int> CountAsync()
    {
        var sql = GenerateCountSql();
        sql = ApplyTenantFilter(sql);
        
        var tenantParams = GetTenantFilterParameters();
        return await _connection.QuerySingleAsync<int>(sql, tenantParams);
    }
    
    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var (sql, parameters) = BuildQuery(predicate);
        sql = ApplyTenantFilter(sql);
        
        var mergedParams = MergeParameters(parameters, GetTenantFilterParameters());
        return await _connection.QueryAsync<T>(sql, mergedParams);
    }
    
    /// <inheritdoc />
    public virtual async Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var (sql, parameters) = BuildQuery(predicate);
        sql = ApplyTenantFilter(sql);
        
        var mergedParams = MergeParameters(parameters, GetTenantFilterParameters());
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, mergedParams);
    }
    
    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool descending = false)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        
        var (sql, parameters) = BuildQuery(predicate, orderBy, descending);
        sql = ApplyTenantFilter(sql);
        
        var mergedParams = MergeParameters(parameters, GetTenantFilterParameters());
        return await _connection.QueryAsync<T>(sql, mergedParams);
    }
    
    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, int skip, int take)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (skip < 0) throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take <= 0) throw new ArgumentException("Take must be positive", nameof(take));
        
        var (sql, parameters) = BuildQuery(predicate, null, false, skip, take);
        sql = ApplyTenantFilter(sql);
        
        var mergedParams = MergeParameters(parameters, GetTenantFilterParameters());
        return await _connection.QueryAsync<T>(sql, mergedParams);
    }
    
    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool descending, int skip, int take)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        if (skip < 0) throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take <= 0) throw new ArgumentException("Take must be positive", nameof(take));
        
        var (sql, parameters) = BuildQuery(predicate, orderBy, descending, skip, take);
        sql = ApplyTenantFilter(sql);
        
        var mergedParams = MergeParameters(parameters, GetTenantFilterParameters());
        return await _connection.QueryAsync<T>(sql, mergedParams);
    }
    
    /// <summary>
    /// Generates SQL for selecting all entities.
    /// </summary>
    /// <returns>The SQL string.</returns>
    protected virtual string GenerateSelectAllSql()
    {
        var columns = string.Join(", ", _metadata.Properties.Values.Select(p => p.ColumnName));
        return $"SELECT {columns} FROM {_metadata.FullTableName}";
    }
    
    /// <summary>
    /// Generates SQL for counting entities.
    /// </summary>
    /// <returns>The SQL string.</returns>
    protected virtual string GenerateCountSql()
    {
        return $"SELECT COUNT(*) FROM {_metadata.FullTableName}";
    }
    
    /// <summary>
    /// Builds a query from a predicate expression.
    /// </summary>
    /// <param name="predicate">The predicate expression.</param>
    /// <param name="orderBy">Optional order by expression.</param>
    /// <param name="descending">Whether to order descending.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <returns>A tuple containing the SQL string and parameters.</returns>
    protected virtual (string sql, object parameters) BuildQuery(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false,
        int skip = 0,
        int take = 0)
    {
        var sql = new StringBuilder();
        var paramDict = new Dictionary<string, object>();
        
        // SELECT clause
        var columns = string.Join(", ", _metadata.Properties.Values.Select(p => p.ColumnName));
        sql.AppendLine($"SELECT {columns}");
        sql.AppendLine($"FROM {_metadata.FullTableName}");
        
        // WHERE clause
        var translator = new ExpressionTranslator(_metadata, paramDict);
        var whereClause = translator.Translate(predicate.Body);
        sql.AppendLine($"WHERE {whereClause}");
        
        // ORDER BY clause
        if (orderBy != null)
        {
            var orderByColumn = GetColumnName(orderBy);
            var direction = descending ? "DESC" : "ASC";
            sql.AppendLine($"ORDER BY {orderByColumn} {direction}");
        }
        
        // Paging (OFFSET/FETCH or LIMIT/OFFSET depending on database)
        if (skip > 0 || take > 0)
        {
            // SQL Server style (also works with PostgreSQL 8.4+)
            if (orderBy == null)
            {
                // Need ORDER BY for OFFSET/FETCH
                var firstColumn = _metadata.Properties.Values.First().ColumnName;
                sql.AppendLine($"ORDER BY {firstColumn}");
            }
            sql.AppendLine($"OFFSET {skip} ROWS");
            if (take > 0)
            {
                sql.AppendLine($"FETCH NEXT {take} ROWS ONLY");
            }
        }
        
        return (sql.ToString(), paramDict);
    }
    
    /// <summary>
    /// Gets the column name from an expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The column name.</returns>
    protected virtual string GetColumnName(Expression<Func<T, object>> expression)
    {
        var memberExpression = expression.Body as MemberExpression;
        
        // Handle Convert expressions (for value types being boxed to object)
        if (memberExpression == null && expression.Body is UnaryExpression unary && unary.Operand is MemberExpression)
        {
            memberExpression = (MemberExpression)unary.Operand;
        }
        
        if (memberExpression == null)
            throw new ArgumentException("Invalid column expression", nameof(expression));
        
        var propertyName = memberExpression.Member.Name;
        var property = _metadata.Properties.Values.FirstOrDefault(p => p.PropertyName == propertyName);
        
        if (property == null)
            throw new InvalidOperationException($"Property {propertyName} not found in entity metadata");
        
        return property.ColumnName;
    }
    
    /// <summary>
    /// Executes a repository operation without tenant filtering.
    /// Use this for administrative operations that need to query across tenants.
    /// </summary>
    /// <param name="action">The action to execute without tenant filtering.</param>
    /// <returns>The result of the action.</returns>
    /// <exception cref="InvalidOperationException">Thrown when cross-tenant queries are not allowed.</exception>
    public virtual async Task<TResult> WithoutTenantFilterAsync<TResult>(Func<Task<TResult>> action)
    {
        if (_multiTenantAttribute != null && !_multiTenantAttribute.AllowCrossTenantQueries)
        {
            throw new InvalidOperationException(
                $"Cross-tenant queries are not allowed for entity type {typeof(T).Name}. " +
                "Set AllowCrossTenantQueries = true on the [MultiTenant] attribute to enable.");
        }
        
        _bypassTenantFilter = true;
        try
        {
            return await action();
        }
        finally
        {
            _bypassTenantFilter = false;
        }
    }
    
    /// <summary>
    /// Gets the current tenant ID if multi-tenancy is enabled.
    /// </summary>
    /// <returns>The current tenant ID or null.</returns>
    protected virtual string? GetCurrentTenantId()
    {
        return _tenantProvider?.GetCurrentTenantId();
    }
    
    /// <summary>
    /// Checks if tenant filtering should be applied.
    /// </summary>
    /// <returns>True if tenant filtering should be applied.</returns>
    protected virtual bool ShouldApplyTenantFilter()
    {
        return _multiTenantAttribute != null
            && _multiTenantAttribute.EnforceTenantIsolation
            && !_bypassTenantFilter
            && _tenantProvider != null
            && GetCurrentTenantId() != null;
    }
    
    /// <summary>
    /// Applies tenant filter to SQL query.
    /// </summary>
    /// <param name="sql">The base SQL query.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <returns>The modified SQL with tenant filter.</returns>
    protected virtual string ApplyTenantFilter(string sql, object? parameters = null)
    {
        if (!ShouldApplyTenantFilter() || _multiTenantAttribute == null)
            return sql;
        
        var tenantId = GetCurrentTenantId();
        var tenantColumn = _multiTenantAttribute.TenantIdProperty;
        
        // Check if WHERE clause already exists
        if (sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            sql += $" AND {tenantColumn} = @__TenantId";
        }
        else
        {
            sql += $" WHERE {tenantColumn} = @__TenantId";
        }
        
        return sql;
    }
    
    /// <summary>
    /// Gets tenant filter parameters to merge with existing parameters.
    /// </summary>
    /// <returns>Tenant filter parameters.</returns>
    protected virtual object? GetTenantFilterParameters()
    {
        if (!ShouldApplyTenantFilter())
            return null;
        
        return new { __TenantId = GetCurrentTenantId() };
    }
    
    /// <summary>
    /// Merges two parameter objects into a single DynamicParameters object.
    /// </summary>
    /// <param name="parameters1">First parameters object.</param>
    /// <param name="parameters2">Second parameters object.</param>
    /// <returns>Merged parameters.</returns>
    protected virtual object? MergeParameters(object? parameters1, object? parameters2)
    {
        if (parameters1 == null && parameters2 == null)
            return null;
        if (parameters1 == null)
            return parameters2;
        if (parameters2 == null)
            return parameters1;
        
        var dynamicParams = new DynamicParameters(parameters1);
        dynamicParams.AddDynamicParams(parameters2);
        return dynamicParams;
    }
}

/// <summary>
/// Base implementation of repository pattern for entities with object key type.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class BaseRepository<T> : BaseRepository<T, object> where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRepository{T}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="entityManager">The entity manager.</param>
    /// <param name="metadataProvider">The metadata provider.</param>
    /// <param name="tenantProvider">The tenant provider (optional).</param>
    public BaseRepository(
        IDbConnection connection, 
        IEntityManager entityManager, 
        IMetadataProvider metadataProvider,
        ITenantProvider? tenantProvider = null)
        : base(connection, entityManager, metadataProvider, tenantProvider)
    {
    }
}

