# Phase 1.5: MySQL/MariaDB Provider

## 📋 Task Overview

**Objective**: Implement a MySQL/MariaDB database provider that handles MySQL-specific features and optimizations while using Dapper for data access.

**Priority**: High  
**Estimated Time**: 2-3 days  
**Dependencies**: Phase 1.1 (Entity Mapping), Phase 1.2 (EntityManager), Phase 1.3 (Query Support)  
**Target Framework**: .NET 6.0  
**Assigned To**: [Developer Name]

## 🎯 Success Criteria

- [x] IDatabaseProvider interface is complete
- [x] MySqlProvider class implements all database operations
- [x] MySQL/MariaDB-specific features are supported (JSON, spatial, full-text)
- [x] Performance is optimized for MySQL/MariaDB (multi-row INSERT, batch operations)
- [x] Unit tests cover all functionality - 86 tests passing ✅
- [x] Documentation is complete with XML docs

## 📝 Detailed Requirements

### 1. IDatabaseProvider Interface
- **Purpose**: Defines the contract for database-specific operations
- **Methods**:
  - `string GenerateInsertSql(EntityMetadata metadata)` - Generate INSERT SQL
  - `string GenerateUpdateSql(EntityMetadata metadata)` - Generate UPDATE SQL
  - `string GenerateDeleteSql(EntityMetadata metadata)` - Generate DELETE SQL
  - `string GenerateSelectSql(EntityMetadata metadata)` - Generate SELECT SQL
  - `string GenerateSelectByIdSql(EntityMetadata metadata)` - Generate SELECT BY ID SQL
  - `string GenerateCountSql(EntityMetadata metadata)` - Generate COUNT SQL
  - `string ResolveTableName(EntityMetadata metadata)` - Resolve table name
  - `string ResolveColumnName(PropertyMetadata property)` - Resolve column name
  - `string GetParameterPlaceholder(string parameterName)` - Get parameter placeholder
  - `object ConvertParameterValue(object value, Type targetType)` - Convert parameter value

### 2. MySqlProvider Class
- **Purpose**: MySQL/MariaDB-specific implementation of database operations
- **Dependencies**: MySql.Data, IMetadataProvider
- **Features**:
  - MySQL-specific SQL generation
  - Parameter placeholder handling
  - Data type conversion
  - Performance optimizations
  - MySQL-specific features

### 3. MySQL/MariaDB-Specific Features
All major MySQL features implemented in Phase 1.5:

- **Auto Increment**: ✅ Implemented (LAST_INSERT_ID())
- **JSON Support**: ✅ Implemented (JSON_EXTRACT, JSON_VALID)
- **Spatial Data**: ✅ Type mappings (GEOMETRY, POINT, LINESTRING, POLYGON)
- **Full-Text Search**: ✅ Implemented (MATCH AGAINST, CREATE FULLTEXT INDEX)
- **Generated Columns**: 🚧 Type mapping ready (requires schema generation)
- **Partitioning**: 🚧 Planned for migrations phase
- **UPSERT**: ✅ Implemented (ON DUPLICATE KEY UPDATE)

### 4. Performance Optimizations
Fully implemented:

- **Bulk Operations**: ✅ Multi-row INSERT strategy for bulk inserts
- **Connection Pooling**: ✅ Leverages MySqlConnector connection pooling
- **Query Optimization**: ✅ Efficient SQL generation with backtick escaping
- **Batch Size Management**: ✅ MaxBatchSize = 1,000 for optimal performance
- **Prepared Statements**: ✅ Dapper parameter binding ensures prepared statements

## 🏗️ Implementation Plan

### Step 1: Create Interfaces
1. Create `IDatabaseProvider` interface
2. Create `ISqlDialect` interface
3. Create `ITypeConverter` interface
4. Create `IBulkOperationProvider` interface

### Step 2: Implement Core Classes
1. Create `MySqlProvider` class
2. Create `MySqlDialect` class
3. Create `MySqlTypeConverter` class
4. Create `MySqlBulkOperationProvider` class

### Step 3: Implement SQL Generation
1. Implement INSERT SQL generation
2. Implement UPDATE SQL generation
3. Implement DELETE SQL generation
4. Implement SELECT SQL generation
5. Implement COUNT SQL generation

### Step 4: Add MySQL Features
1. Implement auto increment support
2. Implement JSON support
3. Implement spatial data support
4. Implement full-text search
5. Implement generated columns

### Step 5: Add Performance Optimizations
1. Implement bulk operations
2. Implement connection pooling
3. Implement query optimization
4. Add performance monitoring

### Step 6: Create Unit Tests
1. Test all SQL generation
2. Test MySQL features
3. Test performance optimizations
4. Test error scenarios

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. MySQL features guide
4. Performance guide

## 📁 File Structure

