# Phase 2.5: Additional Database Providers

## 📋 Task Overview

**Objective**: Complete database provider support by implementing the SQLite provider, making NPA fully database-agnostic.

**Priority**: Medium  
**Estimated Time**: 1-2 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.4 (All previous Phase 2 tasks)  
**Assigned To**: [Developer Name]  
**Status**: ✅ **COMPLETED** - October 10, 2024

## 🎯 Success Criteria

- [x] PostgreSQL provider is implemented ✅ (Phase 2.5, 132 tests)
- [x] MySQL provider is implemented ✅ (Phase 1.5, 63 tests)
- [x] SQL Server provider is implemented ✅ (Phase 1.4, 63 tests)
- [x] SQLite provider is implemented ✅ (Phase 2.5, 58 tests) **COMPLETED TODAY**
- [x] Provider abstraction is complete ✅
- [x] Unit tests cover all functionality ✅ (316 total tests across all providers)
- [x] Documentation is complete ✅

## 📝 Current Implementation Status

### ✅ Already Implemented Providers

#### 1. **SQL Server Provider** (Phase 1.4) ✅
- **Location:** `src/NPA.Providers.SqlServer/`
- **Files:**
  - `SqlServerProvider.cs` - Main provider implementation
  - `SqlServerDialect.cs` - SQL Server-specific SQL dialect
  - `SqlServerTypeConverter.cs` - Type mapping for SQL Server
  - `SqlServerBulkOperationProvider.cs` - Bulk operations
  - `Extensions/ServiceCollectionExtensions.cs` - DI integration
- **Tests:** 63 tests passing
- **Features:**
  - SCOPE_IDENTITY() for identity columns
  - OFFSET/FETCH for paging
  - Bracket identifiers `[TableName]`
  - No quotes for simple identifiers in aliases

#### 2. **MySQL/MariaDB Provider** (Phase 1.5) ✅
- **Location:** `src/NPA.Providers.MySql/`
- **Files:**
  - `MySqlProvider.cs` - Main provider implementation
  - `MySqlDialect.cs` - MySQL-specific SQL dialect
  - `MySqlTypeConverter.cs` - Type mapping for MySQL
  - `MySqlBulkOperationProvider.cs` - Bulk operations
  - `Extensions/ServiceCollectionExtensions.cs` - DI integration
- **Tests:** 63 tests passing
- **Features:**
  - LAST_INSERT_ID() for identity columns
  - LIMIT/OFFSET for paging
  - Backtick identifiers `` `TableName` ``

#### 3. **PostgreSQL Provider** (Phase 2.5) ✅
- **Location:** `src/NPA.Providers.PostgreSql/`
- **Files:**
  - `PostgreSqlProvider.cs` - Main provider implementation
  - `PostgreSqlDialect.cs` - PostgreSQL-specific SQL dialect
  - `PostgreSqlTypeConverter.cs` - Type mapping for PostgreSQL
  - `PostgreSqlBulkOperationProvider.cs` - Bulk operations with COPY
  - `Extensions/ServiceCollectionExtensions.cs` - DI integration
- **Tests:** 132 tests passing
- **Features:**
  - RETURNING clause for identity columns
  - OFFSET/LIMIT for paging
  - Double-quoted identifiers `"TableName"`
  - JSONB, UUID, arrays, intervals support
  - Full-text search with GIN indexes
  - UPSERT (INSERT...ON CONFLICT)

## 📝 Remaining Requirements - SQLite Provider Only

### SQLite Provider Implementation Needed
- **Connection Management**: SQLite connection handling
- **SQL Generation**: SQLite-specific SQL generation
- **Data Type Mapping**: SQLite data type mapping
- **Feature Support**: SQLite-specific features
- **Performance Optimization**: SQLite-specific optimizations

### SQLite-Specific Features
- **AUTOINCREMENT**: For identity columns
- **LIMIT/OFFSET**: For paging
- **Double quotes**: For identifiers `"TableName"`
- **Pragma support**: For database configuration
- **In-memory database**: Support for `:memory:` databases
- **JSON support**: JSON1 extension
- **Full-text search**: FTS5 extension

## 🏗️ Implementation Plan - SQLite Only

### Step 1: Create SQLite Provider Structure
1. Create `NPA.Providers.Sqlite` project
2. Create `SqliteProvider.cs` class implementing `IDatabaseProvider`
3. Create `SqliteDialect.cs` for SQLite-specific SQL
4. Create `SqliteTypeConverter.cs` for type mapping

### Step 2: Implement SQLite-Specific Features
1. Implement AUTOINCREMENT for identity columns
2. Implement LIMIT/OFFSET for paging (note: different order than other DBs)
3. Implement double-quote identifier escaping
4. Add pragma support for configuration

### Step 3: Implement Bulk Operations
1. Create `SqliteBulkOperationProvider.cs`
2. Implement batch insert optimization
3. Implement transaction-based bulk operations

