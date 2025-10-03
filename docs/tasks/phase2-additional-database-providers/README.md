# Phase 2.5: Additional Database Providers

## 📋 Task Overview

**Objective**: Implement support for additional database providers (PostgreSQL, MySQL, SQLite) to make NPA database-agnostic while maintaining Dapper's performance benefits.

**Priority**: Medium  
**Estimated Time**: 4-5 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.4 (All previous Phase 2 tasks)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] PostgreSQL provider is implemented
- [ ] MySQL provider is implemented
- [ ] SQLite provider is implemented
- [ ] Provider abstraction is complete
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. Database Provider Abstraction
- **IDatabaseProvider Interface**: Common interface for all database providers
- **Provider Registration**: Register providers with dependency injection
- **Provider Selection**: Select provider based on configuration
- **Provider Factory**: Factory for creating provider instances

### 2. PostgreSQL Provider
- **Connection Management**: PostgreSQL connection handling
- **SQL Generation**: PostgreSQL-specific SQL generation
- **Data Type Mapping**: PostgreSQL data type mapping
- **Feature Support**: PostgreSQL-specific features
- **Performance Optimization**: PostgreSQL-specific optimizations

### 3. MySQL Provider
- **Connection Management**: MySQL connection handling
- **SQL Generation**: MySQL-specific SQL generation
- **Data Type Mapping**: MySQL data type mapping
- **Feature Support**: MySQL-specific features
- **Performance Optimization**: MySQL-specific optimizations

### 4. SQLite Provider
- **Connection Management**: SQLite connection handling
- **SQL Generation**: SQLite-specific SQL generation
- **Data Type Mapping**: SQLite data type mapping
- **Feature Support**: SQLite-specific features
- **Performance Optimization**: SQLite-specific optimizations

### 5. Provider Features
- **Connection String Parsing**: Parse provider-specific connection strings
- **SQL Dialect Support**: Support for different SQL dialects
- **Data Type Support**: Support for provider-specific data types
- **Feature Detection**: Detect provider capabilities
- **Error Handling**: Provider-specific error handling

## 🏗️ Implementation Plan

### Step 1: Create Provider Abstraction
1. Create `IDatabaseProvider` interface
2. Create `DatabaseProvider` enum
3. Create `IDatabaseProviderFactory` interface
4. Create `DatabaseProviderFactory` class

### Step 2: Implement PostgreSQL Provider
1. Create `PostgreSQLProvider` class
2. Implement connection management
3. Implement SQL generation
4. Implement data type mapping

### Step 3: Implement MySQL Provider
1. Create `MySQLProvider` class
2. Implement connection management
3. Implement SQL generation
4. Implement data type mapping

### Step 4: Implement SQLite Provider
1. Create `SQLiteProvider` class
2. Implement connection management
3. Implement SQL generation
4. Implement data type mapping

### Step 5: Add Provider Features
1. Implement connection string parsing
2. Implement SQL dialect support
3. Implement data type support
4. Implement feature detection

### Step 6: Create Unit Tests
1. Test provider abstraction
2. Test PostgreSQL provider
3. Test MySQL provider
4. Test SQLite provider

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Provider configuration guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/Providers/
├── IDatabaseProvider.cs
├── DatabaseProvider.cs
├── IDatabaseProviderFactory.cs
├── DatabaseProviderFactory.cs
├── PostgreSQL/
│   ├── PostgreSQLProvider.cs
│   ├── PostgreSQLConnectionManager.cs
│   ├── PostgreSQLSqlGenerator.cs
│   ├── PostgreSQLTypeMapper.cs
│   └── PostgreSQLFeatureDetector.cs
├── MySQL/
│   ├── MySQLProvider.cs
│   ├── MySQLConnectionManager.cs
│   ├── MySQLSqlGenerator.cs
│   ├── MySQLTypeMapper.cs
│   └── MySQLFeatureDetector.cs
└── SQLite/
    ├── SQLiteProvider.cs
    ├── SQLiteConnectionManager.cs
    ├── SQLiteSqlGenerator.cs
    ├── SQLiteTypeMapper.cs
    └── SQLiteFeatureDetector.cs

