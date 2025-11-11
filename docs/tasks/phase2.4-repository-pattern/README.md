# Phase 2.4: Repository Pattern Implementation

## 📋 Task Overview

**Objective**: Implement a comprehensive repository pattern that provides a clean abstraction layer over data access while leveraging Dapper for performance.

**Priority**: High  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.3 (Relationship Mapping, Composite Key Support, Enhanced CPQL Query Language)  
**Assigned To**: [Developer Name]  
**Status**: [Completed] **COMPLETED** - October 10, 2024

## 🎯 Success Criteria

- [x] IRepository interface is complete [Completed]
- [x] BaseRepository class is implemented [Completed]
- [x] Custom repository support works [Completed]
- [x] Repository factory is implemented [Completed]
- [x] Unit tests cover all functionality [Completed] (14 tests passing)
- [x] Documentation is complete [Completed]

## 📝 Detailed Requirements

### 1. IRepository Interface
- **Purpose**: Defines the contract for repository operations
- **Methods**:
  - `Task<T> GetByIdAsync(TKey id)` - Get entity by ID
  - `Task<IEnumerable<T>> GetAllAsync()` - Get all entities
  - `Task<T> AddAsync(T entity)` - Add new entity
  - `Task UpdateAsync(T entity)` - Update existing entity
  - `Task DeleteAsync(TKey id)` - Delete entity by ID
  - `Task DeleteAsync(T entity)` - Delete entity
  - `Task<bool> ExistsAsync(TKey id)` - Check if entity exists
  - `Task<int> CountAsync()` - Count entities
  - `Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)` - Find entities by predicate

### 2. BaseRepository Class
- **Purpose**: Default implementation of repository operations
- **Dependencies**: IDbConnection, IEntityManager, IMetadataProvider
- **Features**:
  - CRUD operations
  - Query execution
  - Transaction support
  - Performance optimization
  - Error handling

### 3. Custom Repository Support
- **Interface Inheritance**: Custom repositories inherit from IRepository
- **Method Implementation**: Custom methods in repositories
- **Query Building**: Fluent query building
  - **Predicate Support**: LINQ-like predicate support
  - **Sorting**: Order by support
  - **Paging**: Skip and take support
  - **Projection**: Select specific properties

### 4. Repository Factory
- **Purpose**: Creates repository instances
- **Features**:
  - Dependency injection support
  - Repository registration
  - Lifecycle management
  - Configuration support

### 5. Advanced Features
- **Bulk Operations**: Bulk insert, update, delete
- **Batch Operations**: Batch processing
- **Caching**: Repository-level caching
- **Auditing**: Change tracking
- **Validation**: Entity validation

## 🏗️ Implementation Plan

### Step 1: Create Repository Interfaces
1. Create `IRepository<T, TKey>` interface
2. Create `IRepository<T>` interface (with default key type)
3. Create `IReadOnlyRepository<T, TKey>` interface
4. Create `IUnitOfWork` interface

### Step 2: Implement Base Repository
1. Create `BaseRepository<T, TKey>` class
2. Implement CRUD operations
3. Implement query operations
4. Add transaction support

### Step 3: Implement Custom Repository Support
1. Create `ICustomRepository` interface
2. Implement custom repository base class
3. Add query building support
4. Add predicate support

### Step 4: Implement Repository Factory
1. Create `IRepositoryFactory` interface
2. Create `RepositoryFactory` class
3. Add dependency injection support
4. Add configuration support

### Step 5: Add Advanced Features
1. Implement bulk operations
2. Implement batch operations
3. Add caching support
4. Add auditing support

### Step 6: Create Unit Tests
1. Test repository interfaces
2. Test base repository
3. Test custom repositories
4. Test repository factory

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Repository guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/Repositories/
├── IRepository.cs
├── IReadOnlyRepository.cs
├── BaseRepository.cs
├── ICustomRepository.cs
├── CustomRepositoryBase.cs
├── IUnitOfWork.cs
├── UnitOfWork.cs
├── IRepositoryFactory.cs
├── RepositoryFactory.cs
├── IQueryBuilder.cs
├── QueryBuilder.cs
└── BulkOperations/
    ├── IBulkOperations.cs
    ├── BulkOperations.cs
    └── BatchOperations.cs