### Step 4: Add DI Extensions
1. Create `Extensions/ServiceCollectionExtensions.cs`
2. Add provider registration methods

### Step 5: Create Unit Tests
1. Test SQLite provider following existing patterns
2. Test type conversions
3. Test dialect-specific SQL generation
4. Test bulk operations

### Step 6: Add Documentation
1. XML documentation comments
2. Update main README with SQLite support
3. Create implementation summary

## 📁 File Structure for SQLite Provider

```
src/NPA.Providers.Sqlite/           (TO BE CREATED)
├── SqliteProvider.cs
├── SqliteDialect.cs
├── SqliteTypeConverter.cs
├── SqliteBulkOperationProvider.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
└── NPA.Providers.Sqlite.csproj

tests/NPA.Providers.Sqlite.Tests/   (TO BE CREATED)
├── SqliteProviderTests.cs
├── SqliteDialectTests.cs
├── SqliteTypeConverterTests.cs
└── NPA.Providers.Sqlite.Tests.csproj
```

### Existing Provider Structure (for reference)
```
src/
├── NPA.Providers.SqlServer/        ✅ (Phase 1.4)
├── NPA.Providers.MySql/            ✅ (Phase 1.5)
└── NPA.Providers.PostgreSql/       ✅ (Phase 2.5)

tests/
├── NPA.Providers.SqlServer.Tests/  ✅ (63 tests)
├── NPA.Providers.MySql.Tests/      ✅ (63 tests)
└── NPA.Providers.PostgreSql.Tests/ ✅ (132 tests)
```

## 💻 Code Examples for SQLite Provider

### Reference: Existing Provider Pattern
You can reference the existing provider implementations as templates:
- SQL Server: `src/NPA.Providers.SqlServer/`
- MySQL: `src/NPA.Providers.MySql/`
- PostgreSQL: `src/NPA.Providers.PostgreSql/`

### SQLite Provider (To Be Implemented)
```csharp
using System.Data;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Providers;

namespace NPA.Providers.Sqlite;

/// <summary>
/// SQLite-specific database provider implementation.
/// </summary>
public class SqliteProvider : IDatabaseProvider
{
    private readonly ISqlDialect _dialect;
    private readonly ITypeConverter _typeConverter;
    private readonly IBulkOperationProvider _bulkOperationProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteProvider"/> class.
    /// </summary>
    public SqliteProvider()
    {
        _dialect = new SqliteDialect();
        _typeConverter = new SqliteTypeConverter();
        _bulkOperationProvider = new SqliteBulkOperationProvider(_dialect, _typeConverter);
    }

    /// <inheritdoc />
    public string GenerateInsertSql(EntityMetadata metadata)
    {
        // Similar to PostgreSQL but uses AUTOINCREMENT instead of SERIAL
        // Returns last_insert_rowid() for identity columns
    }
    
    // ... implement other IDatabaseProvider methods
}
```

### SQLite Dialect (To Be Implemented)
```csharp
public class SqliteDialect : ISqlDialect
{
    public string ParameterPrefix => "@";
    public string IdentifierPrefix => "\"";
    public string IdentifierSuffix => "\"";
    
    public string GetLastInsertIdSql()
    {
        return "SELECT last_insert_rowid()";
    }
    
    public string GetPagingSql(int skip, int take)
    {
        // SQLite uses LIMIT/OFFSET like PostgreSQL
        if (skip > 0 && take > 0)
            return $"LIMIT {take} OFFSET {skip}";
        else if (take > 0)
            return $"LIMIT {take}";
        else if (skip > 0)
            return $"LIMIT -1 OFFSET {skip}"; // SQLite requires LIMIT with OFFSET
        
        return string.Empty;
    }
}
```

### SQLite Type Converter (To Be Implemented)
```csharp
public class SqliteTypeConverter : ITypeConverter
{
    // SQLite has limited type system (TEXT, INTEGER, REAL, BLOB, NULL)
    // Map CLR types to SQLite affinity types
    private readonly Dictionary<Type, string> _clrToSqlite = new()
    {
        { typeof(string), "TEXT" },
        { typeof(int), "INTEGER" },
        { typeof(long), "INTEGER" },
        { typeof(short), "INTEGER" },
        { typeof(byte), "INTEGER" },
        { typeof(bool), "INTEGER" },  // 0 or 1
        { typeof(decimal), "REAL" },
        { typeof(double), "REAL" },
        { typeof(float), "REAL" },
        { typeof(DateTime), "TEXT" },  // ISO8601 string
        { typeof(DateTimeOffset), "TEXT" },
        { typeof(TimeSpan), "TEXT" },
        { typeof(Guid), "TEXT" },
        { typeof(byte[]), "BLOB" }
    };
    
    public object? ConvertToDatabase(object? value, Type clrType)
    {
        // Special handling for DateTime (store as ISO8601)
        if (value is DateTime dt)
            return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
        // Special handling for bool (store as 0/1)
        if (value is bool b)
            return b ? 1 : 0;
            
        return value;
    }
    
    public object? ConvertFromDatabase(object? value, Type clrType)
    {
        if (value == null || value is DBNull)
            return null;
            
        // Special handling for DateTime
        if (clrType == typeof(DateTime) && value is string dateStr)
            return DateTime.Parse(dateStr);
            
        // Special handling for bool
        if (clrType == typeof(bool))
            return Convert.ToInt64(value) != 0;
            
        return Convert.ChangeType(value, clrType);
    }
}
```