tests/NPA.Core.Tests/Providers/
├── DatabaseProviderFactoryTests.cs
├── PostgreSQL/
│   ├── PostgreSQLProviderTests.cs
│   ├── PostgreSQLConnectionManagerTests.cs
│   ├── PostgreSQLSqlGeneratorTests.cs
│   └── PostgreSQLTypeMapperTests.cs
├── MySQL/
│   ├── MySQLProviderTests.cs
│   ├── MySQLConnectionManagerTests.cs
│   ├── MySQLSqlGeneratorTests.cs
│   └── MySQLTypeMapperTests.cs
└── SQLite/
    ├── SQLiteProviderTests.cs
    ├── SQLiteConnectionManagerTests.cs
    ├── SQLiteSqlGeneratorTests.cs
    └── SQLiteTypeMapperTests.cs
```

## 💻 Code Examples

### Database Provider Interface
```csharp
public interface IDatabaseProvider
{
    DatabaseProvider Type { get; }
    string Name { get; }
    string Version { get; }
    
    Task<IDbConnection> CreateConnectionAsync(string connectionString);
    Task<bool> TestConnectionAsync(string connectionString);
    Task<DatabaseInfo> GetDatabaseInfoAsync(IDbConnection connection);
    
    string GenerateSelectSql(EntityMetadata metadata, string whereClause = null, string orderBy = null, int? skip = null, int? take = null);
    string GenerateInsertSql(EntityMetadata metadata);
    string GenerateUpdateSql(EntityMetadata metadata, string whereClause);
    string GenerateDeleteSql(EntityMetadata metadata, string whereClause);
    string GenerateCountSql(EntityMetadata metadata, string whereClause = null);
    
    string GetColumnName(string propertyName, PropertyMetadata property);
    string GetTableName(string entityName, EntityMetadata metadata);
    string GetParameterName(string parameterName, int index = 0);
    
    string MapToDatabaseType(Type clrType, PropertyMetadata property);
    Type MapToClrType(string databaseType, PropertyMetadata property);
    
    bool SupportsFeature(DatabaseFeature feature);
    string GetFeatureSql(DatabaseFeature feature, params object[] parameters);
}

public enum DatabaseProvider
{
    SqlServer,
    PostgreSQL,
    MySQL,
    SQLite
}

public enum DatabaseFeature
{
    IdentityColumns,
    Sequences,
    Schemas,
    Indexes,
    ForeignKeys,
    CheckConstraints,
    Triggers,
    StoredProcedures,
    Functions,
    Views,
    MaterializedViews,
    Partitioning,
    FullTextSearch,
    SpatialData,
    JsonData,
    ArrayData,
    RangeData,
    CustomTypes,
    Extensions
}
```

### PostgreSQL Provider
```csharp
public class PostgreSQLProvider : IDatabaseProvider
{
    public DatabaseProvider Type => DatabaseProvider.PostgreSQL;
    public string Name => "PostgreSQL";
    public string Version => "13+";
    
    private readonly IPostgreSQLConnectionManager _connectionManager;
    private readonly IPostgreSQLSqlGenerator _sqlGenerator;
    private readonly IPostgreSQLTypeMapper _typeMapper;
    private readonly IPostgreSQLFeatureDetector _featureDetector;
    