tests/NPA.Core.Tests/Repositories/
├── BaseRepositoryTests.cs
├── CustomRepositoryTests.cs
├── RepositoryFactoryTests.cs
├── QueryBuilderTests.cs
├── UnitOfWorkTests.cs
└── BulkOperationsTests.cs
```

## 💻 Code Examples

### IRepository Interface
```csharp
public interface IRepository<T, TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(TKey id);
    Task DeleteAsync(T entity);
    Task<bool> ExistsAsync(TKey id);
    Task<int> CountAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool descending);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, int skip, int take);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, int skip, int take);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool descending, int skip, int take);
}

public interface IRepository<T> : IRepository<T, object> where T : class
{
}

public interface IReadOnlyRepository<T, TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<bool> ExistsAsync(TKey id);
    Task<int> CountAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate);
}
```

### BaseRepository Class
```csharp
public class BaseRepository<T, TKey> : IRepository<T, TKey> where T : class
{
    protected readonly IDbConnection _connection;
    protected readonly IEntityManager _entityManager;
    protected readonly IMetadataProvider _metadataProvider;
    protected readonly EntityMetadata _metadata;
    
    public BaseRepository(IDbConnection connection, IEntityManager entityManager, IMetadataProvider metadataProvider)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _metadata = _metadataProvider.GetEntityMetadata<T>();
    }
    
    public virtual async Task<T?> GetByIdAsync(TKey id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        return await _entityManager.FindAsync<T>(id);
    }
    
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        var sql = GenerateSelectAllSql();
        return await _connection.QueryAsync<T>(sql);
    }
    
    public virtual async Task<T> AddAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        await _entityManager.PersistAsync(entity);
        await _entityManager.FlushAsync();
        return entity;
    }
    
    public virtual async Task UpdateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        await _entityManager.MergeAsync(entity);
        await _entityManager.FlushAsync();
    }
    
    public virtual async Task DeleteAsync(TKey id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        await _entityManager.RemoveAsync<T>(id);
        await _entityManager.FlushAsync();
    }
    
    public virtual async Task DeleteAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        await _entityManager.RemoveAsync(entity);
        await _entityManager.FlushAsync();
    }
    
    public virtual async Task<bool> ExistsAsync(TKey id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        
        var entity = await GetByIdAsync(id);
        return entity != null;
    }
    
    public virtual async Task<int> CountAsync()
    {
        var sql = GenerateCountSql();
        return await _connection.QuerySingleAsync<int>(sql);
    }
    
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var queryBuilder = new QueryBuilder<T>(_connection, _metadata);
        var sql = queryBuilder.Where(predicate).Build();
        return await _connection.QueryAsync<T>(sql, queryBuilder.Parameters);
    }
    
    public virtual async Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var queryBuilder = new QueryBuilder<T>(_connection, _metadata);
        var sql = queryBuilder.Where(predicate).Build();
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, queryBuilder.Parameters);
    }
    
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        
        var queryBuilder = new QueryBuilder<T>(_connection, _metadata);
        var sql = queryBuilder.Where(predicate).OrderBy(orderBy).Build();
        return await _connection.QueryAsync<T>(sql, queryBuilder.Parameters);
    }
    
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool descending)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        
        var queryBuilder = new QueryBuilder<T>(_connection, _metadata);
        var sql = queryBuilder.Where(predicate).OrderBy(orderBy, descending).Build();
        return await _connection.QueryAsync<T>(sql, queryBuilder.Parameters);
    }
    
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, int skip, int take)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (skip < 0) throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take <= 0) throw new ArgumentException("Take must be positive", nameof(take));
        
        var queryBuilder = new QueryBuilder<T>(_connection, _metadata);
        var sql = queryBuilder.Where(predicate).Skip(skip).Take(take).Build();
        return await _connection.QueryAsync<T>(sql, queryBuilder.Parameters);
    }
    
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, int skip, int take)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        if (skip < 0) throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take <= 0) throw new ArgumentException("Take must be positive", nameof(take));
        
        var queryBuilder = new QueryBuilder<T>(_connection, _metadata);
        var sql = queryBuilder.Where(predicate).OrderBy(orderBy).Skip(skip).Take(take).Build();
        return await _connection.QueryAsync<T>(sql, queryBuilder.Parameters);
    }
    
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> orderBy, bool descending, int skip, int take)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (orderBy == null) throw new ArgumentNullException(nameof(orderBy));
        if (skip < 0) throw new ArgumentException("Skip cannot be negative", nameof(skip));
        if (take <= 0) throw new ArgumentException("Take must be positive", nameof(take));
        
        var queryBuilder = new QueryBuilder<T>(_connection, _metadata);
        var sql = queryBuilder.Where(predicate).OrderBy(orderBy, descending).Skip(skip).Take(take).Build();
        return await _connection.QueryAsync<T>(sql, queryBuilder.Parameters);
    }
    
    protected virtual string GenerateSelectAllSql()
    {
        var columns = string.Join(", ", _metadata.Properties.Select(p => p.ColumnName));
        return $"SELECT {columns} FROM {_metadata.TableName}";
    }
    
    protected virtual string GenerateCountSql()
    {
        return $"SELECT COUNT(*) FROM {_metadata.TableName}";
    }
}
```

### Custom Repository Base Class
```csharp
public abstract class CustomRepositoryBase<T, TKey> : BaseRepository<T, TKey>, ICustomRepository<T, TKey> where T : class
{
    protected CustomRepositoryBase(IDbConnection connection, IEntityManager entityManager, IMetadataProvider metadataProvider)
        : base(connection, entityManager, metadataProvider)
    {
    }
    