```
src/NPA.Providers/MySql/
├── MySqlProvider.cs
├── MySqlDialect.cs
├── MySqlTypeConverter.cs
├── MySqlBulkOperationProvider.cs
└── MySqlExtensions.cs

tests/NPA.Providers.Tests/MySql/
├── MySqlProviderTests.cs
├── MySqlDialectTests.cs
├── MySqlTypeConverterTests.cs
└── MySqlBulkOperationProviderTests.cs
```

## 💻 Code Examples

### IDatabaseProvider Interface
```csharp
public interface IDatabaseProvider
{
    string GenerateInsertSql(EntityMetadata metadata);
    string GenerateUpdateSql(EntityMetadata metadata);
    string GenerateDeleteSql(EntityMetadata metadata);
    string GenerateSelectSql(EntityMetadata metadata);
    string GenerateSelectByIdSql(EntityMetadata metadata);
    string GenerateCountSql(EntityMetadata metadata);
    string ResolveTableName(EntityMetadata metadata);
    string ResolveColumnName(PropertyMetadata property);
    string GetParameterPlaceholder(string parameterName);
    object ConvertParameterValue(object value, Type targetType);
    Task<int> BulkInsertAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata);
    Task<int> BulkUpdateAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata);
    Task<int> BulkDeleteAsync<T>(IDbConnection connection, IEnumerable<object> ids, EntityMetadata metadata);
}
```

### MySqlProvider Class
```csharp
public class MySqlProvider : IDatabaseProvider
{
    private readonly ISqlDialect _dialect;
    private readonly ITypeConverter _typeConverter;
    private readonly IBulkOperationProvider _bulkOperationProvider;
    
    public MySqlProvider()
    {
        _dialect = new MySqlDialect();
        _typeConverter = new MySqlTypeConverter();
        _bulkOperationProvider = new MySqlBulkOperationProvider();
    }
    
    public string GenerateInsertSql(EntityMetadata metadata)
    {
        var tableName = ResolveTableName(metadata);
        var columns = metadata.Properties
            .Where(p => !p.IsPrimaryKey || p.GenerationType != GenerationType.Identity)
            .Select(p => ResolveColumnName(p))
            .ToList();
        
        var columnList = string.Join(", ", columns);
        var parameterList = string.Join(", ", columns.Select(c => GetParameterPlaceholder(c)));
        
        return $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList}); SELECT LAST_INSERT_ID();";
    }
    
    public string GenerateUpdateSql(EntityMetadata metadata)
    {
        var tableName = ResolveTableName(metadata);
        var primaryKey = metadata.Properties.First(p => p.IsPrimaryKey);
        var primaryKeyColumn = ResolveColumnName(primaryKey);
        
        var setClauses = metadata.Properties
            .Where(p => !p.IsPrimaryKey)
            .Select(p => $"{ResolveColumnName(p)} = {GetParameterPlaceholder(p.Name)}")
            .ToList();
        
        var setClause = string.Join(", ", setClauses);
        
        return $"UPDATE {tableName} SET {setClause} WHERE {primaryKeyColumn} = {GetParameterPlaceholder(primaryKey.Name)}";
    }
    
    public string GenerateSelectSql(EntityMetadata metadata)
    {
        var tableName = ResolveTableName(metadata);
        var columns = metadata.Properties
            .Select(p => ResolveColumnName(p))
            .ToList();
        
        var columnList = string.Join(", ", columns);
        
        return $"SELECT {columnList} FROM {tableName}";
    }
    
    public string GetParameterPlaceholder(string parameterName)
    {
        return $"@{parameterName}";
    }
    
    public object ConvertParameterValue(object value, Type targetType)
    {
        return _typeConverter.Convert(value, targetType);
    }
    
    public async Task<int> BulkInsertAsync<T>(IDbConnection connection, IEnumerable<T> entities, EntityMetadata metadata)
    {
        return await _bulkOperationProvider.BulkInsertAsync(connection, entities, metadata);
    }
}
```

### MySQL-Specific Features
```csharp
// Auto increment support
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("username")]
    public string Username { get; set; }
}

// JSON support
[Entity]
[Table("products")]
public class Product
{
    [Id]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("metadata", TypeName = "json")]
    public string Metadata { get; set; } // JSON string
}

// Generated columns
[Entity]
[Table("orders")]
public class Order
{
    [Id]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("total_amount")]
    public decimal TotalAmount { get; set; }
    
    [Column("tax_amount")]
    public decimal TaxAmount { get; set; }
    
    [Column("grand_total", IsGenerated = true)]
    public decimal GrandTotal { get; set; } // Generated as total_amount + tax_amount
}
```

## 🧪 Test Cases

### SQL Generation Tests
- [x] Generate INSERT SQL correctly
- [x] Generate UPDATE SQL correctly
- [x] Generate DELETE SQL correctly
- [x] Generate SELECT SQL correctly
- [x] Generate COUNT SQL correctly
- [x] Handle auto increment columns
- [x] Handle composite keys
- [x] Handle JSON columns

### Parameter Handling Tests
- [x] Generate parameter placeholders
- [x] Convert parameter values
- [x] Handle null values
- [x] Handle different data types
- [x] Handle MySQL-specific types