    public PostgreSQLProvider(
        IPostgreSQLConnectionManager connectionManager,
        IPostgreSQLSqlGenerator sqlGenerator,
        IPostgreSQLTypeMapper typeMapper,
        IPostgreSQLFeatureDetector featureDetector)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        _typeMapper = typeMapper ?? throw new ArgumentNullException(nameof(typeMapper));
        _featureDetector = featureDetector ?? throw new ArgumentNullException(nameof(featureDetector));
    }
    
    public async Task<IDbConnection> CreateConnectionAsync(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        
        return await _connectionManager.CreateConnectionAsync(connectionString);
    }
    
    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = await CreateConnectionAsync(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<DatabaseInfo> GetDatabaseInfoAsync(IDbConnection connection)
    {
        var sql = @"
            SELECT 
                version() as version,
                current_database() as database_name,
                current_user as user_name,
                inet_server_addr() as server_address,
                inet_server_port() as server_port";
        
        var result = await connection.QueryFirstAsync<DatabaseInfo>(sql);
        return result;
    }
    
    public string GenerateSelectSql(EntityMetadata metadata, string whereClause = null, string orderBy = null, int? skip = null, int? take = null)
    {
        return _sqlGenerator.GenerateSelectSql(metadata, whereClause, orderBy, skip, take);
    }
    
    public string GenerateInsertSql(EntityMetadata metadata)
    {
        return _sqlGenerator.GenerateInsertSql(metadata);
    }
    
    public string GenerateUpdateSql(EntityMetadata metadata, string whereClause)
    {
        return _sqlGenerator.GenerateUpdateSql(metadata, whereClause);
    }
    
    public string GenerateDeleteSql(EntityMetadata metadata, string whereClause)
    {
        return _sqlGenerator.GenerateDeleteSql(metadata, whereClause);
    }
    
    public string GenerateCountSql(EntityMetadata metadata, string whereClause = null)
    {
        return _sqlGenerator.GenerateCountSql(metadata, whereClause);
    }
    
    public string GetColumnName(string propertyName, PropertyMetadata property)
    {
        return _sqlGenerator.GetColumnName(propertyName, property);
    }
    
    public string GetTableName(string entityName, EntityMetadata metadata)
    {
        return _sqlGenerator.GetTableName(entityName, metadata);
    }
    
    public string GetParameterName(string parameterName, int index = 0)
    {
        return _sqlGenerator.GetParameterName(parameterName, index);
    }
    
    public string MapToDatabaseType(Type clrType, PropertyMetadata property)
    {
        return _typeMapper.MapToDatabaseType(clrType, property);
    }
    
    public Type MapToClrType(string databaseType, PropertyMetadata property)
    {
        return _typeMapper.MapToClrType(databaseType, property);
    }
    
    public bool SupportsFeature(DatabaseFeature feature)
    {
        return _featureDetector.SupportsFeature(feature);
    }
    
    public string GetFeatureSql(DatabaseFeature feature, params object[] parameters)
    {
        return _featureDetector.GetFeatureSql(feature, parameters);
    }
}
```

### PostgreSQL SQL Generator
```csharp
public class PostgreSQLSqlGenerator : IPostgreSQLSqlGenerator
{
    public string GenerateSelectSql(EntityMetadata metadata, string whereClause = null, string orderBy = null, int? skip = null, int? take = null)
    {
        var columns = string.Join(", ", metadata.Properties.Select(p => p.ColumnName));
        var sql = $"SELECT {columns} FROM {metadata.TableName}";
        
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }
        
        if (!string.IsNullOrEmpty(orderBy))
        {
            sql += $" ORDER BY {orderBy}";
        }
        
        if (skip.HasValue)
        {
            sql += $" OFFSET {skip.Value}";
        }
        
        if (take.HasValue)
        {
            sql += $" LIMIT {take.Value}";
        }
        