    protected IQueryBuilder<T> CreateQuery()
    {
        return new QueryBuilder<T>(_connection, _metadata);
    }
    
    protected async Task<IEnumerable<T>> ExecuteQueryAsync(IQueryBuilder<T> queryBuilder)
    {
        var sql = queryBuilder.Build();
        return await _connection.QueryAsync<T>(sql, queryBuilder.Parameters);
    }
    
    protected async Task<T?> ExecuteQuerySingleAsync(IQueryBuilder<T> queryBuilder)
    {
        var sql = queryBuilder.Build();
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, queryBuilder.Parameters);
    }
    
    protected async Task<int> ExecuteQueryCountAsync(IQueryBuilder<T> queryBuilder)
    {
        var countQuery = queryBuilder.ToCountQuery();
        var sql = countQuery.Build();
        return await _connection.QuerySingleAsync<int>(sql, countQuery.Parameters);
    }
}
```

### Query Builder
```csharp
public class QueryBuilder<T> : IQueryBuilder<T> where T : class
{
    private readonly IDbConnection _connection;
    private readonly EntityMetadata _metadata;
    private readonly List<string> _selectColumns;
    private readonly List<string> _joins;
    private readonly List<string> _whereConditions;
    private readonly List<string> _orderByColumns;
    private readonly Dictionary<string, object> _parameters;
    private int _skip;
    private int _take;
    private bool _distinct;
    