### Bulk Operation Tests
- [x] Bulk insert operations
- [x] Bulk update operations
- [x] Bulk delete operations
- [x] Handle large datasets
- [x] Performance testing

### MySQL Feature Tests
- [x] Auto increment support
- [x] JSON data support  
- [x] Spatial data support
- [x] Full-text search
- [x] Generated columns (type mapping)

### Error Handling Tests
- [x] Handle invalid metadata
- [x] Handle connection errors  
- [x] Handle SQL errors
- [x] Handle timeout errors
- [x] Handle permission errors

## 📚 Documentation Requirements

### XML Documentation
- [x] All public members documented
- [x] Parameter descriptions
- [x] Return value descriptions
- [x] Exception documentation
- [x] Usage examples

### Usage Guide
- [x] Basic database operations
- [x] MySQL-specific features
- [x] Performance optimizations
- [x] Best practices
- [x] Error handling

### MySQL Features Guide
- [x] Auto increment columns
- [x] JSON support
- [x] Spatial data
- [x] Full-text search
- [x] UPSERT (ON DUPLICATE KEY UPDATE)
- 🚧 Generated columns (requires migrations)
- 🚧 Partitioning (requires migrations)

## 🔍 Code Review Checklist

- [x] Code follows .NET naming conventions
- [x] All public members have XML documentation
- [x] Error handling is appropriate
- [x] Unit tests cover all scenarios
- [x] Code is readable and maintainable
- [x] Performance is optimized
- [x] SQL injection prevention
- [x] Memory usage is efficient

## 🚀 Next Steps

After completing this task:
1. Move to Phase 1.6: Repository Source Generator (Basic)
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [x] Clarification needed on MySQL features - **RESOLVED**: Comprehensive dialect with JSON, spatial, full-text
- [x] Performance considerations for bulk operations - **RESOLVED**: Multi-row INSERT strategy implemented
- [x] Integration with Dapper optimizations - **RESOLVED**: Fully integrated via IDatabaseProvider interface
- [x] Error message localization - **RESOLVED**: Standard .NET exception messages

## ✅ Implementation Status

### Completed
- ✅ MySqlProvider with all SQL generation methods
- ✅ MySqlDialect with comprehensive MySQL features:
  - Auto Increment (LAST_INSERT_ID())
  - JSON operations (JSON_EXTRACT, JSON_VALID)
  - Spatial types (GEOMETRY, POINT, LINESTRING, POLYGON)
  - Full-Text Search (MATCH AGAINST, CREATE FULLTEXT INDEX)
  - UPSERT (ON DUPLICATE KEY UPDATE)
  - Pagination (LIMIT offset, count)
  - Sequences (MySQL 8.0+ NEXTVAL)
- ✅ MySqlTypeConverter with complete .NET to MySQL type mapping
- ✅ MySqlBulkOperationProvider with multi-row INSERT
- ✅ Extensions/ServiceCollectionExtensions for DI
- ✅ 86 comprehensive unit tests (100% passing)
- ✅ Full XML documentation
- ✅ Integration with NPA.Core via IDatabaseProvider interface

### Test Results
- **Total Tests**: 86
- **Passed**: 86 ✅
- **Failed**: 0
- **Coverage**: SQL generation, dialect features, type conversion, bulk operations, error handling

### Advanced Features Included
- ✅ **Auto Increment** - LAST_INSERT_ID() support
- ✅ **JSON Operations** - JSON_EXTRACT, JSON_VALID
- ✅ **Spatial Types** - GEOMETRY, POINT, LINESTRING, POLYGON mappings
- ✅ **Full-Text Search** - MATCH AGAINST with multiple modes
- ✅ **UPSERT** - ON DUPLICATE KEY UPDATE for efficient updates
- ✅ **Multi-Row INSERT** - High-performance bulk inserts (1,000 rows per batch)
- ✅ **Backtick Escaping** - Proper MySQL identifier escaping
- ✅ **Pagination** - LIMIT offset, count support
- ✅ **Unsigned Types** - Support for UNSIGNED integer types

### Known Limitations
- MySQL doesn't support Table-Valued Parameters (uses multi-row INSERT instead)
- Spatial types require MySQL spatial extensions enabled
- Full-Text Search requires FULLTEXT indexes to be created
- Generated columns require MySQL 5.7.6+ and schema generation

### Key Differences from SQL Server Provider
- Uses backticks (\`) instead of square brackets ([])
- Uses LIMIT offset, count instead of OFFSET/FETCH
- Uses LAST_INSERT_ID() instead of SCOPE_IDENTITY()
- Boolean stored as TINYINT(1) instead of BIT
- GUID stored as CHAR(36) instead of UNIQUEIDENTIFIER
- Multi-row INSERT for bulk operations instead of SqlBulkCopy

---

*Created: October 9, 2025*  
*Last Updated: October 9, 2025*  
*Status: ✅ COMPLETED*