        return sql;
    }
    
    public string GenerateInsertSql(EntityMetadata metadata)
    {
        var columns = metadata.Properties
            .Where(p => !p.IsIdentity)
            .Select(p => p.ColumnName)
            .ToArray();
        
        var parameters = columns.Select(c => $"@{c}").ToArray();
        
        var sql = $"INSERT INTO {metadata.TableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})";
        
        if (metadata.HasIdentityColumn)
        {
            var identityColumn = metadata.Properties.First(p => p.IsIdentity);
            sql += $" RETURNING {identityColumn.ColumnName}";
        }
        
        return sql;
    }
    
    public string GenerateUpdateSql(EntityMetadata metadata, string whereClause)
    {
        var columns = metadata.Properties
            .Where(p => !p.IsPrimaryKey)
            .Select(p => $"{p.ColumnName} = @{p.Name}")
            .ToArray();
        
        var sql = $"UPDATE {metadata.TableName} SET {string.Join(", ", columns)}";
        
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }
        
        return sql;
    }
    
    public string GenerateDeleteSql(EntityMetadata metadata, string whereClause)
    {
        var sql = $"DELETE FROM {metadata.TableName}";
        
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }
        
        return sql;
    }
    
    public string GenerateCountSql(EntityMetadata metadata, string whereClause = null)
    {
        var sql = $"SELECT COUNT(*) FROM {metadata.TableName}";
        
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }
        
        return sql;
    }
    
    public string GetColumnName(string propertyName, PropertyMetadata property)
    {
        return property?.ColumnName ?? propertyName.ToSnakeCase();
    }
    
    public string GetTableName(string entityName, EntityMetadata metadata)
    {
        return metadata?.TableName ?? entityName.ToSnakeCase();
    }
    
    public string GetParameterName(string parameterName, int index = 0)
    {
        return $"@{parameterName}";
    }
}
```

### PostgreSQL Type Mapper
```csharp
public class PostgreSQLTypeMapper : IPostgreSQLTypeMapper
{
    private readonly Dictionary<Type, string> _clrToPostgreSQL = new()
    {
        { typeof(string), "TEXT" },
        { typeof(int), "INTEGER" },
        { typeof(long), "BIGINT" },
        { typeof(short), "SMALLINT" },
        { typeof(byte), "SMALLINT" },
        { typeof(bool), "BOOLEAN" },
        { typeof(decimal), "DECIMAL" },
        { typeof(double), "DOUBLE PRECISION" },
        { typeof(float), "REAL" },
        { typeof(DateTime), "TIMESTAMP" },
        { typeof(DateTimeOffset), "TIMESTAMPTZ" },
        { typeof(TimeSpan), "INTERVAL" },
        { typeof(Guid), "UUID" },
        { typeof(byte[]), "BYTEA" },
        { typeof(char), "CHAR(1)" }
    };
    
    private readonly Dictionary<string, Type> _postgreSQLToClr = new()
    {
        { "TEXT", typeof(string) },
        { "VARCHAR", typeof(string) },
        { "CHAR", typeof(string) },
        { "INTEGER", typeof(int) },
        { "BIGINT", typeof(long) },
        { "SMALLINT", typeof(short) },
        { "BOOLEAN", typeof(bool) },
        { "DECIMAL", typeof(decimal) },
        { "NUMERIC", typeof(decimal) },
        { "DOUBLE PRECISION", typeof(double) },
        { "REAL", typeof(float) },
        { "TIMESTAMP", typeof(DateTime) },
        { "TIMESTAMPTZ", typeof(DateTimeOffset) },
        { "INTERVAL", typeof(TimeSpan) },
        { "UUID", typeof(Guid) },
        { "BYTEA", typeof(byte[]) }
    };
    
    public string MapToDatabaseType(Type clrType, PropertyMetadata property)
    {
        if (clrType == typeof(string))
        {
            if (property?.Length.HasValue == true)
            {
                return property.Length.Value <= 255 ? $"VARCHAR({property.Length.Value})" : "TEXT";
            }
            return "TEXT";
        }
        
        if (clrType == typeof(decimal))
        {
            var precision = property?.Precision ?? 18;
            var scale = property?.Scale ?? 2;
            return $"DECIMAL({precision},{scale})";
        }
        
        if (clrType == typeof(DateTime))
        {
            return property?.Precision.HasValue == true ? $"TIMESTAMP({property.Precision.Value})" : "TIMESTAMP";
        }
        
        if (_clrToPostgreSQL.TryGetValue(clrType, out var postgreSQLType))
        {
            return postgreSQLType;
        }
        
        return "TEXT";
    }
    
    public Type MapToClrType(string databaseType, PropertyMetadata property)
    {
        var normalizedType = databaseType.ToUpper().Split('(')[0];
        
        if (_postgreSQLToClr.TryGetValue(normalizedType, out var clrType))
        {
            return clrType;
        }
        
        return typeof(string);
    }
}
```

### MySQL Provider
```csharp
public class MySQLProvider : IDatabaseProvider
{
    public DatabaseProvider Type => DatabaseProvider.MySQL;
    public string Name => "MySQL";
    public string Version => "8.0+";
    
    private readonly IMySQLConnectionManager _connectionManager;
    private readonly IMySQLSqlGenerator _sqlGenerator;
    private readonly IMySQLTypeMapper _typeMapper;
    private readonly IMySQLFeatureDetector _featureDetector;
    