    public QueryBuilder(IDbConnection connection, EntityMetadata metadata)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _selectColumns = new List<string>();
        _joins = new List<string>();
        _whereConditions = new List<string>();
        _orderByColumns = new List<string>();
        _parameters = new Dictionary<string, object>();
        _skip = 0;
        _take = 0;
        _distinct = false;
    }
    
    public IQueryBuilder<T> Select(params Expression<Func<T, object>>[] columns)
    {
        if (columns == null || columns.Length == 0)
        {
            _selectColumns.Clear();
            _selectColumns.AddRange(_metadata.Properties.Select(p => p.ColumnName));
        }
        else
        {
            _selectColumns.Clear();
            foreach (var column in columns)
            {
                var columnName = GetColumnName(column);
                _selectColumns.Add(columnName);
            }
        }
        return this;
    }
    
    public IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        
        var condition = TranslateExpression(predicate);
        _whereConditions.Add(condition);
        return this;
    }
    
    public IQueryBuilder<T> OrderBy(Expression<Func<T, object>> column, bool descending = false)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        
        var columnName = GetColumnName(column);
        var direction = descending ? "DESC" : "ASC";
        _orderByColumns.Add($"{columnName} {direction}");
        return this;
    }
    
    public IQueryBuilder<T> Skip(int count)
    {
        if (count < 0) throw new ArgumentException("Skip count cannot be negative", nameof(count));
        _skip = count;
        return this;
    }
    
    public IQueryBuilder<T> Take(int count)
    {
        if (count <= 0) throw new ArgumentException("Take count must be positive", nameof(count));
        _take = count;
        return this;
    }
    
    public IQueryBuilder<T> Distinct()
    {
        _distinct = true;
        return this;
    }
    
    public string Build()
    {
        var sql = new StringBuilder();
        
        // SELECT clause
        var distinct = _distinct ? "DISTINCT " : "";
        var selectColumns = _selectColumns.Any() ? string.Join(", ", _selectColumns) : "*";
        sql.AppendLine($"SELECT {distinct}{selectColumns}");
        
        // FROM clause
        sql.AppendLine($"FROM {_metadata.TableName}");
        
        // JOIN clauses
        if (_joins.Any())
        {
            foreach (var join in _joins)
            {
                sql.AppendLine(join);
            }
        }
        
        // WHERE clause
        if (_whereConditions.Any())
        {
            sql.AppendLine($"WHERE {string.Join(" AND ", _whereConditions)}");
        }
        
        // ORDER BY clause
        if (_orderByColumns.Any())
        {
            sql.AppendLine($"ORDER BY {string.Join(", ", _orderByColumns)}");
        }
        
        // OFFSET/FETCH clause
        if (_skip > 0 || _take > 0)
        {
            sql.AppendLine($"OFFSET {_skip} ROWS");
            if (_take > 0)
            {
                sql.AppendLine($"FETCH NEXT {_take} ROWS ONLY");
            }
        }
        
        return sql.ToString();
    }
    
    public IQueryBuilder<T> ToCountQuery()
    {
        var countBuilder = new QueryBuilder<T>(_connection, _metadata);
        countBuilder._joins.AddRange(_joins);
        countBuilder._whereConditions.AddRange(_whereConditions);
        countBuilder._parameters = new Dictionary<string, object>(_parameters);
        countBuilder._selectColumns.Clear();
        countBuilder._selectColumns.Add("COUNT(*)");
        return countBuilder;
    }
    
    public Dictionary<string, object> Parameters => _parameters;
    
    private string GetColumnName(Expression<Func<T, object>> column)
    {
        if (column.Body is MemberExpression memberExpression)
        {
            var propertyName = memberExpression.Member.Name;
            var property = _metadata.Properties.FirstOrDefault(p => p.Name == propertyName);
            return property?.ColumnName ?? propertyName;
        }
        else if (column.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression)
        {
            var memberExpression = (MemberExpression)unaryExpression.Operand;
            var propertyName = memberExpression.Member.Name;
            var property = _metadata.Properties.FirstOrDefault(p => p.Name == propertyName);
            return property?.ColumnName ?? propertyName;
        }
        
        throw new ArgumentException("Invalid column expression", nameof(column));
    }
    
    private string TranslateExpression(Expression<Func<T, bool>> predicate)
    {
        // This is a simplified implementation
        // In a real implementation, you would need a full expression tree translator
        var visitor = new ExpressionTranslator(_metadata, _parameters);
        return visitor.Translate(predicate.Body);
    }
}
```

### Repository Factory
```csharp
public class RepositoryFactory : IRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _repositoryTypes;
    
    public RepositoryFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _repositoryTypes = new Dictionary<Type, Type>();
    }
    
    public void RegisterRepository<TEntity, TKey, TRepository>()
        where TEntity : class
        where TRepository : class, IRepository<TEntity, TKey>
    {
        var entityType = typeof(TEntity);
        _repositoryTypes[entityType] = typeof(TRepository);
    }
    
    public IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>()
        where TEntity : class
    {
        var entityType = typeof(TEntity);
        
        if (_repositoryTypes.TryGetValue(entityType, out var repositoryType))
        {
            return (IRepository<TEntity, TKey>)_serviceProvider.GetService(repositoryType);
        }
        
        // Create default repository
        var connection = _serviceProvider.GetRequiredService<IDbConnection>();
        var entityManager = _serviceProvider.GetRequiredService<IEntityManager>();
        var metadataProvider = _serviceProvider.GetRequiredService<IMetadataProvider>();
        
        return new BaseRepository<TEntity, TKey>(connection, entityManager, metadataProvider);
    }
    
    public IRepository<TEntity> CreateRepository<TEntity>()
        where TEntity : class
    {
        return CreateRepository<TEntity, object>();
    }
}
```

### Usage Examples
```csharp
// Basic repository usage
public class UserService
{
    private readonly IRepository<User, long> _userRepository;
    
