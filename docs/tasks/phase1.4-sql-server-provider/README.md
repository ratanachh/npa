# Phase 1.4: SQL Server Provider

## 📋 Task Overview

**Objective**: Implement a SQL Server database provider that handles SQL Server-specific features and optimizations while using Dapper for data access.

**Priority**: High  
**Estimated Time**: 2-3 days  
**Dependencies**: Phase 1.1 (Entity Mapping), Phase 1.2 (EntityManager), Phase 1.3 (Query Support)  
**Target Framework**: .NET 6.0  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] IDatabaseProvider interface is complete
- [ ] SqlServerProvider class implements all database operations
- [ ] SQL Server-specific features are supported
- [ ] Performance is optimized for SQL Server
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

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

### 2. SqlServerProvider Class
- **Purpose**: SQL Server-specific implementation of database operations
- **Dependencies**: IDbConnection, IMetadataProvider
- **Features**:
  - SQL Server-specific SQL generation
  - Parameter placeholder handling
  - Data type conversion
  - Performance optimizations
  - SQL Server-specific features

### 3. SQL Server-Specific Features
- **Identity Columns**: Support for IDENTITY columns
- **Sequences**: Support for SEQUENCE objects
- **Table-Valued Parameters**: Support for TVPs
- **JSON Support**: Support for JSON data types
- **Spatial Data**: Support for geography/geometry types
- **Full-Text Search**: Support for CONTAINS/FREETEXT

### 4. Performance Optimizations
- **Bulk Operations**: Use SqlBulkCopy for bulk inserts
- **Table-Valued Parameters**: Use TVPs for batch operations
- **Connection Pooling**: Optimize connection usage
- **Query Optimization**: Generate efficient SQL

## 🏗️ Implementation Plan

### Step 1: Create Interfaces
1. Create `IDatabaseProvider` interface
2. Create `ISqlDialect` interface
3. Create `ITypeConverter` interface
4. Create `IBulkOperationProvider` interface

### Step 2: Implement Core Classes
1. Create `SqlServerProvider` class
2. Create `SqlServerDialect` class
3. Create `SqlServerTypeConverter` class
4. Create `SqlServerBulkOperationProvider` class

### Step 3: Implement SQL Generation
1. Implement INSERT SQL generation
2. Implement UPDATE SQL generation
3. Implement DELETE SQL generation
4. Implement SELECT SQL generation
5. Implement COUNT SQL generation

### Step 4: Add SQL Server Features
1. Implement identity column support
2. Implement sequence support
3. Implement table-valued parameters
4. Implement JSON support
5. Implement spatial data support

### Step 5: Add Performance Optimizations
1. Implement bulk operations
2. Implement connection pooling
3. Implement query optimization
4. Add performance monitoring

### Step 6: Create Unit Tests
1. Test all SQL generation
2. Test SQL Server features
3. Test performance optimizations
4. Test error scenarios

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. SQL Server features guide
4. Performance guide

## 📁 File Structure

```
src/NPA.Providers/SqlServer/
├── SqlServerProvider.cs
├── SqlServerDialect.cs
├── SqlServerTypeConverter.cs
├── SqlServerBulkOperationProvider.cs
└── SqlServerExtensions.cs

tests/NPA.Providers.Tests/SqlServer/
├── SqlServerProviderTests.cs
├── SqlServerDialectTests.cs
├── SqlServerTypeConverterTests.cs
└── SqlServerBulkOperationProviderTests.cs
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

### SqlServerProvider Class
```csharp
public class SqlServerProvider : IDatabaseProvider
{
    private readonly ISqlDialect _dialect;
    private readonly ITypeConverter _typeConverter;
    private readonly IBulkOperationProvider _bulkOperationProvider;
    
    public SqlServerProvider()
    {
        _dialect = new SqlServerDialect();
        _typeConverter = new SqlServerTypeConverter();
        _bulkOperationProvider = new SqlServerBulkOperationProvider();
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
        
        return $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList}); SELECT SCOPE_IDENTITY();";
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

### SQL Server-Specific Features
```csharp
// Identity column support
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

// Sequence support
[Entity]
[Table("orders")]
public class Order
{
    [Id]
    [GeneratedValue(GenerationType.Sequence)]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("order_number")]
    public string OrderNumber { get; set; }
}

// JSON support
[Entity]
[Table("products")]
public class Product
{
    [Id]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("metadata", TypeName = "nvarchar(max)")]
    public string Metadata { get; set; } // JSON string
}
```

## 🧪 Test Cases

### SQL Generation Tests
- [ ] Generate INSERT SQL correctly
- [ ] Generate UPDATE SQL correctly
- [ ] Generate DELETE SQL correctly
- [ ] Generate SELECT SQL correctly
- [ ] Generate COUNT SQL correctly
- [ ] Handle identity columns
- [ ] Handle sequences
- [ ] Handle composite keys

### Parameter Handling Tests
- [ ] Generate parameter placeholders
- [ ] Convert parameter values
- [ ] Handle null values
- [ ] Handle different data types
- [ ] Handle SQL Server-specific types

### Bulk Operation Tests
- [ ] Bulk insert operations
- [ ] Bulk update operations
- [ ] Bulk delete operations
- [ ] Handle large datasets
- [ ] Performance testing

### SQL Server Feature Tests
- [ ] Identity column support
- [ ] Sequence support
- [ ] Table-valued parameters
- [ ] JSON data support
- [ ] Spatial data support

### Error Handling Tests
- [ ] Handle invalid metadata
- [ ] Handle connection errors
- [ ] Handle SQL errors
- [ ] Handle timeout errors
- [ ] Handle permission errors

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic database operations
- [ ] SQL Server-specific features
- [ ] Performance optimizations
- [ ] Best practices
- [ ] Error handling

### SQL Server Features Guide
- [ ] Identity columns
- [ ] Sequences
- [ ] Table-valued parameters
- [ ] JSON support
- [ ] Spatial data
- [ ] Full-text search

## 🔍 Code Review Checklist

- [ ] Code follows .NET naming conventions
- [ ] All public members have XML documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance is optimized
- [ ] SQL injection prevention
- [ ] Memory usage is efficient

## 🚀 Next Steps

After completing this task:
1. Move to Phase 1.5: Repository Source Generator (Basic)
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on SQL Server features
- [ ] Performance considerations for bulk operations
- [ ] Integration with Dapper optimizations
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