    public MySQLProvider(
        IMySQLConnectionManager connectionManager,
        IMySQLSqlGenerator sqlGenerator,
        IMySQLTypeMapper typeMapper,
        IMySQLFeatureDetector featureDetector)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        _typeMapper = typeMapper ?? throw new ArgumentNullException(nameof(typeMapper));
        _featureDetector = featureDetector ?? throw new ArgumentNullException(nameof(featureDetector));
    }
    
    // ... similar implementation to PostgreSQLProvider
}

public class MySQLSqlGenerator : IMySQLSqlGenerator
{
    public string GenerateSelectSql(EntityMetadata metadata, string whereClause = null, string orderBy = null, int? skip = null, int? take = null)
    {
        var columns = string.Join(", ", metadata.Properties.Select(p => p.ColumnName));
        var sql = $"SELECT {columns} FROM {metadata.TableName}";
        
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }
        
        if (!string.IsNullOrEmpty(orderBy))
        {
            sql += $" ORDER BY {orderBy}";
        }
        
        if (take.HasValue)
        {
            sql += $" LIMIT {take.Value}";
            
            if (skip.HasValue)
            {
                sql += $" OFFSET {skip.Value}";
            }
        }
        
        return sql;
    }
    
    // ... other methods similar to PostgreSQL
}
```

### SQLite Provider
```csharp
public class SQLiteProvider : IDatabaseProvider
{
    public DatabaseProvider Type => DatabaseProvider.SQLite;
    public string Name => "SQLite";
    public string Version => "3.35+";
    
    private readonly ISQLiteConnectionManager _connectionManager;
    private readonly ISQLiteSqlGenerator _sqlGenerator;
    private readonly ISQLiteTypeMapper _typeMapper;
    private readonly ISQLiteFeatureDetector _featureDetector;
    
    public SQLiteProvider(
        ISQLiteConnectionManager connectionManager,
        ISQLiteSqlGenerator sqlGenerator,
        ISQLiteTypeMapper typeMapper,
        ISQLiteFeatureDetector featureDetector)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        _typeMapper = typeMapper ?? throw new ArgumentNullException(nameof(typeMapper));
        _featureDetector = featureDetector ?? throw new ArgumentNullException(nameof(featureDetector));
    }
    
    // ... similar implementation to other providers
}

public class SQLiteSqlGenerator : ISQLiteSqlGenerator
{
    public string GenerateSelectSql(EntityMetadata metadata, string whereClause = null, string orderBy = null, int? skip = null, int? take = null)
    {
        var columns = string.Join(", ", metadata.Properties.Select(p => p.ColumnName));
        var sql = $"SELECT {columns} FROM {metadata.TableName}";
        
        if (!string.IsNullOrEmpty(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }
        
        if (!string.IsNullOrEmpty(orderBy))
        {
            sql += $" ORDER BY {orderBy}";
        }
        
        if (take.HasValue)
        {
            sql += $" LIMIT {take.Value}";
            
            if (skip.HasValue)
            {
                sql += $" OFFSET {skip.Value}";
            }
        }
        
        return sql;
    }
    
    // ... other methods similar to other providers
}
```

### Database Provider Factory
```csharp
public class DatabaseProviderFactory : IDatabaseProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<DatabaseProvider, Type> _providerTypes;
    