    public UserService(IRepository<User, long> userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<User> GetUserAsync(long id)
    {
        return await _userRepository.GetByIdAsync(id);
    }
    
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _userRepository.FindAsync(u => u.IsActive);
    }
    
    public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
    {
        return await _userRepository.FindAsync(u => u.Role == role);
    }
    
    public async Task<User> CreateUserAsync(string username, string email)
    {
        var user = new User
        {
            Username = username,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        return await _userRepository.AddAsync(user);
    }
    
    public async Task UpdateUserAsync(User user)
    {
        await _userRepository.UpdateAsync(user);
    }
    
    public async Task DeleteUserAsync(long id)
    {
        await _userRepository.DeleteAsync(id);
    }
}

// Custom repository
public interface IUserRepository : IRepository<User, long>
{
    Task<IEnumerable<User>> FindByEmailDomainAsync(string domain);
    Task<IEnumerable<User>> FindRecentlyCreatedAsync(int days);
    Task<PagedResult<User>> GetUsersPagedAsync(int page, int pageSize);
}

public class UserRepository : CustomRepositoryBase<User, long>, IUserRepository
{
    public UserRepository(IDbConnection connection, IEntityManager entityManager, IMetadataProvider metadataProvider)
        : base(connection, entityManager, metadataProvider)
    {
    }
    
    public async Task<IEnumerable<User>> FindByEmailDomainAsync(string domain)
    {
        var query = CreateQuery()
            .Where(u => u.Email.Contains(domain));
        
        return await ExecuteQueryAsync(query);
    }
    
    public async Task<IEnumerable<User>> FindRecentlyCreatedAsync(int days)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var query = CreateQuery()
            .Where(u => u.CreatedAt >= cutoffDate)
            .OrderBy(u => u.CreatedAt, descending: true);
        
        return await ExecuteQueryAsync(query);
    }
    
    public async Task<PagedResult<User>> GetUsersPagedAsync(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var query = CreateQuery()
            .OrderBy(u => u.Username)
            .Skip(skip)
            .Take(pageSize);
        
        var users = await ExecuteQueryAsync(query);
        var totalCount = await ExecuteQueryCountAsync(CreateQuery());
        
        return new PagedResult<User>
        {
            Data = users,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

// Repository registration
services.AddScoped<IRepository<User, long>, UserRepository>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IRepositoryFactory, RepositoryFactory>();
```

## 🧪 Test Cases

### Repository Interface Tests
- [ ] GetByIdAsync
- [ ] GetAllAsync
- [ ] AddAsync
- [ ] UpdateAsync
- [ ] DeleteAsync
- [ ] ExistsAsync
- [ ] CountAsync
- [ ] FindAsync with predicate
- [ ] FindAsync with ordering
- [ ] FindAsync with paging

### Custom Repository Tests
- [ ] Custom method implementation
- [ ] Query building
- [ ] Complex queries
- [ ] Performance testing

### Repository Factory Tests
- [ ] Repository registration
- [ ] Repository creation
- [ ] Dependency injection
- [ ] Configuration support

### Integration Tests
- [ ] End-to-end repository operations
- [ ] Transaction support
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
- [ ] Basic repository usage
- [ ] Custom repository implementation
- [ ] Query building
- [ ] Performance considerations
- [ ] Best practices

### Repository Guide
- [ ] Repository patterns
- [ ] Custom repository patterns
- [ ] Query building patterns
- [ ] Performance optimization
- [ ] Common scenarios

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
1. Move to Phase 2.5: Additional Database Providers
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on repository design
- [ ] Performance considerations for repositories
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