### Usage Examples for SQLite

```csharp
// Recommended: Use provider extension (once implemented)
var connectionString = "Data Source=myapp.db;Version=3;";
services.AddSqliteProvider(connectionString);

// Alternative: Manual registration
// services.AddNpaMetadataProvider(); // Smart registration - uses generated provider if available
// services.AddScoped<IDatabaseProvider, SqliteProvider>();
// services.AddScoped<IDbConnection>(sp =>
// {
//     var connectionString = "Data Source=myapp.db;Version=3;";
//     var connection = new SqliteConnection(connectionString);
//     connection.Open();
//     return connection;
// });
//
// services.AddScoped<IEntityManager>(sp =>
// {
//     var connection = sp.GetRequiredService<IDbConnection>();
//     var metadata = sp.GetRequiredService<IMetadataProvider>();
    var provider = sp.GetRequiredService<IDatabaseProvider>();
    var logger = sp.GetRequiredService<ILogger<EntityManager>>();
    return new EntityManager(connection, metadata, provider, logger);
});

// In-memory SQLite database
services.AddScoped<IDbConnection>(sp =>
{
    var connection = new SqliteConnection("Data Source=:memory:;");
    connection.Open();
    return connection;
});

// SQLite-specific features
var provider = new SqliteProvider();
var lastIdSql = provider.Dialect.GetLastInsertIdSql(); // "SELECT last_insert_rowid()"
var pagingSql = provider.Dialect.GetPagingSql(10, 20); // "LIMIT 20 OFFSET 10"
```

## 🧪 Test Cases for SQLite Provider

### SQLite Provider Tests (To Be Created)
- [ ] SqliteProviderTests
  - [ ] GenerateInsertSql with AUTOINCREMENT
  - [ ] GenerateUpdateSql
  - [ ] GenerateDeleteSql
  - [ ] GenerateSelectSql with LIMIT/OFFSET
  - [ ] GetLastInsertId using last_insert_rowid()
  
- [ ] SqliteDialectTests  
  - [ ] Parameter prefix (@)
  - [ ] Identifier escaping (double quotes)
  - [ ] Paging SQL generation
  - [ ] Last insert ID syntax
  
- [ ] SqliteTypeConverterTests
  - [ ] CLR to SQLite type mapping
  - [ ] DateTime to ISO8601 string conversion
  - [ ] Boolean to INTEGER (0/1) conversion
  - [ ] BLOB handling for byte arrays
  - [ ] NULL handling

### Reference: Existing Provider Tests
- ✅ SQL Server: 63 tests passing (Phase 1.4)
- ✅ MySQL: 63 tests passing (Phase 1.5)
- ✅ PostgreSQL: 132 tests passing (Phase 2.5)

**Target:** ~60-70 tests for SQLite provider (similar to SQL Server and MySQL)

## 📚 Documentation Requirements for SQLite

### XML Documentation
- [ ] All public members documented (follow existing provider patterns)
- [ ] SQLite-specific considerations noted
- [ ] Usage examples with in-memory databases

### Updates Needed
- [ ] Update main README.md with SQLite support
- [ ] Create implementation summary document
- [ ] Add SQLite example to samples

## 🔍 Implementation Checklist

- [ ] Follow existing provider naming conventions (SqlServer, MySql, PostgreSql patterns)
- [ ] All public members have XML documentation
- [ ] Error handling matches other providers
- [ ] Unit tests follow existing test patterns (~60-70 tests)
- [ ] Code is readable and maintainable
- [ ] Performance is optimized (use transactions for bulk operations)
- [ ] Support both file-based and in-memory databases

## 🚀 Next Steps

After completing SQLite provider:
1. Phase 2.5 will be 100% complete (all 4 major database providers)
2. Move to Phase 2.6: Metadata Source Generator (optional)
3. Then begin Phase 3: Transaction & Performance
4. Update main README progress to 11/33 tasks

## Summary

**Current State:**
- ✅ 3 out of 4 providers complete (SQL Server, MySQL, PostgreSQL)
- 🚧 1 provider remaining (SQLite)
- ✅ Provider abstraction complete
- ✅ 258 provider tests passing across 3 providers

**Remaining Work:**
- Implement SQLite provider following existing patterns
- Create ~60-70 tests for SQLite
- Update documentation

---

*Created: Phase 2 Planning*  
*Last Updated: October 10, 2024*  
*Status: ✅ 100% Complete - All 4 Database Providers Implemented*