    public DatabaseProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _providerTypes = new Dictionary<DatabaseProvider, Type>
        {
            { DatabaseProvider.SqlServer, typeof(SqlServerProvider) },
            { DatabaseProvider.PostgreSQL, typeof(PostgreSQLProvider) },
            { DatabaseProvider.MySQL, typeof(MySQLProvider) },
            { DatabaseProvider.SQLite, typeof(SQLiteProvider) }
        };
    }
    
    public IDatabaseProvider CreateProvider(DatabaseProvider providerType)
    {
        if (!_providerTypes.TryGetValue(providerType, out var providerTypeInfo))
        {
            throw new NotSupportedException($"Database provider {providerType} is not supported");
        }
        
        return (IDatabaseProvider)_serviceProvider.GetService(providerTypeInfo);
    }
    
    public IDatabaseProvider CreateProvider(string connectionString)
    {
        var providerType = DetectProviderFromConnectionString(connectionString);
        return CreateProvider(providerType);
    }
    
    private DatabaseProvider DetectProviderFromConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
        
        var connectionStringBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        
        if (connectionStringBuilder.ContainsKey("Server") || connectionStringBuilder.ContainsKey("Data Source"))
        {
            if (connectionStringBuilder.ContainsKey("Database"))
            {
                return DatabaseProvider.SqlServer;
            }
            else if (connectionStringBuilder.ContainsKey("Initial Catalog"))
            {
                return DatabaseProvider.SqlServer;
            }
        }
        
        if (connectionStringBuilder.ContainsKey("Host") || connectionStringBuilder.ContainsKey("Server"))
        {
            if (connectionStringBuilder.ContainsKey("Database"))
            {
                return DatabaseProvider.PostgreSQL;
            }
        }
        
        if (connectionStringBuilder.ContainsKey("Server") && connectionStringBuilder.ContainsKey("Database"))
        {
            return DatabaseProvider.MySQL;
        }
        
        if (connectionStringBuilder.ContainsKey("Data Source") && connectionStringBuilder.ContainsKey("Version"))
        {
            return DatabaseProvider.SQLite;
        }
        
        throw new NotSupportedException("Could not detect database provider from connection string");
    }
}
```

### Usage Examples
```csharp
// Provider registration
services.AddNPA(config =>
{
    config.DatabaseProvider = DatabaseProvider.PostgreSQL;
    config.ConnectionString = "Host=localhost;Database=MyApp;Username=postgres;Password=password";
});

// Provider usage
public class UserService
{
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IEntityManager _entityManager;
    
    public UserService(IDatabaseProvider databaseProvider, IEntityManager entityManager)
    {
        _databaseProvider = databaseProvider;
        _entityManager = entityManager;
    }
    
    public async Task<User> GetUserAsync(long id)
    {
        var connection = await _databaseProvider.CreateConnectionAsync(_connectionString);
        return await _entityManager.FindAsync<User>(id);
    }
    
    public async Task<bool> TestConnectionAsync()
    {
        return await _databaseProvider.TestConnectionAsync(_connectionString);
    }
    
    public async Task<DatabaseInfo> GetDatabaseInfoAsync()
    {
        var connection = await _databaseProvider.CreateConnectionAsync(_connectionString);
        return await _databaseProvider.GetDatabaseInfoAsync(connection);
    }
}

// Provider-specific features
public class PostgreSQLUserService
{
    private readonly IDatabaseProvider _databaseProvider;
    
    public PostgreSQLUserService(IDatabaseProvider databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }
    
    public async Task<IEnumerable<User>> GetUsersWithJsonDataAsync()
    {
        if (!_databaseProvider.SupportsFeature(DatabaseFeature.JsonData))
        {
            throw new NotSupportedException("JSON data is not supported by this database provider");
        }
        
        var sql = _databaseProvider.GetFeatureSql(DatabaseFeature.JsonData, "users", "metadata");
        // Execute query with JSON support
        return await ExecuteQueryAsync(sql);
    }
}
```

## 🧪 Test Cases

### Provider Abstraction Tests
- [ ] Provider interface
- [ ] Provider factory
- [ ] Provider selection
- [ ] Provider registration

### PostgreSQL Provider Tests
- [ ] Connection management
- [ ] SQL generation
- [ ] Type mapping
- [ ] Feature detection
- [ ] Error handling

### MySQL Provider Tests
- [ ] Connection management
- [ ] SQL generation
- [ ] Type mapping
- [ ] Feature detection
- [ ] Error handling

### SQLite Provider Tests
- [ ] Connection management
- [ ] SQL generation
- [ ] Type mapping
- [ ] Feature detection
- [ ] Error handling

### Integration Tests
- [ ] End-to-end provider operations
- [ ] Cross-provider compatibility
- [ ] Performance testing
- [ ] Error handling

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Provider configuration
- [ ] Connection string examples
- [ ] Provider-specific features
- [ ] Performance considerations
- [ ] Best practices

### Provider Guide
- [ ] Supported providers
- [ ] Provider features
- [ ] Configuration options
- [ ] Migration between providers
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
1. Move to Phase 2.6: Metadata Source Generator
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on provider design
- [ ] Performance considerations for providers
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
